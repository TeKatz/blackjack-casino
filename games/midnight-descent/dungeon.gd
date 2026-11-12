extends RefCounted
## Eight branching columns, three lanes, one entry and one boss.
const ROOM_COUNT := 26
const BOSS := 25
const MIN_FIGHTS := [6,7,7]
const MIN_ELITES := [0,1,2]
const SPECIAL_COMBAT := ["ritual","survival","bounty","unstable"]
const SPECIAL_SERVICE := ["blood_altar","forgotten_forge","archive"]
const ALL_COMBAT := ["fight","elite","ritual","survival","bounty","unstable"]
var nodes: Array[Dictionary] = []
var floor_index := 0
var layout_rotation := 0

func generate(seed_value: int, floor: int) -> void:
	floor_index=floor
	var rng:=RandomNumberGenerator.new()
	rng.seed=seed_value+floor*7919
	layout_rotation=rng.randi_range(0,3)
	nodes.clear()
	# Combat columns cannot be bypassed by any service route.
	# The opening service and the royal forge remain discoverable, but their
	# surrounding routes and the mandatory elite gauntlets shift every run.
	var service_columns: Array=[]
	var elite_columns: Array=[]
	match floor:
		0:
			service_columns=[2]
			service_columns.append_array(pick_columns([4,5,6,7],2,rng))
		1:
			service_columns=[2]
			service_columns.append_array(pick_columns([5,6,7],1,rng))
			var elite_pool: Array=[3,4,5,6,7,8]
			for col in service_columns: elite_pool.erase(col)
			elite_columns=pick_columns(elite_pool,1,rng)
		2:
			service_columns=[6]
			service_columns.append_array(pick_columns([2,3,5],1,rng))
			var elite_pool: Array=[3,4,5,7,8]
			for col in service_columns: elite_pool.erase(col)
			elite_columns=pick_columns(elite_pool,2,rng)
	var services: Array = ["treasure","shop","event","shrine","secret","blood_altar","forgotten_forge","archive"]
	if floor==2: services.append("forge")
	add_node("fight",0,1,rng)
	for column in range(1,9):
		var choices: Array = []
		if service_columns.has(column):
			var room_pool: Array=services.duplicate()
			for lane in range(3):
				choices.append(room_pool.pop_at(rng.randi_range(0,room_pool.size()-1)))
			# Guarantee one opportunity to use the old royal forge.
			if floor==2 and column==6: choices[1]="forge"
		elif elite_columns.has(column):
			choices=["elite","elite","elite"]
		else:
			choices=["fight","fight","elite" if rng.randf()<0.45 else "fight"]
			for lane in range(3):
				if choices[lane]=="fight" and rng.randf()<0.52:
					choices[lane]=SPECIAL_COMBAT[rng.randi_range(0,SPECIAL_COMBAT.size()-1)]
		for lane in range(3): add_node(choices[lane],column,lane,rng)
	add_node("boss",9,1,rng)
	for lane in range(3):
		link(0,1+lane)
		link(22+lane,BOSS)
	for column in range(1,8):
		for lane in range(3):
			var a: int =1+(column-1)*3+lane
			link(a,a+3)
			if rng.randf()<0.60:
				var other: int =lane+1 if lane<2 else lane-1
				link(a,1+column*3+other)
			if lane>0 and rng.randf()<0.35: link(a,1+column*3+lane-1)
	# Connect every lane without forcing a return to the entrance.
	for column in service_columns:
		var a: int =1+(column-1)*3
		link(a,a+1)
		link(a+1,a+2)

func pick_columns(source: Array, count: int, rng: RandomNumberGenerator) -> Array:
	var pool: Array=source.duplicate()
	var selected: Array=[]
	for i in range(count):
		selected.append(pool.pop_at(rng.randi_range(0,pool.size()-1)))
	return selected

func add_node(kind_name: String, column: int, lane: int, rng: RandomNumberGenerator) -> void:
	# Encounter profiles and danger levels are fixed by seed and visible on the map.
	# The first room stays a predictable introduction, not a random difficulty spike.
	var combat: bool=kind_name in ALL_COMBAT
	var profiles: Array[String]=["skirmish","crossfire","siege","onslaught"]
	var encounter: String=profiles[rng.randi_range(0,profiles.size()-1)] if combat else ""
	var threat: int=clampi(int((column+floor_index)/3)+rng.randi_range(-1,1),0,2) if combat else 0
	if kind_name=="elite": threat=maxi(1,threat)
	if column==0:
		encounter="skirmish"
		threat=0
	nodes.append({"kind":kind_name,"column":column,"lane":lane,"visited":false,"cleared":false,"links":[],"layout":posmod(column+lane+layout_rotation,4),"geometry_seed":rng.randi(),"encounter":encounter,"threat":threat})

func link(a: int,b: int) -> void:
	if not nodes[a].links.has(b): nodes[a].links.append(b)
	if not nodes[b].links.has(a): nodes[b].links.append(a)

func available(index: int) -> Array:
	return nodes[index].links

func kind(index: int) -> String:
	return nodes[index].kind

func cleared_fights() -> int:
	var count:=0
	for node in nodes:
		if node.cleared and node.kind in ALL_COMBAT: count+=1
	return count

func cleared_elites() -> int:
	var count:=0
	for node in nodes:
		if node.cleared and node.kind=="elite": count+=1
	return count

func boss_ready() -> bool:
	return cleared_fights()>=MIN_FIGHTS[floor_index] and cleared_elites()>=MIN_ELITES[floor_index]

func smoke_test() -> bool:
	# Verify every seed/floor has the necessary combat-only cuts and a route.
	for floor in range(3):
		for seed_value in range(24):
			generate(seed_value,floor)
			if nodes.size()!=ROOM_COUNT: return false
			if nodes[0].encounter!="skirmish" or nodes[0].threat!=0: return false
			for node in nodes:
				if node.kind in ALL_COMBAT and (node.encounter not in ["skirmish","crossfire","siege","onslaught"] or node.threat<0 or node.threat>2): return false
				if node.kind=="elite" and node.threat<1: return false
			var seen: Array=[0]
			var cursor:=0
			while cursor<seen.size():
				for neighbor in nodes[seen[cursor]].links:
					if not seen.has(neighbor): seen.append(neighbor)
				cursor+=1
			if seen.size()!=ROOM_COUNT or boss_ready(): return false
			var fights:=0
			var elites:=0
			for column in range(9):
				var all_combat:=true
				var all_elite:=true
				for node in nodes:
					if node.column==column:
						all_combat=all_combat and node.kind in ALL_COMBAT
						all_elite=all_elite and node.kind=="elite"
				if all_combat: fights+=1
				if all_elite: elites+=1
			if fights<MIN_FIGHTS[floor] or elites<MIN_ELITES[floor]: return false
	# Dedicated room types must actually appear across generated runs, not
	# remain unreachable descriptions in the room-type registry.
	var encountered: Dictionary={}
	for floor in range(3):
		for seed_value in range(30,70):
			generate(seed_value,floor)
			for node in nodes:
				if node.kind in SPECIAL_COMBAT or node.kind in SPECIAL_SERVICE:
					encountered[node.kind]=true
	for id in SPECIAL_COMBAT+SPECIAL_SERVICE:
		if not encountered.has(id): return false
	return true
