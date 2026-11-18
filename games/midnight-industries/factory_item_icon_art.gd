extends RefCounted
## High-resolution, material-specific inventory illustrations generated once.
## No generic colored squares. Catalog entries can override these with
## authored icon_path assets at any time (without touching the inventory UI).
static var _cache: Dictionary = {}

static func icon(id: String, entry: Dictionary = {}) -> Texture2D:
	if _cache.has(id):
		return _cache[id]
	var category: String = str(entry.get("category", "component"))
	var base := Color("#748FA2")
	if id.begins_with("copper"):
		base = Color("#D88347")
	elif id.begins_with("gold"):
		base = Color("#E8B94E")
	elif id.begins_with("iron"):
		base = Color("#A8C2CD")
	var highlight: String = base.lightened(0.52).to_html(false)
	var light: String = base.lightened(0.26).to_html(false)
	var face: String = base.to_html(false)
	var deep: String = base.darkened(0.35).to_html(false)
	var edge: String = base.darkened(0.62).to_html(false)
	var svg := '<svg xmlns="http://www.w3.org/2000/svg" width="160" height="160" viewBox="0 0 160 160">'
	svg += '<defs><linearGradient id="metal" x1="0" y1="0" x2="1" y2="1">'
	svg += '<stop offset="0" stop-color="#' + highlight + '"/>'
	svg += '<stop offset="0.42" stop-color="#' + light + '"/>'
	svg += '<stop offset="0.8" stop-color="#' + face + '"/>'
	svg += '<stop offset="1" stop-color="#' + deep + '"/></linearGradient></defs>'
	if category == "component" and id.ends_with("_plate"):
		# Thin stamped metal sheet with chamfered corners, a dark lower
		# extrusion, mechanical punch holes and directional brushed highlights.
		svg += '<path d="M22 103L39 63L122 44L145 70L125 114L42 132Z" fill="#' + edge + '" opacity=".42"/>'
		svg += '<path d="M22 99L39 61L118 43L139 67L123 103L40 122Z" fill="url(#metal)" stroke="#' + deep + '" stroke-width="3"/>'
		svg += '<path d="M40 122L123 103L136 93L134 105L123 116L42 135L21 113L22 99Z" fill="#' + deep + '" stroke="#' + edge + '" stroke-width="2"/>'
		svg += '<path d="M41 62L116 45L127 60L50 79Z" fill="#' + highlight + '" opacity=".68"/>'
		svg += '<path d="M40 113L119 96M44 106L124 89" fill="none" stroke="#' + highlight + '" stroke-width="2.5" opacity=".56"/>'
		svg += '<path d="M55 87L116 72M52 95L113 80" stroke="#' + deep + '" stroke-width="1.2" opacity=".30"/>'
		svg += '<ellipse cx="48" cy="91" rx="5" ry="3" fill="#' + edge + '"/>'
		svg += '<ellipse cx="115" cy="75" rx="5" ry="3" fill="#' + edge + '"/>'
		svg += '<ellipse cx="55" cy="90" rx="1.8" ry="1.2" fill="#' + highlight + '"/>'
	elif category == "ingot" or id.ends_with("_ingot"):
		# Forged, three-dimensional metallic bar, top/side/front faces,
		# cut-metal glints and bevels. Each base metal has its own tint.
		svg += '<path d="M17 102 L39 59 L121 47 L145 80 L126 124 L37 135 Z" fill="#' + edge + '" opacity=".35"/>'
		svg += '<path d="M25 88 L41 58 L114 45 L136 69 L119 100 L42 117 Z" fill="url(#metal)" stroke="#' + deep + '" stroke-width="3"/>'
		svg += '<path d="M25 88 L42 117 L43 134 L20 106 Z" fill="#' + deep + '" stroke="#' + edge + '" stroke-width="2"/>'
		svg += '<path d="M42 117 L119 100 L136 69 L135 104 L120 119 L43 134 Z" fill="#' + face + '" stroke="#' + edge + '" stroke-width="2"/>'
		svg += '<path d="M41 58 L114 45 L125 59 L51 73 Z" fill="#' + highlight + '" opacity=".62"/>'
		svg += '<path d="M44 111 L116 95" stroke="#' + highlight + '" stroke-width="2" opacity=".62"/>'
		svg += '<path d="M48 123 L110 110 M54 127 L120 113" stroke="#' + edge + '" stroke-width="1.3" opacity=".30"/>'
		svg += '<path d="M51 76 L120 63 M61 81 L109 72" stroke="#fff" stroke-width="1.1" opacity=".30"/>'
	elif category == "ore" or id.ends_with("_ore"):
		# Uneven cluster of material-rich rock facets, not an ingot or swatch.
		svg += '<path d="M13 116 L29 74 L45 62 L63 37 L94 42 L112 57 L137 78 L145 119 L114 140 L44 141 Z" fill="#' + edge + '" opacity=".65"/>'
		svg += '<path d="M20 104 L35 68 L60 54 L77 27 L106 46 L116 71 L139 83 L134 116 L112 133 L50 133 Z" fill="#' + deep + '" stroke="#' + edge + '" stroke-width="3"/>'
		svg += '<path d="M20 104 L35 68 L60 54 L69 90 L48 118 Z" fill="#' + light + '"/>'
		svg += '<path d="M60 54 L77 27 L106 46 L95 87 L69 90 Z" fill="url(#metal)"/>'
		svg += '<path d="M95 87 L106 46 L116 71 L139 83 L118 111 Z" fill="#' + face + '"/>'
		svg += '<path d="M69 90 L95 87 L118 111 L112 133 L50 133 L48 118 Z" fill="#' + deep + '"/>'
		svg += '<path d="M31 89 L49 76 L57 102 L42 118 Z M88 53 L101 59 L89 75 L79 74 Z M105 99 L122 105 L111 121 L99 119 Z" fill="#' + highlight + '" opacity=".62"/>'
		svg += '<path d="M46 119 L69 91 L92 89 L112 111" stroke="#' + edge + '" fill="none" stroke-width="2" opacity=".65"/>'
	else:
		# Future parts without bespoke art still get a useful physical
		# component silhouette. Future assets may override via icon_path.
		svg += '<path d="M33 48 L100 31 L130 55 L130 116 L58 139 L28 112 Z" fill="#' + deep + '" stroke="#' + edge + '" stroke-width="3"/>'
		svg += '<path d="M33 48 L100 31 L130 55 L58 75 Z" fill="url(#metal)"/>'
		svg += '<path d="M58 75 L130 55 L130 116 L58 139 Z" fill="#' + face + '"/>'
		svg += '<path d="M58 75 L28 56 L28 112 L58 139 Z" fill="#' + deep + '"/>'
		svg += '<path d="M72 90 L112 80 M72 103 L112 93 M72 116 L102 108" stroke="#' + highlight + '" stroke-width="5" opacity=".5"/>'
	svg += '</svg>'
	var image := Image.new()
	var result := image.load_svg_from_buffer(svg.to_utf8_buffer())
	if result != OK:
		# Never show a blank icon if SVG support is unavailable on a platform.
		image = Image.create(96, 96, false, Image.FORMAT_RGBA8)
		image.fill(Color.TRANSPARENT)
		image.fill_rect(Rect2i(20, 20, 56, 52), base.darkened(0.38))
		image.fill_rect(Rect2i(24, 20, 48, 36), base.lightened(0.26))
	image.generate_mipmaps()
	var texture := ImageTexture.create_from_image(image)
	_cache[id] = texture
	return texture
