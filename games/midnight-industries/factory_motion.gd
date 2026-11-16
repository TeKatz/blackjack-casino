extends Node2D
## Repaint ONLY the visible animated objects; the ground and factory remain cached.
const Model = preload("res://factory_model.gd")
const Art = preload("res://factory_art.gd")
const TILE := 48.0
const GOLD := Color("#f3d28c")
const AQUA := Color("#9cdec9")
const ERROR := Color("#ef937b")
var model
var camera: Camera2D
var hovered := Vector2i(-1, -1)
var inspected := Vector2i(-1, -1)
var cursor_mode := "inspect"
var tool := "belt"
var facing := 0
var menu_open := false
var simulation_paused := false
var reduced_motion := false
var tick := 0.0

func _process(delta: float) -> void:
	if model == null or camera == null:
		return
	if not menu_open and not simulation_paused:
		tick += minf(delta, 0.10)
		queue_redraw()

func _draw() -> void:
	if model == null or camera == null:
		return
	var view: Vector2 = get_viewport_rect().size / camera.zoom
	var world_origin: Vector2 = camera.position - view / 2.0
	var left := clampi(floori(world_origin.x / TILE) - 2, 0, Model.WIDTH - 1)
	var top := clampi(floori(world_origin.y / TILE) - 2, 0, Model.HEIGHT - 1)
	var right := clampi(ceili((world_origin.x + view.x) / TILE) + 2, 0, Model.WIDTH - 1)
	var bottom := clampi(ceili((world_origin.y + view.y) / TILE) + 2, 0, Model.HEIGHT - 1)
	var transition: float = clampf(model.accumulator / Model.STEP, 0.0, 1.0)
	for y in range(top, bottom + 1):
		for x in range(left, right + 1):
			var cell := Vector2i(x, y)
			if not model.buildings.has(cell):
				continue
			var machine: Dictionary = model.buildings[cell]
			var origin := Vector2(cell) * TILE
			match str(machine.kind):
				"belt":
					_draw_belt_motion(origin, int(machine.direction))
					if str(machine.item) != "":
						var at := origin + Vector2(24, 24)
						if model.transit.has(cell):
							var trace: Dictionary = model.transit[cell]
							if trace.item == machine.item:
								var source: Vector2i = trace["from"]
								var blend := transition * transition * (3.0 - 2.0 * transition)
								at = (Vector2(source) + Vector2(0.5, 0.5)).lerp(
									Vector2(cell) + Vector2(0.5, 0.5), blend) * TILE
						Art.cargo(self, at, str(machine.item))
				"miner":
					_draw_miner_motion(origin, machine)
				"smelter":
					_draw_smelter_motion(origin, machine, cell)
	if model.inside(inspected) and model.buildings.has(inspected):
		var center := (Vector2(inspected) + Vector2(0.5, 0.5)) * TILE
		var pulse := 0.1 if reduced_motion else 0.15 + sin(tick * 3.8) * 0.10
		draw_rect(Rect2(Vector2(inspected) * TILE + Vector2(1, 1), Vector2(46, 46)),
			Color(AQUA, 0.8 + pulse), false, 2.5)
		for corner in [Vector2(-21, -21), Vector2(21, -21), Vector2(-21, 21), Vector2(21, 21)]:
			var q: Vector2 = center + Vector2(corner)
			draw_circle(q, 2.4, GOLD)
	if menu_open or not model.inside(hovered):
		return
	var at := Vector2(hovered) * TILE
	if cursor_mode == "build":
		var valid: bool = model.can_place(hovered, tool)
		draw_rect(Rect2(at + Vector2(1, 1), Vector2(46, 46)),
			Color(AQUA if valid else ERROR, 0.20))
		if valid:
			Art.machine(self, at, tool, facing, true)
		else:
			draw_rect(Rect2(at + Vector2(2, 2), Vector2(44, 44)), Color(ERROR, 0.90), false, 2)
			_draw_cross(at, ERROR)
	else:
		if model.buildings.has(hovered):
			draw_rect(Rect2(at + Vector2(1, 1), Vector2(46, 46)), Color(GOLD, 0.82), false, 2)
		elif model.resources.has(hovered):
			draw_rect(Rect2(at + Vector2(3, 3), Vector2(42, 42)), Color(AQUA, 0.35), false, 1.4)

func _draw_cross(at: Vector2, color: Color) -> void:
	draw_line(at + Vector2(15, 15), at + Vector2(33, 33), color, 3)
	draw_line(at + Vector2(33, 15), at + Vector2(15, 33), color, 3)

func _draw_belt_motion(at: Vector2, direction: int) -> void:
	var axis: Vector2 = Vector2(Model.DIRS[direction])
	var side := Vector2(-axis.y, axis.x)
	var center := at + Vector2(24, 24)
	var scroll := 0.0 if reduced_motion else fposmod(tick * 84.0, 12.0)
	for i in range(-2, 3):
		var offset := float(i) * 12.0 + scroll - 4.0
		if absf(offset) > 14.0:
			continue
		var point := center + axis * offset
		draw_line(point - side * 8, point + side * 8, Color("#a4c8c6", 0.42), 2)
		draw_line(point + axis * 3 - side * 5, point + axis * 5, Color("#bbd9d1", 0.27), 1)

func _draw_miner_motion(at: Vector2, machine: Dictionary) -> void:
	if machine.buffer <= 0 and machine.progress <= 0.0:
		return
	var center := at + Vector2(24, 24)
	var angle: float = 0.0 if reduced_motion else tick * 2.6
	var direction := Vector2.RIGHT.rotated(angle)
	draw_line(center - direction * 8, center + direction * 8, Color("#d9e8e2"), 2.5)
	draw_circle(center, 3.0, GOLD)
	var amount := float(machine.buffer) / float(Model.MAX_BUFFER)
	draw_rect(Rect2(at + Vector2(9, 42), Vector2(30 * amount, 2)), AQUA)

func _draw_smelter_motion(at: Vector2, machine: Dictionary, cell: Vector2i) -> void:
	if machine.ore <= 0 and machine.output <= 0:
		return
	var center := at + Vector2(24, 25)
	var glow: float = 0.0 if reduced_motion else 0.14 * sin(tick * 7.0 + cell.x)
	draw_circle(center, 10.0, Color("#ffa158", 0.21 + glow))
	draw_circle(center + Vector2(0, 4), 4, Color("#ffdc8a", 0.75))
	if not reduced_motion:
		var spark := fposmod(tick * 11.0 + float(cell.x % 7), 18.0)
		draw_circle(center + Vector2(3, -spark), 1.7, Color("#ffd28b", 0.8 - spark / 22.0))
	var progress := clampf(float(machine.progress) / 2.0, 0.0, 1.0)
	draw_rect(Rect2(at + Vector2(10, 43), Vector2(28 * progress, 2.5)), GOLD)
