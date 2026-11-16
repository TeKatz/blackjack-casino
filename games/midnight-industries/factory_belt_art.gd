extends RefCounted
## Dedicated vertical industrial belts. Rotate ONLY the detailed moving
## belt surface from the approved artwork. The perspective chassis, feet,
## rails, lighting and connectors are rebuilt for north/south orientation;
## rotating the complete horizontal sprite produces sideways supports.
const Atlas = preload("res://factory_atlas.gd")
const SIZE := 144
static var _north: ImageTexture
static var _south: ImageTexture

static func texture(direction: int) -> Texture2D:
	if posmod(direction, 4) == 1:
		if _south == null:
			_south = _create(true)
		return _south
	if _north == null:
		_north = _create(false)
	return _north

static func _create(south: bool) -> ImageTexture:
	var original: Texture2D = Atlas.sprite(1)
	if original == null:
		return null
	var source: Image = original.get_image()
	if source.is_compressed():
		source.decompress()
	var im := Image.create(SIZE, SIZE, false, Image.FORMAT_RGBA8)
	im.fill(Color.TRANSPARENT)
	# Continuous steel pan to meet the next vertical tile exactly.
	_rect(im, 25, 0, 94, SIZE, Color("#0C1820"))
	_rect(im, 29, 0, 86, SIZE, Color("#344650"))
	_rect(im, 32, 0, 80, SIZE, Color("#596E79"))
	_rect(im, 37, 0, 70, SIZE, Color("#101E27"))
	# Sample the original conveyor deck only (central strip of the
	# horizontal asset). Preserve its shading, illustrated rollers,
	# crosshatching and cyan arrows, oriented to the travel direction.
	for y in range(SIZE):
		for x in range(40, 104):
			var sx := y if south else SIZE - 1 - y
			var sy := SIZE - 1 - x if south else x
			var ink: Color = source.get_pixel(sx, sy)
			if ink.a > 0.025:
				im.set_pixel(x, y, ink)
	# New side rails face the viewer. Their highlights never rotate when
	# direction changes, unlike those in a rotated full horizontal sprite.
	_rect(im, 28, 0, 12, SIZE, Color("#162832"))
	_rect(im, 28, 0, 3, SIZE, Color("#90A6B1"))
	_rect(im, 32, 0, 3, SIZE, Color("#4C6573"))
	_rect(im, 37, 0, 3, SIZE, Color("#0B1820"))
	_rect(im, 104, 0, 12, SIZE, Color("#283D48"))
	_rect(im, 105, 0, 3, SIZE, Color("#92A8B0"))
	_rect(im, 112, 0, 4, SIZE, Color("#101C24"))
	# Three sets of chunky side-mount rollers, bevels, amber fasteners and
	# cyan status lamps match the horizontal belt's industrial silhouette.
	for y in [10, 56, 102]:
		_support(im, y, true)
		_support(im, y, false)
	im.generate_mipmaps()
	return ImageTexture.create_from_image(im)

static func _support(im: Image, y: int, left: bool) -> void:
	var x := 15 if left else 111
	_rect(im, x, y + 6, 19, 21, Color("#07131A"))
	_rect(im, x + 2, y + 4, 17, 18, Color("#283F4C"))
	_rect(im, x + 3, y + 3, 15, 4, Color("#8396A0"))
	_rect(im, x + 5, y + 4, 10, 2, Color("#D5E0DB"))
	_rect(im, x + 4, y + 11, 14, 7, Color("#142631"))
	_rect(im, x + 6, y + 12, 10, 4, Color("#946A39"))
	_rect(im, x + 7, y + 12, 7, 2, Color("#F4C477"))
	_rect(im, x + 3, y + 20, 15, 6, Color("#09212A"))
	_rect(im, x + 5, y + 22, 10, 3, Color("#00B4D6"))
	_rect(im, x + 6, y + 22, 7, 1, Color("#8BECF4"))
	_rect(im, x + 1, y + 7, 2, 10, Color("#516571") if left else Color("#0A151A"))

static func _rect(im: Image, x: int, y: int, w: int, h: int, color: Color) -> void:
	if w > 0 and h > 0:
		im.fill_rect(Rect2i(x, y, w, h), color)
