extends Control
var nodes: Array = []
var current := 0
var clock := 0.0
const POSITIONS = [Vector2(440,32),Vector2(200,101),Vector2(680,101),Vector2(200,170),Vector2(680,170),Vector2(200,239),Vector2(680,239),Vector2(440,308),Vector2(200,308),Vector2(440,377)]
func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_IGNORE
	custom_minimum_size=Vector2(884,410)
func _process(dt: float) -> void:
	clock+=dt
	queue_redraw()
func _draw() -> void:
	draw_rect(Rect2(0,0,884,410),Color("#101827"))
	for i in range(6):
		draw_rect(Rect2(2+i,2+i,880-i*2,406-i*2),Color(0.28,0.37,0.55,0.07),false,1)
	for y in range(0,410,20):
		draw_line(Vector2(0,y),Vector2(884,y),Color("#202a3e"),1)
	for x in range(0,884,20):
		draw_line(Vector2(x,0),Vector2(x,410),Color("#211a30"),1)
	for i in range(nodes.size()):
		for neighbor in nodes[i].links:
			if neighbor>i:
				var active: bool=i==current or neighbor==current
				draw_line(POSITIONS[i],POSITIONS[neighbor],Color("#21182d"),9,true)
				draw_line(POSITIONS[i],POSITIONS[neighbor],Color("#e8c182") if active else Color("#64516f"),3,true)
				if active:
					var point: Vector2=POSITIONS[i].lerp(POSITIONS[neighbor],fposmod(clock*0.6,1.0))
					draw_circle(point,5,Color(0.95,0.75,0.4,0.24))
					draw_circle(point,2,Color("#f5d18d"))
