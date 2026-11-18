extends Control
## Dedicated world-selection and settings UI. The factory HUD, build catalog
## and depot inventory deliberately remain the previously accepted originals.
const MenuTheme = preload("res://factory_menu_theme.gd")
const UiSkin = preload("res://factory_skin.gd")
const TEXT := Color("#EAF3F6")
const MUTED := Color("#A7BAC5")
const CYAN := Color("#68D4EC")
const AMBER := Color("#F0C477")
const RED := Color("#EF8286")
var game: Node
var manager
var settings
var start_screen: Control
var settings_screen: Control
var start_panel: PanelContainer
var settings_panel: PanelContainer
var name_edit: LineEdit
var world_list: VBoxContainer
var info: Label
var waiting_key := ""
var selection := ""
var click_player: AudioStreamPlayer
var settings_tabs: Dictionary = {}
var tab_buttons: Dictionary = {}
var choice_buttons: Dictionary = {}
var key_buttons: Dictionary = {}
var status_label: Label
var active_tab := "audio"

func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = MOUSE_FILTER_IGNORE
	_build_click_sound()
	_build_start()
	_build_settings()
	start_screen.visible = true
	settings_screen.visible = false
	get_viewport().size_changed.connect(_resize_panels)
	_resize_panels()
	_refresh_worlds()

func _word(de: String, en: String) -> String:
	return de if game.german else en

func _resize_panels() -> void:
	var view: Vector2 = get_viewport_rect().size
	if start_panel != null:
		start_panel.custom_minimum_size = Vector2(
			minf(1120.0, maxf(620.0, view.x - 78.0)),
			minf(770.0, maxf(480.0, view.y - 72.0)))
	if settings_panel != null:
		settings_panel.custom_minimum_size = Vector2(
			minf(980.0, maxf(620.0, view.x - 78.0)),
			minf(760.0, maxf(480.0, view.y - 72.0)))

func _build_click_sound() -> void:
	var stream := AudioStreamWAV.new()
	stream.mix_rate = 22050
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	var samples := PackedByteArray()
	samples.resize(1102 * 2)
	for i in range(1102):
		var envelope := pow(1.0 - float(i) / 1102.0, 2.0)
		var tone := sin(float(i) * TAU * 660.0 / 22050.0)
		samples.encode_s16(i * 2, int(tone * envelope * 1150.0))
	stream.data = samples
	click_player = AudioStreamPlayer.new()
	click_player.bus = "UI"
	click_player.stream = stream
	add_child(click_player)

func play_ui_click() -> void:
	if is_instance_valid(click_player):
		click_player.play()

func _label(parent: Node, content: String, font_px: int = 14,
	color: Color = TEXT) -> Label:
	var node := Label.new()
	node.text = content
	MenuTheme.label(node, font_px, color)
	parent.add_child(node)
	return node

func _button(parent: Node, caption: String, action: Callable,
	danger: bool = false, primary: bool = false) -> Button:
	var b := Button.new()
	b.text = caption
	b.custom_minimum_size = Vector2(96, 42)
	MenuTheme.style_button(b, danger, primary)
	b.pressed.connect(action)
	b.pressed.connect(play_ui_click)
	parent.add_child(b)
	return b

func _panel(parent: Node, kind: String = "section") -> PanelContainer:
	var panel := PanelContainer.new()
	panel.add_theme_stylebox_override("panel", MenuTheme.panel(kind))
	parent.add_child(panel)
	return panel

func _stack(parent: Node, separation: int = 12) -> VBoxContainer:
	var col := VBoxContainer.new()
	col.add_theme_constant_override("separation", separation)
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(col)
	return col

func _rule(parent: Node) -> void:
	var line := ColorRect.new()
	line.color = Color("#476879", 0.62)
	line.custom_minimum_size.y = 1
	line.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(line)

func _backdrop() -> ColorRect:
	var bg := ColorRect.new()
	bg.color = Color("#06131D")
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var shader := Shader.new()
	shader.code = """
shader_type canvas_item;
void fragment() {
	vec2 u = UV;
	float vignette = (1.0 - smoothstep(0.08, 1.0, length((u - 0.5) * vec2(1.25, 1.02))));
	float grid = (step(0.992, fract(u.x * 85.0)) +
		step(0.992, fract(u.y * 48.0))) * 0.017;
	float sheen = pow(max(0.0, 1.0 - length(u - vec2(0.12, 0.05)) * 1.18), 3.0);
	vec3 tone = mix(vec3(0.018, 0.039, 0.058),
		vec3(0.029, 0.074, 0.093), vignette * 0.7);
	tone += vec3(0.022, 0.041, 0.043) * sheen + vec3(grid);
	COLOR = vec4(tone, 0.972);
}
"""
	var material := ShaderMaterial.new()
	material.shader = shader
	bg.material = material
	return bg

func _cover(title: String, subtitle: String) -> Dictionary:
	var overlay := Control.new()
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(overlay)
	var bg := _backdrop()
	overlay.add_child(bg)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var center := CenterContainer.new()
	overlay.add_child(center)
	center.mouse_filter = Control.MOUSE_FILTER_PASS
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var panel := _panel(center, "main")
	var col := _stack(panel, 15)
	var accent := ColorRect.new()
	accent.color = CYAN
	accent.custom_minimum_size.y = 3
	accent.mouse_filter = Control.MOUSE_FILTER_IGNORE
	col.add_child(accent)
	var masthead := HBoxContainer.new()
	masthead.add_theme_constant_override("separation", 18)
	col.add_child(masthead)
	var emblem := ColorRect.new()
	emblem.color = CYAN
	emblem.custom_minimum_size = Vector2(4, 67)
	emblem.mouse_filter = Control.MOUSE_FILTER_IGNORE
	masthead.add_child(emblem)
	var header := _stack(masthead, 5)
	_label(header, "MIDNIGHT  /  INDUSTRIES", 13, AMBER)
	_label(header, title, 30, TEXT)
	_label(header, subtitle, 13, MUTED)
	_rule(col)
	return {"overlay":overlay, "panel":panel, "column":col}

func _build_start() -> void:
	var parts := _cover(_word("DEINE WELTEN", "YOUR WORLDS"),
		_word("Fabriken verwalten, sichern und fortsetzen.",
			"Build your industry. Continue where you left off."))
	start_screen = parts.overlay
	start_screen.name = "WorldSelectionScreen"
	start_panel = parts.panel
	var col: VBoxContainer = parts.column
	var actions := HBoxContainer.new()
	actions.add_theme_constant_override("separation", 10)
	col.add_child(actions)
	var resume := _button(actions, _word("Fortsetzen", "Continue"),
		func() -> void: _open_world(manager.last_id), false, true)
	resume.name = "ContinueWorld"
	_button(actions, _word("Einstellungen", "Settings"), open_settings)
	_button(actions, _word("Credits", "Credits"), _show_credits)
	var spacer := Control.new()
	spacer.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	actions.add_child(spacer)
	_button(actions, _word("Beenden", "Quit"), func() -> void: game.get_tree().quit())
	var create_card := _panel(col)
	var create_col := _stack(create_card, 8)
	_label(create_col, _word("NEUE WELT", "CREATE WORLD"), 12, AMBER)
	var create := HBoxContainer.new()
	create.add_theme_constant_override("separation", 12)
	create_col.add_child(create)
	name_edit = LineEdit.new()
	name_edit.name = "WorldNameInput"
	name_edit.placeholder_text = _word("Name deiner neuen Fabrik", "Give your factory a name")
	name_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	name_edit.custom_minimum_size.y = 44
	MenuTheme.style_input(name_edit)
	name_edit.text_submitted.connect(func(_value: String) -> void: _create_world())
	create.add_child(name_edit)
	var new_btn := _button(create, _word("Welt erstellen", "Create world"), _create_world, false, true)
	new_btn.custom_minimum_size.x = 172
	var caption := HBoxContainer.new()
	col.add_child(caption)
	_label(caption, _word("GESPEICHERTE WELTEN", "SAVED WORLDS"), 14, AMBER)
	var stretch := Control.new()
	stretch.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	caption.add_child(stretch)
	_label(caption, _word("Automatische Sicherungen aktiviert", "Rotating backups enabled"), 12, MUTED)
	var scroll := ScrollContainer.new()
	scroll.name = "WorldListScroll"
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.custom_minimum_size.y = 190
	col.add_child(scroll)
	world_list = _stack(scroll, 10)
	world_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	info = _label(col, "", 12, MUTED)
	info.custom_minimum_size.y = 20
	_rule(col)
	_label(col, _word("Rohstoffe  →  Produktion  →  Depot  →  Casino",
		"Resources  →  Production  →  Depot  →  Casino"), 12, MUTED)

func _show_credits() -> void:
	var dialog := AcceptDialog.new()
	dialog.title = "Midnight Industries"
	dialog.dialog_text = _word("Midnight Industries · Godot 4.5\nFabrikspiel und Grafiken",
		"Midnight Industries · Godot 4.5\nFactory simulation and artwork")
	start_screen.add_child(dialog)
	dialog.confirmed.connect(dialog.queue_free)
	dialog.popup_centered()

func _refresh_worlds() -> void:
	if world_list == null:
		return
	for child in world_list.get_children():
		child.queue_free()
	var resume := start_screen.find_child("ContinueWorld", true, false) as Button
	if resume != null:
		resume.disabled = manager.last_id.is_empty()
	if manager.worlds.is_empty():
		var empty := _panel(world_list)
		var row := _stack(empty, 5)
		_label(row, _word("Noch keine Welten vorhanden", "No worlds yet"), 18)
		_label(row, _word("Erstelle oben deine erste Fabrik.",
			"Create your first factory above."), 13, MUTED)
		return
	for world in manager.worlds:
		var id: String = str(world.get("id", ""))
		var card := _panel(world_list)
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 12)
		card.add_child(row)
		var meta_col := _stack(row, 6)
		meta_col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		var display_name: String = str(world.get("name", "?"))
		if bool(world.get("legacy", false)) and display_name == "Bisherige Fabrik" and not game.german:
			display_name = "Previous Factory"
		var first_line := HBoxContainer.new()
		meta_col.add_child(first_line)
		var title := _label(first_line, display_name, 17, TEXT)
		title.clip_text = true
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		_label(first_line, "%02d/12" % int(world.get("phase", 1)), 14, AMBER)
		var stamp: String = Time.get_datetime_string_from_unix_time(int(world.get("updated", 0)), true)
		var minutes: int = int(float(world.get("play_seconds", 0)) / 60.0)
		_label(meta_col, _word("Zuletzt gespielt: ", "Last played: ") + stamp +
			"   ·   %d %s" % [minutes, _word("Min.", "min")], 12, MUTED)
		var actions := HBoxContainer.new()
		actions.add_theme_constant_override("separation", 8)
		row.add_child(actions)
		var play := _button(actions, _word("Laden", "Load"), _open_world.bind(id), false, true)
		play.custom_minimum_size.x = 88
		var rename := _button(actions, _word("Name", "Rename"), _rename_world.bind(id))
		rename.custom_minimum_size.x = 94
		var backup := _button(actions, _word("Backup", "Backups"), _show_backups.bind(id))
		backup.custom_minimum_size.x = 96
		var erase := _button(actions, _word("Löschen", "Delete"), _delete_world.bind(id), true)
		erase.custom_minimum_size.x = 96

func _create_world() -> void:
	var title: String = name_edit.text.strip_edges()
	if title.is_empty():
		title = _word("Neue Welt", "New World")
	var id: String = manager.create_world(title)
	if id.is_empty():
		info.text = _word("Welt konnte nicht gespeichert werden.",
			"World could not be saved.")
		return
	name_edit.clear()
	_refresh_worlds()
	_open_world(id)

func _open_world(id: String, backup_number: int = 0) -> void:
	if id.is_empty():
		return
	var record: Dictionary = manager.load_world(id) if backup_number == 0 \
		else manager.load_backup(id, backup_number)
	if record.is_empty():
		info.text = _word("Keine gültige Sicherung gefunden.",
			"No valid save or backup was found.")
		return
	game.open_world(id, record)
	start_screen.visible = false
	game._show_menu(false)
	if record.recovered:
		game._notice(_word("Backup geladen. Bitte erneut speichern.",
			"Backup recovered. Please save again."))

func _show_backups(id: String) -> void:
	var dialog := AcceptDialog.new()
	dialog.title = _word("Frühere Sicherungen", "Previous backups")
	dialog.dialog_text = _word("Wähle eine frühere Version dieser Welt.",
		"Choose a previous version of this world.")
	start_screen.add_child(dialog)
	for slot in range(1, manager.BACKUPS + 1):
		if not manager.load_backup(id, slot).is_empty():
			var stamp: int = FileAccess.get_modified_time(manager._path(id) + ".bak%d" % slot)
			dialog.add_button("%d · %s" % [
				slot, Time.get_datetime_string_from_unix_time(stamp, true)],
				false, "backup_%d" % slot)
	dialog.custom_action.connect(func(action: StringName) -> void:
		var slot: int = int(str(action).trim_prefix("backup_"))
		dialog.hide()
		_open_world(id, slot)
		dialog.queue_free())
	dialog.confirmed.connect(dialog.queue_free)
	dialog.popup_centered()

func show_start() -> void:
	_refresh_worlds()
	start_screen.visible = true
	settings_screen.visible = false

func _rename_world(id: String) -> void:
	if manager.rename_world(id, name_edit.text):
		name_edit.clear()
		_refresh_worlds()
		info.text = _word("Welt umbenannt.", "World renamed.")
	else:
		info.text = _word("Neuen Namen oben eingeben.", "Enter a new name above.")

func _delete_world(id: String) -> void:
	if selection != id:
		selection = id
		info.add_theme_color_override("font_color", RED)
		info.text = _word("Löschen bestätigen: Erneut auf LÖSCHEN derselben Welt klicken.",
			"Confirm deletion: Click DELETE on the same world again.")
		return
	selection = ""
	if manager.delete_world(id):
		_refresh_worlds()
		info.add_theme_color_override("font_color", MUTED)
		info.text = _word("Welt gelöscht.", "World deleted.")

func _section(parent: Node, title: String, subtitle: String = "") -> VBoxContainer:
	var card := _panel(parent)
	var col := _stack(card, 10)
	_label(col, title, 16, AMBER)
	if not subtitle.is_empty():
		_label(col, subtitle, 12, MUTED)
	return col

func _toggle(parent: Node, key: String, caption: String) -> void:
	var row := HBoxContainer.new()
	row.custom_minimum_size.y = 42
	parent.add_child(row)
	var label := _label(row, caption, 14)
	label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var toggle := CheckButton.new()
	toggle.name = "Toggle_" + key
	toggle.button_pressed = bool(settings.values[key])
	toggle.add_theme_color_override("font_color", TEXT)
	toggle.toggled.connect(func(on: bool) -> void: _change_setting(key, on))
	row.add_child(toggle)

func _slider(parent: Node, key: String, caption: String, lower: float,
	upper: float, step: float, suffix: String = "") -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 14)
	row.custom_minimum_size.y = 48
	parent.add_child(row)
	var label := _label(row, caption, 14)
	label.custom_minimum_size.x = 185
	var slider := HSlider.new()
	slider.name = "Slider_" + key
	slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	slider.min_value = lower
	slider.max_value = upper
	slider.step = step
	slider.value = float(settings.values[key])
	MenuTheme.style_slider(slider)
	row.add_child(slider)
	var value_label := _label(row, "", 14, CYAN)
	value_label.custom_minimum_size.x = 72
	value_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	var fmt := func(value: float) -> String:
		return ("%.1f" % value if step < 1.0 else str(roundi(value))) + suffix
	value_label.text = fmt.call(slider.value)
	slider.value_changed.connect(func(v: float) -> void:
		value_label.text = fmt.call(v)
		_change_setting(key, v))

func _options(parent: Node, key: String, caption: String,
	choices: Array) -> void:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 9)
	row.custom_minimum_size.y = 46
	parent.add_child(row)
	var title := _label(row, caption, 14)
	title.custom_minimum_size.x = 185
	var buttons: Array = []
	for choice in choices:
		var v: Variant = choice[0]
		var b := _button(row, str(choice[1]), _choose.bind(key, v))
		b.toggle_mode = true
		b.button_pressed = settings.values[key] == v
		MenuTheme.style_button(b, false, b.button_pressed)
		b.custom_minimum_size.x = 105
		buttons.append({"button":b, "value":v})
	choice_buttons[key] = buttons

func _choose(key: String, value: Variant) -> void:
	_change_setting(key, value)
	_refresh_choice(key)

func _refresh_choice(key: String) -> void:
	if not choice_buttons.has(key):
		return
	for entry in choice_buttons[key]:
		var b: Button = entry.button
		b.button_pressed = entry.value == settings.values[key]
		MenuTheme.style_button(b, false, b.button_pressed)

func _dropdown(parent: Node, key: String, caption: String,
	choices: Array) -> void:
	var row := HBoxContainer.new()
	row.custom_minimum_size.y = 52
	parent.add_child(row)
	var label := _label(row, caption, 14)
	label.custom_minimum_size.x = 185
	var selector := OptionButton.new()
	selector.name = "Option_" + key
	selector.custom_minimum_size.x = 265
	MenuTheme.style_option(selector)
	for choice in choices:
		selector.add_item(str(choice[1]))
		selector.set_item_metadata(selector.item_count - 1, choice[0])
		if choice[0] == settings.values[key]:
			selector.select(selector.item_count - 1)
	selector.item_selected.connect(func(index: int) -> void:
		_change_setting(key, selector.get_item_metadata(index)))
	row.add_child(selector)

func _build_settings() -> void:
	var parts := _cover(_word("EINSTELLUNGEN", "SETTINGS"),
		_word("Passe Leistung, Bedienung und Darstellung an.",
			"Tune visuals, controls and sound."))
	settings_screen = parts.overlay
	settings_screen.name = "SettingsScreen"
	settings_panel = parts.panel
	var col: VBoxContainer = parts.column
	var tabs := HBoxContainer.new()
	tabs.add_theme_constant_override("separation", 9)
	col.add_child(tabs)
	for entry in [
		["audio", _word("AUDIO", "AUDIO")],
		["video", _word("GRAFIK", "VIDEO")],
		["gameplay", _word("SPIEL & UI", "GAME & UI")],
		["controls", _word("STEUERUNG", "CONTROLS")]]:
		var id: String = str(entry[0])
		var button := _button(tabs, str(entry[1]), _select_tab.bind(id))
		button.toggle_mode = true
		tab_buttons[id] = button
	var scroll := ScrollContainer.new()
	scroll.name = "SettingsScroll"
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	scroll.custom_minimum_size.y = 230
	col.add_child(scroll)
	var body := _stack(scroll, 10)
	for key in ["audio", "video", "gameplay", "controls"]:
		var section := _stack(body, 12)
		settings_tabs[key] = section
	var audio: VBoxContainer = settings_tabs["audio"]
	var sound := _section(audio, _word("AUDIO MIXER", "AUDIO MIXER"),
		_word("Lautstärke je Kanal und Stummschaltung.",
			"Independent volume and mute controls."))
	for channel in ["master", "music", "effects", "ui"]:
		var label: String = {
			"master":_word("Gesamt", "Master"), "music":_word("Musik", "Music"),
			"effects":_word("Effekte", "Effects"), "ui":_word("Oberfläche", "Interface")
		}[channel]
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 10)
		sound.add_child(row)
		var name := _label(row, label, 14)
		name.custom_minimum_size.x = 165
		var slider := HSlider.new()
		slider.name = "Volume_" + channel
		slider.min_value = 0.0
		slider.max_value = 1.0
		slider.step = 0.01
		slider.value = float(settings.values[channel])
		slider.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		MenuTheme.style_slider(slider)
		row.add_child(slider)
		var readout := _label(row, "%d%%" % roundi(slider.value * 100), 14, CYAN)
		readout.custom_minimum_size.x = 58
		readout.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		slider.value_changed.connect(func(value: float) -> void:
			readout.text = "%d%%" % roundi(value * 100)
			_change_setting(channel, value))
		var mute := CheckButton.new()
		mute.name = "Mute_" + channel
		mute.text = _word("Stumm", "Mute")
		mute.button_pressed = bool(settings.values[channel + "_mute"])
		mute.add_theme_color_override("font_color", TEXT)
		mute.toggled.connect(func(on: bool) -> void:
			_change_setting(channel + "_mute", on))
		row.add_child(mute)
	var video: VBoxContainer = settings_tabs["video"]
	var display := _section(video, _word("ANZEIGE", "DISPLAY"),
		_word("Anzeigemodus, Auflösung und Bildrate.",
			"Display mode, resolution and frame rate."))
	_options(display, "window_mode", _word("Fenstermodus", "Display mode"),
		[[0, _word("Fenster", "Windowed")],
		 [1, _word("Randlos", "Borderless")],
		 [2, _word("Vollbild", "Fullscreen")]])
	_dropdown(display, "resolution", _word("Auflösung", "Resolution"),
		[["1280x720", "1280 × 720"], ["1600x900", "1600 × 900"],
		 ["1920x1080", "1920 × 1080"]])
	_toggle(display, "vsync", "VSync")
	_slider(display, "fps", _word("FPS-Limit", "FPS limit"), 30, 240, 1)
	var effects := _section(video, _word("EFFEKTE", "VISUAL EFFECTS"),
		_word("Wähle die Qualität und reduziere Bewegungen.",
			"Set effect quality and reduce visual motion."))
	_options(effects, "effect_quality", _word("Effektqualität", "Effect quality"),
		[[0, _word("Aus", "Off")], [1, _word("Niedrig", "Low")],
		 [2, _word("Hoch", "High")]])
	_toggle(effects, "water_motion", _word("Animiertes Wasser", "Animated water"))
	_toggle(effects, "reduced_motion", _word("Reduzierte Animationen", "Reduced motion"))
	var gameplay: VBoxContainer = settings_tabs["gameplay"]
	var lang := _section(gameplay, _word("SPRACHE & BENUTZEROBERFLÄCHE", "LANGUAGE & INTERFACE"))
	_dropdown(lang, "language", _word("Sprache", "Language"),
		[["en", "English"], ["de", "Deutsch"]])
	_slider(lang, "ui_scale", _word("UI-Skalierung", "UI scale"), 0.8, 1.3, 0.1, "×")
	_options(lang, "tooltip_density", _word("Tooltips", "Tooltips"),
		[[0, _word("Aus", "Off")], [1, _word("Kurz", "Brief")],
		 [2, _word("Detailliert", "Detailed")]])
	_toggle(lang, "high_contrast", _word("Hoher Kontrast", "High contrast"))
	var world := _section(gameplay, _word("SPIELVERHALTEN", "GAMEPLAY"))
	_slider(world, "autosave", _word("Autosave-Abstand", "Autosave interval"),
		30, 300, 1, "s")
	_toggle(world, "grid", _word("Bauraster", "Construction grid"))
	_options(world, "power_lines", _word("Stromleitungen", "Power lines"),
		[["off", _word("Aus", "Off")],
		 ["auto", _word("Automatisch", "Auto")],
		 ["always", _word("Immer an", "Always")]])
	var controls: VBoxContainer = settings_tabs["controls"]
	var control_card := _section(controls, _word("TASTENBELEGUNG", "KEY BINDINGS"),
		_word("Klicke eine Aktion und drücke die gewünschte Taste.",
			"Click an action, then press the new key."))
	var grid := GridContainer.new()
	grid.columns = 2
	grid.add_theme_constant_override("h_separation", 12)
	grid.add_theme_constant_override("v_separation", 8)
	control_card.add_child(grid)
	for action in settings.keys:
		var id: String = str(action)
		var b := _button(grid, _key_title(id), _capture_key.bind(id))
		b.name = "Key_" + id
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		key_buttons[id] = b
	_button(control_card, _word("Standardbelegung wiederherstellen", "Restore default bindings"),
		func() -> void:
			settings.reset_keys()
			_refresh_keys())
	status_label = _label(col, "", 12, MUTED)
	status_label.custom_minimum_size.y = 16
	_rule(col)
	var bottom := HBoxContainer.new()
	col.add_child(bottom)
	var saved_caption := _label(bottom, _word("Änderungen werden automatisch gespeichert.",
		"Changes save automatically."), 12, MUTED)
	saved_caption.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	var close := _button(bottom, _word("Schließen", "Close"), close_settings, false, true)
	close.custom_minimum_size.x = 176
	_select_tab(active_tab)

func _key_title(action: String) -> String:
	var captions := {
		"menu":["Menü", "Menu"], "pause":["Pause", "Pause"],
		"save":["Speichern (Strg)", "Save (Ctrl)"], "map":["Karte", "Map"],
		"inventory":["Depot", "Depot"], "grid":["Raster", "Grid"],
		"casino":["Casino", "Casino"], "inspect":["Auswahl", "Inspect"],
		"build":["Bauen", "Build"], "rotate":["Drehen", "Rotate"],
		"zoom_in":["Zoom +", "Zoom +"], "zoom_out":["Zoom −", "Zoom −"],
		"up":["Hoch", "Up"], "down":["Runter", "Down"],
		"left":["Links", "Left"], "right":["Rechts", "Right"],
		"tool_1":["Maschine 1", "Tool 1"], "tool_2":["Maschine 2", "Tool 2"],
		"tool_3":["Maschine 3", "Tool 3"], "tool_4":["Maschine 4", "Tool 4"],
		"tool_5":["Maschine 5", "Tool 5"]
	}
	var names: Array = captions.get(action, [action, action])
	return "%s   ·   %s" % [
		str(names[0] if game.german else names[1]),
		OS.get_keycode_string(int(settings.keys[action]))]

func _select_tab(key: String) -> void:
	active_tab = key
	for id in settings_tabs:
		settings_tabs[id].visible = id == key
	for id in tab_buttons:
		var b: Button = tab_buttons[id]
		b.button_pressed = id == key
		MenuTheme.style_button(b, false, id == key)

func _refresh_keys() -> void:
	for action in key_buttons:
		var button: Button = key_buttons[action]
		button.text = _key_title(str(action))

func _capture_key(action: String) -> void:
	waiting_key = action
	key_buttons[action].text = _word("Taste drücken …", "Press a key …")
	status_label.text = _word("ESC bricht die Belegung ab.", "Press Esc to cancel.")

func _input(event: InputEvent) -> void:
	if waiting_key.is_empty() or not event is InputEventKey or not event.pressed:
		return
	if event.keycode == KEY_ESCAPE:
		waiting_key = ""
		_refresh_keys()
		get_viewport().set_input_as_handled()
		return
	var conflict: String = settings.rebind(waiting_key, event.keycode)
	if not conflict.is_empty():
		status_label.add_theme_color_override("font_color", RED)
		status_label.text = _word("Bereits vergeben an: ", "Already assigned to: ") + conflict
	else:
		status_label.add_theme_color_override("font_color", CYAN)
		status_label.text = _word("Tastenbelegung gespeichert.", "Key binding saved.")
	waiting_key = ""
	_refresh_keys()
	get_viewport().set_input_as_handled()

func _change_setting(key: String, value: Variant) -> void:
	if key == "language" and str(settings.values.language) == str(value):
		return
	settings.values[key] = value
	if key == "language":
		settings.values["language_explicit"] = true
	if key in ["resolution", "window_mode"]:
		settings.values["window_configured"] = true
	settings.save()
	if key == "language":
		# Switching languages is deliberate and infrequent. Recreate the UI
		# ONCE, deferred to the next frame, while keeping Settings open.
		game.call_deferred("_rebuild_interface_language")
		return
	settings.apply(key)
	game.apply_settings(key)
	_refresh_choice(key)

func open_settings() -> void:
	settings_screen.visible = true
	_select_tab(active_tab)
	status_label.text = ""

func close_settings() -> void:
	waiting_key = ""
	settings_screen.visible = false
	game.apply_settings("ui_scale_commit")
