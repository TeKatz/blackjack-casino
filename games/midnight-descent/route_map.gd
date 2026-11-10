extends Control
var nodes: Array = []
var current := 0
const POSITIONS = [Vector2(300,28),Vector2(110,94),Vector2(490,94),Vector2(110,166),Vector2(490,166),Vector2(110,238),Vector2(490,238),Vector2(300,310),Vector2(110,310),Vector2(300,382)]
func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_IGNORE
	custom_minimum_size=Vector2(610,420)
func _draw() -> void:
	for i in range(nodes.size()):
		for neighbor in nodes[i].links:
			if neighbor>i:
				draw_line(POSITIONS[i],POSITIONS[neighbor],Color("#9e82af") if i==current or neighbor==current else Color("#4a395d"),3,true)
