extends RefCounted
const Construction = preload("res://factory_construction.gd")
## Authoritative, renderer-independent fixed-step factory model.
## Machines are compact dictionaries keyed by cell; no per-item scene nodes.
const WIDTH := 128
const HEIGHT := 96
const STEP := 0.25
const DIRS := [Vector2i.RIGHT, Vector2i.DOWN, Vector2i.LEFT, Vector2i.UP]
const SAVE_VERSION := 1
const SAVE_FILE := "user://midnight-industries-v1.json"
const MAX_BUFFER := 8

var construction
var resources: Dictionary = {}
var buildings: Dictionary = {}
var delivered: Dictionary = {"iron_ingot": 0, "copper_ingot": 0, "iron_ore": 0, "copper_ore": 0}
var steps := 0
var accumulator := 0.0
## Renderer-only movement trace, not part of the save or authority model.
var transit: Dictionary = {}
var cached_cells: Array = []
var cells_dirty := true

func _init(with_starter: bool = true) -> void:
	construction = Construction.new()
	_seed_resources()
	if with_starter:
		# A functioning introductory iron line, intended to be extended.
		place(Vector2i(16, 17), "miner", 0)
		for x in range(17, 21):
			place(Vector2i(x, 17), "belt", 0)
		place(Vector2i(21, 17), "smelter", 0)
		for x in range(22, 25):
			place(Vector2i(x, 17), "belt", 0)
		place(Vector2i(25, 17), "storage", 0)

func _seed_resources() -> void:
	resources.clear()
	var patches := [
		{"pos": Vector2i(16, 17), "radius": 5, "kind": "iron"},
		{"pos": Vector2i(36, 22), "radius": 6, "kind": "copper"},
		{"pos": Vector2i(61, 47), "radius": 8, "kind": "iron"},
		{"pos": Vector2i(82, 65), "radius": 7, "kind": "copper"},
		{"pos": Vector2i(107, 28), "radius": 6, "kind": "iron"},
	]
	for patch in patches:
		var center: Vector2i = patch.pos
		var radius: int = patch.radius
		for y in range(center.y - radius, center.y + radius + 1):
			for x in range(center.x - radius, center.x + radius + 1):
				var cell := Vector2i(x, y)
				if not inside(cell):
					continue
				var distance := cell.distance_squared_to(center)
				if distance <= radius * radius and (distance < 5 or (x * 17 + y * 31) % 11 != 0):
					resources[cell] = patch.kind

func inside(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < WIDTH and cell.y < HEIGHT

func can_place(cell: Vector2i, kind: String) -> bool:
	# The casino foundation is permanent terrain. Existing older-save
	# machines inside its footprint are still restored and kept working,
	# but no new machines can be placed on the protected build site.
	if not inside(cell) or construction.inside(cell) or buildings.has(cell):
		return false
	if not construction.is_machine_unlocked(kind):
		return false
	if kind == "miner":
		return resources.has(cell)
	# Logistics can be built across a deposit; only miners extract it.
	return kind in ["belt", "smelter", "storage"]

func place(cell: Vector2i, kind: String, direction: int) -> bool:
	if not can_place(cell, kind):
		return false
	buildings[cell] = {
		"kind": kind, "direction": posmod(direction, 4), "item": "",
		"buffer": 0, "ore": 0, "output": 0,
		"recipe": "", "progress": 0.0,
	}
	cells_dirty = true
	return true

func erase(cell: Vector2i) -> bool:
	if not buildings.has(cell):
		return false
	buildings.erase(cell)
	transit.erase(cell)
	cells_dirty = true
	return true

func rotate(cell: Vector2i) -> bool:
	if not buildings.has(cell):
		return false
	buildings[cell].direction = (int(buildings[cell].direction) + 1) % 4
	return true

func advance(delta: float) -> void:
	accumulator += minf(maxf(delta, 0.0), 0.5)
	while accumulator >= STEP:
		accumulator -= STEP
		_step()

func _sorted_cells() -> Array:
	if cells_dirty:
		cached_cells = buildings.keys()
		cached_cells.sort_custom(_less_cell)
		cells_dirty = false
	return cached_cells

func _less_cell(a: Vector2i, b: Vector2i) -> bool:
	if a.y == b.y:
		return a.x < b.x
	return a.y < b.y

func _outgoing(cell: Vector2i, machine: Dictionary) -> String:
	match machine.kind:
		"miner":
			if machine.buffer > 0:
				return str(resources.get(cell, "")) + "_ore"
		"belt":
			return str(machine.item)
		"smelter":
			if machine.output > 0:
				return str(machine.recipe) + "_ingot"
	return ""

func _can_receive(machine: Dictionary, item: String) -> bool:
	match machine.kind:
		"belt":
			return machine.item == ""
		"smelter":
			if not item in ["iron_ore", "copper_ore"]:
				return false
			if not construction.is_recipe_unlocked(item.trim_suffix("_ore") + "_ingot"):
				return false
			return machine.ore < MAX_BUFFER and (machine.recipe == "" or machine.recipe == item.trim_suffix("_ore"))
		"storage":
			return delivered.has(item)
	return false

func _remove_output(machine: Dictionary) -> void:
	match machine.kind:
		"miner":
			machine.buffer -= 1
		"belt":
			machine.item = ""
		"smelter":
			machine.output -= 1

func _receive(machine: Dictionary, item: String) -> void:
	match machine.kind:
		"belt":
			machine.item = item
		"smelter":
			machine.recipe = item.trim_suffix("_ore")
			machine.ore += 1
		"storage":
			delivered[item] = int(delivered[item]) + 1

func _step() -> void:
	steps += 1
	transit.clear()
	var cells := _sorted_cells()
	# Production is independent of visual framerate and camera visibility.
	for cell in cells:
		var machine: Dictionary = buildings[cell]
		match machine.kind:
			"miner":
				if machine.buffer < MAX_BUFFER:
					machine.progress += STEP
					if machine.progress >= 1.0:
						machine.progress -= 1.0
						machine.buffer += 1
			"smelter":
				if machine.ore > 0 and machine.output < MAX_BUFFER:
					machine.progress += STEP
					if machine.progress >= 2.0:
						machine.progress -= 2.0
						machine.ore -= 1
						machine.output += 1
				else:
					machine.progress = 0.0
					if machine.ore == 0 and machine.output == 0:
						machine.recipe = ""
	# Two-phase transport: items can move only one belt per tick, regardless
	# of dictionary iteration order; each destination accepts at most one.
	var moves: Array = []
	var reserved: Dictionary = {}
	var reserved_for_site: Dictionary = {}
	for cell in cells:
		var source: Dictionary = buildings[cell]
		var item := _outgoing(cell, source)
		if item == "":
			continue
		var next: Vector2i = cell + DIRS[int(source.direction)]
		# 48 physical site intakes: any inward-facing output adjacent to an
		# outer edge can deliver when that phase explicitly requests the item.
		# Empty requirements intentionally do not consume anything.
		if not buildings.has(next) and construction.is_input(
			next, cell, int(source.direction)):
			var already: int = int(reserved_for_site.get(item, 0))
			if construction.can_accept(item, already):
				moves.append({"from": cell, "to": next, "item": item, "site": true})
				reserved_for_site[item] = already + 1
			continue
		if reserved.has(next) or not buildings.has(next):
			continue
		var destination: Dictionary = buildings[next]
		if not _can_receive(destination, item):
			continue
		moves.append({"from": cell, "to": next, "item": item})
		reserved[next] = true
	for move in moves:
		_remove_output(buildings[move["from"]])
	for move in moves:
		if bool(move.get("site", false)):
			construction.receive(str(move.item))
			continue
		_receive(buildings[move["to"]], move["item"])
		if buildings[move["to"]].kind == "belt":
			transit[move["to"]] = {"from": move["from"], "item": move["item"]}

func snapshot() -> Dictionary:
	var machines: Array = []
	for cell in _sorted_cells():
		var b: Dictionary = buildings[cell]
		machines.append({
			"x": cell.x, "y": cell.y, "kind": b.kind, "direction": b.direction,
			"item": b.item, "buffer": b.buffer, "ore": b.ore,
			"output": b.output, "recipe": b.recipe, "progress": b.progress,
		})
	return {"version": SAVE_VERSION, "steps": steps, "delivered": delivered.duplicate(),
		"machines": machines, "construction": construction.snapshot()}

func restore(data: Variant) -> bool:
	if typeof(data) != TYPE_DICTIONARY or data.get("version", 0) != SAVE_VERSION:
		return false
	var entries: Variant = data.get("machines", null)
	var totals: Variant = data.get("delivered", null)
	if typeof(entries) != TYPE_ARRAY or typeof(totals) != TYPE_DICTIONARY or entries.size() > WIDTH * HEIGHT:
		return false
	var loaded: Dictionary = {}
	for raw in entries:
		if typeof(raw) != TYPE_DICTIONARY:
			return false
		var cell := Vector2i(int(raw.get("x", -1)), int(raw.get("y", -1)))
		var kind := str(raw.get("kind", ""))
		if not inside(cell) or loaded.has(cell):
			return false
		if kind == "miner" and not resources.has(cell):
			return false
		if kind != "miner" and not kind in ["belt", "smelter", "storage"]:
			return false
		var direction := int(raw.get("direction", -1))
		if direction < 0 or direction >= 4:
			return false
		var item := str(raw.get("item", ""))
		var recipe := str(raw.get("recipe", ""))
		if not item in ["", "iron_ore", "copper_ore", "iron_ingot", "copper_ingot"]:
			return false
		if not recipe in ["", "iron", "copper"]:
			return false
		var buffer := int(raw.get("buffer", 0))
		var ore := int(raw.get("ore", 0))
		var output := int(raw.get("output", 0))
		var progress := float(raw.get("progress", 0.0))
		if buffer < 0 or buffer > MAX_BUFFER or ore < 0 or ore > MAX_BUFFER or output < 0 or output > MAX_BUFFER or not is_finite(progress) or progress < 0.0 or progress > 2.0:
			return false
		loaded[cell] = {
			"kind": kind, "direction": direction, "item": item,
			"buffer": buffer, "ore": ore, "output": output,
			"recipe": recipe, "progress": progress,
		}
	var restored_construction = Construction.new()
	if not restored_construction.restore(data.get("construction", {})):
		return false
	var new_totals := {}
	for key in delivered.keys():
		var amount := int(totals.get(key, 0))
		if amount < 0:
			return false
		new_totals[key] = amount
	construction = restored_construction
	buildings = loaded
	cells_dirty = true
	transit.clear()
	delivered = new_totals
	steps = maxi(0, int(data.get("steps", 0)))
	accumulator = 0.0
	return true

func save_game() -> bool:
	var path := ProjectSettings.globalize_path(SAVE_FILE)
	var temporary := path + ".tmp"
	var backup := path + ".bak"
	var file := FileAccess.open(temporary, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(snapshot()))
	file.flush()
	file.close()
	if FileAccess.file_exists(backup):
		DirAccess.remove_absolute(backup)
	if FileAccess.file_exists(path) and DirAccess.rename_absolute(path, backup) != OK:
		return false
	if DirAccess.rename_absolute(temporary, path) != OK:
		if FileAccess.file_exists(backup):
			DirAccess.rename_absolute(backup, path)
		return false
	return true

func load_game() -> bool:
	var candidates := [SAVE_FILE, SAVE_FILE + ".bak"]
	for path in candidates:
		if not FileAccess.file_exists(path):
			continue
		var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
		if restore(parsed):
			return true
	return false

func counts() -> Dictionary:
	var totals := {"miner": 0, "belt": 0, "smelter": 0, "storage": 0}
	for building in buildings.values():
		totals[building.kind] += 1
	return totals
