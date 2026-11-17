extends Node2D
## One camera-clipped terrain quad blends meadow, mineral ground and flowing
## rivers/lakes. The same deterministic water cells are collision data.
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const MASK_SCALE := 2
var model
var meadow_rect: ColorRect
static var _grass_detail_cache: Texture2D
static var _stone_detail_cache: Texture2D

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
uniform sampler2D terrain_detail : filter_linear_mipmap, repeat_enable;
uniform sampler2D stone_detail : filter_linear_mipmap, repeat_enable;
uniform sampler2D water_info : filter_linear, repeat_disable;

void fragment() {
	vec2 tiles = UV * vec2(224.0, 176.0);
	float mask = texture(rock_influence, UV).r;
	// Each tile receives ~128 source texels at the close-detail scale.
	// Actual painted blades and sharp grit survive zooming; the previous
	// smooth-noise-only ground blurred even with 1024px source images.
	float g_macro = texture(terrain_detail, tiles * 0.019).r;
	float g_mid = texture(terrain_detail, tiles * 0.055 + vec2(0.28, 0.41)).r;
	float g_blades = texture(terrain_detail, tiles * 0.125 + vec2(0.63, 0.15)).r;
	float g_specks = texture(terrain_detail, tiles * 0.165 + vec2(0.11, 0.72)).r;
	float s_macro = texture(stone_detail, tiles * 0.022).r;
	float s_mid = texture(stone_detail, tiles * 0.061 + vec2(0.72, 0.34)).r;
	float s_grit = texture(stone_detail, tiles * 0.125 + vec2(0.34, 0.07)).r;
	float s_grains = texture(stone_detail, tiles * 0.175 + vec2(0.53, 0.18)).r;
	float edge_noise = (g_mid - 0.5) * 0.15 + (s_macro - 0.5) * 0.16;
	float rock = smoothstep(0.17, 0.79, mask + edge_noise);

	vec3 meadow = mix(vec3(0.130, 0.213, 0.111),
		vec3(0.215, 0.292, 0.154), g_macro * 0.44 + g_mid * 0.56);
	meadow += (g_blades - 0.5) * vec3(0.145, 0.167, 0.079);
	meadow += (g_specks - 0.5) * vec3(0.041, 0.063, 0.016);
	vec3 stones = mix(vec3(0.227, 0.224, 0.207),
		vec3(0.343, 0.333, 0.295), s_macro * 0.35 + s_mid * 0.65);
	stones += (s_grit - 0.5) * vec3(0.175, 0.159, 0.140);
	stones += (s_grains - 0.5) * vec3(0.062, 0.057, 0.049);
	float transition = smoothstep(0.08, 0.40, rock) *
		(1.0 - smoothstep(0.67, 0.98, rock));
	vec3 result = mix(meadow, stones, rock);
	result = mix(result, vec3(0.270, 0.245, 0.188), transition * 0.12);

	// One flow vector per water cell (R = mask, GB = river direction,
	// A = river/lake type). Flow motion runs entirely on the GPU.
	vec4 water = texture(water_info, UV);
	float coverage = smoothstep(0.21, 0.74, water.r);
	vec2 flow = water.gb * 2.0 - vec2(1.0);
	float river = smoothstep(0.15, 0.75, water.a);
	float river_phase = dot(tiles, flow) * 5.5 - TIME * 2.5;
	float transverse = dot(tiles, vec2(-flow.y, flow.x));
	float moving_ripples = sin(river_phase + sin(transverse * 2.3) * 0.7) * 0.5
		+ sin(river_phase * 1.63 + transverse * 3.1) * 0.25;
	float lake_ripples = sin(tiles.x * 4.1 + TIME * 0.55) *
		sin(tiles.y * 3.7 - TIME * 0.42) * 0.35;
	float motion = mix(lake_ripples, moving_ripples, river);
	float water_noise = texture(stone_detail,
		tiles * 0.11 + flow * TIME * 0.01).r - 0.5;
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
	meadow_rect.material = material
	add_child(meadow_rect)

func _grass_texture() -> Texture2D:
	if _grass_detail_cache == null:
		_grass_detail_cache = _surface_texture(17291, 0.022, false)
	return _grass_detail_cache

func _stone_texture() -> Texture2D:
	if _stone_detail_cache == null:
		_stone_detail_cache = _surface_texture(28719, 0.042, true)
	return _stone_detail_cache

func _surface_texture(seed: int, frequency: float, stone: bool) -> Texture2D:
	# Native seamless base gives organic large-scale color, then sharp
	# individual blades or gravel facets are painted at source resolution.
	# Unlike smooth noise alone, close camera zoom reveals real fine shapes.
	var noise := FastNoiseLite.new()
	noise.seed = seed
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.frequency = frequency
	noise.fractal_type = FastNoiseLite.FRACTAL_FBM
	noise.fractal_octaves = 4
	var image: Image = noise.get_seamless_image(1024, 1024)
	image.convert(Image.FORMAT_RGBA8)
	var rng := RandomNumberGenerator.new()
	rng.seed = seed
	var count := 27000 if stone else 23000
	for i in range(count):
		var x := rng.randi_range(0, 1023)
		var y := rng.randi_range(0, 1023)
		var bright := rng.randf() > 0.53
		var tone := rng.randf_range(0.70, 0.89) if bright else rng.randf_range(0.18, 0.35)
		var color := Color(tone, tone, tone, 1.0)
		if stone:
			# Broken gravel flecks, usually 2-4 source pixels across.
			var width := rng.randi_range(1, 4)
			var height := rng.randi_range(1, 3)
			for dy in range(height):
				for dx in range(width):
					image.set_pixel(posmod(x + dx, 1024), posmod(y + dy, 1024), color)
		else:
			# Slender, slightly slanted grass strokes, not round noise blobs.
			var length := rng.randi_range(4, 10)
			var slant := -1 if rng.randf() < 0.52 else 1
			for pixel in range(length):
				var px := posmod(x + slant * (pixel / 4), 1024)
				var py := posmod(y - pixel, 1024)
				image.set_pixel(px, py, color)
	image.generate_mipmaps()
	return ImageTexture.create_from_image(image)

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
