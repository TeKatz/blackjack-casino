extends Node2D
## Pixel art is a rendering layer only: model coordinates and collision rules stay unchanged.
const Model = preload("res://run_model.gd")
const TILES = preload("res://art/pixel/vault_tiles.svg")
const GARDEN = preload("res://art/pixel/garden_tiles.svg")
const TORCH = preload("res://art/pixel/torch.svg")
const RELICS = preload("res://art/pixel/relics.png")
const ACTORS = {
	"warden": preload("res://art/pixel/warden.svg"),
	"dealer": preload("res://art/pixel/dealer.svg"),
	"arcanist": preload("res://art/pixel/arcanist.svg"),
	"archer": preload("res://art/pixel/archer.svg"),
	"shield": preload("res://art/pixel/shield.svg"),
	"spore": preload("res://art/pixel/spore.svg"),
	"vine": preload("res://art/pixel/vine.svg"),
	"mushroom": preload("res://art/pixel/mushroom.svg"),
	"rootking": preload("res://art/pixel/rootking.svg"),
	"stalker": preload("res://art/pixel/stalker.svg"),
	"caster": preload("res://art/pixel/caster.svg"),
	"brute": preload("res://art/pixel/brute.svg"),
	"boss": preload("res://art/pixel/boss.svg")
}
const TORCH_POSITIONS = [Vector2(142,143),Vector2(394,143),Vector2(650,143),Vector2(906,143),Vector2(1154,143)]
var model
var clock := 0.0
var previous_player := Vector2.ZERO
var walking := false
var player_facing := Vector2.DOWN
var torches: Array[PointLight2D] = []
var projectile_lights: Array[PointLight2D] = []
var player_light: PointLight2D
var gate_light: PointLight2D
var occluders: Array[LightOccluder2D] = []
var last_room := -1
var last_floor := -1
var last_seed: int = -1
var light_texture: GradientTexture2D
var ambient: CanvasModulate
var side_lights: Array[PointLight2D] = []
var boss_light: PointLight2D
var gate_open_amount := 0.0

func setup(value) -> void:
	model = value
	z_index = -1
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	previous_player = model.player
	ambient = CanvasModulate.new()
	ambient.color = Color(0.47,0.43,0.58)
	add_child(ambient)
	var gradient := Gradient.new()
	gradient.colors = PackedColorArray([Color(1,1,1,0.95),Color(1,1,1,0.48),Color(1,1,1,0)])
	gradient.offsets = PackedFloat32Array([0.0,0.48,1.0])
	light_texture = GradientTexture2D.new()
	light_texture.gradient = gradient
	light_texture.width = 128
	light_texture.height = 128
	light_texture.fill = GradientTexture2D.FILL_RADIAL
	light_texture.fill_from = Vector2(0.5,0.5)
	light_texture.fill_to = Vector2(1,0.5)
	for pos in TORCH_POSITIONS:
		var light := make_light(Color("#ffd094"),4.1,1.25,true)
		light.position = pos + Vector2(0,40)
		torches.append(light)
	player_light = make_light(Color("#d8caff"),2.8,0.58,false)
	gate_light = make_light(Color("#b18aff"),3.6,1.15,true)
	gate_light.position = Model.EXIT
	for pos in [Vector2(89,555),Vector2(1190,555)]:
		var light := make_light(Color("#e7ac73"),3.3,0.9,true)
		light.position = pos
		side_lights.append(light)
	boss_light = make_light(Color("#ec6d9c"),3.6,0.0,false)
	for i in range(4):
		projectile_lights.append(make_light(Color("#8adcf8"),1.3,1.0,false))
	rebuild_occluders()
	update_view(0.0)

func make_light(color: Color, scale_value: float, energy_value: float, shadows: bool) -> PointLight2D:
	var light := PointLight2D.new()
	light.texture = light_texture
	light.texture_scale = scale_value
	light.color = color
	light.energy = energy_value
	light.shadow_enabled = shadows
	light.shadow_color = Color(0.06,0.035,0.09,0.82)
	light.blend_mode = Light2D.BLEND_MODE_ADD
	# World canvas is isolated at layer -1; include that layer in light ranges.
	light.range_layer_min = -1
	light.range_layer_max = -1
	add_child(light)
	return light

func rebuild_occluders() -> void:
	for old in occluders:
		remove_child(old)
		old.queue_free()
	occluders.clear()
	for wall in model.walls:
		var polygon := OccluderPolygon2D.new()
		polygon.polygon = PackedVector2Array([Vector2.ZERO,Vector2(wall.size.x,0),wall.size,Vector2(0,wall.size.y)])
		var node := LightOccluder2D.new()
		node.occluder = polygon
		node.position = wall.position
		add_child(node)
		occluders.append(node)
	last_room = model.room
	last_floor = model.floor_index
	last_seed = model.seed_value

func update_view(dt: float) -> void:
	clock += dt
	if last_floor != model.floor_index or last_room != model.room or last_seed != model.seed_value or occluders.size() != model.walls.size():
		rebuild_occluders()
	var delta: Vector2 = model.player-previous_player
	walking = delta.length_squared() > 0.05
	if walking:
		player_facing = delta.normalized()
	if model.attack_timer > 0.17 or model.bolt_timer > 0.48:
		player_facing = model.aim
	previous_player = model.player
	for i in range(torches.size()):
		torches[i].energy = 1.25 + sin(clock*7.0+i*2.2)*0.19 + sin(clock*13+i)*0.08
	for i in range(side_lights.size()):
		side_lights[i].energy = 0.85 + sin(clock*5+i*2)*0.15
	var boss_present := false
	for enemy in model.enemies:
		if enemy.kind in ["boss","rootking"]:
			boss_present = true
			boss_light.position = enemy.pos
			boss_light.energy = 0.75 + sin(clock*3)*0.2 + (0.6 if enemy.wind > 0 else 0.0)
	boss_light.enabled = boss_present
	var target_ambient := Color(0.39,0.30,0.47) if boss_present else Color(0.55,0.49,0.64)
	if model.floor_index==1: target_ambient=Color(0.35,0.53,0.39)
	ambient.color = ambient.color.lerp(target_ambient, minf(1.0,dt*2.5))
	gate_open_amount = move_toward(gate_open_amount,1.0 if model.state == "exit" else 0.0,dt*2.5)
	gate_light.energy = gate_open_amount*(1.1+sin(clock*4)*0.2)
	player_light.position = model.player
	player_light.color = Color("#ffd2d9") if model.invulnerable > 0.3 else Color("#d8caff")
	gate_light.enabled = gate_open_amount > 0.01
	for i in range(projectile_lights.size()):
		var light := projectile_lights[i]
		light.enabled = i < model.shots.size()
		if light.enabled:
			var shot: Dictionary = model.shots[i]
			light.position = shot.pos
			light.color = Color("#ff728d") if shot.hostile else Color("#81dbff")
	queue_redraw()

func pixel_pos(value: Vector2) -> Vector2:
	return (value/2.0).round()*2.0

func tile(index: int, rect: Rect2, source_size: Vector2 = Vector2(32,32)) -> void:
	draw_texture_rect_region(GARDEN if model.floor_index==1 else TILES,rect,Rect2(Vector2(index*32,0),source_size))

func _draw() -> void:
	if model == null:
		return
	draw_rect(Rect2(0,0,1280,720),Color("#100c18"))
	draw_rect(Rect2(22,103,1236,545),Color("#231a2c"))
	var arena: Rect2 = Model.ARENA
	draw_rect(arena,Color("#211d2d"))
	for y in range(8):
		for x in range(19):
			var offset := Vector2(x*64,y*64)
			var size := Vector2(minf(64,arena.size.x-offset.x),minf(64,arena.size.y-offset.y))
			if size.x <= 0 or size.y <= 0:
				continue
			var variant: int = (x*13+y*7+int(model.room)*3)%3
			if (x*19+y*11+int(model.room))%17 == 0:
				variant = 3
			tile(variant,Rect2(arena.position+offset,size),size/2.0)
	for x in range(19):
		var width := minf(64,1222.0-(58+x*64))
		if width > 0:
			tile(4,Rect2(58+x*64,105,width,40),Vector2(width/2,32))
			tile(4,Rect2(58+x*64,624,width,20),Vector2(width/2,32))
	for y in range(7):
		tile(4,Rect2(23,146+y*68,35,68))
		tile(4,Rect2(1222,146+y*68,35,68))
	draw_rect(Rect2(23,105,1234,542),Color("#755b73"),false,2)
	for i in range(TORCH_POSITIONS.size()):
		var pos: Vector2 = TORCH_POSITIONS[i]
		var frame := int(clock*8+i)%4
		draw_texture_rect_region(TORCH,Rect2(pos-Vector2(16,37),Vector2(32,64)),Rect2(frame*16,0,16,32))
	for i in range(side_lights.size()):
		var pos := side_lights[i].position
		draw_texture_rect_region(TORCH,Rect2(pos-Vector2(16,37),Vector2(32,64)),Rect2((int(clock*8)+i)%4*16,0,16,32))
	# Sparse cracks, scattered stones and drifting dust break up the repeated grid.
	for i in range(22):
		var pos := Vector2(78+(i*173+int(model.room)*37)%1110,165+(i*97)%440)
		draw_rect(Rect2(pixel_pos(pos),Vector2(4,2)),Color("#756174"))
		if i%3 == 0:
			draw_rect(Rect2(pixel_pos(pos+Vector2(4,2)),Vector2(2,4)),Color("#31283e"))
	for i in range(28):
		var pos := Vector2(70+fposmod(i*149+clock*(6+i%4),1138),160+fposmod(i*71-clock*9,450))
		draw_rect(Rect2(pixel_pos(pos),Vector2(2,2)),Color(0.85,0.65,0.43,0.16+0.12*sin(clock*2+i)))
	if model.floor_index==1:
		for i in range(24):
			var pos := Vector2(66+(i*173)%1140,154+(i*97)%460)
			draw_rect(Rect2(pos,Vector2(10,4)),Color("#486b46"))
			draw_rect(Rect2(pos+Vector2(3,-5),Vector2(4,8)),Color("#75a65f"))
	# Gate is drawn as stone architecture, with an illuminated open portal.
	var gate: Vector2 = Model.EXIT
	tile(6,Rect2(gate-Vector2(22,54),Vector2(48,108)))
	if model.state == "exit":
		draw_rect(Rect2(gate-Vector2(12,42),Vector2(24,84)),Color("#43316f"))
		for i in range(7):
			var p := gate + Vector2(sin(clock*3+i*1.9)*9,-36+float(i)*11)
			draw_rect(Rect2(pixel_pos(p),Vector2(4,4)),Color("#dcc7ff"))
	# Painter's ordering: pillars and actor feet share the same depth list.
	var objects: Array[Dictionary] = []
	for wall in model.walls:
		objects.append({"kind":"pillar","depth":wall.end.y,"rect":wall})
	for enemy in model.enemies:
		objects.append({"kind":"enemy","depth":enemy.pos.y+float(enemy.r),"enemy":enemy})
	objects.append({"kind":"player","depth":model.player.y+15})
	objects.sort_custom(func(a: Dictionary,b: Dictionary): return a.depth < b.depth)
	for object in objects:
		match object.kind:
			"pillar": draw_pillar(object.rect)
			"enemy": draw_actor(object.enemy)
			"player": draw_warden()

func draw_pillar(rect: Rect2) -> void:
	draw_rect(Rect2(rect.position+Vector2(5,6),rect.size),Color("#191320"))
	tile(5,rect)
	draw_rect(Rect2(rect.position,Vector2(rect.size.x,4)),Color("#a38b92"))

func direction_row(direction: Vector2) -> int:
	if absf(direction.x) > absf(direction.y):
		return 2 if direction.x < 0 else 3
	return 1 if direction.y < 0 else 0

func draw_shadow(pos: Vector2, size: float) -> void:
	var center := pixel_pos(pos)
	draw_rect(Rect2(center+Vector2(-size*0.36,8),Vector2(size*0.72,8)),Color(0.04,0.025,0.07,0.62))
	draw_rect(Rect2(center+Vector2(-size*0.26,6),Vector2(size*0.52,12)),Color(0.04,0.025,0.07,0.35))

func sprite(kind: String, pos: Vector2, direction: Vector2, frame: int, size: float, tint: Color = Color.WHITE) -> void:
	var texture: Texture2D = ACTORS[kind]
	var dest := Rect2(pixel_pos(pos-Vector2(size/2,size*0.72)),Vector2(size,size))
	draw_texture_rect_region(texture,dest,Rect2(frame*32,direction_row(direction)*32,32,32),tint)

func draw_warden() -> void:
	var pos: Vector2 = model.player
	draw_shadow(pos,64)
	var frame := 2+int(clock*11)%4 if walking else int(clock*2)%2
	if model.attack_timer > 0.17 or model.bolt_timer > 0.48:
		frame = 6
	if model.invulnerable > 0.3:
		frame = 7
	if model.dash_left > 0:
		frame = 3
		for i in range(3,0,-1):
			sprite(model.character,pos-model.dash_direction*float(i)*14,player_facing,frame,64,Color(0.65,0.5,1,0.12))
	for echo in model.echoes:
		sprite(model.character,echo.pos,echo.dir,6,64,Color(0.65,0.45,1,0.45))
	sprite(model.character,pos,player_facing,frame,64)

func draw_actor(enemy: Dictionary) -> void:
	var kind: String = enemy.kind
	var pos: Vector2 = enemy.pos
	var size := 128.0 if kind in ["boss","rootking"] else (80.0 if kind=="mushroom" else 64.0)
	var direction: Vector2 = pos.direction_to(model.player)
	var frame := 2+int(clock*8+pos.x*0.02)%4
	if enemy.wind > 0:
		frame = 6
	if enemy.flash > 0:
		frame = 7
	draw_shadow(pos,size)
	var tint := Color("#ffedda") if enemy.flash > 0 else Color.WHITE
	if float(enemy.get("chill",0))>0: tint=Color("#a3dfff")
	if enemy.get("elite",false):
		draw_arc(pos+Vector2(0,10),size*0.42,0,TAU,24,Color("#f0bf70"),2,true)
	sprite(kind,pos,direction,frame,size,tint)

func is_ready_for_smoke() -> bool:
	return TILES.get_width() == 256 and RELICS.get_width() == 1920 and RELICS.get_height() == 48 and ACTORS["dealer"].get_height() == 128 and ACTORS["warden"].get_height() == 128 and torches.size() == 5 and side_lights.size() == 2 and ambient != null and boss_light != null and torches[0].range_layer_min == -1 and occluders.size() == model.walls.size()
