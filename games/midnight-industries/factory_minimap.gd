extends Control
## Fully revealed cached atlas; overlays, markers and camera are independent.
const Model = preload("res://factory_model.gd")
const Construction = preload("res://factory_construction.gd")
const Catalog = preload("res://item_catalog.gd")
const UiSkin = preload("res://factory_skin.gd")
const MenuTheme = preload("res://factory_menu_theme.gd")
const MapSurface = preload("res://factory_map_surface.gd")
const TILE := 48.0
const DEFAULT_LAYERS := {"ore":true, "iron":true, "copper":true, "gold":true,
	"water":true, "buildings":true,
	"casino":true, "markers":true}
const ORE_COLOR := {"iron":Color("#96ADB6"), "copper":Color("#E0935B"),
	"gold":Color("#F5CB68")}
var model
var camera: Camera2D
var german := true
var markers: Array = []
var layers: Dictionary = DEFAULT_LAYERS.duplicate()
var _ground: Texture2D
var _ore: Texture2D
var _ore_layers: Dictionary = {}
var _water: Texture2D
var _map_rect := Rect2()
var _base_scale := 1.0
var zoom := 1.0
var pan := Vector2.ZERO
var dragging := false
var drag_distance := 0.0
var last_click := 0
var selected := -1
var marker_popup: PopupPanel
var marker_name: LineEdit
var marker_icon: OptionButton
var marker_color: OptionButton
var marker_cell := Vector2i.ZERO
var moving_marker := false
var reduced_motion := false
var animation_time := 0.0
var animation_accumulator := 0.0
var water_motion := true
var ore_centers: Array = []
var marker_list: VBoxContainer
var layer_row: VBoxContainer
var coordinates: Label
var zoom_label: Label
var _surface: Control
var _map_frame: PanelContainer
var _side_panel: PanelContainer

func _word(de: String, en: String) -> String:
	return de if german else en

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_STOP
	texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
	visible = false
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_build_controls()

func show_map() -> void:
	if _ground == null:
		_bake_geography()
	visible = true
	_refresh_controls()
	_redraw_map()

func hide_map() -> void:
	visible = false

func refresh_map() -> void:
	_ground = null
	_ore = null
	_ore_layers.clear()
	_water = null
	zoom = 1.0
	pan = Vector2.ZERO
	if visible:
		show_map()

func _process(delta: float) -> void:
	if not visible or reduced_motion or (selected < 0 and not water_motion):
		return
	animation_time += delta
	animation_accumulator += delta
	if animation_accumulator >= 0.10:
		animation_accumulator = 0.0
		_redraw_map()

func _bake_geography() -> void:
	if model == null:
		return
	var ground := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	var ore := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	var mineral_images := {}
	for mineral in ORE_COLOR:
		mineral_images[mineral] = Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	var water := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	for y in range(Model.HEIGHT):
		for x in range(Model.WIDTH):
			var cell := Vector2i(x, y)
			var grain := float(posmod(x * 73 + y * 179 + x * y * 3, 91)) / 91.0
			ground.set_pixel(x, y, Color("#263D32").lerp(Color("#334A39"), grain))
			if model.water.has(cell):
				var shore := false
				for adjacent in [Vector2i.RIGHT, Vector2i.LEFT, Vector2i.UP, Vector2i.DOWN]:
					if not model.water.has(cell + adjacent):
						shore = true
						break
				water.set_pixel(x, y, Color("#4A9AA3") if shore else (
					Color("#1D6379") if model.water[cell].kind == "river" else Color("#27748A")))
			if model.resources.has(cell):
				var mineral: String = str(model.resources[cell])
				var tint: Color = ORE_COLOR.get(mineral, Color.WHITE)
				ore.set_pixel(x, y, tint)
				mineral_images[mineral].set_pixel(x, y, tint)
	_ground = ImageTexture.create_from_image(ground)
	_ore = ImageTexture.create_from_image(ore)
	for mineral in mineral_images:
		_ore_layers[mineral] = ImageTexture.create_from_image(mineral_images[mineral])
	_water = ImageTexture.create_from_image(water)
	_find_ore_centers()

func _find_ore_centers() -> void:
	ore_centers.clear()
	var seen := {}
	for origin in model.resources:
		if seen.has(origin):
			continue
		var mineral: String = str(model.resources[origin])
		var frontier: Array = [origin]
		seen[origin] = true
		var sum := Vector2.ZERO
		var count := 0
		while not frontier.is_empty():
			var cell: Vector2i = frontier.pop_back()
			sum += Vector2(cell)
			count += 1
			for step in [Vector2i.UP, Vector2i.RIGHT, Vector2i.DOWN, Vector2i.LEFT]:
				var neighbor: Vector2i = cell + step
				if not seen.has(neighbor) and model.resources.get(neighbor, "") == mineral:
					seen[neighbor] = true
					frontier.append(neighbor)
		if count >= 3:
			ore_centers.append({"kind":mineral, "position":sum / float(count), "size":count})

func _map_scale() -> float:
	return _base_scale * zoom

func _map_position(cell: Vector2) -> Vector2:
	return _map_rect.position + cell * _map_scale()

func _tile_at(point: Vector2) -> Vector2i:
	var tile := (point - _map_rect.position) / _map_scale()
	return Vector2i(clampi(floori(tile.x), 0, Model.WIDTH - 1),
		clampi(floori(tile.y), 0, Model.HEIGHT - 1))

func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, size), Color("#07131D", 0.985))

func _redraw_map() -> void:
	if _surface != null and is_instance_valid(_surface):
		_surface.queue_redraw()
	queue_redraw()

func _draw_surface(canvas: Control) -> void:
	if model == null or camera == null:
		return
	if _ground == null:
		_bake_geography()
	var viewport: Vector2 = canvas.size
	canvas.draw_rect(Rect2(Vector2.ZERO, viewport), Color("#102922"))
	_base_scale = minf((viewport.x - 24.0) / float(Model.WIDTH),
		(viewport.y - 24.0) / float(Model.HEIGHT))
	_base_scale = maxf(_base_scale, 0.01)
	var fit: Vector2 = Vector2(Model.WIDTH, Model.HEIGHT) * _base_scale
	var center: Vector2 = viewport * 0.5 + pan
	_map_rect = Rect2(center - fit * zoom * 0.5, fit * zoom)
	canvas.draw_texture_rect(_ground, _map_rect, false)
	if layers.get("water", true):
		canvas.draw_texture_rect(_water, _map_rect, false)
	if layers.get("ore", true):
		for mineral in ["iron", "copper", "gold"]:
			if layers.get(mineral, true):
				canvas.draw_texture_rect(_ore_layers[mineral], _map_rect, false)
		var map_font: Font = UiSkin.font()
		for cluster in ore_centers:
			if not layers.get(str(cluster.kind), true):
				continue
			var point := _map_position(cluster.position)
			var symbol_size := 14.0 if zoom >= 0.8 else 10.0
			canvas.draw_circle(point, symbol_size + 2.0, Color("#0B1B27", 0.95))
			canvas.draw_arc(point, symbol_size + 1.0, 0.0, TAU, 24,
				ORE_COLOR.get(cluster.kind, Color.WHITE), 2.0)
			var ore_icon: Texture2D = Catalog.icon(str(cluster.kind) + "_ore")
			if ore_icon != null:
				canvas.draw_texture_rect(ore_icon, Rect2(
					point - Vector2.ONE * symbol_size,
					Vector2.ONE * symbol_size * 2.0), false)
			else:
				var mark := str(Catalog.item(str(cluster.kind) + "_ore").get("mark", "?"))
				canvas.draw_string(map_font, point + Vector2(-7, 3), mark,
					HORIZONTAL_ALIGNMENT_LEFT, -1, 10, Color.WHITE)
	if layers.get("buildings", true):
		for cell in model.buildings:
			var kind: String = model.buildings[cell].kind
			if zoom < 1.2 and kind == "belt":
				continue
			var point := _map_position(Vector2(cell) + Vector2(0.5, 0.5))
			canvas.draw_circle(point, maxf(2.0, _map_scale() * 0.65),
				Color("#6FE0E7") if kind == "depot" else Color("#E8F1EF"))
	if layers.get("casino", true):
		var site := Rect2(_map_position(Vector2(Construction.ORIGIN)),
			Vector2(Construction.WIDTH, Construction.HEIGHT) * _map_scale())
		canvas.draw_rect(site, Color("#F6BC50", 0.22))
		canvas.draw_rect(site, Color("#F6BC50"), false, 2.0)
	if layers.get("markers", true):
		for i in range(markers.size()):
			var m: Dictionary = markers[i]
			var point := _map_position(Vector2(float(m.x) + 0.5, float(m.y) + 0.5))
			var tint := Color(str(m.get("color", "#F5C04A")))
			canvas.draw_circle(point, (8.0 + sin(animation_time * 3.5) * 1.5) if i == selected else 5.0, Color("#081A22"))
			canvas.draw_circle(point, 5.5 if i == selected else 3.5, tint)
	if layers.get("water", true) and water_motion and not reduced_motion:
		var glints := 0
		for cell in model.water:
			if posmod(cell.x * 17 + cell.y * 29, 23) != 0:
				continue
			var entry: Dictionary = model.water[cell]
			var flow: Vector2 = entry.flow if entry.kind == "river" else Vector2.RIGHT
			var center_point := _map_position(Vector2(cell) + Vector2(0.5, 0.5))
			var wave := fposmod(animation_time * (0.7 if entry.kind == "river" else 0.2) + glints * 0.19, 1.0)
			canvas.draw_line(center_point + flow * (wave - 0.5) * _map_scale() * 0.6,
				center_point + flow * (wave - 0.3) * _map_scale() * 0.6,
				Color("#A0D2D1", 0.22), 1.0)
			glints += 1
	var visible_world := get_viewport_rect().size / camera.zoom
	var camera_cell := (camera.position - visible_world * 0.5) / TILE
	var frame_rect := Rect2(_map_position(camera_cell), visible_world / TILE * _map_scale())
	canvas.draw_rect(frame_rect, Color("#6BE7FA", 0.07))
	canvas.draw_rect(frame_rect, Color("#6BE7FA"), false, 2.0)
	# Restrained overlay lines help read distance without a distracting grid.
	if zoom > 0.8:
		for x in range(0, Model.WIDTH + 1, 16):
			var x0: float = _map_position(Vector2(float(x), 0)).x
			canvas.draw_line(Vector2(x0, maxi(0, int(_map_rect.position.y))),
				Vector2(x0, mini(int(viewport.y), int(_map_rect.end.y))),
				Color("#D5F4EE", 0.07), 1.0)
		for y in range(0, Model.HEIGHT + 1, 16):
			var y0: float = _map_position(Vector2(0, float(y))).y
			canvas.draw_line(Vector2(maxf(0.0, _map_rect.position.x), y0),
				Vector2(minf(viewport.x, _map_rect.end.x), y0),
				Color("#D5F4EE", 0.07), 1.0)
	canvas.draw_rect(_map_rect, Color("#C2DAE0", 0.7), false, 1.4)


func _build_controls() -> void:
	var heading := HBoxContainer.new()
	heading.name = "WorldAtlasHeading"
	heading.set_anchors_and_offsets_preset(Control.PRESET_TOP_WIDE)
	heading.offset_left = 28
	heading.offset_right = -28
	heading.offset_top = 15
	heading.offset_bottom = 85
	add_child(heading)
	var heading_col := _column(heading, 3)
	heading_col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_label(heading_col, "MIDNIGHT  /  INDUSTRIES", 12, MenuTheme.AMBER)
	_label(heading_col, _word("WELTATLAS", "WORLD ATLAS"), 27, MenuTheme.TEXT)
	var top_right := _column(heading, 3)
	var m := _label(top_right, _word("VOLLSTÄNDIG ERKUNDET", "FULLY REVEALED"),
		12, MenuTheme.CYAN)
	m.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	_label(top_right, _word("224 × 176   ·   KEIN NEBEL",
		"224 × 176   ·   NO FOG OF WAR"), 11, MenuTheme.MUTED).horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT

	var body := HBoxContainer.new()
	body.name = "WorldAtlasBody"
	body.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	body.offset_left = 24
	body.offset_top = 102
	body.offset_right = -24
	body.offset_bottom = -62
	body.add_theme_constant_override("separation", 18)
	add_child(body)
	_map_frame = _panel(body, "main")
	_map_frame.name = "AtlasMapFrame"
	_map_frame.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_map_frame.size_flags_stretch_ratio = 1.0
	var map_pad := MarginContainer.new()
	map_pad.add_theme_constant_override("margin_left", 2)
	map_pad.add_theme_constant_override("margin_top", 2)
	map_pad.add_theme_constant_override("margin_right", 2)
	map_pad.add_theme_constant_override("margin_bottom", 2)
	_map_frame.add_child(map_pad)
	_surface = MapSurface.new()
	_surface.name = "ClippedWorldMap"
	_surface.atlas = self
	_surface.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_surface.size_flags_vertical = Control.SIZE_EXPAND_FILL
	map_pad.add_child(_surface)

	_side_panel = _panel(body, "main")
	_side_panel.name = "AtlasLegendPanel"
	_side_panel.custom_minimum_size.x = 355
	var side_col := _column(_side_panel, 12)
	_label(side_col, _word("NAVIGATION & EBENEN", "NAVIGATION & LAYERS"),
		17, MenuTheme.TEXT)
	coordinates = _label(side_col, "224 × 176", 12, MenuTheme.MUTED)
	var zoom_box := _panel(side_col, "section")
	var zoom_col := _column(zoom_box, 10)
	var zoom_row := HBoxContainer.new()
	zoom_row.add_theme_constant_override("separation", 9)
	zoom_col.add_child(zoom_row)
	var zoom_out := _button(zoom_row, "−", func() -> void:
		_change_zoom(1.0 / 1.25, _surface.size * 0.5))
	zoom_out.custom_minimum_size.x = 48
	zoom_label = _label(zoom_row, "100%", 16, MenuTheme.CYAN)
	zoom_label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	zoom_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	zoom_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	var zoom_in := _button(zoom_row, "+", func() -> void:
		_change_zoom(1.25, _surface.size * 0.5))
	zoom_in.custom_minimum_size.x = 48
	_button(zoom_col, _word("Kamera zentrieren", "Center on camera"),
		func() -> void:
			pan = (Vector2(Model.WIDTH, Model.HEIGHT) * 0.5 -
				camera.position / TILE) * _base_scale * zoom
			_redraw_map())
	_rule(side_col)
	var sections := HBoxContainer.new()
	side_col.add_child(sections)
	_label(sections, _word("KARTENLEGENDE", "MAP LEGEND"), 14, MenuTheme.AMBER)
	var space := Control.new()
	space.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	sections.add_child(space)
	var reset := _button(sections, _word("Alle an", "Show all"),
		func() -> void: _set_layers(true))
	reset.custom_minimum_size.x = 90
	var scroll := ScrollContainer.new()
	scroll.name = "AtlasLegendScroll"
	scroll.horizontal_scroll_mode = ScrollContainer.SCROLL_MODE_DISABLED
	scroll.vertical_scroll_mode = ScrollContainer.SCROLL_MODE_AUTO
	scroll.size_flags_vertical = Control.SIZE_EXPAND_FILL
	scroll.custom_minimum_size.y = 170
	side_col.add_child(scroll)
	var contents := _column(scroll, 10)
	layer_row = _column(contents, 7)
	_rule(contents)
	_label(contents, _word("MARKIERUNGEN", "MARKERS"), 14, MenuTheme.AMBER)
	marker_list = _column(contents, 7)
	_button(side_col, _word("Karte schließen  [M]", "Close map  [M]"), hide_map)

	var help := HBoxContainer.new()
	help.set_anchors_and_offsets_preset(Control.PRESET_BOTTOM_WIDE)
	help.offset_left = 29
	help.offset_right = -25
	help.offset_top = -49
	help.offset_bottom = -15
	add_child(help)
	_label(help, _word(
		"M / ESC   SCHLIESSEN    ·    RAD   ZOOM    ·    ZIEHEN   VERSCHIEBEN    ·    DOPPELKLICK   ZUR POSITION    ·    RECHTSKLICK   MARKIEREN",
		"M / ESC   CLOSE    ·    WHEEL   ZOOM    ·    DRAG   PAN    ·    DOUBLE CLICK   TRAVEL    ·    RIGHT CLICK   MARK"),
		11, MenuTheme.MUTED)

	marker_popup = PopupPanel.new()
	marker_popup.name = "AtlasMarkerEditor"
	marker_popup.add_theme_stylebox_override("panel", MenuTheme.panel("main"))
	add_child(marker_popup)
	var editor := _column(marker_popup, 11)
	_label(editor, _word("MARKIERUNG BEARBEITEN", "EDIT MAP MARKER"),
		17, MenuTheme.AMBER)
	marker_name = LineEdit.new()
	marker_name.placeholder_text = _word("Name der Markierung", "Marker name")
	marker_name.custom_minimum_size = Vector2(320, 43)
	MenuTheme.style_input(marker_name)
	editor.add_child(marker_name)
	marker_icon = OptionButton.new()
	MenuTheme.style_option(marker_icon)
	for symbol in ["◆", "⚑", "●"]:
		marker_icon.add_item(symbol)
	editor.add_child(marker_icon)
	marker_color = OptionButton.new()
	MenuTheme.style_option(marker_color)
	for color in [_word("Gold", "Amber"), _word("Cyan", "Cyan"), _word("Rot", "Red")]:
		marker_color.add_item(color)
	editor.add_child(marker_color)
	var buttons := HBoxContainer.new()
	buttons.add_theme_constant_override("separation", 8)
	editor.add_child(buttons)
	_button(buttons, _word("Speichern", "Save"), _save_marker)
	_button(editor, _word("Versetzen: Ziel rechtsklicken",
		"Move: right-click destination"), func() -> void:
		if selected >= 0:
			moving_marker = true
			marker_popup.hide())
	_button(editor, _word("Markierung löschen", "Delete marker"), _remove_marker)

func _panel(parent: Node, kind: String = "section") -> PanelContainer:
	var node := PanelContainer.new()
	node.add_theme_stylebox_override("panel", MenuTheme.panel(kind))
	parent.add_child(node)
	return node

func _column(parent: Node, gap: int = 10) -> VBoxContainer:
	var col := VBoxContainer.new()
	col.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	col.add_theme_constant_override("separation", gap)
	parent.add_child(col)
	return col

func _rule(parent: Node) -> void:
	var rule := ColorRect.new()
	rule.color = Color("#507484", 0.55)
	rule.custom_minimum_size.y = 1
	rule.mouse_filter = Control.MOUSE_FILTER_IGNORE
	parent.add_child(rule)

func _label(parent: Node, text_value: String, font_size: int,
	color: Color = MenuTheme.TEXT) -> Label:
	var node := Label.new()
	node.text = text_value
	MenuTheme.label(node, font_size, color)
	parent.add_child(node)
	return node

func _button(parent: Node, text_value: String, action: Callable) -> Button:
	var button := Button.new()
	button.text = text_value
	button.custom_minimum_size.y = 38
	MenuTheme.style_button(button)
	button.pressed.connect(action)
	parent.add_child(button)
	return button

func _legend_icon(parent: Node, key: String) -> void:
	if key in ["iron", "copper", "gold"]:
		var icon := TextureRect.new()
		icon.texture = Catalog.icon(key + "_ore")
		icon.custom_minimum_size = Vector2(29, 29)
		icon.expand_mode = TextureRect.EXPAND_IGNORE_SIZE
		icon.stretch_mode = TextureRect.STRETCH_KEEP_ASPECT_CENTERED
		icon.mouse_filter = Control.MOUSE_FILTER_IGNORE
		parent.add_child(icon)
	else:
		var glyphs := {"ore":"◇", "water":"≈", "buildings":"▦",
			"casino":"◆", "markers":"⚑"}
		var icon_label := _label(parent, str(glyphs.get(key, "●")),
			20, MenuTheme.CYAN if key == "water" else MenuTheme.AMBER)
		icon_label.custom_minimum_size.x = 29
		icon_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER

func _refresh_controls() -> void:
	for child in layer_row.get_children():
		child.queue_free()
	for key in DEFAULT_LAYERS:
		var card := _panel(layer_row, "section")
		var row := HBoxContainer.new()
		row.custom_minimum_size.y = 33
		row.add_theme_constant_override("separation", 8)
		card.add_child(row)
		_legend_icon(row, key)
		var labels := {
			"ore":_word("Ressourcen anzeigen", "Resource deposits"),
			"iron":_word("Eisenerz", "Iron ore"),
			"copper":_word("Kupfererz", "Copper ore"),
			"gold":_word("Golderz", "Gold ore"),
			"water":_word("Flüsse & Seen", "Rivers & lakes"),
			"buildings":_word("Fabrikanlagen", "Factory buildings"),
			"casino":_word("Casinobauplatz", "Casino site"),
			"markers":_word("Eigene Markierungen", "Custom markers")}
		var label := _label(row, str(labels[key]), 13, MenuTheme.TEXT)
		label.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		var toggle := CheckButton.new()
		toggle.name = "MapLayer_" + str(key)
		toggle.button_pressed = bool(layers.get(key, true))
		toggle.toggled.connect(func(on: bool) -> void:
			layers[key] = on
			_redraw_map())
		row.add_child(toggle)
	for child in marker_list.get_children():
		child.queue_free()
	if markers.is_empty():
		_label(marker_list, _word("Rechtsklick auf die Karte, um eine Markierung anzulegen.",
			"Right-click the map to create a marker."), 12, MenuTheme.MUTED).autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	for i in range(markers.size()):
		var m: Dictionary = markers[i]
		_button(marker_list, "%s  %s  (%d, %d)" % [
			m.get("icon", "◆"), m.get("name", "?"), m.x, m.y],
			_focus_marker.bind(i))
	if zoom_label != null:
		zoom_label.text = "%d%%" % roundi(zoom * 100.0)

func _set_layers(on: bool) -> void:
	for key in DEFAULT_LAYERS:
		layers[key] = on
	_refresh_controls()
	_redraw_map()

func _focus_marker(index: int) -> void:
	selected = index
	var m: Dictionary = markers[index]
	pan = (Vector2(Model.WIDTH, Model.HEIGHT) * 0.5 - Vector2(m.x, m.y)) * _base_scale * zoom
	_redraw_map()

func _edit_marker(cell: Vector2i) -> void:
	marker_cell = cell
	if moving_marker and selected >= 0:
		moving_marker = false
		var marker: Dictionary = markers[selected]
		marker.x = cell.x
		marker.y = cell.y
		_refresh_controls()
		_redraw_map()
		return
	selected = -1
	for i in range(markers.size()):
		var m: Dictionary = markers[i]
		if Vector2i(int(m.x), int(m.y)).distance_to(cell) <= 2.0:
			selected = i
			break
	marker_name.text = str(markers[selected].name) if selected >= 0 else ""
	if selected >= 0:
		var marker: Dictionary = markers[selected]
		for i in range(marker_icon.item_count):
			if marker_icon.get_item_text(i) == str(marker.get("icon", "◆")):
				marker_icon.select(i)
		var colors := ["#F5C04A", "#54D6E7", "#E67868"]
		marker_color.select(maxi(0, colors.find(str(marker.get("color", colors[0])))))
	marker_popup.popup_centered(Vector2i(260, 180))
	marker_name.grab_focus()

func _save_marker() -> void:
	var colors := ["#F5C04A", "#54D6E7", "#E67868"]
	var data := {"x":marker_cell.x, "y":marker_cell.y,
		"name": marker_name.text.strip_edges().substr(0, 40),
		"icon":marker_icon.get_item_text(marker_icon.selected),
		"color":colors[marker_color.selected]}
	if data.name.is_empty():
		data.name = _word("Markierung", "Marker")
	if selected >= 0:
		markers[selected] = data
	else:
		markers.append(data)
	marker_popup.hide()
	_refresh_controls()
	_redraw_map()

func _remove_marker() -> void:
	if selected >= 0:
		markers.remove_at(selected)
	selected = -1
	marker_popup.hide()
	_refresh_controls()
	_redraw_map()

func _change_zoom(factor: float, at: Vector2) -> void:
	var world := (at - _map_rect.position) / _map_scale()
	zoom = clampf(zoom * factor, 0.5, 4.0)
	var next_fit := Vector2(Model.WIDTH, Model.HEIGHT) * _base_scale * zoom
	var default_center := _surface.size * 0.5
	pan = at - world * _map_scale() + next_fit * 0.5 - default_center
	if zoom_label != null:
		zoom_label.text = "%d%%" % roundi(zoom * 100.0)
	_redraw_map()

func _surface_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		if dragging:
			pan += event.relative
			drag_distance += event.relative.length()
			_redraw_map()
		if _map_rect.has_point(event.position):
			var tile := _tile_at(event.position)
			var named := Catalog.name_for(str(model.resources[tile]) + "_ore", german) if model.resources.has(tile) else _word("Karte", "Map")
			coordinates.text = "%s · %d, %d · %d%%" % [named, tile.x, tile.y, roundi(zoom * 100)]
	elif event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP and event.pressed:
			_change_zoom(1.15, event.position)
			accept_event()
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN and event.pressed:
			_change_zoom(1.0 / 1.15, event.position)
			accept_event()
		elif event.button_index == MOUSE_BUTTON_LEFT:
			if event.pressed and _map_rect.has_point(event.position):
				dragging = true
				drag_distance = 0.0
			elif not event.pressed and dragging:
				dragging = false
				if drag_distance >= 6.0:
					last_click = 0
				if drag_distance < 6.0 and _map_rect.has_point(event.position):
					var now := Time.get_ticks_msec()
					if now - last_click < 350:
						camera.position = Vector2(_tile_at(event.position)) * TILE
						hide_map()
					else:
						last_click = now
						selected = -1
						var cell := _tile_at(event.position)
						for i in range(markers.size()):
							var marker: Dictionary = markers[i]
							if Vector2i(int(marker.x), int(marker.y)).distance_to(cell) <= 2.0:
								selected = i
								coordinates.text = "%s · %d, %d" % [marker.name, marker.x, marker.y]
								break
						_redraw_map()
				accept_event()
		elif event.button_index == MOUSE_BUTTON_RIGHT and event.pressed and _map_rect.has_point(event.position):
			_edit_marker(_tile_at(event.position))
			accept_event()
