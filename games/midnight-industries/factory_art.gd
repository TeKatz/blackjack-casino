extends RefCounted
## Industrial sprite-backed assets. No legacy primitive machine glyphs.
const Atlas = preload("res://factory_atlas.gd")
const TILE := 48.0
const DIRS := [Vector2.RIGHT, Vector2.DOWN, Vector2.LEFT, Vector2.UP]
const GOLD := Color("#F4BD59")
const CYAN := Color("#00D8F4")
const RED := Color("#FF725F")

static func machine(canvas: CanvasItem, at: Vector2, kind: String, direction: int, ghost: bool = false) -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null:
		return
	var index: int = int({"miner": 0, "belt": 1, "smelter": 2, "storage": 3}.get(kind, 0))
	var opacity := 0.68 if ghost else 1.0
	var center := at + Vector2(TILE * 0.5, TILE * 0.5)
	if not ghost:
		canvas.draw_circle(center + Vector2(0, 4), 24.0, Color(0.0, 0.0, 0.0, 0.30))
		canvas.draw_rect(Rect2(at + Vector2(3, 5), Vector2(42, 40)), Color("#131719", 0.58))
	if kind == "belt":
		canvas.draw_set_transform(center, float(posmod(direction, 4)) * PI * 0.5, Vector2.ONE)
		canvas.draw_texture_rect_region(texture, Rect2(Vector2(-24, -24), Vector2(48, 48)),
			Atlas.region(index), Color(1.0, 1.0, 1.0, opacity))
		canvas.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	else:
		canvas.draw_texture_rect_region(texture, Rect2(at, Vector2(48, 48)),
			Atlas.region(index), Color(1.0, 1.0, 1.0, opacity))
	if ghost:
		# Strong readable machine output port, even when the art itself is busy.
		arrow(canvas, center, direction, GOLD, 19.0)
		for c in [Vector2(2, 2), Vector2(46, 2), Vector2(2, 46), Vector2(46, 46)]:
			canvas.draw_circle(at + c, 2.0, Color("#AFFFE8"))
	else:
		var normal: Vector2 = DIRS[posmod(direction, 4)]
		canvas.draw_circle(center + normal * 20.0, 3.0, GOLD)
		canvas.draw_circle(center + normal * 20.0, 1.25, Color.WHITE)

static func ore(canvas: CanvasItem, at: Vector2, material: String, seed: int) -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null:
		return
	var i := 0 if material == "iron" else 1
	var angle := float(posmod(seed, 4)) * (PI * 0.5)
	var offset := Vector2(float(posmod(seed * 13, 7) - 3), float(posmod(seed * 19, 7) - 3))
	var center := at + Vector2(24, 24) + offset
	canvas.draw_circle(center + Vector2(0, 4), 21.0, Color(0.0, 0.0, 0.0, 0.18))
	canvas.draw_set_transform(center, angle, Vector2.ONE * (0.90 + float(posmod(seed, 5)) * 0.028))
	canvas.draw_texture_rect_region(texture, Rect2(-23, -23, 46, 46), Atlas.region(i, 1))
	canvas.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)

static func cargo(canvas: CanvasItem, center: Vector2, item: String, shade: float = 1.0) -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null:
		return
	var index := 0
	match item:
		"iron_ore": index = 0
		"copper_ore": index = 1
		"iron_ingot": index = 2
		"copper_ingot": index = 3
	var alpha := clampf(shade, 0.1, 1.0)
	canvas.draw_circle(center + Vector2(0, 5), 10.0, Color(0, 0, 0, alpha * 0.34))
	canvas.draw_texture_rect_region(texture, Rect2(center - Vector2(12, 12), Vector2(24, 24)),
		Atlas.region(index, 1), Color(1, 1, 1, alpha))

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
