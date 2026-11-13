extends Control
## Soft atmosphere behind modal content; no input interception.
var accent := Color("#80c8bd")
var mode := ""
var clock := 0.0
func _ready() -> void:
	mouse_filter=Control.MOUSE_FILTER_IGNORE
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
func _process(dt: float) -> void:
	clock+=dt
	queue_redraw()
func _draw() -> void:
	var w: float=size.x
	var h: float=size.y
	if w<1 or h<1: return
	for i in range(16):
		var t:=float(i)/16.0
		draw_rect(Rect2(0,t*h,w,h/16+1),Color(0.025+t*0.019,0.066+t*0.014,0.070+t*0.010,0.58))
	for i in range(10,0,-1):
		draw_circle(Vector2(w*0.5,h*0.40),float(i)*h*0.052,Color(accent,0.009))
	for i in range(14):
		var x:=fposmod(i*181.0+clock*(4.0+i%3),w)
		var y:=fposmod(i*97.0-clock*(2.0+i%4),h)
		draw_circle(Vector2(x,y),1.2,Color(accent,0.07+0.04*sin(clock+i)))
