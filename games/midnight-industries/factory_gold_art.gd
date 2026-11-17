extends RefCounted
## Warm gold ore and ingots from approved copper/iron art source silhouettes.
## Explicit one-time material remapping preserves metallic highlights, shadow
## detail and the painterly contour rather than using flat yellow polygons.
const Atlas = preload("res://factory_atlas.gd")
static var _ore: Texture2D
static var _ingot: Texture2D

static func ore_texture() -> Texture2D:
	if _ore == null:
		_ore = _recolor(Atlas.sprite(1, 1), false)
	return _ore

static func ingot_texture() -> Texture2D:
	if _ingot == null:
		_ingot = _recolor(Atlas.sprite(3, 1), true)
	return _ingot

static func _recolor(original: Texture2D, ingot: bool) -> Texture2D:
	if original == null:
		return null
	var source: Image = original.get_image()
	if source.is_compressed() and source.decompress() != OK:
		return null
	source.convert(Image.FORMAT_RGBA8)
	for y in range(source.get_height()):
		for x in range(source.get_width()):
			var pixel: Color = source.get_pixel(x, y)
			if pixel.a <= 0.002:
				continue
			var brightness := pixel.r * 0.31 + pixel.g * 0.53 + pixel.b * 0.16
			var shade := clampf((brightness - 0.34) * 1.65, -0.55, 0.55)
			var gold := Color("#E2B447") if not ingot else Color("#F4C85E")
			gold = gold.lightened(shade) if shade > 0.0 else gold.darkened(-shade)
			# Original micro-contrasts remain; only material hue changes.
			gold.r = clampf(gold.r + (pixel.r - brightness) * 0.13, 0.0, 1.0)
			gold.g = clampf(gold.g + (pixel.g - brightness) * 0.10, 0.0, 1.0)
			gold.b = clampf(gold.b + (pixel.b - brightness) * 0.07, 0.0, 1.0)
			gold.a = pixel.a
			source.set_pixel(x, y, gold)
	source.generate_mipmaps()
	return ImageTexture.create_from_image(source)
