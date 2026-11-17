extends Control
## Menu and data-driven HUD controls; world actions delegate to the game.
const Catalog = preload("res://item_catalog.gd")
const UiSkin = preload("res://factory_skin.gd")
const COLORS := {"ink":Color("#0E1C25"), "text":Color("#ECF2F0"),
	"muted":Color("#A5BDC3"), "accent":Color("#F1BD57")}
var game: Node
var manager
var settings
var start_screen: Control
var settings_screen: Control
var pin_panel: Control
var pin_row: HBoxContainer
var pin_button: Button
var picker: Node
var dock: Control
var machine_row: HBoxContainer
var category_buttons: Dictionary = {}
var open_category := ""
var pins: Array = ["iron_ingot", "copper_ingot", "", "", "", "", "", ""]
var pin_open := false
var edit_pin := -1
var selection := ""
var name_edit: LineEdit
var world_list: VBoxContainer
var info: Label
var _stock_hash := ""
var notice_label: Label
var notice_tween: Tween
var click_player: AudioStreamPlayer

func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	_build_click_sound()
	_build_start()
	_build_dock()
	_build_pins()
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
		var title := "%s  ·  %s  ·  %02d/12  ·  %s" % [str(world.get("name", "?")), stamp,
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
	pins = record.view.get("pins", ["iron_ingot", "copper_ingot", "", "", "", "", "", ""]).duplicate()
	_refresh_pins()
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
	_close_picker()

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

func _build_dock() -> void:
	dock = _panel(self, "BottomConstructionDock")
	dock.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	dock.offset_left = 14
	dock.offset_right = -14
	dock.offset_top = -128
	dock.offset_bottom = -10
	var stack := VBoxContainer.new()
	dock.add_child(stack)
	notice_label = _label(self, "", 12, COLORS.accent)
	notice_label.visible = false
	notice_label.set_anchors_preset(Control.PRESET_BOTTOM_LEFT)
	notice_label.offset_left = 22
	notice_label.offset_top = -154
	notice_label.offset_right = 720
	notice_label.offset_bottom = -137
	var scroll := ScrollContainer.new()
	scroll.custom_minimum_size.y = 54
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	stack.add_child(scroll)
	machine_row = HBoxContainer.new()
	scroll.add_child(machine_row)
	var categories := HBoxContainer.new()
	stack.add_child(categories)
	_button(categories, _word("Auswahl [V]", "Select [V]"), func() -> void:
		game._set_mode("inspect")
		_select_category(""))
	for category in ["mining", "logistics", "production"]:
		var title: String = {"mining":_word("Abbau", "Mining"),
			"logistics":_word("Logistik", "Logistics"),
			"production":_word("Produktion", "Production")}[category]
		category_buttons[category] = _button(categories, title, _select_category.bind(category))
	_select_category("")

func _select_category(category: String) -> void:
	open_category = "" if open_category == category else category
	for child in machine_row.get_children():
		child.queue_free()
	for entry in Catalog.MACHINES:
		if entry.category != open_category:
			continue
		var id: String = entry.id
		var cost: String = game._cost_text(id)
		var name: String = str(entry.de if game.german else entry.en)
		var button := _button(machine_row, "%d  %s\n%s" % [entry.key, name, cost], game._select_tool.bind(id))
		button.icon = game._machine_icon(id)
		button.tooltip_text = name + " · " + cost
		button.disabled = not game.model.construction.is_machine_unlocked(id)
		button.custom_minimum_size.x = 190
		button.custom_minimum_size.y = 53
	machine_row.get_parent().visible = not open_category.is_empty()
	dock.offset_top = -128 if not open_category.is_empty() else -76
	if not bool(settings.values.reduced_motion) and not open_category.is_empty():
		machine_row.modulate.a = 0.0
		var reveal := create_tween()
		reveal.tween_property(machine_row, "modulate:a", 1.0, 0.16)

func select_tool(id: String) -> void:
	var entry: Dictionary = Catalog.machine(id)
	if not entry.is_empty() and open_category != entry.category:
		_select_category(str(entry.category))

func _build_pins() -> void:
	pin_panel = _panel(self, "DepotQuickPins")
	pin_panel.set_anchors_preset(Control.PRESET_TOP_LEFT)
	pin_panel.offset_left = 15
	pin_panel.offset_top = 74
	pin_panel.offset_right = 790
	pin_panel.offset_bottom = 135
	var col := VBoxContainer.new()
	pin_panel.add_child(col)
	pin_button = _button(col, _word("▾  MATERIALÜBERSICHT", "▾  MATERIALS"), _toggle_pins)
	pin_row = HBoxContainer.new()
	col.add_child(pin_row)
	_refresh_pins()

func set_world_ui(on: bool) -> void:
	dock.visible = on
	pin_panel.visible = on

func show_notice(message: String) -> void:
	if notice_label == null:
		return
	notice_label.text = message
	notice_label.visible = true
	if notice_tween != null and notice_tween.is_running():
		notice_tween.kill()
	notice_label.modulate.a = 1.0
	if not bool(settings.values.reduced_motion):
		notice_tween = create_tween()
		notice_tween.tween_interval(2.5)
		notice_tween.tween_property(notice_label, "modulate:a", 0.0, 0.4)

func _toggle_pins() -> void:
	pin_open = not pin_open
	_refresh_pins()

func _refresh_pins() -> void:
	if pin_row == null:
		return
	pin_row.visible = pin_open
	pin_panel.offset_bottom = 148 if pin_open else 121
	for child in pin_row.get_children():
		child.queue_free()
	for i in range(pins.size()):
		var id: String = str(pins[i])
		var amount: int = game.model.economy.available(id) if not id.is_empty() else 0
		var b := _button(pin_row, (Catalog.name_for(id, game.german) + "\n%d" % amount) if not id.is_empty() else "+", _choose_pin.bind(i))
		b.custom_minimum_size.x = 88
		b.tooltip_text = _word("Klicken zum Ändern", "Click to change")
		if not id.is_empty():
			b.icon = Catalog.icon(id)

func update_stock() -> void:
	var next: String = JSON.stringify(game.model.economy.stock)
	if next != _stock_hash:
		_stock_hash = next
		_refresh_pins()

func _choose_pin(index: int) -> void:
	edit_pin = index
	_close_picker()
	var pop := PopupPanel.new()
	pop.name = "MaterialPicker"
	add_child(pop)
	picker = pop
	var column := VBoxContainer.new()
	pop.add_child(column)
	var search := LineEdit.new()
	search.placeholder_text = _word("Material suchen", "Search material")
	column.add_child(search)
	var list := VBoxContainer.new()
	column.add_child(list)
	var filter := OptionButton.new()
	for title in [_word("Alle", "All"), _word("Erze", "Ores"), _word("Barren", "Ingots")]:
		filter.add_item(title)
	column.add_child(filter)
	search.text_changed.connect(func(query: String) -> void: _fill_picker(list, query, filter.selected))
	filter.item_selected.connect(func(index: int) -> void: _fill_picker(list, search.text, index))
	_fill_picker(list, "", 0)
	pop.popup_centered(Vector2i(350, 360))
	search.grab_focus()

func _fill_picker(list: VBoxContainer, query: String, category: int) -> void:
	for child in list.get_children():
		child.queue_free()
	_button(list, _word("Slot leeren", "Clear slot"), _set_pin.bind(""))
	for entry in Catalog.ITEMS:
		if category == 1 and entry.category != "ore":
			continue
		if category == 2 and entry.category != "ingot":
			continue
		var name: String = str(entry.de if game.german else entry.en)
		if query.is_empty() or query.to_lower() in name.to_lower():
			var button := _button(list, name, _set_pin.bind(str(entry.id)))
			button.icon = Catalog.icon(str(entry.id))

func _set_pin(id: String) -> void:
	if edit_pin >= 0 and edit_pin < pins.size():
		pins[edit_pin] = id
		_refresh_pins()
	_close_picker()

func _close_picker() -> void:
	if is_instance_valid(picker):
		picker.queue_free()
	picker = null

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

var waiting_key := ""
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
	settings.save()
	settings.apply()
	game.apply_settings()

func open_settings() -> void:
	settings_screen.visible = true

func close_settings() -> void:
	waiting_key = ""
	settings_screen.visible = false
