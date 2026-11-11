extends Control
## Actual rank nodes, connected in order; selection never purchases automatically.
signal rank_selected(id: String, rank: int)
const UI = preload("res://ui_theme.gd")
const Content = preload("res://content.gd")
const KEYS = ["vitality","alchemy","power","arcane","agility","fortune","resolve","recovery"]
const COLORS = [Color("#e68e9d"),Color("#83d8b2"),Color("#e7ba75"),Color("#bd9cfa"),Color("#90c9f2"),Color("#efd18a"),Color("#eaa78b"),Color("#8edfc9")]
const ICONS = [1,15,0,3,2,13,35,31]
var profile
var german := false
var selected := "vitality"
var selected_rank := 1
var clock := 0.0
var motion := true
var nodes: Array[Button] = []
func _ready() -> void:
	custom_minimum_size=Vector2(748,480)
	mouse_filter=Control.MOUSE_FILTER_IGNORE
	texture_filter=CanvasItem.TEXTURE_FILTER_NEAREST
func configure(value, de: bool, key: String, rank: int) -> void:
	profile=value
	german=de
	selected=key
	selected_rank=rank
	for child in get_children():
		remove_child(child)
		child.queue_free()
	nodes.clear()
	for row in range(KEYS.size()):
		var id: String=KEYS[row]
		var item: Array=Content.UPGRADES[id]
		var caption:=Label.new()
		caption.text=item[1 if german else 0]
		caption.position=Vector2(52,22+row*57)
		caption.size=Vector2(180,42)
		caption.vertical_alignment=VERTICAL_ALIGNMENT_CENTER
		caption.add_theme_font_size_override("font_size",16)
		caption.add_theme_color_override("font_color",COLORS[row])
		caption.mouse_filter=Control.MOUSE_FILTER_IGNORE
		add_child(caption)
		for j in range(int(item[5])):
			var level: int=j+1
			var button:=Button.new()
			button.position=Vector2(244+j*94,22+row*57)
			button.size=Vector2(76,43)
			button.text=str(level)+(" ✓" if level<=profile.ranks[id] else "")
			button.icon=UI.atlas(UI.RELICS,ICONS[row])
			button.expand_icon=true
			button.add_theme_constant_override("icon_max_width",24)
			button.add_theme_font_size_override("font_size",16)
			var owned: bool=level<=profile.ranks[id]
			var next: bool=level==profile.ranks[id]+1
			var active: bool=id==selected and level==selected_rank
			UI.style_button(button,COLORS[row] if owned or next else Color("#625772"),active or owned)
			if not owned and not next:
				button.add_theme_color_override("font_color",Color("#9d90aa"))
			var status: String="Erworben" if german else "Acquired"
			if not owned and next: status=str(profile.cost(id))+" "+("Boss-Seelen" if german else "boss souls")
			elif not owned: status="Vorgängerrang nötig" if german else "Previous rank required"
			button.tooltip_text=str(item[1 if german else 0])+"  "+str(level)+" • "+status
			button.pressed.connect(func(): rank_selected.emit(id,level))
			add_child(button)
			nodes.append(button)
	queue_redraw()
func _process(dt: float) -> void:
	if motion:
		clock+=dt
		queue_redraw()
func _draw() -> void:
	draw_rect(Rect2(0,0,748,480),Color("#12101d"))
	for x in range(0,748,24): draw_line(Vector2(x,0),Vector2(x,480),Color("#211b2c"),1)
	for y in range(0,480,24): draw_line(Vector2(0,y),Vector2(748,y),Color("#211b2c"),1)
	draw_rect(Rect2(5,5,738,470),Color("#594763"),false,2)
	if profile==null: return
	draw_line(Vector2(27,43),Vector2(27,43+57*(KEYS.size()-1)),Color("#8a6d9b"),3)
	draw_circle(Vector2(27,43+57.0*(KEYS.size()-1)*0.5),9,Color("#d2a96e"))
	draw_circle(Vector2(27,43+57.0*(KEYS.size()-1)*0.5),5,Color("#f6e1ab"))
	for row in range(KEYS.size()):
		var id: String=KEYS[row]
		var y: float=43+row*57
		var color: Color=COLORS[row]
		draw_line(Vector2(27,y),Vector2(46,y),color.darkened(0.3),2)
		draw_circle(Vector2(27,y),4,color)
		draw_line(Vector2(224,y),Vector2(282,y),color.darkened(0.35),3)
		draw_circle(Vector2(224,y),4,color)
		for j in range(int(Content.UPGRADES[id][5])-1):
			var a:=Vector2(282+j*94,y)
			var b:=a+Vector2(94,0)
			draw_line(a,b,Color("#45364f"),6)
			if j+2<=profile.ranks[id]: draw_line(a,b,color,3)
		if id==selected:
			var center:=Vector2(282+(selected_rank-1)*94,y)
			draw_rect(Rect2(center-Vector2(42,25),Vector2(84,50)),Color(color,0.46+0.20*sin(clock*4)),false,2)
