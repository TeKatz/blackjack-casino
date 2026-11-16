extends RefCounted
## Imported hand-directed industrial sprite atlas (four machines, four materials).
## Five script chunks are bundled as Godot resources to keep the desktop export
## independent of any runtime download or asset-conversion step.
const P0 = preload("res://assets/atlas_part_0.gd")
const P1 = preload("res://assets/atlas_part_1.gd")
const P2 = preload("res://assets/atlas_part_2.gd")
const P3 = preload("res://assets/atlas_part_3.gd")
const P4 = preload("res://assets/atlas_part_4.gd")
const CELL := 48
static var cached: Texture2D

static func get_texture() -> Texture2D:
	if cached != null:
		return cached
	var data: PackedByteArray = Marshalls.base64_to_raw(
		P0.CHUNK + P1.CHUNK + P2.CHUNK + P3.CHUNK + P4.CHUNK)
	var image := Image.new()
	var err := image.load_webp_from_buffer(data)
	if err != OK or image.get_width() != CELL * 4 or image.get_height() != CELL * 2:
		push_error("Midnight Industries industrial atlas failed to decode")
		return null
	cached = ImageTexture.create_from_image(image)
	return cached

static func region(col: int, row: int = 0) -> Rect2:
	return Rect2(float(col * CELL), float(row * CELL), CELL, CELL)
