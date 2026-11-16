extends Node2D
## GPU-friendly, visible-sector cinematic feedback; simulation stays authoritative.
const Model = preload("res://factory_model.gd")
const Art = preload("res://factory_art.gd")
const TILE := 48.0
const GOLD := Color("#F5C04A")
const CYAN := Color("#00D8F4")
const GHOST := Color("#8CFFE2")
const ERROR := Color("#FF5B50")
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
	var world_origin: Vector2 = camera.position - view * 0.5
	var left := clampi(floori(world_origin.x / TILE) - 2, 0, Model.WIDTH - 1)
	var top := clampi(floori(world_origin.y / TILE) - 2, 0, Model.HEIGHT - 1)
	var right := clampi(ceili((world_origin.x + view.x) / TILE) + 2, 0, Model.WIDTH - 1)
	var bottom := clampi(ceili((world_origin.y + view.y) / TILE) + 2, 0, Model.HEIGHT - 1)
	var transition: float = clampf(model.accumulator / Model.STEP, 0.0, 1.0)
	# One pass over visible machines only. SceneNodes are not created per cargo.
	for y in range(top, bottom + 1):
		for x in range(left, right + 1):
			var cell := Vector2i(x, y)
			if not model.buildings.has(cell):
				continue
			var machine: Dictionary = model.buildings[cell]
			var at := Vector2(cell) * TILE
			match str(machine.kind):
				"belt":
					_belt_vfx(at, int(machine.direction))
					if str(machine.item) != "":
						var center := at + Vector2(24, 24)
						if model.transit.has(cell):
							var trace: Dictionary = model.transit[cell]
							if trace.item == machine.item:
								var source: Vector2i = trace["from"]
								var blend := transition * transition * (3.0 - 2.0 * transition)
								center = (Vector2(source) + Vector2(0.5, 0.5)).lerp(
									Vector2(cell) + Vector2(0.5, 0.5), blend) * TILE
						Art.cargo(self, center, str(machine.item))
				"miner":
					_miner_vfx(at, machine, cell)
				"smelter":
					_smelter_vfx(at, machine, cell)
				"storage":
					_storage_vfx(at, cell)
	_selection_vfx()
	if menu_open or not model.inside(hovered):
		return
	var position := Vector2(hovered) * TILE
	if cursor_mode == "build":
		_hologram(position, model.can_place(hovered, tool))
	elif model.buildings.has(hovered):
		_brackets(position, CYAN, 0.76, 3.0)
	elif model.resources.has(hovered):
		_brackets(position, Color("#80B9FF"), 0.43, 2.0)

func _belt_vfx(at: Vector2, direction: int) -> void:
	var forward: Vector2 = Vector2(Model.DIRS[direction])
	var sideways := Vector2(-forward.y, forward.x)
	var middle := at + Vector2(24, 24)
	var shift := 0.0 if reduced_motion else fposmod(tick * 48.0, 13.0)
	for segment in range(-2, 3):
		var offset := float(segment * 13) + shift
		if absf(offset) > 12:
			continue
		var spot := middle + forward * offset
		var strength := 0.20 + 0.16 * (1.0 - absf(offset) / 13.0)
		var edge := spot + forward * 4.0
		draw_line(spot - sideways * 6.0 - forward * 2.0, edge, Color(CYAN, strength), 2.2)
		draw_line(spot + sideways * 6.0 - forward * 2.0, edge, Color(CYAN, strength), 2.2)

func _miner_vfx(at: Vector2, machine: Dictionary, cell: Vector2i) -> void:
	if machine.buffer <= 0 and machine.progress <= 0.0:
		return
	var hub := at + Vector2(29, 23)
	var angle: float = 0.0 if reduced_motion else tick * 4.6
	var bar := Vector2.RIGHT.rotated(angle) * 6.3
	draw_line(hub - bar, hub + bar, Color("#DAF5FF", 0.74), 2.1)
	draw_circle(hub, 2.0, CYAN)
	if not reduced_motion:
		var shimmer := fposmod(tick * 5.0 + cell.x * 0.17, 1.0)
		draw_circle(hub + Vector2(10, 10 - shimmer * 10.0), 1.5,
			Color("#FFA92C", 0.45 * (1.0 - shimmer)))
	var ratio := float(machine.buffer) / float(Model.MAX_BUFFER)
	draw_rect(Rect2(at + Vector2(7, 44), Vector2(34, 2)), Color("#0B161B"))
	draw_rect(Rect2(at + Vector2(7, 44), Vector2(34 * ratio, 2)), CYAN)

func _smelter_vfx(at: Vector2, machine: Dictionary, cell: Vector2i) -> void:
	if machine.ore <= 0 and machine.output <= 0:
		return
	var center := at + Vector2(24, 29)
	var pulse := 0.0 if reduced_motion else sin(tick * 7.0 + cell.x) * 0.12
	draw_circle(center, 23.0, Color("#FF6E18", 0.052 + pulse * 0.13))
	draw_circle(center, 16.0, Color("#FF982D", 0.090 + pulse * 0.19))
	draw_circle(center, 9.0, Color("#FFC369", 0.16 + pulse * 0.15))
	if not reduced_motion:
		for i in range(3):
			var phase := fposmod(tick * 0.73 + float(i) * 0.27 + float(cell.y % 7) * 0.13, 1.0)
			var rise := phase * 22.0
			var drift := sin(tick * 1.4 + float(i) * 1.8) * 5.0
			draw_circle(center + Vector2(-7.0 + drift + float(i) * 5.0, -rise), 2.2 - phase,
				Color("#CFD6D3", 0.14 * (1.0 - phase)))
			var spark := fposmod(tick * 0.89 + float(i) * 0.34, 1.0)
			draw_circle(center + Vector2(float(i * 8 - 8), 2.0 - 17.0 * spark), 1.6,
				Color("#FFD582", 0.9 * (1.0 - spark)))
	var progress := clampf(float(machine.progress) / 2.0, 0.0, 1.0)
	draw_rect(Rect2(at + Vector2(8, 45), Vector2(32, 2)), Color("#171A1C"))
	draw_rect(Rect2(at + Vector2(8, 45), Vector2(32.0 * progress, 2)), GOLD)

func _storage_vfx(at: Vector2, cell: Vector2i) -> void:
	var phase := 0.85 if reduced_motion else 0.75 + sin(tick * 2.4 + cell.x) * 0.20
	draw_circle(at + Vector2(38, 36), 3.2, Color(CYAN, phase * 0.4))
	draw_circle(at + Vector2(38, 36), 1.6, Color(CYAN, phase))

func _selection_vfx() -> void:
	if not model.inside(inspected) or not model.buildings.has(inspected):
		return
	var p := Vector2(inspected) * TILE
	var glow := 0.83 if reduced_motion else 0.71 + 0.18 * sin(tick * 3.3)
	draw_rect(Rect2(p + Vector2(0.5, 0.5), Vector2(47, 47)),
		Color(CYAN, 0.11 * glow))
	_brackets(p, GOLD, glow, 4.0)
	if not reduced_motion:
		var scan := 7.0 + fposmod(tick * 18.0, 34.0)
		draw_line(p + Vector2(7, scan), p + Vector2(41, scan),
			Color(CYAN, 0.13), 1.1)

func _brackets(p: Vector2, color: Color, alpha: float, thickness: float) -> void:
	var glow := Color(color, alpha * 0.19)
	var bright := Color(color, alpha)
	for x in [3.0, 45.0]:
		for y in [3.0, 45.0]:
			var xs := 1.0 if x < 24.0 else -1.0
			var ys := 1.0 if y < 24.0 else -1.0
			var corner := p + Vector2(x, y)
			draw_line(corner, corner + Vector2(xs * 12, 0), glow, thickness + 4)
			draw_line(corner, corner + Vector2(0, ys * 12), glow, thickness + 4)
			draw_line(corner, corner + Vector2(xs * 12, 0), bright, thickness)
			draw_line(corner, corner + Vector2(0, ys * 12), bright, thickness)
			draw_circle(corner, 1.7, Color.WHITE)

func _hologram(p: Vector2, can_build: bool) -> void:
	var tint := CYAN if can_build else ERROR
	var phase := 0.8 if reduced_motion else 0.66 + 0.12 * sin(tick * 5.8)
	draw_rect(Rect2(p + Vector2(1, 1), Vector2(46, 46)), Color(tint, phase * 0.21))
	_brackets(p, tint, phase, 2.5)
	if can_build:
		Art.machine(self, p, tool, facing, true)
		var scan := 5.0 if reduced_motion else 3.0 + fposmod(tick * 27.0, 40.0)
		draw_line(p + Vector2(4, scan), p + Vector2(44, scan), Color(CYAN, 0.65), 1.8)
		Art.arrow(self, p + Vector2(24, 24), facing, GOLD, 21.0)
	else:
		draw_rect(Rect2(p + Vector2(5, 5), Vector2(38, 38)), Color(ERROR, 0.13))
		draw_line(p + Vector2(12, 12), p + Vector2(36, 36), Color(ERROR, 0.85), 3)
		draw_line(p + Vector2(36, 12), p + Vector2(12, 36), Color(ERROR, 0.85), 3)
