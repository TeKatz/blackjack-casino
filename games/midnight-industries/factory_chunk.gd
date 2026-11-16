extends Node2D
## Draw only ore and placed machines. The meadow/stone biome is a single
## shader quad beneath these cached 16x16 factory chunks.
const Art = preload("res://factory_art.gd")
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const SPAN := 16
var model
var chunk_origin := Vector2i.ZERO

func _ready() -> void:
	texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS

func _draw() -> void:
	if model == null:
		return
	for gy in range(chunk_origin.y, mini(chunk_origin.y + SPAN, Model.HEIGHT)):
		for gx in range(chunk_origin.x, mini(chunk_origin.x + SPAN, Model.WIDTH)):
			var cell := Vector2i(gx, gy)
			var p := Vector2(gx - chunk_origin.x, gy - chunk_origin.y) * TILE
			if model.resources.has(cell):
				Art.ore(self, p, str(model.resources[cell]), gx * 77 + gy * 19)
			if model.buildings.has(cell):
				var machine: Dictionary = model.buildings[cell]
				Art.machine(self, p, str(machine.kind), int(machine.direction))
