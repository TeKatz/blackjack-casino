extends Node2D
## A viewport-culled, procedurally drawn vertical slice; simulation is separate.
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const BG := Color("#0a1920")
const GRID := Color("#1b3037")
const IRON := Color("#83a6b0")
const COPPER := Color("#db986e")
const GOLD := Color("#f0cd83")
const TEXT := Color("#d8e8e7")
const GREEN := Color("#89d1ad")
const RED := Color("#f19885")
const TOOL_NAMES := ["miner", "belt", "smelter", "storage"]

@onready var camera: Camera2D = $Camera
var model
var tool := "belt"
var facing := 0
var clock := 0.0
var draw_accumulator := 0.0
var ui_accumulator := 0.0
var save_accumulator := 0.0
var moving_camera := false
var simulation_paused := false
var hovering := Vector2i(-1, -1)
var german := true
var tool_buttons: Dictionary = {}
var status: Label
var production: Label
var selected: Label
var information: Label

func _ready() -> void:
	german = not "--language=en" in OS.get_cmdline_user_args()
	model = Model.new()
	var loaded: bool = model.load_game()
	camera.position = Vector2(20.5, 17.0) * TILE
	camera.zoom = Vector2.ONE
	_build_interface()
	_status(_tr("Spielstand geladen · Autosave aktiv", "Save loaded · autosave enabled") if loaded else
		_tr("Starter-Fabrik bereit · Baue eine eigene Kupferlinie!", "Starter factory ready · build your own copper line!"))
	_refresh_ui()
	get_tree().auto_accept_quit = false

func _tr(de: String, en: String) -> String:
	return de if german else en

func _panel_style() -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#101f27")
	s.border_color = Color("#46636a")
	s.set_border_width_all(1)
	s.set_corner_radius_all(13)
	s.set_content_margin_all(13)
	s.shadow_color = Color(0, 0, 0, 0.27)
	s.shadow_size = 9
	return s

func _button_style(active: bool = false) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#345b55") if active else Color("#1a3339")
	s.border_color = GOLD if active else Color("#3f5862")
	s.set_border_width_all(1)
	s.set_corner_radius_all(8)
	s.set_content_margin_all(9)
	return s

func _text_label(value: String, font_size: int = 16, color: Color = TEXT) -> Label:
	var l := Label.new()
	l.text = value
	l.add_theme_color_override("font_color", color)
	l.add_theme_font_size_override("font_size", font_size)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return l

func _build_interface() -> void:
	var layer := CanvasLayer.new()
	layer.layer = 5
	add_child(layer)
	var hud := Control.new()
	layer.add_child(hud)
	hud.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE

	var header := PanelContainer.new()
	hud.add_child(header)
	header.set_anchors_preset(Control.PRESET_TOP_WIDE)
	header.offset_left = 20
	header.offset_right = -20
	header.offset_top = 17
	header.offset_bottom = 104
	header.add_theme_stylebox_override("panel", _panel_style())
	var title_row := HBoxContainer.new()
	title_row.add_theme_constant_override("separation", 28)
	header.add_child(title_row)
	var identity := VBoxContainer.new()
	identity.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title_row.add_child(identity)
	identity.add_child(_text_label("MIDNIGHT INDUSTRIES", 27, GOLD))
	identity.add_child(_text_label(_tr("INDUSTRIELLE AUTOMATISIERUNG  /  PROTOTYP", "INDUSTRIAL AUTOMATION  /  PROTOTYPE"), 12, Color("#83a6ae")))
	production = _text_label("", 17, GREEN)
	production.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	title_row.add_child(production)

	var tools := PanelContainer.new()
	hud.add_child(tools)
	tools.set_anchors_preset(Control.PRESET_TOP_LEFT)
	tools.offset_left = 20
	tools.offset_top = 118
	tools.offset_right = 252
	tools.offset_bottom = 512
	tools.add_theme_stylebox_override("panel", _panel_style())
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 8)
	tools.add_child(column)
	column.add_child(_text_label(_tr("BAUWERKZEUGE", "BUILD TOOLS"), 13, GOLD))
	column.add_child(_text_label(_tr("1—4 wählen · R drehen", "1—4 select · R rotate"), 12, Color("#91abb1")))
	var names := [
		_tr("MINER", "MINER"),
		_tr("FÖRDERBAND", "CONVEYOR"),
		_tr("SCHMELZOFEN", "SMELTER"),
		_tr("LAGER", "WAREHOUSE")
	]
	for i in range(TOOL_NAMES.size()):
		var id: String = TOOL_NAMES[i]
		var b := Button.new()
		b.text = "%d   %s" % [i + 1, names[i]]
		b.custom_minimum_size = Vector2(190, 46)
		b.alignment = HORIZONTAL_ALIGNMENT_LEFT
		b.add_theme_color_override("font_color", TEXT)
		b.add_theme_color_override("font_hover_color", GOLD)
		b.add_theme_color_override("font_pressed_color", GOLD)
		b.add_theme_font_size_override("font_size", 15)
		b.add_theme_stylebox_override("normal", _button_style(id == tool))
		b.add_theme_stylebox_override("hover", _button_style(true))
		b.add_theme_stylebox_override("pressed", _button_style(true))
		b.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
		b.pressed.connect(_select_tool.bind(id))
		column.add_child(b)
		tool_buttons[id] = b
	column.add_child(HSeparator.new())
	selected = _text_label("", 13, Color("#a8c5c7"))
	column.add_child(selected)

	var inspector := PanelContainer.new()
	hud.add_child(inspector)
	inspector.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	inspector.offset_left = -330
	inspector.offset_right = -20
	inspector.offset_top = 118
	inspector.offset_bottom = 242
	inspector.add_theme_stylebox_override("panel", _panel_style())
	var inspect_col := VBoxContainer.new()
	inspector.add_child(inspect_col)
	inspect_col.add_child(_text_label(_tr("PRODUKTIONSÜBERSICHT", "PRODUCTION OVERVIEW"), 13, GOLD))
	information = _text_label("", 14, TEXT)
	information.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	inspect_col.add_child(information)

	var footer := PanelContainer.new()
	hud.add_child(footer)
	footer.set_anchors_preset(Control.PRESET_BOTTOM_WIDE)
	footer.offset_left = 20
	footer.offset_right = -20
	footer.offset_top = -86
	footer.offset_bottom = -18
	footer.add_theme_stylebox_override("panel", _panel_style())
	var footer_row := HBoxContainer.new()
	footer_row.add_theme_constant_override("separation", 22)
	footer.add_child(footer_row)
	status = _text_label("", 14, GREEN)
	status.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	footer_row.add_child(status)
	var instructions := _text_label(_tr("WASD Kamera · Mausrad Zoom · Mittlere Taste Verschieben · Rechtsklick Abreißen · Leertaste Pause · Strg+S Speichern · Esc Lobby",
		"WASD camera · wheel zoom · middle drag pan · right click demolish · Space pause · Ctrl+S save · Esc lobby"), 12, Color("#91abb1"))
	instructions.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	footer_row.add_child(instructions)

func _status(message: String) -> void:
	if status != null:
		status.text = message

func _select_tool(id: String) -> void:
	tool = id
	_refresh_ui()

func _refresh_ui() -> void:
	if production == null:
		return
	var quantities: Dictionary = model.delivered
	production.text = "%s  %d     %s  %d" % [
		_tr("EISEN", "IRON"), quantities.iron_ingot,
		_tr("KUPFER", "COPPER"), quantities.copper_ingot,
	]
	var counts: Dictionary = model.counts()
	information.text = "%s  %d     %s  %d\n%s  %d     %s  %d" % [
		_tr("Miner", "Miners"), counts.miner,
		_tr("Bänder", "Belts"), counts.belt,
		_tr("Öfen", "Smelters"), counts.smelter,
		_tr("Lager", "Storage"), counts.storage,
	]
	selected.text = _tr("Aktiv: ", "Selected: ") + tool.to_upper() + "\n" + _tr("Richtung: ", "Direction: ") + ["→", "↓", "←", "↑"][facing]
	for id in tool_buttons:
		tool_buttons[id].add_theme_stylebox_override("normal", _button_style(id == tool))

func _physics_process(dt: float) -> void:
	if not simulation_paused:
		model.advance(dt)
		save_accumulator += dt
		if save_accumulator >= 30.0:
			save_accumulator = 0.0
			if model.save_game():
				_status(_tr("Automatisch gespeichert", "Autosaved"))
	ui_accumulator += dt
	if ui_accumulator >= 0.25:
		ui_accumulator = 0.0
		_refresh_ui()

func _process(dt: float) -> void:
	clock += dt
	var movement := Vector2.ZERO
	if Input.is_key_pressed(KEY_W) or Input.is_key_pressed(KEY_UP):
		movement.y -= 1.0
	if Input.is_key_pressed(KEY_S) or Input.is_key_pressed(KEY_DOWN):
		movement.y += 1.0
	if Input.is_key_pressed(KEY_A) or Input.is_key_pressed(KEY_LEFT):
		movement.x -= 1.0
	if Input.is_key_pressed(KEY_D) or Input.is_key_pressed(KEY_RIGHT):
		movement.x += 1.0
	if movement != Vector2.ZERO:
		camera.position += movement.normalized() * 800.0 * dt / camera.zoom.x
		_bound_camera()
	var mouse_world := get_global_mouse_position()
	hovering = Vector2i(floori(mouse_world.x / TILE), floori(mouse_world.y / TILE))
	draw_accumulator += dt
	if draw_accumulator >= 1.0 / 30.0:
		draw_accumulator = 0.0
		queue_redraw()

func _bound_camera() -> void:
	camera.position.x = clampf(camera.position.x, 0.0, float(Model.WIDTH) * TILE)
	camera.position.y = clampf(camera.position.y, 0.0, float(Model.HEIGHT) * TILE)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP and event.pressed:
			camera.zoom *= 1.12
			camera.zoom = camera.zoom.clamp(Vector2(0.55, 0.55), Vector2(2.4, 2.4))
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN and event.pressed:
			camera.zoom /= 1.12
			camera.zoom = camera.zoom.clamp(Vector2(0.55, 0.55), Vector2(2.4, 2.4))
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_MIDDLE:
			moving_camera = event.pressed
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_LEFT and event.pressed:
			if model.place(hovering, tool, facing):
				_status(_tr("Gebäude platziert", "Building placed"))
			else:
				_status(_tr("Hier kann das Gebäude nicht stehen", "Cannot build on this tile"))
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_RIGHT and event.pressed:
			if model.erase(hovering):
				_status(_tr("Gebäude entfernt · Inhalt verworfen", "Building removed · contents discarded"))
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and moving_camera:
		camera.position -= event.relative / camera.zoom
		_bound_camera()
		get_viewport().set_input_as_handled()
	elif event is InputEventKey and event.pressed and not event.echo:
		if event.ctrl_pressed and event.keycode == KEY_S:
			_status(_tr("Gespeichert", "Saved") if model.save_game() else _tr("Speichern fehlgeschlagen", "Save failed"))
		elif event.keycode >= KEY_1 and event.keycode <= KEY_4:
			_select_tool(TOOL_NAMES[event.keycode - KEY_1])
		elif event.keycode == KEY_R:
			facing = (facing + 1) % 4
			_refresh_ui()
		elif event.keycode == KEY_SPACE:
			simulation_paused = not simulation_paused
			_status(_tr("Pausiert", "Paused") if simulation_paused else _tr("Produktion läuft", "Production running"))
		elif event.keycode == KEY_ESCAPE:
			model.save_game()
			get_tree().quit()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST and model != null:
		model.save_game()
		get_tree().quit()

func _exit_tree() -> void:
	if model != null:
		model.save_game()

func _draw() -> void:
	draw_rect(Rect2(0, 0, float(Model.WIDTH) * TILE, float(Model.HEIGHT) * TILE), BG)
	var viewport_size := get_viewport_rect().size / camera.zoom
	var from := camera.position - viewport_size / 2.0
	var to := camera.position + viewport_size / 2.0
	var left := clampi(floori(from.x / TILE) - 1, 0, Model.WIDTH - 1)
	var right := clampi(ceili(to.x / TILE) + 1, 0, Model.WIDTH - 1)
	var top := clampi(floori(from.y / TILE) - 1, 0, Model.HEIGHT - 1)
	var bottom := clampi(ceili(to.y / TILE) + 1, 0, Model.HEIGHT - 1)
	for y in range(top, bottom + 1):
		for x in range(left, right + 1):
			var cell := Vector2i(x, y)
			var pos := Vector2(x, y) * TILE
			var ground := Color("#10252a") if (x * 31 + y * 13) % 9 < 3 else Color("#11282d")
			draw_rect(Rect2(pos, Vector2(TILE, TILE)), ground)
			draw_rect(Rect2(pos, Vector2(TILE, TILE)), GRID, false, 0.8)
			if model.resources.has(cell):
				_draw_deposit(pos, str(model.resources[cell]), x, y)
			if model.buildings.has(cell):
				_draw_machine(pos, model.buildings[cell], cell)
	draw_rect(Rect2(0, 0, float(Model.WIDTH) * TILE, float(Model.HEIGHT) * TILE), Color("#71959b"), false, 3.0)
	if model.inside(hovering):
		var pos := Vector2(hovering) * TILE
		draw_rect(Rect2(pos + Vector2(2, 2), Vector2(TILE - 4, TILE - 4)),
			GREEN if model.can_place(hovering, tool) else RED, false, 2.0)
		if model.can_place(hovering, tool):
			draw_rect(Rect2(pos + Vector2(5, 5), Vector2(TILE - 10, TILE - 10)),
				Color(GREEN, 0.16))

func _draw_deposit(pos: Vector2, material: String, x: int, y: int) -> void:
	var c := IRON if material == "iron" else COPPER
	var center := pos + Vector2(24, 24)
	draw_circle(center, 17.0, Color(c, 0.10))
	for i in range(3):
		var dx := float((x * 17 + y * 7 + i * 13) % 26) - 13.0
		var dy := float((y * 13 + x * 5 + i * 11) % 22) - 11.0
		var p := center + Vector2(dx, dy)
		draw_colored_polygon(PackedVector2Array([p + Vector2(-8, 4), p + Vector2(-3, -6),
			p + Vector2(5, -8), p + Vector2(9, 3), p + Vector2(1, 8)]),
			c.darkened(0.33))
		draw_line(p + Vector2(-3, -5), p + Vector2(5, -7), c.lightened(0.27), 2.0)

func _draw_machine(pos: Vector2, machine: Dictionary, cell: Vector2i) -> void:
	var kind: String = machine.kind
	var direction: int = machine.direction
	var rect := Rect2(pos + Vector2(3, 3), Vector2(42, 42))
	draw_rect(rect, Color("#0b171c"))
	draw_rect(rect, Color("#3e6269"), false, 2.0)
	match kind:
		"miner":
			draw_rect(Rect2(pos + Vector2(8, 8), Vector2(32, 32)), Color("#395a62"))
			draw_rect(Rect2(pos + Vector2(13, 11), Vector2(22, 27)), Color("#1d353d"), false, 2.0)
			draw_circle(pos + Vector2(24, 24), 12, IRON)
			draw_circle(pos + Vector2(24, 24), 7, Color("#263f49"))
			var t := clock * 2.5
			draw_line(pos + Vector2(24,24) + Vector2.RIGHT.rotated(t) * 4,
				pos + Vector2(24,24) + Vector2.RIGHT.rotated(t) * 10, GOLD, 3)
			_draw_arrow(pos, direction, GOLD)
		"belt":
			draw_rect(Rect2(pos + Vector2(7, 8), Vector2(34, 32)), Color("#2b3e44"))
			draw_rect(Rect2(pos + Vector2(8, 12), Vector2(32, 24)), Color("#50626b"))
			var horizontal: bool = direction % 2 == 0
			for i in range(4):
				var offset: float = fposmod(float(i) * 11.0 + clock * 34.0, 42.0)
				if horizontal:
					draw_line(pos + Vector2(3 + offset, 14), pos + Vector2(3 + offset, 34), Color("#91a5a6"), 2)
				else:
					draw_line(pos + Vector2(14, 3 + offset), pos + Vector2(34, 3 + offset), Color("#91a5a6"), 2)
			draw_rect(Rect2(pos + Vector2(3, 5), Vector2(42, 4)), Color("#14262c"))
			draw_rect(Rect2(pos + Vector2(3, 39), Vector2(42, 4)), Color("#14262c"))
			_draw_arrow(pos, direction, GOLD)
			if machine.item != "":
				var cargo_color := COPPER if str(machine.item).begins_with("copper") else IRON
				if str(machine.item).ends_with("ingot"):
					draw_rect(Rect2(pos + Vector2(19, 18), Vector2(12, 12)), cargo_color.lightened(0.2))
					draw_rect(Rect2(pos + Vector2(19, 18), Vector2(12, 12)), Color("#f5f1d5"), false, 1.0)
				else:
					draw_circle(pos + Vector2(24, 24), 7, cargo_color)
		"smelter":
			draw_rect(Rect2(pos + Vector2(7, 6), Vector2(34, 36)), Color("#3f5258"))
			draw_rect(Rect2(pos + Vector2(13, 12), Vector2(22, 26)), Color("#14242b"))
			var active: bool = machine.ore > 0 or machine.output > 0
			var glow: Color = Color("#ff7a3b") if active else Color("#694c40")
			draw_rect(Rect2(pos + Vector2(17, 20), Vector2(14, 13)), glow)
			if active:
				draw_circle(pos + Vector2(24, 25), 8.0 + 1.5 * sin(clock * 4.0), Color("#ffcb6f", 0.25))
			draw_rect(Rect2(pos + Vector2(10, 7), Vector2(9, 7)), Color("#8c9ea0"))
			draw_rect(Rect2(pos + Vector2(28, 7), Vector2(9, 7)), Color("#8c9ea0"))
			_draw_arrow(pos, direction, GOLD)
		"storage":
			draw_rect(Rect2(pos + Vector2(8, 9), Vector2(32, 30)), Color("#7b6348"))
			draw_rect(Rect2(pos + Vector2(12, 12), Vector2(24, 25)), Color("#a08459"), false, 2.0)
			draw_line(pos + Vector2(8, 21), pos + Vector2(40, 21), Color("#3b3d36"), 3)
			draw_line(pos + Vector2(24, 9), pos + Vector2(24, 39), Color("#3b3d36"), 3)
	if kind != "belt":
		draw_circle(pos + Vector2(41, 7), 2.5, GREEN if (kind == "miner" or machine.progress > 0.0) else Color("#697e83"))

func _draw_arrow(pos: Vector2, direction: int, color: Color) -> void:
	var center := pos + Vector2(24, 24)
	var vector: Vector2 = Vector2(Model.DIRS[direction])
	var side := Vector2(-vector.y, vector.x)
	var p := center + vector * 17
	draw_colored_polygon(PackedVector2Array([p, p - vector * 7 + side * 4,
		p - vector * 7 - side * 4]), color)
