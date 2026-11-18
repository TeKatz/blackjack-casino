extends RefCounted
## Hand-drawn 144px vector machinery in the approved illustrated steel,
## copper piping and cyan-light palette. No generic atlas substitutions.
static var _cache: Dictionary = {}

static func texture(kind: String) -> Texture2D:
	if _cache.has(kind):
		return _cache[kind]
	var drawing := _wheel() if kind == "water_wheel" else _pole()
	var svg := '<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="0 0 144 144">'
	svg += '<defs><linearGradient id="steel" x1="0" y1="0" x2="1" y2="1">'
	svg += '<stop stop-color="#A8C7D1"/><stop offset=".32" stop-color="#4D6E79"/><stop offset=".65" stop-color="#203944"/><stop offset="1" stop-color="#0D232E"/></linearGradient>'
	svg += '<linearGradient id="rim" x1="0" y1="0" x2="1" y2="1">'
	svg += '<stop stop-color="#F4CB7E"/><stop offset=".45" stop-color="#B5793A"/><stop offset="1" stop-color="#653C26"/></linearGradient>'
	svg += '<radialGradient id="glow"><stop stop-color="#A6F9FF"/><stop offset=".43" stop-color="#2BBBD2"/><stop offset="1" stop-color="#06566F"/></radialGradient>'
	svg += '</defs>'
	svg += drawing + '</svg>'
	var img := Image.new()
	if img.load_svg_from_buffer(svg.to_utf8_buffer()) != OK:
		img = Image.create(144, 144, false, Image.FORMAT_RGBA8)
		img.fill(Color.TRANSPARENT)
	img.generate_mipmaps()
	var tex := ImageTexture.create_from_image(img)
	_cache[kind] = tex
	return tex

## The world waterwheel is a physical two-cell machine: dry gearbox on the
## land cell, a paddle wheel and its water wake in the second (river) cell.
## The geometry is authored once vertically, then SVG-transformed into all
## four shore orientations. Never simply rotate a 1x1 sprite in empty grass.
static var _world_wheels: Dictionary = {}

static func wheel_world(direction: int) -> Texture2D:
	var facing: int = posmod(direction, 4)
	if _world_wheels.has(facing):
		return _world_wheels[facing]
	var horizontal: bool = facing == 0 or facing == 2
	var width: int = 288 if horizontal else 144
	var height: int = 144 if horizontal else 288
	var transform := ""
	match facing:
		0: transform = 'matrix(0 -1 1 0 0 144)' # water on right
		1: transform = 'matrix(1 0 0 1 0 0)' # water below
		2: transform = 'matrix(0 1 -1 0 288 0)' # water on left
		3: transform = 'matrix(-1 0 0 -1 144 288)' # water above
	var svg := '<svg xmlns="http://www.w3.org/2000/svg" width="%d" height="%d" viewBox="0 0 %d %d">' % [
		width, height, width, height]
	svg += _defs()
	svg += '<g transform="' + transform + '">'
	# In-ground steel generator body. All geometry within the first 144
	# coordinate units is dry; the wheel itself begins past the shoreline.
	svg += '<ellipse cx="72" cy="127" rx="48" ry="12" fill="#06161C" opacity=".3"/>'
	svg += '<path d="M16 112L71 94L126 111L70 137Z" fill="#19313C" stroke="#071D28" stroke-width="5"/>'
	svg += '<path d="M17 109L71 93L124 110L70 128Z" fill="url(#steel)" stroke="#87A6AC" stroke-width="3"/>'
	svg += '<path d="M36 59L73 45L111 58L111 102L74 118L36 99Z" fill="#162D38" stroke="#091923" stroke-width="5"/>'
	svg += '<path d="M36 59L73 45L111 58L73 73Z" fill="url(#steel)" stroke="#B5CED1" stroke-width="2"/>'
	svg += '<path d="M37 61L74 75L74 116L37 99Z" fill="#2D4E5D" stroke="#0B1D26" stroke-width="3"/>'
	svg += '<path d="M78 74L111 61L111 100L78 114Z" fill="#152F3A" stroke="#547985" stroke-width="2"/>'
	svg += '<rect x="45" y="72" width="22" height="16" rx="3" fill="#0B242F" stroke="#9BAFB5" stroke-width="2"/>'
	svg += '<path d="M49 79h14" stroke="#53E3F2" stroke-width="4"/>'
	svg += '<circle cx="94" cy="87" r="9" fill="url(#rim)" stroke="#162B37" stroke-width="3"/>'
	svg += '<circle cx="94" cy="87" r="4" fill="url(#glow)"/>'
	svg += '<path d="M67 112L62 160" stroke="#081B24" stroke-width="14"/>'
	svg += '<path d="M67 112L62 160" stroke="url(#steel)" stroke-width="8"/>'
	svg += '<path d="M72 113L71 155" stroke="#BD8E54" stroke-width="3"/>'
	# Actual rotating wheel + ripples occupy the river half.
	svg += '<path d="M18 238Q36 223 52 239T91 241T134 229" fill="none" stroke="#B4FAFD" stroke-width="3" opacity=".66"/>'
	svg += '<path d="M11 255Q40 245 57 258T125 249" fill="none" stroke="#159FB9" stroke-width="4" opacity=".57"/>'
	svg += '<path d="M30 269L44 266M94 263L116 257" stroke="#71D6E4" stroke-width="2" opacity=".63"/>'
	svg += '<g transform="translate(0 138)">' + _wheel() + '</g>'
	svg += '</g></svg>'
	var img := Image.new()
	if img.load_svg_from_buffer(svg.to_utf8_buffer()) != OK:
		img = Image.create(width, height, false, Image.FORMAT_RGBA8)
		img.fill(Color.TRANSPARENT)
	img.generate_mipmaps()
	var output: Texture2D = ImageTexture.create_from_image(img)
	_world_wheels[facing] = output
	return output

static func _defs() -> String:
	var defs := '<defs><linearGradient id="steel" x1="0" y1="0" x2="1" y2="1">'
	defs += '<stop stop-color="#A8C7D1"/><stop offset=".32" stop-color="#4D6E79"/><stop offset=".65" stop-color="#203944"/><stop offset="1" stop-color="#0D232E"/></linearGradient>'
	defs += '<linearGradient id="rim" x1="0" y1="0" x2="1" y2="1">'
	defs += '<stop stop-color="#F4CB7E"/><stop offset=".45" stop-color="#B5793A"/><stop offset="1" stop-color="#653C26"/></linearGradient>'
	defs += '<radialGradient id="glow"><stop stop-color="#A6F9FF"/><stop offset=".43" stop-color="#2BBBD2"/><stop offset="1" stop-color="#06566F"/></radialGradient></defs>'
	return defs

static func _wheel() -> String:
	var svg := ''
	# Perspective footing, steel gearcase, upstream intake and copper conduit.
	svg += '<ellipse cx="71" cy="118" rx="54" ry="12" fill="#07141D" opacity=".35"/>'
	svg += '<path d="M18 99L58 82L125 100L88 126Z" fill="#172F3B" stroke="#071720" stroke-width="5"/>'
	svg += '<path d="M19 98L58 80L125 99L87 115Z" fill="url(#steel)" stroke="#88A7AE" stroke-width="2"/>'
	svg += '<path d="M22 93L57 76L81 85L46 104Z" fill="#285361" stroke="#0A242F" stroke-width="4"/>'
	svg += '<path d="M87 91L113 83L124 91L122 109L91 118Z" fill="#142E39" stroke="#071A23" stroke-width="4"/>'
	svg += '<path d="M96 92L110 87L116 91L115 108L98 113Z" fill="url(#steel)"/>'
	svg += '<path d="M99 104L108 101" stroke="#2FE0ED" stroke-width="3"/>'
	# Side-view riveted water wheel with distinctly separated wooden-metal paddles.
	svg += '<ellipse cx="66" cy="67" rx="35" ry="44" transform="rotate(-12 66 67)" fill="#0C2531" stroke="#081A24" stroke-width="7"/>'
	svg += '<ellipse cx="66" cy="67" rx="33" ry="41" transform="rotate(-12 66 67)" fill="#183943" stroke="url(#rim)" stroke-width="8"/>'
	for angle in range(0, 360, 45):
		var rot: String = str(angle)
		svg += '<g transform="rotate(' + rot + ' 66 67)">'
		svg += '<path d="M57 23L72 18L79 32L62 38Z" fill="#93663E" stroke="#362C2A" stroke-width="3"/>'
		svg += '<path d="M60 23L71 20L76 27L63 31Z" fill="#E9BC75" opacity=".8"/>'
		svg += '<path d="M66 39L66 66" stroke="#B87E43" stroke-width="5"/>'
		svg += '<path d="M64 39L65 58" stroke="#FFE0A2" stroke-width="1" opacity=".7"/>'
		svg += '</g>'
	svg += '<ellipse cx="66" cy="67" rx="15" ry="17" fill="url(#steel)" stroke="#0B1822" stroke-width="5"/>'
	svg += '<circle cx="66" cy="67" r="7" fill="url(#rim)" stroke="#493825" stroke-width="3"/>'
	svg += '<circle cx="66" cy="67" r="3" fill="#D0F6F9"/>'
	# Supported axle and water-side turquoise flow panel.
	svg += '<path d="M65 66L99 82" stroke="#122934" stroke-width="12"/>'
	svg += '<path d="M65 66L99 82" stroke="url(#steel)" stroke-width="7"/>'
	svg += '<path d="M31 112Q43 106 56 111T82 114" fill="none" stroke="#17BDD5" stroke-width="3" opacity=".85"/>'
	svg += '<path d="M22 120Q35 114 48 118M53 120Q65 114 76 120" fill="none" stroke="#B2F9FC" stroke-width="1.8" opacity=".7"/>'
	for pt in ["25,98","43,111","88,105","118,99"]:
		var xy: PackedStringArray = pt.split(",")
		svg += '<circle cx="' + xy[0] + '" cy="' + xy[1] + '" r="2.4" fill="#E1B575" stroke="#192A2F" stroke-width="1"/>'
	return svg

static func _pole() -> String:
	var svg := ''
	svg += '<ellipse cx="73" cy="122" rx="39" ry="10" fill="#07121B" opacity=".33"/>'
	svg += '<path d="M31 112L79 99L117 111L70 127Z" fill="#102B39" stroke="#071720" stroke-width="5"/>'
	svg += '<path d="M32 110L78 98L116 111L71 120Z" fill="url(#steel)" stroke="#5D8390" stroke-width="2"/>'
	svg += '<path d="M63 112L68 26L79 24L83 109L75 114Z" fill="url(#steel)" stroke="#0A1A25" stroke-width="4"/>'
	svg += '<path d="M69 28L74 28L76 104L70 107Z" fill="#B4CDD3" opacity=".60"/>'
	svg += '<path d="M64 40L79 37L81 47L62 49Z" fill="#142F3B" stroke="#0B1B25" stroke-width="3"/>'
	svg += '<path d="M68 47L42 58M79 44L105 58" stroke="#163746" stroke-width="8"/>'
	svg += '<path d="M68 47L42 58M79 44L105 58" stroke="#95B2B7" stroke-width="4"/>'
	svg += '<path d="M36 54L50 51L53 65L39 68Z" fill="url(#steel)" stroke="#0A1822" stroke-width="3"/>'
	svg += '<path d="M97 53L109 57L104 69L93 64Z" fill="url(#steel)" stroke="#0A1822" stroke-width="3"/>'
	svg += '<path d="M38 61L47 58M97 60L104 62" stroke="#53D6EB" stroke-width="3"/>'
	svg += '<path d="M69 22L75 20L80 28L74 35L66 30Z" fill="url(#rim)" stroke="#251E20" stroke-width="2"/>'
	svg += '<circle cx="74" cy="27" r="3.5" fill="url(#glow)"/>'
	svg += '<path d="M70 62L54 95L73 106L90 95L78 62Z" fill="none" stroke="#294959" stroke-width="4" opacity=".78"/>'
	svg += '<path d="M60 83L88 80M54 94L90 95" stroke="#A6C9D1" stroke-width="2" opacity=".76"/>'
	svg += '<path d="M67 106L82 105L86 114L66 117Z" fill="#233D4A" stroke="#0D2630" stroke-width="3"/>'
	for pt in ["42,111","72,119","104,111"]:
		var xy: PackedStringArray = pt.split(",")
		svg += '<circle cx="' + xy[0] + '" cy="' + xy[1] + '" r="2.3" fill="#DDA662" stroke="#233846" stroke-width="1"/>'
	return svg
