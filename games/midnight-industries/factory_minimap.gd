extends Control
## Modal atlas-style overview: opens with M, never consumes permanent HUD
## space. Static geography is cached in a 224x176 texture, with only
## buildings and viewport markers redrawn when the map is open.
const Model = preload("res://factory_model.gd")
const Construction = preload("res://factory_construction.gd")
const UiSkin = preload("res://factory_skin.gd")
const TILE := 48.0
const CREAM := Color("#E6F0EE")
const MUTED := Color("#8DAAB3")
const CYAN := Color("#11D1E9")
const GOLD := Color("#E8BC53")

var model
var camera: Camera2D
var _ground: Texture2D
var _map_rect := Rect2()
var _pixel_scale := 1.0

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_STOP
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	visible = false
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

func show_map() -> void:
	if _ground == null:
		_bake_geography()
	visible = true
	queue_redraw()

func hide_map() -> void:
	visible = false

func refresh_map() -> void:
	_ground = null
	if visible:
		show_map()

func _process(_delta: float) -> void:
	if visible:
		queue_redraw()

func _bake_geography() -> void:
	if model == null:
		return
	var pixels := Image.create(Model.WIDTH, Model.HEIGHT, false, Image.FORMAT_RGBA8)
	for y in range(Model.HEIGHT):
		for x in range(Model.WIDTH):
			var cell := Vector2i(x, y)
			var grain := float(posmod(x * 73 + y * 179 + x * y * 3, 91)) / 91.0
			var pixel := Color(0.100, 0.172, 0.106).lerp(
				Color(0.144, 0.211, 0.132), grain)
			if model.water.has(cell):
				var entry: Dictionary = model.water[cell]
				pixel = Color("#12516B") if entry["kind"] == "river" else Color("#19657B")
			elif model.resources.has(cell):
				match str(model.resources[cell]):
					"iron":
						pixel = Color("#8198A6").darkened(grain * 0.15)
					"copper":
						pixel = Color("#CF7543").darkened(grain * 0.15)
					"gold":
						pixel = Color("#F1C354").darkened(grain * 0.17)
			pixels.set_pixel(x, y, pixel)
	_ground = ImageTexture.create_from_image(pixels)

func _draw() -> void:
	if model == null or camera == null:
		return
	if _ground == null:
		_bake_geography()
	if _ground == null:
		return
	var viewport := size
	draw_rect(Rect2(Vector2.ZERO, viewport), Color("#051017", 0.98))
	var font: Font = UiSkin.font()
	var target_width := viewport.x * 0.69
	var target_height := viewport.y * 0.75
	_pixel_scale = minf(target_width / float(Model.WIDTH),
		target_height / float(Model.HEIGHT))
	var world_size := Vector2(Model.WIDTH, Model.HEIGHT) * _pixel_scale
	var sidebar_width := minf(270.0, viewport.x * 0.23)
	var left := maxf(20.0, (viewport.x - world_size.x - sidebar_width - 30.0) * 0.5)
	var top := maxf(65.0, (viewport.y - world_size.y) * 0.50)
	_map_rect = Rect2(Vector2(left, top), world_size)
	draw_rect(Rect2(_map_rect.position - Vector2(12, 12),
		_map_rect.size + Vector2(24, 24)), Color("#10232D"))
	draw_texture_rect(_ground, _map_rect, false)
	draw_rect(_map_rect, Color("#5D7D87"), false, 2.0)
	# Individual built structures are not baked: their markers stay current.
	for cell in model.buildings:
		var p := _map_rect.position + Vector2(cell) * _pixel_scale
		draw_rect(Rect2(p, Vector2.ONE * maxf(1.3, _pixel_scale)),
			Color("#E3EEF0"))
	var site := Rect2(_map_rect.position +
		Vector2(Construction.ORIGIN) * _pixel_scale,
		Vector2(Construction.WIDTH, Construction.HEIGHT) * _pixel_scale)
	draw_rect(site, Color("#E9B54F", 0.21))
	draw_rect(site, GOLD, false, 2.4)
	# Actual visible camera bounds, not the map's entire world dimensions.
	var visible_world := get_viewport_rect().size / camera.zoom
	var start_tile := (camera.position - visible_world * 0.5) / TILE
	var view_tiles := visible_world / TILE
	var cam_rect := Rect2(_map_rect.position + start_tile * _pixel_scale,
		view_tiles * _pixel_scale)
	draw_rect(cam_rect, Color("#7EF0FF", 0.07))
	draw_rect(cam_rect, CYAN, false, 2.2)
	draw_circle(_map_rect.position + (camera.position / TILE) * _pixel_scale,
		3.7, Color.WHITE)
	var panel_x := _map_rect.end.x + 30.0
	var panel_y := maxf(80.0, top + 11.0)
	_label(font, Vector2(left, 49.0), "MIDNIGHT INDUSTRIES   /   WELTKARTE", 22, CREAM)
	_label(font, Vector2(panel_x, panel_y + 4), "WORLD OVERVIEW", 19, CREAM)
	_label(font, Vector2(panel_x, panel_y + 32),
		"%d x %d GRID" % [Model.WIDTH, Model.HEIGHT], 12, MUTED)
	panel_y += 75.0
	_legend(font, panel_x, panel_y, Color("#8198A6"), "IRON / EISEN")
	_legend(font, panel_x, panel_y + 35, Color("#CF7543"), "COPPER / KUPFER")
	_legend(font, panel_x, panel_y + 70, Color("#F1C354"), "GOLD")
	_legend(font, panel_x, panel_y + 105, Color("#12516B"), "RIVER / FLUSS")
	_legend(font, panel_x, panel_y + 140, Color("#19657B"), "LAKE / SEE")
	_legend(font, panel_x, panel_y + 175, GOLD, "CASINO SITE")
	_legend(font, panel_x, panel_y + 210, Color("#E3EEF0"), "FACTORY / FABRIK")
	_legend(font, panel_x, panel_y + 245, CYAN, "CAMERA / SICHTFELD")
	_label(font, Vector2(panel_x, panel_y + 305),
		"CASINO PHASE %02d / 12" % model.construction.phase_number(), 13, GOLD)
	_label(font, Vector2(left, minf(viewport.y - 24.0, _map_rect.end.y + 34.0)),
		"M / ESC   CLOSE MAP     •     CLICK MAP TO MOVE CAMERA", 12, MUTED)

func _label(font: Font, point: Vector2, value: String, pixels: int,
	tint: Color) -> void:
	draw_string(font, point, value, HORIZONTAL_ALIGNMENT_LEFT, -1, pixels, tint)

func _legend(font: Font, x: float, y: float, tint: Color, name: String) -> void:
	draw_rect(Rect2(Vector2(x, y - 12.0), Vector2(17.0, 17.0)), tint)
	_label(font, Vector2(x + 28.0, y + 2.0), name, 12, CREAM)

func _gui_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT and _map_rect.has_point(event.position):
			var tile: Vector2 = (event.position - _map_rect.position) / _pixel_scale
			camera.position = Vector2(
				clampf(tile.x * TILE, 0.0, float(Model.WIDTH) * TILE),
				clampf(tile.y * TILE, 0.0, float(Model.HEIGHT) * TILE))
			hide_map()
			accept_event()
