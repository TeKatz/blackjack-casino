extends Control
## Native Godot window chrome for desktop windowed mode. Hidden in fullscreen
## and borderless-maximized modes. The OS titlebar is deliberately suppressed.
const MenuTheme = preload("res://factory_menu_theme.gd")
const UiSkin = preload("res://factory_skin.gd")
var game: Node
var resize_grip: Label

func _ready() -> void:
	name = "MidnightWindowChrome"
	set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	offset_top = 0.0
	offset_bottom = 30.0
	mouse_filter = Control.MOUSE_FILTER_STOP
	var background := ColorRect.new()
	background.color = Color("#112534")
	background.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(background)
	background.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var row := HBoxContainer.new()
	row.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	row.add_theme_constant_override("separation", 0)
	add_child(row)
	var grab := Control.new()
	grab.name = "WindowTitleDragArea"
	grab.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	grab.mouse_filter = Control.MOUSE_FILTER_STOP
	grab.gui_input.connect(_drag_input)
	row.add_child(grab)
	var title := Label.new()
	title.text = "  MIDNIGHT INDUSTRIES   /   FACTORY CONTROL"
	UiSkin.font()
	MenuTheme.label(title, 11, MenuTheme.CYAN)
	title.mouse_filter = Control.MOUSE_FILTER_IGNORE
	grab.add_child(title)
	title.set_anchors_and_offsets_preset(Control.PRESET_CENTER_LEFT)
	title.position.y = -9.0
	_chrome_button(row, "—", _minimize, false)
	_chrome_button(row, "□", _maximize, false)
	_chrome_button(row, "×", _close, true)
	var resize := Label.new()
	resize.name = "WindowResizeGrip"
	resize.text = "◢"
	resize.tooltip_text = "Drag to resize / Fenstergröße ändern"
	MenuTheme.label(resize, 18, MenuTheme.CYAN)
	resize.mouse_filter = Control.MOUSE_FILTER_STOP
	resize.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
	resize.offset_left = -24
	resize.offset_top = -28
	resize.offset_right = -3
	resize.offset_bottom = -2
	resize.gui_input.connect(_resize_input)
	resize_grip = resize
	# Keep resize grip in a sibling so it remains at the actual window corner.
	var layer: CanvasLayer = game.ui_layer
	layer.add_child(resize)
	resize.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_RIGHT)
	visible = false
	resize_grip.visible = false

func set_windowed(on: bool) -> void:
	visible = on
	if is_instance_valid(resize_grip):
		resize_grip.visible = on

func _chrome_button(parent: Node, caption: String, action: Callable,
	danger: bool) -> void:
	var b := Button.new()
	b.text = caption
	b.tooltip_text = caption
	b.custom_minimum_size = Vector2(44, 30)
	MenuTheme.style_button(b, danger)
	b.add_theme_font_size_override("font_size", 17)
	b.pressed.connect(action)
	parent.add_child(b)

func _drag_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed \
		and event.button_index == MOUSE_BUTTON_LEFT:
		if event.double_click:
			_maximize()
		else:
			DisplayServer.window_start_drag()
		accept_event()

func _resize_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed \
		and event.button_index == MOUSE_BUTTON_LEFT:
		DisplayServer.window_start_resize(DisplayServer.WINDOW_EDGE_BOTTOM_RIGHT)
		accept_event()

func _minimize() -> void:
	DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MINIMIZED)

func _maximize() -> void:
	game.navigation._change_setting("window_mode", 1)

func _close() -> void:
	if game.current_world.is_empty():
		game.get_tree().quit()
	else:
		game._exit_to_casino()
