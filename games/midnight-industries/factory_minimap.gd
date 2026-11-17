extends Control
## Fully revealed cached atlas; overlays, markers and camera are independent.
const Model = preload("res://factory_model.gd")
const Construction = preload("res://factory_construction.gd")
const Catalog = preload("res://item_catalog.gd")
const UiSkin = preload("res://factory_skin.gd")
const TILE := 48.0
const DEFAULT_LAYERS := {"ore":true, "water":true, "buildings":true,
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

func _word(de: String, en: String) -> String:
	return de if german else en

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_STOP
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	visible = false
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	_build_controls()

func show_map() -> void:
	if _ground == null:
		_bake_geography()
	visible = true
	_refresh_controls()
	queue_redraw()

func hide_map() -> void:
	visible = false

func refresh_map() -> void:
	_ground = null
	_ore = null
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
		queue_redraw()

func _bake_geography() -> void:
	if model == null:
		return
	var ground := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	var ore := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
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
				ore.set_pixel(x, y, ORE_COLOR.get(model.resources[cell], Color.WHITE))
	_ground = ImageTexture.create_from_image(ground)
	_ore = ImageTexture.create_from_image(ore)
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
	if model == null or camera == null:
		return
	if _ground == null:
		_bake_geography()
	var viewport := size
	draw_rect(Rect2(Vector2.ZERO, viewport), Color("#06131B", 0.98))
	_base_scale = minf((viewport.x - 320.0) / Model.WIDTH,
		(viewport.y - 140.0) / Model.HEIGHT)
	var fit := Vector2(Model.WIDTH, Model.HEIGHT) * _base_scale
	var center := Vector2((viewport.x - 270.0) * 0.5, viewport.y * 0.5) + pan
	_map_rect = Rect2(center - fit * zoom * 0.5, fit * zoom)
	var frame := _map_rect.grow(10)
	draw_rect(frame, Color("#172B34"))
	draw_rect(frame, Color("#63808C"), false, 2.0)
	draw_texture_rect(_ground, _map_rect, false)
	if layers.get("water", true):
		draw_texture_rect(_water, _map_rect, false)
	if layers.get("ore", true):
		draw_texture_rect(_ore, _map_rect, false)
		var map_font: Font = UiSkin.font()
		for cluster in ore_centers:
			var point := _map_position(cluster.position)
			var mark := str(Catalog.item(str(cluster.kind) + "_ore").get("mark", "?"))
			var symbol_size := 8.5 if zoom >= 0.8 else 6.5
			draw_circle(point, symbol_size, Color("#101E27", 0.90))
			draw_arc(point, symbol_size, 0.0, TAU, 16, ORE_COLOR.get(cluster.kind, Color.WHITE), 1.6)
			draw_string(map_font, point + Vector2(-6, 3), mark,
				HORIZONTAL_ALIGNMENT_LEFT, -1, 9, Color.WHITE)
	if layers.get("buildings", true):
		for cell in model.buildings:
			var kind: String = model.buildings[cell].kind
			if zoom < 1.2 and kind == "belt":
				continue
			var point := _map_position(Vector2(cell) + Vector2(0.5, 0.5))
			draw_circle(point, maxf(2.0, _map_scale() * 0.65),
				Color("#6FE0E7") if kind == "depot" else Color("#E8F1EF"))
	if layers.get("casino", true):
		var site := Rect2(_map_position(Vector2(Construction.ORIGIN)),
			Vector2(Construction.WIDTH, Construction.HEIGHT) * _map_scale())
		draw_rect(site, Color("#F6BC50", 0.22))
		draw_rect(site, Color("#F6BC50"), false, 2.0)
	if layers.get("markers", true):
		for i in range(markers.size()):
			var m: Dictionary = markers[i]
			var point := _map_position(Vector2(float(m.x) + 0.5, float(m.y) + 0.5))
			var tint := Color(str(m.get("color", "#F5C04A")))
			draw_circle(point, (8.0 + sin(animation_time * 3.5) * 1.5) if i == selected else 5.0, Color("#081A22"))
			draw_circle(point, 5.5 if i == selected else 3.5, tint)
	if layers.get("water", true) and water_motion and not reduced_motion:
		var glints := 0
		for cell in model.water:
			if posmod(cell.x * 17 + cell.y * 29, 23) != 0:
				continue
			var entry: Dictionary = model.water[cell]
			var flow: Vector2 = entry.flow if entry.kind == "river" else Vector2.RIGHT
			var center_point := _map_position(Vector2(cell) + Vector2(0.5, 0.5))
			var wave := fposmod(animation_time * (0.7 if entry.kind == "river" else 0.2) + glints * 0.19, 1.0)
			draw_line(center_point + flow * (wave - 0.5) * _map_scale() * 0.6,
				center_point + flow * (wave - 0.3) * _map_scale() * 0.6,
				Color("#A0D2D1", 0.22), 1.0)
			glints += 1
	var visible_world := get_viewport_rect().size / camera.zoom
	var camera_cell := (camera.position - visible_world * 0.5) / TILE
	var frame_rect := Rect2(_map_position(camera_cell), visible_world / TILE * _map_scale())
	draw_rect(frame_rect, Color("#6BE7FA", 0.07))
	draw_rect(frame_rect, Color("#6BE7FA"), false, 2.0)
	var font: Font = UiSkin.font()
	draw_string(font, Vector2(26, 45), _word("MIDNIGHT INDUSTRIES / WELTKARTE", "MIDNIGHT INDUSTRIES / WORLD MAP"), HORIZONTAL_ALIGNMENT_LEFT, -1, 22, Color("#E8F1EF"))
	draw_string(font, Vector2(27, viewport.y - 22),
		_word("M / ESC  SCHLIESSEN  ·  RAD  ZOOM  ·  ZIEHEN  PAN  ·  DOPPELKLICK  REISEN  ·  RECHTSKLICK  MARKIEREN",
			"M / ESC  CLOSE  ·  WHEEL  ZOOM  ·  DRAG  PAN  ·  DOUBLE CLICK  TRAVEL  ·  RIGHT CLICK  MARK"),
		HORIZONTAL_ALIGNMENT_LEFT, -1, 12, Color("#A3B9C1"))

func _build_controls() -> void:
	var side := PanelContainer.new()
	side.add_theme_stylebox_override("panel", UiSkin.panel("panel"))
	add_child(side)
	side.set_anchors_preset(Control.PRESET_RIGHT_WIDE)
	side.offset_left = -290
	side.offset_right = -12
	side.offset_top = 72
	side.offset_bottom = -56
	var scroll := ScrollContainer.new()
	side.add_child(scroll)
	var column := VBoxContainer.new()
	column.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	scroll.add_child(column)
	_label(column, _word("ATLAS / EBENEN", "ATLAS / LAYERS"), 17)
	coordinates = _label(column, "224 × 176", 12)
	var zoom_row := HBoxContainer.new()
	column.add_child(zoom_row)
	_button(zoom_row, "−", func() -> void: _change_zoom(1.0 / 1.25, size * 0.5))
	_button(zoom_row, "+", func() -> void: _change_zoom(1.25, size * 0.5))
	_button(column, _word("Kamera zentrieren", "Center on camera"), func() -> void:
		pan = (Vector2(Model.WIDTH, Model.HEIGHT) * 0.5 - camera.position / TILE) * _base_scale * zoom
		queue_redraw())
	layer_row = VBoxContainer.new()
	column.add_child(layer_row)
	var all_row := HBoxContainer.new()
	column.add_child(all_row)
	_button(all_row, _word("Alle an", "Show all"), func() -> void: _set_layers(true))
	_button(all_row, _word("Alle aus", "Hide all"), func() -> void: _set_layers(false))
	_label(column, _word("EISEN Fe  ·  KUPFER Cu  ·  GOLD Au", "IRON Fe  ·  COPPER Cu  ·  GOLD Au"), 12)
	_label(column, _word("WASSER  ·  FABRIK  ·  CASINO", "WATER  ·  FACTORY  ·  CASINO"), 12)
	_label(column, _word("MARKIERUNGEN", "MARKERS"), 15)
	marker_list = VBoxContainer.new()
	column.add_child(marker_list)
	_button(column, _word("Schließen [M]", "Close [M]"), hide_map)
	marker_popup = PopupPanel.new()
	add_child(marker_popup)
	var editor := VBoxContainer.new()
	marker_popup.add_child(editor)
	marker_name = LineEdit.new()
	marker_name.placeholder_text = _word("Titel der Markierung", "Marker title")
	editor.add_child(marker_name)
	marker_icon = OptionButton.new()
	for icon in ["◆", "⚑", "●"]:
		marker_icon.add_item(icon)
	editor.add_child(marker_icon)
	marker_color = OptionButton.new()
	for color in ["Amber", "Cyan", "Rot"]:
		marker_color.add_item(color)
	editor.add_child(marker_color)
	_button(editor, _word("Speichern", "Save"), _save_marker)
	_button(editor, _word("Versetzen: Ziel rechtsklicken", "Move: right-click destination"), func() -> void:
		if selected >= 0:
			moving_marker = true
			marker_popup.hide())
	_button(editor, _word("Entfernen", "Remove"), _remove_marker)

func _label(parent: Node, text_value: String, font_size: int) -> Label:
	var l := Label.new()
	l.text = text_value
	l.add_theme_font_override("font", UiSkin.font())
	l.add_theme_font_size_override("font_size", font_size)
	parent.add_child(l)
	return l

func _button(parent: Node, text_value: String, action: Callable) -> Button:
	var b := Button.new()
	b.text = text_value
	b.custom_minimum_size.y = 33
	b.add_theme_stylebox_override("normal", UiSkin.button("button"))
	b.add_theme_stylebox_override("hover", UiSkin.button("button_active"))
	b.pressed.connect(action)
	parent.add_child(b)
	return b

func _refresh_controls() -> void:
	for child in layer_row.get_children():
		child.queue_free()
	for key in DEFAULT_LAYERS:
		var toggle := CheckBox.new()
		toggle.text = {
			"ore":_word("Erze", "Ores"), "water":_word("Gewässer", "Water"),
			"buildings":_word("Anlagen", "Factory"), "casino":"Casino",
			"markers":_word("Markierungen", "Markers")}[key]
		toggle.button_pressed = bool(layers.get(key, true))
		toggle.toggled.connect(func(on: bool) -> void:
			layers[key] = on
			queue_redraw())
		layer_row.add_child(toggle)
	for child in marker_list.get_children():
		child.queue_free()
	for i in range(markers.size()):
		var m: Dictionary = markers[i]
		_button(marker_list, "%s  %s (%d,%d)" % [m.get("icon", "◆"), m.get("name", "?"), m.x, m.y],
			_focus_marker.bind(i))

func _set_layers(on: bool) -> void:
	for key in DEFAULT_LAYERS:
		layers[key] = on
	_refresh_controls()
	queue_redraw()

func _focus_marker(index: int) -> void:
	selected = index
	var m: Dictionary = markers[index]
	pan = (Vector2(Model.WIDTH, Model.HEIGHT) * 0.5 - Vector2(m.x, m.y)) * _base_scale * zoom
	queue_redraw()

func _edit_marker(cell: Vector2i) -> void:
	marker_cell = cell
	if moving_marker and selected >= 0:
		moving_marker = false
		var marker: Dictionary = markers[selected]
		marker.x = cell.x
		marker.y = cell.y
		_refresh_controls()
		queue_redraw()
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
	queue_redraw()

func _remove_marker() -> void:
	if selected >= 0:
		markers.remove_at(selected)
	selected = -1
	marker_popup.hide()
	_refresh_controls()
	queue_redraw()

func _change_zoom(factor: float, at: Vector2) -> void:
	var world := (at - _map_rect.position) / _map_scale()
	zoom = clampf(zoom * factor, 0.5, 4.0)
	var next_fit := Vector2(Model.WIDTH, Model.HEIGHT) * _base_scale * zoom
	var default_center := Vector2((size.x - 270.0) * 0.5, size.y * 0.5)
	pan = at - world * _map_scale() + next_fit * 0.5 - default_center
	queue_redraw()

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion:
		if dragging:
			pan += event.relative
			drag_distance += event.relative.length()
			queue_redraw()
		if _map_rect.has_point(event.position):
			var tile := _tile_at(event.position)
			coordinates.text = "%d, %d  ·  %d%%" % [tile.x, tile.y, roundi(zoom * 100)]
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
				accept_event()
		elif event.button_index == MOUSE_BUTTON_RIGHT and event.pressed and _map_rect.has_point(event.position):
			_edit_marker(_tile_at(event.position))
			accept_event()
