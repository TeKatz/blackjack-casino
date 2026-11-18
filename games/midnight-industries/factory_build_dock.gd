extends Control
## Compact Shapez-inspired two-tier dock. No machine lists are hardcoded:
## factory_build_catalog.gd and item_catalog.gd own its metadata.
signal inspect_requested
signal build_mode_requested
signal machine_requested(kind: String)

const Catalog = preload("res://factory_build_catalog.gd")
const ItemCatalog = preload("res://item_catalog.gd")
const MenuTheme = preload("res://factory_menu_theme.gd")
const Icons = preload("res://factory_ui_icons.gd")

var model
var german := false
var reduced_motion := false
var inspecting := true
var selected_machine := "belt"
var selected_category := "logistics"
var icon_provider: Callable
var category_panel: PanelContainer
var machine_panel: PanelContainer
var category_row: HBoxContainer
var machine_row: HBoxContainer
var category_buttons: Dictionary = {}
var machine_buttons: Dictionary = {}
var machine_subtitles: Dictionary = {}
var _material_states: Dictionary = {}
var _open := false
var _transition: Tween

func _ready() -> void:
	name = "FactoryBuildDock"
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	offset_top = -192
	offset_bottom = 0
	_build()
	get_viewport().size_changed.connect(_layout)
	_layout()
	_sync_style()

func _word(de: String, en: String) -> String:
	return de if german else en

func _build() -> void:
	machine_panel = PanelContainer.new()
	machine_panel.name = "BuildMachineShelf"
	# Floating machine cards: no large opaque shelf, border or shadow.
	machine_panel.add_theme_stylebox_override("panel", StyleBoxEmpty.new())
	machine_panel.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(machine_panel)
	var machines_scroll := ScrollContainer.new()
	machines_scroll.name = "BuildMachineScroll"
	machines_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	machines_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	machines_scroll.follow_focus = true
	machine_panel.add_child(machines_scroll)
	machine_row = HBoxContainer.new()
	machine_row.add_theme_constant_override("separation", 10)
	machine_row.alignment = BoxContainer.ALIGNMENT_CENTER
	machine_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	machines_scroll.add_child(machine_row)
	category_panel = PanelContainer.new()
	category_panel.name = "BuildCategoryBar"
	# Categories themselves are the visual controls, not a framed container.
	category_panel.add_theme_stylebox_override("panel", StyleBoxEmpty.new())
	category_panel.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(category_panel)
	var category_scroll := ScrollContainer.new()
	category_scroll.name = "BuildCategoriesScroll"
	category_scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	category_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	category_panel.add_child(category_scroll)
	category_row = HBoxContainer.new()
	category_row.add_theme_constant_override("separation", 11)
	category_row.alignment = BoxContainer.ALIGNMENT_CENTER
	category_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	category_scroll.add_child(category_row)
	_build_categories()
	_build_machines()
	machine_panel.visible = false

func _layout() -> void:
	if machine_panel == null or category_panel == null:
		return
	var width: float = get_viewport_rect().size.x
	var dock_width: float = minf(1020.0, maxf(480.0, width - 28.0))
	var left: float = (width - dock_width) * 0.5
	category_panel.position = Vector2(left, 112.0)
	category_panel.size = Vector2(dock_width, 60.0)
	machine_panel.position = Vector2(left, 14.0 if _open else 28.0)
	machine_panel.size = Vector2(dock_width, 85.0)

func _new_button(parent: Node, name: String, width: float, fn: Callable) -> Button:
	var button := Button.new()
	button.name = name
	button.custom_minimum_size = Vector2(width, 43)
	MenuTheme.style_button(button)
	button.pressed.connect(fn)
	parent.add_child(button)
	return button

func _build_categories() -> void:
	for node in category_row.get_children():
		category_row.remove_child(node)
		node.queue_free()
	category_buttons.clear()
	var inspect := _new_button(category_row, "Category_inspect", 124.0,
		func() -> void: inspect_requested.emit())
	inspect.icon = Icons.icon("inspect")
	inspect.text = _word("Auswahl", "Select")
	category_buttons["inspect"] = inspect
	for category in Catalog.categories():
		var category_id: String = str(category.id)
		var btn := _new_button(category_row, "Category_" + category_id, 136.0,
			_activate_category.bind(category_id))
		btn.icon = Icons.icon(str(category.get("icon", "special")))
		btn.text = str(category.get("de" if german else "en", category_id))
		category_buttons[category_id] = btn

func _activate_category(category: String) -> void:
	selected_category = category
	var options: Array[Dictionary] = Catalog.machines(category)
	var chosen := ""
	# Never leave a belt selected when the player switches to Mining, or
	# allow accidental placement of an unrelated machine.
	for entry in options:
		if str(entry.id) == selected_machine and Catalog.unlocked(entry, model):
			chosen = selected_machine
			break
	if chosen.is_empty():
		for entry in options:
			if Catalog.unlocked(entry, model):
				chosen = str(entry.id)
				break
	if chosen.is_empty():
		# Browsing empty or fully locked future categories does not start
		# placing the machine selected in a different category.
		inspect_requested.emit()
		inspecting = false
		_build_machines()
		_sync_style()
		_set_shelf(true)
		return
	selected_machine = chosen
	inspecting = false
	_build_machines()
	_sync_style()
	_set_shelf(true)
	machine_requested.emit(chosen)

func set_inspecting() -> void:
	inspecting = true
	_sync_style()
	_set_shelf(false)

func set_building() -> void:
	inspecting = false
	_sync_style()
	_set_shelf(true)

func select_machine(kind: String) -> void:
	selected_machine = kind
	var category_changed := false
	for category in Catalog.categories():
		for entry in Catalog.machines(str(category.id)):
			if str(entry.id) == kind and selected_category != str(category.id):
				selected_category = str(category.id)
				category_changed = true
				break
	inspecting = false
	if category_changed:
		_build_machines()
	_sync_style()
	_set_shelf(true)

func _choose_machine(kind: String) -> void:
	if model == null or not model.is_unlocked(kind):
		return
	selected_machine = kind
	_sync_style()
	machine_requested.emit(kind)

func _machine_icon(kind: String) -> Texture2D:
	if icon_provider.is_valid():
		return icon_provider.call(kind)
	return Icons.icon("production")

func _build_machines() -> void:
	for node in machine_row.get_children():
		machine_row.remove_child(node)
		node.queue_free()
	machine_buttons.clear()
	machine_subtitles.clear()
	_material_states.clear()
	var machines: Array[Dictionary] = Catalog.machines(selected_category)
	if machines.is_empty():
		var placeholder := Label.new()
		placeholder.text = _word(
			"Noch keine Maschinen dieser Kategorie · spätere Phasen ergänzen diesen Bereich",
			"No machines here yet · future phases will add new technology")
		MenuTheme.label(placeholder, 13, MenuTheme.MUTED)
		placeholder.custom_minimum_size = Vector2(640, 45)
		placeholder.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		machine_row.add_child(placeholder)
		return
	for entry in machines:
		var id: String = str(entry.id)
		var title: String = str(entry.get("de" if german else "en", id))
		var unlocked: bool = Catalog.unlocked(entry, model)
		var button := _new_button(machine_row, "Machine_" + id, 174,
			_choose_machine.bind(id))
		button.custom_minimum_size.y = 62
		button.text = ""
		button.disabled = not unlocked
		button.tooltip_text = _tooltip(entry, unlocked)
		var card := HBoxContainer.new()
		card.mouse_filter = Control.MOUSE_FILTER_IGNORE
		card.add_theme_constant_override("separation", 8)
		card.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
		card.offset_left = 9.0
		card.offset_right = -9.0
		card.offset_top = 6.0
		card.offset_bottom = -6.0
		button.add_child(card)
		var art := TextureRect.new()
		art.texture = _machine_icon(id) if unlocked else Icons.icon("lock")
		art.custom_minimum_size = Vector2(43, 43)
		art.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		art.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		art.mouse_filter = Control.MOUSE_FILTER_IGNORE
		card.add_child(art)
		var labels := VBoxContainer.new()
		labels.mouse_filter = Control.MOUSE_FILTER_IGNORE
		labels.alignment = BoxContainer.ALIGNMENT_CENTER
		labels.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		labels.add_theme_constant_override("separation", 3)
		card.add_child(labels)
		var label := Label.new()
		MenuTheme.label(label, 12, MenuTheme.TEXT if unlocked else MenuTheme.MUTED)
		label.text = title
		label.clip_text = true
		labels.add_child(label)
		var detail := Label.new()
		MenuTheme.label(detail, 11, MenuTheme.CYAN if unlocked else MenuTheme.MUTED)
		detail.clip_text = true
		labels.add_child(detail)
		machine_subtitles[id] = detail
		machine_buttons[id] = button
		if not unlocked:
			detail.text = _unlock_message(entry)
	_refresh_materials()

func _unlock_message(entry: Dictionary) -> String:
	if str(entry.get("unlock_job", "")) == "first_iron_delivery":
		return _word("Job 001 abschließen", "Complete job 001")
	return _word("Noch nicht verfügbar", "Future stage")

func _tooltip(entry: Dictionary, unlocked: bool) -> String:
	var title: String = str(entry.get("de" if german else "en", entry.id))
	if not unlocked:
		return title + "\n" + _unlock_message(entry)
	var cost: Dictionary = model.building_cost(str(entry.id))
	var parts: Array[String] = []
	for material in cost:
		parts.append("%s × %d" % [
			ItemCatalog.name_for(str(material), german), int(cost[material])])
	if str(entry.id) == "water_wheel":
		return title + "\n" + _word(
			"30 kW · 1 Slot · 1 Landfeld + 1 Flussfeld · R zum Drehen",
			"30 kW · 1 slot · 1 land + 1 river tile · R to rotate")
	if str(entry.id) == "power_pole":
		return title + "\n" + _word("0 kW · Reichweite 5 Felder",
			"0 kW · 5-tile connection range")
	return title + "\n" + (_word("Kostenlos", "Free") if parts.is_empty() else ", ".join(parts))

func _refresh_materials() -> void:
	if model == null:
		return
	for kind in machine_buttons:
		var button: Button = machine_buttons[kind]
		var entry: Dictionary = ItemCatalog.machine(str(kind))
		var unlocked: bool = Catalog.unlocked(entry, model)
		var selected: bool = unlocked and not inspecting and selected_machine == kind
		var affordable: bool = model.economy.has_funds(kind)
		if kind == "water_wheel":
			model._update_power()
			affordable = model.power.can_add_generator(kind)
		var state: String = "%d_%d_%d" % [
			int(unlocked), int(selected), int(affordable)]
		# A production update may arrive many times per second. Only touch
		# theme overrides if its real UI state actually changed.
		if _material_states.get(kind, "") != state:
			_material_states[kind] = state
			button.disabled = not unlocked
			MenuTheme.style_button(button, false, selected)
			if not unlocked:
				continue
			var subtitle_color: Label = machine_subtitles[kind]
			subtitle_color.add_theme_color_override("font_color",
				MenuTheme.CYAN if affordable else MenuTheme.RED)
		if not unlocked:
			continue
		var subtitle: Label = machine_subtitles[kind]
		var caption := ""
		if kind == "water_wheel":
			model._update_power()
			caption = "30 kW · " + _word("Slot ", "Slot ") + \
				"%d/%d" % [model.power.slot_used, model.power.slot_limit]
		elif kind == "power_pole":
			caption = _word("0 kW · kostenlos", "0 kW · free")
		else:
			var price: Dictionary = model.building_cost(kind)
			var parts: Array[String] = []
			for item in price:
				var abbr := "Fe" if str(item).begins_with("iron") else (
					"Cu" if str(item).begins_with("copper") else "Au")
				parts.append("%s %d" % [abbr, int(price[item])])
			caption = " · ".join(parts)
		if caption.is_empty():
			caption = _word("KOSTENLOS", "FREE")
		if subtitle.text != caption:
			subtitle.text = caption

func _sync_style() -> void:
	for category in category_buttons:
		var b: Button = category_buttons[category]
		var active: bool = category == ("inspect" if inspecting else selected_category)
		b.button_pressed = active
		MenuTheme.style_button(b, false, active)
	_refresh_materials()

func _set_shelf(should_open: bool) -> void:
	if _open == should_open and machine_panel.visible == should_open:
		return
	_open = should_open
	if _transition != null and _transition.is_running():
		_transition.kill()
	if should_open:
		machine_panel.visible = true
	if reduced_motion:
		machine_panel.modulate.a = 1.0 if should_open else 0.0
		machine_panel.position.y = 14.0 if should_open else 28.0
		machine_panel.visible = should_open
		return
	_transition = create_tween().set_parallel(true)
	_transition.tween_property(machine_panel, "modulate:a",
		1.0 if should_open else 0.0, 0.19)
	_transition.tween_property(machine_panel, "position:y",
		14.0 if should_open else 28.0, 0.19).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	if not should_open:
		_transition.chain().tween_callback(func() -> void:
			if not _open:
				machine_panel.visible = false)

func refresh() -> void:
	# No rebuilding every UI tick. Only rerender prices and disabled states.
	_refresh_materials()

func is_over(point: Vector2) -> bool:
	return category_panel.get_global_rect().has_point(point) or (
		machine_panel.visible and machine_panel.get_global_rect().has_point(point))
