extends RefCounted
## Eight individually generated iron and copper cluster textures, not rotated
## copies of a single bitmap. Each uses distinct boulder arrangements, facets
## and material-specific veining. Created once, then cached on the GPU.
const SIZE := 96
const VARIANTS := 8
static var cached: Texture2D

static func texture() -> Texture2D:
	if cached != null:
		return cached
	var im := Image.create(SIZE * VARIANTS, SIZE * 2, false, Image.FORMAT_RGBA8)
	var noise := FastNoiseLite.new()
	noise.seed = 49571
	noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH
	noise.frequency = 0.18
	for material in range(2):
		for variant in range(VARIANTS):
			_cluster(im, material, variant, noise)
	im.generate_mipmaps()
	cached = ImageTexture.create_from_image(im)
	return cached

static func region(material: String, seed: int) -> Rect2:
	var row := 0 if material == "iron" else 1
	return Rect2(float(posmod(seed, VARIANTS) * SIZE), float(row * SIZE), SIZE, SIZE)

static func _cluster(im: Image, material: int, variant: int, noise: FastNoiseLite) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = int(310703 + material * 16691 + variant * 10009)
	var offset := Vector2i(variant * SIZE, material * SIZE)
	var count := 3 + variant % 4
	# Cluster morphology varies, not just its orientation. Shapes interlock
	# into irregular piles of exposed mineral with per-cluster unique facets.
	for i in range(count):
		var middle := Vector2(48.0, 49.0)
		if i > 0:
			middle += Vector2(rng.randf_range(-25.0, 25.0), rng.randf_range(-22.0, 20.0))
		else:
			middle += Vector2(rng.randf_range(-8.0, 8.0), rng.randf_range(-5.0, 5.0))
		var width := rng.randf_range(20.0, 31.0) if i == 0 else rng.randf_range(10.0, 21.0)
		var height := rng.randf_range(17.0, 27.0) if i == 0 else rng.randf_range(9.0, 20.0)
		var corners := 6 + (i + variant) % 3
		var angle := rng.randf_range(-0.35, 0.35)
		var polygon := PackedVector2Array()
		for corner in range(corners):
			var theta := TAU * float(corner) / float(corners) - PI * 0.5 + angle
			var irregular := rng.randf_range(0.76, 1.12)
			var point := middle + Vector2(cos(theta) * width, sin(theta) * height) * irregular
			polygon.append(point)
		_raster(im, offset, polygon, middle, Vector2(width, height), material, noise, rng)
		var count_veins := 1 + (i + variant) % 3
		for v in range(count_veins):
			var from := middle + Vector2(rng.randf_range(-width * 0.45, width * 0.10), rng.randf_range(-height * 0.40, 0.0))
			var to := from + Vector2(rng.randf_range(4.0, width * 0.54), rng.randf_range(1.0, height * 0.42))
			var mineral := Color("#B7E1D9") if material == 0 else Color("#EFA86C")
			_vein(im, offset, polygon, from, to, mineral)
	# A few distinct ore flecks reinforce identity without ground-sized shadows.
	for i in range(3 + variant % 3):
		var v := Vector2i(rng.randi_range(22, 75), rng.randi_range(36, 76))
		var c := Color("#87ABAD") if material == 0 else Color("#B86D3D")
		_dot(im, offset + v, c)

static func _raster(im: Image, offset: Vector2i, polygon: PackedVector2Array,
	middle: Vector2, extents: Vector2, material: int, noise: FastNoiseLite,
	rng: RandomNumberGenerator) -> void:
	var min_x := 95
	var min_y := 95
	var max_x := 0
	var max_y := 0
	for p in polygon:
		min_x = mini(min_x, floori(p.x) - 2)
		min_y = mini(min_y, floori(p.y) - 2)
		max_x = maxi(max_x, ceili(p.x) + 2)
		max_y = maxi(max_y, ceili(p.y) + 2)
	var tone := rng.randf_range(-0.09, 0.09)
	var base: Color = Color("#718D9B") if material == 0 else Color("#B77243")
	for y in range(maxi(2, min_y), mini(SIZE - 2, max_y + 1)):
		for x in range(maxi(2, min_x), mini(SIZE - 2, max_x + 1)):
			var point := Vector2(float(x) + 0.5, float(y) + 0.5)
			var is_rock := Geometry2D.is_point_in_polygon(point, polygon)
			if not is_rock:
				# Only a tight contact shade underneath each boulder.
				if Geometry2D.is_point_in_polygon(point - Vector2(0.0, 2.0), polygon):
					im.set_pixel(offset.x + x, offset.y + y, Color(0.09, 0.09, 0.08, 0.13))
				continue
			var fx := (point.x - middle.x) / maxf(1.0, extents.x)
			var fy := (point.y - middle.y) / maxf(1.0, extents.y)
			var grain := noise.get_noise_2d(offset.x * 2 + x * 1.9, offset.y * 3 + y * 1.9)
			var brightness := clampf(0.16 - fy * 0.22 - fx * 0.13 + grain * 0.18 + tone, -0.36, 0.42)
			var color: Color = base.lightened(brightness) if brightness > 0.0 else base.darkened(-brightness)
			# Different exposed crystal planes for each fragment.
			if fx - fy < -0.26:
				color = color.lightened(0.14)
			elif fx + fy > 0.34:
				color = color.darkened(0.19)
			# Rich metal flecks, not a flat repeated icon.
			if grain > 0.43 and fy < 0.25:
				color = color.lightened(0.22)
			var edge := not Geometry2D.is_point_in_polygon(point + Vector2(1.5, 1.5), polygon)
			if edge:
				color = color.darkened(0.31)
			im.set_pixel(offset.x + x, offset.y + y, color)

static func _vein(im: Image, offset: Vector2i, polygon: PackedVector2Array,
	from: Vector2, to: Vector2, color: Color) -> void:
	var steps := maxi(1, int(from.distance_to(to) * 1.6))
	for step in range(steps):
		var p := from.lerp(to, float(step) / float(steps))
		if not Geometry2D.is_point_in_polygon(p, polygon):
			continue
		var x := clampi(roundi(p.x), 0, SIZE - 1)
		var y := clampi(roundi(p.y), 0, SIZE - 1)
		im.set_pixel(offset.x + x, offset.y + y, color)

static func _dot(im: Image, at: Vector2i, color: Color) -> void:
	if at.x >= 0 and at.x < im.get_width() and at.y >= 0 and at.y < im.get_height():
		if im.get_pixelv(at).a < 0.02:
			im.set_pixelv(at, Color(color, 0.63))
