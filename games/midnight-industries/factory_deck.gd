extends RefCounted
## One-time rasterized floor atlas: one textured quad per tile instead of
## 10-20 immediate-mode drawing commands on every visible cell, every frame.
const TILE := 48
const COUNT := 8
static var cached: ImageTexture

static func texture() -> Texture2D:
	if cached != null:
		return cached
	var image := Image.create(TILE * COUNT, TILE, false, Image.FORMAT_RGBA8)
	for variant in range(COUNT):
		_paint_tile(image, variant)
	image.generate_mipmaps()
	cached = ImageTexture.create_from_image(image)
	return cached

static func region(index: int) -> Rect2:
	return Rect2(float(posmod(index, COUNT) * TILE), 0.0, TILE, TILE)

static func variant(cell: Vector2i) -> int:
	var n := posmod(cell.x * 23 + cell.y * 81, 97)
	if n == 11 or n == 72:
		return 6
	if n == 30 or n == 49:
		return 7
	if n < 15:
		return n % 4
	return 4 if n % 13 == 0 else 5

static func _paint_tile(im: Image, index: int) -> void:
	var x := index * TILE
	var plate := Color("#28323A") if index % 3 == 0 else Color("#2B353E")
	if index == 5:
		plate = Color("#242E38")
	im.fill_rect(Rect2i(x, 0, TILE, TILE), Color("#101820"))
	im.fill_rect(Rect2i(x + 2, 2, 44, 43), plate)
	im.fill_rect(Rect2i(x + 3, 3, 41, 1), Color("#566673"))
	im.fill_rect(Rect2i(x + 3, 4, 1, 39), Color("#4F606E"))
	im.fill_rect(Rect2i(x + 44, 4, 1, 40), Color("#131D26"))
	im.fill_rect(Rect2i(x + 4, 44, 39, 1), Color("#111B24"))
	for i in range(4):
		var bx := 6 if i % 2 == 0 else 41
		var by := 6 if i < 2 else 40
		im.fill_rect(Rect2i(x + bx - 1, by - 1, 3, 3), Color("#15232C"))
		im.set_pixel(x + bx, by, Color("#7F919B"))
	if index < 4:
		im.fill_rect(Rect2i(x + 11 + index * 2, 16, 15, 1), Color("#34414A"))
		im.fill_rect(Rect2i(x + 17, 27 + index % 3, 18, 1), Color("#343E48"))
		im.fill_rect(Rect2i(x + 12, 30, 6, 1), Color("#3E4B54"))
	elif index == 4:
		im.fill_rect(Rect2i(x + 9, 12, 30, 22), Color("#242D34"))
		im.fill_rect(Rect2i(x + 10, 13, 28, 1), Color("#57616C"))
		im.fill_rect(Rect2i(x + 10, 32, 28, 1), Color("#151E27"))
	elif index == 5:
		im.fill_rect(Rect2i(x + 12, 12, 24, 24), Color("#1A252D"))
		im.fill_rect(Rect2i(x + 14, 14, 20, 20), Color("#2E3941"))
		for slit in range(5):
			im.fill_rect(Rect2i(x + 17, 16 + slit * 3, 14, 1), Color("#151E26"))
	elif index == 6:
		im.fill_rect(Rect2i(x + 10, 9, 28, 30), Color("#101A23"))
		im.fill_rect(Rect2i(x + 12, 11, 24, 26), Color("#43525C"))
		im.fill_rect(Rect2i(x + 14, 13, 20, 22), Color("#1C2B35"))
		for slit in range(5):
			im.fill_rect(Rect2i(x + 16, 15 + slit * 4, 16, 2), Color("#52636B"))
	elif index == 7:
		im.fill_rect(Rect2i(x + 6, 34, 36, 7), Color("#302818"))
		for stripe in range(5):
			for j in range(7):
				var px := x + 7 + stripe * 8 + j
				var py := 40 - j
				for thickness in range(3):
					if px < x + 42 and py + thickness >= 34 and py + thickness < 41:
						im.set_pixel(px, py + thickness, Color("#C79037"))
