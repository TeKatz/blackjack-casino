extends RefCounted
## Compact scalable UI symbols; independent from machine sprites and fonts.
## Generated at runtime at fixed small sizes so they render cleanly at 1080p.
static var _icons: Dictionary = {}
const PATHS := {
	"achievements": '<path d="M7 4h10v5a5 5 0 0 1-10 0z M7 6H4v2a3 3 0 0 0 3 3 M17 6h3v2a3 3 0 0 1-3 3 M12 14v5 M8 20h8"/>',
	"jobs": '<rect x="5" y="5" width="14" height="17" rx="2"/><path d="M9 5V3h6v2 M8 11h2 M12 11h4 M8 16h2 M12 16h4"/>',
	"upgrades": '<path d="M12 20V5 M6 11l6-6 6 6 M5 20h14"/>',
	"storage": '<path d="M3 10L12 4l9 6v10H3z M7 20v-8h10v8 M3 10h18"/>',
	"pause": '<rect x="6" y="4" width="4" height="16" rx="1"/><rect x="14" y="4" width="4" height="16" rx="1"/>',
	"play": '<path d="M7 4l13 8-13 8z"/>',
	"menu": '<path d="M4 6h16 M4 12h16 M4 18h16"/>',
	"mining": '<path d="M5 5l14 14 M5 15l4 4 M15 5l4 4 M3 6q9-6 18 2"/>',
	"logistics": '<rect x="3" y="8" width="18" height="10" rx="2"/><circle cx="7" cy="13" r="2"/><circle cx="17" cy="13" r="2"/>',
	"production": '<path d="M3 21V10l6 4v-4l6 4V7l6 4v10z M16 3v5"/>',
	"energy": '<path d="M13 2L4 13h7l-1 9 10-13h-7z"/>',
	"special": '<path d="M12 2l2.7 6.8L22 12l-7.3 3.2L12 22l-2.7-6.8L2 12l7.3-3.2z"/>',
	"inspect": '<circle cx="11" cy="11" r="7"/><path d="m16 16 5 5"/>',
	"lock": '<rect x="5" y="10" width="14" height="11" rx="2"/><path d="M8 10V7a4 4 0 0 1 8 0v3"/>',
	"arrow": '<path d="m9 5 7 7-7 7"/>'
}
static func icon(name: String, color: Color = Color("#A7DFEB")) -> Texture2D:
	var id: String = name + "_" + color.to_html(false)
	if _icons.has(id):
		return _icons[id]
	var path: String = str(PATHS.get(name, PATHS["special"]))
	var markup := '<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24">'
	markup += '<g fill="none" stroke="#' + color.to_html(false)
	markup += '" stroke-width="1.65" stroke-linejoin="round" stroke-linecap="round">'
	markup += path + '</g></svg>'
	var img := Image.new()
	if img.load_svg_from_buffer(markup.to_utf8_buffer()) != OK:
		img = Image.create(24, 24, false, Image.FORMAT_RGBA8)
		img.fill(Color.TRANSPARENT)
	var texture := ImageTexture.create_from_image(img)
	_icons[id] = texture
	return texture
