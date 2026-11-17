extends Control
## Global, uncapped build stock. Warehouse contents are deliberately separate.
const Catalog = preload("res://item_catalog.gd")
const Economy = preload("res://factory_economy.gd")
const UiSkin = preload("res://factory_skin.gd")
var model
var german := true
var search: LineEdit
var category: OptionButton
var list: VBoxContainer
var details: Label
var materials_scroll: ScrollContainer
var costs_scroll: ScrollContainer
var cost_list: VBoxContainer
var selected_tab := "stock"
var _last_stock := ""
var _selected := ""

func _word(de: String, en: String) -> String:
	return de if german else en

func _ready() -> void:
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	mouse_filter = Control.MOUSE_FILTER_STOP
	visible = false
	var bg := ColorRect.new()
	bg.color = Color("#07141D", 0.98)
	add_child(bg)
	bg.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	bg.mouse_filter = Control.MOUSE_FILTER_IGNORE
	var center := CenterContainer.new()
	add_child(center)
	center.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	var frame := PanelContainer.new()
	frame.custom_minimum_size = Vector2(830, 580)
	frame.add_theme_stylebox_override("panel", UiSkin.panel("menu"))
	center.add_child(frame)
	var col := VBoxContainer.new()
	frame.add_child(col)
	_label(col, _word("MIDNIGHT INDUSTRIES / GLOBALES BAULAGER", "MIDNIGHT INDUSTRIES / GLOBAL BUILD STOCK"), 22)
	_label(col, _word("Lieferungen an Depots finanzieren Anlagen. Zwischenlager zählen nicht zum globalen Bestand.",
		"Deliveries to depots fund construction. Warehouse contents are separate."), 12)
	var tabs := HBoxContainer.new()
	col.add_child(tabs)
	_button(tabs, _word("Materialien", "Materials"), _show_tab.bind("stock"))
	_button(tabs, _word("Baukosten", "Build costs"), _show_tab.bind("costs"))
	_button(tabs, _word("Schließen [I]", "Close [I]"), close_inventory)
	var controls := HBoxContainer.new()
	controls.name = "MaterialControls"
	col.add_child(controls)
	search = LineEdit.new()
	search.placeholder_text = "Material suchen / Search material"
	search.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	controls.add_child(search)
	search.text_changed.connect(func(_query: String) -> void: _refresh())
	category = OptionButton.new()
	for entry in ["Alle / All", "Erze / Ores", "Barren / Ingots"]:
		category.add_item(entry)
	category.item_selected.connect(func(_index: int) -> void: _refresh())
	controls.add_child(category)
	materials_scroll = ScrollContainer.new()
	materials_scroll.custom_minimum_size.y = 295
	materials_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	col.add_child(materials_scroll)
	list = VBoxContainer.new()
	list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	materials_scroll.add_child(list)
	costs_scroll = ScrollContainer.new()
	costs_scroll.custom_minimum_size.y = 295
	costs_scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	col.add_child(costs_scroll)
	cost_list = VBoxContainer.new()
	cost_list.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	costs_scroll.add_child(cost_list)
	details = _label(col, "", 14)
	details.custom_minimum_size.y = 75
	_show_tab("stock")

func _show_tab(tab: String) -> void:
	selected_tab = tab
	if materials_scroll == null:
		return
	materials_scroll.visible = tab == "stock"
	details.visible = tab == "stock"
	search.get_parent().visible = tab == "stock"
	costs_scroll.visible = tab == "costs"
	_refresh()

func _label(parent: Node, title: String, font_size: int) -> Label:
	var l := Label.new()
	l.text = title
	l.add_theme_font_override("font", UiSkin.font())
	l.add_theme_font_size_override("font_size", font_size)
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	parent.add_child(l)
	return l

func _button(parent: Node, title: String, action: Callable) -> Button:
	var b := Button.new()
	b.text = title
	b.custom_minimum_size.y = 39
	b.add_theme_stylebox_override("normal", UiSkin.button("button"))
	b.add_theme_stylebox_override("hover", UiSkin.button("button_active"))
	b.pressed.connect(action)
	parent.add_child(b)
	return b

func open_inventory() -> void:
	visible = true
	_refresh()

func close_inventory() -> void:
	visible = false

func _process(_delta: float) -> void:
	if visible and model != null:
		var stock: String = JSON.stringify(model.economy.stock)
		if stock != _last_stock:
			_refresh()

func _refresh() -> void:
	if model == null or list == null:
		return
	_last_stock = JSON.stringify(model.economy.stock)
	if selected_tab == "costs":
		_refresh_costs()
		return
	for child in list.get_children():
		child.queue_free()
	for item in Catalog.ITEMS:
		if category.selected == 1 and item.category != "ore":
			continue
		if category.selected == 2 and item.category != "ingot":
			continue
		var name: String = str(item.de if german else item.en)
		if not search.text.is_empty() and not search.text.to_lower() in name.to_lower():
			continue
		var id: String = item.id
		var row := HBoxContainer.new()
		list.add_child(row)
		var b := _button(row, "%s   %s   ·   %d" % [item.mark, name, model.economy.available(id)],
			_show_details.bind(id))
		b.icon = Catalog.icon(id)
		b.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_show_details(_selected)

func _refresh_costs() -> void:
	for child in cost_list.get_children():
		child.queue_free()
	for machine in Catalog.MACHINES:
		var id: String = machine.id
		var needed: Array[String] = []
		for material in Economy.COSTS[id]:
			var amount: int = int(Economy.COSTS[id][material])
			var available: int = model.economy.available(material)
			needed.append("%s %d/%d" % [Catalog.name_for(material, german), available, amount])
		var name: String = str(machine.de if german else machine.en)
		var text_value := name + " · " + "  ·  ".join(needed)
		if model.economy.has_free_kit(id) or (id == "depot" and model.economy.free_depot_available):
			text_value += _word("  ·  Umzugs-Kit/Gratis", "  ·  Free relocation kit")
		elif not model.economy.has_funds(id):
			text_value += _word("  ·  FEHLT", "  ·  MISSING")
		var row := _button(cost_list, text_value, func() -> void: pass)
		row.icon = Catalog.icon(str(Economy.COSTS[id].keys()[0]))
		row.disabled = not model.construction.is_machine_unlocked(id)

func _show_details(id: String) -> void:
	_selected = id
	if id.is_empty():
		details.text = ("%d Depots · Globales Lager ohne Kapazitätsgrenze\n" % int(model.counts().get("depot", 0))) + \
			("Baukosten werden beim Abriss bezahlter Anlagen vollständig erstattet." if german else
			"Paid construction costs are fully refunded on demolition.")
		return
	var info: Dictionary = Catalog.item(id)
	var costs := []
	for machine in Economy.COSTS:
		if Economy.COSTS[machine].has(id):
			costs.append("%s %d" % [machine, Economy.COSTS[machine][id]])
	details.text = "%s · %s · %d\n%s" % [Catalog.name_for(id, german),
		("Erz" if german else "Ore") if info.category == "ore" else ("Barren" if german else "Ingot"),
		model.economy.available(id),
		("Baukosten: " if german else "Build costs: ") + (", ".join(costs) if not costs.is_empty() else "—")]
