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
	draw_rect(Rect2(0,0,1100,334),Color("#101827"))
	for i in range(10):
		draw_rect(Rect2(i*110,0,110,334),Color("#182235") if i%2==0 else Color("#141c2c"))
		draw_line(Vector2(i*110,0),Vector2(i*110,334),Color("#26364b"),1)
	for i in range(4):
		draw_rect(Rect2(2+i,2+i,1096-i*2,330-i*2),Color(0.28,0.37,0.55,0.11),false,1)
	for i in range(nodes.size()):
		for neighbor in nodes[i].links:
			if neighbor>i:
				var active: bool=i==current or neighbor==current
				var a:=position_for(i)
				var b:=position_for(neighbor)
				draw_line(a,b,Color("#0b101b"),8,true)
				draw_line(a,b,Color("#e8c182") if active else Color("#526280"),2,true)
				if active:
					var point: Vector2=a.lerp(b,fposmod(clock*0.6,1.0))
					draw_circle(point,5,Color(0.95,0.75,0.4,0.24))
					draw_circle(point,2,Color("#f5d18d"))
