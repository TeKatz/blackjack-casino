extends RefCounted
const Construction = preload("res://factory_construction.gd")
const World = preload("res://factory_world.gd")
const Economy = preload("res://factory_economy.gd")
const Progression = preload("res://factory_progression.gd")
const Power = preload("res://factory_power.gd")
## Authoritative, renderer-independent fixed-step factory model.
## Machines are compact dictionaries keyed by cell; no per-item scene nodes.
const WIDTH := 224
const HEIGHT := 176
const STEP := 0.25
const DIRS := [Vector2i.RIGHT, Vector2i.DOWN, Vector2i.LEFT, Vector2i.UP]
const SAVE_VERSION := 1
const SAVE_FILE := "user://midnight-industries-v1.json"
const MAX_BUFFER := 8
const WAREHOUSE_CAPACITY := 64

var construction
var economy
var progression
var power
var power_dirty := true
var resources: Dictionary = {}
var water: Dictionary = {}
var buildings: Dictionary = {}
## Secondary occupied cells of multi-tile machines, mapped to land anchors.
var occupied: Dictionary = {}
var delivered: Dictionary = {"iron_ingot": 0, "copper_ingot": 0, "gold_ingot": 0,
	"iron_ore": 0, "copper_ore": 0, "gold_ore": 0}
var steps := 0
var accumulator := 0.0
## Renderer-only movement trace, not part of the save or authority model.
var transit: Dictionary = {}
var cached_cells: Array = []
var cells_dirty := true

func _init(with_starter: bool = false) -> void:
	construction = Construction.new()
	economy = Economy.new()
	progression = Progression.new()
	power = Power.new()
	_seed_resources()
	if with_starter:
		# Optional tutorial/test layout: a raw-ore delivery line only.
		# Fresh user worlds deliberately have NO premade machines.
		place(Vector2i(16, 17), "miner", 0, true)
		for x in range(17, 21):
			place(Vector2i(x, 17), "belt", 0, true)
		place(Vector2i(21, 17), "depot", 0, true)

func _seed_resources() -> void:
	# One deterministic geography source drives ore, water, collisions and map.
	World.populate(resources, water)

func inside(cell: Vector2i) -> bool:
	return cell.x >= 0 and cell.y >= 0 and cell.x < WIDTH and cell.y < HEIGHT

func is_unlocked(kind: String) -> bool:
	return progression.unlocked(kind) and construction.is_machine_unlocked(kind) \
		and economy.registered(kind)

func is_recipe_unlocked(recipe: String) -> bool:
	return progression.recipe_unlocked(recipe) and construction.is_recipe_unlocked(recipe)

## One stable anchor on land and a second occupied tile extending into
## flowing water. Rotation 0/1/2/3 points the wheel to right/down/left/up.
## Other machines retain their original single-cell footprints.
func wheel_water_cell(anchor: Vector2i, direction: int) -> Vector2i:
	return anchor + DIRS[posmod(direction, 4)]

func footprint_cells(anchor: Vector2i, kind: String,
	direction: int = 0) -> Array[Vector2i]:
	var result: Array[Vector2i] = [anchor]
	if kind == "water_wheel":
		result.append(wheel_water_cell(anchor, direction))
	return result

func anchor_at(cell: Vector2i) -> Vector2i:
	if buildings.has(cell):
		return cell
	return occupied.get(cell, Vector2i(-1, -1))

func _wheel_water_valid(cell: Vector2i, direction: int,
	ignore_owner: Vector2i = Vector2i(-1, -1)) -> bool:
	var water_cell: Vector2i = wheel_water_cell(cell, direction)
	if not inside(water_cell) or not water.has(water_cell) \
		or str(water[water_cell].get("kind", "")) != "river":
		return false
	if buildings.has(water_cell):
		return false
	return not occupied.has(water_cell) or occupied[water_cell] == ignore_owner

func _update_power() -> void:
	# Earning a power slot must take effect immediately, even if the
	# milestone was completed while the simulation was paused.
	if power.slot_limit != progression.power_slots:
		power_dirty = true
	if not power_dirty:
		return
	power.rebuild(buildings, progression.power_slots)
	power_dirty = false

func placement_issue(cell: Vector2i, kind: String, direction: int = 0) -> String:
	if not is_unlocked(kind):
		return "locked"
	if not inside(cell) or construction.inside(cell) or water.has(cell) \
		or buildings.has(cell) or occupied.has(cell):
		return "blocked"
	if kind == "water_wheel" and not _wheel_water_valid(cell, direction):
		return "river"
	if kind == "miner" and not resources.has(cell):
		return "ore"
	if Power.slots(kind) > 0:
		_update_power()
		if not power.can_add_generator(kind):
			return "slots"
	return ""

func can_place_site(cell: Vector2i, kind: String,
	direction: int = 0) -> bool:
	return placement_issue(cell, kind, direction).is_empty()

func can_place(cell: Vector2i, kind: String, direction: int = 0) -> bool:
	return can_place_site(cell, kind, direction)

func building_cost(kind: String) -> Dictionary:
	return economy.cost(kind)

func missing_materials(kind: String) -> Dictionary:
	return economy.missing(kind)

func _make_machine(kind: String, direction: int, paid: Dictionary = {},
	free_kit: bool = false) -> Dictionary:
	return {
		"kind": kind, "direction": posmod(direction, 4), "item": "",
		"buffer": 0, "ore": 0, "output": 0,
		"recipe": "", "progress": 0.0,
		"inventory": {}, "paid": paid.duplicate(), "kit": free_kit
	}

func place(cell: Vector2i, kind: String, direction: int,
	bootstrap: bool = false) -> bool:
	if not can_place_site(cell, kind, direction):
		return false
	buildings[cell] = _make_machine(kind, direction, {}, bootstrap)
	if kind == "water_wheel":
		occupied[wheel_water_cell(cell, direction)] = cell
	cells_dirty = true
	power_dirty = true
	return true

func erase(cell: Vector2i) -> bool:
	var anchor: Vector2i = anchor_at(cell)
	if not buildings.has(anchor):
		return false
	var machine: Dictionary = buildings[anchor]
	# Legacy paid amounts are refunded exactly once; new buildings are free.
	economy.refund(machine.get("paid", {}))
	if str(machine.kind) == "water_wheel":
		occupied.erase(wheel_water_cell(anchor, int(machine.direction)))
	buildings.erase(anchor)
	transit.erase(anchor)
	cells_dirty = true
	power_dirty = true
	return true

func rotate(cell: Vector2i) -> bool:
	var anchor: Vector2i = anchor_at(cell)
	if not buildings.has(anchor):
		return false
	var machine: Dictionary = buildings[anchor]
	if str(machine.kind) == "water_wheel":
		var old_direction: int = int(machine.direction)
		for offset in range(1, 4):
			var next_direction: int = (old_direction + offset) % 4
			if _wheel_water_valid(anchor, next_direction, anchor):
				occupied.erase(wheel_water_cell(anchor, old_direction))
				occupied[wheel_water_cell(anchor, next_direction)] = anchor
				machine.direction = next_direction
				cells_dirty = true
				power_dirty = true
				return true
		return false
	machine.direction = (int(machine.direction) + 1) % 4
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

func _warehouse_item(machine: Dictionary) -> String:
	var items: Dictionary = machine.get("inventory", {})
	for material in Economy.MATERIALS:
		if int(items.get(material, 0)) > 0:
			return material
	return ""

func _warehouse_load(machine: Dictionary) -> int:
	var total := 0
	for quantity in machine.get("inventory", {}).values():
		total += int(quantity)
	return total

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
		"storage":
			return _warehouse_item(machine)
	return ""

func _can_receive(machine: Dictionary, item: String) -> bool:
	match machine.kind:
		"belt":
			return machine.item == ""
		"smelter":
			if not item in ["iron_ore", "copper_ore", "gold_ore"]:
				return false
			if not is_recipe_unlocked(item.trim_suffix("_ore") + "_ingot"):
				return false
			return machine.ore < MAX_BUFFER and (machine.recipe == "" or machine.recipe == item.trim_suffix("_ore"))
		"storage":
			return item in Economy.MATERIALS \
				and _warehouse_load(machine) < WAREHOUSE_CAPACITY
		"depot":
			return item in Economy.MATERIALS
	return false

func _remove_output(machine: Dictionary) -> void:
	match machine.kind:
		"miner":
			machine.buffer -= 1
		"belt":
			machine.item = ""
		"smelter":
			machine.output -= 1
		"storage":
			var item: String = _warehouse_item(machine)
			if item != "":
				var inventory: Dictionary = machine["inventory"]
				inventory[item] = int(inventory.get(item, 0)) - 1
				if int(inventory[item]) <= 0:
					inventory.erase(item)

func _receive(machine: Dictionary, item: String) -> void:
	match machine.kind:
		"belt":
			machine.item = item
		"smelter":
			machine.recipe = item.trim_suffix("_ore")
			machine.ore += 1
		"storage":
			var inventory: Dictionary = machine["inventory"]
			inventory[item] = int(inventory.get(item, 0)) + 1
		"depot":
			# Only an actual physical delivery creates spendable inventory.
			if economy.credit(item):
				delivered[item] = int(delivered.get(item, 0)) + 1
				progression.record_delivery(item)
				power_dirty = true

func _step() -> void:
	steps += 1
	transit.clear()
	_update_power()
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
				if not power.is_powered(cell):
					continue
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
			"inventory": b.get("inventory", {}).duplicate(),
			"paid": b.get("paid", {}).duplicate(),
			"kit": bool(b.get("kit", false)),
		})
	return {"version": SAVE_VERSION, "wheel_footprint_version": 2,
		"steps": steps, "delivered": delivered.duplicate(),
		"machines": machines, "construction": construction.snapshot(),
		"economy": economy.snapshot(), "progression": progression.snapshot()}

func restore(data: Variant) -> bool:
	if typeof(data) != TYPE_DICTIONARY or data.get("version", 0) != SAVE_VERSION:
		return false
	var entries: Variant = data.get("machines", null)
	var totals: Variant = data.get("delivered", null)
	if typeof(entries) != TYPE_ARRAY or typeof(totals) != TYPE_DICTIONARY or entries.size() > WIDTH * HEIGHT:
		return false
	var loaded: Dictionary = {}
	# Preserve established miners from the old map even after their big
	# mineral fields were moved. Failed/corrupt saves remain transactional.
	var migrated_resources: Dictionary = resources.duplicate()
	var migrated_water: Dictionary = water.duplicate()
	for raw in entries:
		if typeof(raw) != TYPE_DICTIONARY:
			return false
		var cell := Vector2i(int(raw.get("x", -1)), int(raw.get("y", -1)))
		var kind := str(raw.get("kind", ""))
		if not inside(cell) or loaded.has(cell):
			return false
		if kind == "miner" and not migrated_resources.has(cell):
			if not World.restore_legacy_miner(cell, migrated_resources, migrated_water):
				return false
		if kind != "miner" and not kind in [
			"belt", "smelter", "storage", "depot", "power_pole", "water_wheel"]:
			return false
		var direction := int(raw.get("direction", -1))
		if direction < 0 or direction >= 4:
			return false
		var item := str(raw.get("item", ""))
		var recipe := str(raw.get("recipe", ""))
		if not item in ["", "iron_ore", "copper_ore", "gold_ore",
			"iron_ingot", "copper_ingot", "gold_ingot"]:
			return false
		if not recipe in ["", "iron", "copper", "gold"]:
			return false
		var buffer := int(raw.get("buffer", 0))
		var ore := int(raw.get("ore", 0))
		var output := int(raw.get("output", 0))
		var progress := float(raw.get("progress", 0.0))
		if buffer < 0 or buffer > MAX_BUFFER or ore < 0 or ore > MAX_BUFFER or output < 0 or output > MAX_BUFFER or not is_finite(progress) or progress < 0.0 or progress > 2.0:
			return false
		var inventory: Variant = raw.get("inventory", {})
		var paid: Variant = raw.get("paid", {})
		if typeof(inventory) != TYPE_DICTIONARY or typeof(paid) != TYPE_DICTIONARY:
			return false
		var stored: Dictionary = {}
		var total_stored := 0
		for material in inventory:
			if not material in Economy.MATERIALS:
				return false
			var count: Variant = inventory[material]
			if (typeof(count) != TYPE_INT and typeof(count) != TYPE_FLOAT) \
				or not is_finite(float(count)) or float(count) < 0.0 \
				or float(count) > float(WAREHOUSE_CAPACITY) \
				or float(int(count)) != float(count):
				return false
			total_stored += int(count)
			stored[material] = int(count)
		if (kind != "storage" and not stored.is_empty()) or total_stored > WAREHOUSE_CAPACITY:
			return false
		var actual_paid: Dictionary = {}
		# Save the amount actually paid at construction time. Do not force
		# the present-day price: future balancing must not break old saves.
		for material in paid:
			var paid_amount: Variant = paid[material]
			if not material in Economy.MATERIALS \
				or (typeof(paid_amount) != TYPE_INT and typeof(paid_amount) != TYPE_FLOAT) \
				or not is_finite(float(paid_amount)) or float(paid_amount) < 0.0 \
				or float(paid_amount) > 1000000.0 \
				or float(int(paid_amount)) != float(paid_amount):
				return false
			actual_paid[material] = int(paid_amount)
		# Legacy unpaid buildings act as movable kits instead of
		# granting raw materials on demolition.
		var kit: Variant = raw.get("kit", actual_paid.is_empty())
		if typeof(kit) != TYPE_BOOL or (bool(kit) and not actual_paid.is_empty()):
			return false
		loaded[cell] = {
			"kind": kind, "direction": direction, "item": item,
			"buffer": buffer, "ore": ore, "output": output,
			"recipe": recipe, "progress": progress,
			"inventory": stored, "paid": actual_paid, "kit": bool(kit)
		}
	var restored_construction = Construction.new()
	if not restored_construction.restore(data.get("construction", {})):
		return false
	var restored_economy = Economy.new()
	var has_wallet: bool = data.has("economy")
	if has_wallet:
		if not restored_economy.restore(data["economy"]):
			return false
	else:
		restored_economy.restore_legacy(totals)
		# Old warehouses used to consume items directly into delivered totals.
		# Keep their current layout and attach one free depot to a warehouse
		# outlet when possible. Otherwise preserve a free placement voucher.
		for cell in loaded.keys():
			if str(loaded[cell]["kind"]) != "storage":
				continue
			var after: Vector2i = cell + DIRS[int(loaded[cell]["direction"])]
			if inside(after) and not loaded.has(after) and not migrated_water.has(after) \
				and not restored_construction.inside(after):
				loaded[after] = _make_machine("depot", 0, {}, true)
				restored_economy.free_depot_available = false
				break
	var restored_progression = Progression.new()
	if data.has("progression"):
		if not restored_progression.restore(data["progression"]):
			return false
	else:
		var existing_furnace := false
		for saved_machine in loaded.values():
			if str(saved_machine.kind) == "smelter":
				existing_furnace = true
				break
		restored_progression.restore_legacy(existing_furnace)
	# Version-1 saves placed the wheel on a single land cell next to any
	# river. Migrate their stored facing to an actual adjacent river tile.
	# In the new schema the orientation and the complete footprint are
	# authoritative; reject invalid or overlapping footprints before commit.
	var new_occupied: Dictionary = {}
	var footprint_version: int = int(data.get("wheel_footprint_version", 1))
	if footprint_version < 1 or footprint_version > 2:
		return false
	var wheel_anchors: Array = loaded.keys()
	wheel_anchors.sort_custom(_less_cell)
	for anchor in wheel_anchors:
		var saved: Dictionary = loaded[anchor]
		if str(saved.kind) != "water_wheel":
			continue
		if migrated_water.has(anchor):
			return false
		var original_direction: int = int(saved.direction)
		var directions: Array[int] = [original_direction]
		if footprint_version == 1:
			for candidate_direction in range(4):
				if not directions.has(candidate_direction):
					directions.append(candidate_direction)
		var accepted := false
		for direction in directions:
			var river_cell: Vector2i = wheel_water_cell(anchor, direction)
			if inside(river_cell) and migrated_water.has(river_cell) \
				and str(migrated_water[river_cell].get("kind", "")) == "river" \
				and not loaded.has(river_cell) and not new_occupied.has(river_cell):
				new_occupied[river_cell] = anchor
				saved.direction = direction
				accepted = true
				break
		if not accepted:
			return false
	var used_slots := 0
	for saved_machine in loaded.values():
		used_slots += Power.slots(str(saved_machine.kind))
	if used_slots > restored_progression.power_slots:
		return false
	var new_totals := {}
	for key in delivered.keys():
		var amount := int(totals.get(key, 0))
		if amount < 0:
			return false
		new_totals[key] = amount
	# Preserve previously built machines where rivers or lakes would now
	# appear; the new geography may not flood an existing saved factory.
	for cell in loaded:
		migrated_water.erase(cell)
	construction = restored_construction
	economy = restored_economy
	progression = restored_progression
	power = Power.new()
	power_dirty = true
	resources = migrated_resources
	water = migrated_water
	buildings = loaded
	occupied = new_occupied
	cells_dirty = true
	transit.clear()
	delivered = new_totals
	steps = maxi(0, int(data.get("steps", 0)))
	accumulator = 0.0
	_update_power()
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
	var totals := {"miner": 0, "belt": 0, "smelter": 0, "storage": 0,
		"depot": 0, "power_pole": 0, "water_wheel": 0}
	for building in buildings.values():
		var kind: String = str(building.kind)
		totals[kind] = int(totals.get(kind, 0)) + 1
	return totals
