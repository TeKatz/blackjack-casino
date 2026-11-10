extends RefCounted
## Pure run state. Fixed-tick simulation, seeded room generation and combat.
const ARENA := Rect2(58, 146, 1164, 478)
const SPAWN := Vector2(143, 385)
const EXIT := Vector2(1174, 385)
const ROOM_COUNT := 10
const FLOOR_COUNT := 3
var bosses_defeated := 0
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
var state := "fight"
var player := SPAWN
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

func begin(value: int, loadout: String = "warden_guard", upgrades: Dictionary = {}) -> void:
	seed_value = value
	rng.seed = value
	room = 0
	floor_index = 0
	visited_count = 0
	elites = 0
	boss_defeated = false
	bosses_defeated=0
	souls=0
	specials=0
	harvest_count=0
	guard_pulse_pending=false
	special_multiplier=1.0
	spirit_life=0
	active_item = "heal"
	active_cd = 24.0
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
	bolt_damage = spec.bolt
	dash_cooldown = spec.dash
	attack_cd = spec.attack
	special_cd = spec.special
	active_heal = 25.0+5*int(upgrades.get("alchemy",0))
	active_timer = 0
	attacks = 0
	settled = false
	dash_hits.clear()
	kills = 0
	shards = 0
	elapsed = 0.0
	relics.clear()
	notices.clear()
	enter_room()

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
	offers.clear()
	walls.clear()
	room_kind = dungeon.kind(room)
	temp_power=0
	if relics.has("curse"): temp_power=rng.randf_range(-5,15)
	
	var node: Dictionary = dungeon.nodes[room]
	if not node.visited:
		node.visited = true
		visited_count += 1
		if relics.has("medic"): hp=minf(max_hp,hp+6*relics.count("medic"))
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
		var count := 3 + floor_index + (2 if room_kind=="elite" else room%3)
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
	if room_kind=="boss": return
	var geometry:=RandomNumberGenerator.new()
	geometry.seed=int(node.geometry_seed)
	var layout: int=node.layout
	if room_kind in ["treasure","secret","shrine"]:
		walls.append(Rect2(590,175,100,58))
		return
	if room_kind in ["shop","event","forge"]:
		walls.append(Rect2(560,174,160,48))
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
	return dungeon.available(room)

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
				hp=minf(max_hp,hp+35)
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
			if choice==0: hp=minf(max_hp,hp+40)
			elif choice==1: max_hp+=10; hp+=10
			else: return false
		_: return false
	finish_room()
	return true

func finish_room() -> void:
	dungeon.nodes[room].cleared=true
	service_used=true
	# Non-combat services and their relic selections return directly to the map.
	state="route" if room_kind in ["treasure","shop","event","shrine","secret"] else "exit"


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
	pos = safe_position(pos,radius)
	# Move spawn points out of solid pillars before the enemy becomes active.
	for wall in walls:
		if wall.grow(radius).has_point(pos):
			pos = safe_position(Vector2(pos.x, wall.position.y - radius - 10),radius)
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
		if relics.has("nova") or character=="arcanist":
			for enemy in enemies:
				if player.distance_to(enemy.pos)<100: hit_enemy(enemy,15)
				if set_id=="arcanist_frost" and player.distance_to(enemy.pos)<100: enemy["chill"]=2.0
	var move := dash_direction * 860.0 if dash_left > 0 else direction.limit_length() * speed
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
		if character in ["dealer","arcanist"] or set_id=="soulbinder_wraith":
			shoot(player+aim*21,aim*660,false,damage)
			notices.append("bolt")
		else:
			effect("slash", player, 0.19, aim)
			notices.append("swing")
			for enemy in enemies:
				var offset: Vector2 = enemy.pos-player
				if offset.length() <= (118.0 if set_id in ["warden_executioner","soulbinder_reaper"] else 93.0)+float(enemy.r) and (offset.length()<12 or aim.dot(offset.normalized())>0.15):
					hit_enemy(enemy,damage)
		if relics.has("twin") and echoes.size()<12:
			echoes.append({"pos":player,"dir":aim,"time":0.28,"damage":damage*0.55})
		if relics.has("echo") and attacks%3==0:
			shoot(player+aim*21,aim*590,false,bolt_damage*relics.count("echo"))
	if ranged and bolt_timer <= 0:
		bolt_timer = special_cd
		specials+=1
		special_multiplier=2.0 if relics.has("overcharge") and specials%4==0 else 1.0
		if character=="soulbinder":
			var spent: int=mini(3,souls)
			souls-=spent
			if set_id=="soulbinder_reaper":
				for enemy in enemies:
					if player.distance_to(enemy.pos)<190: hit_enemy(enemy,bolt_damage*(1.0+spent*0.4)*special_multiplier)
				hp=minf(max_hp,hp+spent*3)
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
			effect("slash",player,0.3,aim)
			for enemy in enemies:
				if player.distance_to(enemy.pos)<175: hit_enemy(enemy,bolt_damage*2.0*special_multiplier)
			for i in range(mini(8,2*relics.count("split"))):
				shoot(player+aim*21,aim.rotated((float(i)-0.5)*0.25)*590,false,bolt_damage)
			notices.append("swing")
		else:
			var count := mini(9,(3 if character=="dealer" else 1)+2*relics.count("split"))
			for i in range(count):
				var angle := (float(i)-(count-1)*0.5)*0.19
				var power := bolt_damage*special_multiplier
				if set_id=="dealer_joker": power *= rng.randf_range(0.5,1.8)
				shoot(player+aim*21,aim.rotated(angle)*590,false,power)
			notices.append("bolt")
		if relics.has("cascade") or set_id=="arcanist_fire":
			for enemy in enemies:
				if player.distance_to(enemy.pos)<200: hit_enemy(enemy,bolt_damage*0.8)
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
	update_hazards(dt)
	for i in range(enemies.size()-1, -1, -1):
		if enemies[i].hp <= 0:
			effect("burst", enemies[i].pos, 0.35, Vector2.ZERO)
			kills += 1
			if character=="soulbinder": souls=mini(6,souls+1)
			if relics.has("harvest"):
				harvest_count+=1
				if harvest_count%5==0: hp=minf(max_hp,hp+8*relics.count("harvest"))
			if relics.has("soulstorm"):
				for j in range(3): shoot(enemies[i].pos,Vector2.from_angle(j*TAU/3+elapsed)*420,false,14*relics.count("soulstorm"))
			if relics.has("bounty") and (enemies[i].get("elite",false) or enemies[i].kind in ["boss","rootking","hollowking"]): shards+=10*relics.count("bounty")
			shards += (20 if enemies[i].kind in ["boss","rootking","hollowking"] else 3)+2*relics.count("greed")
			if relics.has("shatter") and float(enemies[i].get("chill",0))>0:
				for j in range(6): shoot(enemies[i].pos,Vector2.from_angle(j*TAU/6)*400,false,12)
			if enemies[i].kind=="mushroom":
				spawn_enemy("spore",enemies[i].pos+Vector2(35,0))
				spawn_enemy("spore",enemies[i].pos-Vector2(35,0))
			hp = minf(max_hp,hp+2.0*relics.count("leech"))
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
		shots.clear()
		hazards.clear()
		if room_kind=="boss": bosses_defeated=maxi(bosses_defeated,floor_index+1)
		if room_kind=="boss" and floor_index==FLOOR_COUNT-1:
			dungeon.nodes[room].cleared=true
			state="victory"
		elif room_kind=="boss" or room_kind=="elite" or room==0 or rng.randf()<0.30:
			if room_kind=="elite": elites+=1
			make_offers(room_kind in ["boss","elite"])
			state="reward"
		else:
			finish_room()
		notices.append("clear")

func effect(kind: String, pos: Vector2, lifetime: float, direction: Vector2) -> void:
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
	if relics.has("hunter") and (enemy.get("elite",false) or enemy.kind in ["boss","rootking"]): amount+=8*relics.count("hunter")
	if relics.has("blood") and hp<max_hp*0.35: amount*=1.4
	if relics.has("execution") and enemy.hp<enemy.max_hp*0.25: amount*=1.3
	if relics.has("frostfire") and float(enemy.get("chill",0))>0 and float(enemy.get("burn",0))>0: amount+=8*relics.count("frostfire")
	if relics.has("sacrifice"): amount*=1.0+0.2*relics.count("sacrifice")
	if enemy.kind=="shield" and enemy.pos.direction_to(player).dot(enemy.get("facing",Vector2.LEFT))>0.35: amount*=0.6
	enemy.hp -= maxf(1,amount+temp_power)
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
	effect("hit", enemy.pos, 0.18, Vector2.ZERO)
	notices.append("hit")

func hurt(amount: float) -> void:
	if invulnerable > 0 or shield_time>0 or hp <= 0:
		return
	hp = maxf(0, hp - maxf(1, amount - armor))
	if relics.has("guardpulse"): guard_pulse_pending=true
	invulnerable = 0.65+0.15*relics.count("warding")
	effect("hurt", player, 0.25, Vector2.ZERO)
	notices.append("hurt")

func shoot(pos: Vector2, velocity: Vector2, hostile: bool, power: float) -> void:
	if shots.size()>=180: return
	shots.append({"pos":pos,"vel":velocity,"hostile":hostile,"damage":power,"life":2.8,"pierce":0 if hostile else mini(5,relics.count("pierce")),"hit":[],"bounce":0 if hostile else mini(3,relics.count("bounce"))})

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
				if kind in ["caster","archer","spore","hexer"]:
					shoot(pos, pos.direction_to(enemy.target)*(390 if kind=="archer" else 265), true, 12)
					if kind=="spore": add_hazard(enemy.target,44,0.6,2.8,9)
					if kind=="hexer":
						for j in range(3): shoot(pos,pos.direction_to(enemy.target).rotated((j-1)*0.25)*330,true,13)
				elif kind=="vine":
					if player.distance_to(enemy.target)<75:
						player=move_actor(player,player.direction_to(pos)*100,15)
						hurt(12)
				elif kind in ["boss","rootking"]:
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
				enemy.timer = 1.8 if kind in ["boss","rootking"] else 0.9
			continue
		var velocity := Vector2.ZERO
		if kind in ["caster","archer","spore"]:
			if dist > 300: velocity = toward*105
			elif dist < 200: velocity = -toward*100
			if enemy.timer <= 0 and dist < 460:
				enemy.wind = 0.55
				enemy.target = player
		elif kind in ["boss","rootking"]:
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
			velocity = toward*(82 if kind in ["brute","shield","mushroom"] else 138)
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

func update_hazards(dt: float) -> void:
	for i in range(hazards.size()-1,-1,-1):
		var hazard: Dictionary=hazards[i]
		hazard.warn-=dt
		hazard.life-=dt
		if hazard.life<=0:
			hazards.remove_at(i)
		elif hazard.warn<=0 and player.distance_to(hazard.pos)<hazard.r:
			hurt(hazard.power)

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
		"potion": active_heal += 15
		"sanguine": damage+=6; max_hp=maxf(20,max_hp-8); hp=minf(hp,max_hp)
		"bulwark": armor+=3
		"swift": speed+=18
		"mana": special_cd=maxf(0.2,special_cd*0.85)
		"titan": max_hp+=30; hp+=30
		"needle": bolt_damage+=5
		"charge": dash_cooldown=maxf(0.35,dash_cooldown-0.15)
		"battery": active_cd=maxf(8,active_cd-3)
		"glass": damage+=16; armor-=3
	hp = minf(max_hp, hp+12)
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
	notices.append("relic")

func smoke_test() -> bool:
	begin(123,"dealer_cards")
	relics.append("split")
	relics.append("pierce")
	tick(0.016,Vector2.ZERO,Vector2(600,385),false,true,false)
	if shots.size()!=5 or shots[0].pierce!=1: return false
	hp=30
	tick(0.016,Vector2.ZERO,Vector2(600,385),false,false,false,true)
	if hp!=55 or active_timer<=0: return false
	begin(74219)
	if enemies.size()!=3 or dungeon.nodes.size()!=10: return false
	for unit in enemies: unit.hp=0
	tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
	if state!="reward" or not choose_relic(0): return false
	player=EXIT
	if not next_room() or not travel(2) or state!="service": return false
	if not service_action(0) or not choose_relic(0): return false
	if state!="route" or not travel(0) or state!="route": return false
	if not travel(2) or state!="route" or service_action(1): return false
	# All non-combat services complete directly on the route map.
	for pair in [[4,3],[5,1],[6,0],[8,1]]:
		room=pair[0]
		enter_room()
		if state!="service" or not service_action(pair[1]) or state!="route": return false
		if service_action(pair[1]): return false
	room=9
	enter_room()
	for unit in enemies: unit.hp=0
	tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
	if not choose_relic(0): return false
	player=EXIT
	if not next_room() or not descend() or floor_index!=1 or relics.size()!=3: return false
	room=9
	enter_room()
	for unit in enemies: unit.hp=0
	tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
	if state!="reward" or not choose_relic(0): return false
	player=EXIT
	if not next_room() or not descend() or floor_index!=2 or relics.size()!=4: return false
	room=6
	enter_room()
	shards=16
	var old_damage:=damage
	if room_kind!="forge" or not service_action(0) or state!="route" or damage!=old_damage+12 or shards!=0: return false
	room=9
	enter_room()
	for unit in enemies: unit.hp=0
	tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
	if state!="victory" or bosses_defeated!=3: return false
	begin(99,"soulbinder_reaper")
	souls=3
	hp=50
	tick(0.016,Vector2.ZERO,Vector2(700,385),false,true,false)
	return souls==0 and hp==59
