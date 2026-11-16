extends SceneTree
const Model = preload("res://factory_model.gd")

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
	print("Midnight Industries factory simulation smoke PASS")
	quit()
