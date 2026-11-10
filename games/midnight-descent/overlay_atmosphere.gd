extends Control
## Soft atmosphere behind modal content; no input interception.
var accent := Color("#b790de")
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
	# Layered dark gradient and a restrained, colored glow around the focal area.
	for i in range(12):
		var t:=float(i)/12
		draw_rect(Rect2(0,t*h,w,h/12+1),Color(0.025+t*0.02,0.018+t*0.01,0.05+t*0.035,0.61))
	for i in range(10,0,-1):
		var radius:=float(i)*h*0.055
		draw_circle(Vector2(w*0.5,h*0.39),radius,Color(accent,0.012))
	for i in range(20):
		var x:=fposmod(i*149.0+clock*(7+i%4),w)
		var y:=fposmod(i*83.0-clock*(4+i%3),h)
		var alpha:=0.08+0.07*(0.5+0.5*sin(clock*1.5+i))
		draw_rect(Rect2(roundf(x),roundf(y),2,2),Color(accent,alpha))
