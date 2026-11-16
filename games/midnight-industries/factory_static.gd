extends Node2D
## Keeps the entire map in screen-cullable 16x16 cached CanvasItems.
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
			part.show_behind_parent = false
			add_child(part)
			chunks.append(part)

func _draw() -> void:
	# Legacy callers call queue_redraw() when a building is changed.
	# Only the draw command cache is invalidated; simulation is untouched.
	for chunk in chunks:
		chunk.model = model
		chunk.queue_redraw()
