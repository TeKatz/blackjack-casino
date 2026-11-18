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
var effect_quality := 2
## off: never show links; auto: while building/inspecting power; always: every frame.
var power_line_mode := "auto"
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
	model._update_power()
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
						Art.cargo(self, center, str(machine.item), 1.0, int(machine.direction))
				"miner":
					_miner_vfx(at, machine, cell)
				"smelter":
					_smelter_vfx(at, machine, cell)
				"constructor":
					_constructor_vfx(at, machine, cell)
				"storage":
					_storage_vfx(at, cell)
				"depot":
					_depot_vfx(at, cell)
				"water_wheel":
					_wheel_vfx(at, cell)
				"power_pole":
					_pole_vfx(at, cell)
			if model.power.demand(str(machine.kind)) > 0:
				var power_state: String = model.power.status(cell)
				if power_state in ["no_power", "insufficient"]:
					_power_warning(at, power_state, cell)
	_power_network_vfx()
	_selection_vfx()
	if menu_open or not model.inside(hovered):
		return
	var position := Vector2(hovered) * TILE
	if cursor_mode == "build":
		_hologram(position, model.can_place(hovered, tool, facing))
	elif model.anchor_at(hovered).x >= 0:
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
	if not reduced_motion and effect_quality > 0:
		var shimmer := fposmod(tick * 5.0 + cell.x * 0.17, 1.0)
		draw_circle(hub + Vector2(10, 10 - shimmer * 10.0), 1.5,
			Color("#FFA92C", 0.45 * (1.0 - shimmer)))
	var ratio := float(machine.buffer) / float(Model.MAX_BUFFER)
	draw_rect(Rect2(at + Vector2(7, 44), Vector2(34, 2)), Color("#0B161B"))
	draw_rect(Rect2(at + Vector2(7, 44), Vector2(34 * ratio, 2)), CYAN)

func _smelter_vfx(at: Vector2, machine: Dictionary, cell: Vector2i) -> void:
	if not model.power.is_powered(cell):
		draw_rect(Rect2(at + Vector2(11, 9), Vector2(27, 28)),
			Color("#10212B", 0.16))
		draw_line(at + Vector2(17, 15), at + Vector2(32, 30),
			Color("#EF6969", 0.64), 2.5)
		return
	if machine.ore <= 0 and machine.output <= 0:
		return
	var center := at + Vector2(24, 29)
	var pulse := 0.0 if reduced_motion else sin(tick * 7.0 + cell.x) * 0.12
	draw_circle(center, 23.0, Color("#FF6E18", 0.052 + pulse * 0.13))
	draw_circle(center, 16.0, Color("#FF982D", 0.090 + pulse * 0.19))
	draw_circle(center, 9.0, Color("#FFC369", 0.16 + pulse * 0.15))
	if not reduced_motion and effect_quality > 0:
		for i in range(1 if effect_quality == 1 else 3):
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

func _constructor_vfx(at: Vector2, machine: Dictionary, cell: Vector2i) -> void:
	if not model.power.is_powered(cell):
		return
	if int(machine.ore) <= 0 and int(machine.output) <= 0:
		return
	var center: Vector2 = at + Vector2(26, 22)
	var strength: float = 0.60 if reduced_motion else 0.50 + sin(tick * 6.0 + cell.x) * 0.22
	draw_circle(center, 10.0, Color("#3EE0F0", strength * 0.11))
	draw_circle(center, 4.2, Color("#BCF9FC", strength * 0.37))
	var ratio: float = clampf(float(machine.progress) / 3.0, 0.0, 1.0)
	draw_rect(Rect2(at + Vector2(9, 44), Vector2(30, 2)), Color("#132C38"))
	draw_rect(Rect2(at + Vector2(9, 44), Vector2(30.0 * ratio, 2)),
		Color("#68D2E8"))
	if not reduced_motion and effect_quality > 0:
		var travel := fposmod(tick * 2.2, 1.0)
		draw_circle(at + Vector2(17 + 12 * travel, 18), 1.5,
			Color("#FFD18B", 0.65 * (1.0 - travel)))

func _storage_vfx(at: Vector2, cell: Vector2i) -> void:
	var phase := 0.85 if reduced_motion else 0.75 + sin(tick * 2.4 + cell.x) * 0.20
	draw_circle(at + Vector2(38, 36), 3.2, Color(CYAN, phase * 0.4))
	draw_circle(at + Vector2(38, 36), 1.6, Color(CYAN, phase))

func _depot_vfx(at: Vector2, cell: Vector2i) -> void:
	var pulse := 0.78 if reduced_motion else 0.74 + 0.18 * sin(tick * 2.3 + float(cell.x))
	draw_circle(at + Vector2(25, 13), 5.5,
		Color("#00C4E4", 0.11 * pulse))
	draw_circle(at + Vector2(25, 13), 2.8,
		Color("#9CECF6", 0.55 * pulse))
	if not reduced_motion:
		draw_line(at + Vector2(16, 40), at + Vector2(32, 40),
			Color("#19D3EC", 0.26 + 0.28 * pulse), 1.7)

func _wheel_vfx(at: Vector2, cell: Vector2i) -> void:
	var direction: int = int(model.buildings[cell].direction)
	var water_at: Vector2 = at + Vector2(Model.DIRS[direction]) * TILE
	var hub := water_at + Vector2(22, 23)
	var glow := 0.70 if reduced_motion else 0.62 + 0.24 * sin(tick * 3.1 + float(cell.x))
	draw_circle(hub, 8.5, Color("#7CF2FF", 0.12 * glow))
	draw_circle(hub, 3.1, Color("#C6FDFF", 0.52 * glow))
	var angle := 0.3 if reduced_motion else tick * 1.8
	for index in range(4):
		var spoke: Vector2 = Vector2.RIGHT.rotated(angle + float(index) * PI / 2.0)
		draw_line(hub, hub + spoke * 10.0, Color("#E6B471", 0.48), 1.6)
	if not reduced_motion:
		var ripple: float = fposmod(tick * 0.65 + float(cell.y) * 0.1, 1.0)
		draw_arc(water_at + Vector2(23, 37), 5.0 + ripple * 12.0,
			0.15, PI - 0.1, 16, Color("#B6F9FF", 0.36 * (1.0 - ripple)), 1.9)

func _pole_vfx(at: Vector2, cell: Vector2i) -> void:
	var pulse := 0.78 if reduced_motion else 0.65 + 0.23 * sin(tick * 4.0 + float(cell.y))
	draw_circle(at + Vector2(25, 10), 5.3, Color("#47DDF1", pulse * 0.19))
	draw_circle(at + Vector2(25, 10), 2.5, Color("#A4F8FC", pulse))

func _power_network_vfx() -> void:
	# Show virtual power links only while designing power distribution or
	# inspecting a power structure. They are not physical conveyor routes.
	var design := cursor_mode == "build" and tool in ["power_pole", "water_wheel"]
	var selected_power: bool = model.buildings.has(inspected) \
		and str(model.buildings[inspected].kind) in ["power_pole", "water_wheel"]
	if power_line_mode == "off":
		return
	if power_line_mode != "always" and not design and not selected_power:
		return
	for link in model.power.edges:
		var from: Vector2 = (Vector2(link[0]) + Vector2.ONE * 0.5) * TILE
		var to: Vector2 = (Vector2(link[1]) + Vector2.ONE * 0.5) * TILE
		draw_line(from, to, Color("#092C3E", 0.57), 4.0)
		draw_line(from, to, Color("#72DDED", 0.66), 1.8)
		var travel := 0.42 if reduced_motion else fposmod(tick * 0.65, 1.0)
		draw_circle(from.lerp(to, travel), 2.4, Color("#B3FAFF", 0.86))

func _selection_vfx() -> void:
	var anchor: Vector2i = model.anchor_at(inspected)
	if not model.inside(anchor) or not model.buildings.has(anchor):
		return
	var kind: String = str(model.buildings[anchor].kind)
	var direction: int = int(model.buildings[anchor].direction)
	var glow := 0.83 if reduced_motion else 0.71 + 0.18 * sin(tick * 3.3)
	for cell in model.footprint_cells(anchor, kind, direction):
		var p: Vector2 = Vector2(cell) * TILE
		draw_rect(Rect2(p + Vector2(0.5, 0.5), Vector2(47, 47)),
			Color(CYAN, 0.11 * glow))
		_brackets(p, GOLD, glow, 4.0)
		if not reduced_motion:
			var scan := 7.0 + fposmod(tick * 18.0, 34.0)
			draw_line(p + Vector2(7, scan), p + Vector2(41, scan),
				Color(CYAN, 0.13), 1.1)

func _power_warning(at: Vector2, state: String, cell: Vector2i) -> void:
	# A floating, animated alert is legible even when a furnace is idle.
	# Disconnected = red; connected but oversubscribed = amber.
	var color: Color = Color("#F16469") if state == "no_power" else Color("#F3B856")
	var offset: float = 0.0 if reduced_motion else sin(tick * 2.6 + float(cell.x + cell.y)) * 3.4
	var center: Vector2 = at + Vector2(24.0, -9.0 + offset)
	var pulse: float = 0.95 if reduced_motion else 0.78 + 0.18 * sin(tick * 3.8)
	draw_circle(center, 14.0, Color("#081A25", 0.74))
	draw_circle(center, 11.5, Color(color, 0.12 * pulse))
	draw_colored_polygon(PackedVector2Array([
		center + Vector2(-9, -8), center + Vector2(9, -8),
		center + Vector2(9, 8), center + Vector2(-9, 8)]),
		Color("#152833", 0.94))
	draw_rect(Rect2(center - Vector2(9, 8), Vector2(18, 16)),
		Color(color, 0.84 * pulse), false, 1.6)
	# A jagged electric bolt with a break on overloaded circuits.
	var bolt := PackedVector2Array([
		center + Vector2(1, -7), center + Vector2(-5, 1),
		center + Vector2(-1, 1), center + Vector2(-3, 7),
		center + Vector2(6, -2), center + Vector2(2, -2)])
	draw_colored_polygon(bolt, color)
	if state == "no_power":
		draw_line(center + Vector2(-7, 7), center + Vector2(7, -7),
			Color("#FFF1EE", 0.90), 1.9)
	else:
		draw_line(center + Vector2(-7, 8), center + Vector2(7, 8),
			Color("#FDF1CA", 0.90), 1.6)

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
	for cell in model.footprint_cells(hovered, tool, facing):
		var at: Vector2 = Vector2(cell) * TILE
		draw_rect(Rect2(at + Vector2(1, 1), Vector2(46, 46)),
			Color(tint, phase * 0.20))
		_brackets(at, tint, phase, 2.5)
	if can_build:
		Art.machine(self, p, tool, facing, true)
		var scan := 5.0 if reduced_motion else 3.0 + fposmod(tick * 27.0, 40.0)
		draw_line(p + Vector2(4, scan), p + Vector2(44, scan),
			Color(CYAN, 0.55), 1.8)
		if tool not in ["power_pole", "water_wheel"]:
			Art.arrow(self, p + Vector2(24, 24), facing, GOLD, 21.0)
	else:
		draw_line(p + Vector2(12, 12), p + Vector2(36, 36),
			Color(ERROR, 0.85), 3)
		draw_line(p + Vector2(36, 12), p + Vector2(12, 36),
			Color(ERROR, 0.85), 3)
