extends SceneTree
func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var packed: PackedScene = load("res://main.tscn")
	var game = packed.instantiate()
	root.add_child(game)
	await process_frame
	await process_frame
	assert(game.frontend.start_screen.visible and game.menu_open)
	assert(game.frontend.dock.visible == false)
	assert(game.find_child("BrushedSteelCommandRail", true, false).visible)
	game.frontend.open_settings()
	game.frontend._change_setting("high_contrast", true)
	assert(game.settings.values.high_contrast)
	game.frontend._change_setting("high_contrast", false)
	game.frontend._change_setting("water_motion", false)
	assert(not game.terrain_view.water_motion)
	game.frontend._change_setting("water_motion", true)
	game.frontend.close_settings()
	game.frontend.name_edit.text = "First"
	game.frontend._create_world()
	var first: String = game.current_world
	assert(first != "" and not game.frontend.start_screen.visible)
	assert(game.frontend.dock.visible and game.frontend.pin_panel.visible)
	game.frontend._select_category("logistics")
	assert(game.frontend.machine_row.get_child_count() == 3)
	game.frontend._set_pin("gold_ore")
	game.frontend.pins[2] = "gold_ore"
	game._toggle_minimap()
	await process_frame
	var gold_visible := false
	for cell in game.model.resources:
		if game.model.resources[cell] == "gold" and cell.x > 150:
			gold_visible = game.minimap._ore.get_image().get_pixel(cell.x, cell.y).a > 0.9
			break
	assert(gold_visible)
	game.minimap._change_zoom(1.25, game.minimap.size * 0.5)
	assert(game.minimap.zoom > 1.0)
	game.minimap._edit_marker(Vector2i(90, 100))
	game.minimap.marker_name.text = "South"
	game.minimap.marker_color.select(1)
	game.minimap._save_marker()
	game.minimap._edit_marker(Vector2i(90, 100))
	game.minimap.moving_marker = true
	game.minimap._edit_marker(Vector2i(100, 110))
	assert(game.minimap.markers[0].x == 100 and game.minimap.markers[0].y == 110)
	game.minimap.layers.ore = false
	game._toggle_minimap()
	game.camera.position = Vector2(2120, 1430)
	game.model.economy.credit("iron_ingot", 23)
	assert(game._save_world())
	game.return_to_main()
	assert(game.frontend.start_screen.visible and game.current_world == "")
	game.frontend.name_edit.text = "Second"
	game.frontend._create_world()
	var second: String = game.current_world
	assert(second != first)
	assert(game.model.economy.available("iron_ingot") != 23)
	assert(game.minimap.markers.is_empty() and game.minimap.layers.ore)
	game.return_to_main()
	game.frontend._open_world(first)
	assert(game.model.economy.available("iron_ingot") == 23)
	assert(game.minimap.markers[0].name == "South" and not game.minimap.layers.ore)
	assert(game.frontend.pins[2] == "gold_ore")
	assert(game.camera.position.is_equal_approx(Vector2(2120, 1430)))
	game.return_to_main()
	assert(game.worlds.delete_world(first) and game.worlds.delete_world(second))
	game.queue_free()
	await process_frame
	print("Midnight Industries main menu, world switching, HUD/map persistence PASS")
	quit()
