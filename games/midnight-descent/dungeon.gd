extends RefCounted
## Eight branching columns, three lanes, one entry and one boss.
const ROOM_COUNT := 26
const BOSS := 25
const MIN_FIGHTS := [6,7,7]
const MIN_ELITES := [0,1,2]
var nodes: Array[Dictionary] = []
var floor_index := 0

func generate(seed_value: int, floor: int) -> void:
	floor_index=floor
	var rng:=RandomNumberGenerator.new()
	rng.seed=seed_value+floor*7919
	nodes.clear()
	# Combat columns cannot be bypassed by any service route.
	var service_columns: Array = [2,5,7] if floor==0 else [2,6]
	var elite_columns: Array = [4] if floor==1 else ([4,7] if floor==2 else [])
	var services: Array = ["treasure","shop","event","shrine","secret"]
	if floor==2: services.append("forge")
	add_node("fight",0,1,rng)
	for column in range(1,9):
		var choices: Array = []
		if service_columns.has(column):
			var offset:=rng.randi_range(0,services.size()-1)
			for lane in range(3): choices.append(services[(offset+lane)%services.size()])
			# Guarantee one opportunity to use the royal forge.
			if floor==2 and column==6: choices[1]="forge"
		elif elite_columns.has(column):
			choices=["elite","elite","elite"]
		else:
			choices=["fight","fight","elite" if rng.randf()<0.45 else "fight"]
		for lane in range(3): add_node(choices[lane],column,lane,rng)
	add_node("boss",9,1,rng)
	for lane in range(3):
		link(0,1+lane)
		link(22+lane,BOSS)
	for column in range(1,8):
		for lane in range(3):
			var a:=1+(column-1)*3+lane
			link(a,a+3)
			if rng.randf()<0.60:
				var other:=lane+1 if lane<2 else lane-1
				link(a,1+column*3+other)
			if lane>0 and rng.randf()<0.35: link(a,1+column*3+lane-1)
	# Connect every lane without forcing a return to the entrance.
	for column in [2,5,7]:
		var a:=1+(column-1)*3
		link(a,a+1)
		link(a+1,a+2)

func add_node(kind_name: String, column: int, lane: int, rng: RandomNumberGenerator) -> void:
	nodes.append({"kind":kind_name,"column":column,"lane":lane,"visited":false,"cleared":false,"links":[],"layout":rng.randi_range(0,3),"geometry_seed":rng.randi()})

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
		if node.cleared and node.kind in ["fight","elite"]: count+=1
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
						all_combat=all_combat and node.kind in ["fight","elite"]
						all_elite=all_elite and node.kind=="elite"
				if all_combat: fights+=1
				if all_elite: elites+=1
			if fights<MIN_FIGHTS[floor] or elites<MIN_ELITES[floor]: return false
	return true
