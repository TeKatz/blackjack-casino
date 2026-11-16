extends Node2D
## Optional camera-aware placement grid. Lines cover only the visible world;
## no tiles, textures, simulation data, or buildings are modified.
const Model = preload("res://factory_model.gd")
const TILE := 48.0
var camera: Camera2D
var _last_camera := Vector2.INF
var _last_zoom := Vector2.ZERO
var _last_viewport := Vector2.ZERO

func _process(_delta: float) -> void:
	if not visible or camera == null:
		return
	var viewport_size: Vector2 = get_viewport_rect().size
	if camera.position != _last_camera or camera.zoom != _last_zoom or viewport_size != _last_viewport:
		_last_camera = camera.position
		_last_zoom = camera.zoom
		_last_viewport = viewport_size
		queue_redraw()

func _draw() -> void:
	if camera == null:
		return
	var visible_world: Vector2 = get_viewport_rect().size / camera.zoom
	var from: Vector2 = camera.position - visible_world * 0.5
	var to: Vector2 = camera.position + visible_world * 0.5
	var left: int = clampi(floori(from.x / TILE) - 1, 0, Model.WIDTH)
	var right: int = clampi(ceili(to.x / TILE) + 1, 0, Model.WIDTH)
	var top: int = clampi(floori(from.y / TILE) - 1, 0, Model.HEIGHT)
	var bottom: int = clampi(ceili(to.y / TILE) + 1, 0, Model.HEIGHT)
	var stroke: float = maxf(0.75, 1.0 / camera.zoom.x)
	var line_color := Color(0.83, 0.92, 0.87, 0.19)
	var major_color := Color(0.92, 0.77, 0.47, 0.25)
	for x in range(left, right + 1):
		var color := major_color if x % 8 == 0 else line_color
		draw_line(Vector2(float(x) * TILE, float(top) * TILE),
			Vector2(float(x) * TILE, float(bottom) * TILE),
			color, stroke)
	for y in range(top, bottom + 1):
		var color := major_color if y % 8 == 0 else line_color
		draw_line(Vector2(float(left) * TILE, float(y) * TILE),
			Vector2(float(right) * TILE, float(y) * TILE),
			color, stroke)

func set_grid_visible(enabled: bool) -> void:
	visible = enabled
	if enabled:
		_last_camera = Vector2.INF
		queue_redraw()
