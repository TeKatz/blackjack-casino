extends RefCounted
## Deterministic, renderer-independent local power networks.
## Wire nodes auto-connect inside a 5-cell radius. Producers and consumers
## never share power across physically disconnected networks.
const LINK_RANGE := 5.0
const GENERATORS := {"water_wheel":{"kw":30, "slots":1}}
const CONSUMERS := {"smelter":10, "constructor":15}
const CONNECTORS := ["power_pole", "water_wheel"]
var slot_limit := 0
var slot_used := 0
var total_supply := 0
var total_demand := 0
var total_powered := 0
var powered: Dictionary = {}
var network_for: Dictionary = {}
var edges: Array = []
var networks: Array[Dictionary] = []

static func slots(kind: String) -> int:
	return int(GENERATORS.get(kind, {}).get("slots", 0))

static func generation(kind: String) -> int:
	return int(GENERATORS.get(kind, {}).get("kw", 0))

static func demand(kind: String) -> int:
	return int(CONSUMERS.get(kind, 0))

func can_add_generator(kind: String) -> bool:
	return slots(kind) == 0 or slot_used + slots(kind) <= slot_limit

func is_powered(cell: Vector2i) -> bool:
	return bool(powered.get(cell, false))

func status(cell: Vector2i) -> String:
	# Consumers need to distinguish no generator/connection from a real
	# overloaded grid. Visuals and inspector share the same source of truth.
	if not powered.has(cell):
		return "not_required"
	if bool(powered[cell]):
		return "ok"
	if not network_for.has(cell):
		return "no_power"
	var index: int = int(network_for[cell])
	if index < 0 or index >= networks.size():
		return "no_power"
	return "no_power" if int(networks[index].supply) == 0 else "insufficient"

func network_stats(cell: Vector2i) -> Dictionary:
	if not network_for.has(cell):
		return {"supply":0, "demand":0, "used":0}
	var index: int = int(network_for[cell])
	if index < 0 or index >= networks.size():
		return {"supply":0, "demand":0, "used":0}
	var network: Dictionary = networks[index]
	return {"supply":int(network.supply), "demand":int(network.requested),
		"used":int(network.used)}

func _near(a: Vector2i, b: Vector2i) -> bool:
	return (Vector2(a) - Vector2(b)).length_squared() <= LINK_RANGE * LINK_RANGE

func _cell_less(a: Vector2i, b: Vector2i) -> bool:
	return a.y < b.y if a.y != b.y else a.x < b.x

func rebuild(buildings: Dictionary, max_slots: int) -> void:
	slot_limit = max_slots
	slot_used = 0
	total_supply = 0
	total_demand = 0
	total_powered = 0
	powered.clear()
	network_for.clear()
	edges.clear()
	networks.clear()
	var nodes: Array[Vector2i] = []
	var consumers: Array[Vector2i] = []
	for at in buildings:
		var kind: String = str(buildings[at].kind)
		slot_used += slots(kind)
		if kind in CONNECTORS:
			nodes.append(at)
		elif demand(kind) > 0:
			consumers.append(at)
			total_demand += demand(kind)
	nodes.sort_custom(_cell_less)
	consumers.sort_custom(_cell_less)
	# Build connected electrical components using generators and masts.
	# Runtime is O(power nodes²) only on building changes, not every tick.
	var visited: Dictionary = {}
	for start in nodes:
		if visited.has(start):
			continue
		var component := {"nodes": [], "supply":0, "used":0, "requested":0}
		var queue: Array[Vector2i] = [start]
		visited[start] = true
		var cursor := 0
		while cursor < queue.size():
			var a: Vector2i = queue[cursor]
			cursor += 1
			component.nodes.append(a)
			component.supply += generation(str(buildings[a].kind))
			for b in nodes:
				if visited.has(b) or not _near(a, b):
					continue
				visited[b] = true
				queue.append(b)
				edges.append([a, b])
		var group_id := networks.size()
		for cell in component.nodes:
			network_for[cell] = group_id
		total_supply += int(component.supply)
		networks.append(component)
	# A consumer connects to precisely one nearby network. Assign
	# deterministic nearest-connector priority, then reserve its full 10kW.
	# Insufficient power stops the entire smelter, never half-processes it.
	for at in consumers:
		var best_group := -1
		var best_distance := 1.0e20
		for index in range(networks.size()):
			for node in networks[index].nodes:
				var distance: float = (Vector2(at) - Vector2(node)).length_squared()
				if distance <= LINK_RANGE * LINK_RANGE and distance < best_distance:
					best_distance = distance
					best_group = index
		if best_group < 0:
			powered[at] = false
			continue
		network_for[at] = best_group
		var circuit: Dictionary = networks[best_group]
		var need: int = demand(str(buildings[at].kind))
		circuit.requested += need
		if int(circuit.used) + need <= int(circuit.supply):
			circuit.used += need
			total_powered += need
			powered[at] = true
		else:
			powered[at] = false
