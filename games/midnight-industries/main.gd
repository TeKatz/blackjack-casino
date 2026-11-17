extends Node2D
## Midnight Industries: isolated simulation, cached world, animated cargo and native UI.
const Model = preload("res://factory_model.gd")
const StaticView = preload("res://factory_static.gd")
const TerrainView = preload("res://factory_terrain.gd")
const GridView = preload("res://factory_grid.gd")
const MinimapView = preload("res://factory_minimap.gd")
const ConstructionView = preload("res://factory_construction_view.gd")
const Construction = preload("res://factory_construction.gd")
const MotionView = preload("res://factory_motion.gd")
const UiSkin = preload("res://factory_skin.gd")
const Atlas = preload("res://factory_atlas.gd")
const DepotArt = preload("res://factory_depot_art.gd")
const StorageView = preload("res://factory_storage_view.gd")
const WorldManager = preload("res://world_manager.gd")
const Settings = preload("res://industries_settings.gd")
const Navigation = preload("res://factory_navigation.gd")
const MenuSkin = preload("res://factory_menu_theme.gd")
const WindowChrome = preload("res://factory_window_chrome.gd")
const BuildDock = preload("res://factory_build_dock.gd")
const Objectives = preload("res://factory_objectives_drawer.gd")
const CommandIcons = preload("res://factory_ui_icons.gd")
const PowerArt = preload("res://factory_power_art.gd")
const JobsView = preload("res://factory_jobs_view.gd")
const TILE := 48.0
const GOLD := Color("#F5C04A")
const CYAN := Color("#00D8F4")
const INK := Color("#0C161D")
const TEXT := Color("#F0F3F3")
const MUTED := Color("#94ACB5")
const DANGER := Color("#FF725F")
const TOOLS := ["miner", "belt", "smelter", "storage", "depot", "power_pole", "water_wheel"]
const DIR_TEXT := ["→", "↓", "←", "↑"]

@onready var camera: Camera2D = $Camera
var model
var terrain_view: Node2D
var static_view: Node2D
var motion_view: Node2D
var grid_view: Node2D
var minimap: Control
var storage_view: Control
var jobs_view: Control
var construction_view: Node2D
var show_grid := false
const VISUAL_SETTINGS_FILE := "user://industries-visual-settings.cfg"
var mode := "inspect"
var tool := "belt"
var facing := 0
var hovered := Vector2i(-1, -1)
var inspected := Vector2i(-1, -1)
var german := true
var moving_camera := false
const SELECT_DRAG_THRESHOLD := 7.0
var selection_pending := false
var selection_dragging := false
var selection_press_screen := Vector2.ZERO
var selection_press_cell := Vector2i(-1, -1)
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
var construction_status: Label
var status: Label
var tool_title: Label
var tool_cost_label: Label
var grid_button: Button
var inspector_portrait: TextureRect
var quick_buttons: Dictionary = {}
var icon_cache: Dictionary = {}
var ui_layer: CanvasLayer
var tool_buttons: Dictionary = {}
var mode_buttons: Dictionary = {}
var sidebar_panel: PanelContainer
var inspector_panel: Panel
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
var worlds
var settings
var navigation: Control
var window_chrome: Control
var header_rail: Control
var status_tween: Tween
var header_compact := false
var build_dock: Control
var objectives_drawer: Control
var header_buttons: Dictionary = {}
var inspector_visible := false
var inspector_tween: Tween
var mode_rail: Control
var pause_world_label: Label
var pause_phase_label: Label
var current_world := ""
var played_since_save := 0.0
var saved_view: Dictionary = {}
var hover_tweens: Dictionary = {}

func _ready() -> void:
	settings = Settings.new()
	worlds = WorldManager.new()
	german = str(settings.values.language) != "en"
	if "--language=en" in OS.get_cmdline_user_args():
		german = false
	# No world is simulated/overwritten until the player explicitly chooses
	# Continue, Load or New World in the recovered main menu.
	model = Model.new(false)
	model.construction.phase_completed.connect(_on_construction_phase)
	model.progression.job_completed.connect(_on_job_completed)
	show_grid = bool(settings.values.grid)
	camera.position = Vector2(21, 18) * TILE
	camera.zoom = Vector2.ONE
	_create_world()
	_create_interface()
	_set_mode("inspect")
	_refresh_ui()
	menu_mask.hide()
	navigation.show_start()
	menu_open = true
	motion_view.menu_open = true
	apply_settings()
	get_viewport().size_changed.connect(_layout_hud)
	_layout_hud()
	get_tree().auto_accept_quit = false

func _tr(de: String, en: String) -> String:
	return de if german else en

func _create_world() -> void:
	# Seamless meadow and stone blend beneath cullable factory chunks.
	terrain_view = TerrainView.new()
	terrain_view.name = "MeadowAndOreBiome"
	terrain_view.model = model
	add_child(terrain_view)
	grid_view = GridView.new()
	grid_view.name = "OptionalPlacementGrid"
	grid_view.camera = camera
	add_child(grid_view)
	grid_view.set_grid_visible(show_grid)
	construction_view = ConstructionView.new()
	construction_view.name = "CasinoConstructionSite"
	construction_view.model = model
	add_child(construction_view)
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
	var index: int = int({"miner":0, "belt":1, "smelter":2, "storage":3}.get(kind, 0))
	var icon: Texture2D = PowerArt.texture(kind) if kind in ["power_pole", "water_wheel"] else (
		DepotArt.texture() if kind == "depot" else Atlas.sprite(index))
	icon_cache[kind] = icon
	return icon

func _material_icon(index: int) -> Texture2D:
	return Atlas.sprite(index, 1)

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
	if reduced_motion or not is_instance_valid(b):
		return
	if hover_tweens.has(b):
		var previous: Tween = hover_tweens[b]
		if previous != null and previous.is_running():
			previous.kill()
	var tween := create_tween()
	hover_tweens[b] = tween
	tween.tween_property(b, "modulate",
		Color.WHITE if entered else Color("#DDE7EC"), 0.13)

func _menu_button(value: String, handler: Callable, danger: bool = false,
	primary: bool = false) -> Button:
	var b := Button.new()
	b.text = value
	b.custom_minimum_size.y = 50
	MenuSkin.style_button(b, danger, primary)
	b.pressed.connect(handler)
	return b

func _header_action(parent: Node, id: String, label: String,
	handler: Callable) -> Button:
	var button := Button.new()
	button.name = "Header_" + id
	button.text = label
	button.icon = CommandIcons.icon(id)
	button.icon_alignment = HORIZONTAL_ALIGNMENT_LEFT
	button.custom_minimum_size = Vector2(95, 42)
	match id:
		"achievements":
			button.custom_minimum_size.x = 144
		"upgrades":
			button.custom_minimum_size.x = 127
		"storage":
			button.custom_minimum_size.x = 111
		"jobs":
			button.custom_minimum_size.x = 86
		"menu":
			button.custom_minimum_size.x = 91
	button.set_meta("full_label", label)
	MenuSkin.style_button(button)
	button.add_theme_font_size_override("font_size", 12)
	button.tooltip_text = label
	button.pressed.connect(handler)
	parent.add_child(button)
	header_buttons[id] = button
	return button

func _header_indicator(parent: HBoxContainer, accent: Color) -> Label:
	var plate := StyleBoxFlat.new()
	plate.bg_color = Color("#102431")
	plate.border_color = Color("#2F4D5A")
	plate.set_border_width_all(1)
	plate.set_corner_radius_all(7)
	plate.content_margin_left = 9
	plate.content_margin_right = 9
	plate.content_margin_top = 8
	plate.content_margin_bottom = 8
	var background := PanelContainer.new()
	background.add_theme_stylebox_override("panel", plate)
	parent.add_child(background)
	var label := _make_label("", 11, accent)
	background.add_child(label)
	return label

func _show_future_panel(id: String) -> void:
	if id == "jobs":
		_toggle_jobs()
		return
	var names: Dictionary = {
		"achievements": _tr("Erfolge", "Achievements"),
		"jobs": "Jobs", "upgrades": "Upgrades"}
	var dialog := AcceptDialog.new()
	dialog.title = str(names.get(id, id))
	dialog.dialog_text = _tr(
		"Dieser Bereich ist für eine kommende Entwicklungsphase vorgesehen.",
		"This feature is reserved for a future development phase.")
	dialog.confirmed.connect(dialog.queue_free)
	ui_layer.add_child(dialog)
	dialog.popup_centered()

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

	# Compact native command rail: factory identity, telemetry, and systems.
	var top := PanelContainer.new()
	header_rail = top
	top.name = "BrushedSteelCommandRail"
	top.add_theme_stylebox_override("panel", MenuSkin.panel("section"))
	hud.add_child(top)
	top.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	top.offset_left = 12
	top.offset_right = -12
	top.offset_top = 10
	top.offset_bottom = 88
	var top_row := HBoxContainer.new()
	top_row.alignment = BoxContainer.ALIGNMENT_CENTER
	top_row.add_theme_constant_override("separation", 13)
	top.add_child(top_row)
	var brand := HBoxContainer.new()
	brand.custom_minimum_size.x = 220
	brand.add_theme_constant_override("separation", 10)
	top_row.add_child(brand)
	var logo := TextureRect.new()
	logo.texture = _machine_icon("miner")
	logo.custom_minimum_size = Vector2(43, 43)
	logo.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	logo.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	brand.add_child(logo)
	var identity := VBoxContainer.new()
	identity.alignment = BoxContainer.ALIGNMENT_CENTER
	identity.add_theme_constant_override("separation", 2)
	brand.add_child(identity)
	identity.add_child(_make_label("MIDNIGHT", 18, TEXT))
	identity.add_child(_make_label("I N D U S T R I E S", 12, GOLD))

	# Compact at-a-glance indicators. Material stock belongs in Storage, not
	# in a cramped header ticker; these three labels never trigger scrolling.
	var status_row := HBoxContainer.new()
	status_row.name = "FactoryStatusIndicators"
	status_row.alignment = BoxContainer.ALIGNMENT_CENTER
	status_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status_row.add_theme_constant_override("separation", 10)
	top_row.add_child(status_row)
	header_status = _header_indicator(status_row, MenuSkin.CYAN)
	construction_status = _header_indicator(status_row, MenuSkin.AMBER)
	fps_label = _header_indicator(status_row, MenuSkin.MUTED)

	# All six actions always fit inside a plain HBoxContainer. No scrollbar,
	# scroll track or extra scrollbar strip can ever be drawn in the header.
	var action_row := HBoxContainer.new()
	action_row.name = "CommandActionsRow"
	action_row.add_theme_constant_override("separation", 6)
	top_row.add_child(action_row)
	_header_action(action_row, "achievements", _tr("ERFOLGE", "ACHIEVEMENTS"),
		func() -> void: _show_future_panel("achievements"))
	_header_action(action_row, "jobs", "JOBS", _toggle_jobs)
	_header_action(action_row, "upgrades", "UPGRADES",
		func() -> void: _show_future_panel("upgrades"))
	_header_action(action_row, "storage", _tr("LAGER", "STORAGE"), _toggle_storage)
	pause_button = _header_action(action_row, "pause", "PAUSE", _toggle_pause)
	_header_action(action_row, "menu", _tr("MENÜ", "MENU"),
		func() -> void: _show_menu(true))

	# Inspector uses actual selection data and an illustrated machine portrait.
	var inspector := Panel.new()
	inspector.name = "IndustrialMachineConsole"
	inspector_panel = inspector
	inspector.add_theme_stylebox_override("panel", MenuSkin.panel("main"))
	hud.add_child(inspector)
	inspector.set_anchors_and_offsets_preset(Control.PRESET_TOP_RIGHT)
	inspector.set_anchor(SIDE_TOP, 0.0, false)
	inspector.set_anchor(SIDE_BOTTOM, 0.0, false)
	inspector.offset_left = -376
	inspector.offset_right = -16
	inspector.offset_top = 103
	inspector.offset_bottom = 438
	inspector.clip_contents = true
	inspector.visible = false
	# A native Panel doesn't grow to fit its children. The old PanelContainer
	# silently stretched to several thousand pixels tall on Windows/Godot.
	# A bounded inner ScrollContainer retains all telemetry and controls,
	# without allowing the inspector to cover the bottom build dock.
	var inspector_scroll := ScrollContainer.new()
	inspector_scroll.name = "InspectorContents"
	inspector_scroll.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	inspector_scroll.offset_left = 22
	inspector_scroll.offset_right = -22
	inspector_scroll.offset_top = 17
	inspector_scroll.offset_bottom = -16
	inspector_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	inspector_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	inspector.add_child(inspector_scroll)
	var inspect_col := VBoxContainer.new()
	inspect_col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	inspect_col.add_theme_constant_override("separation", 9)
	inspector_scroll.add_child(inspect_col)
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

	# Independent bottom category dock with expandable machine shelf.
	build_dock = BuildDock.new()
	build_dock.model = model
	build_dock.german = german
	build_dock.reduced_motion = reduced_motion
	build_dock.selected_machine = tool
	build_dock.icon_provider = Callable(self, "_machine_icon")
	build_dock.inspect_requested.connect(func() -> void: _set_mode("inspect"))
	build_dock.build_mode_requested.connect(func() -> void: _set_mode("build"))
	build_dock.machine_requested.connect(_select_tool)
	hud.add_child(build_dock)
	objectives_drawer = Objectives.new()
	objectives_drawer.german = german
	objectives_drawer.reduced_motion = reduced_motion
	objectives_drawer.position = Vector2(-292, 139)
	hud.add_child(objectives_drawer)
	# Transient notice text only. No permanent empty rectangle at bottom left.
	status = _make_label("", 12, MenuSkin.CYAN)
	status.name = "FactoryNotice"
	status.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_LEFT)
	status.offset_left = 21
	status.offset_right = 540
	status.offset_top = -225
	status.offset_bottom = -201
	status.clip_text = true
	status.mouse_filter = Control.MOUSE_FILTER_IGNORE
	status.visible = false
	hud.add_child(status)

	# Dedicated pause screen, unlike the rejected Phase-1 HUD. A compact,
	# legible control station with actual hierarchy and no random gold corner.
	menu_mask = ColorRect.new()
	menu_mask.name = "IndustrialMainMenuBackdrop"
	menu_mask.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	menu_mask.color = Color("#041019", 0.91)
	menu_mask.mouse_filter = Control.MOUSE_FILTER_STOP
	ui_layer.add_child(menu_mask)
	var frame := CenterContainer.new()
	frame.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	menu_mask.add_child(frame)
	menu_card = PanelContainer.new()
	menu_card.name = "MidnightIndustriesCommandMenu"
	menu_card.custom_minimum_size = Vector2(735, 545)
	menu_card.add_theme_stylebox_override("panel", MenuSkin.panel("main"))
	frame.add_child(menu_card)
	var menu_col := VBoxContainer.new()
	menu_col.add_theme_constant_override("separation", 15)
	menu_card.add_child(menu_col)
	var accent := ColorRect.new()
	accent.color = MenuSkin.CYAN
	accent.custom_minimum_size.y = 3
	accent.mouse_filter = Control.MOUSE_FILTER_IGNORE
	menu_col.add_child(accent)
	menu_col.add_child(_make_label("MIDNIGHT  /  INDUSTRIES", 13, MenuSkin.AMBER))
	menu_col.add_child(_make_label(_tr("PAUSIERT", "PAUSED"), 31, MenuSkin.TEXT))
	menu_col.add_child(_make_label(_tr(
		"Fabriksteuerung · Produktion während des Menüs angehalten",
		"Factory control · Production is suspended while this menu is open"),
		13, MenuSkin.MUTED))
	var divider := ColorRect.new()
	divider.color = Color("#507181", 0.70)
	divider.custom_minimum_size.y = 1
	menu_col.add_child(divider)
	var body := HBoxContainer.new()
	body.add_theme_constant_override("separation", 18)
	body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	menu_col.add_child(body)
	var display := PanelContainer.new()
	display.add_theme_stylebox_override("panel", MenuSkin.panel("section"))
	display.custom_minimum_size.x = 218
	body.add_child(display)
	var display_stack := VBoxContainer.new()
	display_stack.alignment = BoxContainer.ALIGNMENT_CENTER
	display_stack.add_theme_constant_override("separation", 9)
	display.add_child(display_stack)
	var image := TextureRect.new()
	image.texture = _machine_icon("smelter")
	image.custom_minimum_size = Vector2(124, 124)
	image.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	image.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	display_stack.add_child(image)
	pause_world_label = _make_label("", 14, MenuSkin.TEXT)
	pause_world_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	pause_world_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	display_stack.add_child(pause_world_label)
	pause_phase_label = _make_label("", 12, MenuSkin.AMBER)
	pause_phase_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	display_stack.add_child(pause_phase_label)
	var buttons := VBoxContainer.new()
	buttons.add_theme_constant_override("separation", 10)
	buttons.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	body.add_child(buttons)
	resume_button = _menu_button(_tr("▶  FABRIK FORTSETZEN", "▶  RESUME FACTORY"),
		func() -> void: _show_menu(false), false, true)
	buttons.add_child(resume_button)
	buttons.add_child(_menu_button(_tr("▣  SPIELSTAND SPEICHERN", "▣  SAVE WORLD"), _save_now))
	buttons.add_child(_menu_button(_tr("⚙  EINSTELLUNGEN", "⚙  SETTINGS"),
		func() -> void: navigation.open_settings()))
	buttons.add_child(_menu_button(_tr("▤  WELTEN & SPIELSTÄNDE", "▤  WORLDS & SAVES"),
		return_to_main))
	buttons.add_child(_menu_button(_tr("↩  SPEICHERN & ZUM CASINO",
		"↩  SAVE & RETURN TO CASINO"), _exit_to_casino))
	var bottom_line := ColorRect.new()
	bottom_line.color = Color("#507181", 0.45)
	bottom_line.custom_minimum_size.y = 1
	menu_col.add_child(bottom_line)
	var pause_hint := _make_label(_tr("ESC  FORTSETZEN  ·  AUTOSAVE AKTIV",
		"ESC  RESUME  ·  AUTOSAVE ENABLED"), 12, MenuSkin.MUTED)
	pause_hint.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	menu_col.add_child(pause_hint)

	# Full-screen cartographic map is placed above the main menu and world.
	# It remains hidden until M is pressed during gameplay.
	minimap = MinimapView.new()
	minimap.name = "WorldOverviewMap"
	minimap.model = model
	minimap.camera = camera
	minimap.german = german
	ui_layer.add_child(minimap)
	storage_view = StorageView.new()
	storage_view.name = "GlobalConstructionInventory"
	storage_view.model = model
	storage_view.german = german
	ui_layer.add_child(storage_view)
	jobs_view = JobsView.new()
	jobs_view.model = model
	jobs_view.german = german
	ui_layer.add_child(jobs_view)
	navigation = Navigation.new()
	navigation.name = "WorldSaveSettingsNavigation"
	navigation.game = self
	navigation.manager = worlds
	navigation.settings = settings
	ui_layer.add_child(navigation)
	window_chrome = WindowChrome.new()
	window_chrome.game = self
	ui_layer.add_child(window_chrome)

func _notice(message: String) -> void:
	if status == null:
		return
	if status_tween != null and status_tween.is_running():
		status_tween.kill()
	status.text = message
	status.visible = true
	status.modulate.a = 1.0
	# The game doesn't reserve a permanent HUD box for notifications; the
	# text vanishes after a short time, with the last message taking priority.
	status_tween = create_tween()
	status_tween.tween_interval(2.8)
	status_tween.tween_property(status, "modulate:a", 0.0,
		0.0 if reduced_motion else 0.35)
	status_tween.tween_callback(func() -> void:
		if is_instance_valid(status):
			status.visible = false)

func _cost_text(_kind: String) -> String:
	return _tr("KOSTENLOS", "FREE")

func _missing_text(_kind: String) -> String:
	return ""

func _toggle_jobs() -> void:
	if current_world.is_empty() or menu_open:
		return
	if jobs_view.visible:
		jobs_view.hide_jobs()
		return
	storage_view.close_inventory()
	minimap.hide_map()
	_cancel_select_drag()
	moving_camera = false
	jobs_view.show_jobs()

func _on_job_completed(id: String) -> void:
	if build_dock != null:
		build_dock._build_machines()
		build_dock.refresh()
	if jobs_view != null:
		jobs_view.refresh()
	if id == "first_iron_delivery":
		_notice(_tr("Job 001 abgeschlossen: Wasserrad, Strommast und Schmelzofen freigeschaltet! +1 Stromslot",
			"Job 001 complete: Water wheel, power pole and furnace unlocked! +1 power slot"))
	_refresh_ui()


func _set_mode(next_mode: String) -> void:
	_cancel_select_drag()
	mode = next_mode
	motion_view.cursor_mode = mode
	if build_dock != null:
		if mode == "inspect":
			build_dock.set_inspecting()
		else:
			build_dock.set_building()
	if mode == "inspect":
		notice_selection()
	else:
		_notice(_tr("Baumodus aktiv · Richtung in der Vorschau sichtbar",
			"Build mode active · output direction visible in preview"))
	motion_view.queue_redraw()
	_refresh_ui()

func _select_tool(name: String) -> void:
	if not model.is_unlocked(name):
		return
	tool = name
	if build_dock != null:
		build_dock.select_machine(name)
	_set_mode("build")
	motion_view.tool = tool
	_notice(_tr("Ausgewählt: ", "Selected: ") + name.to_upper() +
		" · " + _cost_text(name))

func notice_selection() -> void:
	if model.buildings.has(inspected):
		_notice(_tr("Maschine ausgewählt · Details rechts", "Machine selected · details on right"))
	else:
		_notice(_tr("Auswahlmodus · Klicke eine Maschine", "Inspect mode · click a machine"))

func _inspect(cell: Vector2i) -> void:
	inspected = cell if model.buildings.has(cell) else Vector2i(-1, -1)
	motion_view.inspected = inspected
	_refresh_inspector()
	if model.construction.inside(cell) and not model.buildings.has(cell):
		_notice(_tr("Casino-Bauplatz · Phase %02d/12 · Anforderungen folgen",
			"Casino construction · Phase %02d/12 · requirements pending")
			% model.construction.phase_number())
	else:
		notice_selection()

func _refresh_ui() -> void:
	if model == null or header_status == null:
		return
	model._update_power()
	var unit_count: int = model.buildings.size()
	var pause_state := _tr("STOPP", "PAUSED") if simulation_paused else _tr("AKTIV", "LIVE")
	header_status.text = "● %s  ·  %d" % [pause_state, unit_count]
	var active_phase: int = model.construction.phase_number()
	construction_status.text = _tr("CASINO %02d/12", "CASINO %02d/12") % active_phase
	if model.construction.completed_phases >= Construction.PHASE_COUNT:
		construction_status.text = _tr("CASINO FERTIG", "CASINO COMPLETE")
	fps_label.text = "%d FPS" % Engine.get_frames_per_second()
	pause_button.text = "" if header_compact else (
		_tr("WEITER", "RESUME") if simulation_paused else "PAUSE")
	pause_button.icon = CommandIcons.icon("play" if simulation_paused else "pause")
	if build_dock != null:
		build_dock.refresh()
	if objectives_drawer != null:
		objectives_drawer.set_phase(active_phase)
		objectives_drawer.set_progression(model)
	if tool_cost_label != null and mode == "build":
		tool_cost_label.text = _tr("BAUKOSTEN: ", "BUILD COST: ") + _cost_text(tool)
		tool_cost_label.add_theme_color_override("font_color",
			CYAN if model.economy.has_funds(tool) else DANGER)
	_refresh_inspector()

func _animate_inspector(show: bool) -> void:
	if inspector_panel == null:
		return
	if inspector_visible == show:
		return
	inspector_visible = show
	if inspector_tween != null and inspector_tween.is_running():
		inspector_tween.kill()
	var target_x := get_viewport_rect().size.x - 376.0
	if show:
		inspector_panel.visible = true
		inspector_panel.position.x = target_x + 28.0
		inspector_panel.modulate.a = 0.0
	if reduced_motion:
		inspector_panel.position.x = target_x if show else target_x + 28.0
		inspector_panel.modulate.a = 1.0 if show else 0.0
		inspector_panel.visible = show
		return
	inspector_tween = create_tween().set_parallel(true)
	inspector_tween.tween_property(inspector_panel, "position:x",
		target_x if show else target_x + 28.0, 0.21).set_trans(
		Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	inspector_tween.tween_property(inspector_panel, "modulate:a",
		1.0 if show else 0.0, 0.18)
	if not show:
		inspector_tween.chain().tween_callback(func() -> void:
			if not inspector_visible:
				inspector_panel.visible = false)

func _refresh_inspector() -> void:
	if inspector_title == null:
		return
	if not model.buildings.has(inspected):
		_animate_inspector(false)
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
	_animate_inspector(true)
	inspector_portrait.visible = true
	inspector_portrait.texture = _machine_icon(kind)
	inspector_progress.visible = true
	inspector_controls.visible = true
	rotate_button.disabled = kind in ["depot", "power_pole", "water_wheel"]
	var label_name := {"miner": _tr("Abbaumaschine", "Mining drill"),
		"belt": _tr("Förderband", "Conveyor belt"),
		"smelter": _tr("Schmelzofen", "Smelting furnace"),
		"storage": _tr("Zwischenlager", "Warehouse"),
		"depot": _tr("Globales Depot", "Global depot"),
		"power_pole": _tr("Strommast", "Power pole"),
		"water_wheel": _tr("Wasserrad", "Water wheel")}
	inspector_title.text = str(label_name.get(kind, kind.capitalize()))
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
			inspector_body.text = (_tr("Strom: ", "Power: ") + (
				"10 kW · " + (_tr("AKTIV", "ON") if model.power.is_powered(inspected) else
				_tr("KEINE VERSORGUNG", "NO SUPPLY"))) + "\n" +
				_tr("Rezept: ", "Recipe: ") + str(machine.recipe).to_upper() + "\n" +
				_tr("Erz: %d/8    Produkt: %d/8", "Ore: %d/8    Output: %d/8") % [machine.ore, machine.output] + "\n" +
				_tr("Tempo: 1 Barren / 2 Sekunden", "Rate: 1 ingot / 2 seconds"))
			inspector_progress.value = clampf(100.0 * float(machine.progress) / 2.0, 0.0, 100.0)
		"storage":
			var inventory: Dictionary = machine.get("inventory", {})
			var used := 0
			for quantity in inventory.values():
				used += int(quantity)
			var listing := []
			for material in inventory:
				if int(inventory[material]) > 0:
					listing.append("%s %d" % [str(material), int(inventory[material])])
			inspector_body.text = _tr("Lokales Zwischenlager", "Local warehouse buffer") + \
				"\n%d/%d   ·   %s" % [used, Model.WAREHOUSE_CAPACITY, (
				", ".join(listing) if not listing.is_empty() else _tr("Leer", "Empty"))] + \
				"\n" + _tr("Gibt Waren über den Ausgang weiter", "Forwards goods via its output")
			inspector_progress.value = 100.0 * float(used) / float(Model.WAREHOUSE_CAPACITY)
		"depot":
			inspector_body.text = _tr("Alle eingehenden Materialien", "All arriving materials") + \
				"\n" + _tr("werden dem Baulager gutgeschrieben.", "credit the global build inventory.") + \
				"\nFe %d    Cu %d    Au %d" % [
					model.economy.available("iron_ingot"),
					model.economy.available("copper_ingot"),
					model.economy.available("gold_ingot")]
			inspector_progress.value = 100.0
		"power_pole":
			var attached: bool = model.power.network_for.has(inspected)
			inspector_body.text = _tr("Stromverteilung · Reichweite 5 Felder",
				"Power distribution · range 5 tiles") + "\n" + _tr(
				"Verbindet Generatoren und Verbraucher in Reichweite.",
				"Connects generators and machines in range.") + "\n" + (
				_tr("Netzknoten aktiv", "Node online") if attached else
				_tr("Nicht angeschlossen", "Not connected"))
			inspector_progress.visible = false
		"water_wheel":
			inspector_body.text = _tr("Wasserkraft · 30 kW", "Hydropower · 30 kW") + "\n" + (
				_tr("Stromslots: ", "Power slots: ") + "1") + "\n" + _tr(
				"Nur an Flussufern · Stromnetz mit Masten erweitern.",
				"River banks only · extend the grid with power poles.")
			inspector_progress.visible = false

func _rotate_selected() -> void:
	if model.rotate(inspected):
		static_view.refresh_cell(inspected)
		_refresh_inspector()
		_notice(_tr("Ausgang gedreht", "Output rotated"))

func _demolish_selected() -> void:
	if model.erase(inspected):
		static_view.refresh_cell(inspected)
		_inspect(Vector2i(-1, -1))
		_notice(_tr("Gebäude entfernt · kostenloser Neubau jederzeit möglich",
			"Building removed · rebuild for free anytime"))
		_refresh_ui()

func _toggle_pause() -> void:
	simulation_paused = not simulation_paused
	motion_view.simulation_paused = simulation_paused
	_notice(_tr("Produktion angehalten", "Production paused") if simulation_paused else
		_tr("Produktion läuft", "Production running"))
	_refresh_ui()

func _save_now() -> void:
	_notice(_tr("Spielstand gespeichert", "Factory saved") if _save_world() else
		_tr("Speichern fehlgeschlagen", "Save failed"))

func _toggle_effects() -> void:
	settings.values.reduced_motion = not bool(settings.values.reduced_motion)
	settings.save()
	apply_settings("reduced_motion")

func _load_visual_settings() -> void:
	var settings := ConfigFile.new()
	if settings.load(VISUAL_SETTINGS_FILE) == OK:
		show_grid = bool(settings.get_value("display", "show_grid", false))

func _save_visual_settings() -> void:
	var settings := ConfigFile.new()
	settings.set_value("display", "show_grid", show_grid)
	settings.save(VISUAL_SETTINGS_FILE)

func _refresh_grid_button() -> void:
	if grid_button != null:
		grid_button.text = _tr("▦   RASTER: AN  [G]", "▦   GRID: ON  [G]") if show_grid else _tr(
			"▦   RASTER: AUS  [G]", "▦   GRID: OFF  [G]")

func _toggle_grid() -> void:
	show_grid = not show_grid
	settings.values.grid = show_grid
	settings.save()
	grid_view.set_grid_visible(show_grid)
	_refresh_grid_button()
	_notice(_tr("Bauraster eingeblendet", "Placement grid visible") if show_grid else
		_tr("Bauraster ausgeblendet", "Placement grid hidden"))

func _on_construction_phase(number: int) -> void:
	construction_view.queue_redraw()
	if build_dock != null:
		build_dock._build_machines()
	_notice(_tr("Casino-Bauphase %02d abgeschlossen",
		"Casino construction phase %02d completed") % number)
	_refresh_ui()

func _toggle_storage() -> void:
	if jobs_view != null:
		jobs_view.hide_jobs()
	if storage_view.visible:
		storage_view.close_inventory()
		return
	minimap.hide_map()
	_cancel_select_drag()
	moving_camera = false
	storage_view.open_inventory()

func _toggle_minimap() -> void:
	if jobs_view != null:
		jobs_view.hide_jobs()
	if storage_view.visible:
		storage_view.close_inventory()
	if minimap.visible:
		minimap.hide_map()
		return
	_cancel_select_drag()
	moving_camera = false
	minimap.show_map()

func _focus_construction() -> void:
	_show_menu(false)
	camera.position = (Vector2(Construction.ORIGIN) +
		Vector2(Construction.WIDTH, Construction.HEIGHT) * 0.5) * TILE
	_bound_camera()
	_notice(_tr("Casino-Bauplatz · 48 Materialeingänge",
		"Casino construction · 48 material inputs"))

func _new_factory() -> void:
	return_to_main()
	if navigation != null:
		navigation.name_edit.grab_focus()

func _show_menu(show: bool) -> void:
	_cancel_select_drag()
	if jobs_view != null:
		jobs_view.hide_jobs()
	storage_view.close_inventory()
	minimap.hide_map()
	if current_world.is_empty():
		menu_open = true
		motion_view.menu_open = true
		menu_mask.hide()
		if navigation != null:
			navigation.show_start()
		return
	menu_open = show
	motion_view.menu_open = show
	menu_mask.visible = show
	if show:
		var meta: Dictionary = worlds._meta(current_world)
		if pause_world_label != null:
			pause_world_label.text = str(meta.get("name", _tr("Fabrik", "Factory")))
		if pause_phase_label != null:
			pause_phase_label.text = _tr("CASINO · PHASE ", "CASINO · PHASE ") + \
				"%02d/12" % model.construction.phase_number()
		menu_card.modulate.a = 1.0
	else:
		motion_view.queue_redraw()

func _exit_to_casino() -> void:
	if not _save_world():
		_notice(_tr("Speichern fehlgeschlagen · Zurück nicht möglich", "Save failed · cannot quit safely"))
		return
	get_tree().quit()

func _physics_process(delta: float) -> void:
	if not current_world.is_empty() and not menu_open and not simulation_paused:
		model.advance(delta)
		save_accumulator += delta
		played_since_save += delta
		if save_accumulator >= float(settings.values.autosave):
			save_accumulator = 0.0
			if not _save_world():
				_notice(_tr("Autosave fehlgeschlagen", "Autosave failed"))
	ui_accumulator += delta
	if ui_accumulator >= 0.30:
		ui_accumulator = 0.0
		_refresh_ui()

func _process(delta: float) -> void:
	if menu_open or minimap.visible or storage_view.visible or jobs_view.visible or navigation.settings_screen.visible:
		return
	var movement := Vector2.ZERO
	if Input.is_action_pressed("industries_up") or Input.is_key_pressed(KEY_UP):
		movement.y -= 1.0
	if Input.is_action_pressed("industries_down") or Input.is_key_pressed(KEY_DOWN):
		movement.y += 1.0
	if Input.is_action_pressed("industries_left") or Input.is_key_pressed(KEY_LEFT):
		movement.x -= 1.0
	if Input.is_action_pressed("industries_right") or Input.is_key_pressed(KEY_RIGHT):
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

func _layout_hud() -> void:
	var view: Vector2 = get_viewport_rect().size
	header_compact = view.x < 1430.0
	for id in header_buttons:
		var action: Button = header_buttons[id]
		action.text = "" if header_compact else str(action.get_meta("full_label", ""))
		action.custom_minimum_size.x = 48.0 if header_compact else float({
			"achievements":144.0, "jobs":86.0, "upgrades":127.0,
			"storage":111.0, "pause":95.0, "menu":91.0}.get(id, 95.0))
	if pause_button != null:
		pause_button.text = "" if header_compact else (
			_tr("WEITER", "RESUME") if simulation_paused else "PAUSE")
	if inspector_panel != null:
		# Anchor the inspector to the top only. It must never stretch down
		# behind the bottom build dock at any viewport height or UI scale.
		inspector_panel.set_anchor(SIDE_BOTTOM, 0.0, false)
		inspector_panel.offset_top = 109.0
		inspector_panel.offset_bottom = minf(468.0, view.y - 214.0)
		inspector_panel.offset_left = -376.0
		inspector_panel.offset_right = -16.0


func _pointer_over_hud(point: Vector2) -> bool:
	var top: float = 114.0 if settings != null \
		and int(settings.values.window_mode) == 0 else 92.0
	if point.y < top:
		return true
	if build_dock != null and build_dock.is_over(point):
		return true
	if objectives_drawer != null and objectives_drawer.is_over(point):
		return true
	return inspector_panel != null and inspector_panel.visible \
		and inspector_panel.get_global_rect().has_point(point)

func _cancel_select_drag() -> void:
	selection_pending = false
	selection_dragging = false
	selection_press_cell = Vector2i(-1, -1)

func _begin_select_drag(screen_position: Vector2) -> void:
	selection_pending = true
	selection_dragging = false
	selection_press_screen = screen_position
	var world: Vector2 = get_global_mouse_position()
	selection_press_cell = Vector2i(floori(world.x / TILE), floori(world.y / TILE))

func _input(event: InputEvent) -> void:
	# This keeps panning when the cursor passes over a HUD panel. A short
	# press/release still selects the original world cell.
	if not selection_pending or minimap.visible or storage_view.visible or navigation.settings_screen.visible:
		return
	if event is InputEventMouseMotion:
		if not selection_dragging and event.position.distance_to(selection_press_screen) >= SELECT_DRAG_THRESHOLD:
			selection_dragging = true
		if selection_dragging:
			camera.position -= event.relative / camera.zoom
			_bound_camera()
			get_viewport().set_input_as_handled()
	elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT and not event.pressed:
		if not selection_dragging and mode == "inspect" and not menu_open and not _pointer_over_hud(event.position):
			_inspect(selection_press_cell)
		_cancel_select_drag()
		get_viewport().set_input_as_handled()

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventKey and event.pressed and not event.echo:
		if navigation.waiting_key != "" or navigation.settings_screen.visible:
			if event.is_action_pressed("industries_menu") and navigation.waiting_key == "":
				navigation.close_settings()
			get_viewport().set_input_as_handled()
			return
		if get_viewport().gui_get_focus_owner() is LineEdit:
			return
		if navigation.start_screen.visible:
			get_viewport().set_input_as_handled()
			return
		if jobs_view.visible:
			if event.is_action_pressed("industries_jobs") or event.is_action_pressed("industries_menu"):
				jobs_view.hide_jobs()
			get_viewport().set_input_as_handled()
			return
		if storage_view.visible:
			if event.is_action_pressed("industries_inventory") or event.is_action_pressed("industries_menu"):
				storage_view.close_inventory()
			get_viewport().set_input_as_handled()
			return
		if minimap.visible:
			if event.is_action_pressed("industries_map") or event.is_action_pressed("industries_menu"):
				minimap.hide_map()
			get_viewport().set_input_as_handled()
			return
		if not menu_open and event.is_action_pressed("industries_jobs"):
			_toggle_jobs()
			get_viewport().set_input_as_handled()
			return
		if not menu_open and event.is_action_pressed("industries_inventory"):
			_toggle_storage()
			get_viewport().set_input_as_handled()
			return
		if not menu_open and event.is_action_pressed("industries_map"):
			_toggle_minimap()
			get_viewport().set_input_as_handled()
			return
		if event.is_action_pressed("industries_menu"):
			_show_menu(not menu_open)
			get_viewport().set_input_as_handled()
			return
		if menu_open:
			return
		if event.is_action_pressed("industries_save"):
			_save_now()
		elif event.is_action_pressed("industries_inspect"):
			_set_mode("inspect")
		elif event.is_action_pressed("industries_build"):
			_set_mode("build")
		elif _tool_action(event) >= 0:
			_select_tool(TOOLS[_tool_action(event)])
		elif event.is_action_pressed("industries_rotate"):
			if mode == "build":
				facing = (facing + 1) % 4
				motion_view.facing = facing
				_notice(_tr("Baurichtung: ", "Build direction: ") + DIR_TEXT[facing])
			else:
				_rotate_selected()
		elif event.is_action_pressed("industries_pause"):
			_toggle_pause()
		elif event.is_action_pressed("industries_grid"):
			_toggle_grid()
		elif event.is_action_pressed("industries_casino"):
			_focus_construction()
		elif event.is_action_pressed("industries_zoom_in"):
			camera.zoom = (camera.zoom * 1.12).clamp(Vector2(0.55, 0.55), Vector2(2.4, 2.4))
		elif event.is_action_pressed("industries_zoom_out"):
			camera.zoom = (camera.zoom / 1.12).clamp(Vector2(0.55, 0.55), Vector2(2.4, 2.4))
		return
	if menu_open or minimap.visible or storage_view.visible or jobs_view.visible or navigation.settings_screen.visible:
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
			if event.pressed:
				_cancel_select_drag()
			moving_camera = event.pressed
			get_viewport().set_input_as_handled()
		elif event.pressed and not _pointer_over_hud(event.position):
			if event.button_index == MOUSE_BUTTON_LEFT:
				if mode == "inspect":
					_begin_select_drag(event.position)
				elif model.place(hovered, tool, facing):
					static_view.refresh_cell(hovered)
					_notice(_tr("Gebäude platziert", "Building placed") +
						" · " + _cost_text(tool))
					_refresh_ui()
				else:
					var issue: String = model.placement_issue(hovered, tool)
					match issue:
						"slots":
							_notice(_tr("Keine Stromslots frei · Generator entfernen oder Nebenauftrag abschließen",
								"No power slots free · remove a generator or complete a side job"))
						"river":
							_notice(_tr("Wasserräder benötigen ein Flussufer",
								"Water wheels must be placed beside a river"))
						"locked":
							_notice(_tr("Maschine noch gesperrt · Jobs prüfen",
								"Machine locked · check Jobs"))
						_:
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
		if current_world.is_empty() or _save_world():
			get_tree().quit()
		else:
			_notice(_tr("Speichern fehlgeschlagen", "Save failed"))

func _exit_tree() -> void:
	if model != null and not current_world.is_empty():
		_save_world()

func _tool_action(event: InputEvent) -> int:
	for i in range(TOOLS.size()):
		if event.is_action_pressed("industries_tool_%d" % (i + 1)):
			return i
	return -1

func _world_view() -> Dictionary:
	var record: Dictionary = saved_view.duplicate(true)
	record["objectives_expanded"] = objectives_drawer.expanded if objectives_drawer != null else false
	record["camera_x"] = camera.position.x
	record["camera_y"] = camera.position.y
	record["zoom"] = camera.zoom.x
	record["markers"] = minimap.markers.duplicate(true)
	record["layers"] = minimap.layers.duplicate(true)
	# Preserve old Phase-1 pinned items in saves without reintroducing the
	# rejected oversized toolbar. They remain available for a future UI.
	return record

func _save_world() -> bool:
	if current_world.is_empty():
		return false
	if not worlds.save_world(current_world, model, _world_view(), played_since_save):
		return false
	played_since_save = 0.0
	return true

func open_world(id: String, record: Dictionary) -> void:
	model = record.model
	current_world = id
	saved_view = record.view.duplicate(true)
	model.construction.phase_completed.connect(_on_construction_phase)
	model.progression.job_completed.connect(_on_job_completed)
	terrain_view.model = model
	terrain_view.refresh_from_model()
	static_view.model = model
	static_view.refresh_all()
	motion_view.model = model
	construction_view.model = model
	construction_view.queue_redraw()
	minimap.model = model
	minimap.markers = record.view.get("markers", []).duplicate(true)
	minimap.layers = record.view.get("layers", minimap.DEFAULT_LAYERS).duplicate(true)
	minimap.refresh_map()
	storage_view.model = model
	storage_view.german = german
	jobs_view.model = model
	build_dock.model = model
	build_dock._build_machines()
	build_dock.set_inspecting()
	objectives_drawer.set_expanded(bool(record.view.get("objectives_expanded", false)))
	inspected = Vector2i(-1, -1)
	motion_view.inspected = inspected
	_animate_inspector(false)
	camera.position = Vector2(
		float(record.view.get("camera_x", 1008.0)),
		float(record.view.get("camera_y", 864.0)))
	camera.zoom = Vector2.ONE * clampf(float(record.view.get("zoom", 1.0)), 0.55, 2.4)
	_bound_camera()
	save_accumulator = 0.0
	played_since_save = 0.0
	last_totals = ""
	simulation_paused = false
	motion_view.simulation_paused = false
	_refresh_ui()

func return_to_main() -> void:
	if not current_world.is_empty() and not _save_world():
		_notice(_tr("Speichern fehlgeschlagen; Welt bleibt geöffnet",
			"Save failed; world remains open"))
		return
	current_world = ""
	menu_mask.hide()
	menu_open = true
	motion_view.menu_open = true
	storage_view.close_inventory()
	minimap.hide_map()
	jobs_view.hide_jobs()
	navigation.show_start()

func apply_settings(changed: String = "") -> void:
	if changed in ["", "grid"]:
		show_grid = bool(settings.values.grid)
		grid_view.set_grid_visible(show_grid)
		_refresh_grid_button()
	if changed in ["", "reduced_motion"]:
		reduced_motion = bool(settings.values.reduced_motion)
		if build_dock != null:
			build_dock.reduced_motion = reduced_motion
		if objectives_drawer != null:
			objectives_drawer.reduced_motion = reduced_motion
		motion_view.reduced_motion = reduced_motion
		minimap.reduced_motion = reduced_motion
		if effects_button != null:
			effects_button.text = _tr("✦   ANIMATIONEN: AUS", "✦   ANIMATIONS: OFF") \
				if reduced_motion else _tr("✦   ANIMATIONEN: AN", "✦   ANIMATIONS: ON")
	if changed in ["", "effect_quality"]:
		motion_view.effect_quality = clampi(int(settings.values.effect_quality), 0, 2)
	if changed in ["", "water_motion"]:
		minimap.water_motion = bool(settings.values.water_motion)
		terrain_view.set_water_motion(bool(settings.values.water_motion))
	if changed in ["", "high_contrast"]:
		_apply_contrast(ui_layer)
	if changed in ["", "window_mode"]:
		_sync_window_chrome()
	if changed in ["", "ui_scale_commit"]:
		var scale := clampf(float(settings.values.ui_scale), 0.8, 1.3)
		if not is_equal_approx(get_viewport().content_scale_factor, scale):
			get_viewport().content_scale_factor = scale
	# Language is rebuilt ONCE when Settings closes, not on every
	# _change_setting value_changed signal during panel interaction.

func _sync_window_chrome() -> void:
	var windowed: bool = int(settings.values.window_mode) == 0
	if window_chrome != null:
		window_chrome.set_windowed(windowed)
	if header_rail != null:
		header_rail.offset_top = 39.0 if windowed else 9.0
		header_rail.offset_bottom = 111.0 if windowed else 81.0
	_layout_hud()
	if objectives_drawer != null:
		objectives_drawer.position.y = 159.0 if windowed else 123.0

func _rebuild_interface_language() -> void:
	var next_language := str(settings.values.language) == "de"
	if german == next_language:
		return
	var previous_mode: String = mode
	var previous_menu: bool = menu_open
	var previous_marker_list: Array = minimap.markers.duplicate(true)
	var previous_layers: Dictionary = minimap.layers.duplicate(true)
	var previous_zoom: float = minimap.zoom
	var previous_pan: Vector2 = minimap.pan
	var map_was_open: bool = minimap.visible
	var storage_was_open: bool = storage_view.visible
	var jobs_were_open: bool = jobs_view.visible
	var drawer_was_expanded: bool = objectives_drawer.expanded
	var previous_category: String = build_dock.selected_category
	var previous_tool: String = tool
	var settings_was_open: bool = navigation.settings_screen.visible
	var active_tab: String = navigation.active_tab
	german = next_language
	ui_layer.free()
	hover_tweens.clear()
	tool_buttons.clear()
	quick_buttons.clear()
	mode_buttons.clear()
	header_buttons.clear()
	inspector_visible = false
	_create_interface()
	build_dock.selected_category = previous_category
	build_dock.selected_machine = previous_tool
	build_dock._build_machines()
	objectives_drawer.set_expanded(drawer_was_expanded)
	minimap.markers = previous_marker_list
	minimap.layers = previous_layers
	minimap.zoom = previous_zoom
	minimap.pan = previous_pan
	minimap.reduced_motion = reduced_motion
	minimap.water_motion = bool(settings.values.water_motion)
	_set_mode(previous_mode)
	if current_world.is_empty():
		menu_mask.hide()
		menu_open = true
		motion_view.menu_open = true
		navigation.show_start()
	else:
		navigation.start_screen.hide()
		_show_menu(previous_menu)
		if map_was_open:
			minimap.show_map()
		if storage_was_open:
			storage_view.open_inventory()
		if jobs_were_open:
			jobs_view.show_jobs()
	if settings_was_open:
		navigation.open_settings()
		navigation._select_tab(active_tab)
	apply_settings("window_mode")
	_refresh_ui()

func _apply_contrast(node: Node) -> void:
	if node is Label:
		var label := node as Label
		if bool(settings.values.high_contrast):
			if not label.has_meta("original_font_color"):
				label.set_meta("original_font_color", label.get_theme_color("font_color"))
			label.add_theme_color_override("font_color", Color.WHITE)
			label.add_theme_color_override("font_shadow_color", Color.BLACK)
		else:
			if label.has_meta("original_font_color"):
				label.add_theme_color_override("font_color",
					label.get_meta("original_font_color"))
			label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.38))
	for child in node.get_children():
		_apply_contrast(child)
