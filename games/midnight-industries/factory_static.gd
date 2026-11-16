extends Node2D
## Chunk-sliced industrial terrain/ore/machines. Only edited chunks redraw.
const Chunk = preload("res://factory_chunk.gd")
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const SPAN := 16
var model
var chunks: Array = []

func _ready() -> void:
	for cy in range(ceili(float(Model.HEIGHT) / SPAN)):
		for cx in range(ceili(float(Model.WIDTH) / SPAN)):
			var part := Chunk.new()
			part.name = "IndustrialDeck_%02d_%02d" % [cx, cy]
			part.position = Vector2(cx * SPAN, cy * SPAN) * TILE
			part.chunk_origin = Vector2i(cx * SPAN, cy * SPAN)
			part.model = model
			add_child(part)
			chunks.append(part)

func refresh_cell(cell: Vector2i) -> void:
	if not model.inside(cell):
		return
	var cols: int = ceili(float(Model.WIDTH) / SPAN)
	var cx: int = cell.x / SPAN
	var cy: int = cell.y / SPAN
	var index: int = cy * cols + cx
	if index >= 0 and index < chunks.size():
		chunks[index].queue_redraw()

func refresh_all() -> void:
	for chunk in chunks:
		chunk.model = model
		chunk.queue_redraw()

func _draw() -> void:
	# Existing third-party/rehearsal callers that queue_redraw() remain valid.
	refresh_all()
