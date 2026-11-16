extends RefCounted
## Industrial sprite-backed assets. No legacy primitive machine glyphs.
const Atlas = preload("res://factory_atlas.gd")
const BeltArt = preload("res://factory_belt_art.gd")
const TILE := 48.0
const DIRS := [Vector2.RIGHT, Vector2.DOWN, Vector2.LEFT, Vector2.UP]
const GOLD := Color("#F4BD59")
const CYAN := Color("#00D8F4")
const RED := Color("#FF725F")
## Up to 2.4x camera zoom: sample the 144px originals with filtering instead
## of magnifying the retired 48px thumbnails.

static func machine(canvas: CanvasItem, at: Vector2, kind: String, direction: int, ghost: bool = false) -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null:
		return
	var index: int = int({"miner": 0, "belt": 1, "smelter": 2, "storage": 3}.get(kind, 0))
	var opacity := 0.68 if ghost else 1.0
	var center := at + Vector2(TILE * 0.5, TILE * 0.5)
	# Conveyor art rests flush on the deck. Heavy circular drop shadows
	# made every belt and ore node look like a floating dark disc.
	if not ghost and kind != "belt":
		canvas.draw_rect(Rect2(at + Vector2(12, 40), Vector2(24, 3)),
			Color(0.0, 0.0, 0.0, 0.13))

	if kind == "belt":
		if posmod(direction, 2) == 1:
			# The vertical chassis is drawn in its own top-down perspective.
			var vertical: Texture2D = BeltArt.texture(posmod(direction, 4))
			canvas.draw_texture_rect(vertical, Rect2(at, Vector2(TILE, TILE)),
				false, Color(1.0, 1.0, 1.0, opacity))
		else:
			# Horizontal atlas art remains unchanged (and already approved).
			canvas.draw_set_transform(center, PI if direction == 2 else 0.0, Vector2.ONE)
			canvas.draw_texture_rect_region(texture, Rect2(Vector2(-24, -24), Vector2(48, 48)),
				Atlas.region(index), Color(1.0, 1.0, 1.0, opacity))
			canvas.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	else:
		var source: Rect2 = machine_source(kind)
		var destination: Rect2 = machine_destination(kind, at)
		canvas.draw_texture_rect_region(texture, destination, source,
			Color(1.0, 1.0, 1.0, opacity))
	if ghost:
		# Strong readable machine output port, even when the art itself is busy.
		arrow(canvas, center, direction, GOLD, 19.0)
		for c in [Vector2(2, 2), Vector2(46, 2), Vector2(2, 46), Vector2(46, 46)]:
			canvas.draw_circle(at + c, 2.0, Color("#AFFFE8"))
	else:
		var normal: Vector2 = DIRS[posmod(direction, 4)]
		canvas.draw_circle(center + normal * 20.0, 3.0, GOLD)
		canvas.draw_circle(center + normal * 20.0, 1.25, Color.WHITE)

static func machine_source(kind: String) -> Rect2:
	var index: int = int({"miner": 0, "belt": 1, "smelter": 2, "storage": 3}.get(kind, 0))
	var region: Rect2 = Atlas.region(index)
	if kind == "storage":
		# Inset source prevents bleeding in from the adjacent furnace cell.
		region.position += Vector2(4.0, 1.0)
		region.size -= Vector2(8.0, 2.0)
	return region

static func machine_destination(kind: String, at: Vector2) -> Rect2:
	if kind == "storage":
		# The previous negative X offset caused overlapping neighbors.
		return Rect2(at + Vector2(1, 0), Vector2(46, 48))
	if kind == "smelter":
		# Optical adjustment only, no impact on logical machine position.
		return Rect2(at + Vector2(0, -2), Vector2(48, 48))
	return Rect2(at, Vector2(48, 48))

static func cargo_source(item: String) -> Rect2:
	var index: int = int({"iron_ore": 0, "copper_ore": 1,
		"iron_ingot": 2, "copper_ingot": 3}.get(item, 0))
	return Atlas.region(index, 1)

static func ore_layout(seed: int) -> Array:
	# Keep the existing metal textures, and alter only how pieces are
	# clustered on the ground. These layouts change shapes, not materials.
	var patterns := [
		[Vector2(1, 0), Vector2(-13, 10)],
		[Vector2(-5, -3), Vector2(14, 10), Vector2(-14, 13)],
		[Vector2(2, 2), Vector2(12, -10)],
		[Vector2(-6, 4), Vector2(11, 11), Vector2(16, -8)],
		[Vector2(4, -2), Vector2(-14, -7)],
		[Vector2(0, 1), Vector2(-12, 11), Vector2(14, 13)],
		[Vector2(-3, 0), Vector2(14, 2)],
		[Vector2(5, 3), Vector2(-11, -11), Vector2(11, -9)]
	]
	return patterns[posmod(seed, patterns.size())]

static func ore(canvas: CanvasItem, at: Vector2, material: String, seed: int) -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null:
		return
	var index := 0 if material == "iron" else 1
	var source: Rect2 = Atlas.region(index, 1)
	var layout := ore_layout(seed)
	var main_center := at + Vector2(24, 24) + Vector2(layout[0])
	# The approved original mineral sprites remain the visual source.
	# Their different arrangements create small natural-looking clusters.
	for i in range(layout.size() - 1, 0, -1):
		var p := at + Vector2(24, 24) + Vector2(layout[i])
		var size := 19.0 + float(posmod(seed + i * 3, 5))
		canvas.draw_texture_rect_region(texture,
			Rect2(p - Vector2.ONE * size * 0.5, Vector2.ONE * size),
			source, Color(0.90, 0.90, 0.90, 0.90))
	var main_size := 42.0 + float(posmod(seed * 3, 5))
	canvas.draw_texture_rect_region(texture,
		Rect2(main_center - Vector2.ONE * main_size * 0.5, Vector2.ONE * main_size),
		source)

static func cargo(canvas: CanvasItem, center: Vector2, item: String, shade: float = 1.0) -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null:
		return

	var alpha := clampf(shade, 0.1, 1.0)
	# Restore the previously approved source artwork for all four cargo
	# types instead of drawing different procedural ingots and rocks.
	canvas.draw_rect(Rect2(center + Vector2(-5, 6), Vector2(10, 2)),
		Color(0.0, 0.0, 0.0, alpha * 0.17))
	canvas.draw_texture_rect_region(texture,
		Rect2(center - Vector2(12, 13), Vector2(24, 24)),
		cargo_source(item), Color(1.0, 1.0, 1.0, alpha))

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
