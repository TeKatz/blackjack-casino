extends RefCounted
## 144px approved industrial artwork. Extract each cell into its own GPU
## texture: linear/mipmap filtering of adjacent atlas cells caused stray dark
## strips beside warehouses and belts when other buildings touched them.
const CELL := 144
const TEXTURE = preload("res://assets/industrial_hd_atlas.webp")
static var _isolated: Dictionary = {}

static func get_texture() -> Texture2D:
	return TEXTURE

static func region(col: int, row: int = 0) -> Rect2:
	return Rect2(float(col * CELL), float(row * CELL), float(CELL), float(CELL))

static func sprite(col: int, row: int = 0) -> Texture2D:
	var key := Vector2i(col, row)
	if _isolated.has(key):
		return _isolated[key]
	var sheet: Image = TEXTURE.get_image()
	if sheet.is_compressed():
		if sheet.decompress() != OK:
			return null
	var image: Image = sheet.get_region(Rect2i(col * CELL, row * CELL, CELL, CELL))
	image.convert(Image.FORMAT_RGBA8)
	if col == 3 and row == 0:
		# The warehouse starts at the left boundary of its atlas cell.
		# Remove the three-pixel compression/filtering fringe only; leave
		# its approved metal body and all other machine artwork untouched.
		for y in range(CELL):
			for x in range(3):
				image.set_pixel(x, y, Color.TRANSPARENT)
	image.generate_mipmaps()
	var result: ImageTexture = ImageTexture.create_from_image(image)
	_isolated[key] = result
	return result
