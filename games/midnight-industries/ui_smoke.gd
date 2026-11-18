extends SceneTree
## Scene-level acceptance: controls, selection/build split, direction and menu.
func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var packed: PackedScene = load("res://main.tscn")
	var game: Variant = packed.instantiate()
	root.add_child(game)
	await process_frame
	await process_frame
	if not game.menu_open or not game.navigation.start_screen.visible or game.menu_mask.visible or game.mode != "inspect":
		_fail("startup world-selection menu or selection mode missing", game)
		return
	game.navigation.name_edit.text = "UI Smoke Factory"
	game.navigation._create_world()
	var world_id: String = game.current_world
	if world_id.is_empty() or game.menu_open or game.navigation.start_screen.visible:
		_fail("Creating a world did not enter the factory", game)
		return
	if game.static_view == null or game.motion_view == null:
		_fail("cached/rendering layers not created", game)
		return
	var construction_view: Node = game.get_node_or_null("CasinoConstructionSite")
	if construction_view == null or construction_view.get("model") != game.model:
		_fail("Casino construction site is missing from the world", game)
		return
	if game.construction_status == null or not "01/12" in game.construction_status.text:
		_fail("Twelve-phase construction status is missing", game)
		return
	game._focus_construction()
	if game.menu_open or not game.camera.position.is_equal_approx(
		(Vector2(106, 82) + Vector2(6, 6)) * 48.0):
		_fail("Camera cannot navigate to the casino site", game)
		return
	if game.minimap == null or game.minimap.visible:
		_fail("Full-screen map must exist but remain closed by default", game)
		return
	game._toggle_minimap()
	if not game.minimap.visible:
		_fail("M map did not open", game)
		return
	game._toggle_minimap()
	if game.minimap.visible:
		_fail("M map did not close", game)
		return
	if not game.model.place(Vector2i(16, 17), "miner", 0):
		_fail("New-world miner cannot be placed for free", game)
		return
	game._inspect(Vector2i(16, 17))
	if game.inspector_title.text == "" or not game.inspector_controls.visible:
		_fail("selected miner has no inspector", game)
		return
	game._select_tool("belt")
	if game.mode != "build":
		_fail("tool selection did not enter build mode", game)
		return
	game.facing = 1
	game.motion_view.facing = 1
	var target := Vector2i(26, 17)
	if not game.model.can_place(target, "belt"):
		_fail("Conveyors must be free in a fresh factory", game)
		return
	if not game.model.place(target, "belt", game.facing):
		_fail("belt placement rejected", game)
		return
	game.static_view.queue_redraw()
	if int(game.model.buildings[target].direction) != 1:
		_fail("direction not saved on belt", game)
		return
	game._set_mode("inspect")
	game._inspect(target)
	game._rotate_selected()
	if int(game.model.buildings[target].direction) != 2:
		_fail("inspector rotation failed", game)
		return
	game._toggle_pause()
	if not game.simulation_paused:
		_fail("pause button failed", game)
		return
	game._show_menu(true)
	if not game.menu_open or not game.menu_mask.visible:
		_fail("escape menu opening failed", game)
		return
	game._show_menu(false)
	if game.menu_open:
		_fail("escape menu closing failed", game)
		return
	game._demolish_selected()
	if game.model.buildings.has(target):
		_fail("inspector demolish failed", game)
		return
	if game.model.economy.available("iron_ingot") != 0:
		_fail("Free building placement/demolition generated free materials", game)
		return
	if game.storage_view == null or game.storage_view.visible:
		_fail("Global stock overlay must start hidden", game)
		return
	game._toggle_storage()
	if not game.storage_view.visible or game.storage_view.model != game.model:
		_fail("I inventory did not open on the shared build storage", game)
		return
	game._toggle_storage()
	if game.storage_view.visible:
		_fail("I inventory did not close", game)
		return
	if game.build_dock == null or game.build_dock.category_buttons.size() < 5:
		_fail("Category-driven build dock missing", game)
		return
	if game.header_buttons.size() != 6 or game.header_buttons["pause"] != game.pause_button:
		_fail("Header controls missing or Pause remained in bottom rail", game)
		return
	if game.find_child("CommandActionsScroll", true, false) != null \
		or game.find_child("CompactGameStatus", true, false) != null:
		_fail("Outdated scrollbar or empty bottom-left status background still present", game)
		return
	if game.pause_button.text.contains("Ⅱ") or game.pause_button.icon == null:
		_fail("Pause action must have exactly one icon", game)
		return
	if not game.build_dock.category_panel.get_theme_stylebox("panel") is StyleBoxEmpty:
		_fail("Bottom category bar should use floating buttons, not a frame", game)
		return
	if game.objectives_drawer == null:
		_fail("Objectives drawer missing", game)
		return
	game.navigation._choose("power_lines", "always")
	if game.motion_view.power_line_mode != "always":
		_fail("Always-visible power connections setting not applied", game)
		return
	game.navigation._choose("power_lines", "off")
	if game.motion_view.power_line_mode != "off":
		_fail("Power lines Off setting not applied", game)
		return
	game.navigation._choose("power_lines", "auto")
	if game.motion_view.power_line_mode != "auto":
		_fail("Power lines Auto setting not restored", game)
		return
	game.objectives_drawer.set_expanded(true)
	if not game.objectives_drawer.expanded:
		_fail("Objectives drawer failed to expand", game)
		return
	game.objectives_drawer.set_expanded(false)
	game.build_dock._activate_category("logistics")
	if game.mode != "build" or game.build_dock.machine_buttons.size() < 3:
		_fail("Logistics category did not open the build shelf", game)
		return
	game.build_dock._choose_machine("depot")
	if game.tool != "depot":
		_fail("Machine tile did not select its machine", game)
		return
	game.build_dock._activate_category("mining")
	if game.tool != "miner" or game.mode != "build":
		_fail("Switching categories kept an unrelated build tool selected", game)
		return
	game.build_dock._activate_category("production")
	if game.mode != "inspect" or not game.build_dock._open:
		_fail("Locked production category enabled an unearned smelter", game)
		return
	game._toggle_jobs()
	if not game.jobs_view.visible or not "100" in game.jobs_view.description.text:
		_fail("Jobs screen did not show the first delivery objective", game)
		return
	game._toggle_jobs()
	assert(not game.jobs_view.visible)
	# The core smoke verifies real depot delivery; the UI test directly
	# exercises the unlock signal so it also validates live dock refreshing.
	game.model.progression.record_delivery("iron_ore", 100)
	if not game.model.is_unlocked("smelter") or game.model.progression.power_slots != 1:
		_fail("First job did not unlock power and the furnace", game)
		return
	game.build_dock._activate_category("production")
	if game.tool != "smelter" or game.mode != "build":
		_fail("Production category did not unlock after first job", game)
		return
	assert(game.build_dock.machine_buttons.has("smelter"))
	assert(game.model.progression.power_slots == 1)
	game.build_dock._activate_category("energy")
	if not game.build_dock.machine_buttons.has("power_pole") \
		or not game.build_dock.machine_buttons.has("water_wheel") \
		or game.tool != "power_pole" or game.mode != "build":
		_fail("Energy category did not expose new electricity buildings", game)
		return
	var riverside := Vector2i(-1, -1)
	var bank_dir := 0
	for tile in game.model.water:
		if str(game.model.water[tile].kind) != "river":
			continue
		for direction in range(4):
			var land: Vector2i = tile - game.model.DIRS[direction]
			if game.model.can_place_site(land, "water_wheel", direction):
				riverside = land
				bank_dir = direction
				break
		if riverside.x >= 0:
			break
	if riverside.x < 0 or not game.model.place(riverside, "water_wheel", bank_dir):
		_fail("Rotatable two-cell river waterwheel cannot be placed", game)
		return
	game._inspect(game.model.wheel_water_cell(riverside, bank_dir))
	if game.inspected != riverside or game.inspector_title.text.is_empty() \
		or game.rotate_button.disabled:
		_fail("Clicking the submerged wheel half did not inspect the full machine", game)
		return
	game._demolish_selected()
	if game.model.buildings.has(riverside) or not game.model.occupied.is_empty():
		_fail("Dismantling a two-cell wheel left an occupied river tile", game)
		return
	# A disconnected smelter must show a visible, localizable warning.
	var furnace_at := Vector2i(30, 30)
	while not game.model.can_place_site(furnace_at, "smelter"):
		furnace_at += Vector2i(1, 0)
	assert(game.model.place(furnace_at, "smelter", 0))
	game._inspect(furnace_at)
	if not game.inspector_power_banner.visible \
		or not "NO POWER" in game.inspector_power_label.text:
		_fail("Disconnected furnace missing prominent No Power inspector banner", game)
		return
	game._demolish_selected()
	game._set_mode("inspect")
	game.return_to_main()
	if not game.worlds.delete_world(world_id):
		_fail("Temporary test world could not be removed", game)
		return
	print("Midnight Industries menu, inspector and build visual smoke PASS")
	game.queue_free()
	await process_frame
	quit()

func _fail(reason: String, game: Node2D) -> void:
	push_error(reason)
	game.queue_free()
	quit(2)
