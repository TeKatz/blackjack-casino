extends Control
## Animated pixel-art gate chamber, independent of menu widgets.
const TILES = preload("res://art/pixel/vault_tiles.svg")
const TORCH = preload("res://art/pixel/torch.svg")
const WARDEN = preload("res://art/pixel/warden.svg")
var clock := 0.0
var motion := true

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

func _process(dt: float) -> void:
	if motion:
		clock += dt
		queue_redraw()

func tile(index: int, rect: Rect2) -> void:
	draw_texture_rect_region(TILES,rect,Rect2(index*32,0,32,32))

func _draw() -> void:
	draw_set_transform(Vector2.ZERO,0,size/Vector2(1280,720))
	draw_rect(Rect2(0,0,1280,720),Color("#100d19"))
	for y in range(8):
		for x in range(15):
			var variant := 4 if y<3 else (x*7+y*5)%3
			tile(variant,Rect2(320+x*64,100+y*64,64,64))
	draw_rect(Rect2(320,100,960,512),Color(0.07,0.035,0.14,0.42))
	# Grand portal with stepped masonry, a raised dais and rune accents.
	for i in range(4):
		draw_rect(Rect2(745-i*13,440+i*22,350+i*26,23),Color("#292039"))
		draw_rect(Rect2(745-i*13,440+i*22,350+i*26,4),Color("#79617e"))
	for i in range(4):
		tile(5,Rect2(731,188+i*64,64,64))
		tile(5,Rect2(1055,188+i*64,64,64))
	draw_rect(Rect2(788,174,274,280),Color("#0c0917"))
	draw_rect(Rect2(805,156,240,20),Color("#5f4b71"))
	draw_rect(Rect2(831,140,188,18),Color("#8b708b"))
	draw_rect(Rect2(849,126,152,16),Color("#a58b85"))
	draw_rect(Rect2(849,176,152,275),Color("#26183e"))
	for i in range(6,0,-1):
		draw_circle(Vector2(925,300),float(i)*23,Color(0.44,0.22,0.82,0.036))
	for i in range(26):
		var y := 190+fposmod(i*19-clock*22,248)
		var x := 924+sin(i*2.7+clock*0.8)*61
		draw_rect(Rect2(Vector2(roundf(x/3)*3,roundf(y/3)*3),Vector2(3,6)),Color(0.8,0.62,1,0.28+0.16*sin(clock*2+i)))
	var rune := Vector2(925,285)
	var pulse := 0.65+sin(clock*2.5)*0.15
	var diamond := PackedVector2Array([rune+Vector2(0,-43),rune+Vector2(30,0),rune+Vector2(0,43),rune+Vector2(-30,0),rune+Vector2(0,-43)])
	draw_polyline(diamond,Color(1,0.8,0.49,pulse),3)
	draw_rect(Rect2(rune-Vector2(7,7),Vector2(14,14)),Color("#f9dca2"))
	for x in [666.0,1172.0]:
		var pos := Vector2(x,286)
		for i in range(7,0,-1):
			draw_circle(pos,float(i)*16+sin(clock*5+x)*2,Color(0.92,0.44,0.17,0.032))
		draw_texture_rect_region(TORCH,Rect2(pos-Vector2(16,37),Vector2(32,64)),Rect2(int(clock*8)%4*16,0,16,32))
	# The same actual player atlas as gameplay, enlarged at integer scale.
	draw_rect(Rect2(881,552,88,12),Color(0,0,0,0.55))
	draw_texture_rect_region(WARDEN,Rect2(861,430,128,128),Rect2(int(clock*2)%2*32,0,32,32))
	for i in range(34):
		var pos := Vector2(540+fposmod(i*79+clock*(4+i%4),730),112+fposmod(i*71-clock*7,490))
		draw_rect(Rect2(Vector2(roundf(pos.x/2)*2,roundf(pos.y/2)*2),Vector2(2,2)),Color(0.9,0.73,0.5,0.14+0.12*sin(clock*2+i)))
	# Dark left and bottom edges keep the menu readable.
	for i in range(16):
		draw_rect(Rect2(i*37,0,38,720),Color(0.035,0.025,0.07,0.65*(1.0-float(i)/16)))
	draw_rect(Rect2(0,630,1280,90),Color("#100d19"))
	draw_line(Vector2(0,630),Vector2(1280,630),Color("#765a65"),2)
	draw_set_transform(Vector2.ZERO)
