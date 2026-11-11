extends Node2D
## Pixel art is a rendering layer only: model coordinates and collision rules stay unchanged.
const Model = preload("res://run_model.gd")
const TILES = preload("res://art/pixel/vault_tiles.svg")
const GARDEN = preload("res://art/pixel/garden_tiles.svg")
const CROWN = preload("res://art/pixel/crown_tiles.svg")
const BIOME_WALLS = preload("res://art/pixel/biome_walls.svg")
const PROPS = preload("res://art/pixel/room_props.svg")
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
	"soulbinder": preload("res://art/pixel/soulbinder.svg"),
	"revenant": preload("res://art/pixel/revenant.svg"),
	"hexer": preload("res://art/pixel/hexer.svg"),
	"sentinel": preload("res://art/pixel/sentinel.svg"),
	"hollowking": preload("res://art/pixel/hollowking.svg"),
	"stalker": preload("res://art/pixel/stalker.svg"),
	"caster": preload("res://art/pixel/caster.svg"),
	"brute": preload("res://art/pixel/brute.svg"),
	"boss": preload("res://art/pixel/boss.svg")
}
const TORCH_POSITIONS = [Vector2(142,143),Vector2(394,143),Vector2(650,143),Vector2(906,143),Vector2(1154,143)]
# Fixtures are suspended from the actual 143px timber rail, not free-floating.
# The bottle shelving, forge and portal are deliberately kept clear.
const HUB_LANTERN_POSITIONS = [Vector2(164,198),Vector2(900,198),Vector2(1168,198)]
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
var decor: Array[Dictionary]=[]
var decor_exclusions: Array[Rect2]=[]
var decor_lights: Array[PointLight2D]=[]
var floor_decals: Array[Vector2]=[]

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
	for i in range(4): decor_lights.append(make_light(Color("#f3c18b"),1.65,0.65,false))
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
	build_decor()

func update_view(dt: float) -> void:
	clock += dt
	if last_floor != model.floor_index or last_room != model.room or last_seed != model.seed_value or occluders.size() != model.walls.size():
		rebuild_occluders()
	var delta: Vector2 = model.player-previous_player
	walking = delta.length_squared() > 0.05
	if walking:
		player_facing = delta.normalized()
	if model.attack_timer > maxf(0.0,model.attack_cd-0.18) or model.bolt_timer > maxf(0.0,model.special_cd-0.25):
		player_facing = model.aim
	previous_player = model.player
	for i in range(torches.size()):
		torches[i].enabled=model.state!="hub" or i<HUB_LANTERN_POSITIONS.size()
		if model.state=="hub" and torches[i].enabled:
			torches[i].position=HUB_LANTERN_POSITIONS[i]+Vector2(0,4)
		else:
			torches[i].position=TORCH_POSITIONS[i]+Vector2(0,40)
		torches[i].energy=1.1+sin(clock*5.0+i*2.2)*0.15 if model.state=="hub" else 1.25+sin(clock*7.0+i*2.2)*0.19+sin(clock*13+i)*0.08
	for i in range(side_lights.size()):
		side_lights[i].energy = 0.85 + sin(clock*5+i*2)*0.15
	var boss_present := false
	for enemy in model.enemies:
		if enemy.kind in ["boss","rootking","hollowking"]:
			boss_present = true
			boss_light.position = enemy.pos
			boss_light.energy = 0.75 + sin(clock*3)*0.2 + (0.6 if enemy.wind > 0 else 0.0)
	boss_light.enabled = boss_present
	var target_ambient := Color(0.39,0.30,0.47) if boss_present else Color(0.55,0.49,0.64)
	if model.state=="hub": target_ambient=Color("#9b8490")
	if model.floor_index==1: target_ambient=Color(0.35,0.53,0.39)
	if model.floor_index==2: target_ambient=Color(0.47,0.34,0.50)
	for light in torches:
		light.color=[Color("#ffd094"),Color("#83e5be"),Color("#e9a0c4")][model.floor_index]
		light.energy*= [0.72,0.66,0.70][model.floor_index]
	for i in range(decor_lights.size()):
		decor_lights[i].energy=0.24+0.06*sin(clock*3+i*1.7)
	ambient.color = ambient.color.lerp(target_ambient, minf(1.0,dt*2.5))
	gate_open_amount = move_toward(gate_open_amount,1.0 if model.state in ["exit","hub"] else 0.0,dt*2.5)
	gate_light.position = Model.HUB_PORTAL if model.state=="hub" else Model.EXIT
	gate_light.color=Color("#d5a2ff") if model.state=="hub" else Color("#b18aff")
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
	if index==4 and model.floor_index>0:
		draw_texture_rect_region(BIOME_WALLS,rect,Rect2((model.floor_index-1)*32,0,32,32))
		return
	draw_texture_rect_region([TILES,GARDEN,CROWN][model.floor_index],rect,Rect2(Vector2(index*32,0),source_size))

func _draw() -> void:
	if model == null:
		return
	if model.state=="hub":
		draw_hub()
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
	for i in range(10):
		var pos := Vector2(78+(i*173+int(model.room)*37)%1110,165+(i*97)%440)
		draw_rect(Rect2(pixel_pos(pos),Vector2(4,2)),Color("#756174"))
		if i%3 == 0:
			draw_rect(Rect2(pixel_pos(pos+Vector2(4,2)),Vector2(2,4)),Color("#31283e"))
	for i in range(16):
		var pos := Vector2(70+fposmod(i*149+clock*(6+i%4),1138),160+fposmod(i*71-clock*9,450))
		draw_rect(Rect2(pixel_pos(pos),Vector2(2,2)),Color(0.85,0.65,0.43,0.16+0.12*sin(clock*2+i)))
	if model.floor_index==1:
		for i in range(10):
			var pos := Vector2(66+(i*173)%1140,154+(i*97)%460)
			draw_rect(Rect2(pos,Vector2(10,4)),Color("#486b46"))
			draw_rect(Rect2(pos+Vector2(3,-5),Vector2(4,8)),Color("#75a65f"))
	draw_architecture(arena)
	draw_room_decals()
	draw_biome_details(arena)
	draw_service_scene()
	draw_royal_seals()
	draw_gate()
	# Painter's ordering: pillars and actor feet share the same depth list.
	var objects: Array[Dictionary] = []
	for wall in model.walls:
		draw_rect(Rect2(wall.position+Vector2(9,12),wall.size),Color(0.025,0.015,0.045,0.48))
		objects.append({"kind":"pillar","depth":wall.end.y,"rect":wall})
	for prop in decor:
		objects.append({"kind":"prop","depth":prop.pos.y+8,"prop":prop})
	for enemy in model.enemies:
		objects.append({"kind":"enemy","depth":enemy.pos.y+float(enemy.r),"enemy":enemy})
	objects.append({"kind":"player","depth":model.player.y+15})
	objects.sort_custom(func(a: Dictionary,b: Dictionary): return a.depth < b.depth)
	for object in objects:
		match object.kind:
			"pillar": draw_pillar(object.rect)
			"prop": draw_prop(object.prop)
			"enemy": draw_actor(object.enemy)
			"player": draw_warden()

func draw_wall_lantern(pos: Vector2, index: int) -> void:
	# The sconce is riveted to a timber backplate, never suspended in mid-air.
	var flame_shift: float=roundf(sin(clock*6.5+index*1.7)*2.0)
	# Iron mount is visibly connected to the horizontal timber wall rail.
	draw_rect(Rect2(pos.x-7,143,14,pos.y-143-19),Color("#1b1720"))
	draw_rect(Rect2(pos.x-4,144,8,pos.y-144-19),Color("#77615d"))
	draw_rect(Rect2(pos.x-17,143,34,9),Color("#a67e66"))
	draw_rect(Rect2(pos.x-11,146,4,4),Color("#e1b78b"))
	draw_rect(Rect2(pos.x+7,146,4,4),Color("#e1b78b"))
	draw_rect(Rect2(pos.x-13,pos.y-22,26,8),Color("#2a232c"))
	for r in range(4,0,-1):
		draw_circle(pos+Vector2(0,5),float(r)*16,Color(1,0.54,0.22,0.017))
	draw_rect(Rect2(pos+Vector2(-20,-29),Vector2(40,9)),Color("#241b24"))
	draw_rect(Rect2(pos+Vector2(-17,-27),Vector2(34,5)),Color("#96705f"))
	for offset in [-13.0,13.0]:
		draw_rect(Rect2(pos+Vector2(offset,-26),Vector2(3,3)),Color("#e2b77f"))
	draw_rect(Rect2(pos+Vector2(-3,-23),Vector2(6,8)),Color("#2a222a"))
	draw_rect(Rect2(pos+Vector2(-13,-16),Vector2(26,5)),Color("#d8aa72"))
	draw_rect(Rect2(pos+Vector2(-15,-12),Vector2(30,32)),Color("#211a25"))
	draw_rect(Rect2(pos+Vector2(-10,-9),Vector2(20,24)),Color("#8b533d"))
	draw_rect(Rect2(pos+Vector2(-7,-6),Vector2(14,18)),Color("#f6a956"))
	draw_rect(Rect2(pos+Vector2(-4,-3),Vector2(8,13)),Color("#ffd58a"))
	draw_rect(Rect2(pos+Vector2(-2,flame_shift-7),Vector2(4,13)),Color("#fff0bb"))
	draw_rect(Rect2(pos+Vector2(-13,-12),Vector2(4,29)),Color("#c49566"))
	draw_rect(Rect2(pos+Vector2(9,-12),Vector2(4,29)),Color("#c49566"))
	draw_rect(Rect2(pos+Vector2(-15,17),Vector2(30,6)),Color("#e4b47c"))
	draw_rect(Rect2(pos+Vector2(-11,23),Vector2(22,4)),Color("#493340"))
	draw_rect(Rect2(pos+Vector2(-2,27),Vector2(4,5)),Color("#e3b67c"))

func draw_bar_stool(x: float) -> void:
	# Foot rails and a padded seat make every stool readable from the front.
	draw_rect(Rect2(x-13,300,26,25),Color("#201721"))
	draw_rect(Rect2(x-10,300,5,26),Color("#67494b"))
	draw_rect(Rect2(x+5,300,5,26),Color("#67494b"))
	draw_rect(Rect2(x-14,315,28,4),Color("#a47a68"))
	draw_rect(Rect2(x-20,285,40,13),Color("#251c26"))
	draw_rect(Rect2(x-18,283,36,11),Color("#be886d"))
	draw_rect(Rect2(x-14,286,28,5),Color("#704651"))
	draw_rect(Rect2(x-11,282,22,3),Color("#dfae86"))

func draw_seated_player() -> void:
	var stool: int=model.hub_seat_index
	if stool<0: return
	var x: float=float(Model.HUB_STOOL_X[stool])
	var progress: float=clampf(model.hub_seat_anim,0.0,1.0)
	var eased: float=progress*progress*(3.0-2.0*progress)
	# Fast upward hop with feet curling onto the stool and a tiny idle sway.
	var hop: float=sin(progress*PI)*6.0
	var p:=Vector2(x,339.0-17.0*eased-hop+sin(clock*1.7)*eased)
	draw_shadow(Vector2(x,334),45)
	sprite(model.character,p,Vector2.DOWN,2 if progress<0.7 else int(clock*1.4)%2,61)
	draw_rect(Rect2(x-15,330,13,5),Color("#251b2a"))
	draw_rect(Rect2(x+2,330,13,5),Color("#251b2a"))
	# Cushion and the foot rail remain visible in front of the folded legs.
	draw_rect(Rect2(x-17,336,34,4),Color("#976e69"))

func draw_card_guest(pos: Vector2, side: int) -> void:
	var palette: Color=Color("#547e79") if side==0 else Color("#876079")
	var trim: Color=Color("#b5d4b2") if side==0 else Color("#dcb7cc")
	var idle: float=roundf(sin(clock*2.1+side*1.8))
	var p:=pos+Vector2(0,idle)
	# Chair, boots, coat, face, hair and independent blinking.
	draw_rect(Rect2(pos+Vector2(-25,-31),Vector2(50,72)),Color("#211a24"))
	draw_rect(Rect2(pos+Vector2(-22,-30),Vector2(44,64)),Color("#79544f"))
	draw_rect(Rect2(pos+Vector2(-18,22),Vector2(36,7)),Color("#bb8971"))
	draw_rect(Rect2(p+Vector2(-21,-30),Vector2(42,48)),palette.darkened(0.3))
	draw_rect(Rect2(p+Vector2(-17,-28),Vector2(34,43)),palette)
	draw_rect(Rect2(p+Vector2(-4,-28),Vector2(8,43)),trim)
	draw_rect(Rect2(p+Vector2(-16,-62),Vector2(32,38)),Color("#201a23"))
	draw_rect(Rect2(p+Vector2(-13,-58),Vector2(26,31)),Color("#d9a686") if side==0 else Color("#c58d79"))
	draw_rect(Rect2(p+Vector2(-16,-64),Vector2(32,10)),Color("#4b3837") if side==0 else Color("#30253c"))
	var blink: bool=fposmod(clock+side*1.6,4.2)<0.14
	for eye in [-8.0,5.0]:
		draw_rect(Rect2(p+Vector2(eye,-44),Vector2(4,1 if blink else 3)),Color("#211b28"))
	draw_rect(Rect2(p+Vector2(-5,-33),Vector2(10,2)),Color("#8e5a59"))
	# Small readable fidget, as if each is counting the opponent's cards.
	var shoulder: float=roundf(sin(clock*2.8+side*2)*3)
	draw_rect(Rect2(p+Vector2(-23 if side==0 else 15,-12+shoulder),Vector2(8,25)),palette.darkened(0.1))
	draw_rect(Rect2(p+Vector2(-23 if side==0 else 15,8+shoulder),Vector2(8,7)),Color("#d49f83"))

func draw_playing_card(center: Vector2, face: bool, red: bool) -> void:
	var p:=pixel_pos(center)+Vector2(-7,-10)
	draw_rect(Rect2(p+Vector2(2,2),Vector2(14,20)),Color("#241b29"))
	draw_rect(Rect2(p,Vector2(14,20)),Color("#f0dfc6"))
	draw_rect(Rect2(p+Vector2(1,1),Vector2(12,18)),Color("#fff4df"))
	if face:
		var ink: Color=Color("#9d3b54") if red else Color("#31304b")
		draw_rect(Rect2(p+Vector2(3,3),Vector2(4,4)),ink)
		draw_rect(Rect2(p+Vector2(7,8),Vector2(4,4)),ink)
		draw_rect(Rect2(p+Vector2(3,14),Vector2(4,3)),ink)
	else:
		draw_rect(Rect2(p+Vector2(3,3),Vector2(8,14)),Color("#543c73"))
		for stripe in range(3):
			draw_rect(Rect2(p+Vector2(4,5+stripe*4),Vector2(6,2)),Color("#c99db7"))

func draw_card_duel() -> void:
	# Six-second loop: examine cards, reach, play, reveal, react, repeat.
	var phase: float=fposmod(clock,6.4)
	var left_turn: bool=phase<3.2
	var turn: float=fposmod(phase,3.2)
	var reach: float=smoothstep(0.22,1.05,turn)*(1.0-smoothstep(1.48,2.05,turn))
	var left_hand: Vector2=Vector2(449,506)+Vector2(18*reach if left_turn else 2*sin(clock*2),-9*reach)
	var right_hand: Vector2=Vector2(600,506)+Vector2(-18*reach if not left_turn else 2*sin(clock*2+2),-9*reach)
	# Hands and wrist motion are drawn above the table, not behind its front.
	draw_line(Vector2(430,495),left_hand,Color("#4f726d"),10)
	draw_line(Vector2(619,495),right_hand,Color("#70556c"),10)
	draw_rect(Rect2(pixel_pos(left_hand-Vector2(6,4)),Vector2(12,8)),Color("#d6a582"))
	draw_rect(Rect2(pixel_pos(right_hand-Vector2(6,4)),Vector2(12,8)),Color("#c9947e"))
	for i in range(3):
		draw_playing_card(Vector2(491+i*12,495+(i%2)*3),i<2 or phase>2.0, i%2==0)
	for i in range(2):
		draw_playing_card(Vector2(546+i*12,495+(i%2)*3),phase>3.65 or i==0,i==1)
	# The active hand slides one new card onto the felt before it flips face-up.
	var deal: float=smoothstep(0.48,1.32,turn)
	var from_x: float=468.0 if left_turn else 582.0
	var to_x: float=522.0 if left_turn else 531.0
	var mid: Vector2=Vector2(lerpf(from_x,to_x,deal),483.0-9*sin(deal*PI))
	draw_playing_card(mid,turn>1.4,not left_turn)
	# Chips shift in sync with each round; alternating glow marks a challenge.
	for i in range(3):
		var chip_x: float=513+i*12
		var chip_y: float=512+sin(clock*2+i*2)*2
		draw_rect(Rect2(chip_x,chip_y,9,5),Color("#c69b55"))
		draw_rect(Rect2(chip_x+2,chip_y+1,5,2),Color("#fae6ae"))
	if turn>2.25:
		var p: Vector2=Vector2(422,446) if left_turn else Vector2(629,446)
		for i in range(2):
			draw_rect(Rect2(pixel_pos(p+Vector2(-9+i*16,-12-sin(clock*7+i)*3)),Vector2(5,5)),Color(1.0,0.86,0.53,0.58))

func draw_barkeeper() -> void:
	# Hand-drawn pixel silhouette. Idle breathing, blinking and glass polishing.
	var p:=Vector2(483,215)
	var breath: float=roundf(sin(clock*2.2)*1.5)
	draw_rect(Rect2(p+Vector2(-25,-19+breath),Vector2(50,35)),Color("#251b29"))
	draw_rect(Rect2(p+Vector2(-21,-17+breath),Vector2(42,30)),Color("#5c4650"))
	draw_rect(Rect2(p+Vector2(-9,-16+breath),Vector2(18,28)),Color("#e4d4bb"))
	draw_rect(Rect2(p+Vector2(-4,-16+breath),Vector2(8,27)),Color("#825e51"))
	for x in [-20.0,15.0]:
		draw_rect(Rect2(p+Vector2(x,-13+breath),Vector2(7,25)),Color("#c7a88a"))
	# Warm face, swept-back hair and a short beard under the tavern light.
	draw_rect(Rect2(p+Vector2(-16,-51+breath),Vector2(32,37)),Color("#29202b"))
	draw_rect(Rect2(p+Vector2(-13,-46+breath),Vector2(26,29)),Color("#d99e82"))
	draw_rect(Rect2(p+Vector2(-15,-53+breath),Vector2(30,9)),Color("#4d3233"))
	draw_rect(Rect2(p+Vector2(-11,-24+breath),Vector2(22,10)),Color("#65423d"))
	var blinking: bool=fposmod(clock+0.25,4.5)<0.13
	for eye in [-7.0,6.0]:
		draw_rect(Rect2(p+Vector2(eye,-35+breath),Vector2(4,1 if blinking else 3)),Color("#2c1d28"))
	draw_rect(Rect2(p+Vector2(-4,-22+breath),Vector2(8,2)),Color("#c58b77"))
	# A glass rises and lowers in the polishing hand.
	var polish: float=roundf(sin(clock*3.0)*4.0)
	draw_rect(Rect2(p+Vector2(22,-15+polish),Vector2(10,8)),Color("#d5a484"))
	draw_rect(Rect2(p+Vector2(27,-32+polish),Vector2(11,20)),Color("#aaa4bf"))
	draw_rect(Rect2(p+Vector2(29,-30+polish),Vector2(7,14)),Color("#ecdddc"))
	draw_rect(Rect2(p+Vector2(25,-18+polish),Vector2(15,3)),Color("#ffecb7"))

func draw_hub() -> void:
	# The Threshold Tavern: standalone foyer before the first dungeon node.
	# All furniture aligns to the hub's actual collision volumes.
	draw_rect(Rect2(0,0,1280,720),Color("#110d1b"))
	draw_rect(Rect2(24,107,1232,537),Color("#2a202a"))
	var floor_rect:=Rect2(58,146,1164,478)
	draw_rect(floor_rect,Color("#49313a"))
	for y in range(10):
		for x in range(19):
			var tx: float=58+x*64
			var ty: float=146+y*48
			var wide: float=minf(64,1222-tx)
			var tall: float=minf(48,624-ty)
			if wide<=0 or tall<=0: continue
			var plank_color:=Color("#4d343c") if (x+y)%3==0 else (Color("#3b2935") if y%3==0 else Color("#49323a"))
			draw_rect(Rect2(tx,ty,wide,tall),plank_color)
			draw_rect(Rect2(tx,ty+3,wide,2),Color("#74505a"))
			draw_rect(Rect2(tx+6,ty+tall-4,wide-12,2),Color("#2b222e"))
			if (x+y)%4==0:
				draw_rect(Rect2(tx+17,ty+20,4,2),Color("#90716a"))
	# Tavern brick facade and horizontal beams. 32 px tiles retain the native pixel language.
	for x in range(19):
		var tx: float=58+x*64
		draw_texture_rect_region(TILES,Rect2(tx,101,64,48),Rect2(4*32,0,32,32))
	draw_rect(Rect2(54,133,1170,12),Color("#8e6c63"))
	draw_rect(Rect2(54,138,1170,4),Color("#c29a77"))
	for x in [86.0,508.0,858.0,1200.0]:
		draw_rect(Rect2(x,140,22,480),Color("#241c29"))
		draw_rect(Rect2(x+4,145,6,470),Color("#88665f"))
		draw_rect(Rect2(x-9,132,40,15),Color("#b28a72"))
	draw_rect(Rect2(58,606,1164,18),Color("#2b212c"))
	draw_rect(Rect2(58,607,1164,4),Color("#916d66"))
	# Dark wine-red runner leads directly from the tavern into the portal.
	draw_rect(Rect2(844,342,312,90),Color("#251729"))
	draw_rect(Rect2(844,344,312,7),Color("#966575"))
	draw_rect(Rect2(844,423,312,6),Color("#966575"))
	for i in range(7):
		draw_rect(Rect2(867+i*42,367,19,38),Color("#5d374b"),false,2)
	# Bar sign and shelf of bottles, behind the physical wooden counter.
	draw_rect(Rect2(263,152,390,41),Color("#1c1522"))
	draw_rect(Rect2(272,158,374,29),Color("#715061"),false,2)
	draw_string(ThemeDB.fallback_font,Vector2(310,179),"THE THRESHOLD",HORIZONTAL_ALIGNMENT_LEFT,-1,20,Color("#f1d49f"))
	for j in range(9):
		var x: float=242+j*52
		var bottle_color: Color=Color("#73aa96") if j%3==0 else (Color("#b88b6c") if j%3==1 else Color("#a18bba"))
		draw_rect(Rect2(x,185,17,21),Color("#171320"))
		draw_rect(Rect2(x+2,182,13,4),Color("#b18d75"))
		draw_rect(Rect2(x+3,189,11,15),bottle_color)
		draw_rect(Rect2(x+5,190,3,9),bottle_color.lightened(0.35))
	# Barkeeper stands behind the counter; players reach him from its front.
	draw_barkeeper()
	# Back-wall bar and five stools.
	draw_rect(Rect2(230,202,500,85),Color("#2a1d2a"))
	draw_rect(Rect2(220,208,520,71),Color("#63414a"))
	draw_rect(Rect2(220,208,520,14),Color("#b78969"))
	draw_rect(Rect2(220,224,520,5),Color("#d0a17c"))
	for x in Model.HUB_STOOL_X:
		draw_bar_stool(float(x))
	# Two named occupants stay on opposite sides of an ornate gaming table.
	draw_card_guest(Vector2(418,524),0)
	draw_card_guest(Vector2(630,524),1)
	# Carved table feet, dark leather apron, gold fittings and deep green felt.
	draw_rect(Rect2(467,498,140,78),Color("#1e1320"))
	draw_rect(Rect2(462,510,126,53),Color("#482e34"))
	for x in [465.0,573.0]:
		draw_rect(Rect2(x,521,12,47),Color("#281d28"))
		draw_rect(Rect2(x+3,519,6,43),Color("#90644e"))
		draw_rect(Rect2(x+1,562,10,6),Color("#b88b67"))
	draw_rect(Rect2(458,481,132,74),Color("#291e28"))
	draw_rect(Rect2(459,483,130,66),Color("#a57654"))
	draw_rect(Rect2(465,488,118,55),Color("#54363b"))
	draw_rect(Rect2(469,491,110,45),Color("#235146"))
	draw_rect(Rect2(474,495,100,37),Color("#2d6a56"))
	draw_rect(Rect2(474,495,100,37),Color("#bc955e"),false,2)
	draw_rect(Rect2(458,541,132,18),Color("#623d3c"))
	draw_rect(Rect2(463,544,122,4),Color("#bb8965"))
	for i in range(6):
		var x: float=465+i*22
		draw_rect(Rect2(x,551,8,4),Color("#d7ad73"))
	draw_rect(Rect2(519,546,12,9),Color("#37202c"))
	draw_rect(Rect2(522,548,6,5),Color("#f0c787"))
	draw_card_duel()
	draw_rect(Rect2(740,223,108,93),Color("#18131e"))
	draw_rect(Rect2(735,218,108,92),Color("#6c4852"))
	draw_rect(Rect2(746,225,86,15),Color("#be8e79"))
	draw_rect(Rect2(750,251,30,50),Color("#3b2c39"))
	draw_rect(Rect2(790,251,30,50),Color("#3b2c39"))
	# Wall lanterns now hang from the timber rail in unobstructed wall bays.
	for i in range(HUB_LANTERN_POSITIONS.size()):
		draw_wall_lantern(HUB_LANTERN_POSITIONS[i],i)
	# The hub gate is a distinct wall portal, not the dungeon route-map exit.
	var portal: Vector2=Model.HUB_PORTAL
	var p: Vector2=portal-Vector2(62,91)
	draw_rect(Rect2(p+Vector2(6,8),Vector2(124,182)),Color("#130e1e"))
	draw_rect(Rect2(p,Vector2(124,182)),Color("#4d3b55"))
	draw_rect(Rect2(p+Vector2(7,6),Vector2(110,170)),Color("#94778d"))
	draw_rect(Rect2(p+Vector2(13,13),Vector2(98,158)),Color("#21182f"))
	draw_rect(Rect2(p+Vector2(18,17),Vector2(88,149)),Color("#30204e"))
	for j in range(6,0,-1):
		draw_circle(portal,float(j)*14,Color(0.58,0.27,0.91,0.023))
	for j in range(12):
		var pos:=portal+Vector2(sin(clock*1.9+j*1.7)*36,-62+fposmod(float(j)*14+clock*18,138))
		draw_rect(Rect2(pixel_pos(pos),Vector2(4,6)),Color(0.79,0.54,1,0.25+0.20*sin(clock*3+j)))
	# Slowly counter-rotating diamond seals with a pulsing center.
	var pulse: float=0.5+0.5*sin(clock*3.1)
	for ring in range(2):
		var rotation: float=clock*(0.70 if ring==0 else -1.0)+PI*0.5
		var radius_x: float=30.0 if ring==0 else 19.0
		var radius_y: float=38.0 if ring==0 else 24.0
		var seal:=PackedVector2Array()
		for point in range(5):
			var angle: float=rotation+float(point)*TAU/4.0
			seal.append(portal+Vector2(cos(angle)*radius_x,sin(angle)*radius_y))
		draw_polyline(seal,Color(0.88,0.72,1.0,0.75+0.23*pulse),3 if ring==0 else 2)
	for spark in range(4):
		var orbit: float=clock*1.15+float(spark)*TAU/4.0
		var center: Vector2=portal+Vector2(cos(orbit)*34,sin(orbit)*45)
		draw_rect(Rect2(pixel_pos(center)-Vector2(2,2),Vector2(5,5)),Color(0.94,0.68,1.0,0.35+0.52*pulse))
	var core_size: float=9.0+3.0*pulse
	draw_rect(Rect2(portal-Vector2(core_size*0.5,core_size*0.5),Vector2(core_size,core_size)),Color("#fff2d3"))
	draw_rect(Rect2(portal-Vector2(3,3),Vector2(6,6)),Color("#f0bffa"))
	draw_rect(Rect2(p-Vector2(8,1),Vector2(140,14)),Color("#ba967d"))
	draw_rect(Rect2(p-Vector2(8,172),Vector2(140,13)),Color("#876d79"))
	# Gold-capped masonry and flickering runes give the doorway real depth.
	for side in [0.0,1.0]:
		var x: float=p.x+(7 if side==0.0 else 108)
		for step in range(6):
			var y: float=p.y+22+step*24
			draw_rect(Rect2(x,y,9,6),Color("#b68a9c"))
			draw_rect(Rect2(x+2,y+1,5,3),Color("#efd0a4"))
	for i in range(3):
		var glow: float=0.42+0.35*sin(clock*2.6+i*1.8)
		draw_rect(Rect2(portal+Vector2(-42+i*31,-73),Vector2(17,5)),Color(0.80,0.53,1,glow))
	# Selected character keeps its own 4x8 sprite sheet and walking frames.
	var facing: Vector2=player_facing
	var frame: int=2+int(clock*10)%4 if walking else int(clock*2)%2
	if model.hub_seat_index>=0:
		draw_seated_player()
	else:
		draw_shadow(model.player,68)
		sprite(model.character,model.player,facing,frame,68)

func draw_gate() -> void:
	var gate: Vector2=Model.EXIT
	var p: Vector2=gate-Vector2(30,61)
	var color: Color=[Color("#bd9be8"),Color("#8ce0ba"),Color("#e0a3d7")][model.floor_index]
	draw_rect(Rect2(p+Vector2(5,6),Vector2(60,122)),Color("#100c1b"))
	draw_rect(Rect2(p,Vector2(60,122)),Color("#352a40"))
	draw_rect(Rect2(p+Vector2(4,4),Vector2(52,114)),Color("#6f536e"))
	draw_rect(Rect2(p+Vector2(8,8),Vector2(44,106)),Color("#20172d"))
	draw_rect(Rect2(p+Vector2(12,12),Vector2(36,98)),Color("#100d1c"))
	for i in range(4):
		draw_rect(Rect2(p+Vector2(4,12+i*28),Vector2(6,7)),color.darkened(0.35))
		draw_rect(Rect2(p+Vector2(50,12+i*28),Vector2(6,7)),color.darkened(0.35))
	draw_rect(Rect2(p-Vector2(4,4),Vector2(68,10)),Color("#8b6c83"))
	draw_rect(Rect2(p+Vector2(-4,115),Vector2(68,9)),Color("#756078"))
	if gate_open_amount>0.01:
		var alpha: float=gate_open_amount
		draw_rect(Rect2(p+Vector2(13,13),Vector2(34,96)),Color(color.darkened(0.62),alpha))
		for i in range(5):
			var y: float=15+fposmod(clock*19+i*19,92)
			var x: float=30+sin(clock*2.5+i*1.7)*9
			draw_rect(Rect2(pixel_pos(p+Vector2(x-9,y)),Vector2(18,4)),Color(color,alpha*0.36))
			draw_rect(Rect2(pixel_pos(p+Vector2(x,y)),Vector2(4,4)),Color(1,0.93,1,alpha*0.85))
		draw_rect(Rect2(p+Vector2(10,10),Vector2(40,102)),Color(color,alpha*0.75),false,2)
		draw_colored_polygon(PackedVector2Array([p+Vector2(30,42),p+Vector2(40,59),p+Vector2(30,76),p+Vector2(20,59)]),Color(color,alpha*0.5))
	else:
		for i in range(5): draw_rect(Rect2(p+Vector2(16+i*6,16),Vector2(3,90)),Color("#897080"))
		draw_rect(Rect2(p+Vector2(12,53),Vector2(36,8)),Color("#b39391"))

func draw_royal_seals() -> void:
	if model.floor_index!=2: return
	for hazard in model.hazards:
		if hazard.get("type","")!="rune": continue
		var height: float=clampf(-float(hazard.warn)/0.14,0,1)*clampf(float(hazard.life)/0.24,0,1)
		var warning: bool=hazard.warn>0
		var color:=Color("#e5a2dc") if warning else Color("#f7c5ff")
		for cell in hazard.cells:
			var rect: Rect2=cell
			var p: Vector2=rect.get_center()
			var alpha: float=0.32+0.17*sin(clock*12) if warning else 0.65
			draw_rect(rect,Color(0.23,0.08,0.32,alpha))
			draw_rect(rect.grow(-2),Color(color,alpha+0.25),false,2)
			var diamond:=PackedVector2Array([p+Vector2(0,-18),p+Vector2(21,0),p+Vector2(0,18),p+Vector2(-21,0),p+Vector2(0,-18)])
			draw_polyline(diamond,Color(color,alpha+0.25),2)
			draw_rect(Rect2(p-Vector2(3,10),Vector2(6,20)),Color(color,alpha))
			draw_rect(Rect2(p-Vector2(10,3),Vector2(20,6)),Color(color,alpha))
			if height>0:
				for j in range(3):
					var foot:=p+Vector2((j-1)*14,7)
					var tip:=foot+Vector2(0,-roundf(height*(32+8*(j%2))))
					draw_colored_polygon(PackedVector2Array([foot+Vector2(-5,3),tip,foot+Vector2(5,3)]),Color(0.66,0.29,0.83,height*0.65))
					draw_line(foot,tip,Color(0.98,0.84,1,height),3)
					var spark:=foot+Vector2(sin(clock*21+j)*8,-height*22)
					draw_rect(Rect2(pixel_pos(spark),Vector2(3,3)),Color(1,0.76,0.95,height))

func build_decor() -> void:
	decor.clear()
	floor_decals.clear()
	var rng:=RandomNumberGenerator.new()
	rng.seed=int(model.seed_value)+int(model.room)*7919+int(model.floor_index)*104729
	var floor_id: int=model.floor_index
	for i in range(18):
		floor_decals.append(pixel_pos(Vector2(rng.randf_range(92,1180),rng.randf_range(165,601))))
	# Reserve full sprite footprints, wall fixtures and spike lanes. Keep
	# decorative silhouettes in a few intentional clusters, never on top of lamps.
	var occupied: Array[Rect2]=[]
	for wall in model.walls: occupied.append(wall.grow(14))
	for pos in TORCH_POSITIONS: occupied.append(Rect2(pos-Vector2(30,42),Vector2(60,92)))
	for light in side_lights: occupied.append(Rect2(light.position-Vector2(35,48),Vector2(70,100)))
	occupied.append(Rect2(Model.EXIT-Vector2(60,80),Vector2(120,160)))
	if floor_id==1:
		occupied.append(Rect2(58,146,1164,56))
		occupied.append(Rect2(58,585,1164,39))
	elif floor_id==2:
		occupied.append(Rect2(58,580,1164,44))
	if floor_id==0:
		for x in [420.0,690.0,960.0]: occupied.append(Rect2(x-43,155,86,460))
	if model.room_kind in ["shop","treasure","secret","shrine","event","forge"]:
		occupied.append(Rect2(470,145,340,190))
	decor_exclusions=occupied.duplicate()
	for side in range(2):
		for j in range(5):
			var pos:=Vector2(225+j*205+rng.randi_range(-18,18),252 if side==0 else 552)
			var prop_size: float=48
			var bounds:=Rect2(pos-Vector2(prop_size*0.5,prop_size*0.83),Vector2(prop_size,prop_size)).grow(8)
			var clear:=true
			for area in occupied:
				if area.intersects(bounds): clear=false; break
			if pos.distance_to(Model.SPAWN)<100: clear=false
			if clear and rng.randf()<0.68:
				decor.append({"pos":pos,"index":floor_id*6+rng.randi_range(0,5),"size":prop_size})
				occupied.append(bounds.grow(14))
	for i in range(decor_lights.size()):
		var light: PointLight2D=decor_lights[i]
		light.color=[Color("#e7b170"),Color("#65dcb4"),Color("#db8fd3")][floor_id]
		light.position=Vector2(180+i*300,198 if i%2==0 else 572)
		light.enabled=true
	if model.room_kind in ["shop","treasure","secret","shrine","event","forge"]:
		decor_lights[0].position=Vector2(640,240)
		decor_lights[0].color={"shop":Color("#f4c283"),"treasure":Color("#f7d88f"),"secret":Color("#a38fe8"),"shrine":Color("#9cf0cd"),"event":Color("#d8a4ee"),"forge":Color("#ff9d62")}[model.room_kind]

func decor_is_clear() -> bool:
	var occupied: Array[Rect2]=decor_exclusions.duplicate()
	for prop in decor:
		var pos: Vector2=prop.pos
		var size: float=prop.size
		var bounds:=Rect2(pos-Vector2(size*0.5,size*0.83),Vector2(size,size)).grow(8)
		for area in occupied:
			if area.intersects(bounds): return false
		occupied.append(bounds.grow(14))
	return true

func draw_prop(prop: Dictionary) -> void:
	var pos: Vector2=prop.pos
	var size: float=prop.size
	draw_rect(Rect2(pixel_pos(pos+Vector2(-size*0.40,1)),Vector2(size*0.8,8)),Color(0.03,0.02,0.05,0.50))
	draw_texture_rect_region(PROPS,Rect2(pixel_pos(pos-Vector2(size*0.5,size*0.83)),Vector2(size,size)),Rect2(int(prop.index)*24,0,24,24),Color(0.76,0.73,0.78))

func draw_architecture(arena: Rect2) -> void:
	var floor_id: int=model.floor_index
	var edge: Color=[Color("#9b7d84"),Color("#648d75"),Color("#a58282")][floor_id]
	# Layered wall faces, cornices and deep contact shadows.
	draw_rect(Rect2(58,105,1164,8),edge.darkened(0.4))
	draw_rect(Rect2(58,114,1164,3),edge)
	draw_rect(Rect2(58,138,1164,8),Color("#171421"))
	for i in range(7):
		draw_rect(Rect2(58,146+i*3,1164,3),Color(0.025,0.018,0.05,0.25*(1-float(i)/7)))
		draw_rect(Rect2(58,616-i*3,1164,3),Color(0.025,0.018,0.05,0.18*(1-float(i)/7)))
	for x in [58.0,1210.0]:
		draw_rect(Rect2(x,146,12,472),edge.darkened(0.55))
		draw_rect(Rect2(x+2,146,2,472),edge.darkened(0.15))
	for i in range(4):
		var x: float=270+i*250
		if floor_id==2:
			draw_rect(Rect2(x-18,109,36,30),Color("#271c2c"))
			draw_rect(Rect2(x-14,111,28,25),Color("#816373"),false,2)
			draw_rect(Rect2(x-3,116,6,15),Color("#d1ac85"))
			draw_rect(Rect2(x-9,121,18,4),Color("#d1ac85"))
			continue
		draw_rect(Rect2(x-26,115,52,24),Color("#171320"))
		draw_rect(Rect2(x-22,117,44,18),edge.darkened(0.45))
		draw_rect(Rect2(x-18,119,36,14),Color("#1b1726"))
		if floor_id==0:
			draw_rect(Rect2(x-8,119,16,13),Color("#665262"))
			for j in range(4): draw_rect(Rect2(x-7+j*4,119,2,13),Color("#b1958c"))
		elif floor_id==1:
			for j in range(5):
				draw_rect(Rect2(x-18+j*8,120,5,12),Color("#477a59"))
		else:
			draw_rect(Rect2(x-4,119,8,13),Color("#d5a87e"))
			draw_rect(Rect2(x-11,122,22,3),Color("#d5a87e"))

func draw_room_decals() -> void:
	var floor_id: int=model.floor_index
	var color: Color=[Color("#82707c"),Color("#587e53"),Color("#99736f")][floor_id]
	# Stable floor-level wear and biome-specific motifs; no changes to collision.
	for i in range(floor_decals.size()):
		var p: Vector2=floor_decals[i]
		if floor_id==0:
			draw_rect(Rect2(p,Vector2(8,2)),color.darkened(0.4))
			draw_rect(Rect2(p+Vector2(8,2),Vector2(2,5)),color.darkened(0.5))
			if i%4==0: draw_rect(Rect2(p+Vector2(10,7),Vector2(8,2)),color.darkened(0.35))
		elif floor_id==1:
			for j in range(4):
				draw_rect(Rect2(p+Vector2(j*4,sin(j*2)*4),Vector2(6,5)),Color(color,0.42))
			if i%3==0:
				draw_rect(Rect2(p-Vector2(2,2),Vector2(4,4)),Color("#a1ce83"))
		else:
			if i%3==0:
				draw_rect(Rect2(p,Vector2(16,16)),color.darkened(0.3),false,2)
				draw_rect(Rect2(p+Vector2(6,4),Vector2(4,8)),color.darkened(0.15))
			else: draw_rect(Rect2(p,Vector2(10,3)),color.darkened(0.5))
	# A separate focal ornament for each layout and boss arena.
	if model.room_kind=="boss":
		var center:=Vector2(860,385)
		for i in range(3):
			draw_arc(center,96+i*16,0,TAU,48,Color(color,0.38-i*0.08),2,true)
		for i in range(8):
			var p:=pixel_pos(center+Vector2.from_angle(i*TAU/8)*128)
			draw_rect(Rect2(p-Vector2(4,4),Vector2(8,8)),color)
	elif floor_id==0:
		draw_rect(Rect2(298,294,80,182),Color("#3b3042"),false,2)
		for i in range(5): draw_rect(Rect2(312,310+i*32,52,3),Color("#5f4d61"))
	elif floor_id==1:
		for i in range(7):
			var p:=Vector2(300+i*24,304+sin(i*0.9)*28)
			draw_rect(Rect2(pixel_pos(p),Vector2(26,7)),Color("#4c6a43"))
			draw_rect(Rect2(pixel_pos(p+Vector2(1,0)),Vector2(22,2)),Color("#839a60"))
	else:
		draw_rect(Rect2(295,325,94,120),Color("#5d414e"),false,2)
		draw_rect(Rect2(302,332,80,106),Color("#8b6b63"),false,1)

func draw_service_scene() -> void:
	if model.room_kind not in ["shop","treasure","secret","shrine","event","forge"]: return
	var center:=Vector2(640,252)
	var color: Color={"shop":Color("#d7aa73"),"treasure":Color("#e7be7b"),"secret":Color("#9174c4"),"shrine":Color("#83c6a2"),"event":Color("#c184c4"),"forge":Color("#e98754")}[model.room_kind]
	# The service scene remains behind its modal: a staged, biome-specific landmark.
	draw_rect(Rect2(480,160,320,170),Color(0.035,0.025,0.055,0.55))
	draw_rect(Rect2(492,164,296,160),color.darkened(0.65),false,3)
	draw_rect(Rect2(505,174,270,140),color.darkened(0.75),false,1)
	match model.room_kind:
		"shop":
			for i in range(7):
				draw_rect(Rect2(500+i*40,164,40,25),Color("#7b4c53") if i%2==0 else Color("#c1a16e"))
			draw_rect(Rect2(510,240,260,18),Color("#8a604d"))
			draw_rect(Rect2(510,240,260,4),Color("#d5aa75"))
			for i in range(5): draw_prop({"pos":Vector2(534+i*52,231),"index":i%6,"size":36})
		"forge":
			draw_rect(Rect2(512,180,80,65),Color("#322230"))
			draw_rect(Rect2(520,190,64,48),Color("#7d423a"))
			draw_rect(Rect2(528,202,48,32),Color("#d06438"))
			for i in range(8):
				var p:=Vector2(531+i*6,223-fposmod(clock*(16+i*2)+i*11,35))
				draw_rect(Rect2(pixel_pos(p),Vector2(4,4)),Color("#ffd28b"))
			draw_prop({"pos":Vector2(738,246),"index":5,"size":72})
		"treasure","secret":
			for i in range(3):
				draw_rect(Rect2(536+i*12,276-i*10,208-i*24,12),color.darkened(0.48-i*0.10))
			draw_rect(Rect2(600,175,80,100),Color(color,0.08+sin(clock*2)*0.025))
		"shrine","event":
			for i in range(3):
				draw_arc(center,62+i*18,0,TAU,48,Color(color,0.42-i*0.10),2,true)
			for i in range(8):
				var p:=pixel_pos(center+Vector2.from_angle(i*TAU/8+clock*0.16)*80)
				draw_rect(Rect2(p,Vector2(5,5)),color)
	for i in range(10):
		var p:=Vector2(530+fposmod(i*53+clock*9,220),180+fposmod(i*31-clock*14,125))
		draw_rect(Rect2(pixel_pos(p),Vector2(2,3)),Color(color,0.46))

func draw_biome_details(arena: Rect2) -> void:
	if model.floor_index==0:
		# Each physical plate is drawn on the floor canvas. No tiles are placed
		# within the collision margin of a wall, and actors sort above the tips.
		for x in [420.0,690.0,960.0]:
			for cell in model.spike_cells(x):
				var plate: Rect2=cell
				draw_rect(plate,Color("#181520"))
				draw_rect(plate.grow(-2),Color("#4b4354"))
				draw_rect(plate.grow(-4),Color("#292533"))
				draw_line(plate.position+Vector2(3,3),plate.position+Vector2(plate.size.x-3,3),Color("#8c7985"),2)
				var height:=0.0
				var warning:=false
				for hazard in model.hazards:
					if hazard.get("type","")!="spikes" or not hazard.cells.has(plate): continue
					warning=hazard.warn>0
					if hazard.warn<=0:
						height=clampf(-float(hazard.warn)/0.12,0,1)*clampf(float(hazard.life)/0.20,0,1)
				for i in range(3):
					var p:=plate.position+Vector2(10+i*16,15)
					draw_rect(Rect2(p-Vector2(5,4),Vector2(10,8)),Color("#100e17"))
					draw_rect(Rect2(p-Vector2(4,4),Vector2(8,2)),Color("#736372"))
					if warning:
						draw_rect(Rect2(p-Vector2(4,2),Vector2(8,3)),Color(0.98,0.66,0.24,0.55+sin(clock*18)*0.35))
					if height>0:
						var h: float=roundf(height*18/2)*2
						draw_rect(Rect2(p-Vector2(6,2),Vector2(12,5)),Color(0.05,0.03,0.09,0.6))
						var tip:=p+Vector2(0,-h)
						draw_colored_polygon(PackedVector2Array([p+Vector2(-5,2),tip,p+Vector2(5,2)]),Color("#c4b4bf"))
						draw_colored_polygon(PackedVector2Array([p,tip,p+Vector2(5,2)]),Color("#746577"))
						draw_line(tip,p+Vector2(-4,1),Color("#f5e4d0"),1)
	elif model.floor_index==1:
		# Dense moss, branching vines and luminous mushrooms, not new collision.
		for i in range(7):
			var x: float=120+i*160
			draw_rect(Rect2(x,148,38,13),Color("#234330"))
			for j in range(5):
				var p:=Vector2(x+j*6,158+j*7)
				draw_rect(Rect2(p,Vector2(7,9)),Color("#47764c"))
				draw_rect(Rect2(p+Vector2(2,2),Vector2(3,4)),Color("#81a964"))
			var pos:=Vector2(x+22,610)
			draw_rect(Rect2(pos-Vector2(2,0),Vector2(4,13)),Color("#d0c69b"))
			draw_rect(Rect2(pos-Vector2(11,6),Vector2(22,9)),Color("#72bd91"))
			draw_rect(Rect2(pos-Vector2(7,10),Vector2(14,5)),Color("#b1e4a5"))
			draw_circle(pos,16,Color(0.37,0.9,0.58,0.08+sin(clock*2+i)*0.025))
	else:
		# Royal processional mosaic, torn carpets and sepulchral candles.
		draw_rect(Rect2(80,355,1115,60),Color("#3e2438"))
		draw_rect(Rect2(80,356,1115,4),Color("#94725d"))
		draw_rect(Rect2(80,410,1115,4),Color("#94725d"))
		for x in range(130,1180,80):
			draw_rect(Rect2(x,374,24,24),Color("#76504d"),false,2)
			draw_rect(Rect2(x+10,378,4,16),Color("#ac8068"))
			draw_rect(Rect2(x+4,384,16,4),Color("#ac8068"))
		# Candles are integrated into the wall fixtures below, not scattered
		# into the floor props or sarcophagi.

func draw_pillar(rect: Rect2) -> void:
	draw_rect(Rect2(rect.position+Vector2(5,6),rect.size),Color("#191320"))
	tile(5,rect)
	draw_rect(Rect2(rect.position+Vector2(0,rect.size.y-12),Vector2(rect.size.x,12)),Color("#1f1927"))
	draw_rect(Rect2(rect.position+Vector2(0,rect.size.y-14),Vector2(rect.size.x,2)),Color("#877284"))
	if model.floor_index==1:
		draw_rect(Rect2(rect.position+Vector2(5,5),rect.size-Vector2(10,10)),Color("#365b42"))
		for i in range(5):
			var p: Vector2=rect.position+Vector2(i*rect.size.x/5,8+sin(i*2)*4)
			draw_rect(Rect2(p,Vector2(10,rect.size.y-12)),Color("#664d39"))
			draw_rect(Rect2(p+Vector2(2,2),Vector2(5,rect.size.y-20)),Color("#8b704a"))
		draw_rect(Rect2(rect.position,Vector2(rect.size.x,9)),Color("#80a45e"))
	elif model.floor_index==2:
		draw_rect(rect.grow(-5),Color("#382c3a"))
		draw_rect(rect.grow(-9),Color("#947b76"),false,3)
		var center:=rect.get_center()
		draw_rect(Rect2(center-Vector2(5,22),Vector2(10,44)),Color("#b5a18e"))
		draw_rect(Rect2(center-Vector2(17,6),Vector2(34,12)),Color("#b5a18e"))
	else:
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
	if model.attack_timer > maxf(0.0,model.attack_cd-0.18) or model.bolt_timer > maxf(0.0,model.special_cd-0.25):
		frame = 6
	if model.invulnerable > 0.3:
		frame = 7
	if model.dash_left > 0:
		frame = 3
		for i in range(3,0,-1):
			sprite(model.character,pos-model.dash_direction*float(i)*14,player_facing,frame,64,Color(0.65,0.5,1,0.12))
	for echo in model.echoes:
		sprite(model.character,echo.pos,echo.dir,6,64,Color(0.65,0.45,1,0.45))
	if model.spirit_life>0:
		sprite("soulbinder",model.spirit_pos,Vector2.DOWN,2+int(clock*8)%4,52,Color(0.55,1.0,0.8,0.65))
	sprite(model.character,pos,player_facing,frame,64)

func draw_actor(enemy: Dictionary) -> void:
	var kind: String = enemy.kind
	var pos: Vector2 = enemy.pos
	var size := 128.0 if kind in ["boss","rootking","hollowking"] else (80.0 if kind=="mushroom" else 64.0)
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
	if kind in ["boss","rootking","hollowking"]:
		var phase: int=int(enemy.get("phase",1))
		var boss_color: Color=Color("#f1c986") if kind=="boss" else (Color("#96d6a3") if kind=="rootking" else Color("#d59cf4"))
		for ring in range(phase):
			var radius: float=size*(0.39+float(ring)*0.095)+sin(clock*4.2+ring)*2
			draw_arc(pos+Vector2(0,8),radius,0,TAU,48,Color(boss_color,0.4+0.14*sin(clock*3+ring)),3,true)
		if float(enemy.get("phase_lock",0))>0.0:
			var lock_alpha: float=clampf(float(enemy.get("phase_lock",0)),0,1)
			draw_circle(pos+Vector2(0,-16),size*0.37,Color(boss_color,0.10*lock_alpha))
			for spark in range(12):
				var ray: Vector2=Vector2.from_angle(float(spark)*TAU/12.0+clock*1.8)
				var p: Vector2=pos+Vector2(0,-16)+ray*(size*0.46)
				draw_rect(Rect2(pixel_pos(p)-Vector2(3,3),Vector2(6,6)),Color(boss_color,lock_alpha))
	var affix: String=str(enemy.get("affix",""))
	if affix!="":
		# Champion identity must be visible before the player attacks.
		var accent: Color=Color("#f28b7b") if affix=="frenzied" else (Color("#83cbf3") if affix=="warded" else Color("#ee83b7"))
		draw_arc(pos+Vector2(0,9),size*0.48,0,TAU,32,Color(accent,0.7+0.25*sin(clock*4.0)),3,true)
	sprite(kind,pos,direction,frame,size,tint)
	if affix!="":
		var accent: Color=Color("#f28b7b") if affix=="frenzied" else (Color("#83cbf3") if affix=="warded" else Color("#ee83b7"))
		var badge_pos: Vector2=pos+Vector2(-10,-size*0.75)
		draw_rect(Rect2(badge_pos,Vector2(20,13)),Color("#1e1726"))
		draw_rect(Rect2(badge_pos,Vector2(20,13)),accent,false,2)
		for i in range(3):
			draw_rect(Rect2(badge_pos+Vector2(4+i*5,4),Vector2(3,5)),accent if affix!="warded" else Color("#d5f4ff"))

func is_ready_for_smoke() -> bool:
	return TILES.get_width() == 256 and RELICS.get_width() == 1920 and RELICS.get_height() == 48 and ACTORS["dealer"].get_height() == 128 and ACTORS["warden"].get_height() == 128 and torches.size() == 5 and side_lights.size() == 2 and ambient != null and boss_light != null and torches[0].range_layer_min == -1 and occluders.size() == model.walls.size() and PROPS.get_width()==432 and decor_lights.size()==4 
