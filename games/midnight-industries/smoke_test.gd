extends SceneTree
const Model = preload("res://factory_model.gd")
const Construction = preload("res://factory_construction.gd")
const World = preload("res://factory_world.gd")
const GoldArt = preload("res://factory_gold_art.gd")
const Economy = preload("res://factory_economy.gd")

func _initialize() -> void:
	var model = Model.new(false)
	assert(not model.place(Vector2i(0, 0), "miner", 0), "Miner placed off ore")
	assert(not model.place(Vector2i(-1, 0), "belt", 0), "Outside placement allowed")
	assert(model.place(Vector2i(16, 17), "miner", 0, true))
	for x in range(17, 21):
		assert(model.place(Vector2i(x, 17), "belt", 0, true))
	assert(model.place(Vector2i(21, 17), "smelter", 0, true))
	for x in range(22, 25):
		assert(model.place(Vector2i(x, 17), "belt", 0))
	assert(model.place(Vector2i(25, 17), "depot", 0, true))
	for i in range(320):
		model.advance(0.25)
	if int(model.delivered.iron_ingot) < 8:
		push_error("Iron line failed to produce ingots: %s" % str(model.delivered))
		quit(2)
		return
	var saved := model.snapshot()
	var again = Model.new(false)
	if not again.restore(saved) or again.snapshot() != saved:
		push_error("Factory snapshot roundtrip failed")
		quit(3)
		return
	if again.restore({"version": 99, "machines": [], "delivered": {}}):
		push_error("Unrecognized save version was accepted")
		quit(4)
		return
	assert(again.erase(Vector2i(17, 17)))
	assert(again.place(Vector2i(17, 17), "belt", 0))
	assert(again.rotate(Vector2i(17, 17)))
	assert(again.buildings[Vector2i(17, 17)].direction == 1)
	# The 12x12 protected lot has 48 separately addressable intakes,
	# exactly one for each gridpoint on every side.
	var construction = model.construction
	assert(Construction.PHASE_IDS.size() == 12)
	assert(construction.input_count() == 48)
	assert(construction.completed_phases == 0)
	assert(construction.requirements().is_empty())
	assert(not construction.is_machine_unlocked("future_assembler"))
	assert(not construction.is_recipe_unlocked("future_board"))
	assert(construction.is_machine_unlocked("smelter"))
	assert(construction.is_recipe_unlocked("iron_ingot"))
	assert(not model.can_place(Construction.ORIGIN, "belt"))
	assert(not model.can_place(
		Construction.ORIGIN + Vector2i(Construction.WIDTH - 1, Construction.HEIGHT - 1),
		"storage"))
	for cell in model.resources:
		assert(not construction.inside(cell), "Construction overwrote an existing ore field")
	for i in range(12):
		var west := Vector2i(Construction.ORIGIN.x, Construction.ORIGIN.y + i)
		var east := Vector2i(Construction.ORIGIN.x + 11, Construction.ORIGIN.y + i)
		var north := Vector2i(Construction.ORIGIN.x + i, Construction.ORIGIN.y)
		var south := Vector2i(Construction.ORIGIN.x + i, Construction.ORIGIN.y + 11)
		assert(construction.is_input(west, west + Vector2i.LEFT, 0))
		assert(construction.is_input(east, east + Vector2i.RIGHT, 2))
		assert(construction.is_input(north, north + Vector2i.UP, 1))
		assert(construction.is_input(south, south + Vector2i.DOWN, 3))
		assert(not construction.is_input(west, west + Vector2i.LEFT, 2))
		assert(not construction.is_input(north, north + Vector2i.UP, 3))
	# Expanded-world contract. The original massive central deposits have
	# moved out, casino access is open, and water never occupies mineral tiles.
	assert(Model.WIDTH == 224 and Model.HEIGHT == 176)
	assert(World.WIDTH == Model.WIDTH and World.HEIGHT == Model.HEIGHT)
	assert(Construction.ORIGIN == Vector2i(106, 82))
	assert(not model.resources.has(Vector2i(61, 47)))
	assert(not model.resources.has(Vector2i(82, 65)))
	assert(model.resources.get(Vector2i(16, 17)) == "iron")
	assert(model.resources.get(Vector2i(44, 124)) == "iron")
	assert(model.resources.get(Vector2i(174, 128)) == "copper")
	var iron_nodes := 0
	var gold_nodes := 0
	for cell in model.resources:
		match str(model.resources[cell]):
			"iron":
				iron_nodes += 1
			"gold":
				gold_nodes += 1
				assert(not World.near_site(cell, 24))
	assert(gold_nodes > 12 and gold_nodes < iron_nodes)
	assert(model.water.size() > 600)
	var saw_river := false
	var saw_lake := false
	var flowing := false
	for cell in model.water:
		assert(not model.resources.has(cell))
		assert(not construction.inside(cell))
		assert(not model.can_place(cell, "belt"))
		assert(not model.can_place(cell, "miner"))
		if model.water[cell].kind == "river":
			saw_river = true
			var direction: Vector2 = model.water[cell].flow
			if direction.length() > 0.5:
				flowing = true
		else:
			saw_lake = true
	assert(saw_river and saw_lake and flowing)
	assert(GoldArt.ore_texture() != null)
	assert(GoldArt.ingot_texture() != null)
	# Gold participates in the existing ore > furnace > ingot > storage
	# production pipeline without affecting iron/copper throughput.
	var gold_line = Model.new(false)
	var gold_cell := Vector2i(205, 154)
	assert(gold_line.resources.get(gold_cell) == "gold")
	assert(gold_line.place(gold_cell, "miner", 0, true))
	assert(gold_line.place(gold_cell + Vector2i(1, 0), "belt", 0, true))
	assert(gold_line.place(gold_cell + Vector2i(2, 0), "smelter", 0, true))
	assert(gold_line.place(gold_cell + Vector2i(3, 0), "belt", 0, true))
	assert(gold_line.place(gold_cell + Vector2i(4, 0), "depot", 0, true))
	for iteration in range(120):
		gold_line.advance(Model.STEP)
	assert(int(gold_line.delivered.gold_ingot) > 0)
	# Legacy miners on the two moved large deposits stay operational.
	var older: Dictionary = model.snapshot()
	older["machines"].append({
		"x": 61, "y": 47, "kind": "miner", "direction": 0,
		"item": "", "buffer": 0, "ore": 0,
		"output": 0, "recipe": "", "progress": 0.0
	})
	var migrated = Model.new(false)
	assert(migrated.restore(older))
	assert(migrated.resources[Vector2i(61, 47)] == "iron")
	assert(not migrated.water.has(Vector2i(61, 47)))
	var entrance := Construction.ORIGIN + Vector2i(-1, 5)
	var parked = Model.new(false)
	assert(parked.place(entrance, "belt", 0, true))
	parked.buildings[entrance].item = "iron_ingot"
	parked.advance(Model.STEP)
	assert(parked.buildings[entrance].item == "iron_ingot")
	assert(parked.construction.completed_phases == 0)
	# Test-only requirement injection proves later recipes and phase
	# unlocking work without shipping any construction quotas yet.
	var staged = Model.new(false)
	staged.construction = Construction.new({
		"phase_01": {"iron_ingot": 2},
		"phase_02": {"copper_ingot": 1}
	}, {"future_assembler": 1}, {"future_board": 2})
	assert(staged.place(entrance, "belt", 0, true))
	assert(not staged.construction.is_machine_unlocked("future_assembler"))
	assert(not staged.construction.is_recipe_unlocked("future_board"))
	staged.buildings[entrance].item = "iron_ingot"
	staged.advance(Model.STEP)
	assert(staged.construction.completed_phases == 0)
	assert(int(staged.construction.materials["iron_ingot"]) == 1)
	staged.buildings[entrance].item = "iron_ingot"
	staged.advance(Model.STEP)
	assert(staged.construction.completed_phases == 1)
	assert(staged.construction.materials.is_empty())
	assert(staged.construction.is_machine_unlocked("future_assembler"))
	assert(not staged.construction.is_recipe_unlocked("future_board"))
	staged.buildings[entrance].item = "copper_ingot"
	staged.advance(Model.STEP)
	assert(staged.construction.completed_phases == 2)
	assert(staged.construction.is_recipe_unlocked("future_board"))
	var progress_snapshot: Dictionary = staged.snapshot()
	var progress_reload = Model.new(false)
	assert(progress_reload.restore(progress_snapshot))
	assert(progress_reload.snapshot() == progress_snapshot)
	# Existing V1 files without a construction field, even ones with older
	# machines on the new lot, must remain importable.
	var legacy_snapshot: Dictionary = model.snapshot()
	legacy_snapshot.erase("construction")
	legacy_snapshot.erase("economy")
	legacy_snapshot["machines"].append({
		"x": Construction.ORIGIN.x, "y": Construction.ORIGIN.y,
		"kind": "belt", "direction": 0, "item": "",
		"buffer": 0, "ore": 0, "output": 0, "recipe": "", "progress": 0.0
	})
	var legacy = Model.new(false)
	assert(legacy.restore(legacy_snapshot))
	assert(legacy.construction.completed_phases == 0)
	assert(legacy.buildings.has(Construction.ORIGIN))
	assert(not legacy.can_place(Construction.ORIGIN, "belt"))
	var corrupted: Dictionary = progress_snapshot.duplicate(true)
	corrupted["construction"]["completed_phases"] = 13
	assert(not Model.new(false).restore(corrupted))
	# Construction economy: genuine insufficient-material rejection, exact
	# debit, 100% refund and no refunds for complimentary starter units.
	assert(Economy.COSTS.size() == 5)
	var economy_test = Model.new(false)
	var cost_cell := Vector2i(16, 17)
	assert(not economy_test.can_place(cost_cell, "miner"))
	assert(not economy_test.place(cost_cell, "miner", 0))
	assert(economy_test.economy.available("iron_ingot") == 0)
	assert(economy_test.economy.credit("iron_ingot", 8))
	assert(economy_test.can_place(cost_cell, "miner"))
	assert(economy_test.place(cost_cell, "miner", 0))
	assert(economy_test.economy.available("iron_ingot") == 0)
	assert(economy_test.buildings[cost_cell]["paid"]["iron_ingot"] == 8)
	assert(economy_test.erase(cost_cell))
	assert(economy_test.economy.available("iron_ingot") == 8)
	assert(economy_test.place(cost_cell, "miner", 0, true))
	assert(economy_test.erase(cost_cell))
	assert(economy_test.economy.available("iron_ingot") == 8)
	# Copper bootstrap makes a paid furnace possible after iron production.
	assert(economy_test.economy.available("copper_ingot") >= 6)
	assert(economy_test.economy.credit("iron_ingot", 4))
	assert(economy_test.place(Vector2i(30, 17), "smelter", 0))
	assert(economy_test.economy.available("iron_ingot") == 0)
	assert(economy_test.economy.available("copper_ingot") == Economy.START_COPPER - 6)
	assert(economy_test.erase(Vector2i(30, 17)))
	assert(economy_test.economy.available("iron_ingot") == 12)
	assert(economy_test.economy.available("copper_ingot") == Economy.START_COPPER)
	# Warehouse is a local buffer, NOT the same as the global bank. It must
	# send physical cargo onward into a depot before money increases.
	var logistics = Model.new(false)
	var entry := Vector2i(30, 17)
	assert(logistics.place(entry, "belt", 0, true))
	assert(logistics.place(entry + Vector2i(1, 0), "storage", 0, true))
	assert(logistics.place(entry + Vector2i(2, 0), "depot", 0, true))
	logistics.buildings[entry].item = "iron_ingot"
	logistics.advance(Model.STEP)
	assert(logistics.economy.available("iron_ingot") == 0)
	assert(logistics.buildings[entry + Vector2i(1, 0)]["inventory"]["iron_ingot"] == 1)
	logistics.advance(Model.STEP)
	assert(logistics.economy.available("iron_ingot") == 1)
	assert(logistics.buildings[entry + Vector2i(1, 0)]["inventory"].is_empty())
	var logistics_save: Dictionary = logistics.snapshot()
	var logistics_loaded = Model.new(false)
	assert(logistics_loaded.restore(logistics_save))
	assert(logistics_loaded.snapshot() == logistics_save)
	# The first free depot voucher is one-use; no paid construction costs
	# can be fabricated by demolishing the complimentary depot.
	var voucher = Model.new(false)
	voucher.economy.free_depot_available = true
	assert(voucher.place(Vector2i(31, 17), "depot", 0))
	assert(not voucher.economy.free_depot_available)
	assert(voucher.erase(Vector2i(31, 17)))
	assert(voucher.economy.available("iron_ingot") == 0)
	assert(not voucher.can_place(Vector2i(31, 17), "depot"))
	# Existing warehouse counters become spendable exactly once. Save/restore
	# carries the bank separately, so a reload must not mint them again.
	var old_factory: Dictionary = logistics_save.duplicate(true)
	old_factory.erase("economy")
	for machine in old_factory["machines"]:
		if machine["kind"] == "depot":
			machine["kind"] = "storage"
		machine.erase("inventory")
		machine.erase("paid")
	old_factory["delivered"]["iron_ingot"] = 47
	var loaded_old = Model.new(false)
	assert(loaded_old.restore(old_factory))
	assert(loaded_old.economy.available("iron_ingot") == 47)
	assert(loaded_old.economy.available("copper_ingot") >= Economy.START_COPPER)
	var migrated_once: Dictionary = loaded_old.snapshot()
	var loaded_twice = Model.new(false)
	assert(loaded_twice.restore(migrated_once))
	assert(loaded_twice.economy.available("iron_ingot") == 47)
	assert(loaded_twice.snapshot() == migrated_once)
	print("Midnight Industries factory simulation smoke PASS")
	quit()
