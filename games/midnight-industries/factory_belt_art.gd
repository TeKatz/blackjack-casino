extends RefCounted
## Four genuine directional conveyor textures. Right retains the approved
## original; left reverses only its moving deck, not the fixed steel housing.
## North/south are assembled from separate deck, rail and support samples so
## horizontal lighting and projecting motor parts never rotate as one image.
const Atlas = preload("res://factory_atlas.gd")
const SIZE := 144
static var _cached: Dictionary = {}

static func texture(direction: int) -> Texture2D:
	var d := posmod(direction, 4)
	if d == 0:
		return Atlas.sprite(1)
	if _cached.has(d):
		return _cached[d]
	var original: Texture2D = Atlas.sprite(1)
	if original == null:
		return null
	var source: Image = original.get_image()
	if source.is_compressed() and source.decompress() != OK:
		return null
	var target := Image.create(SIZE, SIZE, false, Image.FORMAT_RGBA8)
	target.fill(Color.TRANSPARENT)
	if d == 2:
		_left(target, source)
	else:
		_vertical(target, source, d == 1)
	target.generate_mipmaps()
	var result := ImageTexture.create_from_image(target)
	_cached[d] = result
	return result

static func _left(target: Image, source: Image) -> void:
	# Preserve the original side rails, lighting, bolts and orange mounts.
	target.blit_rect(source, Rect2i(0, 0, SIZE, SIZE), Vector2i.ZERO)
	# Only the moving track reverses; the roof and front steel still face
	# the same screen-space light. This is not a 180-degree chassis rotation.
	for y in range(53, 93):
		for x in range(16, 128):
			var opposite: Color = source.get_pixel(143 - x, y)
			var original: Color = source.get_pixel(x, y)
			var edge := mini(y - 53, 92 - y)
			var amount := clampf(float(edge) / 5.0, 0.0, 1.0)
			target.set_pixel(x, y, original.lerp(opposite, amount))

static func _vertical(target: Image, source: Image, south: bool) -> void:
	# The detailed roller tread is turned into a continuous up/down track;
	# the chassis/rail layers are sampled separately and relit afterwards.
	for y in range(SIZE):
		for x in range(25, 119):
			var source_x := y if south else 143 - y
			var source_y := 143 - x if south else x
			var pixel: Color = source.get_pixel(source_x, source_y)
			if x >= 48 and x < 96:
				# Central moving bed: approved cyan arrows, rollers, grain.
				target.set_pixel(x, y, pixel)
			elif pixel.a > 0.025:
				# Structural side rails keep their metallic texture.
				var brighten := 0.09 if x < 45 else -0.05
				target.set_pixel(x, y,
					pixel.lightened(brighten) if brighten > 0.0
					else pixel.darkened(-brighten))
	# Paired roller/motor supports are copied from the approved end-cap art.
	# The nodes are placed on the sides, not spun through 90 degrees.
	for step in [10, 57, 104]:
		_support(target, source, 13, step, true)
		_support(target, source, 113, step, false)
	# Direction-independent lighting on the exposed steel rails.
	for y in range(SIZE):
		_sample_line(target, 30, y, Color("#8DA1AA"), 0.43)
		_sample_line(target, 34, y, Color("#435763"), 0.55)
		_sample_line(target, 111, y, Color("#14232D"), 0.43)

static func _support(target: Image, source: Image,
	x: int, y: int, left: bool) -> void:
	# Sample metallic side hardware from the original at native resolution;
	# then add small bolt/status lights for an intentionally built chassis.
	for sy in range(25):
		for sx in range(18):
			var tx := x + sx
			var ty := y + sy
			if tx < 0 or tx >= SIZE or ty < 0 or ty >= SIZE:
				continue
			var sample_x := 6 + sx if left else 120 + sx
			var sample_y := 62 + (sy % 21)
			var pixel := source.get_pixel(sample_x, sample_y)
			if pixel.a > 0.02:
				target.set_pixel(tx, ty, pixel)
	_rect(target, x + 4, y + 7, 10, 4, Color("#0B1A24"))
	_rect(target, x + 6, y + 8, 6, 2, Color("#E5B260"))
	_rect(target, x + 5, y + 17, 9, 3, Color("#007E99"))
	_rect(target, x + 7, y + 17, 5, 1, Color("#74E6F3"))

static func _sample_line(target: Image, x: int, y: int, tint: Color, weight: float) -> void:
	var existing: Color = target.get_pixel(x, y)
	if existing.a > 0.03:
		target.set_pixel(x, y, existing.lerp(tint, weight))

static func _rect(target: Image, x: int, y: int, w: int, h: int, color: Color) -> void:
	if w > 0 and h > 0:
		target.fill_rect(Rect2i(x, y, w, h), color)
