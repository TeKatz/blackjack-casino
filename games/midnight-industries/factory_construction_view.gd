extends Node2D
## Permanent 12x12 casino construction foundation, with one inbound
## material dock at each grid position along all four sides (48 in total).
## Only visual scaffolding lives here; the factory model owns every receipt.
const Construction = preload("res://factory_construction.gd")
const UiSkin = preload("res://factory_skin.gd")
const TILE := 48.0
const STEEL := Color("#334B58")
const GOLD := Color("#F2B951")
const CYAN := Color("#10CDE6")
var model

func _ready() -> void:
	texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS
	queue_redraw()

func _draw() -> void:
	if model == null:
		return
	var start := Vector2(Construction.ORIGIN) * TILE
	var extent := Vector2(Construction.WIDTH, Construction.HEIGHT) * TILE
	var frame := Rect2(start, extent)
	# The outer foundation is larger than a single machine and has its own
	# legible geometry, not a repeated factory-machine sprite.
	draw_rect(Rect2(start - Vector2(8, 8), extent + Vector2(16, 16)),
		Color(0.013, 0.025, 0.032, 0.36))
	draw_rect(frame, Color("#111D24"))
	draw_rect(Rect2(start + Vector2(10, 10), extent - Vector2(20, 20)),
		Color("#263640"))
	for k in range(Construction.WIDTH + 1):
		var xx := start.x + float(k) * TILE
		draw_line(Vector2(xx, start.y + 12),
			Vector2(xx, start.y + extent.y - 12), Color(0.55, 0.72, 0.76, 0.12), 1.0)
	for k in range(Construction.HEIGHT + 1):
		var yy := start.y + float(k) * TILE
		draw_line(Vector2(start.x + 12, yy),
			Vector2(start.x + extent.x - 12, yy), Color(0.55, 0.72, 0.76, 0.12), 1.0)
	# Interior footprint and incomplete structural beams.
	var inset := Rect2(start + Vector2(88, 88), extent - Vector2(176, 176))
	draw_rect(inset, Color("#17252D"))
	draw_rect(inset, Color("#64757A"), false, 5.0)
	draw_rect(Rect2(inset.position + Vector2(17, 17),
		inset.size - Vector2(34, 34)), Color("#293A42"), false, 2.0)
	for t in range(5):
		var xx := inset.position.x + 26.0 + float(t) * (inset.size.x - 52.0) / 4.0
		draw_line(Vector2(xx, inset.position.y + 26),
			Vector2(xx, inset.end.y - 26), Color(0.24, 0.36, 0.42, 0.28), 3.0)
	# Industrial corner clamps, foundation bolts and perimeter ports.
	draw_rect(frame, STEEL, false, 7.0)
	draw_rect(Rect2(start + Vector2(7, 7), extent - Vector2(14, 14)),
		GOLD.darkened(0.26), false, 2.0)
	for x in range(Construction.WIDTH):
		_port(start, 0, x)
		_port(start, 2, x)
	for y in range(Construction.HEIGHT):
		_port(start, 1, y)
		_port(start, 3, y)
	for p in [
		start + Vector2(17, 17),
		start + Vector2(extent.x - 17, 17),
		start + Vector2(17, extent.y - 17),
		start + extent - Vector2(17, 17)
	]:
		draw_circle(p, 7.0, Color("#091720"))
		draw_circle(p, 3.0, GOLD)
	var font: Font = UiSkin.font()
	var center := start + extent * 0.5
	draw_string(font, Vector2(start.x + 64, center.y - 45),
		"PROJECT MIDNIGHT", HORIZONTAL_ALIGNMENT_CENTER,
		extent.x - 128, 26, Color("#E6ECED"))
	var display_phase: int = model.construction.phase_number()
	var phase_name := "CASINO  /  %02d OF %02d" % [display_phase, Construction.PHASE_COUNT]
	if model.construction.completed_phases >= Construction.PHASE_COUNT:
		phase_name = "CASINO CONSTRUCTION COMPLETE"
	draw_string(font, Vector2(start.x + 68, center.y + 4), phase_name,
		HORIZONTAL_ALIGNMENT_CENTER, extent.x - 136, 17, GOLD)
	var instructions := "AWAITING MATERIAL REQUIREMENTS"
	if not model.construction.requirements().is_empty():
		instructions = "MATERIAL DELIVERY ACTIVE"
	draw_string(font, Vector2(start.x + 72, center.y + 43), instructions,
		HORIZONTAL_ALIGNMENT_CENTER, extent.x - 144, 13, Color("#9BB9C4"))
	draw_string(font, Vector2(start.x + 72, center.y + 68), "48 PERIMETER INPUT PORTS",
		HORIZONTAL_ALIGNMENT_CENTER, extent.x - 144, 12, CYAN)

func _port(start: Vector2, side: int, index: int) -> void:
	var along := (float(index) + 0.5) * TILE
	var point := start
	var inward := Vector2.ZERO
	var slot := Rect2()
	match side:
		0: # North: receives from above, points south.
			point = start + Vector2(along, 9)
			inward = Vector2.DOWN
			slot = Rect2(point - Vector2(13, 6), Vector2(26, 12))
		1: # West: receives from left, points east.
			point = start + Vector2(9, along)
			inward = Vector2.RIGHT
			slot = Rect2(point - Vector2(6, 13), Vector2(12, 26))
		2: # South: receives from below, points north.
			point = start + Vector2(along, Construction.HEIGHT * TILE - 9)
			inward = Vector2.UP
			slot = Rect2(point - Vector2(13, 6), Vector2(26, 12))
		3: # East: receives from right, points west.
			point = start + Vector2(Construction.WIDTH * TILE - 9, along)
			inward = Vector2.LEFT
			slot = Rect2(point - Vector2(6, 13), Vector2(12, 26))
	draw_rect(slot, Color("#07131A"))
	draw_rect(slot, Color("#718A94"), false, 1.3)
	draw_circle(point, 3.3, CYAN.darkened(0.24))
	var tip := point + inward * 6.0
	var wing := Vector2(-inward.y, inward.x)
	draw_colored_polygon(PackedVector2Array([
		tip, point - inward * 2 + wing * 3.2,
		point - inward * 2 - wing * 3.2]), CYAN)
