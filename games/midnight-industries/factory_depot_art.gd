extends RefCounted
## Depot variant of the approved industrial storage art. Keeps the same 144px
## illuminated-metal style but adds cloud-storage hardware and teal lighting.
const Atlas = preload("res://factory_atlas.gd")
static var _texture: Texture2D

static func texture() -> Texture2D:
	if _texture != null:
		return _texture
	var base: Texture2D = Atlas.sprite(3)
	if base == null:
		return null
	var image: Image = base.get_image().duplicate()
	if image.is_compressed() and image.decompress() != OK:
		return null
	image.convert(Image.FORMAT_RGBA8)
	for y in range(29, 119):
		for x in range(21, 127):
			var color: Color = image.get_pixel(x, y)
			if color.a < 0.2:
				continue
			# Restrained new teal panels; don't replace underlying detail.
			if color.b > color.r * 1.02 and color.g > color.r * 0.84:
				color = color.lerp(Color("#178BA4"), 0.22)
				image.set_pixel(x, y, color)
	# Heavier industrial communications spine and cyan luminous bars.
	image.fill_rect(Rect2i(22, 64, 8, 39), Color("#142B36"))
	image.fill_rect(Rect2i(24, 68, 4, 31), Color("#42BDD3"))
	image.fill_rect(Rect2i(115, 64, 7, 39), Color("#142B36"))
	image.fill_rect(Rect2i(117, 68, 3, 31), Color("#31BCD5"))
	image.fill_rect(Rect2i(51, 32, 44, 8), Color("#152D36"))
	image.fill_rect(Rect2i(53, 35, 40, 3), Color("#28D7F0"))
	# Cloud terminal LED array.
	for x in [58, 70, 82]:
		image.fill_rect(Rect2i(x, 47, 5, 5), Color("#041820"))
		image.fill_rect(Rect2i(x + 1, 48, 3, 2), Color("#8DE9F4"))
	image.generate_mipmaps()
	_texture = ImageTexture.create_from_image(image)
	return _texture
