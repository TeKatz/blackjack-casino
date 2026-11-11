extends Control
## Animated pixel-art gate chamber, independent of menu widgets.
const TILES = preload("res://art/pixel/vault_tiles.svg")
const TORCH = preload("res://art/pixel/torch.svg")
const PixelWorld = preload("res://pixel_world.gd")
var cinematic_active := false
var cinematic_clock := 0.0
var cinematic_character := "warden"
var clock := 0.0
var motion := true

func _ready() -> void:
	mouse_filter = Control.MOUSE_FILTER_IGNORE
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

func _process(dt: float) -> void:
	if motion or cinematic_active:
		clock += dt
		if cinematic_active: cinematic_clock+=dt
		queue_redraw()

func begin_cinematic(character: String) -> void:
	cinematic_active=true
	cinematic_clock=0.0
	cinematic_character=character if PixelWorld.ACTORS.has(character) else "warden"
	queue_redraw()

func tile(index: int, rect: Rect2) -> void:
	draw_texture_rect_region(TILES,rect,Rect2(index*32,0,32,32))

func _draw() -> void:
	var camera_progress: float=clampf((cinematic_clock-0.25)/1.10,0,1) if cinematic_active else 0.0
	var eased: float=camera_progress*camera_progress*(3-2*camera_progress)
	var zoom: float=1.0+0.22*eased
	var focus: Vector2=Vector2(925,350)
	var destination: Vector2=Vector2(810,350)
	var translation: Vector2=(destination-focus*zoom)*eased
	draw_set_transform(translation*size/Vector2(1280,720),0,size/Vector2(1280,720)*zoom)
	draw_rect(Rect2(0,0,1280,720),Color("#100d19"))
	for y in range(8):
		for x in range(15):
			var variant := 4 if y<3 else (x*7+y*5)%3
			tile(variant,Rect2(320+x*64,100+y*64,64,64))
	draw_rect(Rect2(320,100,960,512),Color(0.07,0.035,0.14,0.42))
	# Distant arches and layered masonry add architectural depth.
	for i in range(5):
		var x: float=380+i*190
		draw_rect(Rect2(x,102,42,360),Color("#20172c"))
		draw_rect(Rect2(x+5,102,8,360),Color("#5a435c"))
		draw_rect(Rect2(x-8,98,58,14),Color("#78596a"))
		draw_rect(Rect2(x-9,442,60,20),Color("#302337"))
		if i<4:
			draw_rect(Rect2(x+42,120,148,18),Color("#36283e"))
			draw_rect(Rect2(x+60,137,110,12),Color("#302338"))
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
	# Camera intro focuses the selected actual player sprite and the portal.
	var walk: float=clampf((cinematic_clock-1.02)/0.99,0,1) if cinematic_active else 0.0
	var y: float=430.0-184.0*(walk*walk*(3-2*walk))
	var actor_opacity: float=1.0-clampf((cinematic_clock-1.94)/0.30,0,1) if cinematic_active else 1.0
	draw_rect(Rect2(881,y+122,88,12),Color(0,0,0,0.55*actor_opacity))
	var frame: int=int(clock*2)%2
	if cinematic_active:
		if cinematic_clock<1.02: frame=6 if cinematic_clock>0.65 else int(clock*2)%2
		elif cinematic_clock<2.04: frame=2+int(clock*12)%4
		else: frame=0
	var tex: Texture2D=PixelWorld.ACTORS[cinematic_character]
	draw_texture_rect_region(tex,Rect2(861,y,128,128),Rect2(frame*32,0,32,32),Color(1,1,1,actor_opacity))
	if cinematic_active and cinematic_clock>0.72 and cinematic_clock<1.12:
		var ring_alpha: float=sin((cinematic_clock-0.72)/0.40*PI)
		draw_arc(Vector2(925,y+68),44,0,TAU,32,Color(0.89,0.73,1,ring_alpha*0.9),3,true)
		for i in range(6):
			var p: Vector2=Vector2(925,y+68)+Vector2.from_angle(i*TAU/6+clock)*58
			draw_rect(Rect2(p,Vector2(3,3)),Color("#ffdcaa"))
	for i in range(34):
		var pos := Vector2(540+fposmod(i*79+clock*(4+i%4),730),112+fposmod(i*71-clock*7,490))
		draw_rect(Rect2(Vector2(roundf(pos.x/2)*2,roundf(pos.y/2)*2),Vector2(2,2)),Color(0.9,0.73,0.5,0.14+0.12*sin(clock*2+i)))
	# Slow near-floor mist, floating motes and a foreground arch silhouette.
	for i in range(11):
		var x: float=510+fposmod(i*117+clock*(9+i%3),760)
		var y: float=510+sin(clock*0.55+i)*24+i%3*22
		draw_rect(Rect2(x,y,120,5),Color(0.59,0.45,0.78,0.045))
		draw_rect(Rect2(x+24,y+6,88,4),Color(0.59,0.45,0.78,0.035))
	for x in [645.0,1243.0]:
		draw_rect(Rect2(x,80,36,540),Color("#110d1a"))
		draw_rect(Rect2(x+4,80,5,540),Color("#39283e"))
		draw_rect(Rect2(x-8,80,52,12),Color("#68506b"))
		draw_rect(Rect2(x-8,600,52,18),Color("#37283e"))
	# Dark left and bottom edges keep the menu readable.
	for i in range(16):
		draw_rect(Rect2(i*37,0,38,720),Color(0.035,0.025,0.07,0.65*(1.0-float(i)/16)))
	draw_rect(Rect2(0,630,1280,90),Color("#100d19"))
	draw_line(Vector2(0,630),Vector2(1280,630),Color("#765a65"),2)
	draw_set_transform(Vector2.ZERO)
