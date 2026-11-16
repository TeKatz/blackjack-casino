extends RefCounted
## Native 144-pixel industrial artwork, prepared from the original high-detail
## source sheet. Godot imports this compressed WebP into a GPU texture and
## retains detail at close zoom; the former 48px embedded atlas is gone.
const CELL := 144
const TEXTURE = preload("res://assets/industrial_hd_atlas.webp")

static func get_texture() -> Texture2D:
	return TEXTURE

static func region(col: int, row: int = 0) -> Rect2:
	return Rect2(float(col * CELL), float(row * CELL), float(CELL), float(CELL))
