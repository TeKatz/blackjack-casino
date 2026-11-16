extends Node2D
## Visual-only biome. One fullscreen-cullable world quad blends meadow and rocky
## ore ground in a shader; the fixed factory/resource model is never touched.
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const MASK_SCALE := 2
var model
var meadow_rect: ColorRect

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
void fragment() {
	vec2 tiles = UV * vec2(128.0, 96.0);
	vec4 fine = texture(terrain_detail, tiles * 0.33);
	vec4 broad = texture(terrain_detail, tiles * 0.052 + vec2(0.14, 0.31));
	float mask = texture(rock_influence, UV).r;
	float edge_breakup = (fine.g - 0.5) * 0.16 + (broad.b - 0.5) * 0.23;
	float rock = smoothstep(0.17, 0.77, mask + edge_breakup);
	float grit = (fine.r - 0.5) * 0.13 + (broad.g - 0.5) * 0.09;
	vec3 grass = mix(vec3(0.145, 0.238, 0.124), vec3(0.226, 0.313, 0.164), clamp(fine.r * 0.66 + broad.r * 0.34, 0.0, 1.0));
	grass += vec3(0.022, 0.039, 0.010) * grit;
	// The rock is grey earth, muted so machine LEDs remain legible.
	vec3 stone = mix(vec3(0.225, 0.229, 0.217), vec3(0.325, 0.316, 0.284), fine.b * 0.60 + broad.g * 0.40);
	stone += vec3(grit * 0.40);
	float edge = smoothstep(0.05, 0.42, rock) * (1.0 - smoothstep(0.65, 0.99, rock));
	vec3 earth = vec3(0.285, 0.252, 0.178);
	vec3 tint = mix(grass, stone, rock);
	tint = mix(tint, earth, edge * 0.17);
	// Sparse subdued blades and gravel glints rather than repeating tile detail.
	float speck = smoothstep(0.76, 0.91, fine.a) * (1.0 - rock);
	tint += vec3(0.018, 0.036, 0.003) * speck;
	COLOR = vec4(tint, 1.0);
}
"""
	material.shader = shader
	material.set_shader_parameter("rock_influence", _rock_mask())
	material.set_shader_parameter("terrain_detail", _detail_texture())
	meadow_rect.material = material
	add_child(meadow_rect)

func _detail_texture() -> Texture2D:
	var w := 256
	var h := 256
	var a := FastNoiseLite.new()
	a.seed = 17291
	a.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	a.frequency = 0.045
	a.fractal_type = FastNoiseLite.FRACTAL_FBM
	a.fractal_octaves = 3
	var b := FastNoiseLite.new()
	b.seed = 28719
	b.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	b.frequency = 0.10
	b.fractal_type = FastNoiseLite.FRACTAL_FBM
	b.fractal_octaves = 3
	var img := Image.create(w, h, false, Image.FORMAT_RGBA8)
	for y in range(h):
		for x in range(w):
			var v: float = clampf((a.get_noise_2d(x, y) + 1.0) * 0.5, 0.0, 1.0)
			var u: float = clampf((b.get_noise_2d(x, y) + 1.0) * 0.5, 0.0, 1.0)
			var fleck: float = fposmod(float(x * 17 + y * 37 + x * y * 7), 103.0) / 103.0
			img.set_pixel(x, y, Color(v, u, clampf(v * 0.36 + u * 0.64, 0.0, 1.0), fleck))
	img.generate_mipmaps()
	return ImageTexture.create_from_image(img)

func _rock_mask() -> Texture2D:
	# Nearby resources influence ground continuously, even between ore tiles.
	# Smooth bilinear sampling and fine detail breakup conceal grid borders.
	var size := Vector2i(Model.WIDTH * MASK_SCALE, Model.HEIGHT * MASK_SCALE)
	var img := Image.create(size.x, size.y, false, Image.FORMAT_L8)
	for y in range(size.y):
		for x in range(size.x):
			var cx := float(x) / MASK_SCALE
			var cy := float(y) / MASK_SCALE
			var center := Vector2i(floori(cx), floori(cy))
			var weight := 0.0
			for dy in range(-2, 3):
				for dx in range(-2, 3):
					var p := center + Vector2i(dx, dy)
					if not model.resources.has(p):
						continue
					var dist := Vector2(cx - float(p.x), cy - float(p.y)).length()
					weight = maxf(weight, 1.0 - dist / 2.65)
			img.set_pixel(x, y, Color(weight, weight, weight))
	return ImageTexture.create_from_image(img)
