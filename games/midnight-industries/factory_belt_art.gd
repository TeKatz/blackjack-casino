extends RefCounted
## Purpose-built high-resolution north/south conveyor artwork. The horizontal
## atlas sprite contains asymmetric perspective lighting and must not be
## rotated wholesale by 90 degrees. These two sprites are built once, not
## painted with CanvasItem primitives during gameplay.
const SIZE := 144
static var _north: ImageTexture
static var _south: ImageTexture

static func texture(direction: int) -> Texture2D:
	if direction == 1:
		if _south == null:
			_south = _create(true)
		return _south
	if _north == null:
		_north = _create(false)
	return _north

static func _create(south: bool) -> ImageTexture:
	var im := Image.create(SIZE, SIZE, false, Image.FORMAT_RGBA8)
	im.fill(Color.TRANSPARENT)
	# A narrow longitudinal belt on a steel chassis. Top and bottom are
	# flush so neighboring conveyor sections form one uninterrupted line.
	_rect(im, 21, 0, 102, 144, Color("#0A1218"))
	_rect(im, 24, 0, 96, 144, Color("#394D57"))
	_rect(im, 27, 0, 90, 144, Color("#687882"))
	_rect(im, 35, 0, 74, 144, Color("#13222A"))
	_rect(im, 39, 0, 66, 144, Color("#2B3C44"))
	_rect(im, 43, 0, 58, 144, Color("#1A2D36"))
	# Rollers appear orthogonal to the direction of travel.
	for y in range(-8, 154, 22):
		var py: int = maxi(y, 0)
		var height: int = mini(5, SIZE - py)
		if height <= 0:
			continue
		_rect(im, 45, py, 54, height, Color("#425865"))
		_rect(im, 46, py, 51, mini(2, height), Color("#899BA4"))
		_rect(im, 48, py + mini(2, height - 1), 48, 1, Color("#111C22"))
	# Extruded rails with independent top-facing highlights; unlike the old
	# rotation, the lighting always comes from the top-left of the screen.
	_rect(im, 23, 0, 13, 144, Color("#202F3A"))
	_rect(im, 23, 0, 3, 144, Color("#8C9FA9"))
	_rect(im, 27, 0, 3, 144, Color("#4F6874"))
	_rect(im, 33, 0, 3, 144, Color("#111C24"))
	_rect(im, 108, 0, 13, 144, Color("#273742"))
	_rect(im, 110, 0, 4, 144, Color("#83929B"))
	_rect(im, 118, 0, 3, 144, Color("#0A161D"))
	# Steel brackets / cyan status strips every 36px.
	for y in range(9, SIZE, 36):
		_rect(im, 17, y, 22, 9, Color("#0F2028"))
		_rect(im, 20, y + 1, 16, 4, Color("#BA873F"))
		_rect(im, 22, y + 2, 11, 2, Color("#F0C06A"))
		_rect(im, 106, y, 22, 9, Color("#09161E"))
		_rect(im, 110, y + 1, 15, 4, Color("#946C38"))
		_rect(im, 112, y + 2, 9, 2, Color("#F2BC69"))
		_rect(im, 24, y + 13, 3, 10, Color("#15A1BC"))
		_rect(im, 116, y + 13, 3, 10, Color("#0AB2CE"))
	# Direction chevrons are laid out along the vertical track, not rotated
	# with the chassis. Keep them readable even with cargo covering one.
	for y in [22, 66, 110]:
		_chevron(im, y, south)
	im.generate_mipmaps()
	return ImageTexture.create_from_image(im)

static func _chevron(im: Image, at_y: int, south: bool) -> void:
	var sign := 1 if south else -1
	for step in range(16):
		var dx := step / 2
		var center_y := at_y + sign * (step - 8)
		for t in range(4):
			var yy := center_y + t
			_pixel(im, 64 - dx, yy, Color("#00C8ED"))
			_pixel(im, 81 + dx, yy, Color("#00C8ED"))
			_pixel(im, 65 - dx, yy, Color("#4DE5FC"))
			_pixel(im, 80 + dx, yy, Color("#4DE5FC"))

static func _rect(im: Image, x: int, y: int, w: int, h: int, color: Color) -> void:
	if w <= 0 or h <= 0:
		return
	im.fill_rect(Rect2i(x, y, w, h), color)

static func _pixel(im: Image, x: int, y: int, color: Color) -> void:
	if x >= 0 and x < SIZE and y >= 0 and y < SIZE:
		im.set_pixel(x, y, color)
