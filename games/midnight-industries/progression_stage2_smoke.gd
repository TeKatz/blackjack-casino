extends SceneTree
## Stage 2 contract: parallel side objective, physical plate production,
## recipe lock-in, 15 kW constructor, persistent unlocks and depot delivery.
const Model = preload("res://factory_model.gd")
const Power = preload("res://factory_power.gd")
const Economy = preload("res://factory_economy.gd")
const Catalog = preload("res://item_catalog.gd")
const Progression = preload("res://factory_progression.gd")

func _initialize() -> void:
	var sim = Model.new(false)
	assert(not sim.is_unlocked("constructor"))
	assert(not sim.is_recipe_unlocked("iron_plate"))
	assert(not sim.is_recipe_unlocked("copper_plate"))
	assert(Economy.COSTS.has("constructor") and Economy.COSTS.constructor.is_empty())
	assert(Power.demand("constructor") == 15)
	assert(Catalog.item("iron_plate").category == "component")
	assert(Catalog.item("copper_plate").category == "component")
	assert(Catalog.machine("constructor").unlock_job == "metal_ingot_delivery")
	sim.progression.record_delivery("iron_ingot", 500)
	assert(not sim.progression.is_complete("metal_ingot_delivery"),
		"Second job is locked until the first delivery is completed")
	sim.progression.record_delivery("iron_ore", 100)
	assert(sim.progression.active_jobs().size() == 2)
	assert(sim.progression.current_job().id == "metal_ingot_delivery")
	var side: Dictionary = sim.progression.job("furnace_expansion")
	assert(str(side.type) == "side")
	assert(side.slots_reward == 1)
	sim.progression.record_delivery("iron_ingot", 49)
	sim.progression.record_delivery("copper_ingot", 50)
	assert(not sim.is_unlocked("constructor"))
	assert(sim.progression.delivered_count("metal_ingot_delivery", "iron_ingot") == 49)
	assert(sim.progression.delivered_count("metal_ingot_delivery", "copper_ingot") == 50)
	var partial: Dictionary = sim.snapshot()
	var resumed = Model.new(false)
	assert(resumed.restore(JSON.parse_string(JSON.stringify(partial))))
	assert(resumed.snapshot() == partial)
	sim.progression.record_delivery("iron_ingot", 1)
	assert(sim.is_unlocked("constructor"))
	assert(sim.is_recipe_unlocked("iron_plate"))
	assert(sim.is_recipe_unlocked("copper_plate"))
	assert(sim.progression.current_job().is_empty())
	assert(sim.progression.power_slots == 1)

	var land := Vector2i(-1, -1)
	var water_direction := 0
	var constructor_at := Vector2i(-1, -1)
	var incoming := Vector2i(-1, -1)
	var destination := Vector2i(-1, -1)
	for river in sim.water:
		if str(sim.water[river].kind) != "river":
			continue
		for direction in range(4):
			var candidate: Vector2i = river - Model.DIRS[direction]
			if not sim.can_place(candidate, "water_wheel", direction):
				continue
			for y in range(-3, 4):
				for x in range(-3, 4):
					var place_at: Vector2i = candidate + Vector2i(x, y)
					if place_at.distance_squared_to(candidate) > 25:
						continue
					if sim.can_place(place_at, "constructor") and \
						sim.can_place(place_at - Vector2i.RIGHT, "belt") and \
						sim.can_place(place_at + Vector2i.RIGHT, "depot") and \
						place_at != candidate:
						land = candidate
						water_direction = direction
						constructor_at = place_at
						incoming = place_at - Vector2i.RIGHT
						destination = place_at + Vector2i.RIGHT
						break
				if land.x >= 0:
					break
			if land.x >= 0:
				break
		if land.x >= 0:
			break
	assert(land.x >= 0, "Could not find a power-connected river bank with a production chain")
	assert(sim.place(constructor_at, "constructor", 0))
	assert(sim.place(incoming, "belt", 0))
	assert(sim.place(destination, "depot", 0))
	assert(sim.power.status(constructor_at) == "no_power" or not sim.power.is_powered(constructor_at))
	assert(not sim.set_constructor_recipe(constructor_at, "gold_plate"))
	assert(sim.buildings[constructor_at].recipe == "iron_plate")
	sim.buildings[incoming].item = "iron_ingot"
	for i in range(3):
		sim.advance(Model.STEP)
	assert(int(sim.buildings[constructor_at].ore) == 1)
	assert(float(sim.buildings[constructor_at].progress) == 0.0,
		"No electricity must prevent plate production")
	assert(not sim.set_constructor_recipe(constructor_at, "copper_plate"),
		"Recipe may not be switched while material is buffered")
	assert(sim.place(land, "water_wheel", water_direction))
	sim._update_power()
	assert(sim.power.is_powered(constructor_at))
	assert(sim.power.total_demand == 15)
	assert(sim.power.total_supply == 30)
	for i in range(22):
		sim.advance(Model.STEP)
	assert(sim.economy.available("iron_plate") == 1,
		"One real iron plate must arrive at the depot after input conversion")
	assert(sim.delivered.iron_plate == 1)
	assert(sim.set_constructor_recipe(constructor_at, "copper_plate"))
	assert(sim.buildings[constructor_at].recipe == "copper_plate")
	sim.buildings[incoming].item = "copper_ingot"
	for i in range(22):
		sim.advance(Model.STEP)
	assert(sim.economy.available("copper_plate") == 1,
		"Copper plate must use separate recipe and real physical depot delivery")
	assert(sim.delivered.copper_plate == 1)
	var final_save: Dictionary = sim.snapshot()
	var loaded = Model.new(false)
	assert(loaded.restore(JSON.parse_string(JSON.stringify(final_save))))
	assert(loaded.snapshot() == final_save)
	assert(loaded.buildings[constructor_at].recipe == "copper_plate")

	# The optional job must NOT complete merely from four idle furnaces.
	var factory = Progression.new()
	factory.record_delivery("iron_ore", 100)
	var nodes: Dictionary = {Vector2i(5, 5): {"kind":"water_wheel"}}
	for x in range(4):
		nodes[Vector2i(6+x, 5)] = {
			"kind":"smelter", "ore": 0, "output": 0, "progress": 0.0}
	var net = Power.new()
	net.rebuild(nodes, factory.power_slots)
	assert(net.total_powered == 30 and net.total_demand == 40)
	factory.observe_factory(nodes, net)
	assert(not factory.is_complete("furnace_expansion"))
	for x in range(2):
		nodes[Vector2i(6+x, 5)].ore = 1
	factory.observe_factory(nodes, net)
	assert(not factory.is_complete("furnace_expansion"),
		"Two active smelters must not unlock another power slot")
	nodes[Vector2i(8, 5)].ore = 1
	factory.observe_factory(nodes, net)
	assert(factory.is_complete("furnace_expansion"))
	assert(factory.power_slots == 2)
	factory.observe_factory(nodes, net)
	assert(factory.power_slots == 2, "Side job reward may only be granted once")
	var progress_save: Dictionary = factory.snapshot()
	var saved = Progression.new()
	assert(saved.restore(JSON.parse_string(JSON.stringify(progress_save))))
	assert(saved.is_complete("furnace_expansion") and saved.power_slots == 2)
	var invalid_dependency: Dictionary = saved.snapshot()
	invalid_dependency["completed"].erase("first_iron_delivery")
	invalid_dependency["power_slots"] = 1
	assert(not Progression.new().restore(invalid_dependency),
		"An optional job cannot be restored without its main prerequisite")
	print("Midnight Industries stage-two constructor, metal deliveries and side-job smoke PASS")
	quit()
