extends Control
var nodes: Array = []
var current := 0
var clock := 0.0
func position_for(index: int) -> Vector2:
	var node: Dictionary=nodes[index]
	return Vector2(58+float(node.column)*110,65+float(node.lane)*102)
func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_IGNORE
	custom_minimum_size=Vector2(1100,334)
func _process(dt: float) -> void:
	clock+=dt
	queue_redraw()
func _draw() -> void:
	# A quiet wayfinding chart, deliberately free of the old pixel-panel grid.
	draw_rect(Rect2(0,0,1100,334),Color("#152629"))
	for i in range(10):
		var x: float=float(i)*110
		draw_rect(Rect2(x+2,8,106,318),Color("#23373755") if i%2==0 else Color("#1d303344"))
		draw_line(Vector2(x+55,20),Vector2(x+55,314),Color("#5b8079",0.17),1,true)
	draw_line(Vector2(0,1),Vector2(1100,1),Color("#82b9a5",0.55),2,true)
	draw_line(Vector2(0,333),Vector2(1100,333),Color("#82b9a5",0.36),1,true)
	for i in range(nodes.size()):
		for neighbor in nodes[i].links:
			if neighbor>i:
				var active: bool=i==current or neighbor==current
				var a:=position_for(i)
				var b:=position_for(neighbor)
				draw_line(a,b,Color("#0e1b1d",0.85),7,true)
				draw_line(a,b,Color("#e4c98e") if active else Color("#577f78"),2,true)
				if active:
					var point: Vector2=a.lerp(b,fposmod(clock*0.58,1.0))
					draw_circle(point,8,Color(0.78,0.95,0.76,0.12))
					draw_circle(point,3,Color("#e8d5aa"))
