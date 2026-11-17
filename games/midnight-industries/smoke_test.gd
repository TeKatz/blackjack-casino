extends SceneTree
const Model = preload("res://factory_model.gd")
const Construction = preload("res://factory_construction.gd")

func _initialize() -> void:
	var model = Model.new(false)
	assert(not model.place(Vector2i(0, 0), "miner", 0), "Miner placed off ore")
	assert(not model.place(Vector2i(-1, 0), "belt", 0), "Outside placement allowed")
	assert(model.place(Vector2i(16, 17), "miner", 0))
	for x in range(17, 21):
		assert(model.place(Vector2i(x, 17), "belt", 0))
	assert(model.place(Vector2i(21, 17), "smelter", 0))
	for x in range(22, 25):
		assert(model.place(Vector2i(x, 17), "belt", 0))
	assert(model.place(Vector2i(25, 17), "storage", 0))
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
	var entrance := Construction.ORIGIN + Vector2i(-1, 5)
	var parked = Model.new(false)
	assert(parked.place(entrance, "belt", 0))
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
	assert(staged.place(entrance, "belt", 0))
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
	print("Midnight Industries factory simulation smoke PASS")
	quit()
