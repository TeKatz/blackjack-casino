extends RefCounted
## Pure run state. Fixed-tick simulation, seeded room generation and combat.
const ARENA := Rect2(58, 146, 1164, 478)
const SPAWN := Vector2(143, 385)
const EXIT := Vector2(1174, 385)
const ROOM_COUNT := 5
const RELICS := ["blade", "heart", "boots", "focus", "ward", "fury"]
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

func begin(value: int) -> void:
	seed_value = value
	rng.seed = value
	room = 0
	hp = 100.0
	max_hp = 100.0
	damage = 26.0
	speed = 245.0
	armor = 0.0
	bolt_damage = 17.0
	dash_cooldown = 1.3
	kills = 0
	shards = 0
	elapsed = 0.0
	relics.clear()
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
	offers.clear()
	walls.clear()
	# Keep the central passage and entrance/exit unobstructed.
	if room < ROOM_COUNT - 1:
		for x in [490.0, 790.0]:
			var y: float = 220.0 if rng.randf() < 0.5 else 490.0
			walls.append(Rect2(x + rng.randf_range(-55, 55), y, 84, 62))
		var count := 3 + room
		for i in range(count):
			var kind := "stalker" if i % 3 != 2 else "caster"
			if room > 0 and i % 4 == 1:
				kind = "brute"
			spawn_enemy(kind, Vector2(520 + (i % 3) * 235, 195 + (i / 3) * 135 + rng.randf_range(-18, 18)))
	else:
		spawn_enemy("boss", Vector2(925, 385))
		spawn_enemy("caster", Vector2(785, 230))
		spawn_enemy("stalker", Vector2(785, 540))

func spawn_enemy(kind: String, pos: Vector2) -> void:
	var health := 42.0
	var radius := 17.0
	match kind:
		"caster": health = 35.0
		"brute":
			health = 85.0
			radius = 24.0
		"boss":
			health = 430.0
			radius = 43.0
	enemies.append({"kind":kind,"pos":pos,"hp":health,"max_hp":health,"r":radius,"timer":rng.randf_range(0.6,1.4),"wind":0.0,"flash":0.0,"target":pos})

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

func tick(dt: float, direction: Vector2, target: Vector2, melee: bool, ranged: bool, dash: bool) -> void:
	if state != "fight" and state != "exit":
		return
	elapsed += dt
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
		dash_timer = dash_cooldown
		invulnerable = maxf(invulnerable, 0.28)
		effect("dash", player, 0.22, aim)
	var move := dash_direction * 860.0 if dash_left > 0 else direction.limit_length() * speed
	player = move_actor(player, move * dt, 15)
	if state == "exit":
		update_effects(dt)
		return
	if melee and attack_timer <= 0:
		attack_timer = 0.36
		effect("slash", player, 0.19, aim)
		notices.append("swing")
		for enemy in enemies:
			var offset: Vector2 = enemy.pos - player
			if offset.length() <= 93.0 + float(enemy.r) and (offset.length() < 12 or aim.dot(offset.normalized()) > 0.15):
				hit_enemy(enemy, damage)
	if ranged and bolt_timer <= 0:
		bolt_timer = 0.62
		shoot(player + aim * 21, aim * 590, false, bolt_damage)
		notices.append("bolt")
	update_enemies(dt)
	update_shots(dt)
	for i in range(enemies.size()-1, -1, -1):
		if enemies[i].hp <= 0:
			effect("burst", enemies[i].pos, 0.35, Vector2.ZERO)
			kills += 1
			shards += 3 if enemies[i].kind != "boss" else 20
			enemies.remove_at(i)
			notices.append("kill")
	update_effects(dt)
	if hp <= 0:
		state = "defeat"
		shots.clear()
	elif enemies.is_empty():
		shots.clear()
		if room == ROOM_COUNT - 1:
			state = "victory"
		else:
			make_offers()
			state = "reward"
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
	enemy.hp -= amount
	enemy.flash = 0.12
	effect("hit", enemy.pos, 0.18, Vector2.ZERO)
	notices.append("hit")

func hurt(amount: float) -> void:
	if invulnerable > 0 or hp <= 0:
		return
	hp = maxf(0, hp - maxf(1, amount - armor))
	invulnerable = 0.65
	effect("hurt", player, 0.25, Vector2.ZERO)
	notices.append("hurt")

func shoot(pos: Vector2, velocity: Vector2, hostile: bool, power: float) -> void:
	shots.append({"pos":pos,"vel":velocity,"hostile":hostile,"damage":power,"life":2.8})

func update_enemies(dt: float) -> void:
	for enemy in enemies:
		if enemy.hp <= 0:
			continue
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
				if kind == "caster":
					shoot(pos, pos.direction_to(enemy.target)*265, true, 12)
				elif kind == "boss":
					var phase: int = 12 if enemy.hp < enemy.max_hp*0.5 else 8
					for i in range(phase):
						var angle := TAU*float(i)/phase + elapsed*0.3
						shoot(pos, Vector2.from_angle(angle)*290, true, 15)
					if dist < 125:
						hurt(24)
				elif dist < radius + 45:
					hurt(18 if kind == "brute" else 11)
				enemy.timer = 1.8 if kind == "boss" else 0.9
			continue
		var velocity := Vector2.ZERO
		if kind == "caster":
			if dist > 300: velocity = toward*105
			elif dist < 200: velocity = -toward*100
			if enemy.timer <= 0 and dist < 460:
				enemy.wind = 0.55
				enemy.target = player
		elif kind == "boss":
			velocity = toward*78 if dist > 125 else Vector2.ZERO
			if enemy.timer <= 0:
				enemy.wind = 0.8
				enemy.target = player
		else:
			velocity = toward*(82 if kind == "brute" else 138)
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
		var next := move_actor(pos, velocity*dt, radius)
		if velocity.length_squared() > 1 and next.distance_to(pos) < velocity.length()*dt*0.25:
			next = move_actor(pos, velocity.rotated(PI/2)*dt, radius)
		enemy.pos = next

func update_shots(dt: float) -> void:
	for i in range(shots.size()-1,-1,-1):
		var shot: Dictionary = shots[i]
		shot.pos += shot.vel*dt
		shot.life -= dt
		var pos: Vector2 = shot.pos
		var remove: bool = shot.life <= 0 or not ARENA.has_point(pos) or blocked(pos, 4)
		if not remove:
			if shot.hostile:
				if pos.distance_to(player) < 22:
					hurt(shot.damage)
					remove = true
			else:
				for enemy in enemies:
					if enemy.hp > 0 and pos.distance_to(enemy.pos) < float(enemy.r)+7:
						hit_enemy(enemy, shot.damage)
						remove = true
						break
		if remove:
			shots.remove_at(i)

func make_offers() -> void:
	offers.clear()
	var pool: Array[String] = []
	for item in RELICS:
		pool.append(item)
	for i in range(3):
		var index := rng.randi_range(0,pool.size()-1)
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
	hp = minf(max_hp, hp+12)
	state = "exit"
	return true

func next_room() -> bool:
	if state != "exit" or player.distance_to(EXIT) > 75 or room >= ROOM_COUNT-1:
		return false
	room += 1
	enter_room()
	return true

func smoke_test() -> bool:
	begin(74219)
	if enemies.size() != 3 or state != "fight":
		return false
	var start := player
	tick(0.1, Vector2.RIGHT, Vector2(600,385), false, false, false)
	if player.x <= start.x:
		return false
	var enemy: Dictionary = enemies[0]
	var before: float = enemy.hp
	hit_enemy(enemy, 10)
	if enemy.hp != before-10:
		return false
	for step in range(ROOM_COUNT):
		for unit in enemies:
			unit.hp = 0
		tick(0.016, Vector2.ZERO, Vector2(700,385), false, false, false)
		if step == ROOM_COUNT-1:
			return state == "victory" and kills > 0
		if state != "reward" or offers.size()!=3 or not choose_relic(0):
			return false
		if choose_relic(0) or next_room():
			return false
		player = EXIT
		if not next_room():
			return false
	return false
