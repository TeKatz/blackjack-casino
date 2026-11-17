extends RefCounted
const UiSkin = preload("res://factory_skin.gd")
## Dedicated high-fidelity native menu theme. NOT applied to the established
## in-world factory HUD or building art. No texture-stretched gold corners.
const BASE := Color("#101E29")
const RAISED := Color("#1B303E")
const PANEL := Color("#142430")
const TEXT := Color("#ECF4F4")
const MUTED := Color("#9DAFBD")
const CYAN := Color("#5DCEE6")
const AMBER := Color("#F2C273")
const RED := Color("#F17979")
const BORDER := Color("#405868")

static func panel(kind: String = "main") -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#122431") if kind == "main" else (
		Color("#1D3442") if kind == "section" else Color("#10202C"))
	s.border_color = Color("#4A7283", 0.95) if kind == "main" else Color("#385463", 0.84)
	s.set_border_width_all(1)
	s.set_corner_radius_all(16 if kind == "main" else 9)
	s.corner_detail = 12
	s.shadow_color = Color(0.0, 0.0, 0.0, 0.50) if kind == "main" else Color(0, 0, 0, 0.22)
	s.shadow_size = 24 if kind == "main" else 8
	s.shadow_offset = Vector2(0, 9 if kind == "main" else 3)
	s.content_margin_left = 26 if kind == "main" else 16
	s.content_margin_right = 26 if kind == "main" else 16
	s.content_margin_top = 20 if kind == "main" else 12
	s.content_margin_bottom = 20 if kind == "main" else 12
	s.anti_aliasing = true
	return s

static func button(state: String = "normal", danger: bool = false) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	var selected := state == "selected" or state == "pressed"
	var hovered := state == "hover" or state == "focus"
	s.bg_color = Color("#315367") if selected else (
		Color("#284354") if hovered else Color("#1B3342"))
	if danger:
		s.bg_color = Color("#713844") if selected else (
			Color("#55303C") if hovered else Color("#352733"))
	s.border_color = (RED if danger else CYAN) if selected else (
		(Color("#BA6573") if danger else Color("#678D9C")) if hovered else Color("#3E5967"))
	s.set_border_width_all(1)
	s.set_corner_radius_all(8)
	s.content_margin_left = 13
	s.content_margin_right = 13
	s.content_margin_top = 8
	s.content_margin_bottom = 8
	s.shadow_color = Color(0, 0, 0, 0.27)
	s.shadow_size = 4
	s.shadow_offset = Vector2(0, 2)
	return s

static func field() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#081822")
	s.border_color = Color("#416374")
	s.set_border_width_all(1)
	s.set_corner_radius_all(7)
	s.content_margin_left = 13
	s.content_margin_right = 13
	s.content_margin_top = 8
	s.content_margin_bottom = 8
	return s

static func slider_track() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#0B1A24")
	s.set_corner_radius_all(4)
	s.content_margin_top = 4
	s.content_margin_bottom = 4
	return s

static func slider_fill() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = CYAN
	s.set_corner_radius_all(4)
	s.content_margin_top = 4
	s.content_margin_bottom = 4
	return s

static func style_button(node: Button, danger: bool = false,
	active: bool = false) -> void:
	node.add_theme_stylebox_override("normal", button("selected" if active else "normal", danger))
	node.add_theme_stylebox_override("hover", button("hover", danger))
	node.add_theme_stylebox_override("pressed", button("selected", danger))
	node.add_theme_stylebox_override("hover_pressed", button("selected", danger))
	node.add_theme_stylebox_override("focus", button("focus", danger))
	node.add_theme_stylebox_override("disabled", button("normal", danger))
	node.add_theme_color_override("font_color", TEXT)
	node.add_theme_color_override("font_hover_color", TEXT)
	node.add_theme_color_override("font_pressed_color", TEXT)
	node.add_theme_color_override("font_disabled_color", MUTED)
	node.add_theme_font_override("font", UiSkin.font())
	node.add_theme_font_size_override("font_size", 14)
	node.mouse_default_cursor_shape = Control.CURSOR_POINTING_HAND

static func style_input(node: LineEdit) -> void:
	node.add_theme_stylebox_override("normal", field())
	node.add_theme_stylebox_override("focus", button("focus"))
	node.add_theme_color_override("font_color", TEXT)
	node.add_theme_color_override("font_placeholder_color", MUTED)
	node.add_theme_font_override("font", UiSkin.font())
	node.add_theme_font_size_override("font_size", 15)

static func style_option(node: OptionButton) -> void:
	style_button(node)
	node.add_theme_stylebox_override("normal", field())
	node.add_theme_stylebox_override("hover", button("hover"))
	node.add_theme_font_size_override("font_size", 15)
	node.custom_minimum_size.y = 42

static func style_slider(node: HSlider) -> void:
	node.add_theme_stylebox_override("slider", slider_track())
	node.add_theme_stylebox_override("grabber_area", slider_fill())
	node.add_theme_stylebox_override("grabber_area_highlight", slider_fill())
	node.custom_minimum_size.y = 26

static func label(node: Label, px: int = 14, tint: Color = TEXT) -> void:
	node.add_theme_font_override("font", UiSkin.font())
	node.add_theme_font_size_override("font_size", px)
	node.add_theme_color_override("font_color", tint)
	node.mouse_filter = Control.MOUSE_FILTER_IGNORE
