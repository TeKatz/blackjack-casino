extends RefCounted
## Four authored facing directions for a metal-plate Constructor.
## Body is fully illustrated at 144px, matching the existing industrial atlas
## (brushed steel, copper mechanisms, cyan status light, riveted housing).
static var _cache: Dictionary = {}

static func texture(direction: int = 0) -> Texture2D:
	var facing: int = posmod(direction, 4)
	if _cache.has(facing):
		return _cache[facing]
	var svg := '<svg xmlns="http://www.w3.org/2000/svg" width="144" height="144" viewBox="0 0 144 144">'
	svg += '<defs><linearGradient id="steel" x1="0" y1="0" x2="1" y2="1">'
	svg += '<stop stop-color="#B5C7CB"/><stop offset=".3" stop-color="#5C7B87"/><stop offset=".67" stop-color="#29434F"/><stop offset="1" stop-color="#122732"/></linearGradient>'
	svg += '<linearGradient id="iron" x1="0" y1="0" x2=".6" y2="1">'
	svg += '<stop stop-color="#D3E7ED"/><stop offset=".6" stop-color="#65848D"/><stop offset="1" stop-color="#31505D"/></linearGradient>'
	svg += '<linearGradient id="copper" x1="0" y1="0" x2="1" y2="1">'
	svg += '<stop stop-color="#F6CC7A"/><stop offset=".5" stop-color="#C67A3C"/><stop offset="1" stop-color="#6D402D"/></linearGradient>'
	svg += '<radialGradient id="core"><stop stop-color="#D6FDFF"/><stop offset=".4" stop-color="#3BDBEE"/><stop offset="1" stop-color="#06667E"/></radialGradient></defs>'
	svg += '<g transform="rotate(%d 72 72)">' % [facing * 90]
	# Solid diamond deck, extruded sides and industrial foundation.
	svg += '<ellipse cx="72" cy="123" rx="59" ry="12" fill="#07161C" opacity=".34"/>'
	svg += '<path d="M10 102L65 75L134 102L80 132Z" fill="#102633" stroke="#071B23" stroke-width="6"/>'
	svg += '<path d="M14 97L65 71L130 96L80 121Z" fill="url(#steel)" stroke="#6F919D" stroke-width="3"/>'
	svg += '<path d="M21 99L67 78L119 99L78 117Z" fill="#193E4A" stroke="#A5C1C5" stroke-width="2"/>'
	# Distinct right-facing output assembly with plate feeding fingers.
	svg += '<path d="M105 68L139 60L140 102L111 111Z" fill="#10232D" stroke="#071821" stroke-width="5"/>'
	svg += '<path d="M107 68L137 62L138 81L107 88Z" fill="url(#steel)" stroke="#7295A0" stroke-width="2"/>'
	svg += '<path d="M110 89L138 83L138 100L112 107Z" fill="#244450" stroke="#081923" stroke-width="3"/>'
	svg += '<path d="M116 77L137 73M116 95L137 91" stroke="#36D3E8" stroke-width="3.5"/>'
	# Massive top housing with press ram and armored copper flywheels.
	svg += '<path d="M22 44L64 27L106 42L106 96L64 114L21 94Z" fill="#152C38" stroke="#071822" stroke-width="6"/>'
	svg += '<path d="M22 45L64 27L106 42L65 61Z" fill="url(#steel)" stroke="#BBC9C9" stroke-width="2.2"/>'
	svg += '<path d="M22 45L65 61L65 111L22 94Z" fill="#3E6070" stroke="#102632" stroke-width="3"/>'
	svg += '<path d="M65 61L106 43L105 95L65 113Z" fill="#233D4A" stroke="#0A202B" stroke-width="3"/>'
	svg += '<path d="M40 56L72 43L91 53L60 67Z" fill="#132C3A" stroke="#B17A45" stroke-width="4"/>'
	svg += '<path d="M46 55L72 45L84 52L59 63Z" fill="url(#iron)" stroke="#4A6A73" stroke-width="2"/>'
	svg += '<path d="M41 71L60 78L60 100L42 93Z" fill="#0F2E3B" stroke="#C18A52" stroke-width="3"/>'
	svg += '<path d="M46 77L55 80L55 90L46 87Z" fill="url(#core)"/>'
	# Vertical piston with luminous position sensor.
	svg += '<path d="M63 63L79 57L80 89L64 96Z" fill="#A5C1C8" stroke="#0B2630" stroke-width="4"/>'
	svg += '<path d="M66 73L76 69L76 84L66 88Z" fill="#152A35" stroke="#4DBED1" stroke-width="2"/>'
	svg += '<path d="M70 79L75 77" stroke="#B5F5FF" stroke-width="3"/>'
	svg += '<path d="M84 65L103 57L102 73L85 80Z" fill="url(#copper)" stroke="#533727" stroke-width="3"/>'
	svg += '<circle cx="94" cy="67" r="5" fill="#223B44" stroke="#EFB66E" stroke-width="2"/>'
	svg += '<circle cx="94" cy="67" r="2" fill="url(#core)"/>'
	# Rivets, exposed pipes and a very clear right-hand conveyor throat.
	svg += '<path d="M25 82L41 88M85 100L100 94" stroke="#DCA566" stroke-width="4"/>'
	svg += '<path d="M28 39L42 33M78 30L92 35" stroke="#D6A068" stroke-width="5"/>'
	for point in ["18,100","63,77","126,97","79,118","39,47","102,44"]:
		var xy: PackedStringArray = point.split(",")
		svg += '<circle cx="%s" cy="%s" r="2.3" fill="#E3B975" stroke="#20303A" stroke-width="1"/>' % [xy[0], xy[1]]
	svg += '</g></svg>'
	var image := Image.new()
	if image.load_svg_from_buffer(svg.to_utf8_buffer()) != OK:
		image = Image.create(144, 144, false, Image.FORMAT_RGBA8)
		image.fill(Color.TRANSPARENT)
	image.generate_mipmaps()
	var result: Texture2D = ImageTexture.create_from_image(image)
	_cache[facing] = result
	return result
