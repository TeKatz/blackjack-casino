extends RefCounted
const ICONS = preload("res://art/pixel/ui_icons.svg")
const RELICS = preload("res://art/pixel/relics.png")
const CROWN_RELICS = preload("res://art/pixel/crown_relics.svg")
const CLASS_RELICS = preload("res://art/pixel/class_relics.svg")
const SPECIAL_ROOM_ICONS = preload("res://art/pixel/special_room_icons.svg")
const SERVICE_MAP_ICONS = preload("res://art/pixel/service_map_icons.svg")
const SERVICE_MAP_ORDER = ["shrine","event","shop"]
const SPECIAL_ROOM_ORDER = ["ritual","survival","bounty","unstable","blood_altar","forgotten_forge","archive"]
const RARITY_COLORS = [Color("#b7b8ca"),Color("#63d2b1"),Color("#bd94f2"),Color("#f6c66c")]
const ROOM_ICONS = {"fight":0,"elite":1,"treasure":2,"shop":3,"event":4,"shrine":5,"secret":6,"boss":7,"forge":12,"?":4,"ritual":0,"survival":0,"bounty":0,"unstable":0,"blood_altar":0,"forgotten_forge":0,"archive":0}
const ROOM_COLORS = {"fight":Color("#c1a6df"),"elite":Color("#ef8b88"),"treasure":Color("#f2ca79"),"shop":Color("#72d4bc"),"event":Color("#d4a1ff"),"shrine":Color("#9ad9b1"),"secret":Color("#d7a5ce"),"boss":Color("#f17b9b"),"forge":Color("#e2a875"),"?":Color("#a9a1b9"),"ritual":Color("#d6a0ff"),"survival":Color("#90d9f3"),"bounty":Color("#f5a071"),"unstable":Color("#8ff0dd"),"blood_altar":Color("#f16b89"),"forgotten_forge":Color("#ffb26b"),"archive":Color("#d6c3ff")}
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
		# Redesigned Shadow Steps boots use the first new pixel-art cell.
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
	var s:=StyleBoxFlat.new()
	s.bg_color=fill
	s.border_color=edge
	s.set_border_width_all(2)
	s.set_corner_radius_all(9)
	s.shadow_color=Color(0,0,0,0.6)
	s.shadow_size=5
	s.shadow_offset=Vector2(0,3)
	s.content_margin_left=margin
	s.content_margin_right=margin
	s.content_margin_top=margin
	s.content_margin_bottom=margin
	return s
static func style_button(button: Button, color: Color, selected: bool=false) -> void:
	button.texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
	button.add_theme_stylebox_override("normal",panel(Color("#30243c") if not selected else Color("#594333"),color.darkened(0.33),8))
	button.add_theme_stylebox_override("hover",panel(Color("#493555"),color,8))
	button.add_theme_stylebox_override("pressed",panel(Color("#654b70"),color,8))
	button.add_theme_stylebox_override("disabled",panel(Color("#1e192b"),color.darkened(0.65),8))
	button.add_theme_stylebox_override("focus",panel(Color(0,0,0,0),color,4))
	button.add_theme_color_override("font_color",Color("#f8effb"))
	button.add_theme_color_override("font_disabled_color",Color("#a59bae"))
	button.add_theme_color_override("font_hover_color",Color.WHITE)
