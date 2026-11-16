extends RefCounted
## Industrial sprite-backed assets. No legacy primitive machine glyphs.
const Atlas = preload("res://factory_atlas.gd")
const OreVariants = preload("res://factory_ore_variants.gd")
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
		canvas.draw_set_transform(center, float(posmod(direction, 4)) * PI * 0.5, Vector2.ONE)
		canvas.draw_texture_rect_region(texture, Rect2(Vector2(-24, -24), Vector2(48, 48)),
			Atlas.region(index), Color(1.0, 1.0, 1.0, opacity))
		canvas.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	else:
		# Warehouse source silhouette is biased to the right in its atlas cell.
		# Shift artwork only: gameplay coordinates and port stay unchanged.
		var sprite_offset := Vector2(-3.5, 0.5) if kind == "storage" else Vector2.ZERO
		canvas.draw_texture_rect_region(texture, Rect2(at + sprite_offset, Vector2(48, 48)),
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
	var atlas: Texture2D = OreVariants.texture()
	if atlas == null:
		return
	# Distinct 96px silhouettes with independent crystal facets and colors.
	# Light positional variation helps prevent an obvious wallpaper pattern.
	var d := Vector2(float(posmod(seed * 13, 7) - 3), float(posmod(seed * 19, 7) - 3))
	var size := 50.0 + float(posmod(seed, 4)) * 1.0
	var center := at + Vector2(24, 24) + d
	canvas.draw_texture_rect_region(atlas, Rect2(center - Vector2.ONE * size * 0.5,
		Vector2.ONE * size), OreVariants.region(material, seed))

static func cargo(canvas: CanvasItem, center: Vector2, item: String, shade: float = 1.0) -> void:
	# Transported products have actual raised geometry, not decals pasted flat
	# onto a conveyor texture. Geometry is fast and has a compact contact shade.
	var alpha := clampf(shade, 0.1, 1.0)
	var shadow := Color(0.02, 0.05, 0.055, 0.20 * alpha)
	canvas.draw_colored_polygon(PackedVector2Array([
		center + Vector2(-7, 6), center + Vector2(7, 6),
		center + Vector2(9, 8), center + Vector2(-5, 8)]), shadow)
	if item.ends_with("_ingot"):
		var copper := item.begins_with("copper")
		var top := Color("#F5B679") if copper else Color("#C9E0E5")
		var face := Color("#B46C40") if copper else Color("#849DA9")
		var side := Color("#764730") if copper else Color("#526A78")
		var outline := Color("#342F2C") if copper else Color("#273842")
		canvas.draw_colored_polygon(PackedVector2Array([
			center + Vector2(-8, -6), center + Vector2(4, -8),
			center + Vector2(9, -2), center + Vector2(-4, 0)]), Color(top, alpha))
		canvas.draw_colored_polygon(PackedVector2Array([
			center + Vector2(-4, 0), center + Vector2(9, -2),
			center + Vector2(9, 5), center + Vector2(-4, 6)]), Color(face, alpha))
		canvas.draw_colored_polygon(PackedVector2Array([
			center + Vector2(-8, -6), center + Vector2(-4, 0),
			center + Vector2(-4, 6), center + Vector2(-8, 1)]), Color(side, alpha))
		canvas.draw_line(center + Vector2(-7, -6), center + Vector2(4, -8),
			Color.WHITE if not copper else Color("#FFE0A2"), 1.1)
		canvas.draw_line(center + Vector2(-4, 6), center + Vector2(9, 5), outline, 0.9)
	else:
		var material := "copper" if item.begins_with("copper") else "iron"
		var atlas: Texture2D = OreVariants.texture()
		var variant := 5 if material == "copper" else 2
		canvas.draw_texture_rect_region(atlas, Rect2(center + Vector2(-11, -15),
			Vector2(22, 22)), OreVariants.region(material, variant),
			Color(1, 1, 1, alpha))
		canvas.draw_line(center + Vector2(-6, -9), center + Vector2(-1, -12),
			Color("#EFD6B9") if material == "copper" else Color("#DCF4F5"), 1.2)

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
