extends SceneTree
const Model = preload("res://factory_model.gd")

func _initialize() -> void:
	var sim = Model.new(false)
	for y in range(28, 72):
		for x in range(24, 112):
			var tile := Vector2i(x, y)
			if sim.can_place(tile, "belt"):
				sim.place(tile, "belt", 0)
	var total: int = int(sim.buildings.size())
	if total < 3000:
		push_error("Factory scale test did not create enough buildings: %d" % total)
		quit(2)
		return
	# Exercise the fixed-step backend with many factories but no graphics.
	var start := Time.get_ticks_usec()
	for i in range(120):
		sim.advance(0.25)
	var elapsed := float(Time.get_ticks_usec() - start) / 1000.0
	if sim.steps != 120 or int(sim.snapshot().machines.size()) != total:
		push_error("Factory scale test lost simulation state")
		quit(3)
		return
	print("Midnight Industries simulation benchmark: %d buildings, 120 ticks, %.1f ms elapsed, %.2f ms/tick" %
		[total, elapsed, elapsed / 120.0])
	print("Midnight Industries large-grid simulation smoke PASS")
	quit()
