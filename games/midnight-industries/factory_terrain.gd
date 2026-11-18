extends Node2D
## One camera-clipped terrain quad blends original Poly Haven CC0 diffuse
## textures (grass_ground, rocks_ground_05) under existing ore biome masks.
## Rivers/lakes retain their original deterministic mask and GPU animation.
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const MASK_SCALE := 2
var model
var meadow_rect: ColorRect
var water_motion := true
const GRASS_DIFFUSE = preload("res://assets/grass_ground_diff_1k.jpg")
const ROCKS_DIFFUSE = preload("res://assets/rocks_ground_05_diff_1k.jpg")

func _ready() -> void:
	_build_terrain()

func _build_terrain() -> void:
	meadow_rect = ColorRect.new()
	meadow_rect.name = "SoftMeadowAndOreBiomes"
	meadow_rect.position = Vector2.ZERO
	meadow_rect.size = Vector2(Model.WIDTH, Model.HEIGHT) * TILE
	meadow_rect.mouse_filter = Control.MOUSE_FILTER_IGNORE
	meadow_rect.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	var material := ShaderMaterial.new()
	var shader := Shader.new()
	shader.code = """
shader_type canvas_item;
render_mode unshaded;
uniform sampler2D rock_influence : filter_linear, repeat_disable;
uniform sampler2D terrain_detail : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D stone_detail : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D water_info : filter_linear, repeat_disable;
uniform float water_speed = 1.0;

void fragment() {
	vec2 tiles = UV * vec2(224.0, 176.0);
	float mask = texture(rock_influence, UV).r;
	// CC0 Poly Haven materials are sampled in full RGB, preserving the
	// actual grass/soil and pebbled-rock color data instead of recoloring
	// procedural grayscale noise. Each 1K image repeats roughly every
	// 12-15 terrain tiles (48 world pixels per tile).
	vec3 grass_primary = texture(terrain_detail, tiles * 0.082).rgb;
	vec3 grass_secondary = texture(terrain_detail,
		tiles * 0.121 + vec2(0.41, 0.69)).rgb;
	vec3 rock_primary = texture(stone_detail, tiles * 0.071).rgb;
	vec3 rock_secondary = texture(stone_detail,
		tiles * 0.103 + vec2(0.65, 0.23)).rgb;

	// Lightly layered scales reduce visible repetition on the large map,
	// without hiding distinctive color and grain of the source textures.
	vec3 meadow = mix(grass_primary, grass_secondary, 0.12);
	vec3 stones = mix(rock_primary, rock_secondary, 0.10);

	float g_luma = dot(grass_secondary, vec3(0.299, 0.587, 0.114));
	float s_luma = dot(rock_secondary, vec3(0.299, 0.587, 0.114));
	float edge_noise = (g_luma - 0.5) * 0.16 +
		(s_luma - 0.5) * 0.19;
	float rock = smoothstep(0.17, 0.79, mask + edge_noise);
	float transition = smoothstep(0.10, 0.37, rock) *
		(1.0 - smoothstep(0.73, 0.97, rock));
	vec3 result = mix(meadow, stones, rock);
	// Subtle soil tint in the transition only, not on solid rock/grass.
	result = mix(result, mix(meadow, stones, 0.5),
		transition * 0.085);

	// One flow vector per water cell (R = mask, GB = river direction,
	// A = river/lake type). Flow motion runs entirely on the GPU.
	vec4 water = texture(water_info, UV);
	float coverage = smoothstep(0.21, 0.74, water.r);
	vec2 flow = water.gb * 2.0 - vec2(1.0);
	float river = smoothstep(0.15, 0.75, water.a);
	float river_phase = dot(tiles, flow) * 5.5 - TIME * water_speed * 2.5;
	float transverse = dot(tiles, vec2(-flow.y, flow.x));
	float moving_ripples = sin(river_phase + sin(transverse * 2.3) * 0.7) * 0.5
		+ sin(river_phase * 1.63 + transverse * 3.1) * 0.25;
	float lake_ripples = sin(tiles.x * 4.1 + TIME * water_speed * 0.55) *
		sin(tiles.y * 3.7 - TIME * water_speed * 0.42) * 0.35;
	float motion = mix(lake_ripples, moving_ripples, river);
	float water_noise = texture(stone_detail,
		tiles * 0.11 + flow * TIME * water_speed * 0.01).r - 0.5;
	vec3 deep = vec3(0.042, 0.179, 0.224);
	vec3 shallow = vec3(0.088, 0.277, 0.318);
	vec3 water_color = mix(deep, shallow,
		clamp(0.50 + motion * 0.16 + water_noise * 0.22, 0.0, 1.0));
	// Light shore foam where water and land meet, without covering ore.
	float edge = smoothstep(0.18, 0.55, water.r) *
		(1.0 - smoothstep(0.68, 0.95, water.r));
	result = mix(result, vec3(0.23, 0.32, 0.30), edge * 0.36);
	result = mix(result, water_color, coverage);
	result += vec3(0.030, 0.062, 0.061) * edge * coverage;
	COLOR = vec4(result, 1.0);
}
"""
	material.shader = shader
	material.set_shader_parameter("rock_influence", _rock_mask())
	material.set_shader_parameter("terrain_detail", _grass_texture())
	material.set_shader_parameter("stone_detail", _stone_texture())
	material.set_shader_parameter("water_info", _water_texture())
	material.set_shader_parameter("water_speed", 1.0 if water_motion else 0.0)
	meadow_rect.material = material
	add_child(meadow_rect)

func _grass_texture() -> Texture2D:
	return GRASS_DIFFUSE

func _stone_texture() -> Texture2D:
	return ROCKS_DIFFUSE

func refresh_from_model() -> void:
	if meadow_rect == null:
		return
	var surface: ShaderMaterial = meadow_rect.material
	surface.set_shader_parameter("rock_influence", _rock_mask())
	surface.set_shader_parameter("water_info", _water_texture())

func _water_texture() -> Texture2D:
	var image := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	image.fill(Color(0.0, 0.5, 0.5, 0.0))
	for cell in model.water:
		var water: Dictionary = model.water[cell]
		var flow: Vector2 = water["flow"]
		image.set_pixel(cell.x, cell.y, Color(1.0,
			0.5 + flow.x * 0.5, 0.5 + flow.y * 0.5,
			1.0 if water["kind"] == "river" else 0.0))
	return ImageTexture.create_from_image(image)

func _rock_mask() -> Texture2D:
	# Mark a 2.65-cell falloff around each actual mineral tile. Painting
	# local regions costs far less than sampling a 5x5 mineral neighborhood
	# for every pixel of the newly expanded 224x176 map.
	var size := Vector2i(Model.WIDTH * MASK_SCALE, Model.HEIGHT * MASK_SCALE)
	var image := Image.create(size.x, size.y, false, Image.FORMAT_L8)
	image.fill(Color.BLACK)
	var radius := 2.65
	var reach := ceili(radius * float(MASK_SCALE))
	for ore in model.resources:
		var at: Vector2i = ore
		var px := at.x * MASK_SCALE
		var py := at.y * MASK_SCALE
		for y in range(maxi(0, py - reach), mini(size.y, py + reach + 1)):
			for x in range(maxi(0, px - reach), mini(size.x, px + reach + 1)):
				var dist := Vector2(float(x - px), float(y - py)).length() \
					/ float(MASK_SCALE)
				var strength := maxf(0.0, 1.0 - dist / radius)
				if strength <= 0.0:
					continue
				var old: float = image.get_pixel(x, y).r
				if strength > old:
					image.set_pixel(x, y, Color(strength, strength, strength))
	return ImageTexture.create_from_image(image)

func set_water_motion(enabled: bool) -> void:
	water_motion = enabled
	if meadow_rect != null:
		(meadow_rect.material as ShaderMaterial).set_shader_parameter("water_speed", 1.0 if enabled else 0.0)
