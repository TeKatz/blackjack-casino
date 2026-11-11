extends RefCounted
## Pure run state. Fixed-tick simulation, seeded room generation and combat.
const ARENA := Rect2(58, 146, 1164, 478)
const SPAWN := Vector2(143, 385)
const EXIT := Vector2(1174, 385)
const HUB_SPAWN := Vector2(318,425)
const HUB_PORTAL := Vector2(1045,388)
const HUB_RANGE := 125.0
# Hub furniture is authoritative geometry; art and interaction points use these footprints.
const HUB_BAR := Rect2(220,146,520,178)
const HUB_FORGE := Rect2(735,146,108,166)
const HUB_TABLE := Rect2(458,469,132,96)
const HUB_GATE_FRAME := Rect2(988,298,115,180)
const HUB_BARTENDER_POINT := Vector2(482,326)
const HUB_FORGE_POINT := Vector2(790,329)
const HUB_SERVICE_RANGE := 104.0
const HUB_STOOL_X := [275.0,380.0,485.0,590.0,695.0]
const HUB_CARD_GUEST_LEFT := Rect2(393,468,49,93)
const HUB_CARD_GUEST_RIGHT := Rect2(607,468,49,93)
const HUB_SEAT_HOLD := 1.0
# Strong run-scoped choices, separate from persistent casino progression.
const FORGE_DATA := {
 "rift_echo":{"en":"Echo Anvil","de":"Echosschmiede","en_effect":"Each primary attack releases a delayed spectral copy at 85% damage.","de_effect":"Jeder Hauptangriff entfesselt ein verzögertes Geistergeschoss mit 85 % Schaden.","en_cost":"Special cooldown +40%.","de_cost":"Spezial-Cooldown +40 %."},
 "stormheart":{"en":"Stormheart","de":"Sturmherz","en_effect":"Every special launches FIVE extra arcane bolts.","de_effect":"Jeder Spezialangriff entfesselt FÜNF zusätzliche Arkangeschosse.","en_cost":"Maximum HP -30%.","de_cost":"Maximale LP -30 %."},
 "void_stride":{"en":"Void Stride","de":"Leerenritt","en_effect":"Dash twice as often and blast nearby enemies with every dash.","de_effect":"Dash doppelt so häufig und entfessle bei jedem Dash eine Schadenswelle.","en_cost":"Maximum HP -25%.","de_cost":"Maximale LP -25 %."},
 "iron_aegis":{"en":"Iron Aegis","de":"Eisenägis","en_effect":"Negate the first TWO hits in each combat room; shatter into a shockwave.","de_effect":"Blocke ZWEI Treffer pro Kampfraum, danach explodiert der Schild.","en_cost":"Movement speed -25%.","de_cost":"Bewegungstempo -25 %."}
}
const CONTRACT_DATA := {
 "slayer":{"en":"Bloodied Hands","de":"Blutige Hände","en_goal":"Defeat 12 enemies.","de_goal":"Besiege 12 Gegner.","en_reward":"Gilded Edge: +9 main damage.","de_reward":"Goldene Klinge: +9 Hauptschaden.","goal":12},
 "untouched":{"en":"Untouchable","de":"Unberührbar","en_goal":"Clear 2 combat rooms without losing HP.","de_goal":"Schließe 2 Kampfräume ohne LP-Verlust ab.","en_reward":"Iron Ward: +4 armor.","de_reward":"Eisenwall: +4 Rüstung.","goal":2},
 "hoarder":{"en":"The Collector","de":"Der Sammler","en_goal":"Hold 30 shards at once on floor 1.","de_goal":"Besitze 30 Splitter gleichzeitig auf Ebene 1.","en_reward":"Arcane Focus: +11 special damage.","de_reward":"Arkaner Fokus: +11 Spezialschaden.","goal":30},
 "spellweaver":{"en":"Arcane Trial","de":"Arkane Prüfung","en_goal":"Use 12 special attacks.","de_goal":"Nutze 12 Spezialangriffe.","en_reward":"Mana Sigil: 15% faster specials.","de_reward":"Manasiegel: 15 % schnellere Spezialangriffe.","goal":12}
}
const ROOM_COUNT := 26
var environment_timer := 0.0
var shot_style := ""
var enemy_shot_style := ""
var environment_cycle := 0
var wave := 1
var wave_total := 1
const FLOOR_COUNT := 3
var bosses_defeated := 0
var combat_clears := 0
var souls := 0
var specials := 0
var harvest_count := 0
var guard_pulse_pending := false
var special_multiplier := 1.0
var spirit_timer := 0.0
var spirit_life := 0.0
var spirit_pos := Vector2.ZERO
const Dungeon = preload("res://dungeon.gd")
var dungeon = Dungeon.new()
var floor_index := 0
var visited_count := 0
var room_kind := "fight"
var service_used := false
var boss_defeated := false
var elites := 0
var active_item := "heal"
var active_cd := 24.0
var slow_time := 0.0
var shield_time := 0.0
var temp_power := 0.0
var revive_used := false
var orbit_timer := 0.0
var echoes: Array[Dictionary] = []
var hazards: Array[Dictionary] = []
const Content = preload("res://content.gd")
const RELICS = Content.RELICS
var character := "warden"
var set_id := "warden_guard"
var settled := false
var attack_cd := 0.36
var special_cd := 0.62
var active_timer := 0.0
var active_heal := 25.0
var attacks := 0
var dash_hits: Array[Dictionary] = []
var rng := RandomNumberGenerator.new()
var seed_value: int
var room := 0
var state := "hub"
var player := SPAWN
var hub_seat_index := -1
var hub_seat_target := -1
var hub_seat_hold := 0.0
var hub_seat_anim := 0.0
var hub_seat_released := false
var aim := Vector2.RIGHT
var hp := 100.0
var max_hp := 100.0
var damage := 26.0
var speed := 245.0
var armor := 0.0
var bolt_damage := 17.0
var dash_cooldown := 1.3
var attack_timer := 0.0
var bolt_timer := 0.0
var dash_timer := 0.0
var dash_left := 0.0
var invulnerable := 0.0
var dash_direction := Vector2.RIGHT
var elapsed := 0.0
var kills := 0
var shards := 0
var relics: Array[String] = []
var offers: Array[String] = []
var enemies: Array[Dictionary] = []
var shots: Array[Dictionary] = []
var walls: Array[Rect2] = []
var effects: Array[Dictionary] = []
var notices: Array[String] = []
var forge_id := ""
var forge_offers: Array[String] = []
var forge_guard_charges := 0
var contract_id := ""
var contract_offers: Array[String] = []
var contract_progress := 0
var contract_completed := false
var contract_clean_room := true

func begin(value: int, loadout: String = "warden_guard", upgrades: Dictionary = {}) -> void:
	seed_value = value
	rng.seed = value
	room = 0
	floor_index = 0
	visited_count = 0
	elites = 0
	boss_defeated = false
	bosses_defeated=0
	combat_clears=0
	souls=0
	specials=0
	harvest_count=0
	guard_pulse_pending=false
	special_multiplier=1.0
	spirit_life=0
	active_item = "heal"
	active_cd = 40.0
	slow_time = 0
	shield_time = 0
	temp_power = 0
	revive_used = false
	orbit_timer=0
	dungeon.generate(value,0)
	set_id = loadout if Content.SETS.has(loadout) else "warden_guard"
	var spec: Dictionary = Content.SETS[set_id]
	character = spec.character
	max_hp = float(spec.hp)+5*int(upgrades.get("vitality",0))
	hp = max_hp
	damage = float(spec.damage)+2*int(upgrades.get("power",0))
	speed = float(spec.speed)+8*int(upgrades.get("agility",0))
	armor = 0.0
	bolt_damage = float(spec.bolt)+3*int(upgrades.get("arcane",0))
	dash_cooldown = spec.dash
	attack_cd = spec.attack
	special_cd = spec.special
	active_heal = 18.0+3*int(upgrades.get("alchemy",0))
	active_timer = 0
	attacks = 0
	settled = false
	dash_hits.clear()
	kills = 0
	shards = 0
	elapsed = 0.0
	relics.clear()
	notices.clear()
	forge_id=""
	forge_guard_charges=0
	contract_id=""
	contract_progress=0
	contract_completed=false
	contract_clean_room=true
	forge_offers=roll_hub_offers(FORGE_DATA.keys(),1031)
	contract_offers=roll_hub_offers(CONTRACT_DATA.keys(),4001)
	enter_hub()

func roll_hub_offers(source: Array, salt: int) -> Array[String]:
	# An isolated RNG prevents this hub roll from perturbing the dungeon seed.
	var pool: Array=source.duplicate()
	var picker:=RandomNumberGenerator.new()
	picker.seed=seed_value+salt
	var result: Array[String]=[]
	for i in range(mini(3,pool.size())):
		var index: int=picker.randi_range(0,pool.size()-1)
		result.append(str(pool[index]))
		pool.remove_at(index)
	return result

func select_hub_forge(id: String) -> bool:
	if state!="hub" or forge_id!="" or not forge_offers.has(id): return false
	forge_id=id
	match id:
		"rift_echo": special_cd*=1.40
		"stormheart": max_hp=maxf(20.0,max_hp*0.70)
		"void_stride":
			dash_cooldown=maxf(0.38,dash_cooldown*0.50)
			max_hp=maxf(20.0,max_hp*0.75)
		"iron_aegis": speed*=0.75
	hp=minf(hp,max_hp)
	notices.append("forge_select")
	return true

func select_hub_contract(id: String) -> bool:
	if state!="hub" or contract_id!="" or not contract_offers.has(id): return false
	contract_id=id
	contract_progress=0
	contract_completed=false
	notices.append("contract_accept")
	return true

func advance_contract(id: String, quantity: int=1) -> void:
	if contract_id!=id or contract_completed or state=="hub": return
	contract_progress=mini(int(CONTRACT_DATA[id]["goal"]),contract_progress+quantity)
	if contract_progress>=int(CONTRACT_DATA[id]["goal"]): complete_contract()

func check_shard_contract() -> void:
	if contract_id!="hoarder" or contract_completed or floor_index!=0: return
	contract_progress=mini(int(CONTRACT_DATA["hoarder"]["goal"]),maxi(0,shards))
	if contract_progress>=int(CONTRACT_DATA["hoarder"]["goal"]): complete_contract()

func complete_contract() -> void:
	if contract_completed or contract_id=="": return
	contract_completed=true
	var reward_id: String={"slayer":"blade","untouched":"ward","hoarder":"focus","spellweaver":"mana"}[contract_id]
	relics.append(reward_id)
	match reward_id:
		"blade": damage+=9
		"ward": armor+=4
		"focus": bolt_damage+=11
		"mana": special_cd=maxf(0.2,special_cd*0.85)
	notices.append("contract_complete")
	effect("contract_complete",player,1.0,Vector2.ZERO)

func enter_hub() -> void:
	# This tavern is outside the route graph: no free combat progress or rewards.
	state="hub"
	room_kind="hub"
	player=HUB_SPAWN
	hub_seat_index=-1
	hub_seat_target=-1
	hub_seat_hold=0.0
	hub_seat_anim=0.0
	hub_seat_released=false
	aim=Vector2.RIGHT
	attack_timer=0.0
	bolt_timer=0.0
	dash_timer=0.0
	dash_left=0.0
	invulnerable=0.0
	enemies.clear()
	shots.clear()
	effects.clear()
	hazards.clear()
	echoes.clear()
	offers.clear()
	walls.clear()
	# Each volume extends over the entire visual footprint. The bar and forge
	# meet the northern arena limit so neither can be bypassed from behind.
	walls.append_array([HUB_BAR,HUB_FORGE,HUB_TABLE,HUB_GATE_FRAME,HUB_CARD_GUEST_LEFT,HUB_CARD_GUEST_RIGHT])

func reset_hub_stool_hold() -> void:
	hub_seat_target=-1
	hub_seat_hold=0.0

func update_hub_stool(dt: float, direction: Vector2) -> void:
	if hub_seat_index>=0: return
	var candidate: int=-1
	# A deliberate, continuous upward push against a stool is required.
	# Walking past the bar or briefly tapping toward it never seats the player.
	if direction.y < -0.75 and absf(direction.x)<0.35 and player.y>=HUB_BAR.end.y+14.5 and player.y<=HUB_BAR.end.y+20.0:
		for i in range(HUB_STOOL_X.size()):
			if absf(player.x-float(HUB_STOOL_X[i]))<=21.0:
				candidate=i
				break
	if candidate<0:
		reset_hub_stool_hold()
		return
	if hub_seat_target!=candidate:
		hub_seat_target=candidate
		hub_seat_hold=0.0
	hub_seat_hold+=dt
	if hub_seat_hold>=HUB_SEAT_HOLD:
		hub_seat_index=candidate
		hub_seat_anim=0.0
		hub_seat_released=false
		player=Vector2(float(HUB_STOOL_X[candidate]),HUB_BAR.end.y+15.0)
		reset_hub_stool_hold()

func hub_bartender_available() -> bool:
	return state=="hub" and player.y >= HUB_BAR.end.y and player.distance_to(HUB_BARTENDER_POINT)<=HUB_SERVICE_RANGE

func hub_forge_available() -> bool:
	return state=="hub" and player.distance_to(HUB_FORGE_POINT)<=HUB_SERVICE_RANGE

func hub_portal_available() -> bool:
	return state=="hub" and player.distance_to(HUB_PORTAL)<=HUB_RANGE

func enter_first_room() -> bool:
	if not hub_portal_available(): return false
	room=0
	floor_index=0
	enter_room()
	notices.append("portal_enter")
	return true

func enter_room() -> void:
	state = "fight"
	player = SPAWN
	aim = Vector2.RIGHT
	attack_timer = 0.0
	bolt_timer = 0.0
	dash_timer = 0.0
	dash_left = 0.0
	invulnerable = 0.7
	enemies.clear()
	shots.clear()
	effects.clear()
	hazards.clear()
	echoes.clear()
	spirit_life=0
	guard_pulse_pending=false
	offers.clear()
	walls.clear()
	room_kind = dungeon.kind(room)
	contract_clean_room=true
	forge_guard_charges=2 if forge_id=="iron_aegis" and room_kind in ["fight","elite","boss"] else 0
	environment_timer=2.5
	environment_cycle=0
	wave=1
	wave_total=2 if floor_index>=1 and room_kind=="elite" else 1
	temp_power=0
	if relics.has("curse"): temp_power=rng.randf_range(-5,15)
	
	var node: Dictionary = dungeon.nodes[room]
	if not node.visited:
		node.visited = true
		visited_count += 1
		if relics.has("medic"): hp=minf(max_hp,hp+3*relics.count("medic"))
		if relics.has("sacrifice") and room_kind in ["fight","elite","boss"]: hp=maxf(1,hp-2*relics.count("sacrifice"))
	service_used = node.cleared
	build_room_layout(node)
	if node.cleared:
		state = "route"
		return
	if room_kind in ["treasure","shop","event","shrine","secret","forge"]:
		state = "service"
		return
	if room_kind != "boss":
		var count := 4 + floor_index + (2 if room_kind=="elite" else room%3)
		if wave_total==2: count=4+floor_index
		for i in range(count):
			var family: Array = [["stalker","caster","brute","archer","shield"],["spore","vine","mushroom","caster","brute"],["revenant","hexer","sentinel","archer","shield"]][floor_index]
			var kind: String = family[(i+room+floor_index)%family.size()]
			spawn_enemy(kind,Vector2(520+(i%3)*235,195+(i/3)*135+rng.randf_range(-18,18)))
			if room_kind=="elite":
				enemies[-1].hp *= 1.65
				enemies[-1].max_hp = enemies[-1].hp
				enemies[-1]["elite"] = true
	else:
		spawn_enemy(["boss","rootking","hollowking"][floor_index],Vector2(925,385))
		spawn_enemy(["caster","spore","hexer"][floor_index],Vector2(785,230))
		spawn_enemy(["stalker","vine","revenant"][floor_index],Vector2(785,540))

func build_room_layout(node: Dictionary) -> void:
	if room_kind=="boss":
		if floor_index==2:
			walls.append(Rect2(480,172,76,76))
			walls.append(Rect2(480,525,76,76))
		return
	var geometry:=RandomNumberGenerator.new()
	geometry.seed=int(node.geometry_seed)
	var layout: int=node.layout
	if room_kind in ["treasure","secret","shrine"]:
		walls.append(Rect2(590,175,100,58))
		return
	if room_kind in ["shop","event","forge"]:
		walls.append(Rect2(560,174,160,48))
		return
	if floor_index==1:
		# Root islands create curved routes around dense growth.
		for x in [420,710,930]:
			var y: float=205 if (int(x)+layout)%3==0 else 490
			walls.append(Rect2(x,y,95,80))
		if layout%2==0: walls.append(Rect2(635,335,80,90))
		return
	if floor_index==2:
		# Tomb corridors: long sarcophagi with alternating gaps.
		for x in [400,650,900]:
			walls.append(Rect2(x,180 if layout%2==0 else 460,78,145))
		walls.append(Rect2(525,460 if layout%2==0 else 180,110,90))
		walls.append(Rect2(780,460 if layout%2==0 else 180,110,90))
		return
	match layout:
		0:
			walls.append(Rect2(435+geometry.randf_range(-25,25),210,84,64))
			walls.append(Rect2(790+geometry.randf_range(-25,25),485,84,64))
		1:
			for x in [410,650,890]:
				walls.append(Rect2(x,230,64,72))
				walls.append(Rect2(x,480,64,72))
		2:
			walls.append(Rect2(470,170,210,48))
			walls.append(Rect2(700,540,210,48))
			walls.append(Rect2(610,340,72,80))
		3:
			walls.append(Rect2(385,285,65,160))
			walls.append(Rect2(870,285,65,160))
			walls.append(Rect2(610,178,100,64))
			walls.append(Rect2(610,490,100,64))

func route_options() -> Array:
	var options: Array=dungeon.available(room).duplicate()
	if not dungeon.boss_ready(): options.erase(Dungeon.BOSS)
	return options

func open_route() -> bool:
	if state!="exit" or player.distance_to(EXIT)>75: return false
	state="route"
	return true

func travel(destination: int) -> bool:
	if state!="route" or not route_options().has(destination): return false
	room=destination
	enter_room()
	return true

func descend() -> bool:
	if state!="route" or room_kind!="boss" or not dungeon.nodes[room].cleared or floor_index>=FLOOR_COUNT-1: return false
	floor_index += 1
	room=0
	dungeon.generate(seed_value,floor_index)
	enter_room()
	return true

func service_action(choice: int) -> bool:
	if state!="service" or service_used: return false
	match room_kind:
		"treasure","secret":
			if choice==0:
				make_offers(true)
				state="reward"
				return true
			elif choice==1:
				var items: Array = ["heal","time","bomb","shield","dice","mirror"]
				active_item=items[rng.randi_range(0,items.size()-1)]
				active_timer=0
			else: return false
		"shop":
			if choice==0 and shards>=12:
				shards-=12
				make_offers(true)
				state="reward"
				return true
			elif choice==1 and shards>=8:
				shards-=8
				hp=minf(max_hp,hp+25)
			elif choice==2 and shards>=10:
				shards-=10
				var items: Array = ["time","bomb","shield","dice","mirror"]
				active_item=items[rng.randi_range(0,items.size()-1)]
				active_timer=0
			elif choice!=3: return false
		"event":
			if choice==0:
				hp=maxf(1,hp-15)
				make_offers(true)
				state="reward"
				return true
			elif choice==1: shards+=8
			else: return false
		"forge":
			if choice==0 and shards>=16:
				shards-=16
				damage+=12
			elif choice==1 and shards>=16:
				shards-=16
				bolt_damage+=14
				special_cd=maxf(0.2,special_cd*0.90)
			elif choice==2 and shards>=12:
				shards-=12
				armor+=4
			elif choice!=3: return false
		"shrine":
			if choice==0: hp=minf(max_hp,hp+30)
			elif choice==1: max_hp+=10; hp+=10
			else: return false
		_: return false
	finish_room()
	check_shard_contract()
	return true

func finish_room() -> void:
	dungeon.nodes[room].cleared=true
	service_used=true
	# Non-combat services and their relic selections return directly to the map.
	state="route" if room_kind in ["treasure","shop","event","shrine","secret","forge"] else "exit"


func spawn_enemy(kind: String, pos: Vector2) -> void:
	var health := 42.0
	var radius := 17.0
	match kind:
		"caster": health = 35.0
		"brute":
			health = 85.0
			radius = 24.0
		"archer": health=48.0
		"shield": health=95.0; radius=24.0
		"spore": health=50.0
		"vine": health=65.0
		"mushroom": health=100.0; radius=26.0
		"rootking": health=680.0; radius=48.0
		"revenant": health=85.0; radius=19.0
		"hexer": health=75.0; radius=19.0
		"sentinel": health=140.0; radius=27.0
		"hollowking": health=970.0; radius=49.0
		"boss":
			health = 430.0
			radius = 43.0
	health*=1.25+0.25*floor_index
	if kind in ["boss","rootking","hollowking"]: health*=1.25
	pos = safe_position(pos,radius)
	# Move spawn points out of solid pillars before the enemy becomes active.
	if blocked(pos,radius):
		var origin: Vector2=pos
		for step in range(1,9):
			for j in range(8):
				var candidate:=safe_position(origin+Vector2.from_angle(j*TAU/8)*step*36,radius)
				if not blocked(candidate,radius):
					pos=candidate
					break
			if not blocked(pos,radius): break
	enemies.append({"kind":kind,"pos":pos,"hp":health,"max_hp":health,"r":radius,"timer":rng.randf_range(0.6,1.4),"wind":0.0,"flash":0.0,"target":pos,"facing":Vector2.LEFT})

func safe_position(pos: Vector2, radius: float) -> Vector2:
	return Vector2(clampf(pos.x, ARENA.position.x + radius, ARENA.end.x - radius), clampf(pos.y, ARENA.position.y + radius, ARENA.end.y - radius))

func blocked(pos: Vector2, radius: float) -> bool:
	for wall in walls:
		if wall.grow(radius).has_point(pos):
			return true
	return false

func move_actor(pos: Vector2, motion: Vector2, radius: float) -> Vector2:
	var x := safe_position(pos + Vector2(motion.x, 0), radius)
	if not blocked(x, radius):
		pos.x = x.x
	var y := safe_position(pos + Vector2(0, motion.y), radius)
	if not blocked(y, radius):
		pos.y = y.y
	return pos

func tick(dt: float, direction: Vector2, target: Vector2, melee: bool, ranged: bool, dash: bool, active: bool = false) -> void:
	if state=="hub":
		if target.distance_to(player)>4: aim=player.direction_to(target)
		if hub_seat_index>=0:
			hub_seat_anim=minf(1.0,hub_seat_anim+dt/0.22)
			if direction.length_squared()<0.01:
				hub_seat_released=true
				return
			if not hub_seat_released:
				return
			# Reposition in front of the same stool, outside of bar collision.
			player=Vector2(float(HUB_STOOL_X[hub_seat_index]),HUB_BAR.end.y+19.0)
			hub_seat_index=-1
			hub_seat_anim=0.0
			reset_hub_stool_hold()
		# Substeps prevent walking through furniture during a frame hitch.
		var motion: Vector2=direction.limit_length()*speed*dt
		var steps: int=maxi(1,ceili(motion.length()/8.0))
		for step in range(steps):
			player=move_actor(player,motion/float(steps),15)
		update_hub_stool(dt,direction)
		return
	if state != "fight" and state != "exit":
		return
	elapsed += dt
	active_timer = maxf(0,active_timer-dt)
	slow_time=maxf(0,slow_time-dt)
	shield_time=maxf(0,shield_time-dt)
	if active and active_timer<=0:
		use_active()
	attack_timer = maxf(0, attack_timer - dt)
	bolt_timer = maxf(0, bolt_timer - dt)
	dash_timer = maxf(0, dash_timer - dt)
	invulnerable = maxf(0, invulnerable - dt)
	dash_left = maxf(0, dash_left - dt)
	if target.distance_to(player) > 4:
		aim = player.direction_to(target)
	if dash and dash_timer <= 0:
		dash_direction = direction.normalized() if direction.length_squared() > 0.01 else aim
		dash_left = 0.17
		dash_hits.clear()
		dash_timer = dash_cooldown
		invulnerable = maxf(invulnerable, 0.28)
		effect("dash", player, 0.22, dash_direction)
		notices.append("dash")
		if forge_id=="void_stride":
			for enemy in enemies:
				if enemy.hp>0 and player.distance_to(enemy.pos)<185:
					hit_enemy(enemy,damage*1.2+16.0)
			effect("forge_stride",player,0.40,Vector2.ZERO)
		if relics.has("nova") or character=="arcanist":
			for enemy in enemies:
				if player.distance_to(enemy.pos)<100: hit_enemy(enemy,15)
				if set_id=="arcanist_frost" and player.distance_to(enemy.pos)<100: enemy["chill"]=2.0
	var move := dash_direction * 860.0 if dash_left > 0 else direction.limit_length() * speed*environment_speed()
	player = move_actor(player, move * dt, 15)
	if state == "exit":
		update_effects(dt)
		return
	if dash_left>0 and relics.has("dashfire"):
		for enemy in enemies:
			if not dash_hits.has(enemy) and player.distance_to(enemy.pos)<65:
				dash_hits.append(enemy)
				hit_enemy(enemy,14.0*relics.count("dashfire"))
	if melee and attack_timer <= 0:
		attack_timer = attack_cd
		attacks += 1
		shot_style=set_id+"_main"
		notices.append(shot_style)
		if character in ["dealer","arcanist"] or set_id=="soulbinder_wraith":
			shoot(player+aim*21,aim*660,false,damage)
		else:
			effect("slash_"+set_id, player, 0.23, aim)
			for enemy in enemies:
				var offset: Vector2 = enemy.pos-player
				if offset.length() <= (118.0 if set_id in ["warden_executioner","soulbinder_reaper"] else 93.0)+float(enemy.r) and (offset.length()<12 or aim.dot(offset.normalized())>0.15):
					hit_enemy(enemy,damage)
		if forge_id=="rift_echo" and echoes.size()<12:
			echoes.append({"pos":player,"dir":aim,"time":0.19,"damage":damage*0.85})
			effect("forge_echo",player,0.34,aim)
		if relics.has("twin") and echoes.size()<12:
			echoes.append({"pos":player,"dir":aim,"time":0.28,"damage":damage*0.55})
		if relics.has("echo") and attacks%3==0:
			shoot(player+aim*21,aim*590,false,bolt_damage*relics.count("echo"))
	if ranged and bolt_timer <= 0:
		bolt_timer = special_cd
		specials+=1
		advance_contract("spellweaver")
		shot_style=set_id+"_special"
		notices.append(shot_style)
		effect("special_"+set_id,player,0.42,aim)
		special_multiplier=2.0 if relics.has("overcharge") and specials%4==0 else 1.0
		if character=="soulbinder":
			var spent: int=mini(3,souls)
			souls-=spent
			if set_id=="soulbinder_reaper":
				for enemy in enemies:
					if player.distance_to(enemy.pos)<190: hit_enemy(enemy,bolt_damage*(1.0+spent*0.4)*special_multiplier)
				hp=minf(max_hp,hp+spent*2)
				effect("burst",player,0.45,Vector2.ZERO)
			else:
				spirit_pos=player+aim*45
				spirit_life=4.0+spent*1.5
				spirit_timer=0
				for i in range(3+spent): shoot(player+aim*21,aim.rotated((i-1.0)*0.19)*560,false,bolt_damage*special_multiplier)
		elif set_id=="arcanist_frost":
			for enemy in enemies:
				if player.distance_to(enemy.pos)<185:
					hit_enemy(enemy,bolt_damage*special_multiplier)
					enemy["chill"]=4.0
			effect("burst",player,0.4,Vector2.ZERO)
		elif set_id=="warden_executioner":
			effect("slash_"+set_id,player,0.3,aim)
			for enemy in enemies:
				if player.distance_to(enemy.pos)<175: hit_enemy(enemy,bolt_damage*2.0*special_multiplier)
			for i in range(mini(8,2*relics.count("split"))):
				shoot(player+aim*21,aim.rotated((float(i)-0.5)*0.25)*590,false,bolt_damage*special_multiplier)
		else:
			var count := mini(9,(3 if character=="dealer" else 1)+2*relics.count("split"))
			for i in range(count):
				var angle := (float(i)-(count-1)*0.5)*0.19
				var power := bolt_damage*special_multiplier
				if set_id=="dealer_joker": power *= rng.randf_range(0.6,1.35)
				shoot(player+aim*21,aim.rotated(angle)*590,false,power)
		if forge_id=="stormheart":
			for j in range(5):
				var arc: float=(float(j)-2.0)*0.31
				shoot(player+aim*21,aim.rotated(arc)*620,false,bolt_damage*0.80*special_multiplier)
			effect("forge_storm",player,0.58,aim)
		if relics.has("cascade") or set_id=="arcanist_fire":
			for enemy in enemies:
				if player.distance_to(enemy.pos)<200: hit_enemy(enemy,bolt_damage*0.8*special_multiplier)
			effect("burst",player,0.35,Vector2.ZERO)
	for i in range(echoes.size()-1,-1,-1):
		echoes[i].time-=dt
		if echoes[i].time<=0:
			var echo: Dictionary=echoes[i]
			shoot(echo.pos+echo.dir*21,echo.dir*590,false,echo.damage)
			effect("burst",echo.pos,0.16,echo.dir)
			echoes.remove_at(i)
	orbit_timer-=dt
	if relics.has("sun") and orbit_timer<=0:
		orbit_timer=0.25
		for enemy in enemies:
			if player.distance_to(enemy.pos)<95: hit_enemy(enemy,5*relics.count("sun"))
	if spirit_life>0:
		spirit_life-=dt
		spirit_timer-=dt
		if spirit_timer<=0:
			spirit_timer=0.55
			var nearest: Dictionary={}
			var distance:=100000.0
			for enemy in enemies:
				var d: float=spirit_pos.distance_to(enemy.pos)
				if enemy.hp>0 and d<distance:
					distance=d; nearest=enemy
			if not nearest.is_empty():
				spirit_pos=spirit_pos.move_toward(nearest.pos,35)
				shoot(spirit_pos,spirit_pos.direction_to(nearest.pos)*510,false,bolt_damage*0.6)
	update_enemies(dt)
	update_shots(dt)
	if guard_pulse_pending:
		guard_pulse_pending=false
		for enemy in enemies:
			if player.distance_to(enemy.pos)<120: hit_enemy(enemy,18*relics.count("guardpulse"))
	update_environment(dt)
	update_hazards(dt)
	for i in range(enemies.size()-1, -1, -1):
		if enemies[i].hp <= 0:
			effect("death", enemies[i].pos, 0.35, Vector2.ZERO)
			if enemies[i].kind in ["boss","rootking","hollowking"]: notices.append("boss_kill")
			kills += 1
			advance_contract("slayer")
			if character=="soulbinder": souls=mini(6,souls+1)
			if relics.has("harvest"):
				harvest_count+=1
				if harvest_count%5==0: hp=minf(max_hp,hp+8*relics.count("harvest"))
			if relics.has("soulstorm"):
				for j in range(3): shoot(enemies[i].pos,Vector2.from_angle(j*TAU/3+elapsed)*420,false,14*relics.count("soulstorm"))
			if relics.has("bounty") and (enemies[i].get("elite",false) or enemies[i].kind in ["boss","rootking","hollowking"]): shards+=10*relics.count("bounty")
			shards += (20 if enemies[i].kind in ["boss","rootking","hollowking"] else 3)+2*relics.count("greed")
			check_shard_contract()
			if relics.has("shatter") and float(enemies[i].get("chill",0))>0:
				for j in range(6): shoot(enemies[i].pos,Vector2.from_angle(j*TAU/6)*400,false,12)
			if enemies[i].kind=="mushroom":
				spawn_enemy("spore",enemies[i].pos+Vector2(35,0))
				spawn_enemy("spore",enemies[i].pos-Vector2(35,0))
			hp = minf(max_hp,hp+1.0*relics.count("leech"))
			enemies.remove_at(i)
			notices.append("kill")
	update_effects(dt)
	if hp <= 0 and relics.has("phoenix") and not revive_used:
		revive_used=true
		hp=max_hp*0.5
		invulnerable=2.0
	if hp <= 0:
		state = "defeat"
		shots.clear()
	elif enemies.is_empty():
		if wave<wave_total:
			wave+=1
			notices.append("wave")
			shots.clear()
			hazards.clear()
			spawn_reinforcements()
			return
		shots.clear()
		hazards.clear()
		combat_clears+=1
		if contract_clean_room: advance_contract("untouched")
		if room_kind=="boss": bosses_defeated=maxi(bosses_defeated,floor_index+1)
		if room_kind=="boss" and floor_index==FLOOR_COUNT-1:
			dungeon.nodes[room].cleared=true
			state="victory"
		elif room_kind=="boss" or room_kind=="elite" or room==0 or rng.randf()<0.22:
			if room_kind=="elite": elites+=1
			make_offers(room_kind in ["boss","elite"])
			state="reward"
		else:
			finish_room()
		notices.append("clear")

func effect(kind: String, pos: Vector2, lifetime: float, direction: Vector2) -> void:
	if effects.size()>=100: return
	effects.append({"kind":kind,"pos":pos,"time":lifetime,"max":lifetime,"dir":direction})

func update_effects(dt: float) -> void:
	for i in range(effects.size()-1,-1,-1):
		effects[i].time -= dt
		if effects[i].time <= 0:
			effects.remove_at(i)

func hit_enemy(enemy: Dictionary, amount: float) -> void:
	if enemy.hp <= 0:
		return
	if relics.has("crit") and rng.randf()<minf(0.8,0.2*relics.count("crit")): amount *= 2
	if relics.has("hunter") and (enemy.get("elite",false) or enemy.kind in ["boss","rootking","hollowking"]): amount+=8*relics.count("hunter")
	if relics.has("blood") and hp<max_hp*0.35: amount*=1.4
	if relics.has("execution") and enemy.hp<enemy.max_hp*0.25: amount*=1.3
	if relics.has("frostfire") and float(enemy.get("chill",0))>0 and float(enemy.get("burn",0))>0: amount+=8*relics.count("frostfire")
	if relics.has("sacrifice"): amount*=1.0+0.2*relics.count("sacrifice")
	if enemy.kind=="shield" and enemy.pos.direction_to(player).dot(enemy.get("facing",Vector2.LEFT))>0.35: amount*=0.6
	var dealt: float=maxf(1,amount+temp_power)
	enemy.hp -= dealt
	if relics.has("frost") or set_id=="arcanist_frost": enemy["chill"]=2.0
	if relics.has("venom"): enemy["poison"]=3.0
	if relics.has("storm") and float(enemy.get("burn",0))>0:
		for other in enemies:
			if other!=enemy and other.hp>0 and enemy.pos.distance_to(other.pos)<160:
				other.hp -= 8.0*relics.count("storm")
				effect("hit",other.pos,0.18,Vector2.ZERO)
				break
	if relics.has("ember") or set_id=="arcanist_fire": enemy["burn"]=3.0
	enemy.flash = 0.12
	effect("hit", enemy.pos, 0.22, Vector2.ZERO)
	if effects.size()<80: effects.append({"kind":"damage","pos":enemy.pos,"time":0.65,"max":0.65,"dir":Vector2.ZERO,"amount":int(round(dealt))})
	notices.append("hit")

func hurt(amount: float) -> void:
	if invulnerable > 0 or shield_time>0 or hp <= 0:
		return
	if forge_id=="iron_aegis" and forge_guard_charges>0:
		forge_guard_charges-=1
		invulnerable=0.55
		effect("forge_guard",player,0.45,Vector2.ZERO)
		notices.append("forge_guard")
		if forge_guard_charges==0:
			for enemy in enemies:
				if enemy.hp>0 and player.distance_to(enemy.pos)<180:
					hit_enemy(enemy,38.0)
			effect("forge_shatter",player,0.65,Vector2.ZERO)
		return
	contract_clean_room=false
	var scaled: float=amount*(1.0+0.20*floor_index)
	# Armor never negates more than half of an incoming hit.
	hp = maxf(0,hp-maxf(scaled*0.5,scaled-armor))
	if relics.has("guardpulse"): guard_pulse_pending=true
	invulnerable = 0.65+0.15*relics.count("warding")
	effect("hurt", player, 0.25, Vector2.ZERO)
	notices.append("hurt")

func shoot(pos: Vector2, velocity: Vector2, hostile: bool, power: float) -> void:
	if shots.size()>=180: return
	shots.append({"pos":pos,"vel":velocity,"hostile":hostile,"damage":power,"life":2.8,"pierce":0 if hostile else mini(5,relics.count("pierce")),"hit":[],"bounce":0 if hostile else mini(3,relics.count("bounce")),"style":enemy_shot_style if hostile else shot_style})

func update_enemies(dt: float) -> void:
	for enemy in enemies:
		if enemy.hp <= 0:
			continue
		if float(enemy.get("burn",0))>0:
			enemy["burn"] = maxf(0,float(enemy.burn)-dt)
			enemy.hp -= dt*4.0*(relics.count("ember")+(1 if set_id=="arcanist_fire" else 0))
		if float(enemy.get("poison",0))>0:
			enemy["poison"]=maxf(0,float(enemy.poison)-dt)
			enemy.hp-=dt*5*relics.count("venom")
		enemy["chill"]=maxf(0,float(enemy.get("chill",0))-dt)
		enemy.flash = maxf(0, float(enemy.flash)-dt)
		enemy.timer -= dt
		var pos: Vector2 = enemy.pos
		var toward := pos.direction_to(player)
		var dist := pos.distance_to(player)
		var kind: String = enemy.kind
		var radius: float = enemy.r
		if enemy.wind > 0:
			enemy.wind -= dt
			if enemy.wind <= 0:
				enemy_shot_style=kind
				notices.append("enemy_"+kind)
				effect("enemy_"+kind,pos,0.30,pos.direction_to(enemy.target))
				if kind in ["caster","archer","spore","hexer"]:
					shoot(pos, pos.direction_to(enemy.target)*(390 if kind=="archer" else 265), true, 12)
					if kind=="spore": add_hazard(enemy.target,44,0.6,2.8,9)
					if kind=="hexer":
						for j in range(3): shoot(pos,pos.direction_to(enemy.target).rotated((j-1)*0.25)*330,true,13)
				elif kind=="vine":
					if player.distance_to(enemy.target)<75:
						player=move_actor(player,player.direction_to(pos)*100,15)
						hurt(12)
				elif kind in ["boss","rootking","hollowking"]:
					var phase: int = 12 if enemy.hp < enemy.max_hp*0.5 else 8
					for i in range(phase):
						var angle := TAU*float(i)/phase + elapsed*0.3
						shoot(pos, Vector2.from_angle(angle)*290, true, 15)
					if kind=="hollowking":
						for j in range(5 if enemy.hp<enemy.max_hp*0.5 else 3): add_hazard(enemy.target+Vector2.from_angle(j*TAU/5)*85,38,0.7,2.8,19)
						for j in range(3): shoot(pos,pos.direction_to(enemy.target).rotated((j-1)*0.24)*430,true,18)
					if kind=="rootking":
						add_hazard(enemy.target,62,0.8,3.5,18)
						if enemy.hp<enemy.max_hp*0.5:
							for i in range(3): add_hazard(enemy.target+Vector2.from_angle(i*TAU/3)*95,40,1.1,3.0,12)
					if dist < 125:
						hurt(24)
				elif dist < radius + 45:
					hurt(18 if kind in ["brute","shield","mushroom","sentinel"] else 11)
				enemy.timer = 1.8 if kind in ["boss","rootking","hollowking"] else 0.9
			continue
		var velocity := Vector2.ZERO
		if kind in ["caster","archer","spore","hexer"]:
			if dist > 300: velocity = toward*105
			elif dist < 200: velocity = -toward*100
			if enemy.timer <= 0 and dist < 460:
				enemy.wind = 0.55
				enemy.target = player
		elif kind in ["boss","rootking","hollowking"]:
			velocity = toward*78 if dist > 125 else Vector2.ZERO
			if enemy.timer <= 0:
				enemy.wind = 0.8
				enemy.target = player
		elif kind=="vine":
			velocity=toward*100 if dist>180 else Vector2.ZERO
			if enemy.timer<=0 and dist<280:
				enemy.wind=0.65
				enemy.target=player
		else:
			velocity = toward*(82 if kind in ["brute","shield","mushroom","sentinel"] else (190 if kind=="revenant" else 138))
			if dist < radius+40 and enemy.timer <= 0:
				enemy.wind = 0.48 if kind == "brute" else 0.3
				velocity = Vector2.ZERO
		# Simple separation and obstacle steering; no clumping at pillars.
		for other in enemies:
			if other == enemy or other.hp <= 0:
				continue
			var separation: Vector2 = pos - other.pos
			var length := separation.length()
			if length > 0.01 and length < radius + float(other.r) + 8:
				velocity += separation.normalized()*100
		if slow_time>0 or enemy.chill>0: velocity*=0.4
		if kind=="shield" and enemy.wind<=0 and enemy.timer<0.15:
			enemy.facing=toward
		var next := move_actor(pos, velocity*dt, radius)
		if velocity.length_squared() > 1 and next.distance_to(pos) < velocity.length()*dt*0.25:
			next = move_actor(pos, velocity.rotated(PI/2)*dt, radius)
		enemy.pos = next

func add_hazard(pos: Vector2, radius: float, warning: float, life: float, power: float) -> void:
	if hazards.size()>=24: return
	hazards.append({"pos":safe_position(pos,10),"r":radius,"warn":warning,"life":warning+life,"power":power})

func environment_speed() -> float:
	if floor_index!=1 or state!="fight": return 1.0
	for hazard in hazards:
		if hazard.get("type","")=="sporepool" and hazard.warn<=0 and player.distance_to(hazard.pos)<hazard.r: return 0.65
	return 1.0

func spawn_reinforcements() -> void:
	var family: Array=["spore","vine","mushroom"] if floor_index==1 else ["revenant","hexer","sentinel"]
	for i in range(3):
		spawn_enemy(family[i],Vector2(840+i*105,220+i*120))
		enemies[-1].hp*=1.2
		enemies[-1].max_hp=enemies[-1].hp
		enemies[-1]["elite"]=true
	environment_timer=1.5

func update_environment(dt: float) -> void:
	if hazards.size()>=20: return
	environment_timer-=dt
	if environment_timer>0: return
	environment_cycle+=1
	match floor_index:
		0:
			# The vault's pressure plates fire a telegraphed row of spikes.
			environment_timer=4.5
			var x: float=420+(environment_cycle%3)*270
			var cells: Array[Rect2]=spike_cells(x)
			if not cells.is_empty():
				hazards.append({"pos":Vector2(x,385),"r":40,"warn":0.85,"life":1.60,"power":16,"type":"spikes","cells":cells,"phase":"warn"})
				notices.append("spike_warn")
		1:
			# Persistent toxic pools restrict movement, not merely damage.
			environment_timer=3.4
			var pos: Vector2=safe_position(player+Vector2.from_angle(environment_cycle*2.4)*95,48)
			add_hazard(pos,58,0.85,5.0,12)
			hazards[-1]["type"]="sporepool"
			notices.append("sporepool")
		2:
			# Crown seals erupt as spectral lances, never inside tomb geometry.
			environment_timer=3.4
			var vertical: bool=environment_cycle%2==0
			var pos: Vector2=player
			add_royal_seals(pos,vertical,1.15,22)
			if room_kind=="boss" and environment_cycle%3==0:
				add_royal_seals(pos,not vertical,1.45,20)

func add_royal_seals(pos: Vector2, vertical: bool, warning: float, power: float) -> void:
	var lane:=Rect2(pos.x-30,166,60,438) if vertical else Rect2(78,pos.y-30,1124,60)
	var cells: Array[Rect2]=[]
	var count: int=7 if vertical else 18
	for i in range(count):
		var center:=Vector2(pos.x,196+i*60) if vertical else Vector2(110+i*60,pos.y)
		var cell:=Rect2(center-Vector2(27,24),Vector2(54,48))
		if not ARENA.grow(-8).encloses(cell): continue
		var clear:=true
		for wall in walls:
			if wall.grow(18).intersects(cell): clear=false; break
		if clear: cells.append(cell)
	if cells.is_empty(): return
	hazards.append({"pos":pos,"r":40,"warn":warning,"warning":warning,"life":warning+0.84,"power":power,"type":"rune","rect":lane,"cells":cells,"phase":"warn"})
	notices.append("rune")

func spike_cells(x: float) -> Array[Rect2]:
	var cells: Array[Rect2]=[]
	for y in range(180,584,32):
		var cell:=Rect2(x-26,y,52,28)
		var clear:=true
		for wall in walls:
			if wall.grow(12).intersects(cell): clear=false
		if clear: cells.append(cell)
	return cells

func update_hazards(dt: float) -> void:
	for i in range(hazards.size()-1,-1,-1):
		var hazard: Dictionary=hazards[i]
		hazard.warn-=dt
		hazard.life-=dt
		if hazard.life<=0:
			hazards.remove_at(i)
		elif hazard.get("type","")=="spikes":
			if hazard.warn<=0 and hazard.phase=="warn":
				hazard.phase="rise"
				notices.append("spike_rise")
			if hazard.life<0.20 and hazard.phase=="rise":
				hazard.phase="retract"
				notices.append("spike_retract")
			if hazard.warn<=-0.10 and hazard.life>=0.20:
				for cell in hazard.cells:
					if cell.has_point(player):
						hurt(hazard.power)
						break
		elif hazard.get("type","")=="rune":
			if hazard.warn<=0 and hazard.phase=="warn":
				hazard.phase="rise"
				notices.append("rune_strike")
			if hazard.life<0.24 and hazard.phase=="rise":
				hazard.phase="retract"
				notices.append("rune_fade")
			if hazard.warn<=-0.12 and hazard.life>=0.24:
				for cell in hazard.cells:
					if cell.has_point(player):
						hurt(hazard.power)
						break
		elif hazard.warn<=0:
			var inside: bool=hazard.rect.has_point(player) if hazard.has("rect") else player.distance_to(hazard.pos)<hazard.r
			if inside: hurt(hazard.power)

func update_shots(dt: float) -> void:
	for i in range(shots.size()-1,-1,-1):
		var shot: Dictionary = shots[i]
		shot.pos += shot.vel*dt*(0.4 if shot.hostile and slow_time>0 else 1.0)
		shot.life -= dt
		var pos: Vector2 = shot.pos
		if not ARENA.has_point(pos) and shot.bounce>0:
			if pos.x<ARENA.position.x or pos.x>ARENA.end.x: shot.vel.x=-shot.vel.x
			if pos.y<ARENA.position.y or pos.y>ARENA.end.y: shot.vel.y=-shot.vel.y
			shot.pos=safe_position(pos,5)
			pos=shot.pos
			shot.bounce-=1
		var remove: bool = shot.life <= 0 or not ARENA.has_point(pos) or blocked(pos, 4)
		if not remove:
			if shot.hostile:
				if pos.distance_to(player) < 22:
					hurt(shot.damage)
					remove = true
			else:
				for enemy in enemies:
					if enemy.hp > 0 and not shot.hit.has(enemy) and pos.distance_to(enemy.pos) < float(enemy.r)+7:
						hit_enemy(enemy, shot.damage)
						shot.hit.append(enemy)
						remove = shot.pierce <= 0
						shot.pierce -= 1
						break
		if remove:
			shots.remove_at(i)

func make_offers(premium: bool = false) -> void:
	offers.clear()
	var pool: Array[String] = []
	for item in RELICS:
		pool.append(item)
	for i in range(3):
		var index := rng.randi_range(0,pool.size()-1)
		for attempt in range(5):
			var rarity: int = Content.rarity(pool[index])
			if rng.randf() < (0.95 if rarity==0 else (0.75 if premium else 0.4)/rarity): break
			index=rng.randi_range(0,pool.size()-1)
		offers.append(pool[index])
		pool.remove_at(index)

func choose_relic(index: int) -> bool:
	if state != "reward" or index < 0 or index >= offers.size():
		return false
	var item: String = offers[index]
	relics.append(item)
	match item:
		"blade": damage += 9
		"heart":
			max_hp += 20
			hp = minf(max_hp, hp+35)
		"boots": speed += 32
		"focus": bolt_damage += 11
		"ward": armor += 4
		"fury": dash_cooldown = maxf(0.55, dash_cooldown-0.23)
		"tempo": attack_cd = maxf(0.12,attack_cd*0.88)
		"potion": active_heal += 8
		"sanguine": damage+=6; max_hp=maxf(20,max_hp-8); hp=minf(hp,max_hp)
		"bulwark": armor+=3
		"swift": speed+=18
		"mana": special_cd=maxf(0.2,special_cd*0.85)
		"titan": max_hp+=30; hp+=30
		"needle": bolt_damage+=5
		"charge": dash_cooldown=maxf(0.35,dash_cooldown-0.15)
		"battery": active_cd=maxf(20,active_cd-3)
		"glass": damage+=16; armor-=3
	hp = minf(max_hp, hp+(5.0 if floor_index==0 else 3.0))
	finish_room()
	return true

func next_room() -> bool:
	return open_route()

func use_active() -> void:
	if active_item=="heal":
		if hp>=max_hp: return
		hp=minf(max_hp,hp+active_heal)
	elif active_item=="time": slow_time=5.0
	elif active_item=="bomb":
		for enemy in enemies:
			if player.distance_to(enemy.pos)<220: hit_enemy(enemy,75)
	elif active_item=="shield": shield_time=4.0
	elif active_item=="dice":
		temp_power=rng.randf_range(5,18)
	elif active_item=="mirror":
		for shot in shots:
			if shot.hostile:
				shot.hostile=false
				shot.vel=-shot.vel
	active_timer=active_cd
	notices.append(active_item)
	effect("active_"+active_item,player,0.55,aim)

func hub_perks_smoke() -> bool:
	# Each forge must materially affect a real combat action; choices are one-shot.
	for forge in FORGE_DATA:
		var found:=false
		for seed in range(101,125):
			begin(seed)
			if forge_offers.has(forge):
				found=true
				break
		if not found or not select_hub_forge(forge) or select_hub_forge(forge): return false
		if room!=0 or visited_count!=0 or dungeon.nodes[0].visited: return false
		player=HUB_PORTAL
		if not enter_first_room() or forge_id!=forge: return false
		if select_hub_forge(forge): return false
		match forge:
			"rift_echo":
				tick(0.016,Vector2.ZERO,Vector2(700,385),true,false,false)
				if echoes.is_empty() or special_cd<=float(Content.SETS[set_id].special): return false
			"stormheart":
				if max_hp>=float(Content.SETS[set_id].hp): return false
				tick(0.016,Vector2.ZERO,Vector2(700,385),false,true,false)
				if shots.size()<5: return false
			"void_stride":
				if dash_cooldown>=float(Content.SETS[set_id].dash) or max_hp>=float(Content.SETS[set_id].hp): return false
				tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,true)
				var detected:=false
				for fx in effects:
					if fx.kind=="forge_stride": detected=true
				if not detected: return false
			"iron_aegis":
				if forge_guard_charges!=2 or speed>=float(Content.SETS[set_id].speed): return false
				invulnerable=0
				var starting_hp: float=hp
				hurt(20)
				if forge_guard_charges!=1 or hp!=starting_hp: return false
				invulnerable=0
				hurt(20)
				if forge_guard_charges!=0 or hp!=starting_hp: return false
				invulnerable=0
				hurt(20)
				if hp>=starting_hp or contract_clean_room: return false
	for contract in CONTRACT_DATA:
		var found:=false
		for seed in range(201,225):
			begin(seed)
			if contract_offers.has(contract):
				found=true
				break
		if not found or not select_hub_contract(contract) or select_hub_contract(contract): return false
		player=HUB_PORTAL
		if not enter_first_room() or select_hub_contract(contract): return false
		match contract:
			"slayer": advance_contract("slayer",12)
			"untouched": advance_contract("untouched",2)
			"hoarder":
				shards=30
				check_shard_contract()
			"spellweaver": advance_contract("spellweaver",12)
		if not contract_completed or relics.size()!=1: return false
		if contract_progress!=int(CONTRACT_DATA[contract]["goal"]): return false
		advance_contract(contract)
		if relics.size()!=1: return false
	begin(912)
	if not forge_id.is_empty() or not contract_id.is_empty() or contract_completed or not relics.is_empty(): return false
	if forge_offers.size()!=3 or contract_offers.size()!=3: return false
	return true

func smoke_test() -> bool:
	if not dungeon.smoke_test(): return false
	if not hub_perks_smoke(): return false
	begin(123,"dealer_cards")
	if state!="hub" or visited_count!=0 or dungeon.nodes[0].visited or not enemies.is_empty(): return false
	if walls.size()!=6 or not blocked(HUB_BAR.get_center(),15) or not blocked(HUB_FORGE.get_center(),15) or not blocked(HUB_TABLE.get_center(),15) or not blocked(HUB_PORTAL,15): return false
	if not blocked(HUB_CARD_GUEST_LEFT.get_center(),15) or not blocked(HUB_CARD_GUEST_RIGHT.get_center(),15): return false
	for seat in range(HUB_STOOL_X.size()):
		player=Vector2(float(HUB_STOOL_X[seat]),344)
		reset_hub_stool_hold()
		for tick_index in range(59):
			tick(1.0/60.0,Vector2.UP,HUB_BARTENDER_POINT,false,false,false)
		if hub_seat_index!=-1: return false
		for tick_index in range(4):
			tick(1.0/60.0,Vector2.UP,HUB_BARTENDER_POINT,false,false,false)
		if hub_seat_index!=seat or hub_seat_released: return false
		tick(0.25,Vector2.UP,HUB_BARTENDER_POINT,false,false,false)
		if hub_seat_index!=seat or hub_seat_anim<0.99: return false
		tick(0.016,Vector2.ZERO,HUB_BARTENDER_POINT,false,false,false)
		tick(0.016,Vector2.DOWN,HUB_BARTENDER_POINT,false,false,false)
		if hub_seat_index!=-1 or hub_seat_target!=-1: return false
	player=HUB_SPAWN
	tick(0.25,Vector2.UP,HUB_BARTENDER_POINT,false,false,false)
	if hub_seat_hold>0: return false
	if enter_first_room() or hub_bartender_available() or hub_forge_available(): return false
	player=Vector2(482,370)
	tick(0.7,Vector2.UP,HUB_BARTENDER_POINT,false,false,false)
	if player.y < HUB_BAR.end.y+15 or not hub_bartender_available(): return false
	player=Vector2(790,370)
	tick(0.7,Vector2.UP,HUB_FORGE_POINT,false,false,false)
	if player.y < HUB_FORGE.end.y+15 or not hub_forge_available(): return false
	player=Vector2(525,600)
	tick(0.7,Vector2.UP,HUB_TABLE.get_center(),false,false,false)
	if player.y < HUB_TABLE.end.y+15: return false
	player=Vector2(1045,535)
	tick(0.7,Vector2.UP,HUB_PORTAL,false,false,false)
	if player.y < HUB_GATE_FRAME.end.y+15 or not hub_portal_available(): return false
	player=HUB_SPAWN
	if enter_first_room(): return false
	player=HUB_PORTAL
	if not enter_first_room() or state!="fight" or visited_count!=1 or not dungeon.nodes[0].visited: return false
	if not (Content.SETS.dealer_cards.damage==11.0 and Content.SETS.dealer_cards.attack==0.33): return false
	if not (Content.SETS.dealer_joker.damage==12.0 and Content.SETS.dealer_joker.attack==0.41): return false
	relics.append("split")
	relics.append("pierce")
	tick(0.016,Vector2.ZERO,Vector2(600,385),false,true,false)
	if shots.size()!=5 or shots[0].pierce!=1: return false
	hp=30
	tick(0.016,Vector2.ZERO,Vector2(600,385),false,false,false,true)
	if hp!=48 or active_timer<=0: return false
	begin(74219)
	player=HUB_PORTAL
	if not enter_first_room(): return false
	for floor in range(3):
		if dungeon.boss_ready() or dungeon.nodes.size()!=ROOM_COUNT: return false
		for column in range(9):
			var index: int=0 if column==0 else 1+(column-1)*3+1
			if column>0:
				if state=="exit":
					player=EXIT
					if not open_route(): return false
				if not travel(index): return false
			if state=="fight":
				for step in range(wave_total):
					for unit in enemies: unit.hp=0
					# Mushroom splits may require a second cleanup tick.
					for retry in range(3):
						if state!="fight": break
						for unit in enemies: unit.hp=0
						tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
						if wave>step+1: break
				if state=="reward":
					if not choose_relic(0): return false
				if state!="exit": return false
			elif state=="service":
				var choice: int=3 if room_kind in ["shop","forge"] else 1 if room_kind in ["event","treasure","secret"] else 0
				if not service_action(choice) or state!="route" or service_action(choice): return false
		if not dungeon.boss_ready(): return false
		if state=="exit":
			player=EXIT
			if not open_route(): return false
		if not travel(Dungeon.BOSS): return false
		for unit in enemies: unit.hp=0
		tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
		if floor<2:
			if not choose_relic(0): return false
			player=EXIT
			if not next_room() or not descend(): return false
		elif state!="victory" or bosses_defeated!=3: return false
	begin(99,"soulbinder_reaper")
	player=HUB_PORTAL
	if not enter_first_room(): return false
	souls=3
	hp=50
	tick(0.016,Vector2.ZERO,Vector2(700,385),false,true,false)
	if souls!=0 or hp!=56: return false
	environment_timer=0
	update_environment(0.1)
	if hazards.is_empty() or hazards[-1].get("type")!="spikes": return false
	var trap: Dictionary=hazards[-1]
	for cell in trap.cells:
		for wall in walls:
			if wall.grow(12).intersects(cell): return false
	player=trap.cells[0].get_center()
	hp=100
	invulnerable=0
	update_hazards(0.5)
	if hp!=100: return false
	update_hazards(0.5)
	if hp!=84 or not notices.has("spike_rise"): return false
	invulnerable=0
	update_hazards(0.45)
	if hp!=84 or not notices.has("spike_retract"): return false
	for id in Content.SETS:
		begin(321,id)
		player=HUB_PORTAL
		if not enter_first_room(): return false
		tick(0.016,Vector2.ZERO,Vector2(700,385),true,true,false)
		if not notices.has(id+"_main") or not notices.has(id+"_special"): return false
	floor_index=1
	hazards.clear()
	environment_timer=0
	update_environment(0.1)
	if hazards[-1].get("type")!="sporepool": return false
	floor_index=2
	hazards.clear()
	environment_timer=0
	update_environment(0.1)
	if hazards.is_empty() or hazards[-1].get("type")!="rune": return false
	var royal: Dictionary=hazards[-1]
	for cell in royal.cells:
		for wall in walls:
			if wall.grow(18).intersects(cell): return false
	player=royal.cells[0].get_center()
	hp=100
	invulnerable=0
	update_hazards(0.6)
	if hp!=100: return false
	update_hazards(0.8)
	if not notices.has("rune_strike") or hp>=100: return false
	var after: float=hp
	invulnerable=0
	update_hazards(0.42)
	return hp==after and notices.has("rune_fade")
