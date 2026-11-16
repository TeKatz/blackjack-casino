extends Node2D
## Midnight Industries: isolated simulation, cached world, animated cargo and native UI.
const Model = preload("res://factory_model.gd")
const StaticView = preload("res://factory_static.gd")
const MotionView = preload("res://factory_motion.gd")
const UiSkin = preload("res://factory_skin.gd")
const Atlas = preload("res://factory_atlas.gd")
const TILE := 48.0
const GOLD := Color("#F5C04A")
const CYAN := Color("#00D8F4")
const INK := Color("#0C161D")
const TEXT := Color("#F0F3F3")
const MUTED := Color("#94ACB5")
const DANGER := Color("#FF725F")
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
var overview_label: Label
var inspector_portrait: TextureRect
var quick_buttons: Dictionary = {}
var icon_cache: Dictionary = {}
var ui_layer: CanvasLayer
var tool_buttons: Dictionary = {}
var mode_buttons: Dictionary = {}
var sidebar_panel: PanelContainer
var inspector_panel: PanelContainer
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
	# Avoid unlimited rendering load when the window is idle or on high-refresh monitors.
	Engine.max_fps = 60
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
	# Metallic platform and machines are separately cached in cullable
	# industrial 16x16 chunks. No full-map redraw per animation frame.
	static_view = StaticView.new()
	static_view.name = "IndustrialDeckChunks"
	static_view.model = model
	add_child(static_view)
	motion_view = MotionView.new()
	motion_view.name = "IndustrialActiveVFX"
	motion_view.model = model
	motion_view.camera = camera
	add_child(motion_view)

func _panel_style(tone: String = "panel") -> StyleBoxTexture:
	return UiSkin.panel(tone)

func _button_style(active: bool = false, destructive: bool = false) -> StyleBoxTexture:
	if destructive:
		return UiSkin.button("button_danger_hover" if active else "button_danger")
	return UiSkin.button("button_active" if active else "button")

func _make_label(value: String, size: int = 14, color: Color = TEXT) -> Label:
	var label := Label.new()
	label.text = value
	label.add_theme_font_override("font", UiSkin.font())
	label.add_theme_font_size_override("font_size", size)
	label.add_theme_color_override("font_color", color)
	label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.38))
	label.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return label

func _machine_icon(kind: String) -> Texture2D:
	if icon_cache.has(kind):
		return icon_cache[kind]
	var atlas: Texture2D = Atlas.get_texture()
	if atlas == null:
		return null
	var icon := AtlasTexture.new()
	icon.atlas = atlas
	icon.region = Atlas.region(int({"miner":0, "belt":1, "smelter":2, "storage":3}.get(kind, 0)))
	icon_cache[kind] = icon
	return icon

func _material_icon(index: int) -> Texture2D:
	var icon := AtlasTexture.new()
	icon.atlas = Atlas.get_texture()
	icon.region = Atlas.region(index, 1)
	return icon

func _button(value: String, handler: Callable, destructive: bool = false) -> Button:
	var b := Button.new()
	b.text = value
	b.focus_mode = Control.FOCUS_NONE
	b.custom_minimum_size.y = 45
	b.add_theme_font_override("font", UiSkin.font())
	b.add_theme_color_override("font_color", TEXT)
	b.add_theme_color_override("font_hover_color", GOLD if not destructive else DANGER)
	b.add_theme_color_override("font_pressed_color", GOLD)
	b.add_theme_color_override("font_disabled_color", Color("#71858A"))
	b.add_theme_font_size_override("font_size", 14)
	b.add_theme_stylebox_override("normal", _button_style(false, destructive))
	b.add_theme_stylebox_override("hover", _button_style(true, destructive))
	b.add_theme_stylebox_override("pressed", _button_style(true, destructive))
	b.add_theme_stylebox_override("disabled", _button_style(false, destructive))
	b.add_theme_stylebox_override("focus", StyleBoxEmpty.new())
	b.pressed.connect(handler)
	b.mouse_entered.connect(_button_hover.bind(b, true))
	b.mouse_exited.connect(_button_hover.bind(b, false))
	return b

func _button_hover(b: Button, entered: bool) -> void:
	if reduced_motion:
		return
	var tween := create_tween()
	tween.tween_property(b, "modulate", Color.WHITE if entered else Color("#DDE7EC"), 0.16)

func _section_title(text_value: String) -> VBoxContainer:
	var group := VBoxContainer.new()
	group.add_theme_constant_override("separation", 6)
	group.add_child(_make_label("▰  " + text_value, 12, GOLD))
	return group

func _create_interface() -> void:
	ui_layer = CanvasLayer.new()
	ui_layer.name = "IndustrialControlUI"
	ui_layer.layer = 8
	add_child(ui_layer)
	var hud := Control.new()
	hud.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	hud.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ui_layer.add_child(hud)

	# Top command rail: identity, real production receipts and system state.
	var top := PanelContainer.new()
	top.name = "BrushedSteelCommandRail"
	top.add_theme_stylebox_override("panel", _panel_style("header"))
	hud.add_child(top)
	top.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	top.offset_left = 11
	top.offset_right = -11
	top.offset_top = 9
	top.offset_bottom = 81
	var top_row := HBoxContainer.new()
	top_row.add_theme_constant_override("separation", 19)
	top.add_child(top_row)
	var brand := HBoxContainer.new()
	brand.add_theme_constant_override("separation", 11)
	brand.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	top_row.add_child(brand)
	var logo := TextureRect.new()
	logo.texture = _machine_icon("miner")
	logo.custom_minimum_size = Vector2(42, 42)
	logo.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	logo.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	brand.add_child(logo)
	var identity := VBoxContainer.new()
	identity.alignment = BoxContainer.ALIGNMENT_CENTER
	brand.add_child(identity)
	identity.add_child(_make_label("MIDNIGHT", 17, TEXT))
	identity.add_child(_make_label("I N D U S T R I E S", 14, GOLD))
	identity.add_child(_make_label(_tr("INDUSTRIELLE AUTOMATISIERUNG", "INDUSTRIAL AUTOMATION"), 9, MUTED))
	var resource_column := VBoxContainer.new()
	resource_column.alignment = BoxContainer.ALIGNMENT_CENTER
	resource_column.add_theme_constant_override("separation", 4)
	top_row.add_child(resource_column)
	production = _make_label("", 15, TEXT)
	production.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	resource_column.add_child(production)
	var status_row := HBoxContainer.new()
	status_row.alignment = BoxContainer.ALIGNMENT_END
	status_row.add_theme_constant_override("separation", 16)
	resource_column.add_child(status_row)
	header_status = _make_label("", 11, CYAN)
	fps_label = _make_label("", 11, MUTED)
	status_row.add_child(header_status)
	status_row.add_child(fps_label)
	var menu_button := _button(_tr("  MENÜ  [ESC]  ", "  MENU  [ESC]  "), func() -> void: _show_menu(true))
	menu_button.custom_minimum_size.x = 109
	menu_button.custom_minimum_size.y = 39
	top_row.add_child(menu_button)

	# Production catalog deliberately replaces the previous debug-style tool list.
	var sidebar := PanelContainer.new()
	sidebar.name = "IndustrialBuildCatalog"
	sidebar_panel = sidebar
	sidebar.add_theme_stylebox_override("panel", _panel_style("panel"))
	hud.add_child(sidebar)
	sidebar.set_anchors_and_offsets_preset(Control.PRESET_TOP_LEFT)
	sidebar.offset_left = 11
	sidebar.offset_top = 160
	sidebar.offset_right = 277
	sidebar.offset_bottom = 578
	var side_col := VBoxContainer.new()
	side_col.add_theme_constant_override("separation", 6)
	sidebar.add_child(side_col)
	side_col.add_child(_make_label(_tr("WERKZEUGZENTRALE", "FACTORY WORKSHOP"), 15, TEXT))
	side_col.add_child(_make_label(_tr("PRODUKTION  /  LOGISTIK", "PRODUCTION  /  LOGISTICS"), 10, CYAN))
	var mode_strip := PanelContainer.new()
	mode_strip.name = "CompactBuildSelectSwitcher"
	mode_strip.add_theme_stylebox_override("panel", _panel_style("header"))
	hud.add_child(mode_strip)
	mode_strip.set_anchors_preset(Control.PRESET_TOP_LEFT)
	mode_strip.offset_left = 11
	mode_strip.offset_top = 87
	mode_strip.offset_right = 277
	mode_strip.offset_bottom = 153
	var tabs := HBoxContainer.new()
	tabs.add_theme_constant_override("separation", 7)
	mode_strip.add_child(tabs)
	var inspect_btn := _button(_tr("V  AUSWAHL", "V  SELECT"), func() -> void: _set_mode("inspect"))
	var build_btn := _button(_tr("B  BAUEN", "B  BUILD"), func() -> void: _set_mode("build"))
	inspect_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	build_btn.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inspect_btn.add_theme_font_size_override("font_size", 12)
	build_btn.add_theme_font_size_override("font_size", 12)
	tabs.add_child(inspect_btn)
	tabs.add_child(build_btn)
	mode_buttons = {"inspect": inspect_btn, "build": build_btn}
	var ruler := HSeparator.new()
	side_col.add_child(ruler)
	side_col.add_child(_make_label(_tr("MASCHINENKATALOG", "MACHINE CATALOG"), 11, GOLD))
	tool_title = _make_label(_tr("VIER ANLAGEN VERFÜGBAR", "FOUR SYSTEMS AVAILABLE"), 10, MUTED)
	side_col.add_child(tool_title)
	var names: Array[String] = [
		_tr("ABBAUMASCHINE", "MINING DRILL"),
		_tr("FÖRDERBAND", "CONVEYOR BELT"),
		_tr("SCHMELZOFEN", "SMELTING FURNACE"),
		_tr("INDUSTRIELAGER", "INDUSTRIAL STORAGE")
	]
	for i in range(TOOLS.size()):
		var key: String = TOOLS[i]
		if i == 2:
			side_col.add_child(_make_label(_tr("VERARBEITUNG  /  LAGER", "PROCESSING  /  STORAGE"), 10, GOLD))
		var btn := _button("%d   %s" % [i + 1, names[i]], _select_tool.bind(key))
		btn.icon = _machine_icon(key)
		btn.expand_icon = true
		btn.alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.icon_alignment = HORIZONTAL_ALIGNMENT_LEFT
		btn.custom_minimum_size.y = 55
		side_col.add_child(btn)
		tool_buttons[key] = btn
	side_col.add_child(HSeparator.new())
	var hint := _make_label(_tr("R   AUSGANG DREHEN\nRECHTSKLICK   BAU ABBRECHEN",
		"R   ROTATE OUTPUT\nRIGHT CLICK   CANCEL BUILD"), 11, MUTED)
	side_col.add_child(hint)

	# Inspector uses actual selection data and an illustrated machine portrait.
	var inspector := PanelContainer.new()
	inspector.name = "IndustrialMachineConsole"
	inspector_panel = inspector
	inspector.add_theme_stylebox_override("panel", _panel_style("inspector"))
	hud.add_child(inspector)
	inspector.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	inspector.offset_left = -325
	inspector.offset_right = -11
	inspector.offset_top = 87
	inspector.offset_bottom = 413
	var inspect_col := VBoxContainer.new()
	inspect_col.add_theme_constant_override("separation", 12)
	inspector.add_child(inspect_col)
	inspect_col.add_child(_make_label(_tr("ANLAGENÜBERWACHUNG", "MACHINE TELEMETRY"), 12, GOLD))
	var portrait_row := HBoxContainer.new()
	portrait_row.add_theme_constant_override("separation", 8)
	inspect_col.add_child(portrait_row)
	inspector_portrait = TextureRect.new()
	inspector_portrait.custom_minimum_size = Vector2(54, 54)
	inspector_portrait.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	inspector_portrait.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	portrait_row.add_child(inspector_portrait)
	var title_col := VBoxContainer.new()
	title_col.alignment = BoxContainer.ALIGNMENT_CENTER
	title_col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	portrait_row.add_child(title_col)
	inspector_title = _make_label("", 16, TEXT)
	title_col.add_child(inspector_title)
	inspector_subtitle = _make_label("", 12, CYAN)
	inspector_subtitle.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	title_col.add_child(inspector_subtitle)
	inspect_col.add_child(HSeparator.new())
	inspect_col.add_child(_make_label(_tr("PROZESSDATEN / STATUS", "PROCESS DATA / STATUS"), 11, MUTED))
	inspector_body = _make_label("", 13, TEXT)
	inspector_body.custom_minimum_size.y = 79
	inspector_body.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	inspect_col.add_child(inspector_body)
	inspector_progress = ProgressBar.new()
	inspector_progress.custom_minimum_size.y = 8
	inspector_progress.show_percentage = false
	inspector_progress.min_value = 0.0
	inspector_progress.max_value = 100.0
	inspector_progress.add_theme_stylebox_override("fill", UiSkin.progress_fill())
	inspector_progress.add_theme_stylebox_override("background", UiSkin.progress_back())
	inspect_col.add_child(inspector_progress)
	inspector_controls = HBoxContainer.new()
	inspector_controls.add_theme_constant_override("separation", 8)
	inspect_col.add_child(inspector_controls)
	rotate_button = _button(_tr("↻ DREHEN", "↻ ROTATE"), _rotate_selected)
	demolish_button = _button(_tr("× ABREISSEN", "× DEMOLISH"), _demolish_selected, true)
	rotate_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	demolish_button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inspector_controls.add_child(rotate_button)
	inspector_controls.add_child(demolish_button)

	var overview := PanelContainer.new()
	overview.name = "SectorProductionReadout"
	overview.add_theme_stylebox_override("panel", _panel_style("panel"))
	hud.add_child(overview)
	overview.set_anchors_preset(Control.PRESET_TOP_RIGHT)
	overview.offset_left = -325
	overview.offset_right = -11
	overview.offset_top = 425
	overview.offset_bottom = 519
	var summary_col := VBoxContainer.new()
	summary_col.add_theme_constant_override("separation", 10)
	overview.add_child(summary_col)
	summary_col.add_child(_make_label(_tr("SEKTOR-ÜBERSICHT", "SECTOR OVERVIEW"), 12, GOLD))
	overview_label = _make_label("", 14, TEXT)
	summary_col.add_child(overview_label)

	var bottom := PanelContainer.new()
	bottom.name = "IndustrialQuickAccessRail"
	bottom.add_theme_stylebox_override("panel", _panel_style("header"))
	hud.add_child(bottom)
	bottom.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	bottom.offset_left = 11
	bottom.offset_right = -11
	bottom.offset_top = -76
	bottom.offset_bottom = -10
	var footer_row := HBoxContainer.new()
	footer_row.add_theme_constant_override("separation", 12)
	bottom.add_child(footer_row)
	var left_footer := VBoxContainer.new()
	left_footer.alignment = BoxContainer.ALIGNMENT_CENTER
	left_footer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	left_footer.add_theme_constant_override("separation", 4)
	footer_row.add_child(left_footer)
	status = _make_label("", 12, CYAN)
	left_footer.add_child(status)
	left_footer.add_child(_make_label(_tr("WASD BEWEGEN   •   MAUSRAD ZOOM   •   MITTELKLICK ZIEHEN", "WASD PAN   •   WHEEL ZOOM   •   MIDDLE DRAG"), 9, MUTED))
	var slots := HBoxContainer.new()
	slots.add_theme_constant_override("separation", 5)
	footer_row.add_child(slots)
	for i in range(TOOLS.size()):
		var name: String = TOOLS[i]
		var shortcut := _button(str(i + 1), _select_tool.bind(name))
		shortcut.icon = _machine_icon(name)
		shortcut.expand_icon = true
		shortcut.custom_minimum_size = Vector2(49, 44)
		shortcut.add_theme_font_size_override("font_size", 11)
		slots.add_child(shortcut)
		quick_buttons[name] = shortcut
	pause_button = _button(_tr("Ⅱ  PAUSE", "Ⅱ  PAUSE"), _toggle_pause)
	pause_button.custom_minimum_size.x = 92
	footer_row.add_child(pause_button)
	var save_btn := _button(_tr("SPEICHERN", "SAVE"), _save_now)
	save_btn.custom_minimum_size.x = 84
	footer_row.add_child(save_btn)

	# Full-screen modal main menu. No hidden destructive Escape quit.
	menu_mask = ColorRect.new()
	menu_mask.name = "IndustrialMainMenuBackdrop"
	menu_mask.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	menu_mask.color = Color("#071118", 0.94)
	menu_mask.mouse_filter = Control.MOUSE_FILTER_STOP
	ui_layer.add_child(menu_mask)
	var frame := CenterContainer.new()
	frame.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	menu_mask.add_child(frame)
	menu_card = PanelContainer.new()
	menu_card.name = "MidnightIndustriesCommandMenu"
	menu_card.custom_minimum_size = Vector2(508, 473)
	menu_card.add_theme_stylebox_override("panel", _panel_style("menu"))
	frame.add_child(menu_card)
	var menu_col := VBoxContainer.new()
	menu_col.add_theme_constant_override("separation", 12)
	menu_card.add_child(menu_col)
	var menu_logo := HBoxContainer.new()
	menu_logo.alignment = BoxContainer.ALIGNMENT_CENTER
	menu_col.add_child(menu_logo)
	var emblem := TextureRect.new()
	emblem.texture = _machine_icon("smelter")
	emblem.custom_minimum_size = Vector2(62, 62)
	emblem.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	emblem.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	menu_logo.add_child(emblem)
	var names_col := VBoxContainer.new()
	names_col.alignment = BoxContainer.ALIGNMENT_CENTER
	menu_logo.add_child(names_col)
	names_col.add_child(_make_label("MIDNIGHT", 25, TEXT))
	names_col.add_child(_make_label("I N D U S T R I E S", 16, GOLD))
	var subtitle := _make_label(_tr("AUTOMATION  ·  INDUSTRIELLE KONSTRUKTION", "AUTOMATION  ·  INDUSTRIAL ENGINEERING"), 12, MUTED)
	subtitle.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	menu_col.add_child(subtitle)
	menu_col.add_child(HSeparator.new())
	resume_button = _button(_tr("▶   FABRIK FORTSETZEN", "▶   RESUME FACTORY"), func() -> void: _show_menu(false))
	resume_button.custom_minimum_size.y = 47
	menu_col.add_child(resume_button)
	menu_col.add_child(_button(_tr("▣   SPIELSTAND SPEICHERN", "▣   SAVE PROGRESS"), _save_now))
	effects_button = _button(_tr("✦   ANIMATIONEN: AN", "✦   ANIMATIONS: ON"), _toggle_effects)
	menu_col.add_child(effects_button)
	new_button = _button(_tr("＋   NEUE FABRIK STARTEN", "＋   START NEW FACTORY"), _new_factory, true)
	menu_col.add_child(new_button)
	menu_col.add_child(HSeparator.new())
	var exit_btn := _button(_tr("↩   SPEICHERN & ZUM CASINO", "↩   SAVE & RETURN TO CASINO"), _exit_to_casino)
	exit_btn.custom_minimum_size.y = 45
	menu_col.add_child(exit_btn)
	var menu_note := _make_label(_tr("ESC  MENÜ SCHLIESSEN     •     DEINE FABRIK WIRD AUTO-GESPEICHERT",
		"ESC  CLOSE MENU     •     YOUR FACTORY AUTOSAVES"), 10, MUTED)
	menu_note.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	menu_col.add_child(menu_note)

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
	sidebar_panel.visible = mode == "build"
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
	for key in quick_buttons:
		quick_buttons[key].add_theme_stylebox_override("normal", _button_style(key == tool and mode == "build"))
	tool_title.text = _tr("WERKZEUGE BEREIT", "TOOLS READY") if mode == "build" else _tr("AUSWAHLMODUS AKTIV", "INSPECTION ACTIVE")
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
	var counts: Dictionary = model.counts()
	if overview_label != null:
		overview_label.text = "%s  %d     %s  %d\n%s  %d     %s  %d" % [
			_tr("MINER", "DRILLS"), counts.miner, _tr("BÄNDER", "BELTS"), counts.belt,
			_tr("ÖFEN", "FURNACES"), counts.smelter, _tr("LAGER", "STORAGE"), counts.storage]
	var pause_state := _tr("PAUSIERT", "PAUSED") if simulation_paused else _tr("PRODUKTION AKTIV", "PRODUCTION LIVE")
	header_status.text = "●  %s   ·   %d %s    " % [pause_state, unit_count, _tr("Gebäude", "buildings")]
	fps_label.text = "FPS  %d" % Engine.get_frames_per_second()
	pause_button.text = _tr("▶  WEITER", "▶  RESUME") if simulation_paused else _tr("Ⅱ  PAUSE", "Ⅱ  PAUSE")
	_refresh_inspector()

func _refresh_inspector() -> void:
	if inspector_title == null:
		return
	if not model.buildings.has(inspected):
		inspector_panel.visible = false
		inspector_portrait.visible = false
		inspector_title.text = _tr("Kein Gebäude gewählt", "No building selected")
		inspector_subtitle.text = _tr("AUSWAHLMODUS  [V]", "INSPECT MODE  [V]")
		inspector_body.text = _tr("Klicke einen Miner, ein Förderband, einen Ofen oder ein Lager. Hier erscheinen Zustand, Inventar und Produktionswerte.",
			"Click a drill, conveyor, furnace or storage unit to see status, inventory and throughput.")
		inspector_progress.visible = false
		inspector_controls.visible = false
		return
	var machine: Dictionary = model.buildings[inspected]
	var kind: String = machine.kind
	inspector_panel.visible = true
	inspector_portrait.visible = true
	inspector_portrait.texture = _machine_icon(kind)
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
		static_view.refresh_cell(inspected)
		_refresh_inspector()
		_notice(_tr("Ausgang gedreht", "Output rotated"))

func _demolish_selected() -> void:
	if model.erase(inspected):
		static_view.refresh_cell(inspected)
		_inspect(Vector2i(-1, -1))
		_notice(_tr("Gebäude demontiert · Inhalt verworfen", "Building demolished · items discarded"))

func _toggle_pause() -> void:
	simulation_paused = not simulation_paused
	motion_view.simulation_paused = simulation_paused
	_notice(_tr("Produktion angehalten", "Production paused") if simulation_paused else
		_tr("Produktion läuft", "Production running"))
	_refresh_ui()

func _save_now() -> void:
	_notice(_tr("Spielstand gespeichert", "Factory saved") if model.save_game() else
		_tr("Speichern fehlgeschlagen", "Save failed"))

func _toggle_effects() -> void:
	reduced_motion = not reduced_motion
	motion_view.reduced_motion = reduced_motion
	effects_button.text = _tr("✦   ANIMATIONEN: AUS", "✦   ANIMATIONS: OFF") if reduced_motion else _tr("✦   ANIMATIONEN: AN", "✦   ANIMATIONS: ON")

func _new_factory() -> void:
	if not confirming_new:
		confirming_new = true
		new_button.text = _tr("!   WIRKLICH NEU STARTEN?", "!   CONFIRM NEW FACTORY?")
		return
	confirming_new = false
	new_button.text = _tr("＋   NEUE FABRIK STARTEN", "＋   START NEW FACTORY")
	model = Model.new()
	static_view.model = model
	motion_view.model = model
	static_view.refresh_all()
	inspected = Vector2i(-1, -1)
	motion_view.inspected = inspected
	camera.position = Vector2(21, 18) * TILE
	simulation_paused = false
	motion_view.simulation_paused = false
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
	return point.y < 84.0 or point.y > screen.y - 80.0 or (
		point.x < 282.0 and (point.y < 157.0 or (sidebar_panel.visible and point.y < 583.0))) or (
		point.x > screen.x - 329.0 and ((inspector_panel.visible and point.y < 418.0) or
		(point.y > 420.0 and point.y < 525.0)))

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
					static_view.refresh_cell(hovered)
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
