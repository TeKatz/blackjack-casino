extends Node2D
## Small static terrain/ore/machine chunk. Godot culls offscreen CanvasItems.
const Art = preload("res://factory_art.gd")
const Model = preload("res://factory_model.gd")
const TILE := 48.0
const SPAN := 16
const STEEL_A := Color("#252C33")
const STEEL_B := Color("#303A41")
const SEAM := Color("#121C23")
const EDGE := Color("#53616A")
const AMBER := Color("#F3B34C")
var model
var chunk_origin := Vector2i.ZERO

func _draw() -> void:
	if model == null:
		return
	for gy in range(chunk_origin.y, mini(chunk_origin.y + SPAN, Model.HEIGHT)):
		for gx in range(chunk_origin.x, mini(chunk_origin.x + SPAN, Model.WIDTH)):
			var cell := Vector2i(gx, gy)
			var p := Vector2(gx - chunk_origin.x, gy - chunk_origin.y) * TILE
			_tile(p, cell)
			if model.resources.has(cell):
				Art.ore(self, p, str(model.resources[cell]), gx * 77 + gy * 19)
			if model.buildings.has(cell):
				var machine: Dictionary = model.buildings[cell]
				Art.machine(self, p, str(machine.kind), int(machine.direction))

func _tile(p: Vector2, cell: Vector2i) -> void:
	var n := posmod(cell.x * 101 + cell.y * 47, 17)
	var body := STEEL_B if n < 4 else STEEL_A
	draw_rect(Rect2(p, Vector2(TILE, TILE)), SEAM)
	draw_rect(Rect2(p + Vector2(2, 2), Vector2(44, 43)), body)
	draw_line(p + Vector2(3, 3), p + Vector2(44, 3), Color("#657179", 0.35), 1.0)
	draw_line(p + Vector2(3, 4), p + Vector2(3, 44), Color("#66757D", 0.19), 1.0)
	draw_line(p + Vector2(44, 5), p + Vector2(44, 43), Color("#050D13", 0.44), 1.5)
	draw_line(p + Vector2(4, 44), p + Vector2(43, 44), Color("#060F15", 0.57), 2.0)
	if posmod(cell.x, 2) == 0 and posmod(cell.y, 2) == 0:
		for q in [Vector2(5, 5), Vector2(42, 5), Vector2(5, 42), Vector2(42, 42)]:
			draw_circle(p + q, 1.3, Color("#71818A", 0.72))
			draw_circle(p + q + Vector2(0, 0.5), 0.6, Color("#111F29"))
	var seed := posmod(cell.x * 23 + cell.y * 81, 97)
	if seed < 19:
		var sx := 11.0 + float(posmod(seed * 3, 19))
		var sy := 12.0 + float(posmod(seed * 7, 19))
		draw_line(p + Vector2(sx, sy), p + Vector2(sx + 8, sy - 1),
			Color("#A1A3A3", 0.09), 0.7)
	if seed == 11 or seed == 72:
		# Inset maintenance hatch, genuinely recessed rather than a flat grid.
		draw_rect(Rect2(p + Vector2(10, 8), Vector2(28, 32)), Color("#101D24"))
		draw_rect(Rect2(p + Vector2(13, 11), Vector2(22, 26)), Color("#44545B"), false, 1.1)
		for row in range(4):
			draw_line(p + Vector2(16, 15 + row * 6), p + Vector2(32, 15 + row * 6),
				Color("#17252D"), 2.4)
	if seed == 30 or seed == 49:
		# Industrial hazard-warning plate, low contrast away from machines.
		draw_rect(Rect2(p + Vector2(5, 34), Vector2(38, 8)), Color("#3F3220"))
		for stripe in range(5):
			var x := 7.0 + float(stripe) * 8.0
			draw_line(p + Vector2(x, 42), p + Vector2(x + 6, 34),
				Color(AMBER, 0.54), 3.3)
	if seed == 62 and not model.resources.has(cell):
		draw_circle(p + Vector2(24, 24), 13, Color("#E8A74A", 0.045))
		draw_rect(Rect2(p + Vector2(19, 19), Vector2(10, 10)), Color("#182329"))
		draw_circle(p + Vector2(24, 24), 3, Color("#EBA84C", 0.73))
		draw_circle(p + Vector2(24, 24), 1, Color("#FFE5A8"))
