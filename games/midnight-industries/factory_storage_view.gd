extends Control
## Data-driven global build inventory. Items come from the catalog, actual
## economy stock and construction recipes; none are hardcoded into the UI.
## Individual material textures can be overridden in item_catalog.gd.
const Catalog = preload("res://item_catalog.gd")
const Economy = preload("res://factory_economy.gd")
const MenuTheme = preload("res://factory_menu_theme.gd")
const UiSkin = preload("res://factory_skin.gd")

var model
var german := false
var main_panel: PanelContainer
var item_rows: VBoxContainer
var costs_rows: VBoxContainer
var search_field: LineEdit
var category_menu: OptionButton
var total_label: Label
var depot_label: Label
var voucher_label: Label
var empty_label: Label
var quantities: Dictionary = {}
var availability: Dictionary = {}
var selected_category := "all"
var _known_ids := ""
var _refresh_elapsed := 0.0

func _ready() -> void:
	name = "GlobalConstructionInventory"
	mouse_filter = Control.MOUSE_FILTER_STOP
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_build()
	get_viewport().size_changed.connect(_resize)
	_resize()
	visible = false

func _word(de: String, en: String) -> String:
	return de if german else en

func _label(parent: Node, value: String, font_size: int = 14,
	color: Color = MenuTheme.TEXT) -> Label:
	var node := Label.new()
	node.text = value
	MenuTheme.label(node, font_size, color)
	parent.add_child(node)
	return node

func _panel(parent: Node, kind: String = "section") -> PanelContainer:
	var node := PanelContainer.new()
	node.add_theme_stylebox_override("panel", MenuTheme.panel(kind))
	parent.add_child(node)
	return node

func _stack(parent: Node, gap: int = 10) -> VBoxContainer:
	var column := VBoxContainer.new()
	column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	column.add_theme_constant_override("separation", gap)
	parent.add_child(column)
	return column

func _button(parent: Node, title: String, fn: Callable) -> Button:
	var b := Button.new()
	b.text = title
	MenuTheme.style_button(b)
	b.custom_minimum_size.y = 40
	b.pressed.connect(fn)
	parent.add_child(b)
	return b

func _divider(parent: Node) -> void:
	var line := ColorRect.new()
	line.color = Color("#547080", 0.48)
	line.custom_minimum_size.y = 1
	line.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(line)

func _build() -> void:
	var veil := ColorRect.new()
	veil.color = Color("#031019", 0.94)
	veil.mouse_filter = Control.MOUSE_FILTER_STOP
	add_child(veil)
	veil.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var center := CenterContainer.new()
	center.mouse_filter = Control.MOUSE_FILTER_PASS
	add_child(center)
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	main_panel = _panel(center, "main")
	main_panel.name = "InventoryMainPanel"
	var shell := _stack(main_panel, 14)
	var strip := ColorRect.new()
	strip.color = MenuTheme.CYAN
	strip.custom_minimum_size.y = 3
	shell.add_child(strip)
	var head := HBoxContainer.new()
	head.add_theme_constant_override("separation", 18)
	shell.add_child(head)
	var titles := _stack(head, 5)
	titles.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_label(titles, "MIDNIGHT  /  INDUSTRIES", 12, MenuTheme.AMBER)
	_label(titles, _word("GLOBALES BAULAGER", "GLOBAL BUILD INVENTORY"),
		28, MenuTheme.TEXT)
	_label(titles, _word(
		"Physisch angelieferte Materialien stehen überall zum Bauen bereit.",
		"Materials delivered to depots are available for construction anywhere."),
		13, MenuTheme.MUTED)
	var close := _button(head, "×", close_inventory)
	close.name = "CloseInventory"
	close.custom_minimum_size = Vector2(46, 43)
	close.add_theme_font_size_override("font_size", 23)
	_divider(shell)
	var body := HBoxContainer.new()
	body.size_flags_vertical = Control.SIZE_EXPAND_FILL
	body.add_theme_constant_override("separation", 16)
	shell.add_child(body)
	var left := _panel(body)
	left.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	left.size_flags_stretch_ratio = 1.35
	var left_col := _stack(left, 11)
	var left_top := HBoxContainer.new()
	left_col.add_child(left_top)
	_label(left_top, _word("MATERIALIEN", "MATERIALS"), 16, MenuTheme.AMBER)
	var filler := Control.new()
	filler.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	left_top.add_child(filler)
	total_label = _label(left_top, "", 12, MenuTheme.MUTED)
	var tools := HBoxContainer.new()
	tools.add_theme_constant_override("separation", 9)
	left_col.add_child(tools)
	search_field = LineEdit.new()
	search_field.name = "InventorySearch"
	search_field.placeholder_text = _word("Material suchen …", "Search materials …")
	search_field.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	search_field.custom_minimum_size.y = 42
	MenuTheme.style_input(search_field)
	tools.add_child(search_field)
	search_field.text_changed.connect(func(_v: String) -> void: _rebuild_materials())
	category_menu = OptionButton.new()
	category_menu.name = "InventoryCategory"
	category_menu.custom_minimum_size.x = 154
	MenuTheme.style_option(category_menu)
	tools.add_child(category_menu)
	category_menu.item_selected.connect(func(i: int) -> void:
		selected_category = str(category_menu.get_item_metadata(i))
		_rebuild_materials())
	_divider(left_col)
	var scroll := ScrollContainer.new()
	scroll.name = "InventoryMaterialsScroll"
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	left_col.add_child(scroll)
	item_rows = _stack(scroll, 8)
	empty_label = _label(left_col, _word("Keine Materialien gefunden.", "No matching materials."),
		14, MenuTheme.MUTED)
	empty_label.visible = false

	var right := _panel(body)
	right.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	right.size_flags_stretch_ratio = 0.96
	var right_col := _stack(right, 12)
	_label(right_col, _word("MASCHINEN & BAUKOSTEN", "MACHINES & BUILD COSTS"),
		16, MenuTheme.AMBER)
	_label(right_col, _word(
		"Die Kosten werden beim Platzieren vollständig abgezogen.",
		"Full material costs are deducted when a machine is placed."),
		12, MenuTheme.MUTED)
	_divider(right_col)
	var cost_scroll := ScrollContainer.new()
	cost_scroll.name = "InventoryCostsScroll"
	cost_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	cost_scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	right_col.add_child(cost_scroll)
	costs_rows = _stack(cost_scroll, 9)
	_divider(right_col)
	depot_label = _label(right_col, "", 14, MenuTheme.CYAN)
	voucher_label = _label(right_col, "", 12, MenuTheme.AMBER)
	voucher_label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	var foot := HBoxContainer.new()
	shell.add_child(foot)
	_label(foot, _word("I / ESC SCHLIESSEN", "I / ESC CLOSE"),
		12, MenuTheme.MUTED)
	var foot_space := Control.new()
	foot_space.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	foot.add_child(foot_space)
	_label(foot, _word("GLOBALES LAGER  ·  KEINE KAPAZITÄTSGRENZE",
		"SHARED STORAGE  ·  NO CAPACITY LIMIT"), 12, MenuTheme.MUTED)

func _resize() -> void:
	if main_panel == null:
		return
	var viewport := get_viewport_rect().size
	main_panel.custom_minimum_size = Vector2(
		minf(1200.0, maxf(660.0, viewport.x - 82.0)),
		minf(795.0, maxf(460.0, viewport.y - 74.0)))

func open_inventory() -> void:
	visible = true
	_known_ids = ""
	_refresh_elapsed = 0.0
	_refresh_categories()
	_rebuild_materials()
	_rebuild_costs()
	_refresh_live()

func close_inventory() -> void:
	visible = false

func _ids() -> Array[String]:
	# Catalog entries appear even at zero stock. Future items in saves or
	# recipes remain visible even if no icon metadata was authored yet.
	var found: Dictionary = {}
	for entry in Catalog.ITEMS:
		found[str(entry.get("id", ""))] = true
	if model != null:
		for key in model.economy.stock:
			found[str(key)] = true
	for kind in Economy.COSTS:
		for key in Economy.COSTS[kind]:
			found[str(key)] = true
	found.erase("")
	var ids: Array[String] = []
	for key in found:
		ids.append(str(key))
	ids.sort_custom(func(a: String, b: String) -> bool:
		var order := {"ingot":0, "ore":1, "component":2}
		var cat_a := str(Catalog.item(a).get("category", "component"))
		var cat_b := str(Catalog.item(b).get("category", "component"))
		var ra: int = int(order.get(cat_a, 9))
		var rb: int = int(order.get(cat_b, 9))
		return a < b if ra == rb else ra < rb)
	return ids

func _material_name(id: String) -> String:
	var entry: Dictionary = Catalog.item(id)
	if entry.is_empty():
		return id.replace("_", " ").capitalize()
	return Catalog.name_for(id, german)

func _category_name(category: String) -> String:
	match category:
		"ore": return _word("Roherz", "Raw ore")
		"ingot": return _word("Metallbarren", "Metal ingot")
		"component": return _word("Bauteil", "Component")
	return category.replace("_", " ").capitalize()

func _refresh_categories() -> void:
	category_menu.clear()
	category_menu.add_item(_word("Alle Typen", "All types"))
	category_menu.set_item_metadata(0, "all")
	var categories: Dictionary = {}
	for id in _ids():
		categories[str(Catalog.item(id).get("category", "component"))] = true
	var names: Array[String] = []
	for key in categories:
		names.append(str(key))
	names.sort()
	for category in names:
		category_menu.add_item(_category_name(category))
		category_menu.set_item_metadata(category_menu.item_count - 1, category)
	var found := false
	for index in range(category_menu.item_count):
		if str(category_menu.get_item_metadata(index)) == selected_category:
			category_menu.select(index)
			found = true
			break
	if not found:
		selected_category = "all"
		category_menu.select(0)

func _item_icon(id: String, edge: int) -> TextureRect:
	var icon := TextureRect.new()
	icon.texture = Catalog.icon(id)
	icon.custom_minimum_size = Vector2(edge, edge)
	icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
	icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
	icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
	return icon

func _rebuild_materials() -> void:
	if item_rows == null:
		return
	for child in item_rows.get_children():
		child.queue_free()
	quantities.clear()
	var ids: Array[String] = _ids()
	_known_ids = "|".join(ids)
	var search := search_field.text.strip_edges().to_lower()
	var count := 0
	for id in ids:
		var entry: Dictionary = Catalog.item(id)
		var category: String = str(entry.get("category", "component"))
		if selected_category != "all" and selected_category != category:
			continue
		var name: String = _material_name(id)
		if not search.is_empty() and not search in name.to_lower() \
			and not search in id.to_lower():
			continue
		var card := _panel(item_rows)
		card.name = "Material_" + id
		var row := HBoxContainer.new()
		row.add_theme_constant_override("separation", 12)
		row.custom_minimum_size.y = 61
		card.add_child(row)
		row.add_child(_item_icon(id, 52))
		var labels := _stack(row, 3)
		labels.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		_label(labels, name, 15, MenuTheme.TEXT)
		_label(labels, _category_name(category), 11, MenuTheme.MUTED)
		var amount := _label(row, "0", 21, MenuTheme.CYAN)
		amount.custom_minimum_size.x = 100
		amount.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
		amount.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
		quantities[id] = amount
		count += 1
	empty_label.visible = count == 0
	total_label.text = _word("%d Materialien", "%d materials") % ids.size()
	_refresh_live()

func _machine_ids() -> Array[String]:
	var ids: Array[String] = []
	for entry in Catalog.MACHINES:
		var id: String = str(entry.get("id", ""))
		if Economy.COSTS.has(id) and not ids.has(id):
			ids.append(id)
	for key in Economy.COSTS:
		if not ids.has(str(key)):
			ids.append(str(key))
	return ids

func _rebuild_costs() -> void:
	for child in costs_rows.get_children():
		child.queue_free()
	availability.clear()
	for kind in _machine_ids():
		var entry: Dictionary = Catalog.machine(kind)
		var name: String = str(entry.get("de" if german else "en",
			kind.replace("_", " ").capitalize()))
		var card := _panel(costs_rows, "section")
		var col := _stack(card, 7)
		var row := HBoxContainer.new()
		col.add_child(row)
		var title := _label(row, name, 14, MenuTheme.TEXT)
		title.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		var state := _label(row, "", 11, MenuTheme.CYAN)
		availability[kind] = state
		var costs: Dictionary = model.building_cost(kind)
		if costs.is_empty():
			_label(col, _word("Kostenlos platzierbar", "Free to build"),
				12, MenuTheme.CYAN)
		for material in costs:
			var requirement := HBoxContainer.new()
			requirement.add_theme_constant_override("separation", 9)
			col.add_child(requirement)
			requirement.add_child(_item_icon(str(material), 27))
			var text_name := _label(requirement, _material_name(str(material)),
				12, MenuTheme.MUTED)
			text_name.size_flags_horizontal = Control.SIZE_EXPAND_FILL
			var quantity := _label(requirement, "%d" % int(costs[material]),
				13, MenuTheme.TEXT)
			quantity.name = "Cost_" + str(material)
		var status := _label(col, "", 12, MenuTheme.MUTED)
		status.name = "CostStatus_" + kind
		availability[kind + "_status"] = status

func _refresh_live() -> void:
	if not visible or model == null:
		return
	for id in quantities:
		var label: Label = quantities[id]
		if is_instance_valid(label):
			label.text = "%d" % model.economy.available(str(id))
	model._update_power()
	for kind in _machine_ids():
		if not availability.has(kind):
			continue
		var unlocked: bool = model.is_unlocked(kind)
		var allowed: bool = unlocked and (
			kind != "water_wheel" or model.power.can_add_generator(kind))
		var state: Label = availability[kind]
		if not unlocked:
			state.text = _word("GESPERRT", "LOCKED")
		elif not allowed:
			state.text = _word("SLOTS VOLL", "SLOTS FULL")
		else:
			state.text = _word("BAUBAR", "READY")
		state.add_theme_color_override("font_color",
			MenuTheme.CYAN if allowed else MenuTheme.RED)
		var status: Label = availability[kind + "_status"]
		if not unlocked:
			status.text = _word("Schalte Technologie über einen Job frei",
				"Unlock through progression Jobs")
		elif kind == "water_wheel":
			status.text = _word("30 kW · 1 Slot · Land und Flussfeld",
				"30 kW · 1 slot · land and river tiles")
		elif kind == "smelter":
			status.text = _word("10 kW Verbrauch · Stromanschluss nötig",
				"10 kW demand · requires power network")
		elif kind == "constructor":
			status.text = _word("15 kW · Eisenplatten / Kupferplatten",
				"15 kW · Iron plates / Copper plates")
		elif kind == "power_pole":
			status.text = _word("0 kW · Reichweite 5 Felder",
				"0 kW · 5-tile link range")
		else:
			status.text = _word("Keine Materialkosten", "No material costs")

	depot_label.text = _word("DEPOTS VERBUNDEN: %d", "CONNECTED DEPOTS: %d") % \
		int(model.counts().get("depot", 0))
	model._update_power()
	voucher_label.text = _word("Stromslots: %d/%d · Strom: %d/%d kW",
		"Power slots: %d/%d · Power: %d/%d kW") % [
		model.power.slot_used, model.power.slot_limit,
		model.power.total_powered, model.power.total_supply]

func _process(delta: float) -> void:
	if not visible or model == null:
		return
	_refresh_elapsed += delta
	if _refresh_elapsed < 0.16:
		return
	_refresh_elapsed = 0.0
	var ids := "|".join(_ids())
	if ids != _known_ids:
		_refresh_categories()
		_rebuild_materials()
		_rebuild_costs()
	_refresh_live()
