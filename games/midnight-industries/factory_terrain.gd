extends Node2D
## Visual-only biome. One fullscreen-cullable world quad blends meadow and rocky
## ore ground in a shader; the fixed factory/resource model is never touched.
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

void fragment() {
	vec2 tiles = UV * vec2(128.0, 96.0);
	float mask = texture(rock_influence, UV).r;

	// Both materials sample 1024px textures at several physical scales.
	// The fine sampling stays detailed even at the maximum camera zoom.
	float grass_macro = texture(terrain_detail, tiles * 0.018).r;
	float grass_mid = texture(terrain_detail, tiles * 0.053 + vec2(0.28, 0.41)).r;
	float grass_fine = texture(terrain_detail, tiles * 0.105 + vec2(0.63, 0.15)).r;
	float blades = texture(terrain_detail, tiles * 0.21 + vec2(0.11, 0.72)).r;
	float stone_macro = texture(stone_detail, tiles * 0.020).r;
	float stone_mid = texture(stone_detail, tiles * 0.072 + vec2(0.72, 0.34)).r;
	float stone_fine = texture(stone_detail, tiles * 0.135 + vec2(0.34, 0.07)).r;
	float stone_grit = texture(stone_detail, tiles * 0.24 + vec2(0.53, 0.18)).r;
	float edge_noise = (grass_mid - 0.5) * 0.17 + (stone_macro - 0.5) * 0.16;
	float rock = smoothstep(0.17, 0.79, mask + edge_noise);

	vec3 meadow = mix(vec3(0.128, 0.209, 0.108),
		vec3(0.217, 0.292, 0.150), grass_macro * 0.42 + grass_mid * 0.58);
	meadow += (grass_fine - 0.5) * vec3(0.065, 0.087, 0.036);
	// Delicate blades and soil flecks add crisp texture, without a noisy grid.
	float blade_highlights = smoothstep(0.62, 0.79, blades);
	float blade_shadows = 1.0 - smoothstep(0.25, 0.41, blades);
	meadow += blade_highlights * vec3(0.029, 0.050, 0.011);
	meadow -= blade_shadows * vec3(0.016, 0.023, 0.010);

	vec3 stones = mix(vec3(0.227, 0.224, 0.207),
		vec3(0.343, 0.332, 0.295), stone_macro * 0.35 + stone_mid * 0.65);
	stones += (stone_fine - 0.5) * vec3(0.091, 0.083, 0.075);
	float granular_highlight = smoothstep(0.69, 0.83, stone_grit);
	float granular_shadow = 1.0 - smoothstep(0.22, 0.36, stone_grit);
	stones += granular_highlight * vec3(0.069, 0.062, 0.052);
	stones -= granular_shadow * vec3(0.034, 0.032, 0.031);

	float transition = smoothstep(0.08, 0.40, rock) *
		(1.0 - smoothstep(0.67, 0.98, rock));
	vec3 result = mix(meadow, stones, rock);
	result = mix(result, vec3(0.270, 0.245, 0.188), transition * 0.12);
	COLOR = vec4(result, 1.0);
}
"""
	material.shader = shader
	material.set_shader_parameter("rock_influence", _rock_mask())
	material.set_shader_parameter("terrain_detail", _grass_texture())
	material.set_shader_parameter("stone_detail", _stone_texture())
	meadow_rect.material = material
	add_child(meadow_rect)

func _grass_texture() -> Texture2D:
	if _grass_detail_cache == null:
		_grass_detail_cache = _noise_texture(17291, 0.017)
	return _grass_detail_cache

func _stone_texture() -> Texture2D:
	if _stone_detail_cache == null:
		_stone_detail_cache = _noise_texture(28719, 0.033)
	return _stone_detail_cache

func _noise_texture(seed: int, frequency: float) -> Texture2D:
	# FastNoiseLite fills these 1024px greyscale textures in native code
	# instead of looping through millions of pixels in GDScript.
	var noise := FastNoiseLite.new()
	noise.seed = seed
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.frequency = frequency
	noise.fractal_type = FastNoiseLite.FRACTAL_FBM
	noise.fractal_octaves = 5
	var image: Image = noise.get_seamless_image(1024, 1024)
	image.generate_mipmaps()
	return ImageTexture.create_from_image(image)

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
