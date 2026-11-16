extends RefCounted
## Midnight Industries premium-industrial native UI system.
## Cached nine-slice raster skins replace the old rounded flat controls.
const GRAPHITE := Color("#111A21")
const STEEL := Color("#273743")
const GOLD := Color("#F5C04A")
const CYAN := Color("#00D8F4")
const ALERT := Color("#FF6B50")
static var _textures: Dictionary = {}
static var _font: SystemFont

static func font() -> Font:
	if _font == null:
		_font = SystemFont.new()
		_font.font_names = PackedStringArray(["Bahnschrift", "Segoe UI", "Inter", "Arial"])
		_font.antialiasing = TextServer.FONT_ANTIALIASING_GRAY
	return _font

static func panel(tone: String = "panel") -> StyleBoxTexture:
	var style := StyleBoxTexture.new()
	style.texture = _texture(tone)
	style.texture_margin_left = 19.0
	style.texture_margin_top = 22.0
	style.texture_margin_right = 19.0
	style.texture_margin_bottom = 17.0
	style.content_margin_left = 17.0
	style.content_margin_top = 15.0
	style.content_margin_right = 17.0
	style.content_margin_bottom = 14.0
	style.draw_center = true
	return style

static func button(tone: String = "button") -> StyleBoxTexture:
	var style := StyleBoxTexture.new()
	style.texture = _texture(tone)
	style.texture_margin_left = 15.0
	style.texture_margin_top = 13.0
	style.texture_margin_right = 15.0
	style.texture_margin_bottom = 13.0
	style.content_margin_left = 12.0
	style.content_margin_top = 10.0
	style.content_margin_right = 12.0
	style.content_margin_bottom = 9.0
	style.draw_center = true
	return style

static func _texture(tone: String) -> Texture2D:
	if _textures.has(tone):
		return _textures[tone]
	var is_button := tone.begins_with("button")
	var width := 192
	var height := 62 if is_button else 128
	var image := Image.create(width, height, false, Image.FORMAT_RGBA8)
	var selection := tone == "button_active" or tone == "button_hover" or tone == "button_danger_hover"
	var dangerous := tone.begins_with("button_danger")
	var accent := ALERT if dangerous else (GOLD if selection or tone == "menu" else CYAN)
	var lower := GRAPHITE
	var upper := STEEL
	if tone == "header":
		upper = Color("#26323D")
		lower = Color("#0C1820")
	elif tone == "menu":
		upper = Color("#30353D")
		lower = Color("#0C141C")
	elif tone == "inspector":
		upper = Color("#233442")
		lower = Color("#0A1824")
	elif is_button:
		upper = Color("#2C404A") if not selection else (Color("#675025") if not dangerous else Color("#71382F"))
		lower = Color("#101E27") if not selection else (Color("#3B301C") if not dangerous else Color("#40202B"))
	var bevel := 13 if not is_button else 10
	var w := float(width)
	var h := float(height)
	for y in range(height):
		var fy := float(y) / float(height - 1)
		var sweep := pow(1.0 - fy, 2.0)
		for x in range(width):
			var fx := float(x) / float(width - 1)
			if x + y < bevel or (width - x - 1) + y < bevel:
				image.set_pixel(x, y, Color.TRANSPARENT)
				continue
			var noise := fract(sin(float(x) * 14.4 + float(y) * 49.71) * 4273.391)
			var brushed := sin(float(y) * 0.70) * 0.007 + (noise - 0.5) * 0.018
			var c := lower.lerp(upper, sweep * 0.52 + 0.22)
			c = c.lightened(maxf(0.0, brushed)) if brushed > 0 else c.darkened(-brushed)
			if y < 2 or y >= height - 2 or x < 2 or x >= width - 2:
				c = Color("#617987") if y < 2 else Color("#1B3742")
			if y == 2 and x >= bevel and x < width - bevel:
				c = Color("#9BB2B9")
			if y == height - 3 and x > 14 and x < width - 14:
				c = Color("#07121A")
			if x == 3 or x == width - 4:
				c = Color("#385769") if y > bevel else c
			# Section label rail and angled amber trim.
			if y >= 4 and y <= 6 and x > bevel + 5 and x < (67 if is_button else 93):
				c = accent.lerp(Color.WHITE, 0.12)
			if not is_button and x >= width - 35 and x < width - 18 and y < 13:
				c = Color("#506575")
			if is_button and selection and y > 10 and y < height - 9:
				var emphasis := maxf(0.0, 1.0 - absf(fx - 0.34) * 2.0) * 0.14
				c = c.lerp(accent, emphasis)
			if x + y == bevel or (width - x - 1) + y == bevel:
				c = Color("#718A95")
			image.set_pixel(x, y, c)
	_textures[tone] = ImageTexture.create_from_image(image)
	return _textures[tone]

static func progress_fill() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = CYAN
	s.set_corner_radius_all(2)
	return s

static func progress_back() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#0B141B")
	s.border_color = Color("#53717D")
	s.set_border_width_all(1)
	s.set_corner_radius_all(2)
	return s
