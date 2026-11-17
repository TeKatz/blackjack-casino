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
	if not game.menu_open or not game.menu_mask.visible or game.mode != "inspect":
		_fail("startup menu or selection mode missing", game)
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
	print("Midnight Industries menu, inspector and build visual smoke PASS")
	game.queue_free()
	await process_frame
	quit()

func _fail(reason: String, game: Node2D) -> void:
	push_error(reason)
	game.queue_free()
	quit(2)
