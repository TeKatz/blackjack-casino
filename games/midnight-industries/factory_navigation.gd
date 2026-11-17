extends Control
## Recovered main menu, multi-save manager and settings only.
## Rejected Phase-1 build/depot toolbars are excluded from this UI.
const UiSkin = preload("res://factory_skin.gd")
const COLORS := {"ink":Color("#0E1C25"), "text":Color("#ECF2F0"),
	"muted":Color("#A5BDC3"), "accent":Color("#F1BD57")}
var game: Node
var manager
var settings
var start_screen: Control
var settings_screen: Control
var name_edit: LineEdit
var world_list: VBoxContainer
var info: Label
var selection := ""
var waiting_key := ""
var pending_language := false
var click_player: AudioStreamPlayer

func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_build_click_sound()
	_build_start()
	_build_settings()
	start_screen.visible = true
	settings_screen.visible = false
	_refresh_worlds()

func _build_click_sound() -> void:
	# Short synthesized control feedback; no external sound asset or license.
	var stream := AudioStreamWAV.new()
	stream.mix_rate = 22050
	stream.format = AudioStreamWAV.FORMAT_16_BITS
	var samples := PackedByteArray()
	samples.resize(1102 * 2)
	for i in range(1102):
		var envelope := pow(1.0 - float(i) / 1102.0, 2.0)
		var tone := sin(float(i) * TAU * 660.0 / 22050.0)
		samples.encode_s16(i * 2, int(tone * envelope * 1400.0))
	stream.data = samples
	click_player = AudioStreamPlayer.new()
	click_player.bus = "UI"
	click_player.stream = stream
	add_child(click_player)

func play_ui_click() -> void:
	if click_player != null:
		click_player.play()

func _word(de: String, en: String) -> String:
	return de if bool(game.german) else en

func _panel(parent: Node, name: String) -> PanelContainer:
	var panel := PanelContainer.new()
	panel.name = name
	panel.add_theme_stylebox_override("panel", UiSkin.panel("menu"))
	parent.add_child(panel)
	return panel

func _label(parent: Node, value: String, size_px: int = 15, color: Color = COLORS.text) -> Label:
	var label := Label.new()
	label.text = value
	label.add_theme_font_override("font", UiSkin.font())
	label.add_theme_font_size_override("font_size", size_px)
	label.add_theme_color_override("font_color", color)
	parent.add_child(label)
	return label

func _button(parent: Node, title: String, action: Callable) -> Button:
	var button := Button.new()
	button.text = title
	button.custom_minimum_size = Vector2(90, 40)
	button.add_theme_font_override("font", UiSkin.font())
	button.add_theme_font_size_override("font_size", 14)
	button.add_theme_stylebox_override("normal", UiSkin.button("button"))
	button.add_theme_stylebox_override("hover", UiSkin.button("button_active"))
	button.add_theme_stylebox_override("pressed", UiSkin.button("button_active"))
	button.pressed.connect(action)
	button.pressed.connect(play_ui_click)
	parent.add_child(button)
	return button

func _cover(parent: Node, title: String) -> Dictionary:
	var overlay := ColorRect.new()
	overlay.color = Color("#06131B", 0.96)
	overlay.mouse_filter = Control.MOUSE_FILTER_STOP
	parent.add_child(overlay)
	overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var center := CenterContainer.new()
	center.mouse_filter = Control.MOUSE_FILTER_PASS
	overlay.add_child(center)
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var panel := _panel(center, title)
	panel.custom_minimum_size = Vector2(620, 550)
	var column := VBoxContainer.new()
	column.add_theme_constant_override("separation", 12)
	panel.add_child(column)
	_label(column, "MIDNIGHT  /  INDUSTRIES", 25, COLORS.accent)
	_label(column, title, 19)
	column.add_child(HSeparator.new())
	return {"overlay":overlay, "column":column}

func _build_start() -> void:
	var parts := _cover(self, _word("WELTEN UND SPIELSTÄNDE", "WORLDS AND SAVES"))
	start_screen = parts.overlay
	var col: VBoxContainer = parts.column
	var actions := HBoxContainer.new()
	col.add_child(actions)
	_button(actions, _word("Fortsetzen", "Continue"), func() -> void: _open_world(manager.last_id)).name = "ContinueWorld"
	_button(actions, _word("Einstellungen", "Settings"), open_settings)
	_button(actions, _word("Credits", "Credits"), _show_credits)
	_button(actions, _word("Beenden", "Quit"), func() -> void: game.get_tree().quit())
	var create := HBoxContainer.new()
	col.add_child(create)
	name_edit = LineEdit.new()
	name_edit.placeholder_text = _word("Name der neuen Welt", "New world name")
	name_edit.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	create.add_child(name_edit)
	_button(create, _word("Neue Welt", "New world"), _create_world)
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size.y = 260
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	col.add_child(scroll)
	world_list = VBoxContainer.new()
	world_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(world_list)
	info = _label(col, "", 12, COLORS.muted)
	_label(col, _word("Starterlinie → Depot → neue Anlagen → Casinobauplatz", "Starter line → depot → new machines → casino site"), 12, COLORS.muted)

func _show_credits() -> void:
	var dialog := AcceptDialog.new()
	dialog.title = "Midnight Industries"
	dialog.dialog_text = "The Midnight Club · Godot 4.5.1\nFactory world and industrial artwork: Midnight Industries"
	start_screen.add_child(dialog)
	dialog.confirmed.connect(dialog.queue_free)
	dialog.popup_centered()

func _refresh_worlds() -> void:
	if world_list == null:
		return
	for child in world_list.get_children():
		child.queue_free()
	var resume := start_screen.find_child("ContinueWorld", true, false) as Button
	resume.disabled = manager.last_id.is_empty() if resume != null else false
	for world in manager.worlds:
		var id := str(world.get("id", ""))
		var row := HBoxContainer.new()
		world_list.add_child(row)
		var stamp := Time.get_datetime_string_from_unix_time(int(world.get("updated", 0)), true)
		var title := "%s  ·  %s  ·  %02d/12  ·  %s" % [str(world.get("name", "?")).substr(0, 28), stamp,
			int(world.get("phase", 1)), _word("%d Min." , "%d min.") % int(float(world.get("play_seconds", 0)) / 60.0)]
		var open := _button(row, title, _open_world.bind(id))
		open.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		_button(row, _word("Name", "Rename"), _rename_world.bind(id))
		_button(row, _word("Backup", "Backup"), _show_backups.bind(id))
		_button(row, _word("Löschen", "Delete"), _delete_world.bind(id))

func _create_world() -> void:
	var id: String = manager.create_world(name_edit.text)
	if id.is_empty():
		info.text = _word("Welt konnte nicht gespeichert werden", "World could not be saved")
		return
	name_edit.clear()
	_refresh_worlds()
	_open_world(id)

func _open_world(id: String, backup_number: int = 0) -> void:
	if id.is_empty():
		return
	var record: Dictionary = manager.load_world(id) if backup_number == 0 else manager.load_backup(id, backup_number)
	if record.is_empty():
		info.text = _word("Save beschädigt; keine gültige Sicherung gefunden", "Save damaged; no valid backup found")
		return
	game.open_world(id, record)
	start_screen.visible = false
	game._show_menu(false)
	if record.recovered:
		game._notice(_word("Welt aus Sicherung wiederhergestellt; jetzt speichern", "Recovered from backup; please save"))

func _show_backups(id: String) -> void:
	var dialog := AcceptDialog.new()
	dialog.title = _word("Sicherungen laden", "Load backup")
	dialog.dialog_text = _word("Ein Backup lädt einen früheren Stand dieser Welt.", "A backup loads an earlier state of this world.")
	start_screen.add_child(dialog)
	for slot in range(1, manager.BACKUPS + 1):
		if not manager.load_backup(id, slot).is_empty():
			var stamp := FileAccess.get_modified_time(manager._path(id) + ".bak%d" % slot)
			dialog.add_button("%d · %s" % [slot, Time.get_datetime_string_from_unix_time(stamp, true)],
				false, "backup_%d" % slot)
	dialog.custom_action.connect(func(action: StringName) -> void:
		var slot := int(str(action).trim_prefix("backup_"))
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
	else:
		info.text = _word("Neuen Namen oben eingeben", "Enter a new name above")

func _delete_world(id: String) -> void:
	if selection != id:
		selection = id
		info.text = _word("Zum Löschen derselben Welt erneut klicken", "Click Delete on the same world again to confirm")
		return
	selection = ""
	if manager.delete_world(id):
		_refresh_worlds()
		info.text = _word("Welt gelöscht", "World deleted")

func _build_settings() -> void:
	var parts := _cover(self, _word("EINSTELLUNGEN", "SETTINGS"))
	settings_screen = parts.overlay
	var col: VBoxContainer = parts.column
	var scroller := ScrollContainer.new()
	scroller.custom_minimum_size.y = 390
	col.add_child(scroller)
	var items := VBoxContainer.new()
	items.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroller.add_child(items)
	for entry in [
		["grid", "Raster / Grid"], ["reduced_motion", "Reduzierte Animationen / Reduced motion"],
		["high_contrast", "Hoher Kontrast / High contrast"],
		["water_motion", "Wasseranimation / Water animation"],
		["vsync", "VSync"]]:
		var key: String = entry[0]
		var toggle := CheckBox.new()
		toggle.text = entry[1]
		toggle.button_pressed = bool(settings.values[key])
		items.add_child(toggle)
		toggle.toggled.connect(func(on: bool) -> void: _change_setting(key, on))
	for channel in ["master", "music", "effects", "ui"]:
		var audio_row := HBoxContainer.new()
		items.add_child(audio_row)
		_label(audio_row, channel.capitalize(), 13)
		var volume := HSlider.new()
		volume.min_value = 0.0
		volume.max_value = 1.0
		volume.step = 0.05
		volume.value = float(settings.values[channel])
		volume.custom_minimum_size.x = 240
		audio_row.add_child(volume)
		volume.value_changed.connect(func(value: float) -> void: _change_setting(channel, value))
		var mute := CheckBox.new()
		mute.text = "Mute"
		mute.button_pressed = bool(settings.values[channel + "_mute"])
		audio_row.add_child(mute)
		mute.toggled.connect(func(on: bool) -> void: _change_setting(channel + "_mute", on))
	for entry in [["autosave", "Autosave (Sekunden)", 30, 300],
		["fps", "FPS Limit", 30, 240], ["ui_scale", "UI Skalierung", 0.8, 1.3]]:
		var key: String = entry[0]
		_label(items, str(entry[1]), 13)
		var slider := HSlider.new()
		slider.min_value = entry[2]
		slider.max_value = entry[3]
		slider.step = 0.1 if key == "ui_scale" else 1
		slider.value = float(settings.values[key])
		items.add_child(slider)
		slider.value_changed.connect(func(value: float) -> void: _change_setting(key, value))
	var tooltip_row := HBoxContainer.new()
	items.add_child(tooltip_row)
	_label(tooltip_row, _word("Tooltips", "Tooltips"), 13)
	for choice in [[_word("Aus", "Off"), 0], [_word("Kurz", "Brief"), 1],
		[_word("Detailliert", "Detailed"), 2]]:
		_button(tooltip_row, choice[0], func() -> void: _change_setting("tooltip_density", choice[1]))
	var quality_row := HBoxContainer.new()
	items.add_child(quality_row)
	_label(quality_row, _word("Effektqualität", "Effect quality"), 13)
	for choice in [[_word("Aus", "Off"), 0], [_word("Niedrig", "Low"), 1],
		[_word("Hoch", "High"), 2]]:
		_button(quality_row, choice[0], func() -> void: _change_setting("effect_quality", choice[1]))
	var resolutions := HBoxContainer.new()
	items.add_child(resolutions)
	for dimension in ["1280x720", "1600x900", "1920x1080"]:
		_button(resolutions, dimension, func() -> void: _change_setting("resolution", dimension))
	var modes := HBoxContainer.new()
	items.add_child(modes)
	for pair in [["Fenster", 0], ["Randlos", 1], ["Vollbild", 2]]:
		_button(modes, pair[0], func() -> void: _change_setting("window_mode", pair[1]))
	var languages := HBoxContainer.new()
	items.add_child(languages)
	_button(languages, "Deutsch", func() -> void: _change_setting("language", "de"))
	_button(languages, "English", func() -> void: _change_setting("language", "en"))
	_label(items, _word("Tasten: Klick auf Aktion, dann neue Taste drücken", "Keys: click action, then press a new key"), 13)
	for action in settings.keys:
		var button := _button(items, "%s: %s" % [action, OS.get_keycode_string(int(settings.keys[action]))],
			func() -> void: _capture_key(action))
		button.name = "Key_" + action
	_button(items, _word("Tasten zurücksetzen", "Reset keys"), func() -> void:
		settings.reset_keys()
		_refresh_keys())
	_button(col, _word("Schließen", "Close"), close_settings)

func _refresh_keys() -> void:
	for action in settings.keys:
		var button := settings_screen.find_child("Key_" + action, true, false) as Button
		if button != null:
			button.text = "%s: %s" % [action, OS.get_keycode_string(int(settings.keys[action]))]



func _capture_key(action: String) -> void:
	waiting_key = action
	var button := settings_screen.find_child("Key_" + action, true, false) as Button
	button.text = action + ": ..."

func _input(event: InputEvent) -> void:
	if waiting_key.is_empty() or not event is InputEventKey or not event.pressed:
		return
	var conflict: String = settings.rebind(waiting_key, event.keycode)
	if not conflict.is_empty():
		game._notice(_word("Taste bereits belegt: ", "Key already bound: ") + conflict)
	waiting_key = ""
	_refresh_keys()
	get_viewport().set_input_as_handled()

func _change_setting(key: String, value: Variant) -> void:
	settings.values[key] = value
	if key in ["resolution", "window_mode"]:
		settings.values["window_configured"] = true
	settings.save()
	# Apply only the edited option; never resize/rebuild the UI on slider movement.
	settings.apply(key)
	game.apply_settings(key)
	if key == "language":
		pending_language = true


func open_settings() -> void:
	settings_screen.visible = true


func close_settings() -> void:
	waiting_key = ""
	settings_screen.visible = false
	game.apply_settings("ui_scale_commit")
	if pending_language:
		pending_language = false
		game.call_deferred("_rebuild_interface_language")

