extends RefCounted
## Seeded connected room graph. Every edge is bidirectional and the boss is reachable.
const KINDS = ["fight","fight","treasure","elite","shop","event","shrine","fight","secret","boss"]
var nodes: Array[Dictionary] = []
func generate(seed_value: int, floor_index: int) -> void:
	var rng := RandomNumberGenerator.new()
	rng.seed = seed_value + floor_index*7919
	nodes.clear()
	for i in range(10):
		nodes.append({"kind":"forge" if floor_index==2 and i==6 else KINDS[i],"visited":false,"cleared":false,"links":[],"reward":false,"layout":rng.randi_range(0,3),"geometry_seed":rng.randi()})
	var edges := [[0,1],[0,2],[1,3],[2,4],[3,5],[4,6],[5,7],[6,7],[7,9],[5,8],[8,9]]
	if rng.randf()<0.5: edges.append([1,4])
	else: edges.append([2,3])
	if rng.randf()<0.5: edges.append([6,8])
	for edge in edges:
		nodes[edge[0]].links.append(edge[1])
		nodes[edge[1]].links.append(edge[0])
func available(index: int) -> Array:
	return nodes[index].links
func kind(index: int) -> String:
	return nodes[index].kind
