extends Node2D
## Pixel art is a rendering layer only: model coordinates and collision rules stay unchanged.
const Model = preload("res://run_model.gd")
const TILES = preload("res://art/pixel/vault_tiles.svg")
const TORCH = preload("res://art/pixel/torch.svg")
const RELICS = preload("res://art/pixel/relics.svg")
const ACTORS = {
	"warden": preload("res://art/pixel/warden.svg"),
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
var light_texture: GradientTexture2D

func setup(value) -> void:
	model = value
	z_index = -1
	texture_filter = CanvasItem.TEXTURE_FILTER_NEAREST
	previous_player = model.player
	var gradient := Gradient.new()
	gradient.colors = PackedColorArray([Color(1,1,1,0.95),Color(1,1,1,0.32),Color(1,1,1,0)])
	gradient.offsets = PackedFloat32Array([0.0,0.48,1.0])
	light_texture = GradientTexture2D.new()
	light_texture.gradient = gradient
	light_texture.width = 128
	light_texture.height = 128
	light_texture.fill = GradientTexture2D.FILL_RADIAL
	light_texture.fill_from = Vector2(0.5,0.5)
	light_texture.fill_to = Vector2(1,0.5)
	for pos in TORCH_POSITIONS:
		var light := make_light(Color("#ffc277"),2.8,0.85,true)
		light.position = pos + Vector2(0,18)
		torches.append(light)
	player_light = make_light(Color("#d8caff"),2.1,0.32,false)
	gate_light = make_light(Color("#b18aff"),2.0,0.7,true)
	gate_light.position = Model.EXIT
	for i in range(4):
		projectile_lights.append(make_light(Color("#8adcf8"),0.75,0.65,false))
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
	add_child(light)
	return light

func rebuild_occluders() -> void:
	for old in occluders:
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

func update_view(dt: float) -> void:
	clock += dt
	if last_room != model.room or occluders.size() != model.walls.size():
		rebuild_occluders()
	var delta: Vector2 = model.player-previous_player
	walking = delta.length_squared() > 0.05
	if walking:
		player_facing = delta.normalized()
	if model.attack_timer > 0.17 or model.bolt_timer > 0.48:
		player_facing = model.aim
	previous_player = model.player
	for i in range(torches.size()):
		torches[i].energy = 0.75 + sin(clock*7.0+i*2.2)*0.1 + sin(clock*13+i)*0.04
	player_light.position = model.player
	gate_light.enabled = model.state == "exit"
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
	draw_texture_rect_region(TILES,rect,Rect2(Vector2(index*32,0),source_size))

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
			var variant := (x*13+y*7+model.room*3)%4
			tile(variant,Rect2(arena.position+offset,size),size/2.0)
	for x in range(19):
		tile(4,Rect2(56+x*64,105,64,40))
		tile(4,Rect2(56+x*64,624,64,20))
	for y in range(7):
		tile(4,Rect2(23,146+y*68,35,68))
		tile(4,Rect2(1222,146+y*68,35,68))
	draw_rect(Rect2(23,105,1234,542),Color("#755b73"),false,2)
	for i in range(TORCH_POSITIONS.size()):
		var pos: Vector2 = TORCH_POSITIONS[i]
		var frame := int(clock*8+i)%4
		draw_texture_rect_region(TORCH,Rect2(pos-Vector2(16,37),Vector2(32,64)),Rect2(frame*16,0,16,32))
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
			sprite("warden",pos-model.dash_direction*float(i)*14,player_facing,frame,64,Color(0.65,0.5,1,0.12))
	sprite("warden",pos,player_facing,frame,64)

func draw_actor(enemy: Dictionary) -> void:
	var kind: String = enemy.kind
	var pos: Vector2 = enemy.pos
	var size := 128.0 if kind == "boss" else 64.0
	var direction: Vector2 = pos.direction_to(model.player)
	var frame := 2+int(clock*8+pos.x*0.02)%4
	if enemy.wind > 0:
		frame = 6
	if enemy.flash > 0:
		frame = 7
	draw_shadow(pos,size)
	var tint := Color("#ffedda") if enemy.flash > 0 else Color.WHITE
	sprite(kind,pos,direction,frame,size,tint)

func is_ready_for_smoke() -> bool:
	return TILES.get_width() == 256 and ACTORS["warden"].get_height() == 128 and torches.size() == 5 and occluders.size() == model.walls.size()
