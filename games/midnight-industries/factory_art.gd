extends RefCounted
## Illustrated machine sprites and 12 independently composed ore deposits.
## Cargo is smaller physical material carried inside belt guide rails, with a
## directional contact shadow, not a copy of the full ground-deposit image.
const Atlas = preload("res://factory_atlas.gd")
const BeltArt = preload("res://factory_belt_art.gd")
const OreVariants = preload("res://factory_ore_variants.gd")
const GoldArt = preload("res://factory_gold_art.gd")
const DepotArt = preload("res://factory_depot_art.gd")
const PowerArt = preload("res://factory_power_art.gd")
const TILE := 48.0
const DIRS := [Vector2.RIGHT, Vector2.DOWN, Vector2.LEFT, Vector2.UP]
const GOLD := Color("#F4BD59")
const CYAN := Color("#00D8F4")

static func machine(canvas: CanvasItem, at: Vector2, kind: String, direction: int, ghost: bool = false) -> void:
	var index: int = int({"miner": 0, "belt": 1, "smelter": 2, "storage": 3}.get(kind, 0))
	var opacity := 0.68 if ghost else 1.0
	var center := at + Vector2(24, 24)
	if not ghost and kind != "belt":
		canvas.draw_rect(Rect2(at + Vector2(12, 40), Vector2(24, 3)),
			Color(0.0, 0.0, 0.0, 0.13))
	if kind == "belt":
		var belt: Texture2D = BeltArt.texture(direction)
		if belt != null:
			canvas.draw_texture_rect(belt, Rect2(at, Vector2(48, 48)),
				false, Color(1.0, 1.0, 1.0, opacity))
	else:
		var texture: Texture2D = PowerArt.texture(kind) if kind in ["water_wheel", "power_pole"] else (
			DepotArt.texture() if kind == "depot" else Atlas.sprite(index))
		if texture != null:
			canvas.draw_texture_rect(texture, machine_destination(kind, at),
				false, Color(1.0, 1.0, 1.0, opacity))
		if kind == "depot":
			canvas.draw_rect(Rect2(at + Vector2(17, 37), Vector2(15, 3)),
				Color("#16D4F0", opacity * 0.64))
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
	return Rect2(Vector2.ZERO, Vector2(144, 144)) if kind in ["water_wheel", "power_pole"] 		else Atlas.region(index)

static func machine_destination(kind: String, at: Vector2) -> Rect2:
	if kind == "smelter":
		# Existing accepted furnace/belt alignment, intentionally unchanged.
		return Rect2(at + Vector2(0, -2), Vector2(48, 48))
	return Rect2(at, Vector2(48, 48))

static func cargo_source(item: String) -> Rect2:
	var index: int = int({"iron_ore": 0, "copper_ore": 1,
		"iron_ingot": 2, "copper_ingot": 3}.get(item, 0))
	return Atlas.region(index, 1)

static func ore_layout(seed: int) -> Array:
	# Exposes the actual composition variant index for smoke coverage.
	return [posmod(seed, OreVariants.VARIANTS)]

static func ore(canvas: CanvasItem, at: Vector2, material: String, seed: int) -> void:
	var variants: Texture2D = OreVariants.texture()
	if variants == null:
		return
	canvas.draw_texture_rect_region(variants,
		Rect2(at + Vector2(-4, -4), Vector2(56, 56)),
		OreVariants.region(material, seed))

static func cargo(canvas: CanvasItem, center: Vector2, item: String,
	shade: float = 1.0, direction: int = 0) -> void:
	var opacity := clampf(shade, 0.1, 1.0)
	var d := posmod(direction, 4)
	var ore_item := item.ends_with("_ore")
	var mineral := "gold" if item.begins_with("gold") else (
		"copper" if item.begins_with("copper") else "iron")
	var cargo_texture: Texture2D
	if ore_item:
		cargo_texture = OreVariants.cargo_texture(mineral)
	elif mineral == "gold":
		cargo_texture = GoldArt.ingot_texture()
	else:
		cargo_texture = Atlas.sprite(3 if mineral == "copper" else 2, 1)
	if cargo_texture == null:
		return
	# The contact patch stays on the conveyor bed while the ore/billet sits
	# a little higher. It also follows the interpolated item position.
	var footprint := PackedVector2Array([
		center + Vector2(-9, 4), center + Vector2(-5, 2),
		center + Vector2(7, 3), center + Vector2(11, 6),
		center + Vector2(6, 9), center + Vector2(-7, 9)])
	canvas.draw_colored_polygon(footprint,
		Color(0.005, 0.012, 0.018, 0.32 * opacity))
	canvas.draw_colored_polygon(PackedVector2Array([
		center + Vector2(-7, 0), center + Vector2(7, 0),
		center + Vector2(9, 6), center + Vector2(-7, 6)]),
		Color(0.06, 0.10, 0.13, 0.38 * opacity))
	var size := Vector2(29, 28) if ore_item else Vector2(22, 20)
	var position := center - size * 0.5 + Vector2(0, -4)
	canvas.draw_texture_rect(cargo_texture, Rect2(position, size), false,
		Color(1.0, 1.0, 1.0, opacity))
	# Thin front-guide occlusion makes the object sit *between* the rail
	# walls rather than hovering as a decal over the whole belt image.
	var rail := Color(0.11, 0.20, 0.25, opacity * 0.76)
	var highlight := Color(0.48, 0.65, 0.72, opacity * 0.58)
	if d == 0 or d == 2:
		canvas.draw_line(center + Vector2(-10, 9),
			center + Vector2(10, 9), rail, 2.5)
		canvas.draw_line(center + Vector2(-10, 8),
			center + Vector2(10, 8), highlight, 0.9)
	else:
		canvas.draw_line(center + Vector2(9, -11),
			center + Vector2(9, 10), rail, 2.5)
		canvas.draw_line(center + Vector2(8, -11),
			center + Vector2(8, 10), highlight, 0.9)

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
