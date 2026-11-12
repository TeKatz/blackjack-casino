extends RefCounted
## Midnight Descent's second-generation UI skin. World sprites remain pixel art;
## typography and interface chrome deliberately use smooth canvas rendering.
const DISPLAY_FONT: FontFile = preload("res://fonts/Cinzel-Variable.ttf")
static var _font_rasterizer_ready := false

# The canvas is scaled 1.5x at 1920×1080. Godot's default 1x
# font rasterization plus shadows produces doubled, jagged serifs and
# shimmering letter edges. Configure all three bundled fonts once,
# independently of the default editor/UI font import settings.
static func configure_fonts() -> void:
	if _font_rasterizer_ready: return
	_font_rasterizer_ready=true
	for font: FontFile in [DISPLAY_FONT,BODY_FONT,BOLD_FONT]:
		font.antialiasing=TextServer.FONT_ANTIALIASING_GRAY
		font.hinting=TextServer.HINTING_LIGHT
		font.subpixel_positioning=TextServer.SUBPIXEL_POSITIONING_AUTO
		font.multichannel_signed_distance_field=false
		font.generate_mipmaps=false
		font.oversampling=2.0
const BODY_FONT: FontFile = preload("res://fonts/AlegreyaSans-Regular.ttf")
const BOLD_FONT: FontFile = preload("res://fonts/AlegreyaSans-Bold.ttf")
const INK := Color("#0d171a")
const SURFACE := Color("#17262a")
const SURFACE_RAISED := Color("#23383b")
const HAIRLINE := Color("#50676b")
const PARCHMENT := Color("#f3eddc")
const MUTED := Color("#b1c0bb")
const BRASS := Color("#dbba81")
const AETHER := Color("#80c8bd")
const DANGER := Color("#f17879")
const ICONS = preload("res://art/pixel/ui_icons.svg")
const RELICS = preload("res://art/pixel/relics.png")
const CROWN_RELICS = preload("res://art/pixel/crown_relics.svg")
const CLASS_RELICS = preload("res://art/pixel/class_relics.svg")
const SPECIAL_ROOM_ICONS = preload("res://art/pixel/special_room_icons.svg")
const SERVICE_MAP_ICONS = preload("res://art/pixel/service_map_icons.svg")
const SERVICE_MAP_ORDER = ["shrine","event","shop"]
const SPECIAL_ROOM_ORDER = ["ritual","survival","bounty","unstable","blood_altar","forgotten_forge","archive"]
const RARITY_COLORS = [Color("#c6c9bf"),Color("#81c7aa"),Color("#c3a6e9"),Color("#f0c47e")]
const ROOM_ICONS = {"fight":0,"elite":1,"treasure":2,"shop":3,"event":4,"shrine":5,"secret":6,"boss":7,"forge":12,"?":4,"ritual":0,"survival":0,"bounty":0,"unstable":0,"blood_altar":0,"forgotten_forge":0,"archive":0}
const ROOM_COLORS = {"fight":Color("#a9bdc6"),"elite":Color("#ed978a"),"treasure":Color("#efd08f"),"shop":Color("#78c6b7"),"event":Color("#bcabd6"),"shrine":Color("#9bd7a7"),"secret":Color("#cab4cb"),"boss":Color("#ef8791"),"forge":Color("#e4b17d"),"?":Color("#a6b1ae"),"ritual":Color("#bfa4e0"),"survival":Color("#93c9de"),"bounty":Color("#e4ac80"),"unstable":Color("#8ad9c9"),"blood_altar":Color("#ed8992"),"forgotten_forge":Color("#eabb84"),"archive":Color("#bdb3d7")}

static func install_theme(root: Control) -> void:
	configure_fonts()
	var skin := Theme.new()
	skin.default_font = BODY_FONT
	skin.default_font_size = 18
	skin.set_font("font", "Label", BODY_FONT)
	skin.set_font("font", "Button", BOLD_FONT)
	skin.set_font("font", "CheckButton", BODY_FONT)
	skin.set_font("font", "TooltipLabel", BODY_FONT)
	skin.set_font_size("font_size", "TooltipLabel", 16)
	skin.set_color("font_color", "TooltipLabel", PARCHMENT)
	skin.set_stylebox("panel", "TooltipPanel", panel(Color("#122124f6"), AETHER.darkened(0.52), 10))
	skin.set_stylebox("panel", "PanelContainer", panel(SURFACE, HAIRLINE, 12))
	skin.set_stylebox("normal", "LineEdit", panel(SURFACE, HAIRLINE, 8))
	skin.set_stylebox("focus", "LineEdit", panel(SURFACE_RAISED, AETHER, 8))
	root.theme = skin

static func set_display(label: Label) -> void:
	label.add_theme_font_override("font", DISPLAY_FONT)
	# Shadows on thin Cinzel strokes were visibly doubling glyph contours,
	# particularly in the pause and route overlays on 1080p displays.
	label.add_theme_color_override("font_shadow_color", Color.TRANSPARENT)
	label.add_theme_constant_override("shadow_offset_y",0)
	label.add_theme_constant_override("shadow_offset_x",0)

static func room_icon(kind: String) -> AtlasTexture:
	if SERVICE_MAP_ORDER.has(kind):
		return atlas(SERVICE_MAP_ICONS,SERVICE_MAP_ORDER.find(kind))
	if SPECIAL_ROOM_ORDER.has(kind):
		return atlas(SPECIAL_ROOM_ICONS,SPECIAL_ROOM_ORDER.find(kind))
	return atlas(ICONS,int(ROOM_ICONS.get(kind,4)))

static func atlas(texture: Texture2D, index: int) -> AtlasTexture:
	var icon := AtlasTexture.new()
	icon.atlas=texture
	if texture==RELICS and index==2:
		icon.atlas=CLASS_RELICS
		icon.region=Rect2(0,0,48,48)
	elif texture==RELICS and index>=48:
		icon.atlas=CLASS_RELICS
		icon.region=Rect2((index-47)*48,0,48,48)
	elif texture==RELICS and index>=40:
		icon.atlas=CROWN_RELICS
		icon.region=Rect2((index-40)*48,0,48,48)
	else:
		var cell: int=48 if texture==RELICS else 24
		icon.region=Rect2(index*cell,0,cell,cell)
	return icon

static func panel(fill: Color, edge: Color, margin: int = 12) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = fill
	s.border_color = Color(edge, edge.a * 0.72)
	s.set_border_width_all(1)
	s.border_width_top = 2
	s.set_corner_radius_all(12)
	s.corner_detail = 12
	s.anti_aliasing = true
	s.anti_aliasing_size = 1.2
	s.shadow_color = Color(0.015,0.035,0.04,0.72)
	s.shadow_size = 13
	s.shadow_offset = Vector2(0,7)
	s.content_margin_left = margin
	s.content_margin_right = margin
	s.content_margin_top = margin
	s.content_margin_bottom = margin
	return s

static func button_surface(fill: Color, accent: Color, selected: bool = false) -> StyleBoxFlat:
	var s := panel(fill, accent, 12)
	s.border_width_top = 2 if selected else 1
	s.border_width_bottom = 1
	s.shadow_size = 9 if selected else 6
	s.shadow_color = Color(accent, 0.13 if selected else 0.035)
	s.shadow_offset = Vector2(0,3)
	return s

static func style_button(button: Button, color: Color, selected: bool=false) -> void:
	button.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
	button.add_theme_font_override("font", BOLD_FONT)
	button.add_theme_color_override("font_shadow_color",Color.TRANSPARENT)
	button.add_theme_stylebox_override("normal", button_surface(Color("#1b2b2e") if not selected else Color("#304143"), color, selected))
	button.add_theme_stylebox_override("hover", button_surface(Color("#34484a"), color.lightened(0.22), true))
	button.add_theme_stylebox_override("pressed", button_surface(Color("#3d5251"), color, true))
	button.add_theme_stylebox_override("disabled", button_surface(Color("#162123"), HAIRLINE.darkened(0.32)))
	var focus := panel(Color(0,0,0,0), color.lightened(0.20), 5)
	focus.shadow_size = 0
	button.add_theme_stylebox_override("focus", focus)
	button.add_theme_color_override("font_color", PARCHMENT)
	button.add_theme_color_override("font_hover_color", Color.WHITE)
	button.add_theme_color_override("font_pressed_color", PARCHMENT)
	button.add_theme_color_override("font_disabled_color", Color("#83928e"))
	button.add_theme_color_override("font_focus_color", PARCHMENT)
