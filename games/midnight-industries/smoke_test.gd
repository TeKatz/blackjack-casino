extends SceneTree
## V1 factory contracts plus first job-driven electricity progression.
const Model = preload("res://factory_model.gd")
const World = preload("res://factory_world.gd")
const Construction = preload("res://factory_construction.gd")
const Economy = preload("res://factory_economy.gd")
const Progression = preload("res://factory_progression.gd")
const Power = preload("res://factory_power.gd")

func _initialize() -> void:
	var sim = Model.new()
	assert(sim.buildings.is_empty(), "New factories start without premade machines")
	assert(sim.progression.power_slots == 0 and sim.progression.completed.is_empty())
	assert(sim.progression.current_job().id == "first_iron_delivery")
	for name in ["miner", "belt", "depot"]:
		assert(sim.is_unlocked(name))
	for name in ["smelter", "water_wheel", "power_pole", "storage"]:
		assert(not sim.is_unlocked(name), "Premature machine unlock: " + name)
	assert(not sim.is_recipe_unlocked("iron_ingot"))
	assert(not sim.can_place_site(Vector2i(30, 17), "smelter"))
	for kind in Economy.COSTS:
		assert(sim.building_cost(str(kind)).is_empty(), "Building material costs not removed")
	assert(sim.economy.available("copper_ingot") == 0)
	assert(sim.economy.credit("iron_ore", 500))
	assert(sim.progression.delivered_count("first_iron_delivery", "iron_ore") == 0)
	assert(not sim.can_place(Vector2i(-1, 0), "belt"))
	assert(not sim.can_place(Construction.ORIGIN, "belt"))
	assert(not sim.can_place(Vector2i(0, 0), "miner"))
	assert(sim.resources.get(Vector2i(16, 17)) == "iron")

	# Physical conveyor -> depot receipts, not total stock or old save
	# statistics, are the only eligible input for the first milestone.
	assert(sim.place(Vector2i(16, 17), "miner", 0))
	assert(sim.place(Vector2i(17, 17), "belt", 0))
	assert(sim.place(Vector2i(18, 17), "depot", 0))
	var ticks := 0
	while sim.progression.delivered_count("first_iron_delivery", "iron_ore") < 99 \
		and ticks < 800:
		sim.advance(Model.STEP)
		ticks += 1
	assert(ticks < 800, "Raw iron conveyor line stalled")
	assert(not sim.progression.is_complete("first_iron_delivery"))
	assert(not sim.is_unlocked("smelter"))
	var partial_save: Dictionary = sim.snapshot()
	var partial = Model.new(false)
	assert(partial.restore(JSON.parse_string(JSON.stringify(partial_save))))
	assert(partial.progression.delivered_count("first_iron_delivery", "iron_ore") == 99)
	assert(partial.snapshot() == partial_save)
	# Continue the real game path to 100: milestone awards exactly one slot
	# and three technologies, but never adds any physical items by itself.
	var stock_before: int = sim.economy.available("iron_ore")
	while not sim.progression.is_complete("first_iron_delivery") and ticks < 820:
		sim.advance(Model.STEP)
		ticks += 1
	assert(sim.progression.is_complete("first_iron_delivery"))
	assert(sim.economy.available("iron_ore") > stock_before)
	assert(sim.progression.power_slots == 1)
	for name in ["smelter", "water_wheel", "power_pole"]:
		assert(sim.is_unlocked(name), "Job reward absent: " + name)
	assert(not sim.is_unlocked("storage"))
	assert(sim.is_recipe_unlocked("iron_ingot"))
	assert(sim.progression.current_job().is_empty())
	var completed_snapshot: Dictionary = sim.snapshot()
	var roundtrip = Model.new(false)
	assert(roundtrip.restore(JSON.parse_string(JSON.stringify(completed_snapshot))))
	assert(roundtrip.snapshot() == completed_snapshot)

	# Water wheels must sit on LAND with an adjacent actual flowing river,
	# not inside water or next to an isolated lake.
	var bank := Vector2i(-1, -1)
	var other_bank := Vector2i(-1, -1)
	for water_cell in sim.water:
		if str(sim.water[water_cell].kind) != "river":
			continue
		for direction in Model.DIRS:
			var candidate: Vector2i = water_cell + direction
			if sim.can_place_site(candidate, "water_wheel"):
				if bank.x < 0:
					bank = candidate
				elif candidate.distance_squared_to(bank) > 100:
					other_bank = candidate
					break
		if other_bank.x >= 0:
			break
	assert(bank.x >= 0 and other_bank.x >= 0, "No actual river banks found")
	assert(not sim.can_place_site(Vector2i(100, 100), "water_wheel"))
	assert(sim.place(bank, "water_wheel", 0))
	sim._update_power()
	assert(sim.power.slot_used == 1 and sim.power.slot_limit == 1)
	assert(sim.power.total_supply == 30)
	assert(not sim.can_place(other_bank, "water_wheel"))
	assert(sim.placement_issue(other_bank, "water_wheel") == "slots")

	# The power pole consumes no energy and no generation slot.
	var pole_cell := Vector2i(-1, -1)
	var smelters: Array[Vector2i] = []
	for dy in range(-4, 5):
		for dx in range(-4, 5):
			var candidate := bank + Vector2i(dx, dy)
			if candidate.distance_squared_to(bank) > 25:
				continue
			if pole_cell.x < 0 and sim.can_place_site(candidate, "power_pole"):
				pole_cell = candidate
			elif smelters.size() < 4 and sim.can_place_site(candidate, "smelter"):
				smelters.append(candidate)
	assert(pole_cell.x >= 0 and smelters.size() == 4)
	assert(sim.place(pole_cell, "power_pole", 0))
	assert(sim.power.slot_used == 1)
	assert(sim.power.total_supply == 30)
	for cell in smelters:
		assert(sim.place(cell, "smelter", 0))
		sim.buildings[cell].ore = 1
		sim.buildings[cell].recipe = "iron"
	sim._update_power()
	assert(sim.power.total_demand == 40)
	assert(sim.power.total_powered == 30)
	var powered_count := 0
	var offline := Vector2i(-1, -1)
	for cell in smelters:
		if sim.power.is_powered(cell):
			powered_count += 1
		else:
			offline = cell
	assert(powered_count == 3 and offline.x >= 0)
	for tick in range(9):
		sim.advance(Model.STEP)
	assert(int(sim.buildings[offline].output) == 0,
		"Unpowered smelter produced an ingot")
	var produced := 0
	for cell in smelters:
		produced += int(sim.buildings[cell].output)
	assert(produced == 3, "Three powered smelters did not produce")
	assert(sim.erase(bank))
	sim._update_power()
	assert(sim.power.slot_used == 0 and sim.power.total_supply == 0)
	assert(sim.power.total_powered == 0)
	for cell in smelters:
		assert(not sim.power.is_powered(cell))
	assert(sim.can_place(bank, "water_wheel"), "Dismantling must free power slot")
	assert(sim.place(other_bank, "water_wheel", 0))
	assert(sim.power.slot_used == 1)

	# Extended power networks connect through poles within 5 cells and
	# never transmit power across disjoint components.
	var network = Power.new()
	var nodes := {
		Vector2i(10, 10): {"kind":"water_wheel"},
		Vector2i(15, 10): {"kind":"power_pole"},
		Vector2i(20, 10): {"kind":"power_pole"},
		Vector2i(24, 10): {"kind":"smelter"},
		Vector2i(80, 80): {"kind":"smelter"}
	}
	network.rebuild(nodes, 1)
	assert(network.total_supply == 30 and network.total_demand == 20)
	assert(network.is_powered(Vector2i(24, 10)))
	assert(not network.is_powered(Vector2i(80, 80)))
	assert(network.edges.size() >= 2)

	# Existing saved furnaces from pre-job versions retain the milestone.
	# The legacy factory is not discarded; real paid stock is preserved.
	var old_save: Dictionary = sim.snapshot()
	old_save.erase("progression")
	var added := {"x":62,"y":82,"kind":"smelter","direction":0,"item":"",
		"buffer":0,"ore":0,"output":0,"recipe":"","progress":0.0,
		"inventory":{},"paid":{"iron_ingot":12},"kit":false}
	old_save["machines"].append(added)
	var legacy = Model.new(false)
	assert(legacy.restore(old_save))
	assert(legacy.progression.is_complete("first_iron_delivery"))
	assert(legacy.progression.power_slots == 1)
	assert(legacy.buildings.has(Vector2i(62, 82)))
	var malformed := sim.snapshot()
	malformed["progression"]["power_slots"] = 500
	assert(not Model.new(false).restore(malformed))
	var invalid_wheel := sim.snapshot()
	invalid_wheel["progression"]["power_slots"] = 0
	assert(not Model.new(false).restore(invalid_wheel))
	assert(Model.WIDTH == World.WIDTH and Model.HEIGHT == World.HEIGHT)
	assert(sim.water.size() > 600)
	assert(sim.construction.input_count() == 48)
	assert(sim.construction.completed_phases == 0)
	assert(sim.construction.requirements().is_empty())
	assert(not sim.construction.is_recipe_unlocked("future_board"))
	print("Midnight Industries factory simulation smoke PASS")
	quit()
