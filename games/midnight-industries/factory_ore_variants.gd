extends RefCounted
## Twelve genuinely distinct iron/copper/gold mineral formations. Each deposit is
## assembled from individual faceted boulders (not scaled copies of the old
## complete ore pile), with approved ore artwork sampled for surface material.
## Built once into a mipmapped atlas; no per-frame procedural drawing.
const Atlas = preload("res://factory_atlas.gd")
const GoldArt = preload("res://factory_gold_art.gd")
const SIZE := 112
const VARIANTS := 12
const LAYOUTS = [
	[Vector4(54, 68, 36, 31), Vector4(28, 82, 17, 13), Vector4(80, 84, 17, 14)],
	[Vector4(35, 71, 25, 22), Vector4(78, 76, 19, 17), Vector4(57, 49, 24, 23)],
	[Vector4(36, 75, 21, 17), Vector4(69, 78, 23, 20), Vector4(54, 43, 29, 27), Vector4(24, 91, 12, 9)],
	[Vector4(29, 72, 20, 18), Vector4(55, 76, 18, 17), Vector4(80, 73, 16, 16), Vector4(45, 45, 16, 15), Vector4(71, 46, 12, 11)],
	[Vector4(52, 70, 40, 25), Vector4(30, 90, 20, 12), Vector4(79, 88, 15, 13)],
	[Vector4(26, 77, 16, 15), Vector4(49, 80, 18, 18), Vector4(73, 77, 16, 14), Vector4(90, 84, 12, 11), Vector4(57, 54, 15, 13)],
	[Vector4(34, 80, 23, 18), Vector4(77, 80, 22, 19), Vector4(57, 57, 19, 18), Vector4(54, 33, 15, 16)],
	[Vector4(57, 78, 34, 20), Vector4(28, 88, 14, 12), Vector4(81, 58, 15, 16)],
	[Vector4(29, 82, 16, 14), Vector4(50, 82, 16, 15), Vector4(74, 81, 16, 16), Vector4(43, 58, 15, 15), Vector4(67, 55, 14, 14), Vector4(55, 37, 13, 12)],
	[Vector4(29, 74, 28, 23), Vector4(73, 80, 26, 20), Vector4(69, 46, 17, 18), Vector4(23, 93, 12, 8)],
	[Vector4(45, 76, 18, 17), Vector4(69, 81, 19, 15), Vector4(49, 52, 19, 16), Vector4(73, 39, 15, 17), Vector4(29, 87, 12, 10)],
	[Vector4(54, 80, 22, 18), Vector4(30, 78, 17, 16), Vector4(81, 80, 18, 17), Vector4(55, 57, 23, 21), Vector4(34, 38, 13, 14), Vector4(76, 38, 13, 12)]
]
static var _atlas: Texture2D
static var _cargo: Dictionary = {}

static func texture() -> Texture2D:
	if _atlas != null:
		return _atlas
	var result := Image.create(SIZE * VARIANTS, SIZE * 3, false, Image.FORMAT_RGBA8)
	result.fill(Color.TRANSPARENT)
	for material in range(3):
		var sprite: Texture2D = GoldArt.ore_texture() if material == 2 else Atlas.sprite(material, 1)
		if sprite == null:
			return null
		var source: Image = sprite.get_image()
		if source.is_compressed() and source.decompress() != OK:
			return null
		for variant in range(VARIANTS):
			var origin := Vector2i(variant * SIZE, material * SIZE)
			_cluster(result, origin, source, variant, material)
	result.generate_mipmaps()
	_atlas = ImageTexture.create_from_image(result)
	return _atlas

static func region(material: String, seed: int) -> Rect2:
	var row := 0 if material == "iron" else (1 if material == "copper" else 2)
	return Rect2(float(posmod(seed, VARIANTS) * SIZE), float(row * SIZE),
		float(SIZE), float(SIZE))

static func cargo_texture(material: String) -> Texture2D:
	if _cargo.has(material):
		return _cargo[material]
	var index := 0 if material == "iron" else 1
	var sprite: Texture2D = GoldArt.ore_texture() if material == "gold" else Atlas.sprite(index, 1)
	if sprite == null:
		return null
	var source: Image = sprite.get_image()
	var image := Image.create(80, 80, false, Image.FORMAT_RGBA8)
	image.fill(Color.TRANSPARENT)
	_paint_rock(image, Vector2i.ZERO, source, Vector4(40, 44, 26, 25), 41 + index * 17, 80)
	image.generate_mipmaps()
	var result := ImageTexture.create_from_image(image)
	_cargo[material] = result
	return result

static func _cluster(target: Image, offset: Vector2i, material_image: Image,
	variant: int, material: int) -> void:
	var pieces: Array = LAYOUTS[variant]
	# All contact shadows are behind the formation, not pasted on top of it.
	for i in range(pieces.size()):
		_shadow(target, offset, pieces[i], SIZE)
	# Paint rear rocks before front rocks; taller pieces actually overlap.
	var ordered := pieces.duplicate()
	ordered.sort_custom(func(a: Vector4, b: Vector4) -> bool:
		return a.y < b.y)
	for i in range(ordered.size()):
		_paint_rock(target, offset, material_image, ordered[i],
			variant * 37 + i * 11 + material * 149, SIZE)

static func _shadow(target: Image, origin: Vector2i, part: Vector4, size: int) -> void:
	var cx := part.x
	var cy := part.y + part.w * 0.67
	var rx := part.z * 0.95
	var ry := maxf(3.0, part.w * 0.25)
	for y in range(maxi(0, floori(cy - ry)), mini(size, ceili(cy + ry))):
		for x in range(maxi(0, floori(cx - rx)), mini(size, ceili(cx + rx))):
			var distance := pow((float(x) - cx) / rx, 2.0) + pow((float(y) - cy) / ry, 2.0)
			if distance > 1.0:
				continue
			var location := origin + Vector2i(x, y)
			var old: Color = target.get_pixelv(location)
			var shade := Color(0.015, 0.021, 0.025, (1.0 - distance) * 0.28)
			target.set_pixelv(location, old.blend(shade))

static func _paint_rock(target: Image, origin: Vector2i, source: Image,
	part: Vector4, seed: int, cell_size: int) -> void:
	var cx := part.x
	var cy := part.y
	var rx := part.z
	var ry := part.w
	var polygon := PackedVector2Array()
	for i in range(9):
		var angle := TAU * float(i) / 9.0 - PI * 0.5
		var jitter := 0.83 + 0.16 * (0.5 + 0.5 * sin(float(seed * 13 + i * 27)))
		polygon.append(Vector2(cx + cos(angle) * rx * jitter,
			cy + sin(angle) * ry * jitter))
	var min_x := maxi(0, floori(cx - rx) - 1)
	var max_x := mini(cell_size - 1, ceili(cx + rx) + 1)
	var min_y := maxi(0, floori(cy - ry) - 1)
	var max_y := mini(cell_size - 1, ceili(cy + ry) + 1)
	for y in range(min_y, max_y + 1):
		for x in range(min_x, max_x + 1):
			var point := Vector2(float(x) + 0.5, float(y) + 0.5)
			if not Geometry2D.is_point_in_polygon(point, polygon):
				continue
			var u := (point.x - cx) / maxf(1.0, rx)
			var v := (point.y - cy) / maxf(1.0, ry)
			# Material comes from the ORIGINAL approved mineral art, including
			# its natural surface grain and iron/copper color differences.
			var sx := clampi(roundi(67.0 + u * 31.0 + sin(float(seed)) * 8.0), 36, 104)
			var sy := clampi(roundi(69.0 + v * 25.0 + cos(float(seed)) * 7.0), 43, 101)
			var color: Color = source.get_pixel(sx, sy)
			if color.a < 0.2:
				color = source.get_pixel(71, 72)
			color.a = 1.0
			# Distinct facets create 3D boulders rather than miniature copies
			# of the complete old ore heap.
			if v < -0.30 + u * 0.19:
				color = color.lightened(0.20)
			elif u > 0.18 and v > -0.27:
				color = color.darkened(0.27)
			elif u < -0.25 and v > 0.05:
				color = color.darkened(0.13)
			if v > 0.50:
				color = color.darkened(0.22)
			if absf(v + 0.20 - u * 0.31) < 0.028 and u < 0.26:
				color = color.lightened(0.18)
			# One-pixel contour, keeping the painterly metallic edge.
			if not Geometry2D.is_point_in_polygon(point + Vector2(1.5, 1.5), polygon):
				color = color.darkened(0.29)
			target.set_pixelv(origin + Vector2i(x, y), color)
