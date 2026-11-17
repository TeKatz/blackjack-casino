extends RefCounted
## Isolated versions of the approved 144px industrial sprites.
## Storage's visible left spur was inside its original source cell; fixing
## texture filtering alone could never remove it. Its left machinery side is
## rebuilt from the undamaged right-side silhouette.
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
	if sheet.is_compressed() and sheet.decompress() != OK:
		return null
	var cell: Image = sheet.get_region(Rect2i(col * CELL, row * CELL, CELL, CELL))
	cell.convert(Image.FORMAT_RGBA8)
	if col == 3 and row == 0:
		cell = _repair_storage(cell)
	cell.generate_mipmaps()
	var isolated: ImageTexture = ImageTexture.create_from_image(cell)
	_isolated[key] = isolated
	return isolated

static func _repair_storage(source: Image) -> Image:
	var repaired: Image = source.duplicate()
	# Not atlas bleed: the stray silhouette is already baked into the
	# original left quarter. Reconstruct that side using the right-hand
	# matching steel side-panel rather than trimming a mere 3 source pixels.
	for y in range(CELL):
		for x in range(50):
			var donor: Color = source.get_pixel(CELL - 1 - x, y)
			repaired.set_pixel(x, y, donor)
		# Keep mipmapping from reviving a stray edge fringe.
		for x in range(3):
			repaired.set_pixel(x, y, Color.TRANSPARENT)
	return repaired
