extends RefCounted
## Shared, scale-independent machine art for the factory and its build ghost.
const TILE := 48.0
const DIRS := [Vector2.RIGHT, Vector2.DOWN, Vector2.LEFT, Vector2.UP]
const FRAME := Color("#182d34")
const EDGE := Color("#54727d")
const GOLD := Color("#f2cb79")
const STEEL := Color("#82acb4")
const COPPER := Color("#e2a070")

static func machine(canvas: CanvasItem, at: Vector2, kind: String, direction: int, ghost: bool = false) -> void:
	var opacity := 0.56 if ghost else 1.0
	var center := at + Vector2(24, 24)
	canvas.draw_rect(Rect2(at + Vector2(2, 4), Vector2(44, 42)), Color(0.01, 0.035, 0.047, 0.50 * opacity))
	canvas.draw_rect(Rect2(at + Vector2(3, 2), Vector2(42, 42)), Color("#213740", opacity))
	canvas.draw_rect(Rect2(at + Vector2(4, 3), Vector2(40, 39)), Color("#5f8290", opacity), false, 1.4)
	match kind:
		"miner":
			canvas.draw_rect(Rect2(at + Vector2(10, 9), Vector2(28, 29)), Color("#3f6270", opacity))
			canvas.draw_circle(center, 12, Color("#102933", opacity))
			canvas.draw_arc(center, 11, 0.0, TAU, 16, Color("#91c8c6", opacity), 3, true)
			for i in range(6):
				var d := Vector2.RIGHT.rotated(float(i) * TAU / 6.0)
				canvas.draw_line(center + d * 7, center + d * 13, Color("#bcd4ce", opacity), 2.5)
			canvas.draw_circle(center, 5, Color("#b2d8cf", opacity))
			canvas.draw_rect(Rect2(at + Vector2(8, 35), Vector2(32, 4)), Color("#17282e", opacity))
		"belt":
			canvas.draw_rect(Rect2(at + Vector2(7, 8), Vector2(34, 32)), Color("#0e222b", opacity))
			canvas.draw_rect(Rect2(at + Vector2(10, 11), Vector2(28, 26)), Color("#42606c", opacity))
			if direction % 2 == 0:
				canvas.draw_line(at + Vector2(8, 9), at + Vector2(40, 9), Color("#849ba0", opacity), 2)
				canvas.draw_line(at + Vector2(8, 39), at + Vector2(40, 39), Color("#849ba0", opacity), 2)
			else:
				canvas.draw_line(at + Vector2(8, 9), at + Vector2(8, 39), Color("#849ba0", opacity), 2)
				canvas.draw_line(at + Vector2(40, 9), at + Vector2(40, 39), Color("#849ba0", opacity), 2)
		"smelter":
			canvas.draw_rect(Rect2(at + Vector2(9, 7), Vector2(30, 35)), Color("#617482", opacity))
			canvas.draw_rect(Rect2(at + Vector2(12, 12), Vector2(24, 25)), Color("#17272c", opacity))
			canvas.draw_rect(Rect2(at + Vector2(15, 21), Vector2(18, 12)), Color("#bc563a", opacity))
			canvas.draw_rect(Rect2(at + Vector2(18, 24), Vector2(12, 8)), Color("#f7aa57", opacity))
			canvas.draw_rect(Rect2(at + Vector2(10, 6), Vector2(9, 6)), Color("#a9b5b8", opacity))
			canvas.draw_rect(Rect2(at + Vector2(29, 6), Vector2(9, 6)), Color("#a9b5b8", opacity))
		"storage":
			canvas.draw_rect(Rect2(at + Vector2(8, 8), Vector2(32, 31)), Color("#927454", opacity))
			canvas.draw_rect(Rect2(at + Vector2(12, 12), Vector2(24, 22)), Color("#c5a27c", opacity), false, 2.0)
			canvas.draw_line(at + Vector2(24, 9), at + Vector2(24, 38), Color("#4e443b", opacity), 3)
			canvas.draw_line(at + Vector2(9, 23), at + Vector2(39, 23), Color("#4e443b", opacity), 3)
	for offset in [Vector2(6, 6), Vector2(40, 6), Vector2(6, 39), Vector2(40, 39)]:
		canvas.draw_circle(at + offset, 1.5, Color("#bacbd0", opacity))
	arrow(canvas, center, direction, Color(GOLD, opacity), 15.0 if ghost else 12.5)
	if ghost:
		canvas.draw_rect(Rect2(at + Vector2(1, 1), Vector2(46, 46)), Color("#a6e9ce", 0.78), false, 2)

static func arrow(canvas: CanvasItem, center: Vector2, direction: int, color: Color, reach: float = 13.0) -> void:
	var d: Vector2 = DIRS[posmod(direction, 4)]
	var side := Vector2(-d.y, d.x)
	var tip := center + d * reach
	var root := center + d * (reach - 10)
	canvas.draw_line(root - d * 4, tip - d * 6, Color("#122832", color.a), 3)
	canvas.draw_colored_polygon(PackedVector2Array([tip, tip - d * 8 + side * 5, tip - d * 8 - side * 5]), color)

static func cargo(canvas: CanvasItem, center: Vector2, item: String, shade: float = 1.0) -> void:
	var metal := COPPER if item.begins_with("copper") else STEEL
	metal = metal * Color(shade, shade, shade)
	if item.ends_with("ingot"):
		canvas.draw_rect(Rect2(center + Vector2(-7, -5), Vector2(14, 11)), Color(0.02, 0.09, 0.11, 0.5))
		canvas.draw_rect(Rect2(center + Vector2(-7, -8), Vector2(14, 11)), metal)
		canvas.draw_line(center + Vector2(-6, -7), center + Vector2(5, -7), Color("#f6e3be"), 1.6)
	else:
		canvas.draw_colored_polygon(PackedVector2Array([
			center + Vector2(-7, 4), center + Vector2(-4, -7),
			center + Vector2(4, -9), center + Vector2(8, -2),
			center + Vector2(5, 6)]), metal)
		canvas.draw_line(center + Vector2(-4, -5), center + Vector2(3, -7), Color("#e5e8d6"), 1.6)
