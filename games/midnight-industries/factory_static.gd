extends Node2D
## Static factory objects are cached by CanvasItem until a building changes.
const Model = preload("res://factory_model.gd")
const Art = preload("res://factory_art.gd")
const TILE := 48.0
const IRON := Color("#88b2c0")
const COPPER := Color("#dfa078")
var model

func _draw() -> void:
	if model == null:
		return
	# Ore outcrops and buildings never invalidate the animated layer.
	for cell in model.resources:
		var material: String = model.resources[cell]
		_draw_deposit(Vector2(cell) * TILE, material, cell)
	for cell in model.buildings:
		var machine: Dictionary = model.buildings[cell]
		Art.machine(self, Vector2(cell) * TILE, str(machine.kind), int(machine.direction))

func _draw_deposit(origin: Vector2, material: String, cell: Vector2i) -> void:
	var metal: Color = IRON if material == "iron" else COPPER
	var center := origin + Vector2(24, 24)
	draw_circle(center, 18, Color(metal, 0.07))
	for i in range(3):
		var dx := float((cell.x * 17 + cell.y * 7 + i * 13) % 26) - 13.0
		var dy := float((cell.y * 13 + cell.x * 5 + i * 11) % 22) - 11.0
		var p := center + Vector2(dx, dy)
		draw_colored_polygon(PackedVector2Array([
			p + Vector2(-8, 5), p + Vector2(-4, -6),
			p + Vector2(4, -9), p + Vector2(9, 3),
			p + Vector2(2, 8)]), metal.darkened(0.30))
		draw_line(p + Vector2(-4, -5), p + Vector2(4, -8), metal.lightened(0.28), 1.8)
