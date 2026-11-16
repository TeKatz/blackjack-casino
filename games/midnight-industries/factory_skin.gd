extends RefCounted
## Compact aerospace-industrial skin: differentiated surfaces, engraved depth,
## amber functional accents and subtle instrument lights. No repeated cyan rail.
const GOLD := Color("#DDAE56")
const CYAN := Color("#62BED3")
const ALERT := Color("#D27767")
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
	style.texture_margin_left = 13.0
	style.texture_margin_top = 14.0
	style.texture_margin_right = 13.0
	style.texture_margin_bottom = 12.0
	style.content_margin_left = 11.0
	style.content_margin_top = 10.0
	style.content_margin_right = 11.0
	style.content_margin_bottom = 10.0
	style.draw_center = true
	return style

static func button(tone: String = "button") -> StyleBoxTexture:
	var style := StyleBoxTexture.new()
	style.texture = _texture(tone)
	style.texture_margin_left = 11.0
	style.texture_margin_top = 10.0
	style.texture_margin_right = 11.0
	style.texture_margin_bottom = 9.0
	style.content_margin_left = 9.0
	style.content_margin_top = 7.0
	style.content_margin_right = 9.0
	style.content_margin_bottom = 7.0
	style.draw_center = true
	return style

static func _texture(tone: String) -> Texture2D:
	if _textures.has(tone):
		return _textures[tone]
	var is_button := tone.begins_with("button")
	var selected := tone == "button_active" or tone == "button_hover" or tone == "button_danger_hover"
	var danger := tone.begins_with("button_danger")
	var width := 192
	var height := 50 if is_button else 104
	var img := Image.create(width, height, false, Image.FORMAT_RGBA8)
	var accent: Color = ALERT if danger else (GOLD if selected else CYAN)
	var top: Color = Color("#35414A")
	var bottom: Color = Color("#15232D")
	if tone == "header":
		top = Color("#303E48")
		bottom = Color("#142029")
	elif tone == "inspector":
		top = Color("#2F3C46")
		bottom = Color("#17232C")
	elif tone == "menu":
		top = Color("#393F47")
		bottom = Color("#1D2229")
	elif is_button:
		top = Color("#36434C") if not selected else (Color("#6E552F") if not danger else Color("#68413D"))
		bottom = Color("#1A2934") if not selected else (Color("#3D3122") if not danger else Color("#382729"))
	for y in range(height):
		var fy: float = float(y) / float(height - 1)
		for x in range(width):
			var corner_cut := 5 if is_button else 8
			if x + y < corner_cut or (width - 1 - x) + y < corner_cut or x + (height - 1 - y) < corner_cut / 2:
				img.set_pixel(x, y, Color.TRANSPARENT)
				continue
			var fx: float = float(x) / float(width - 1)
			var grain: float = (fposmod(sin(float(y) * 17.1 + float(x) * 3.1) * 9234.0, 1.0) - 0.5) * 0.014
			var c: Color = bottom.lerp(top, clampf(pow(1.0 - fy, 1.65) * 0.76 + 0.07 + grain, 0, 1))
			# Embossed border, recessed center and a different construction per panel type.
			if x < 2 or x >= width - 2 or y < 2 or y >= height - 2:
				c = Color("#627681") if y <= 1 else Color("#0C161E")
			elif x == 3 or x == width - 4:
				c = Color("#304551")
			elif y == 3:
				c = Color("#536776")
			elif y == height - 4:
				c = Color("#0F1A21")
			if not is_button:
				if tone == "panel":
					if x >= 5 and x <= 7 and y > 16 and y < height - 16:
						c = Color("#886B3D") if x == 5 else Color("#3B342B")
				elif tone == "inspector":
					if x >= width - 8 and x <= width - 6 and y > 16 and y < height - 16:
						c = Color("#4E7D8D")
					if y >= 12 and y <= 16 and x > 18 and x < 43:
						c = Color("#5C7F86")
				elif tone == "header":
					if y == height - 9 and x > 18 and x < width - 18:
						c = Color("#395260")
				elif tone == "menu":
					if y == 9 and x > 22 and x < width - 22:
						c = Color("#9E7F4B")
					if x <= 11 and y < 27 and x + y > 18:
						c = Color("#AB894D")
			else:
				if selected and y > 7 and y < height - 7:
					c = c.lerp(accent, 0.20 + 0.19 * maxf(0, 1.0 - absf(fx - 0.4) * 1.5))
				if y == 7 and x > 14 and x < width - 14:
					c = Color("#B3A17D") if selected else Color("#51646F")
				if x == 9 and y >= 14 and y < height - 14:
					c = accent if selected else Color("#52636C")
				if y == height - 8 and x > 14 and x < width - 14:
					c = Color("#111D25")
			img.set_pixel(x, y, c)
	# Tiny inset metal fasteners, not expensive per-frame draw commands.
	if not is_button:
		for px in [13, width - 14]:
			for py in [10, height - 11]:
				img.fill_rect(Rect2i(px - 2, py - 2, 5, 5), Color("#0C151C"))
				img.fill_rect(Rect2i(px - 1, py - 1, 3, 3), Color("#8A959A"))
				img.set_pixel(px, py, Color("#15252E"))
	img.generate_mipmaps()
	_textures[tone] = ImageTexture.create_from_image(img)
	return _textures[tone]

static func progress_fill() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#4BA8BC")
	s.set_corner_radius_all(1)
	return s

static func progress_back() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#0B161F")
	s.border_color = Color("#425761")
	s.set_border_width_all(1)
	s.set_corner_radius_all(1)
	return s
