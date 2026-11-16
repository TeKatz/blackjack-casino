extends RefCounted
## Render the approved 144px machinery, ore and cargo artwork. Only on-ground
## ore composition varies; resources on belts never get replacement graphics.
const Atlas = preload("res://factory_atlas.gd")
const BeltArt = preload("res://factory_belt_art.gd")
const TILE := 48.0
const DIRS := [Vector2.RIGHT, Vector2.DOWN, Vector2.LEFT, Vector2.UP]
const GOLD := Color("#F4BD59")
const CYAN := Color("#00D8F4")
const RED := Color("#FF725F")

static func machine(canvas: CanvasItem, at: Vector2, kind: String, direction: int, ghost: bool = false) -> void:
	var index: int = int({"miner": 0, "belt": 1, "smelter": 2, "storage": 3}.get(kind, 0))
	var texture: Texture2D = Atlas.sprite(index)
	if texture == null:
		return
	var opacity := 0.68 if ghost else 1.0
	var center := at + Vector2(TILE * 0.5, TILE * 0.5)
	if not ghost and kind != "belt":
		canvas.draw_rect(Rect2(at + Vector2(12, 40), Vector2(24, 3)),
			Color(0.0, 0.0, 0.0, 0.13))
	if kind == "belt":
		if posmod(direction, 2) == 1:
			var vertical: Texture2D = BeltArt.texture(posmod(direction, 4))
			if vertical != null:
				canvas.draw_texture_rect(vertical, Rect2(at, Vector2(TILE, TILE)),
					false, Color(1.0, 1.0, 1.0, opacity))
		else:
			canvas.draw_set_transform(center, PI if direction == 2 else 0.0, Vector2.ONE)
			canvas.draw_texture_rect(texture, Rect2(Vector2(-24, -24), Vector2(48, 48)),
				false, Color(1.0, 1.0, 1.0, opacity))
			canvas.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	else:
		# Each building samples a standalone texture and stays inside its
		# logical tile. Furnace optical alignment was already approved.
		canvas.draw_texture_rect(texture, machine_destination(kind, at),
			false, Color(1.0, 1.0, 1.0, opacity))
	if ghost:
		arrow(canvas, center, direction, GOLD, 19.0)
		for c in [Vector2(2, 2), Vector2(46, 2), Vector2(2, 46), Vector2(46, 46)]:
			canvas.draw_circle(at + c, 2.0, Color("#AFFFE8"))
	else:
		var normal: Vector2 = DIRS[posmod(direction, 4)]
		canvas.draw_circle(center + normal * 20.0, 3.0, GOLD)
		canvas.draw_circle(center + normal * 20.0, 1.25, Color.WHITE)

static func machine_source(kind: String) -> Rect2:
	var index: int = int({"miner": 0, "belt": 1, "smelter": 2, "storage": 3}.get(kind, 0))
	return Atlas.region(index)

static func machine_destination(kind: String, at: Vector2) -> Rect2:
	if kind == "storage":
		return Rect2(at, Vector2(48, 48))
	if kind == "smelter":
		return Rect2(at + Vector2(0, -2), Vector2(48, 48))
	return Rect2(at, Vector2(48, 48))

static func cargo_source(item: String) -> Rect2:
	var index: int = int({"iron_ore": 0, "copper_ore": 1,
		"iron_ingot": 2, "copper_ingot": 3}.get(item, 0))
	return Atlas.region(index, 1)

static func ore_layout(seed: int) -> Array:
	# Whole approved ore boulder/cluster sprite, eight subtle arrangements;
	# avoid building new rocks or pasting several tiny identical piles onto
	# each cell. Small rotations and aspect/position differences are enough.
	var patterns := [
		[Vector2(-2, 1), Vector2(1.00, 0.96), -0.07],
		[Vector2(2, -1), Vector2(0.88, 1.03), 0.09],
		[Vector2(-3, -2), Vector2(1.04, 0.91), -0.13],
		[Vector2(1, 2), Vector2(0.96, 1.05), 0.04],
		[Vector2(3, 0), Vector2(0.93, 0.94), 0.14],
		[Vector2(-1, -3), Vector2(1.02, 1.02), -0.03],
		[Vector2(-3, 2), Vector2(0.98, 0.88), -0.11],
		[Vector2(2, 2), Vector2(0.90, 1.00), 0.12]
	]
	return patterns[posmod(seed, patterns.size())]

static func ore(canvas: CanvasItem, at: Vector2, material: String, seed: int) -> void:
	var index := 0 if material == "iron" else 1
	var texture: Texture2D = Atlas.sprite(index, 1)
	if texture == null:
		return
	var layout: Array = ore_layout(seed)
	var offset: Vector2 = layout[0]
	var scale: Vector2 = layout[1]
	var angle: float = layout[2]
	var center := at + Vector2(24, 24) + offset
	canvas.draw_set_transform(center, angle, scale)
	canvas.draw_texture_rect(texture, Rect2(Vector2(-23, -23), Vector2(46, 46)), false)
	canvas.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)

static func cargo(canvas: CanvasItem, center: Vector2, item: String, shade: float = 1.0) -> void:
	var index: int = int({"iron_ore": 0, "copper_ore": 1,
		"iron_ingot": 2, "copper_ingot": 3}.get(item, 0))
	var texture: Texture2D = Atlas.sprite(index, 1)
	if texture == null:
		return
	var alpha := clampf(shade, 0.1, 1.0)
	# Exact source material art and original on-belt sizing/placement.
	canvas.draw_rect(Rect2(center + Vector2(-4, 6), Vector2(8, 2)),
		Color(0.0, 0.0, 0.0, alpha * 0.15))
	canvas.draw_texture_rect(texture, Rect2(center - Vector2(12, 12), Vector2(24, 24)),
		false, Color(1.0, 1.0, 1.0, alpha))

static func arrow(canvas: CanvasItem, center: Vector2, direction: int,
	color: Color = GOLD, reach: float = 18.0) -> void:
	var d: Vector2 = DIRS[posmod(direction, 4)]
	var side := Vector2(-d.y, d.x)
	var tip := center + d * reach
	var base := tip - d * 10.0
	canvas.draw_line(base - d * 8, base, Color("#091B25"), 5.0)
	canvas.draw_line(base - d * 8, base, color, 2.6)
	canvas.draw_colored_polygon(PackedVector2Array([
		tip, base + side * 6.0, base - side * 6.0]), color)
