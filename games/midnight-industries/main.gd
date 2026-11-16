extends Node2D
## Midnight Industries: isolated simulation, cached world, animated cargo and native UI.
const Model = preload("res://factory_model.gd")
const StaticView = preload("res://factory_static.gd")
const MotionView = preload("res://factory_motion.gd")
const TILE := 48.0
const GOLD := Color("#efc67a")
const CYAN := Color("#91dacc")
const INK := Color("#0c1d24")
const TEXT := Color("#dcebe9")
const MUTED := Color("#92aeb4")
const DANGER := Color("#e79a89")
const TOOLS := ["miner", "belt", "smelter", "storage"]
const DIR_TEXT := ["→", "↓", "←", "↑"]

@onready var camera: Camera2D = $Camera
var model
var static_view: Node2D
var motion_view: Node2D
var mode := "inspect"
var tool := "belt"
var facing := 0
var hovered := Vector2i(-1, -1)
var inspected := Vector2i(-1, -1)
var german := true
var moving_camera := false
var simulation_paused := false
var reduced_motion := false
var menu_open := true
var save_accumulator := 0.0
var ui_accumulator := 0.0
var last_totals: String = ""
var menu_mask: ColorRect
var menu_card: PanelContainer
var header_status: Label
var production: Label
var fps_label: Label
var status: Label
var tool_title: Label
var tool_buttons: Dictionary = {}
var mode_buttons: Dictionary = {}
var inspector_title: Label
var inspector_body: Label
var inspector_subtitle: Label
var inspector_progress: ProgressBar
var inspector_controls: HBoxContainer
var rotate_button: Button
var demolish_button: Button
var pause_button: Button
var resume_button: Button
var new_button: Button
var effects_button: Button
var confirming_new := false

func _ready() -> void:
	german = not "--language=en" in OS.get_cmdline_user_args()
	model = Model.new()
	var previous: bool = model.load_game()
	camera.position = Vector2(21, 18) * TILE
	camera.zoom = Vector2.ONE
	_create_world()
	_create_interface()
	_set_mode("inspect")
	_refresh_ui()
	_show_menu(true)
	_notice(_tr("Spielstand geladen", "Save restored") if previous else
		_tr("Starterproduktion aufgebaut", "Starter line initialized"))
	get_tree().auto_accept_quit = false

func _tr(de: String, en: String) -> String:
	return de if german else en

func _create_world() -> void:
	var ground := ColorRect.new()
	ground.name = "CachedTerrainGrid"
	ground.position = Vector2.ZERO
	ground.size = Vector2(Model.WIDTH, Model.HEIGHT) * TILE
	ground.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ground.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
	var shader := Shader.new()
	shader.code = """
shader_type canvas_item;
void fragment() {
	vec2 coords = UV * vec2(128.0, 96.0);
	vec2 c = floor(coords);
	float pattern = fract(sin(dot(c, vec2(13.73, 78.31))) * 43758.5453);
	float subtle = (pattern - 0.5) * 0.012;
	vec3 base = vec3(0.047, 0.112, 0.132) + vec3(subtle);
	vec2 lines = fract(coords);
	float wire = max(1.0 - step(0.022, lines.x), 1.0 - step(0.022, lines.y));
	vec3 output_color = mix(base, vec3(0.098, 0.178, 0.195), wire * 0.42);
	COLOR = vec4(output_color, 1.0);
}
"""
	var mat := ShaderMaterial.new()
	mat.shader = shader
	ground.material = mat
	add_child(ground)

	static_view = StaticView.new()
	static_view.name = "CachedFactoryAndDeposits"
	static_view.model = model
	add_child(static_view)
	motion_view = MotionView.new()
	motion_view.name = "VisibleAnimatedMachinesAndCursor"
	motion_view.model = model
	motion_view.camera = camera
	add_child(motion_view)

func _panel_style(tone: String = "panel") -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#10262e") if tone == "panel" else Color("#142f38")
	s.border_color = Color("#355b65")
	s.set_border_width_all(1)
	s.set_corner_radius_all(14)
	s.set_content_margin_all(15)
	s.shadow_color = Color(0.0, 0.0, 0.0, 0.32)
	s.shadow_size = 10
	return s

func _button_style(active: bool = false, destructive: bool = false) -> StyleBoxFlat:
	var s := StyleBoxFlat.new()
	s.bg_color = Color("#355f5d") if active else Color("#1b3941")
	if destructive:
		s.bg_color = Color("#593b3a")
	s.border_color = GOLD if active else (DANGER if destructive else Color("#41616a"))
	s.set_border_width_all(1)
	s.set_corner_radius_all(9)
	s.set_content_margin_all(10)
	return s

func _make_label(value: String, size: int = 14, color: Color = TEXT) -> Label:
	var label := Label.new()
	label.text = value
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", color)
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return label

func _button(value: String, handler: Callable, destructive: bool = false) -> Button:
	var b := Button.new()
	b.text = value
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size.y = 43
	b.add_theme_color_override("font_color", TEXT)
	b.add_theme_color_override("font_hover_color", GOLD)
	b.add_theme_font_size_override("font_size", 14)
	b.add_theme_stylebox_override("normal", _button_style(false, destructive))
	b.add_theme_stylebox_override("hover", _button_style(true, destructive))
	b.add_theme_stylebox_override("pressed", _button_style(true, destructive))
	b.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
	b.pressed.connect(handler)
	b.mouse_entered.connect(_button_hover.bind(b, true))
	b.mouse_exited.connect(_button_hover.bind(b, false))
	return b

func _button_hover(b: Button, entered: bool) -> void:
	if reduced_motion:
		return
	var fade := create_tween()
	fade.tween_property(b, "modulate", Color("#ffffff") if entered else Color("#d9e7e7"), 0.15)

func _section_title(text_value: String) -> VBoxContainer:
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 4)
	v.add_child(_make_label(text_value, 12, GOLD))
	return v

func _create_interface() -> void:
	var layer := CanvasLayer.new()
	layer.name = "FactoryHUD"
	layer.layer = 8
	add_child(layer)
	var hud := Control.new()
	hud.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	layer.add_child(hud)

	var top := PanelContainer.new()
	top.add_theme_stylebox_override("panel", _panel_style())
	hud.add_child(top)
	top.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	top.offset_left = 18
	top.offset_right = -18
	top.offset_top = 16
	top.offset_bottom = 110
	var top_row := HBoxContainer.new()
	top_row.add_theme_constant_override("separation", 21)
	top.add_child(top_row)
	var title := VBoxContainer.new()
	title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title.add_theme_constant_override("separation", 2)
	top_row.add_child(title)
	title.add_child(_make_label("MIDNIGHT INDUSTRIES", 27, GOLD))
	title.add_child(_make_label(_tr("AUTOMATION  /  FABRIKSEKTOR 01", "AUTOMATION  /  FACTORY SECTOR 01"), 12, MUTED))
	var readouts := VBoxContainer.new()
	readouts.alignment = BoxContainer.ALIGNMENT_CENTER
	readouts.add_theme_constant_override("separation", 4)
	top_row.add_child(readouts)
	production = _make_label("", 18, CYAN)
	production.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	readouts.add_child(production)
	var small_row := HBoxContainer.new()
	small_row.alignment = BoxContainer.ALIGNMENT_END
	readouts.add_child(small_row)
	header_status = _make_label("", 11, MUTED)
	small_row.add_child(header_status)
	fps_label = _make_label("", 11, MUTED)
	small_row.add_child(fps_label)
	var menu_button := _button(_tr("MENÜ  [ESC]", "MENU  [ESC]"), func() -> void: _show_menu(true))
	menu_button.custom_minimum_size.x = 134
	top_row.add_child(menu_button)

	var sidebar := PanelContainer.new()
	sidebar.name = "WorkshopSidebar"
	sidebar.add_theme_stylebox_override("panel", _panel_style())
	hud.add_child(sidebar)
	sidebar.set_anchors_and_offsets_preset(Control.PRESET_TOP_LEFT)
	sidebar.offset_left = 18
	sidebar.offset_top = 122
	sidebar.offset_right = 289
	sidebar.offset_bottom = 598
	var side_col := VBoxContainer.new()
	side_col.add_theme_constant_override("separation", 11)
	sidebar.add_child(side_col)
	side_col.add_child(_make_label(_tr("WERKZEUGKASTEN", "FACTORY TOOLKIT"), 15, GOLD))
	var tabs := HBoxContainer.new()
	tabs.add_theme_constant_override("separation", 7)
	side_col.add_child(tabs)
	var inspect_btn := _button(_tr("AUSWAHL [V]", "INSPECT [V]"), func() -> void: _set_mode("inspect"))
	var build_btn := _button(_tr("BAUEN [B]", "BUILD [B]"), func() -> void: _set_mode("build"))
	inspect_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	build_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inspect_btn.add_theme_font_size_override("font_size", 12)
	build_btn.add_theme_font_size_override("font_size", 12)
	tabs.add_child(inspect_btn)
	tabs.add_child(build_btn)
	mode_buttons = {"inspect": inspect_btn, "build": build_btn}
	var split := HSeparator.new()
	side_col.add_child(split)
	tool_title = _make_label(_tr("MASCHINEN", "MACHINERY"), 12, MUTED)
	side_col.add_child(tool_title)
	var tool_names: Array[String] = [
		_tr("ABBAUMASCHINE", "MINING DRILL"),
		_tr("FÖRDERBAND", "CONVEYOR BELT"),
		_tr("SCHMELZOFEN", "SMELTING FURNACE"),
		_tr("LAGER", "STORAGE UNIT")
	]
	for index in range(TOOLS.size()):
		var key: String = TOOLS[index]
		var btn := _button("%d   %s" % [index + 1, tool_names[index]],
			_select_tool.bind(key))
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.custom_minimum_size.y = 46
		side_col.add_child(btn)
		tool_buttons[key] = btn
	var hint := _make_label(_tr("R  Drehen  ·  Klick  Platzieren", "R  Rotate  ·  Click  Place"), 12, MUTED)
	side_col.add_child(hint)
	hint.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART

	var inspector := PanelContainer.new()
	inspector.name = "MachineInspector"
	inspector.add_theme_stylebox_override("panel", _panel_style())
	hud.add_child(inspector)
	inspector.set_anchors_and_offsets_preset(Control.PRESET_TOP_RIGHT)
	inspector.offset_left = -348
	inspector.offset_right = -18
	inspector.offset_top = 122
	inspector.offset_bottom = 462
	var insight_col := VBoxContainer.new()
	insight_col.add_theme_constant_override("separation", 13)
	inspector.add_child(insight_col)
	insight_col.add_child(_make_label(_tr("MASCHINENINSPEKTOR", "MACHINE INSPECTOR"), 12, GOLD))
	inspector_title = _make_label("", 21, TEXT)
	insight_col.add_child(inspector_title)
	inspector_subtitle = _make_label("", 12, MUTED)
	insight_col.add_child(inspector_subtitle)
	insight_col.add_child(HSeparator.new())
	inspector_body = _make_label("", 14, TEXT)
	inspector_body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	insight_col.add_child(inspector_body)
	inspector_progress = ProgressBar.new()
	inspector_progress.custom_minimum_size.y = 10
	inspector_progress.show_percentage = false
	inspector_progress.min_value = 0.0
	inspector_progress.max_value = 100.0
	var progress_fill := StyleBoxFlat.new()
	progress_fill.bg_color = CYAN
	progress_fill.set_corner_radius_all(4)
	inspector_progress.add_theme_stylebox_override("fill", progress_fill)
	var progress_bg := StyleBoxFlat.new()
	progress_bg.bg_color = Color("#23404b")
	progress_bg.set_corner_radius_all(4)
	inspector_progress.add_theme_stylebox_override("background", progress_bg)
	insight_col.add_child(inspector_progress)
	inspector_controls = HBoxContainer.new()
	inspector_controls.add_theme_constant_override("separation", 7)
	insight_col.add_child(inspector_controls)
	rotate_button = _button(_tr("DREHEN", "ROTATE"), _rotate_selected)
	demolish_button = _button(_tr("ABREISSEN", "DEMOLISH"), _demolish_selected, true)
	rotate_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	demolish_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inspector_controls.add_child(rotate_button)
	inspector_controls.add_child(demolish_button)

	var bottom := PanelContainer.new()
	bottom.add_theme_stylebox_override("panel", _panel_style())
	hud.add_child(bottom)
	bottom.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	bottom.offset_left = 18
	bottom.offset_right = -18
	bottom.offset_top = -92
	bottom.offset_bottom = -18
	var footer_row := HBoxContainer.new()
	footer_row.add_theme_constant_override("separation", 14)
	bottom.add_child(footer_row)
	var left_footer := VBoxContainer.new()
	left_footer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	left_footer.add_theme_constant_override("separation", 5)
	footer_row.add_child(left_footer)
	status = _make_label("", 14, CYAN)
	left_footer.add_child(status)
	var controls_help := _make_label(_tr("WASD Kamera     Mausrad Zoom     Mittlere Maustaste Verschieben     Strg+S Speichern",
		"WASD Camera     Mousewheel Zoom     Middle Drag Pan     Ctrl+S Save"), 12, MUTED)
	left_footer.add_child(controls_help)
	pause_button = _button(_tr("PAUSE  [SPACE]", "PAUSE  [SPACE]"), _toggle_pause)
	pause_button.custom_minimum_size.x = 151
	footer_row.add_child(pause_button)
	var save_btn := _button(_tr("SPEICHERN", "SAVE"), _save_now)
	footer_row.add_child(save_btn)

	menu_mask = ColorRect.new()
	menu_mask.name = "PauseAndMainMenu"
	menu_mask.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	menu_mask.color = Color(0.025, 0.062, 0.077, 0.93)
	menu_mask.mouse_filter = Control.MOUSE_FILTER_STOP
	layer.add_child(menu_mask)
	menu_card = PanelContainer.new()
	menu_card.add_theme_stylebox_override("panel", _panel_style("menu"))
	menu_mask.add_child(menu_card)
	menu_card.set_anchors_and_offsets_preset(Control.PRESET_CENTER)
	menu_card.offset_left = -274
	menu_card.offset_right = 274
	menu_card.offset_top = -254
	menu_card.offset_bottom = 254
	var menu_content := VBoxContainer.new()
	menu_content.add_theme_constant_override("separation", 14)
	menu_card.add_child(menu_content)
	var menu_title := _make_label("MIDNIGHT INDUSTRIES", 27, GOLD)
	menu_title.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	menu_content.add_child(menu_title)
	var subtitle := _make_label(_tr("DEINE FABRIK. DEIN TEMPO.", "YOUR FACTORY. YOUR PACE."), 13, MUTED)
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	menu_content.add_child(subtitle)
	menu_content.add_child(HSeparator.new())
	resume_button = _button(_tr("FABRIK FORTSETZEN", "RESUME FACTORY"), func() -> void: _show_menu(false))
	resume_button.custom_minimum_size.y = 51
	menu_content.add_child(resume_button)
	var save_menu := _button(_tr("SPIELSTAND SPEICHERN", "SAVE PROGRESS"), _save_now)
	menu_content.add_child(save_menu)
	effects_button = _button(_tr("ANIMATIONEN: AN", "ANIMATIONS: ON"), _toggle_effects)
	menu_content.add_child(effects_button)
	new_button = _button(_tr("NEUE FABRIK STARTEN", "START NEW FACTORY"), _new_factory, true)
	menu_content.add_child(new_button)
	menu_content.add_child(HSeparator.new())
	var exit_btn := _button(_tr("SPEICHERN & ZUM CASINO", "SAVE & RETURN TO CASINO"), _exit_to_casino)
	exit_btn.custom_minimum_size.y = 49
	menu_content.add_child(exit_btn)
	menu_content.add_child(_make_label(_tr("Escape schließt dieses Menü. Dein Fortschritt wird automatisch gesichert.",
		"Escape closes this menu. Your progress is saved automatically."), 12, MUTED))

func _notice(message: String) -> void:
	if status == null:
		return
	status.text = message
	if not reduced_motion:
		status.modulate = Color("#ffffff")
		var tween := create_tween()
		tween.tween_property(status, "modulate", Color("#9fd2c9"), 0.55)

func _set_mode(next_mode: String) -> void:
	mode = next_mode
	motion_view.cursor_mode = mode
	if mode == "inspect":
		notice_selection()
	else:
		_notice(_tr("Baumodus aktiv · Richtung in der Vorschau sichtbar",
			"Build mode active · output direction visible in preview"))
	for key in mode_buttons:
		mode_buttons[key].add_theme_stylebox_override("normal", _button_style(key == mode))
	for key in tool_buttons:
		tool_buttons[key].disabled = mode == "inspect"
		tool_buttons[key].add_theme_stylebox_override("normal", _button_style(key == tool and mode == "build"))
	tool_title.text = _tr("BAUPALETTE", "BUILD PALETTE") if mode == "build" else _tr("BAUPALETTE · AUSWAHLMODUS", "BUILD PALETTE · INSPECT MODE")
	motion_view.queue_redraw()

func _select_tool(name: String) -> void:
	tool = name
	_set_mode("build")
	motion_view.tool = tool
	_notice(_tr("Ausgewählt: ", "Selected: ") + name.to_upper())

func notice_selection() -> void:
	if model.buildings.has(inspected):
		_notice(_tr("Maschine ausgewählt · Details rechts", "Machine selected · details on right"))
	else:
		_notice(_tr("Auswahlmodus · Klicke eine Maschine", "Inspect mode · click a machine"))

func _inspect(cell: Vector2i) -> void:
	inspected = cell if model.buildings.has(cell) else Vector2i(-1, -1)
	motion_view.inspected = inspected
	_refresh_inspector()
	notice_selection()

func _refresh_ui() -> void:
	if model == null or production == null:
		return
	var totals: Dictionary = model.delivered
	var output := "%s  %d    ·    %s  %d" % [
		_tr("EISEN", "IRON"), int(totals.iron_ingot),
		_tr("KUPFER", "COPPER"), int(totals.copper_ingot)
	]
	if last_totals != output:
		last_totals = output
		production.text = output
		if not reduced_motion:
			var pulse := create_tween()
			production.modulate = Color("#ffffff")
			pulse.tween_property(production, "modulate", CYAN, 0.35)
	var unit_count: int = model.buildings.size()
	var pause_state := _tr("PAUSIERT", "PAUSED") if simulation_paused else _tr("PRODUKTION AKTIV", "PRODUCTION LIVE")
	header_status.text = "●  %s   ·   %d %s    " % [pause_state, unit_count, _tr("Gebäude", "buildings")]
	fps_label.text = "FPS  %d" % Engine.get_frames_per_second()
	pause_button.text = _tr("WEITER  [SPACE]", "RESUME  [SPACE]") if simulation_paused else _tr("PAUSE  [SPACE]", "PAUSE  [SPACE]")
	_refresh_inspector()

func _refresh_inspector() -> void:
	if inspector_title == null:
		return
	if not model.buildings.has(inspected):
		inspector_title.text = _tr("Kein Gebäude gewählt", "No building selected")
		inspector_subtitle.text = _tr("AUSWAHLMODUS  [V]", "INSPECT MODE  [V]")
		inspector_body.text = _tr("Klicke einen Miner, ein Förderband, einen Ofen oder ein Lager. Hier erscheinen Zustand, Inventar und Produktionswerte.",
			"Click a drill, conveyor, furnace or storage unit to see status, inventory and throughput.")
		inspector_progress.visible = false
		inspector_controls.visible = false
		return
	var machine: Dictionary = model.buildings[inspected]
	var kind: String = machine.kind
	inspector_progress.visible = true
	inspector_controls.visible = true
	rotate_button.disabled = kind == "storage"
	var label_name := {"miner": _tr("Abbaumaschine", "Mining drill"),
		"belt": _tr("Förderband", "Conveyor belt"),
		"smelter": _tr("Schmelzofen", "Smelting furnace"),
		"storage": _tr("Lager", "Warehouse")}
	inspector_title.text = label_name[kind]
	inspector_subtitle.text = "%s  %d, %d     ·     %s  %s" % [
		_tr("POSITION", "POSITION"), inspected.x, inspected.y,
		_tr("AUSGANG", "OUTPUT"), DIR_TEXT[int(machine.direction)]]
	match kind:
		"miner":
			var ore_type: String = model.resources.get(inspected, "iron")
			inspector_body.text = _tr("Rohstoff: ", "Resource: ") + ore_type.to_upper() + "\n" + (
				_tr("Puffer: %d/%d Erz", "Buffer: %d/%d ore") % [machine.buffer, Model.MAX_BUFFER]) + "\n" + (
				_tr("Tempo: 1 Erz / Sekunde", "Rate: 1 ore / second"))
			inspector_progress.value = 100.0 * float(machine.progress) / 1.0
		"belt":
			var item: String = machine.item
			inspector_body.text = _tr("Transportiert: ", "Carrying: ") + (item.to_upper() if item != "" else
				_tr("Leer", "Empty")) + "\n" + _tr("Transport: 4 Felder / Sekunde", "Transport: 4 tiles / second") + "\n" + _tr("Kapazität: 1 Gegenstand", "Capacity: 1 item")
			inspector_progress.value = 100.0 if item != "" else 0.0
		"smelter":
			inspector_body.text = (_tr("Rezept: ", "Recipe: ") + str(machine.recipe).to_upper() + "\n" +
				_tr("Erz: %d/8    Produkt: %d/8", "Ore: %d/8    Output: %d/8") % [machine.ore, machine.output] + "\n" +
				_tr("Tempo: 1 Barren / 2 Sekunden", "Rate: 1 ingot / 2 seconds"))
			inspector_progress.value = clampf(100.0 * float(machine.progress) / 2.0, 0.0, 100.0)
		"storage":
			inspector_body.text = (_tr("Gemeinsame Lagerbilanz", "Shared warehouse totals") + "\n" +
				_tr("Eisenbarren: ", "Iron ingots: ") + str(model.delivered.iron_ingot) + "\n" +
				_tr("Kupferbarren: ", "Copper ingots: ") + str(model.delivered.copper_ingot))
			inspector_progress.value = 100.0

func _rotate_selected() -> void:
	if model.rotate(inspected):
		static_view.queue_redraw()
		_refresh_inspector()
		_notice(_tr("Ausgang gedreht", "Output rotated"))

func _demolish_selected() -> void:
	if model.erase(inspected):
		static_view.queue_redraw()
		_inspect(Vector2i(-1, -1))
		_notice(_tr("Gebäude demontiert · Inhalt verworfen", "Building demolished · items discarded"))

func _toggle_pause() -> void:
	simulation_paused = not simulation_paused
	_notice(_tr("Produktion angehalten", "Production paused") if simulation_paused else
		_tr("Produktion läuft", "Production running"))
	_refresh_ui()

func _save_now() -> void:
	_notice(_tr("Spielstand gespeichert", "Factory saved") if model.save_game() else
		_tr("Speichern fehlgeschlagen", "Save failed"))

func _toggle_effects() -> void:
	reduced_motion = not reduced_motion
	motion_view.reduced_motion = reduced_motion
	effects_button.text = _tr("ANIMATIONEN: AUS", "ANIMATIONS: OFF") if reduced_motion else _tr("ANIMATIONEN: AN", "ANIMATIONS: ON")

func _new_factory() -> void:
	if not confirming_new:
		confirming_new = true
		new_button.text = _tr("WIRKLICH NEU STARTEN?", "CONFIRM NEW FACTORY?")
		return
	confirming_new = false
	new_button.text = _tr("NEUE FABRIK STARTEN", "START NEW FACTORY")
	model = Model.new()
	static_view.model = model
	motion_view.model = model
	static_view.queue_redraw()
	inspected = Vector2i(-1, -1)
	motion_view.inspected = inspected
	camera.position = Vector2(21, 18) * TILE
	simulation_paused = false
	save_accumulator = 0.0
	last_totals = ""
	if model.save_game():
		_notice(_tr("Neue Fabrik erstellt", "New factory created"))
	else:
		_notice(_tr("Neue Fabrik: Speichern fehlgeschlagen", "New factory: save failed"))
	_show_menu(false)

func _show_menu(show: bool) -> void:
	menu_open = show
	motion_view.menu_open = show
	menu_mask.visible = show
	if show:
		confirming_new = false
		new_button.text = _tr("NEUE FABRIK STARTEN", "START NEW FACTORY")
		menu_card.modulate.a = 0.0 if not reduced_motion else 1.0
		if not reduced_motion:
			var pop := create_tween()
			pop.tween_property(menu_card, "modulate:a", 1.0, 0.20)
	else:
		motion_view.queue_redraw()

func _exit_to_casino() -> void:
	if not model.save_game():
		_notice(_tr("Speichern fehlgeschlagen · Zurück nicht möglich", "Save failed · cannot quit safely"))
		return
	get_tree().quit()

func _physics_process(delta: float) -> void:
	if not menu_open and not simulation_paused:
		model.advance(delta)
		save_accumulator += delta
		if save_accumulator >= 30.0:
			save_accumulator = 0.0
			model.save_game()
	ui_accumulator += delta
	if ui_accumulator >= 0.30:
		ui_accumulator = 0.0
		_refresh_ui()

func _process(delta: float) -> void:
	if menu_open:
		return
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
		camera.position += movement.normalized() * (740.0 * delta / camera.zoom.x)
		_bound_camera()
	var now := get_global_mouse_position()
	var next_hover := Vector2i(floori(now.x / TILE), floori(now.y / TILE))
	if now != Vector2.ZERO and hovered != next_hover:
		hovered = next_hover
		motion_view.hovered = next_hover
		motion_view.queue_redraw()

func _bound_camera() -> void:
	camera.position.x = clampf(camera.position.x, 0.0, float(Model.WIDTH) * TILE)
	camera.position.y = clampf(camera.position.y, 0.0, float(Model.HEIGHT) * TILE)

func _pointer_over_hud(point: Vector2) -> bool:
	var screen := get_viewport_rect().size
	return point.y < 116.0 or point.y > screen.y - 100.0 or (
		point.x < 300.0 and point.y < 615.0) or (
		point.x > screen.x - 362.0 and point.y < 478.0)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_ESCAPE:
			_show_menu(not menu_open)
			get_viewport().set_input_as_handled()
			return
		if menu_open:
			return
		if event.ctrl_pressed and event.keycode == KEY_S:
			_save_now()
		elif event.keycode == KEY_V:
			_set_mode("inspect")
		elif event.keycode == KEY_B:
			_set_mode("build")
		elif event.keycode >= KEY_1 and event.keycode <= KEY_4:
			_select_tool(TOOLS[event.keycode - KEY_1])
		elif event.keycode == KEY_R:
			if mode == "build":
				facing = (facing + 1) % 4
				motion_view.facing = facing
				_notice(_tr("Baurichtung: ", "Build direction: ") + DIR_TEXT[facing])
			else:
				_rotate_selected()
		elif event.keycode == KEY_SPACE:
			_toggle_pause()
		return
	if menu_open:
		return
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP and event.pressed:
			var before := get_global_mouse_position()
			camera.zoom = (camera.zoom * 1.12).clamp(Vector2(0.55, 0.55), Vector2(2.4, 2.4))
			camera.position += before - get_global_mouse_position()
			_bound_camera()
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN and event.pressed:
			var before := get_global_mouse_position()
			camera.zoom = (camera.zoom / 1.12).clamp(Vector2(0.55, 0.55), Vector2(2.4, 2.4))
			camera.position += before - get_global_mouse_position()
			_bound_camera()
			get_viewport().set_input_as_handled()
		elif event.button_index == MOUSE_BUTTON_MIDDLE:
			moving_camera = event.pressed
			get_viewport().set_input_as_handled()
		elif event.pressed and not _pointer_over_hud(event.position):
			if event.button_index == MOUSE_BUTTON_LEFT:
				if mode == "inspect":
					_inspect(hovered)
				elif model.place(hovered, tool, facing):
					static_view.queue_redraw()
					_notice(_tr("Gebäude platziert", "Building placed"))
				else:
					_notice(_tr("Bauplatz blockiert", "Building site blocked"))
				get_viewport().set_input_as_handled()
			elif event.button_index == MOUSE_BUTTON_RIGHT:
				if mode == "build":
					_set_mode("inspect")
				else:
					_inspect(Vector2i(-1, -1))
				get_viewport().set_input_as_handled()
	elif event is InputEventMouseMotion and moving_camera:
		camera.position -= event.relative / camera.zoom
		_bound_camera()
		get_viewport().set_input_as_handled()

func _notification(what: int) -> void:
	if what == NOTIFICATION_WM_CLOSE_REQUEST and model != null:
		if model.save_game():
			get_tree().quit()
		else:
			_notice(_tr("Speichern fehlgeschlagen", "Save failed"))

func _exit_tree() -> void:
	if model != null:
		model.save_game()
