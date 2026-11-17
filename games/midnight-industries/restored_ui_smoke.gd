extends SceneTree
## Recovered main menu/settings/map integration without rejected build/depot HUD.
func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var game = load("res://main.tscn").instantiate()
	root.add_child(game)
	await process_frame
	await process_frame
	assert(game.navigation != null and game.navigation.start_screen.visible)
	assert(game.find_child("IndustrialBuildCatalog", true, false) != null)
	assert(game.find_child("IndustrialQuickAccessRail", true, false) != null)
	# These oversized rejected UI surfaces must NOT be restored.
	assert(game.find_child("BottomConstructionDock", true, false) == null)
	assert(game.find_child("DepotQuickPins", true, false) == null)
	assert(game.find_child("WorldAndFactoryInterface", true, false) == null)
	game.navigation.open_settings()
	assert(game.navigation.settings_screen.visible)
	game.navigation._change_setting("high_contrast", true)
	assert(game.settings.values.high_contrast)
	game.navigation._change_setting("high_contrast", false)
	game.navigation._change_setting("water_motion", false)
	assert(not game.terrain_view.water_motion)
	game.navigation._change_setting("water_motion", true)
	game.navigation._change_setting("effect_quality", 0)
	assert(game.motion_view.effect_quality == 0)
	game.navigation._change_setting("effect_quality", 2)
	# Settings changes must not change window size or viewport scale while
	# sliders are being dragged; this avoids the previous full-screen flicker.
	var scale_before: float = game.get_viewport().content_scale_factor
	game.navigation._change_setting("ui_scale", 1.1)
	assert(is_equal_approx(game.get_viewport().content_scale_factor, scale_before))
	game.navigation._change_setting("ui_scale", 1.0)
	game.navigation.close_settings()
	game.navigation.name_edit.text = "Recovered World One"
	game.navigation._create_world()
	var first: String = game.current_world
	assert(first != "" and not game.navigation.start_screen.visible)
	assert(not game.menu_open)
	assert(game.sidebar_panel.visible == false)
	game._toggle_storage()
	assert(game.storage_view.visible)
	game._toggle_storage()
	game._toggle_minimap()
	await process_frame
	assert(game.minimap.visible)
	assert(game.minimap._ore_layers.has("gold"))
	var ore_found := false
	for cell in game.model.resources:
		if game.model.resources[cell] == "gold":
			ore_found = game.minimap._ore_layers["gold"].get_image().get_pixel(cell.x, cell.y).a > 0.9
			break
	assert(ore_found)
	game.minimap.layers.gold = false
	game.minimap._change_zoom(1.25, game.minimap.size * 0.5)
	assert(game.minimap.zoom > 1.0)
	game.minimap._edit_marker(Vector2i(90, 100))
	game.minimap.marker_name.text = "Future electronics"
	game.minimap._save_marker()
	assert(game.minimap.markers.size() == 1)
	game.minimap.layers.ore = false
	game._toggle_minimap()
	game.camera.position = Vector2(2120, 1430)
	game.model.economy.credit("iron_ingot", 23)
	assert(game._save_world())
	game.return_to_main()
	assert(game.navigation.start_screen.visible and game.current_world == "")
	game.navigation.name_edit.text = "Recovered World Two"
	game.navigation._create_world()
	var second: String = game.current_world
	assert(second != first)
	assert(game.model.economy.available("iron_ingot") != 23)
	assert(game.minimap.markers.is_empty())
	assert(game.minimap.layers.ore)
	game.return_to_main()
	game.navigation._open_world(first)
	assert(game.model.economy.available("iron_ingot") == 23)
	assert(game.minimap.markers[0].name == "Future electronics")
	assert(not game.minimap.layers.ore and not game.minimap.layers.gold)
	assert(game.camera.position.is_equal_approx(Vector2(2120, 1430)))
	game.navigation.open_settings()
	game.navigation._change_setting("language", "en")
	game.navigation.close_settings()
	await process_frame
	await process_frame
	assert(not game.german)
	assert(game.current_world == first)
	assert(game.minimap.markers.size() == 1)
	game.navigation.open_settings()
	game.navigation._change_setting("language", "de")
	game.navigation.close_settings()
	await process_frame
	await process_frame
	assert(game.german)
	game.return_to_main()
	assert(game.worlds.delete_world(first))
	assert(game.worlds.delete_world(second))
	game.queue_free()
	await process_frame
	print("Midnight Industries restored menu, settings and map UI smoke PASS")
	quit()
