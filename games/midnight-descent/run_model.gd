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
# All enemy-origin attacks deal 40% more; world hazards retain their own tuning.
const ENEMY_DAMAGE_MULTIPLIER := 1.40
# The starting healing item has a finite tactical budget for each floor.
const HEAL_CHARGES_PER_FLOOR := 2
# Clear telegraphs and positioning, rather than huge enemy-HP inflation.
const ENEMY_PACE_PER_FLOOR := 0.12
const ENEMY_PACE_PER_THREAT := 0.07
var environment_timer := 0.0
var shot_style := ""
var enemy_shot_style := ""
var environment_cycle := 0
var wave := 1
var wave_total := 1
const FLOOR_COUNT := 3
# Every biome keeps the common combat roster and fields two exclusive specialists.
const COMMON_ENEMIES := ["stalker","caster","brute","archer","shield"]
const STAGE_SPECIALISTS := [
	["gearhound","coilgunner"],
	["briarling","sporewitch"],
	["bone_lancer","crypt_oracle"]
]
const STAGE_VETERANS := [
	[],
	["vine","mushroom","spore"],
	["revenant","hexer","sentinel"]
]
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
const RoomLayouts = preload("res://room_layouts.gd")
var dungeon = Dungeon.new()
var floor_index := 0
var visited_count := 0
var room_kind := "fight"
var encounter_id := ""
var encounter_threat := 0
var service_used := false
var boss_defeated := false
var elites := 0
var active_item := "heal"
var heal_charges := HEAL_CHARGES_PER_FLOOR
var active_cd := 24.0
var slow_time := 0.0
var shield_time := 0.0
var temp_power := 0.0
var revive_used := false
var orbit_timer := 0.0
var arcane_wisp_timer := 0.0
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
var player_motion := Vector2.ZERO
var hp := 100.0
var max_hp := 100.0
var damage := 26.0
var speed := 245.0
var armor := 0.0
var resolve_rank := 0
var recovery_rank := 0
var bolt_damage := 17.0
var dash_cooldown := 1.3
var attack_timer := 0.0
var bolt_timer := 0.0
var dash_timer := 0.0
var dash_left := 0.0
var dash_trail_timer := 0.0
var invulnerable := 0.0
var dash_direction := Vector2.RIGHT
var elapsed := 0.0
var kills := 0
var shards := 0
var relics: Array[String] = []
var last_synergy_id := ""
var offers: Array[String] = []
var enemies: Array[Dictionary] = []
var shots: Array[Dictionary] = []
var walls: Array[Rect2] = []
var large_room := false
var room_bounds: Rect2 = ARENA
var room_regions: Array[Rect2] = []
var spawn_point: Vector2 = SPAWN
var exit_point: Vector2 = EXIT
var layout_name := ""
var layout_name_de := ""
var boss_arena: Rect2 = Rect2()
var boss_trigger_x := 0.0
var boss_awake := false
var boss_intro_timer := 0.0
# Dedicated mechanical gate: opening is traversable; closing/closed is solid.
var boss_gate: Rect2=Rect2()
var boss_gate_state := "open"
var boss_gate_time := 0.0
const BOSS_GATE_CLOSE_DURATION := 1.12
const SURVIVAL_DURATION := 30.0
# Per-stage Shard economy, separate from the permanent Boss Souls.
const ORACLE_BASE_COST := 12
const ORACLE_COST_STEP := 8
const MAGE_REACH := 112.0
const MAGE_STOCK := ["heal_charge","special_charge","ward","blessing"]
# Each floor is a bigger financial commitment. A character who hoards all
# of one stage's drops can still choose, but not sweep the entire catalogue.
const MAGE_PRICES := {
	"heal_charge":[35,45,55],
	"special_charge":[50,62,74],
	"ward":[65,78,92],
	"blessing":[80,96,112]
}
const SHARDS_BOSS_KILL := 10
const SHARDS_STANDARD_KILL := 1
var oracle_revealed: Dictionary = {}
var oracle_purchases := 0
var threshold_mage_pos := Vector2.ZERO
var mage_stock_bought: Dictionary = {}
var mage_special_charges := 0
var mage_guard_charges := 0
var mage_special_blessing := false
var flow_map: Dictionary = {}
var flow_origin: Vector2i = Vector2i(-999,-999)
var flow_timer := 0.0
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
# All seven chambers have their own run-local state. No progress persists
# into another room or run; deterministic geometry comes from the room seed.
var special_elapsed := 0.0
var special_timer := 0.0
var special_wave_timer := 0.0
var special_objectives := 0
var special_stage := 0
var special_targets := 0
var special_blast_timer := -1.0
var tempered_relics: Array[String] = []
var archive_discarded := ""
const RITUAL_ANCHORS := [Vector2(635,242),Vector2(885,390),Vector2(635,532)]
const UNSTABLE_CORES := [Vector2(555,265),Vector2(745,390),Vector2(985,515)]

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
	heal_charges = HEAL_CHARGES_PER_FLOOR
	player_motion = Vector2.ZERO
	active_cd = 40.0
	slow_time = 0
	shield_time = 0
	temp_power = 0
	revive_used = false
	orbit_timer=0
	arcane_wisp_timer=0.0
	dungeon.generate(value,0)
	set_id = loadout if Content.SETS.has(loadout) else "warden_guard"
	var spec: Dictionary = Content.SETS[set_id]
	character = spec.character
	max_hp = float(spec.hp)+5*int(upgrades.get("vitality",0))
	hp = max_hp
	damage = float(spec.damage)+2*int(upgrades.get("power",0))
	speed = float(spec.speed)+8*int(upgrades.get("agility",0))
	armor = 0.0
	resolve_rank=clampi(int(upgrades.get("resolve",0)),0,4)
	recovery_rank=clampi(int(upgrades.get("recovery",0)),0,4)
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
	oracle_revealed.clear()
	oracle_purchases=0
	threshold_mage_pos=Vector2.ZERO
	mage_stock_bought.clear()
	mage_special_charges=0
	mage_guard_charges=0
	mage_special_blessing=false
	elapsed = 0.0
	relics.clear()
	tempered_relics.clear()
	archive_discarded=""
	last_synergy_id=""
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
	var previous_synergies: Array[String]=active_synergies()
	var reward_id: String={"slayer":"blade","untouched":"ward","hoarder":"focus","spellweaver":"mana"}[contract_id]
	relics.append(reward_id)
	match reward_id:
		"blade": damage+=9
		"ward": armor+=4
		"focus": bolt_damage+=11
		"mana": special_cd=maxf(0.2,special_cd*0.85)
	notify_new_synergies(previous_synergies)
	notices.append("contract_complete")
	effect("contract_complete",player,1.0,Vector2.ZERO)

func has_synergy(id: String) -> bool:
	if not Content.SYNERGIES.has(id): return false
	for part in Content.SYNERGIES[id]["parts"]:
		if not relics.has(str(part)): return false
	return true

func active_synergies() -> Array[String]:
	var result: Array[String]=[]
	for id in Content.SYNERGIES:
		if has_synergy(id): result.append(str(id))
	return result

func synergy_preview(item: String) -> Dictionary:
	# Preview never mutates state; every card uses this same gameplay truth.
	var strongest: Dictionary={}
	var priority:=0
	for id in Content.SYNERGIES:
		var data: Dictionary=Content.SYNERGIES[id]
		var parts: Array=data["parts"]
		if not parts.has(item): continue
		var active: bool=has_synergy(str(id))
		var filled:=0
		for part in parts:
			if relics.has(str(part)) or (str(part)==item and not relics.has(item)):
				filled+=1
		var state: String="active" if active else ("ready" if filled==parts.size() else "building")
		var rank: int=3 if state=="ready" else (2 if state=="active" else 1)
		if rank>priority:
			priority=rank
			strongest={"id":str(id),"state":state,"owned":filled,"total":parts.size()}
	return strongest

func synergy_candidates() -> Array[String]:
	var result: Array[String]=[]
	for id in Content.SYNERGIES:
		var parts: Array=Content.SYNERGIES[id]["parts"]
		var owned:=0
		var missing: String=""
		for part in parts:
			if relics.has(str(part)): owned+=1
			else: missing=str(part)
		if owned==parts.size()-1 and missing!="" and not result.has(missing):
			result.append(missing)
	return result

func notify_new_synergies(previous: Array[String]) -> void:
	for id in active_synergies():
		if previous.has(id): continue
		last_synergy_id=str(id)
		notices.append("synergy_complete")
		effect("synergy_unlock",player,0.8,Vector2.ZERO)

# Class relic runtime powers. All of these depend on the current run inventory,
# not persistent unlocks; the normal shared relics remain available to everyone.
func class_main_attack() -> void:
	if character=="dealer" and relics.has("d_marked") and attacks%4==0:
		var flush: bool=has_synergy("d_royal")
		var count: int=5 if flush else 3
		for i in range(count):
			var spread: float=(float(i)-float(count-1)*0.5)*0.22
			shoot(player+aim*22,aim.rotated(spread)*610,false,damage*(0.56 if flush else 0.42)*relics.count("d_marked"))
			effect("class_cards",player,0.38,aim)
		notices.append("class_cards")

func class_special_attack() -> void:
	if relics.has("w_lance"):
		for i in range(mini(3,relics.count("w_lance"))):
			var direction: Vector2=aim.rotated((float(i)-float(mini(3,relics.count("w_lance"))-1)*0.5)*0.16)
			shoot(player+aim*25,direction*680,false,bolt_damage*1.25*special_multiplier*(1.0+0.20*maxi(0,relics.count("w_lance")-3)))
			if not shots.is_empty(): shots[-1]["pierce"]=mini(6,int(shots[-1].pierce)+2)
		effect("class_lance",player,0.38,aim)
		notices.append("class_lance")
	if relics.has("d_ace") and specials%5==0:
		var full_house: bool=has_synergy("d_jackpot")
		var count: int=3 if full_house else 1
		for i in range(count):
			shoot(player+aim*26,aim.rotated((float(i)-float(count-1)*0.5)*0.23)*710,false,bolt_damage*1.75*special_multiplier*relics.count("d_ace"))
			effect("class_ace",player,0.54,aim)
		if full_house:
			shards+=5
			check_shard_contract()
		notices.append("class_ace")
	if relics.has("a_frost"):
		for enemy in enemies:
			if enemy.hp>0 and player.distance_to(enemy.pos)<195:
				enemy["chill"]=maxf(float(enemy.get("chill",0)),3.0+0.6*relics.count("a_frost"))
		effect("class_ice",player,0.48,Vector2.ZERO)
		notices.append("class_ice")
	if relics.has("a_surge") and specials%3==0:
		var convergence: bool=has_synergy("a_convergence")
		for enemy in enemies:
			if enemy.hp>0 and player.distance_to(enemy.pos)<(225 if convergence else 165):
				hit_enemy(enemy,(30 if convergence else 18)*special_multiplier*relics.count("a_surge"))
		effect("class_surge",player,0.62,Vector2.ZERO)
		notices.append("class_surge")
	if relics.has("s_grim") and souls>0:
		souls-=1
		var dark_requiem: bool=has_synergy("s_requiem")
		var count: int=3 if dark_requiem else 1
		for i in range(count):
			shoot(player+aim*20,aim.rotated((float(i)-float(count-1)*0.5)*0.19)*570,false,bolt_damage*1.15*special_multiplier*relics.count("s_grim"))
		if dark_requiem: hp=minf(max_hp,hp+6)
		effect("class_soul",player,0.52,aim)
		notices.append("class_soul")

func class_dash_landing() -> void:
	if relics.has("w_charger"):
		for enemy in enemies:
			if enemy.hp>0 and player.distance_to(enemy.pos)<145:
				hit_enemy(enemy,16.0*relics.count("w_charger"))
		if has_synergy("w_phalanx"):
			for i in range(3):
				shoot(player+dash_direction*21,dash_direction.rotated((i-1)*0.25)*640,false,bolt_damage*0.85)
				shots[-1]["pierce"]=2
		effect("class_shield",player,0.54,dash_direction)
		notices.append("class_shield")
	if relics.has("d_decoy"):
		var bluff: bool=has_synergy("d_bluff")
		for enemy in enemies:
			if enemy.hp>0 and player.distance_to(enemy.pos)<(150 if bluff else 98):
				var bonus: float=24 if bluff and float(enemy.get("hex_mark",0))>0 else 0
				hit_enemy(enemy,14*relics.count("d_decoy")+bonus)
				if bluff: enemy["hex_mark"]=maxf(2.5,float(enemy.get("hex_mark",0)))
		effect("class_smoke",player,0.60,Vector2.ZERO)
		notices.append("class_smoke")

func class_enemy_hit(enemy: Dictionary, damage_dealt: float) -> void:
	if relics.has("d_coin") and rng.randf()<minf(0.68,0.17*relics.count("d_coin")):
		enemy["hex_mark"]=3.5
		effect("class_coin",enemy.pos,0.35,Vector2.ZERO)
		notices.append("class_coin")
	if relics.has("a_ember"):
		enemy["burn"]=maxf(float(enemy.get("burn",0)),3.0+0.5*relics.count("a_ember"))
	if has_synergy("a_crystal") and float(enemy.get("chill",0))>0 and float(enemy.get("prism_lock",0))<=0:
		enemy["prism_lock"]=0.85
		for j in range(4):
			var angle: float=float(j)*TAU/4.0+elapsed*0.3
			shoot(enemy.pos,Vector2.from_angle(angle)*500,false,bolt_damage*0.35)
		effect("class_crystal",enemy.pos,0.46,Vector2.ZERO)
		notices.append("class_crystal")

func class_enemy_kill(dead: Dictionary) -> void:
	if relics.has("w_oath") and kills%4==0:
		shield_time=maxf(shield_time,1.6+0.4*relics.count("w_oath"))
		if has_synergy("w_sanctuary"):
			hp=minf(max_hp,hp+10)
			for enemy in enemies:
				if enemy!=dead and enemy.hp>0 and player.distance_to(enemy.pos)<145: hit_enemy(enemy,22)
		effect("class_oath",player,0.57,Vector2.ZERO)
		notices.append("class_oath")
	if relics.has("a_ash") and float(dead.get("burn",0))>0:
		var inferno: bool=has_synergy("a_inferno")
		var radius: float=175 if inferno else 110
		for enemy in enemies:
			if enemy!=dead and enemy.hp>0 and dead.pos.distance_to(enemy.pos)<radius:
				hit_enemy(enemy,(27.0 if inferno else 16.0)*relics.count("a_ash"))
				if inferno: enemy["burn"]=maxf(3.5,float(enemy.get("burn",0)))
		effect("class_ash",dead.pos,0.55,Vector2.ZERO)
		notices.append("class_ash")
	if relics.has("s_harvest"):
		var overflow: int=maxi(0,souls+relics.count("s_harvest")-6)
		souls=mini(6,souls+relics.count("s_harvest"))
		if overflow>0: hp=minf(max_hp,hp+2*overflow)
	if relics.has("s_haunt"):
		var target: Dictionary={}
		var nearest: float=520.0
		for enemy in enemies:
			if enemy==dead or enemy.hp<=0: continue
			var dist: float=dead.pos.distance_to(enemy.pos)
			if dist<nearest:
				nearest=dist
				target=enemy
		if not target.is_empty():
			var count: int=3 if has_synergy("s_legion") else 1
			for i in range(count):
				var direction: Vector2=dead.pos.direction_to(target.pos).rotated((float(i)-float(count-1)*0.5)*0.24)
				shoot(dead.pos,direction*530,false,bolt_damage*0.60*relics.count("s_haunt"))
				if not shots.is_empty(): shots[-1]["seeking"]=true
			effect("class_ghost",dead.pos,0.53,dead.pos.direction_to(target.pos))
			notices.append("class_ghost")
	if relics.has("s_coven") and kills%5==0:
		var radius: float=220 if has_synergy("s_legion") else 145
		for enemy in enemies:
			if enemy!=dead and enemy.hp>0 and player.distance_to(enemy.pos)<radius:
				hit_enemy(enemy,27*relics.count("s_coven"))
		effect("class_grave",player,0.64,Vector2.ZERO)
		notices.append("class_grave")

func class_room_clear() -> void:
	if recovery_rank>0: hp=minf(max_hp,hp+float(recovery_rank))
	if relics.has("w_banner"):
		hp=minf(max_hp,hp+6.0*relics.count("w_banner"))
		effect("class_banner",player,0.48,Vector2.ZERO)
		notices.append("class_banner")

func class_damage_taken() -> void:
	if relics.has("w_bastion"):
		for enemy in enemies:
			if enemy.hp>0 and player.distance_to(enemy.pos)<(185 if has_synergy("w_fortress") else 120):
				hit_enemy(enemy,(29.0 if has_synergy("w_fortress") else 16.0)*relics.count("w_bastion"))
				if has_synergy("w_fortress"): enemy["chill"]=maxf(2.0,float(enemy.get("chill",0)))
		effect("class_thorns",player,0.54,Vector2.ZERO)
		notices.append("class_thorns")
	if relics.has("s_blood"):
		souls=mini(6,souls+relics.count("s_blood"))
		if has_synergy("s_undying") and hp<=max_hp*0.4:
			shield_time=maxf(shield_time,1.4)
			for enemy in enemies:
				if enemy.hp>0 and player.distance_to(enemy.pos)<170: hit_enemy(enemy,32)
			effect("class_blood",player,0.60,Vector2.ZERO)
		notices.append("class_blood")

func class_wisps_tick(dt: float) -> void:
	if not relics.has("a_orbs"): return
	arcane_wisp_timer-=dt
	if arcane_wisp_timer>0: return
	arcane_wisp_timer=0.70
	var count: int=mini(4,relics.count("a_orbs"))
	var converged: bool=has_synergy("a_convergence")
	for i in range(count):
		var origin: Vector2=player+Vector2.from_angle(elapsed*1.4+float(i)*TAU/count)*62
		var nearest: Dictionary={}
		var shortest: float=490.0
		for enemy in enemies:
			if enemy.hp>0 and enemy.pos.distance_to(origin)<shortest:
				shortest=enemy.pos.distance_to(origin)
				nearest=enemy
		if nearest.is_empty(): continue
		var shots_per_orb: int=2 if converged else 1
		for j in range(shots_per_orb):
			shoot(origin,origin.direction_to(nearest.pos).rotated((float(j)-float(shots_per_orb-1)*0.5)*0.18)*510,false,bolt_damage*(0.45+0.10*float(maxi(0,relics.count("a_orbs")-4))))
		effect("class_wisp",origin,0.26,origin.direction_to(nearest.pos))
		notices.append("class_wisp")

func enter_hub() -> void:
	# This tavern is outside the route graph: no free combat progress or rewards.
	state="hub"
	room_kind="hub"
	# Always restore the tavern's fixed collision grid after an expedition.
	large_room=false
	room_bounds=ARENA
	room_regions.clear()
	spawn_point=SPAWN
	exit_point=EXIT
	boss_awake=false
	boss_intro_timer=0.0
	boss_arena=Rect2()
	boss_gate=Rect2()
	boss_gate_state="open"
	boss_gate_time=0.0
	boss_trigger_x=0.0
	threshold_mage_pos=Vector2.ZERO
	layout_name=""
	layout_name_de=""
	flow_map.clear()
	flow_timer=0.0
	encounter_id=""
	encounter_threat=0
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
	dash_trail_timer=0.0
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
	large_room=false
	room_bounds=ARENA
	room_regions.clear()
	spawn_point=SPAWN
	exit_point=EXIT
	layout_name=""
	layout_name_de=""
	boss_arena=Rect2()
	boss_trigger_x=0.0
	boss_awake=false
	boss_intro_timer=0.0
	boss_gate=Rect2()
	boss_gate_state="open"
	boss_gate_time=0.0
	flow_map.clear()
	flow_origin=Vector2i(-999,-999)
	flow_timer=0.0
	special_elapsed=0.0
	special_timer=0.0
	special_wave_timer=0.0
	special_objectives=0
	special_stage=0
	special_targets=0
	special_blast_timer=-1.0
	archive_discarded=""
	contract_clean_room=true
	forge_guard_charges=2 if forge_id=="iron_aegis" and room_kind in ["fight","elite","boss"] else 0
	environment_timer=2.5
	environment_cycle=0
	wave=1
	wave_total=2 if floor_index>=1 and room_kind=="elite" else 1
	temp_power=0
	if relics.has("curse"): temp_power=rng.randf_range(-5,15)
	
	var node: Dictionary = dungeon.nodes[room]
	encounter_id=str(node.get("encounter","skirmish")) if room_kind in Dungeon.ALL_COMBAT else ""
	encounter_threat=int(node.get("threat",0)) if room_kind in Dungeon.ALL_COMBAT else 0
	if encounter_id=="onslaught" and room>0: wave_total=2
	if not node.visited:
		node.visited = true
		visited_count += 1
		if relics.has("medic"): hp=minf(max_hp,hp+3*relics.count("medic"))
		if relics.has("sacrifice") and room_kind in ["fight","elite","boss"]: hp=maxf(1,hp-2*relics.count("sacrifice"))
	service_used = node.cleared
	build_room_layout(node)
	if room_kind=="boss": place_threshold_mage()
	player=spawn_point
	if node.cleared:
		state = "route"
		return
	if room_kind in ["treasure","shop","event","shrine","secret","forge"]+Dungeon.SPECIAL_SERVICE:
		state="service"
		return
	if room_kind in Dungeon.SPECIAL_COMBAT:
		start_special_combat(node)
		return
	if room_kind != "boss":
		spawn_encounter_wave(node)
		if encounter_id!="skirmish":
			notices.append("encounter_"+encounter_id)
	else:
		var boss_position: Vector2=Vector2(boss_arena.position.x+boss_arena.size.x*0.62,boss_arena.get_center().y) if large_room else Vector2(925,385)
		spawn_enemy(["boss","rootking","hollowking"][floor_index],boss_position)
		spawn_enemy(["caster","spore","hexer"][floor_index],boss_position+Vector2(-195,-172))
		spawn_enemy(["stalker","vine","revenant"][floor_index],boss_position+Vector2(-195,172))
		# Each boss also fields an actual native biome specialist.
		spawn_enemy(str(STAGE_SPECIALISTS[floor_index][1]),boss_position+Vector2(-300,0))

func start_special_combat(node: Dictionary) -> void:
	var family: Array=encounter_family()
	special_stage=0
	notices.append("sp_"+room_kind+"_enter")
	match room_kind:
		"ritual":
			special_timer=36.0
			special_wave_timer=6.0
			special_objectives=3
			for anchor in RITUAL_ANCHORS:
				spawn_enemy("ritual_anchor",anchor)
			for i in range(3+floor_index):
				var selected: String=str(STAGE_SPECIALISTS[floor_index][0]) if i==3 else str(family[i%family.size()])
				spawn_enemy(selected,Vector2(420+(i%3)*245,310+(i%2)*150))
		"survival":
			special_timer=SURVIVAL_DURATION
			special_wave_timer=7.0
			for i in range(5+floor_index*2):
				var selected: String=str(STAGE_SPECIALISTS[floor_index][i%2]) if i>=5 else str(family[i%family.size()])
				spawn_enemy(selected,Vector2(370+(i%4)*212,228+(i%2)*285))
		"bounty":
			special_timer=0.0
			special_wave_timer=9.0
			var target_kind: String=str(["brute","mushroom","sentinel"][floor_index])
			spawn_enemy(target_kind,Vector2(890,385),"frenzied")
			enemies[-1]["bounty"]=true
			enemies[-1]["hp"]*=3.8+float(floor_index)*0.5
			enemies[-1]["max_hp"]=enemies[-1].hp
			special_objectives=1
			for i in range(4+floor_index*2):
				var selected: String=str(STAGE_SPECIALISTS[floor_index][i%2]) if i>=4 else str(family[i])
				spawn_enemy(selected,Vector2(420+(i%3)*240,218+(int(i/3))*165))
		"unstable":
			special_timer=0.0
			special_wave_timer=3.8
			special_objectives=3
			for i in range(6+floor_index*2):
				var selected: String=str(STAGE_SPECIALISTS[floor_index][i%2]) if i>=6 else str(family[i%family.size()])
				spawn_enemy(selected,Vector2(290+(i%4)*213,200+int(i/4)*138))

func special_room_shard_bonus(kind: String,premium: bool,threat: int) -> int:
	if kind=="ritual": return 9 if premium else 3
	if kind=="bounty": return 10
	return 6+maxi(0,threat)*2

func complete_special_combat() -> void:
	if special_stage==99 or state!="fight": return
	var premium: bool=room_kind!="ritual" or special_stage==0
	special_stage=99
	# Sparse special-room purses: the old 14–23 Shards were balanced
	# for 4–6 enemies, before large-arena spawn densities were increased.
	var bonus: int=special_room_shard_bonus(room_kind,premium,encounter_threat)
	shards+=bonus
	check_shard_contract()
	enemies.clear()
	shots.clear()
	hazards.clear()
	combat_clears+=1
	class_room_clear()
	if contract_clean_room: advance_contract("untouched")
	make_offers(premium)
	state="reward"
	notices.append("sp_"+room_kind+"_success")
	effect("sp_"+room_kind+"_success",player,0.85,Vector2.ZERO)

func tick_special_room(dt: float) -> bool:
	if state!="fight" or room_kind not in Dungeon.SPECIAL_COMBAT: return false
	special_elapsed+=dt
	match room_kind:
		"ritual":
			if special_stage==0:
				special_timer=maxf(0.0,special_timer-dt)
				if special_timer<=0.0:
					special_stage=1
					spawn_enemy(str(["shield","brute","revenant"][floor_index]),Vector2(1050,380),"warded")
					enemies[-1]["hp"]*=2.4
					enemies[-1]["max_hp"]=enemies[-1].hp
					notices.append("sp_ritual_breach")
				special_wave_timer-=dt
				if special_wave_timer<=0 and enemies.size()<14:
					special_wave_timer=6.5
					var family: Array=encounter_family()
					for i in range(2):
						spawn_enemy(str(family[(int(special_elapsed/6.5)+i)%family.size()]),Vector2(450+i*530,240+i*270))
			if special_objectives==0 and special_stage==0:
				complete_special_combat()
				return true
		"survival":
			special_timer=maxf(0.0,special_timer-dt)
			if special_timer<=0.0 and hp>0.0:
				complete_special_combat()
				return true
			special_wave_timer-=dt
			if special_wave_timer<=0:
				special_wave_timer=7.0
				var family: Array=encounter_family()
				for i in range(3):
					if enemies.size()>=17: break
					spawn_enemy(str(family[(int(special_elapsed/7)+i)%family.size()]),Vector2(560+i*205,210+(i%2)*345))
				notices.append("sp_survival_wave")
			if int(special_elapsed)%9==1 and special_elapsed-dt<float(int(special_elapsed)):
				add_hazard(player,86,0.88,0.65,19+encounter_threat*3)
				hazards[-1]["type"]="sp_survival_hazard"
		"bounty":
			special_wave_timer-=dt
			if special_wave_timer<=0 and enemies.size()<13:
				special_wave_timer=9.0
				spawn_enemy(str(encounter_family()[int(special_elapsed/9)%encounter_family().size()]),Vector2(1080,300))
				notices.append("sp_bounty_backup")
		"unstable":
			special_wave_timer-=dt
			if special_wave_timer<=0:
				special_wave_timer=4.0
				special_blast_timer=0.84
				for core in UNSTABLE_CORES:
					add_hazard(core,104,0.84,0.43,22+encounter_threat*4)
					hazards[-1]["type"]="sp_unstable_blast"
				notices.append("sp_unstable_charge")
			if special_blast_timer>0:
				special_blast_timer-=dt
				if special_blast_timer<=0:
					for enemy in enemies:
						if enemy.hp<=0: continue
						for core in UNSTABLE_CORES:
							if enemy.pos.distance_to(core)<104:
								hit_enemy(enemy,40)
								break
					notices.append("sp_unstable_burst")
	return false

func encounter_family() -> Array:
	var family: Array=COMMON_ENEMIES.duplicate()
	family.append_array(STAGE_VETERANS[floor_index])
	family.append_array(STAGE_SPECIALISTS[floor_index])
	return family

func encounter_enemy_kind(family: Array, index: int, picker: RandomNumberGenerator) -> String:
	# Give both stage specialists a guaranteed role in sufficiently large waves.
	# Their attack patterns make biome identity mechanical, not just a recolor.
	if room>0 and index%6 in [2,5]:
		return str(STAGE_SPECIALISTS[floor_index][0 if index%6==2 else 1])
	match encounter_id:
		"crossfire":
			return str(family[[1,3,1,3,0][index%5]])
		"siege":
			return str(family[[4,2,4,1,2,3][index%6]])
		"onslaught":
			return str(family[[0,0,2,0,1][index%5]])
		_:
			return str(family[picker.randi_range(0,family.size()-1)])

func encounter_spawn_point(index: int, picker: RandomNumberGenerator) -> Vector2:
	if large_room and room_regions.size()>1:
		# Use the full traversable rooms instead of stacking every enemy in
		# one central 170px ring. Stable per-wave spacing prevents overlap.
		var region: Rect2=room_regions[1+index%(room_regions.size()-1)]
		var angle: float=float(index)*2.39996+picker.randf_range(-0.20,0.20)
		var radius_x: float=region.size.x*picker.randf_range(0.17,0.37)
		var radius_y: float=region.size.y*picker.randf_range(0.17,0.35)
		var proposal: Vector2=region.get_center()+Vector2(cos(angle)*radius_x,sin(angle)*radius_y)
		for attempt in range(9):
			var candidate: Vector2=proposal if attempt==0 else region.get_center()+Vector2.from_angle(angle+float(attempt)*1.73)*Vector2(radius_x,radius_y)*float(9-attempt)/9.0
			candidate=safe_position(candidate,36.0)
			if not blocked(candidate,30.0) and candidate.distance_to(player)>175.0:
				return candidate
		return region.get_center()
	var jitter: float=picker.randf_range(-20,20)
	match encounter_id:
		"crossfire":
			return Vector2(585+(index%3)*195,200 if index%2==0 else 565)+Vector2(jitter,0)
		"siege":
			return Vector2(520+(index%2)*120 if index%3!=2 else 950,250+(index%3)*125)+Vector2(jitter,jitter*0.5)
		"onslaught":
			return Vector2(470+(index%4)*175,215+(index%3)*140)+Vector2(jitter,jitter*0.5)
		_:
			return Vector2(520+(index%3)*235,195+(index/3)*135+jitter)

func enemy_pace() -> float:
	# Room zero on the first floor remains an approachable introduction.
	return 1.0+float(floor_index)*ENEMY_PACE_PER_FLOOR+float(encounter_threat)*ENEMY_PACE_PER_THREAT

func predicted_player_position(lead: float) -> Vector2:
	# Short prediction makes circling a threat, but every dangerous hit is telegraphed.
	var candidate: Vector2=safe_position(player+player_motion*lead,22.0)
	return player if blocked(candidate,18.0) else candidate

func champion_pressure(enemy: Dictionary, dt: float) -> void:
	if not bool(enemy.get("champion",false)) or float(enemy.get("wind",0.0))>0.0: return
	enemy["pressure_timer"]=float(enemy.get("pressure_timer",2.4))-dt
	if float(enemy["pressure_timer"])>0.0 or hazards.size()>=18: return
	var affix: String=str(enemy.get("affix",""))
	var center: Vector2=predicted_player_position(0.20)
	var radius: float=57.0
	var warning: float=0.82
	if affix=="warded":
		# A defensive ground pulse makes approaching shielded champions risky.
		center=enemy.pos
		radius=76.0
		warning=0.95
	elif affix=="volatile":
		radius=67.0
		warning=0.92
	add_hazard(center,radius,warning,0.46,15.0+3.0*floor_index+2.0*encounter_threat,true)
	hazards[-1]["type"]="champion_"+affix
	enemy["pressure_timer"]=maxf(3.1,5.1-0.35*floor_index)+rng.randf_range(-0.35,0.35)
	notices.append("rune")

func spawn_encounter_wave(node: Dictionary) -> void:
	var picker:=RandomNumberGenerator.new()
	picker.seed=int(node.geometry_seed)+wave*30011
	var family: Array=encounter_family()
	# The authored rooms are world-scale arenas: 4–11 enemies left vast
	# corridors empty. Density now climbs by stage, danger and room depth.
	var column: int=int(node.column)
	var count: int=6+floor_index*4+encounter_threat*2+(3 if room_kind=="elite" else 0)+mini(2,int(column/4))
	if large_room: count+=2+floor_index
	if room==0: count=6+floor_index*3
	if wave_total>1: count-=1
	count=clampi(count,5,26)
	for i in range(count):
		var kind: String=encounter_enemy_kind(family,i,picker)
		var affix: String=""
		if (room_kind=="elite" or encounter_threat==2) and i==count/2:
			affix=["frenzied","warded","volatile"][picker.randi_range(0,2)]
		spawn_enemy(kind,encounter_spawn_point(i,picker),affix)
		if affix!="":
			enemies[-1]["champion"]=true
			enemies[-1]["pressure_timer"]=2.2+picker.randf_range(0.0,0.8)
		if room_kind=="elite":
			enemies[-1].hp*=1.45
			enemies[-1].max_hp=enemies[-1].hp
			enemies[-1]["elite"]=true

func build_room_layout(node: Dictionary) -> void:
	if room_kind in ["fight","elite","boss"]:
		var plan: Dictionary=RoomLayouts.build(floor_index,int(node.layout),room_kind=="boss")
		large_room=true
		room_bounds=plan.bounds
		room_regions.assign(plan.regions)
		spawn_point=plan.spawn
		exit_point=plan.exit
		boss_arena=plan.boss_arena
		boss_trigger_x=float(plan.boss_trigger)
		boss_gate=plan.boss_gate
		boss_gate_state="open"
		boss_gate_time=0.0
		layout_name=str(plan.name)
		layout_name_de=str(plan.de_name)
		walls.assign(plan.walls)
		return
	if room_kind in Dungeon.SPECIAL_COMBAT:
		match room_kind:
			"ritual":
				walls.append(Rect2(382,206,82,67))
				walls.append(Rect2(380,515,85,65))
			"survival":
				for x in [430.0,900.0]:
					walls.append(Rect2(x,335,62,95))
			"bounty":
				walls.append(Rect2(635,195,80,74))
				walls.append(Rect2(635,504,80,74))
			"unstable":
				for core in UNSTABLE_CORES:
					# Reinforced reactor housing is wider than the previous square;
					# keep collider inside the painted 124 px device.
					walls.append(Rect2(core-Vector2(42,42),Vector2(84,84)))
		return
	if room_kind=="boss":
		if floor_index==2:
			walls.append(Rect2(480,172,76,76))
			walls.append(Rect2(480,525,76,76))
		return
	var geometry:=RandomNumberGenerator.new()
	geometry.seed=int(node.geometry_seed)
	var layout: int=node.layout
	if room_kind in ["treasure","secret","shrine","blood_altar","archive"]:
		walls.append(Rect2(590,175,100,58))
		return
	if room_kind in ["shop","event","forge","forgotten_forge"]:
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
	# A run commits to ONE lane per dungeon column. Neither reversing to an
	# earlier column nor sweeping sibling service rooms is allowed.
	var options: Array=[]
	var current_column: int=int(dungeon.nodes[room].column)
	for id in dungeon.available(room):
		var node: Dictionary=dungeon.nodes[int(id)]
		if int(node.column)<=current_column or bool(node.visited): continue
		if int(id)==Dungeon.BOSS and not dungeon.boss_ready(): continue
		options.append(id)
	return options

# Fog-of-war truth lives in the run model, never in button cosmetics.
# Previously visited rooms and the *currently reachable* next-column rooms
# are readable. Every other node is hidden, including distant bosses/elites.
func map_node_visibility(index: int) -> String:
	if index<0 or index>=dungeon.nodes.size(): return "unknown"
	if index==room or bool(dungeon.nodes[index].get("visited",false)):
		return "visited"
	if route_options().has(index): return "available"
	if oracle_revealed.has(index): return "oracle"
	return "unknown"

func oracle_next_column() -> int:
	if state!="route": return -1
	var current_column: int=int(dungeon.nodes[room].column)
	for column in range(current_column+2,10):
		for index in range(dungeon.nodes.size()):
			if int(dungeon.nodes[index].column)==column and map_node_visibility(index)=="unknown":
				return column
	return -1

func oracle_price() -> int:
	return ORACLE_BASE_COST+ORACLE_COST_STEP*oracle_purchases

func oracle_can_buy() -> bool:
	return oracle_next_column()>=0 and shards>=oracle_price()

func oracle_reveal() -> bool:
	if not oracle_can_buy(): return false
	var column: int=oracle_next_column()
	shards-=oracle_price()
	oracle_purchases+=1
	for index in range(dungeon.nodes.size()):
		if int(dungeon.nodes[index].column)==column:
			oracle_revealed[index]=true
	check_shard_contract()
	return true

func place_threshold_mage() -> void:
	threshold_mage_pos=Vector2.ZERO
	if room_kind!="boss" or not large_room or not boss_gate.has_area(): return
	var approach: Rect2=room_regions[-2]
	# Stall belongs to the entrance side of the boss shutter, against the
	# upper/lower wall, leaving the center of the corridor unobstructed.
	for offset in [70.0,approach.size.y-70.0,110.0,approach.size.y*0.5]:
		var candidate: Vector2=Vector2(boss_gate.position.x-142.0,approach.position.y+float(offset))
		if RoomLayouts.accessible(candidate,28.0,room_regions,walls):
			threshold_mage_pos=candidate
			return

func threshold_mage_available() -> bool:
	return state=="fight" and room_kind=="boss" and not boss_awake and boss_gate_state=="open" and threshold_mage_pos!=Vector2.ZERO and player.distance_to(threshold_mage_pos)<=MAGE_REACH

func threshold_mage_price(item: String) -> int:
	if not MAGE_PRICES.has(item): return -1
	return int(MAGE_PRICES[item][floor_index])

func threshold_mage_can_buy(item: String) -> bool:
	if not threshold_mage_available() or not MAGE_STOCK.has(item) or mage_stock_bought.has(item): return false
	if item=="heal_charge" and (active_item!="heal" or heal_charges>=HEAL_CHARGES_PER_FLOOR+1): return false
	if item=="special_charge" and mage_special_charges>0: return false
	if item=="ward" and mage_guard_charges>0: return false
	if item=="blessing" and mage_special_blessing: return false
	return shards>=threshold_mage_price(item)

func threshold_mage_purchase(item: String) -> bool:
	if not threshold_mage_can_buy(item): return false
	shards-=threshold_mage_price(item)
	mage_stock_bought[item]=true
	match item:
		"heal_charge": heal_charges+=1
		"special_charge": mage_special_charges=2
		"ward": mage_guard_charges=1
		"blessing": mage_special_blessing=true
	check_shard_contract()
	return true

func open_route() -> bool:
	if state!="exit" or player.distance_to(exit_point)>75: return false
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
	heal_charges = HEAL_CHARGES_PER_FLOOR
	oracle_revealed.clear()
	oracle_purchases=0
	mage_stock_bought.clear()
	mage_special_charges=0
	mage_guard_charges=0
	mage_special_blessing=false
	threshold_mage_pos=Vector2.ZERO
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
			elif choice==1: shards+=5
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
		"blood_altar":
			if choice==0:
				max_hp=maxf(20.0,max_hp*0.80)
				hp=minf(hp,max_hp)
				make_offers(true)
				# The greater sacrifice guarantees at least one epic class-safe card.
				var candidates: Array[String]=[]
				for id in available_relic_pool():
					if Content.rarity(id)>=2 and not offers.has(id): candidates.append(id)
				if not candidates.is_empty():
					offers[0]=candidates[rng.randi_range(0,candidates.size()-1)]
				state="reward"
				notices.append("sp_blood_altar_sacrifice")
				return true
			elif choice==1:
				max_hp=maxf(20.0,max_hp*0.90)
				hp=minf(hp,max_hp)
				damage+=12
				bolt_damage+=9
				notices.append("sp_blood_altar_sacrifice")
			elif choice==2:
				shards+=3
			else: return false
		"forgotten_forge":
			var targets: Array[String]=forge_relic_targets()
			if choice>=0 and choice<targets.size() and shards>=12:
				var selected: String=targets[choice]
				shards-=12
				var previously: Array[String]=active_synergies()
				relics.append(selected)
				apply_relic_stat(selected)
				tempered_relics.append(selected)
				notify_new_synergies(previously)
				notices.append("sp_forgotten_forge_temper")
			elif choice!=3: return false
		"archive":
			var targets: Array[String]=archive_relic_targets()
			if choice>=0 and choice<targets.size():
				archive_discarded=targets[choice]
				relics.erase(archive_discarded)
				remove_relic_stat(archive_discarded)
				make_offers(true)
				# Do not offer the exact same relic we just traded away.
				for i in range(offers.size()):
					if offers[i]!=archive_discarded: continue
					for candidate in available_relic_pool():
						if candidate!=archive_discarded and not offers.has(candidate):
							offers[i]=candidate
							break
				state="reward"
				notices.append("sp_archive_exchange")
				return true
			elif choice!=3: return false
		_: return false
	finish_room()
	check_shard_contract()
	return true

func forge_relic_targets() -> Array[String]:
	var result: Array[String]=[]
	for id in relics:
		if not result.has(id):
			result.append(id)
			if result.size()==3: break
	return result

func archive_relic_targets() -> Array[String]:
	return forge_relic_targets()

func apply_relic_stat(item: String) -> void:
	match item:
		"blade": damage+=9
		"heart": max_hp+=20; hp=minf(max_hp,hp+35)
		"boots": speed+=32
		"focus": bolt_damage+=11
		"ward": armor+=4
		"fury": dash_cooldown=maxf(0.55,dash_cooldown-0.23)
		"tempo": attack_cd=maxf(0.12,attack_cd*0.88)
		"potion": active_heal+=8
		"sanguine": damage+=6; max_hp=maxf(20,max_hp-8)
		"bulwark": armor+=3
		"swift": speed+=18
		"mana": special_cd=maxf(0.2,special_cd*0.85)
		"titan": max_hp+=30; hp=minf(max_hp,hp+30)
		"needle": bolt_damage+=5
		"charge": dash_cooldown=maxf(0.35,dash_cooldown-0.15)
		"battery": active_cd=maxf(20,active_cd-3)
		"glass": damage+=16; armor-=3
		"w_steel": max_hp+=18; hp=minf(max_hp,hp+18); armor+=1
	hp=minf(hp,max_hp)

func remove_relic_stat(item: String) -> void:
	# A transmutation must not retain the discarded relic's flat stat effects.
	match item:
		"blade": damage-=9
		"heart": max_hp=maxf(20,max_hp-20)
		"boots": speed-=32
		"focus": bolt_damage-=11
		"ward": armor-=4
		"fury": dash_cooldown+=0.23
		"tempo": attack_cd/=0.88
		"potion": active_heal-=8
		"sanguine": damage-=6; max_hp+=8
		"bulwark": armor-=3
		"swift": speed-=18
		"mana": special_cd/=0.85
		"titan": max_hp=maxf(20,max_hp-30)
		"needle": bolt_damage-=5
		"charge": dash_cooldown+=0.15
		"battery": active_cd+=3
		"glass": damage-=16; armor+=3
		"w_steel": max_hp=maxf(20,max_hp-18); armor-=1
	hp=minf(hp,max_hp)

func finish_room() -> void:
	dungeon.nodes[room].cleared=true
	service_used=true
	# Non-combat services and their relic selections return directly to the map.
	state="route" if room_kind in ["treasure","shop","event","shrine","secret","forge"]+Dungeon.SPECIAL_SERVICE else "exit"


func spawn_enemy(kind: String, pos: Vector2, affix: String="") -> void:
	var health := 42.0
	var radius := 17.0
	match kind:
		"caster": health = 35.0
		"gearhound": health=54.0; radius=17.0
		"coilgunner": health=62.0; radius=18.0
		"briarling": health=82.0; radius=21.0
		"sporewitch": health=58.0; radius=19.0
		"bone_lancer": health=106.0; radius=22.0
		"crypt_oracle": health=88.0; radius=20.0
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
	if kind=="ritual_anchor":
		health=110+floor_index*40
		radius=28.0
	if kind in ["boss","rootking","hollowking"]: health*=1.5625 # +25% compared with the previous boss HP
	else: health*=1.0+float(encounter_threat)*0.16
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
	enemies.append({"kind":kind,"pos":pos,"hp":health,"max_hp":health,"r":radius,"timer":rng.randf_range(0.6,1.4),"wind":0.0,"flash":0.0,"target":pos,"facing":Vector2.LEFT,"affix":affix,"primed":false,"strafe_dir":-1 if enemies.size()%2==0 else 1,"phase":1,"phase_lock":0.0})

func safe_position(pos: Vector2, radius: float) -> Vector2:
	return Vector2(clampf(pos.x,room_bounds.position.x+radius,room_bounds.end.x-radius),clampf(pos.y,room_bounds.position.y+radius,room_bounds.end.y-radius))

func blocked(pos: Vector2, radius: float) -> bool:
	# Dynamic obstruction is separate from static layout geometry so an open
	# door never obstructs the corridor, but a closing one cannot be dashed through.
	if room_kind=="boss" and boss_gate_state!="open" and boss_gate.grow(radius).has_point(pos):
		return true
	if large_room:
		var inside:=false
		for region in room_regions:
			if region.grow(-radius).has_point(pos):
				inside=true
				break
		if not inside: return true
	for wall in walls:
		if wall.grow(radius).has_point(pos): return true
	return false

func move_actor(pos: Vector2, motion: Vector2, radius: float) -> Vector2:
	# Substeps prevent high-speed dashes from tunneling through walls or
	# jumping the playable boundary between two adjoining chambers.
	var steps: int=maxi(1,ceili(motion.length()/9.0))
	for i in range(steps):
		var part: Vector2=motion/float(steps)
		var x: Vector2=safe_position(pos+Vector2(part.x,0),radius)
		if not blocked(x,radius): pos.x=x.x
		var y: Vector2=safe_position(pos+Vector2(0,part.y),radius)
		if not blocked(y,radius): pos.y=y.y
	return pos

func build_navigation_flow() -> void:
	# A shared flow field avoids each enemy doing its own path search.
	# It resolves winding corridors/branches without spawning through walls.
	flow_map.clear()
	var size: float=48.0
	var origin: Vector2i=Vector2i(roundi(player.x/size),roundi(player.y/size))
	if blocked(Vector2(origin.x*size,origin.y*size),18):
		for dx in range(-2,3):
			for dy in range(-2,3):
				var candidate: Vector2i=origin+Vector2i(dx,dy)
				if not blocked(Vector2(candidate.x*size,candidate.y*size),18):
					origin=candidate
					break
			if not blocked(Vector2(origin.x*size,origin.y*size),18): break
	if blocked(Vector2(origin.x*size,origin.y*size),18): return
	flow_origin=origin
	var queue: Array[Vector2i]=[origin]
	flow_map[origin]=0
	var head:=0
	while head<queue.size() and queue.size()<2400:
		var cell: Vector2i=queue[head]
		head+=1
		var depth: int=int(flow_map[cell])
		for step in [Vector2i(1,0),Vector2i(0,1),Vector2i(-1,0),Vector2i(0,-1)]:
			var next: Vector2i=cell+step
			if flow_map.has(next): continue
			if blocked(Vector2(next.x*size,next.y*size),18): continue
			flow_map[next]=depth+1
			queue.append(next)

func navigation_direction(pos: Vector2, fallback: Vector2) -> Vector2:
	if flow_map.is_empty(): return fallback
	var tile_size:=48.0
	var cell: Vector2i=Vector2i(roundi(pos.x/tile_size),roundi(pos.y/tile_size))
	var current_depth: int=int(flow_map.get(cell,99999))
	var best: Vector2=Vector2.ZERO
	var best_depth: int=current_depth
	for delta in [Vector2i(1,0),Vector2i(-1,0),Vector2i(0,1),Vector2i(0,-1)]:
		var next: Vector2i=cell+delta
		if not flow_map.has(next): continue
		var depth: int=int(flow_map[next])
		if depth>=best_depth: continue
		var target: Vector2=Vector2(next.x*tile_size,next.y*tile_size)
		if blocked(target,18): continue
		best_depth=depth
		best=pos.direction_to(target)
	return best if best!=Vector2.ZERO else fallback

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
	player_motion=direction.limit_length()*speed
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
	var was_dashing: bool=dash_left>0.0
	dash_left = maxf(0, dash_left - dt)
	if target.distance_to(player) > 4:
		aim = player.direction_to(target)
	if dash and dash_timer <= 0:
		dash_direction = direction.normalized() if direction.length_squared() > 0.01 else aim
		dash_left = 0.17
		dash_trail_timer=0.0
		dash_hits.clear()
		dash_timer = dash_cooldown
		invulnerable = maxf(invulnerable, 0.28)
		effect("dash",player,0.22,dash_direction)
		effect("dash_burst_"+character,player,0.39,dash_direction)
		notices.append("dash")
		if forge_id=="void_stride":
			for enemy in enemies:
				if enemy.hp>0 and player.distance_to(enemy.pos)<185:
					hit_enemy(enemy,damage*1.2+16.0)
			effect("forge_stride",player,0.40,Vector2.ZERO)
		if relics.has("nova") or character=="arcanist":
			effect("dash_nova_"+("frost" if set_id=="arcanist_frost" else "fire" if set_id=="arcanist_fire" else "arcane"),player,0.42,Vector2.ZERO)
			notices.append("dash_nova")
			for enemy in enemies:
				if player.distance_to(enemy.pos)<100: hit_enemy(enemy,15)
				if set_id=="arcanist_frost" and player.distance_to(enemy.pos)<100: enemy["chill"]=2.0
	var move := dash_direction * 860.0 if dash_left > 0 else direction.limit_length() * speed*environment_speed()
	player_motion=move
	player=move_actor(player,move*dt,15)
	if dash_left>0.0:
		dash_trail_timer-=dt
		if dash_trail_timer<=0.0:
			dash_trail_timer=0.028
			effect("dash_trail_"+character,player,0.30,dash_direction)
	if was_dashing and dash_left<=0.0:
		effect("dash_land_"+character,player,0.36,dash_direction)
		class_dash_landing()
		if has_synergy("inferno_rush"):
			for enemy in enemies:
				if enemy.hp>0 and player.distance_to(enemy.pos)<140:
					hit_enemy(enemy,damage*0.95+19.0)
					enemy["burn"]=maxf(4.0,float(enemy.get("burn",0)))
			effect("synergy_inferno",player,0.64,Vector2.ZERO)
			notices.append("dash_nova")
	if large_room and room_kind=="boss" and not boss_awake and player.x>=boss_trigger_x:
		boss_awake=true
		boss_intro_timer=1.85
		boss_gate_state="closing"
		boss_gate_time=0.0
		notices.append("boss_gate_close")
		notices.append("boss_reveal")
		effect("boss_reveal",boss_arena.get_center(),1.75,Vector2.ZERO)
	if boss_gate_state=="closing":
		boss_gate_time=minf(BOSS_GATE_CLOSE_DURATION,boss_gate_time+dt)
		if boss_gate_time>=BOSS_GATE_CLOSE_DURATION:
			boss_gate_state="closed"
			notices.append("boss_gate_lock")
	if boss_intro_timer>0.0: boss_intro_timer=maxf(0.0,boss_intro_timer-dt)
	if state == "exit":
		update_effects(dt)
		return
	if dash_left>0 and relics.has("dashfire"):
		for enemy in enemies:
			if not dash_hits.has(enemy) and player.distance_to(enemy.pos)<65:
				dash_hits.append(enemy)
				hit_enemy(enemy,14.0*relics.count("dashfire"))
				effect("dash_cinder_hit",enemy.pos,0.36,dash_direction)
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
		class_main_attack()
	if ranged and bolt_timer <= 0:
		# Paid swift casts are spent only during the actual boss encounter.
		if room_kind=="boss" and boss_awake and mage_special_charges>0:
			mage_special_charges-=1
			bolt_timer=maxf(0.09,special_cd*0.15)
		else:
			bolt_timer=special_cd
		specials+=1
		advance_contract("spellweaver")
		shot_style=set_id+"_special"
		notices.append(shot_style)
		effect("special_"+set_id,player,0.42,aim)
		special_multiplier=2.0 if relics.has("overcharge") and specials%4==0 else 1.0
		if room_kind=="boss" and boss_awake and mage_special_blessing: special_multiplier*=1.15
		if relics.has("s_pact") and hp<max_hp*0.4: special_multiplier*=1.0+0.3*relics.count("s_pact")
		class_special_attack()
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
	class_wisps_tick(dt)
	orbit_timer-=dt
	if relics.has("sun") and orbit_timer<=0:
		orbit_timer=0.16
		var sun_count: int=relics.count("sun")
		var visible_orbits: int=mini(8,sun_count)
		for enemy in enemies:
			if enemy.hp<=0: continue
			# Keep a reduced base aura so one sun remains useful against nearby
			# enemies that do not intersect the orbiting spheres themselves.
			if player.distance_to(enemy.pos)<96:
				hit_enemy(enemy,2.3*sun_count)
			# Every copy adds a physical orbit, and excess copies still scale damage.
			for orb_index in range(visible_orbits):
				var angle: float=elapsed*2.6+float(orb_index)*TAU/float(visible_orbits)
				var radius: float=73.0+float(orb_index%3)*17.0
				var center: Vector2=player+Vector2.from_angle(angle)*radius
				if enemy.pos.distance_to(center)<float(enemy.r)+16.0:
					hit_enemy(enemy,7.0+float(maxi(0,sun_count-8))*2.0)
					effect("relic_sun_hit",enemy.pos,0.26,Vector2.ZERO)
					notices.append("relic_sun")
					break
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
	if tick_special_room(dt): return
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
			if enemies[i].kind=="ritual_anchor":
				special_objectives=maxi(0,special_objectives-1)
				effect("sp_ritual_anchor",enemies[i].pos,0.70,Vector2.ZERO)
				notices.append("sp_ritual_anchor")
				enemies.remove_at(i)
				continue
			if bool(enemies[i].get("bounty",false)): special_stage=3
			effect("death", enemies[i].pos, 0.35, Vector2.ZERO)
			if enemies[i].kind in ["boss","rootking","hollowking"]:
				notices.append("boss_kill")
				# A defeated boss counts even if remaining adds end the expedition.
				bosses_defeated=maxi(bosses_defeated,floor_index+1)
				notices.append("boss_soul")
				effect("boss_soul",player,1.10,Vector2.ZERO)
			kills += 1
			advance_contract("slayer")
			if character=="soulbinder": souls=mini(6,souls+1)
			class_enemy_kill(enemies[i])
			if relics.has("d_purse"): shards+=relics.count("d_purse")
			if relics.has("harvest"):
				harvest_count+=1
				if harvest_count%5==0: hp=minf(max_hp,hp+8*relics.count("harvest"))
			if relics.has("soulstorm"):
				var spirits: int=6 if has_synergy("soul_requiem") else 3
				for j in range(spirits):
					shoot(enemies[i].pos,Vector2.from_angle(j*TAU/spirits+elapsed)*420,false,14*relics.count("soulstorm"))
				if spirits==6: effect("synergy_soul",enemies[i].pos,0.46,Vector2.ZERO)
			if relics.has("bounty") and (enemies[i].get("elite",false) or enemies[i].kind in ["boss","rootking","hollowking"]): shards+=5*relics.count("bounty")
			# Room density now rises from ~8 to ~18+; never multiply the
			# old kill purse by every extra enemy, especially on stage III.
			shards+=kill_shard_reward(enemies[i])+relics.count("greed")
			check_shard_contract()
			if relics.has("shatter") and float(enemies[i].get("chill",0))>0:
				var frozen_burst: bool=has_synergy("permafrost")
				var shards_count: int=10 if frozen_burst else 6
				for j in range(shards_count):
					shoot(enemies[i].pos,Vector2.from_angle(j*TAU/shards_count)*400,false,17 if frozen_burst else 12)
				if frozen_burst:
					for other in enemies:
						if other!=enemies[i] and other.hp>0 and enemies[i].pos.distance_to(other.pos)<150:
							other["chill"]=maxf(3.5,float(other.get("chill",0)))
					effect("synergy_frost",enemies[i].pos,0.56,Vector2.ZERO)
			if enemies[i].kind=="mushroom":
				spawn_enemy("spore",enemies[i].pos+Vector2(35,0))
				spawn_enemy("spore",enemies[i].pos-Vector2(35,0))
			hp=minf(max_hp,hp+(4.0 if has_synergy("soul_requiem") else 1.0)*relics.count("leech"))
			enemies.remove_at(i)
			notices.append("kill")
	update_effects(dt)
	if state=="fight" and room_kind=="bounty" and special_stage==3:
		complete_special_combat()
		return
	if state=="fight" and room_kind=="ritual" and special_objectives==0 and special_stage==0:
		complete_special_combat()
		return
	if hp <= 0 and relics.has("phoenix") and not revive_used:
		revive_used=true
		hp=max_hp*0.5
		invulnerable=2.0
	if hp <= 0:
		state = "defeat"
		shots.clear()
	elif enemies.is_empty() and (room_kind not in ["survival","ritual"] or (room_kind=="ritual" and special_stage==1)):
		if room_kind in ["ritual","unstable"]:
			complete_special_combat()
			return
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
		class_room_clear()
		if contract_clean_room: advance_contract("untouched")
		if room_kind=="boss": bosses_defeated=maxi(bosses_defeated,floor_index+1)
		if room_kind=="boss" and floor_index==FLOOR_COUNT-1:
			dungeon.nodes[room].cleared=true
			state="victory"
		elif room_kind=="boss" or room_kind=="elite" or room==0 or (encounter_threat==2 and rng.randf()<0.45) or rng.randf()<0.22:
			if room_kind=="elite": elites+=1
			make_offers(room_kind in ["boss","elite"] or encounter_threat==2)
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

func kill_shard_reward(enemy: Dictionary) -> int:
	var kind: String=str(enemy.get("kind",""))
	if kind in ["boss","rootking","hollowking"]: return SHARDS_BOSS_KILL
	var reward: int=SHARDS_STANDARD_KILL
	if bool(enemy.get("elite",false)) or bool(enemy.get("champion",false)):
		reward+=1
	if encounter_threat>=2: reward+=1
	return reward

func hit_enemy(enemy: Dictionary, amount: float) -> void:
	if enemy.hp<=0: return
	if large_room and room_kind=="boss" and not boss_awake: return
	var boss: bool=str(enemy.kind) in ["boss","rootking","hollowking"]
	if boss and float(enemy.get("phase_lock",0))>0:
		effect("boss_guard",enemy.pos,0.24,Vector2.ZERO)
		return
	var crit_triggered: bool=relics.has("crit") and rng.randf()<minf(0.8,0.2*relics.count("crit"))
	var poisoned_before: bool=float(enemy.get("poison",0))>0
	var burning_before: bool=float(enemy.get("burn",0))>0
	if crit_triggered: amount*=2
	if relics.has("hunter") and (enemy.get("elite",false) or enemy.kind in ["boss","rootking","hollowking"]): amount+=8*relics.count("hunter")
	if relics.has("blood") and hp<max_hp*0.35: amount*=1.4
	if relics.has("execution") and enemy.hp<enemy.max_hp*0.25: amount*=1.3
	if relics.has("frostfire") and float(enemy.get("chill",0))>0 and float(enemy.get("burn",0))>0: amount+=8*relics.count("frostfire")
	if relics.has("sacrifice"): amount*=1.0+0.2*relics.count("sacrifice")
	if relics.has("d_coin") and float(enemy.get("hex_mark",0))>0: amount+=12.0*relics.count("d_coin")
	if relics.has("a_prism") and float(enemy.get("chill",0))>0: amount+=7.0*relics.count("a_prism")
	if str(enemy.get("affix",""))=="warded" and enemy.hp>enemy.max_hp*0.5: amount*=0.62
	if enemy.kind=="shield" and enemy.pos.direction_to(player).dot(enemy.get("facing",Vector2.LEFT))>0.35: amount*=0.6
	var dealt: float=maxf(1,amount+temp_power)
	if boss:
		# Mandatory phase thresholds keep burst builds from one-shotting a boss.
		var phase: int=int(enemy.get("phase",1))
		if phase<3:
			var gate: float=float(enemy.max_hp)*(0.68 if phase==1 else 0.32)
			dealt=minf(dealt,maxf(0.0,float(enemy.hp)-gate))
	enemy.hp-=dealt
	if relics.has("frost") or set_id=="arcanist_frost": enemy["chill"]=2.0
	if relics.has("venom"): enemy["poison"]=3.0
	if relics.has("storm") and burning_before:
		var jumps:=0
		var wildfire: bool=has_synergy("wildfire_circuit")
		for other in enemies:
			if other!=enemy and other.hp>0 and enemy.pos.distance_to(other.pos)<185:
				other.hp-= (17.0 if wildfire else 8.0)*relics.count("storm")
				if wildfire: other["burn"]=maxf(3.0,float(other.get("burn",0)))
				effect("synergy_wildfire" if wildfire else "hit",other.pos,0.32,Vector2.ZERO)
				jumps+=1
				if jumps>=(3 if wildfire else 1): break
	if has_synergy("toxic_jackpot") and crit_triggered and poisoned_before:
		for other in enemies:
			if other!=enemy and other.hp>0 and enemy.pos.distance_to(other.pos)<125:
				other.hp-=maxf(14.0,dealt*0.38)
				other["poison"]=maxf(4.0,float(other.get("poison",0)))
		effect("synergy_toxic",enemy.pos,0.56,Vector2.ZERO)
	if relics.has("ember") or set_id=="arcanist_fire": enemy["burn"]=3.0
	class_enemy_hit(enemy,dealt)
	enemy.flash = 0.12
	effect("hit", enemy.pos, 0.22, Vector2.ZERO)
	if effects.size()<80: effects.append({"kind":"damage","pos":enemy.pos,"time":0.65,"max":0.65,"dir":Vector2.ZERO,"amount":int(round(dealt))})
	notices.append("hit")

func hurt(amount: float, from_enemy: bool = true) -> void:
	if invulnerable > 0 or shield_time>0 or hp <= 0:
		return
	if room_kind=="boss" and boss_awake and mage_guard_charges>0:
		mage_guard_charges-=1
		invulnerable=0.62
		effect("forge_guard",player,0.48,Vector2.ZERO)
		notices.append("forge_guard")
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
	if from_enemy: scaled*=ENEMY_DAMAGE_MULTIPLIER
	if from_enemy and resolve_rank>0: scaled*=maxf(0.70,1.0-0.03*resolve_rank)
	# Armor never negates more than half of an incoming hit.
	hp = maxf(0,hp-maxf(scaled*0.5,scaled-armor))
	class_damage_taken()
	if relics.has("guardpulse"): guard_pulse_pending=true
	invulnerable = 0.65+0.15*relics.count("warding")
	effect("hurt", player, 0.25, Vector2.ZERO)
	notices.append("hurt")

func shoot(pos: Vector2, velocity: Vector2, hostile: bool, power: float) -> void:
	if shots.size()>=180: return
	shots.append({"pos":pos,"vel":velocity,"hostile":hostile,"damage":power,"life":2.8,"pierce":0 if hostile else mini(5,relics.count("pierce")),"hit":[],"bounce":0 if hostile else mini(5,relics.count("bounce")+relics.count("d_sleight")),"style":enemy_shot_style if hostile else shot_style})

func boss_phase_for(enemy: Dictionary) -> int:
	var ratio: float=float(enemy.hp)/maxf(1.0,float(enemy.max_hp))
	return 3 if ratio<=0.32 else (2 if ratio<=0.68 else 1)

func update_boss_phase(enemy: Dictionary, dt: float) -> bool:
	var next_phase: int=boss_phase_for(enemy)
	var old_phase: int=int(enemy.get("phase",1))
	if next_phase>old_phase:
		enemy["phase"]=next_phase
		enemy["phase_lock"]=0.85 if next_phase==2 else 0.65
		enemy["wind"]=0.0
		enemy["timer"]=0.74 if next_phase==2 else 0.62
		effect("boss_phase_"+str(enemy.kind),enemy.pos,1.06,Vector2.ZERO)
		notices.append("boss_phase")
	var remaining: float=maxf(0.0,float(enemy.get("phase_lock",0))-dt)
	enemy["phase_lock"]=remaining
	return remaining>0.0

func boss_circle(pos: Vector2, radius: float, warning: float, power: float, kind: String) -> void:
	if hazards.size()>=20: return
	add_hazard(pos,radius,warning,0.52,power,true)
	hazards[-1]["type"]=kind

func boss_lane(pos: Vector2, vertical: bool, warning: float, power: float) -> void:
	if hazards.size()>=20: return
	var area: Rect2=Rect2(pos.x-40,pos.y-240 if large_room else 160,80,480 if large_room else 450) if vertical else Rect2(pos.x-400 if large_room else 80,pos.y-40,800 if large_room else 1120,80)
	var clean_center: Vector2=safe_position(pos,48)
	boss_circle(clean_center,44,warning,power,"boss_lane")
	hazards[-1]["rect"]=area

func boss_start_attack(enemy: Dictionary) -> void:
	var phase: int=int(enemy.get("phase",1))
	var kind: String=str(enemy.kind)
	var aim_point: Vector2=predicted_player_position(0.10 if phase==1 else (0.22 if phase==2 else 0.28))
	enemy["target"]=aim_point
	# Stage-three attacks demand deliberate dodge timing, not blind running.
	enemy["wind"]=0.93 if phase==1 else (0.79 if phase==2 else 0.68)
	effect("boss_charge_"+kind,enemy.pos,float(enemy.wind),enemy.pos.direction_to(aim_point))
	notices.append("boss_charge")
	match kind:
		"boss":
			if phase>=2: boss_lane(aim_point,true,float(enemy.wind),16+phase*3)
			if phase==3:
				# A delayed crossing lane creates a safe dodge window, then closes it.
				boss_lane(aim_point+Vector2(0,110),false,float(enemy.wind)+0.18,23)
				boss_circle(aim_point+Vector2(0,-112),68,float(enemy.wind)+0.12,24,"boss_blast")
		"rootking":
			boss_circle(aim_point,65,float(enemy.wind),15+phase*4,"boss_root")
			if phase>=2:
				for i in range(3 if phase==2 else 5):
					var pos: Vector2=aim_point+Vector2.from_angle(float(i)*TAU/(3 if phase==2 else 5))*125
					boss_circle(pos,49,float(enemy.wind)+0.12,16+phase*3,"boss_root")
			if phase==3:
				# Two late root eruptions punish players who dodge straight back.
				var retreat: Vector2=aim_point+enemy.pos.direction_to(aim_point)*122.0
				boss_circle(safe_position(retreat,42),54,float(enemy.wind)+0.28,22,"boss_root")
		"hollowking":
			add_royal_seals(aim_point,phase%2==0,float(enemy.wind),17+phase*2,true)
			if phase==3:
				add_royal_seals(aim_point,phase%2!=0,float(enemy.wind)+0.22,19,true)
				boss_circle(aim_point,59,float(enemy.wind)+0.10,22,"boss_blast")

func boss_release_attack(enemy: Dictionary) -> void:
	var phase: int=int(enemy.get("phase",1))
	var kind: String=str(enemy.kind)
	var pos: Vector2=enemy.pos
	var direction: Vector2=pos.direction_to(enemy.target)
	if direction==Vector2.ZERO: direction=Vector2.LEFT
	match kind:
		"boss":
			var volley: int=5 if phase==1 else (7 if phase==2 else 9)
			for i in range(volley):
				var arc: float=(float(i)-(volley-1)*0.5)*0.14
				shoot(pos,direction.rotated(arc)*(355+phase*20),true,13+phase*2)
			if phase>=2:
				for j in range(10 if phase==2 else 14):
					var angle: float=float(j)*TAU/(10 if phase==2 else 14)+elapsed*0.25
					shoot(pos,Vector2.from_angle(angle)*295,true,14+phase)
		"rootking":
			for i in range(3+phase*2):
				var arc: float=(float(i)-(2+phase*2)*0.5)*0.20
				shoot(pos,direction.rotated(arc)*305,true,13+phase*2)
			if phase>=2:
				for j in range(5+phase*2):
					shoot(pos,Vector2.from_angle(float(j)*TAU/(5+phase*2)+elapsed*0.15)*255,true,12+phase*2)
		"hollowking":
			var count: int=7 if phase==1 else (11 if phase==2 else 15)
			for i in range(count):
				var angle: float=float(i)*TAU/count+elapsed*0.3
				shoot(pos,Vector2.from_angle(angle)*(310+phase*15),true,14+phase*2)
			if phase>=2:
				for j in range(3+phase):
					var arc: float=(float(j)-(phase+2)*0.5)*0.16
					shoot(pos,direction.rotated(arc)*420,true,16+phase*2)
	effect("boss_release_"+kind,pos,0.50,direction)
	notices.append("boss_release")

func update_enemies(dt: float) -> void:
	if large_room and room_kind=="boss" and (not boss_awake or boss_intro_timer>0.0): return
	if large_room:
		flow_timer-=dt
		if flow_timer<=0.0:
			flow_timer=0.33
			build_navigation_flow()
	for enemy in enemies:
		if enemy.hp <= 0:
			continue
		if float(enemy.get("burn",0))>0:
			enemy["burn"] = maxf(0,float(enemy.burn)-dt)
			enemy.hp -= dt*4.0*(relics.count("ember")+relics.count("a_ember")+(1 if set_id=="arcanist_fire" else 0))
		if float(enemy.get("poison",0))>0:
			enemy["poison"]=maxf(0,float(enemy.poison)-dt)
			enemy.hp-=dt*5*relics.count("venom")
		enemy["chill"]=maxf(0,float(enemy.get("chill",0))-dt)
		enemy["hex_mark"]=maxf(0,float(enemy.get("hex_mark",0))-dt)
		enemy["prism_lock"]=maxf(0,float(enemy.get("prism_lock",0))-dt)
		enemy.flash = maxf(0,float(enemy.flash)-dt)
		if str(enemy.kind) in ["boss","rootking","hollowking"] and int(enemy.get("phase",1))<3:
			var phase_gate: float=float(enemy.max_hp)*(0.68 if int(enemy.get("phase",1))==1 else 0.32)
			enemy.hp=maxf(phase_gate,float(enemy.hp))
		if str(enemy.kind) in ["boss","rootking","hollowking"] and update_boss_phase(enemy,dt):
			continue
		var affix: String=str(enemy.get("affix",""))
		enemy.timer-=dt*enemy_pace()*(1.35 if affix=="frenzied" else 1.0)
		champion_pressure(enemy,dt)
		if affix=="volatile" and not bool(enemy.get("primed",false)) and enemy.hp<enemy.max_hp*0.5:
			enemy["primed"]=true
			add_hazard(enemy.pos,67,0.9,1.10,21+encounter_threat*3,true)
			hazards[-1]["type"]="volatile"
			notices.append("rune")
		var pos: Vector2 = enemy.pos
		var toward: Vector2=pos.direction_to(player)
		if large_room and pos.distance_to(player)>100:
			toward=navigation_direction(pos,toward)
		var dist := pos.distance_to(player)
		var kind: String = enemy.kind
		if kind=="ritual_anchor":
			continue
		var radius: float = enemy.r
		if enemy.wind > 0:
			enemy.wind -= dt
			if enemy.wind <= 0:
				enemy_shot_style=kind
				notices.append("enemy_"+kind)
				effect("enemy_"+kind,pos,0.30,pos.direction_to(enemy.target))
				if kind=="gearhound":
					# The gearhound vaults at its marked target; a short windup
					# and a forward line make the lunge readable and dodgeable.
					var leap: Vector2=pos.direction_to(enemy.target)
					enemy.pos=move_actor(pos,leap*120.0,radius)
					if enemy.pos.distance_to(player)<radius+30: hurt(17)
					effect("enemy_gearhound",enemy.pos,0.30,leap)
				elif kind=="briarling":
					if hazards.size()<24:
						add_hazard(enemy.target,65,0.70,0.65,16.0+floor_index*3.0,true)
						hazards[-1]["type"]="briar_snare"
					if dist<radius+42: hurt(14)
				elif kind=="sporewitch":
					if hazards.size()<24:
						add_hazard(enemy.target,66,0.78,3.4,12.0+floor_index*2.0,true)
						hazards[-1]["type"]="sporepool"
					var poison_direction: Vector2=pos.direction_to(enemy.target)
					shoot(pos,poison_direction*315.0,true,12)
				elif kind=="coilgunner":
					var barrel: Vector2=pos.direction_to(enemy.target)
					for arc in [-0.14,0.14]:
						shoot(pos,barrel.rotated(arc)*425.0,true,13.0+floor_index)
				elif kind=="bone_lancer":
					var spear: Vector2=pos.direction_to(enemy.target)
					for arc in [-0.10,0.0,0.10]:
						shoot(pos,spear.rotated(arc)*455.0,true,15.0+floor_index)
				elif kind=="crypt_oracle":
					if hazards.size()<24:
						add_hazard(enemy.target,72,0.84,0.75,21.0+floor_index*2.0,true)
						hazards[-1]["type"]="oracle_seal"
					var beam: Vector2=pos.direction_to(enemy.target)
					shoot(pos,beam*305.0,true,13.0)
				elif kind in ["caster","archer","spore","hexer"]:
					shoot(pos, pos.direction_to(enemy.target)*(390 if kind=="archer" else 265), true, 12)
					if kind=="spore": add_hazard(enemy.target,44,0.6,2.8,9,true)
					if kind=="hexer":
						for j in range(3): shoot(pos,pos.direction_to(enemy.target).rotated((j-1)*0.25)*330,true,13)
				elif kind=="vine":
					if player.distance_to(enemy.target)<75:
						player=move_actor(player,player.direction_to(pos)*100,15)
						hurt(12)
				elif kind in ["boss","rootking","hollowking"]:
					boss_release_attack(enemy)
				elif dist < radius + 45:
					hurt(18 if kind in ["brute","shield","mushroom","sentinel"] else 11)
				enemy.timer = maxf(0.92,1.86-float(enemy.get("phase",1))*0.24) if kind in ["boss","rootking","hollowking"] else 0.95
			continue
		var velocity := Vector2.ZERO
		if kind in ["caster","archer","spore","hexer","coilgunner","sporewitch","bone_lancer","crypt_oracle"]:
			# Casters and specialists circle, instead of standing still in the kite zone.
			var strafe: Vector2=Vector2(-toward.y,toward.x)*float(enemy.get("strafe_dir",1))
			if dist>325: velocity=toward*132+strafe*36
			elif dist<185: velocity=-toward*116+strafe*54
			else: velocity=strafe*116
			if enemy.timer<=0 and dist<555:
				enemy.wind=maxf(0.40,0.61-0.045*floor_index-0.025*encounter_threat)
				if kind in ["sporewitch","crypt_oracle"]: enemy.wind+=0.16
				enemy.target=predicted_player_position(0.16 if floor_index==0 else 0.24)
		elif kind in ["boss","rootking","hollowking"]:
			velocity=toward*(92+20*int(enemy.get("phase",1))) if dist>145 else Vector2.ZERO
			if enemy.timer<=0: boss_start_attack(enemy)
		elif kind in ["vine","briarling","gearhound"]:
			var rapid: bool=kind=="gearhound"
			velocity=toward*(196.0 if rapid else 123.0) if dist>(102.0 if rapid else 180.0) else Vector2.ZERO
			if enemy.timer<=0 and dist<(275.0 if rapid else 325.0):
				enemy.wind=0.44 if rapid else 0.65
				enemy.target=predicted_player_position(0.24 if rapid else 0.16)
		else:
			velocity = toward*(100 if kind in ["brute","shield","mushroom","sentinel"] else (204 if kind=="revenant" else 159))
			# Minions flank alternating sides when closing, but still commit to melee.
			if dist>radius+72 and dist<260:
				var flank: Vector2=Vector2(-toward.y,toward.x)*float(enemy.get("strafe_dir",1))
				velocity+=flank*34.0
			if dist < radius+45 and enemy.timer <= 0:
				enemy.wind = 0.48 if kind in ["brute","shield","mushroom","sentinel"] else 0.34
				velocity = Vector2.ZERO
		# Simple separation and obstacle steering; no clumping at pillars.
		for other in enemies:
			if other == enemy or other.hp <= 0:
				continue
			var separation: Vector2 = pos - other.pos
			var length := separation.length()
			if length > 0.01 and length < radius + float(other.r) + 8:
				velocity += separation.normalized()*100
		if kind not in ["boss","rootking","hollowking"]:
			velocity*=enemy_pace()
		if affix=="frenzied": velocity*=1.3
		if slow_time>0 or enemy.chill>0: velocity*=0.4
		if kind=="shield" and enemy.wind<=0 and enemy.timer<0.15:
			enemy.facing=toward
		var next := move_actor(pos, velocity*dt, radius)
		if velocity.length_squared() > 1 and next.distance_to(pos) < velocity.length()*dt*0.25:
			next = move_actor(pos, velocity.rotated(PI/2)*dt, radius)
		enemy.pos = next

func add_hazard(pos: Vector2, radius: float, warning: float, life: float, power: float, enemy_origin: bool=false) -> void:
	if hazards.size()>=24: return
	hazards.append({"pos":safe_position(pos,10),"r":radius,"warn":warning,"life":warning+life,"power":power,"enemy_origin":enemy_origin})

func environment_speed() -> float:
	if floor_index!=1 or state!="fight": return 1.0
	for hazard in hazards:
		if hazard.get("type","")=="sporepool" and hazard.warn<=0 and player.distance_to(hazard.pos)<hazard.r: return 0.65
	return 1.0

func spawn_reinforcements() -> void:
	var family: Array=encounter_family()
	var picker:=RandomNumberGenerator.new()
	picker.seed=int(dungeon.nodes[room].geometry_seed)+wave*30011
	var count: int=6+floor_index*3+encounter_threat*2+(2 if room_kind=="elite" else 0)+(2 if large_room else 0)
	count=clampi(count,6,21)
	for i in range(count):
		var kind: String=encounter_enemy_kind(family,i+wave*6,picker)
		var affix: String=""
		if room_kind=="elite" and i==count/2: affix="frenzied"
		spawn_enemy(kind,encounter_spawn_point(i+2,picker),affix)
		if affix!="":
			enemies[-1]["champion"]=true
			enemies[-1]["pressure_timer"]=2.0
		if room_kind=="elite":
			enemies[-1].hp*=1.3
			enemies[-1].max_hp=enemies[-1].hp
			enemies[-1]["elite"]=true
	environment_timer=1.5

func update_environment(dt: float) -> void:
	if room_kind in Dungeon.SPECIAL_COMBAT: return
	if large_room and room_kind=="boss" and not boss_awake: return
	if hazards.size()>=20: return
	environment_timer-=dt
	if environment_timer>0: return
	environment_cycle+=1
	var hazard_multiplier: float=1.0-0.12*encounter_threat
	match floor_index:
		0:
			# The vault's pressure plates fire a telegraphed row of spikes.
			environment_timer=4.5*hazard_multiplier
			var x: float=(player.x+float((environment_cycle%3)-1)*95) if large_room else 420+(environment_cycle%3)*270
			var cells: Array[Rect2]=spike_cells(x)
			if not cells.is_empty():
				hazards.append({"pos":Vector2(x,player.y if large_room else 385),"r":40,"warn":0.85,"life":1.60,"power":16,"type":"spikes","cells":cells,"phase":"warn"})
				notices.append("spike_warn")
		1:
			# Persistent toxic pools restrict movement, not merely damage.
			environment_timer=3.4*hazard_multiplier
			var pos: Vector2=safe_position(player+Vector2.from_angle(environment_cycle*2.4)*95,48)
			add_hazard(pos,58,0.85,5.0,12)
			hazards[-1]["type"]="sporepool"
			notices.append("sporepool")
		2:
			# Crown seals erupt as spectral lances, never inside tomb geometry.
			environment_timer=3.4*hazard_multiplier
			var vertical: bool=environment_cycle%2==0
			var pos: Vector2=player
			add_royal_seals(pos,vertical,1.15,22)
			if room_kind=="boss" and environment_cycle%3==0:
				add_royal_seals(pos,not vertical,1.45,20)

func add_royal_seals(pos: Vector2, vertical: bool, warning: float, power: float, enemy_origin: bool=false) -> void:
	var lane:=Rect2(pos.x-30,pos.y-220 if large_room else 166,60,440 if large_room else 438) if vertical else Rect2(pos.x-300 if large_room else 78,pos.y-30,600 if large_room else 1124,60)
	var cells: Array[Rect2]=[]
	var count: int=9 if vertical and large_room else (12 if large_room else (7 if vertical else 18))
	for i in range(count):
		var center: Vector2=Vector2(pos.x,pos.y+(i-4)*50) if vertical and large_room else (Vector2(pos.x+(i-6)*50,pos.y) if large_room else (Vector2(pos.x,196+i*60) if vertical else Vector2(110+i*60,pos.y)))
		var cell:=Rect2(center-Vector2(27,24),Vector2(54,48))
		if not room_bounds.grow(-8).encloses(cell): continue
		if large_room and blocked(cell.get_center(),26): continue
		var clear:=true
		for wall in walls:
			if wall.grow(18).intersects(cell): clear=false; break
		if clear: cells.append(cell)
	if cells.is_empty(): return
	hazards.append({"pos":pos,"r":40,"warn":warning,"warning":warning,"life":warning+0.84,"power":power,"type":"rune","rect":lane,"cells":cells,"phase":"warn","enemy_origin":enemy_origin})
	notices.append("rune")

func spike_cells(x: float) -> Array[Rect2]:
	var cells: Array[Rect2]=[]
	var top: int=int(player.y)-160 if large_room else 180
	var bottom: int=int(player.y)+161 if large_room else 584
	for y in range(top,bottom,32):
		var cell:=Rect2(x-26,y,52,28)
		if large_room and blocked(cell.get_center(),14): continue
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
						hurt(hazard.power,bool(hazard.get("enemy_origin",false)))
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
						hurt(hazard.power,bool(hazard.get("enemy_origin",false)))
						break
		elif hazard.warn<=0:
			var inside: bool=hazard.rect.has_point(player) if hazard.has("rect") else player.distance_to(hazard.pos)<hazard.r
			if inside: hurt(hazard.power,bool(hazard.get("enemy_origin",false)))

func update_shots(dt: float) -> void:
	for i in range(shots.size()-1,-1,-1):
		var shot: Dictionary = shots[i]
		if bool(shot.get("seeking",false)) and not shot.hostile:
			var nearest: Dictionary={}
			var distance: float=500.0
			for candidate in enemies:
				if candidate.hp<=0: continue
				var d: float=shot.pos.distance_to(candidate.pos)
				if d<distance:
					distance=d
					nearest=candidate
			if not nearest.is_empty():
				var desired: Vector2=shot.pos.direction_to(nearest.pos)*maxf(430.0,float(shot.vel.length()))
				shot.vel=shot.vel.lerp(desired,minf(1.0,dt*6.5))
		shot.pos += shot.vel*dt*(0.4 if shot.hostile and slow_time>0 else 1.0)
		shot.life -= dt
		var pos: Vector2 = shot.pos
		if not room_bounds.has_point(pos) and shot.bounce>0:
			if pos.x<room_bounds.position.x or pos.x>room_bounds.end.x: shot.vel.x=-shot.vel.x
			if pos.y<room_bounds.position.y or pos.y>room_bounds.end.y: shot.vel.y=-shot.vel.y
			shot.pos=safe_position(pos,5)
			pos=shot.pos
			shot.bounce-=1
		var remove: bool = shot.life <= 0 or not room_bounds.has_point(pos) or blocked(pos, 4)
		if not remove:
			if shot.hostile:
				if pos.distance_to(player) < 22:
					hurt(shot.damage)
					remove = true
			else:
				for enemy in enemies:
					if enemy.hp > 0 and not shot.hit.has(enemy) and pos.distance_to(enemy.pos) < float(enemy.r)+7:
						hit_enemy(enemy, shot.damage)
						if has_synergy("ricochet_storm") and not bool(shot.get("fragment",false)) and not shot.hostile and shots.size()<175:
							for arc in [-0.78,0.78]:
								shoot(pos,shot.vel.normalized().rotated(arc)*515,false,shot.damage*0.43)
								shots[-1]["fragment"]=true
							effect("synergy_ricochet",pos,0.38,shot.vel.normalized())
						shot.hit.append(enemy)
						remove = shot.pierce <= 0
						shot.pierce -= 1
						break
		if remove:
			shots.remove_at(i)

func class_relic_pool() -> Array[String]:
	var result: Array[String]=[]
	for key in Content.CLASS_POOLS[character]: result.append(str(key))
	return result

func available_relic_pool() -> Array[String]:
	var result: Array[String]=[]
	for key in RELICS:
		var owner: String=Content.relic_owner(str(key))
		if owner=="" or owner==character: result.append(str(key))
	return result

func make_offers(premium: bool = false) -> void:
	offers.clear()
	var pool: Array[String]=available_relic_pool()
	var class_pool: Array[String]=class_relic_pool()
	for i in range(3):
		# Guaranteed one class relic; a second with 34% probability.
		var class_pick: bool=i==0 or (i==1 and rng.randf()<0.34)
		var index: int=rng.randi_range(0,pool.size()-1)
		if class_pick and not class_pool.is_empty():
			var chosen: String=class_pool[rng.randi_range(0,class_pool.size()-1)]
			index=pool.find(chosen)
		if not class_pick:
			for attempt in range(5):
				var rarity: int=Content.rarity(pool[index])
				if rng.randf()<(0.95 if rarity==0 else (0.75 if premium else 0.4)/maxf(1.0,float(rarity))): break
				index=rng.randi_range(0,pool.size()-1)
		var picked: String=pool[index]
		offers.append(picked)
		pool.remove_at(index)
		class_pool.erase(picked)
	# Once a build has a matching half, occasionally offer an actual payoff.
	# Only replace one slot and never generate duplicate choices.
	var candidates: Array[String]=synergy_candidates()
	if not candidates.is_empty() and rng.randf()<0.68:
		var candidate: String=candidates[rng.randi_range(0,candidates.size()-1)]
		if not offers.has(candidate): offers[2]=candidate

func choose_relic(index: int) -> bool:
	if state != "reward" or index < 0 or index >= offers.size():
		return false
	var item: String = offers[index]
	var previous_synergies: Array[String]=active_synergies()
	relics.append(item)
	apply_relic_stat(item)
	hp = minf(max_hp, hp+(5.0 if floor_index==0 else 3.0))
	notify_new_synergies(previous_synergies)
	finish_room()
	return true

func next_room() -> bool:
	return open_route()

func use_active() -> void:
	if active_item=="heal":
		if hp>=max_hp or heal_charges<=0: return
		heal_charges-=1
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

func encounter_variation_smoke() -> bool:
	var profiles: Dictionary={}
	var dangers: Dictionary={}
	var champions: Dictionary={}
	for seed in range(6):
		begin(700+seed)
		player=HUB_PORTAL
		if not enter_first_room() or encounter_id!="skirmish" or encounter_threat!=0: return false
		for floor in range(FLOOR_COUNT):
			floor_index=floor
			dungeon.generate(700+seed,floor)
			for room_id in range(1,Dungeon.BOSS):
				if dungeon.kind(room_id) not in ["fight","elite"]: continue
				room=room_id
				enter_room()
				if state!="fight" or enemies.is_empty() or encounter_id=="": return false
				if int(dungeon.nodes[room_id].threat)!=encounter_threat: return false
				profiles[encounter_id]=true
				dangers[encounter_threat]=true
				if encounter_id=="onslaught" and wave_total!=2: return false
				var has_champion:=false
				for enemy in enemies:
					var affix: String=str(enemy.get("affix",""))
					if affix!="":
						has_champion=true
						champions[affix]=true
				if (room_kind=="elite" or encounter_threat==2) and not has_champion: return false
				for enemy in enemies:
					if str(enemy.get("affix",""))!="" and not bool(enemy.get("champion",false)): return false
	if profiles.size()!=4 or dangers.size()!=3 or champions.size()<3: return false
	return true

func build_and_boss_smoke() -> bool:
	# A card must preview the identical combination that gameplay activates.
	for id in Content.SYNERGIES:
		begin(405)
		var parts: Array=Content.SYNERGIES[id]["parts"]
		var first: String=str(parts[0])
		var second: String=str(parts[1])
		relics.append(first)
		if has_synergy(str(id)) or not synergy_candidates().has(second): return false
		var info: Dictionary=synergy_preview(second)
		if info.is_empty() or info["state"]!="ready" or info["id"]!=id: return false
		relics.append(second)
		if not has_synergy(str(id)) or synergy_preview(second)["state"]!="active": return false
	begin(109)
	if not active_synergies().is_empty(): return false
	# Actually trigger different transformations, not just their metadata.
	player=HUB_PORTAL
	if not enter_first_room(): return false
	relics.assign(["ember","storm"])
	enemies.clear()
	# Spawn inside the authored opening region, independent of which
	# stage-specific layout is currently selected.
	for i in range(4): spawn_enemy("brute",Vector2(230+i*34,900))
	enemies[0]["burn"]=3.0
	var health_before: float=float(enemies[3].hp)
	hit_enemy(enemies[0],12.0)
	if float(enemies[3].hp)>=health_before:
		push_error("Chain-lightning smoke lost clustered enemies in the new authored room")
		return false
	relics.assign(["pierce","bounce"])
	enemies.clear()
	spawn_enemy("brute",Vector2(320,900))
	shots.clear()
	shoot(Vector2(310,900),Vector2(400,0),false,17.0)
	update_shots(0.016)
	var ricochet_count:=0
	for shot in shots:
		if bool(shot.get("fragment",false)): ricochet_count+=1
	if ricochet_count!=2:
		push_error("Ricochet smoke failed in authored room: "+str(ricochet_count))
		return false
	relics.assign(["dashfire","nova"])
	enemies.clear()
	spawn_enemy("brute",Vector2(250,385))
	dash_timer=0
	tick(0.016,Vector2.RIGHT,Vector2(650,385),false,false,true)
	var burst_seen:=false
	for fx in effects:
		if str(fx.kind).begins_with("dash_burst_"): burst_seen=true
	if not burst_seen: return false
	tick(0.18,Vector2.RIGHT,Vector2(650,385),false,false,false)
	var inferno_seen:=false
	for fx in effects:
		if fx.kind=="synergy_inferno": inferno_seen=true
	if not inferno_seen: return false
	for floor in range(3):
		begin(557+floor)
		floor_index=floor
		dungeon.generate(seed_value,floor)
		room=Dungeon.BOSS
		enter_room()
		var boss: Dictionary={}
		for enemy in enemies:
			if enemy.kind in ["boss","rootking","hollowking"]: boss=enemy
		if boss.is_empty(): return false
		# The boss cannot be attacked before traversing its entrance corridor.
		player=Vector2(boss_trigger_x+145,boss_arena.get_center().y)
		tick(0.016,Vector2.ZERO,Vector2(boss.pos.x,boss.pos.y),false,false,false)
		if not boss_awake: return false
		boss_intro_timer=0.0
		hit_enemy(boss,float(boss.max_hp)*8.0)
		if absf(float(boss.hp)/float(boss.max_hp)-0.68)>0.002: return false
		update_enemies(0.016)
		if int(boss.phase)!=2 or float(boss.phase_lock)<=0: return false
		var health_at_lock: float=float(boss.hp)
		hit_enemy(boss,99999.0)
		if float(boss.hp)!=health_at_lock: return false
		boss["phase_lock"]=0.0
		boss["hp"]=float(boss.max_hp)*0.32
		update_enemies(0.016)
		if int(boss.phase)!=3: return false
		boss["phase_lock"]=0.0
		boss_start_attack(boss)
		if hazards.is_empty() or float(boss.wind)<=0: return false
		boss_release_attack(boss)
		if shots.size()<7: return false
	return true

func class_relic_smoke() -> bool:
	# All four classes: shared + exactly six exclusive relics, no cross-class leaks.
	var loadouts: Array[String]=["warden_guard","dealer_cards","arcanist_fire","soulbinder_reaper"]
	for loadout in loadouts:
		begin(605,loadout)
		var expected: Array[String]=class_relic_pool()
		var pool: Array[String]=available_relic_pool()
		if expected.size()!=6 or pool.size()!=54: return false
		for key in pool:
			var owner: String=Content.relic_owner(key)
			if owner!="" and owner!=character: return false
		for key in expected:
			if not RELICS.has(key) or not pool.has(key) or int(RELICS[key][4])<48: return false
		for i in range(12):
			make_offers(i%2==0)
			if offers.size()!=3 or Content.relic_owner(offers[0])!=character: return false
			for j in range(offers.size()):
				if Content.relic_owner(offers[j]) not in ["",character] or offers.count(offers[j])!=1: return false
		player=HUB_PORTAL
		if not enter_first_room(): return false
		match character:
			"warden":
				relics.assign(["w_lance","w_charger"])
				shots.clear()
				class_special_attack()
				class_dash_landing()
				if shots.size()<4 or not has_synergy("w_phalanx"): return false
			"dealer":
				relics.assign(["d_marked","d_sleight"])
				attacks=4
				shots.clear()
				class_main_attack()
				if shots.size()!=5 or int(shots[0].bounce)<1 or not has_synergy("d_royal"): return false
			"arcanist":
				relics.assign(["a_orbs","a_surge"])
				player=Vector2(300,385)
				enemies.clear()
				spawn_enemy("brute",Vector2(390,385))
				shots.clear()
				arcane_wisp_timer=0
				class_wisps_tick(0.72)
				if shots.size()!=2 or not has_synergy("a_convergence"): return false
				specials=3
				var hp_before: float=float(enemies[0].hp)
				class_special_attack()
				if float(enemies[0].hp)>=hp_before: return false
			"soulbinder":
				relics.assign(["s_harvest","s_grim"])
				souls=2
				hp=40
				shots.clear()
				class_special_attack()
				if souls!=1 or shots.size()!=3 or hp<=40 or not has_synergy("s_requiem"): return false
	begin(606,"warden_guard")
	player=HUB_PORTAL
	if not enter_first_room(): return false
	relics.assign(["sun","sun"])
	player=Vector2(250,385)
	elapsed=0
	orbit_timer=0
	enemies.clear()
	spawn_enemy("brute",Vector2(323,385))
	var before: float=float(enemies[0].hp)
	tick(0.016,Vector2.ZERO,Vector2(500,385),false,false,false)
	if float(enemies[0].hp)>=before or relics.count("sun")!=2: return false
	# +25% boss HP must apply consistently to all three bosses.
	for floor in range(3):
		begin(611+floor)
		floor_index=floor
		dungeon.generate(seed_value,floor)
		room=Dungeon.BOSS
		enter_room()
		var threshold: float=[800.0,1550.0,2500.0][floor]
		var boss_found:=false
		for enemy in enemies:
			if enemy.kind in ["boss","rootking","hollowking"]:
				boss_found=true
				if float(enemy.max_hp)<threshold: return false
		if not boss_found: return false
	return true

func route_and_damage_smoke() -> bool:
	for floor in range(FLOOR_COUNT):
		begin(970+floor)
		floor_index=floor
		dungeon.generate(seed_value,floor)
		for source in range(Dungeon.BOSS):
			room=source
			state="route"
			var current: Dictionary=dungeon.nodes[source]
			var options: Array=route_options()
			for destination in options:
				if not dungeon.available(source).has(destination): return false
				if int(dungeon.nodes[int(destination)].column)<=int(current.column): return false
				if bool(dungeon.nodes[int(destination)].visited): return false
			for neighbor in dungeon.available(source):
				if int(dungeon.nodes[int(neighbor)].column)<=int(current.column) and options.has(neighbor): return false
		room=0
		state="route"
		if route_options().size()!=3: return false
		var first: int=int(route_options()[0])
		if not travel(first): return false
		# Even forced overlay reopening cannot reclaim past or sibling rooms.
		state="route"
		if travel(0): return false
		if floor>=0 and dungeon.nodes[first].column!=1: return false
		for neighbor in dungeon.available(first):
			if int(dungeon.nodes[int(neighbor)].column)==1 and travel(int(neighbor)): return false
	# Enemy attacks, including hostile projectiles and melee, use a shared
	# 40% multiplier; environmental hazards must retain their old tuning.
	begin(995)
	player=HUB_PORTAL
	if not enter_first_room(): return false
	invulnerable=0
	hp=100
	hurt(10)
	if absf(hp-86.0)>0.01: return false
	invulnerable=0
	hp=100
	hurt(10,false)
	if absf(hp-90.0)>0.01: return false
	begin(996,"warden_guard",{"resolve":1,"recovery":1})
	player=HUB_PORTAL
	if not enter_first_room(): return false
	invulnerable=0
	hp=100
	hurt(10)
	if absf(hp-86.42)>0.02: return false
	hp=70
	class_room_clear()
	if absf(hp-71.0)>0.01: return false
	return true

func special_rooms_smoke() -> bool:
	var types: Array[String]=[]
	for kind in Dungeon.SPECIAL_COMBAT+Dungeon.SPECIAL_SERVICE: types.append(str(kind))
	for kind in types:
		print("Special chamber smoke: "+kind)
		var found:=false
		for floor in range(FLOOR_COUNT):
			if found: break
			for seed in range(240,270):
				begin(850+seed)
				floor_index=floor
				dungeon.generate(seed_value,floor)
				for id in range(1,Dungeon.BOSS):
					if dungeon.nodes[id].kind==kind:
						room=id
						found=true
						break
				if found: break
		if not found:
			push_error("Special room missing: "+kind)
			return false
		# Initialize each room through its real gameplay lifecycle.
		enter_room()
		if not dungeon.nodes[room].visited or room_kind!=kind: return false
		match kind:
			"ritual":
				if state!="fight" or special_objectives!=3 or special_timer<35.9: return false
				var anchors:=0
				for enemy in enemies:
					if enemy.kind=="ritual_anchor":
						anchors+=1
						enemy.hp=0
				if anchors!=3: return false
				tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
				if state!="reward" or offers.size()!=3 or shards<20: return false
				if not choose_relic(0) or state!="exit": return false
			"survival":
				if state!="fight" or special_timer!=SURVIVAL_DURATION: return false
				if tick_special_room(29.99) or state!="fight": return false
				if not tick_special_room(0.02) or state!="reward" or offers.size()!=3: return false
				if not choose_relic(1) or state!="exit": return false
			"bounty":
				var targets:=0
				for enemy in enemies:
					if bool(enemy.get("bounty",false)):
						targets+=1
						enemy.hp=0
				if targets!=1: return false
				tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
				if state!="reward" or shards<23 or offers.size()!=3: return false
			"unstable":
				if state!="fight" or special_objectives!=3: return false
				enemies[0].pos=UNSTABLE_CORES[0]+Vector2(20,0)
				var before: float=float(enemies[0].hp)
				tick_special_room(4.0)
				if float(enemies[0].hp)>=before or hazards.size()!=3: return false
				for enemy in enemies: enemy.hp=0
				tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
				if state!="reward": return false
			"blood_altar":
				if state!="service" or not service_action(0): return false
				if state!="reward" or max_hp>=100 or offers.size()!=3: return false
				if Content.rarity(offers[0])<2: return false
				if not choose_relic(0) or state!="route" or service_action(1): return false
			"forgotten_forge":
				if state!="service": return false
				relics.append("blade")
				apply_relic_stat("blade")
				shards=20
				var previous: float=damage
				if not service_action(0) or relics.count("blade")!=2: return false
				if damage<=previous or shards!=8 or state!="route" or service_action(0): return false
			"archive":
				if state!="service": return false
				relics.append("blade")
				apply_relic_stat("blade")
				if not service_action(0) or relics.has("blade") or archive_discarded!="blade": return false
				if offers.size()!=3 or state!="reward" or offers.has("blade"): return false
				if not choose_relic(0) or state!="route" or service_action(0): return false
		print("Special chamber smoke OK: "+kind)
	return true

func boss_gate_smoke() -> bool:
	# The shutter is a REAL collision wall, not merely a decorative door.
	# All six boss approaches must be open initially, animate shut on entry,
	# block every actor after closing, and reset on new runs.
	for floor in range(FLOOR_COUNT):
		for plan in range(2):
			begin(9600+floor*2+plan)
			floor_index=floor
			dungeon.generate(seed_value,floor)
			room=Dungeon.BOSS
			dungeon.nodes[room]["layout"]=plan
			enter_room()
			if not large_room or boss_gate_state!="open" or boss_awake:
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 1: if not large_room or boss_gate_state!='open' or boss_awake")
				return false
			if boss_gate.size.x<50 or boss_gate.size.y<150:
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 2: if boss_gate.size.x<50 or boss_gate.size.y<150")
				return false
			var gate: Rect2=boss_gate
			# Approach around the Grove's permanent pillar; the shutter itself
			# spans the whole corridor, not just its centerline.
			var passage_y: float=gate.position.y+67.0
			var enter_side: Vector2=Vector2(gate.position.x-46,passage_y)
			var arena_side: Vector2=Vector2(gate.end.x+46,passage_y)
			if blocked(gate.get_center(),15):
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 3: if blocked(gate.get_center(),15)")
				return false
			var crossing: Vector2=move_actor(enter_side,Vector2(155,0),15)
			if crossing.x<=gate.end.x+20:
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 4: if crossing.x<=gate.end.x+20")
				return false
			player=Vector2(boss_trigger_x+140,boss_arena.get_center().y)
			tick(0.016,Vector2.ZERO,boss_arena.get_center(),false,false,false)
			if not boss_awake or boss_gate_state!="closing" or not blocked(gate.get_center(),15):
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 5: if not boss_awake or boss_gate_state!='closing' or not blocked(ga")
				return false
			if not notices.has("boss_gate_close"):
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 6: if not notices.has('boss_gate_close')")
				return false
			# Even a dash-scale movement must stop against the full shutter.
			var bounced: Vector2=move_actor(arena_side,Vector2(-220,0),15)
			if bounced.x<gate.end.x+14:
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 7: if bounced.x<gate.end.x+14")
				return false
			tick(BOSS_GATE_CLOSE_DURATION+0.03,Vector2.ZERO,boss_arena.get_center(),false,false,false)
			if boss_gate_state!="closed" or not notices.has("boss_gate_lock"):
				push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 8: if boss_gate_state!='closed' or not notices.has('boss_gate_lock')")
				return false
		begin(9660+floor)
		if boss_gate.has_area() or boss_gate_state!="open":
			push_error("Boss gate stage "+str(floor_index)+" room "+str(room)+" checkpoint 9: if boss_gate.has_area() or boss_gate_state!='open'")
			return false
	return true

func difficulty_smoke() -> bool:
	# Heal is finite, does not spend a charge at full HP, and refills on descent.
	begin(5150,"warden_guard")
	player=HUB_PORTAL
	if not enter_first_room() or heal_charges!=HEAL_CHARGES_PER_FLOOR: return false
	hp=20.0
	use_active()
	if heal_charges!=1 or absf(hp-38.0)>0.01 or active_timer<=0.0: return false
	active_timer=0.0
	use_active()
	if heal_charges!=0 or absf(hp-56.0)>0.01: return false
	active_timer=0.0
	use_active()
	if heal_charges!=0 or absf(hp-56.0)>0.01 or active_timer!=0.0: return false
	heal_charges=1
	hp=max_hp
	use_active()
	if heal_charges!=1 or hp!=max_hp: return false
	heal_charges=0
	room=Dungeon.BOSS
	room_kind="boss"
	dungeon.nodes[room].cleared=true
	state="route"
	if not descend() or heal_charges!=HEAL_CHARGES_PER_FLOOR or floor_index!=1: return false
	# An elite/champion gets a visible, delayed pressure attack.
	enemies.clear()
	hazards.clear()
	spawn_enemy("stalker",player+Vector2(140,0),"frenzied")
	enemies[-1]["champion"]=true
	enemies[-1]["pressure_timer"]=0.0
	update_enemies(0.016)
	if hazards.is_empty() or str(hazards[-1].get("type",""))!="champion_frenzied" or float(hazards[-1].warn)<0.6: return false
	# Phase III of the final boss must add a lethal-looking, telegraphed blast.
	begin(5151)
	floor_index=2
	dungeon.generate(seed_value,floor_index)
	room=Dungeon.BOSS
	enter_room()
	var final_boss: Dictionary={}
	for enemy in enemies:
		if str(enemy.kind)=="hollowking": final_boss=enemy
	if final_boss.is_empty(): return false
	final_boss["phase"]=3
	hazards.clear()
	boss_start_attack(final_boss)
	var marked_blast:=false
	for danger in hazards:
		if str(danger.get("type",""))=="boss_blast" and float(danger.warn)>=0.6: marked_blast=true
	if not marked_blast: return false
	# Defeat must actually remain reachable and end the run.
	begin(5152)
	player=HUB_PORTAL
	if not enter_first_room(): return false
	hp=5.0
	invulnerable=0.0
	hurt(20.0)
	if hp>0.0: return false
	tick(0.016,Vector2.ZERO,Vector2(640,385),false,false,false)
	return state=="defeat"

func stage_roster_smoke() -> bool:
	# All twelve map variants must be represented in each stage and every
	# larger fight must contain the stage's exclusive enemy identities.
	for floor in range(FLOOR_COUNT):
		begin(8118+floor)
		floor_index=floor
		dungeon.generate(seed_value,floor)
		var variants: Dictionary={}
		for id in range(Dungeon.BOSS):
			var node: Dictionary=dungeon.nodes[id]
			if str(node.kind) not in ["fight","elite"]: continue
			variants[int(node.layout)]=true
		if variants.size()!=4:
			push_error("Missing combat layout variants on floor "+str(floor))
			return false
		var first_count: int=-1
		var peak_count: int=0
		var found_specialists: Dictionary={}
		for room_id in range(Dungeon.BOSS):
			if dungeon.kind(room_id) not in ["fight","elite"]: continue
			room=room_id
			enter_room()
			if not large_room or enemies.is_empty(): return false
			if room_id==0: first_count=enemies.size()
			peak_count=maxi(peak_count,enemies.size())
			for enemy in enemies:
				var kind: String=str(enemy.kind)
				if STAGE_SPECIALISTS[floor].has(kind): found_specialists[kind]=true
				for other_floor in range(FLOOR_COUNT):
					if other_floor!=floor and STAGE_SPECIALISTS[other_floor].has(kind):
						push_error("Wrong-stage enemy "+kind+" in floor "+str(floor))
						return false
		if first_count!=6+floor*3 or peak_count<8+floor*4:
			push_error("Stage spawn density did not scale for floor "+str(floor))
			return false
		if found_specialists.size()!=2:
			push_error("Missing biome-exclusive specialists on floor "+str(floor))
			return false
		# Exercise *each* specialist's actual windup-to-release behavior.
		# Every new sprite must correspond to a distinct gameplay attack.
		room=0
		enter_room()
		for kind in STAGE_SPECIALISTS[floor]:
			enemies.clear()
			hazards.clear()
			shots.clear()
			effects.clear()
			notices.clear()
			spawn_enemy(str(kind),player+Vector2(205,0))
			var specialist: Dictionary=enemies[0]
			specialist["target"]=player
			specialist["wind"]=0.01
			update_enemies(0.03)
			match str(kind):
				"gearhound":
					if not notices.has("enemy_gearhound"): return false
				"coilgunner":
					if shots.size()!=2: return false
				"briarling":
					if hazards.is_empty() or str(hazards[-1].type)!="briar_snare": return false
				"sporewitch":
					if hazards.is_empty() or shots.size()!=1 or str(hazards[-1].type)!="sporepool": return false
				"bone_lancer":
					if shots.size()!=3: return false
				"crypt_oracle":
					if hazards.is_empty() or shots.size()!=1 or str(hazards[-1].type)!="oracle_seal": return false
	return true

func map_fog_smoke() -> bool:
	# A run reveals only its traversed history and next legal destinations.
	# No future room identity may leak from an available choice or an old floor.
	for floor in range(FLOOR_COUNT):
		begin(98730+floor)
		floor_index=floor
		dungeon.generate(seed_value,floor)
		room=0
		enter_room()
		if map_node_visibility(0)!="visited": return false
		var next_rooms: Array=route_options()
		if next_rooms.size()!=3: return false
		for i in range(dungeon.nodes.size()):
			var state_for_node: String=map_node_visibility(i)
			var expected: String="visited" if i==0 else ("available" if next_rooms.has(i) else "unknown")
			if state_for_node!=expected:
				push_error("Fog initial visibility mismatch at stage %d / node %d" % [floor,i])
				return false
		# Even a secret / elite destination is correctly disclosed only
		# when directly selectable, and other siblings remain unknown.
		state="route"
		var chosen: int=int(next_rooms[1])
		if not travel(chosen): return false
		if map_node_visibility(0)!="visited" or map_node_visibility(chosen)!="visited": return false
		for other in next_rooms:
			if int(other)!=chosen and map_node_visibility(int(other))!="unknown": return false
		var second_choices: Array=route_options()
		if second_choices.is_empty(): return false
		for next in second_choices:
			if map_node_visibility(int(next))!="available": return false
		for i in range(dungeon.nodes.size()):
			if i!=0 and i!=chosen and not second_choices.has(i) and map_node_visibility(i)!="unknown":
				return false
		if map_node_visibility(Dungeon.BOSS)!="unknown": return false
	# Out-of-range indices must never expose names or crash the chart.
	return map_node_visibility(-1)=="unknown" and map_node_visibility(Dungeon.ROOM_COUNT)=="unknown"

func shard_economy_smoke() -> bool:
	# 70 ordinary kills previously paid >=140 Shards before specials. With
	# the new model they pay only 70 at threat I and 140 at danger III.
	begin(5411)
	encounter_threat=0
	var ordinary: Dictionary={"kind":"stalker","elite":false,"champion":false}
	if kill_shard_reward(ordinary)!=1 or 70*kill_shard_reward(ordinary)!=70: return false
	ordinary["elite"]=true
	if kill_shard_reward(ordinary)!=2: return false
	ordinary["elite"]=false
	encounter_threat=2
	if kill_shard_reward(ordinary)!=2: return false
	if kill_shard_reward({"kind":"boss"})!=10: return false
	if special_room_shard_bonus("ritual",true,2)!=9: return false
	if special_room_shard_bonus("ritual",false,2)!=3: return false
	if special_room_shard_bonus("bounty",true,2)!=10: return false
	if special_room_shard_bonus("survival",true,0)!=6: return false
	if special_room_shard_bonus("survival",true,2)!=10: return false
	for floor in range(FLOOR_COUNT):
		floor_index=floor
		for item in MAGE_STOCK:
			if threshold_mage_price(item)!=int(MAGE_PRICES[item][floor]): return false
			if threshold_mage_price(item)<=0: return false
	# All four goods deliberately cost more than the typical stage-I
	# purse of 90–130 Shards; this is a tradeoff, not a free loadout.
	var total: int=0
	floor_index=0
	for item in MAGE_STOCK: total+=threshold_mage_price(item)
	return total==230

func oracle_mage_smoke() -> bool:
	begin(9160)
	player=HUB_PORTAL
	if not enter_first_room(): return false
	shards=200
	if oracle_reveal() or map_node_visibility(4)!="unknown": return false
	state="route"
	var next_rooms: Array=route_options()
	if next_rooms.size()!=3 or oracle_next_column()!=2 or oracle_price()!=12: return false
	if not oracle_reveal() or shards!=188 or oracle_price()!=20: return false
	for i in range(dungeon.nodes.size()):
		var column: int=int(dungeon.nodes[i].column)
		if column==2 and map_node_visibility(i)!="oracle": return false
		if column==3 and map_node_visibility(i)!="unknown": return false
		if column==1 and map_node_visibility(i)!="available": return false
	if route_options().size()!=3 or travel(4): return false
	if not oracle_reveal() or shards!=168 or map_node_visibility(7)!="oracle": return false
	shards=0
	if oracle_reveal(): return false
	# Each floor must have an interactable, safely placed magus *before*
	# its boss shutter; purchases are single-use and paid in run Shards.
	for floor in range(FLOOR_COUNT):
		begin(9380+floor)
		floor_index=floor
		dungeon.generate(seed_value,floor)
		room=Dungeon.BOSS
		enter_room()
		if threshold_mage_pos==Vector2.ZERO or boss_gate_state!="open" or boss_awake: return false
		if threshold_mage_pos.x>=boss_gate.position.x or not RoomLayouts.accessible(threshold_mage_pos,27.0,room_regions,walls): return false
		player=threshold_mage_pos+Vector2(-34,0)
		if not threshold_mage_available(): return false
		shards=400
		if threshold_mage_purchase("invalid") or threshold_mage_price("invalid")!=-1: return false
		if not threshold_mage_purchase("special_charge") or mage_special_charges!=2: return false
		if threshold_mage_purchase("special_charge"): return false
		if not threshold_mage_purchase("ward") or mage_guard_charges!=1: return false
		if not threshold_mage_purchase("blessing") or not mage_special_blessing: return false
		if not threshold_mage_purchase("heal_charge") or heal_charges!=3: return false
		if threshold_mage_purchase("heal_charge"): return false
		boss_awake=true
		boss_gate_state="closing"
		if threshold_mage_available() or threshold_mage_purchase("ward"): return false
		invulnerable=0.0
		hp=max_hp
		hurt(22)
		if hp!=max_hp or mage_guard_charges!=0: return false
		# Two accelerated casts are consumed; the third casts normally.
		# Keep an inert target alive so the fight is not immediately cleared
		# by the test tick's normal empty-wave victory transition.
		enemies.clear()
		shots.clear()
		spawn_enemy("brute",boss_arena.get_center())
		enemies[-1]["hp"]=999999.0
		enemies[-1]["max_hp"]=999999.0
		bolt_timer=0
		tick(0.016,Vector2.ZERO,player+Vector2(210,0),false,true,false)
		if mage_special_charges!=1 or bolt_timer>=special_cd*0.5: return false
		bolt_timer=0
		tick(0.016,Vector2.ZERO,player+Vector2(210,0),false,true,false)
		if mage_special_charges!=0 or bolt_timer>=special_cd*0.5: return false
		bolt_timer=0
		tick(0.016,Vector2.ZERO,player+Vector2(210,0),false,true,false)
		if bolt_timer<special_cd*0.7: return false
	# A standard special must apply its multiplier only once, including
	# the pre-boss blessing and old Overcharge relic interaction.
	begin(9499,"warden_guard")
	floor_index=0
	dungeon.generate(seed_value,floor_index)
	room=Dungeon.BOSS
	enter_room()
	boss_awake=true
	mage_special_blessing=true
	shots.clear()
	shot_style="warden_guard_special"
	special_multiplier=1.15
	shoot(player,Vector2.RIGHT*500,false,100.0*special_multiplier)
	if shots.is_empty() or absf(float(shots[-1].damage)-115.0)>0.01: return false
	shots.clear()
	special_multiplier=2.0*1.15
	shoot(player,Vector2.RIGHT*500,false,100.0*special_multiplier)
	if shots.is_empty() or absf(float(shots[-1].damage)-230.0)>0.01: return false
	# Ascending to a new biome must not carry over purchased foresight or buffs.
	begin(9551)
	player=HUB_PORTAL
	if not enter_first_room(): return false
	shards=40
	state="route"
	if not oracle_reveal(): return false
	room=Dungeon.BOSS
	room_kind="boss"
	dungeon.nodes[room].cleared=true
	if not descend(): return false
	if not oracle_revealed.is_empty() or oracle_purchases!=0 or mage_special_charges!=0 or mage_guard_charges!=0 or mage_special_blessing: return false
	return true

func smoke_test() -> bool:
	if not RoomLayouts.smoke_test():
		push_error("Authored chamber connectivity smoke failed")
		return false
	if not boss_gate_smoke():
		push_error("Boss door physical lock / animation smoke failed")
		return false
	if not dungeon.smoke_test():
		push_error("Dungeon generation smoke failed")
		return false
	if not map_fog_smoke():
		push_error("Dungeon map fog-of-war regression failed")
		return false
	if not oracle_mage_smoke():
		push_error("Threshold mage and Oracle economy regression failed")
		return false
	if not shard_economy_smoke():
		push_error("Shard income and mage price regression failed")
		return false
	if not route_and_damage_smoke():
		push_error("Route/damage smoke failed")
		return false
	if not special_rooms_smoke():
		push_error("Special chamber model smoke failed")
		return false
	if not class_relic_smoke():
		push_error("Regression failed: class_relic_smoke")
		return false
	if not build_and_boss_smoke():
		push_error("Regression failed: build_and_boss_smoke")
		return false
	if not encounter_variation_smoke():
		push_error("Regression failed: encounter_variation_smoke")
		return false
	if not stage_roster_smoke():
		push_error("Regression failed: stage_roster_smoke")
		return false
	if not hub_perks_smoke():
		push_error("Regression failed: hub_perks_smoke")
		return false
	if not difficulty_smoke():
		push_error("Regression failed: difficulty_smoke (healing, elites, bosses or game over)")
		return false
	begin(123,"dealer_cards")
	if state!="hub" or visited_count!=0 or dungeon.nodes[0].visited or not enemies.is_empty(): return false
	if large_room or room_bounds!=ARENA or room_regions.size()!=0 or exit_point!=EXIT: return false
	if boss_gate.has_area() or boss_gate_state!="open": return false
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
					player=exit_point
					if not open_route(): return false
				if not travel(index): return false
			if state=="fight":
				# Representative traversal tests the shared graph. Bespoke objective
				# behavior is tested independently in special_rooms_smoke().
				if room_kind in Dungeon.SPECIAL_COMBAT:
					complete_special_combat()
				for step in range(wave_total if room_kind not in Dungeon.SPECIAL_COMBAT else 0):
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
				var choice: int=3 if room_kind in ["shop","forge","forgotten_forge","archive"] else 2 if room_kind=="blood_altar" else 1 if room_kind in ["event","treasure","secret"] else 0
				if not service_action(choice) or state!="route" or service_action(choice): return false
		if not dungeon.boss_ready(): return false
		if state=="exit":
			player=exit_point
			if not open_route(): return false
		if not travel(Dungeon.BOSS): return false
		for unit in enemies: unit.hp=0
		tick(0.016,Vector2.ZERO,Vector2(700,385),false,false,false)
		if floor<2:
			if not choose_relic(0): return false
			player=exit_point
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
