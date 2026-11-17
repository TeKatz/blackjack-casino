extends SceneTree
const Catalog = preload("res://item_catalog.gd")
## Recovered main menu/settings/map integration without rejected build/depot HUD.
func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var game = load("res://main.tscn").instantiate()
	root.add_child(game)
	await process_frame
	await process_frame
	assert(game.navigation != null and game.navigation.start_screen.visible)
	assert(not game.german) # English is the default; old implicit German ignored.
	assert(game.navigation.start_panel.custom_minimum_size.x >= 1000.0)
	assert(game.navigation.settings_screen.find_child("Option_language", true, false) != null)
	assert(game.navigation.settings_screen.find_child("Option_resolution", true, false) != null)
	assert(game.navigation.choice_buttons.has("effect_quality"))
	for choice in game.navigation.choice_buttons["effect_quality"]:
		if choice.value == 2:
			assert(choice.button.button_pressed)
			assert(choice.button.get_theme_stylebox("normal") is StyleBoxFlat)
	assert(game.find_child("FactoryBuildDock", true, false) != null)
	assert(game.find_child("ObjectivesDrawer", true, false) != null)
	assert(game.find_child("IndustrialBuildCatalog", true, false) == null)
	assert(game.find_child("IndustrialQuickAccessRail", true, false) == null)
	# These oversized rejected UI surfaces must NOT be restored.
	assert(game.find_child("BottomConstructionDock", true, false) == null)
	assert(game.find_child("DepotQuickPins", true, false) == null)
	assert(game.find_child("WorldAndFactoryInterface", true, false) == null)
	game.navigation.open_settings()
	assert(game.navigation.settings_screen.visible)
	game.navigation._select_tab("video")
	assert(game.navigation.tab_buttons["video"].button_pressed)
	assert(not game.navigation.tab_buttons["audio"].button_pressed)
	game.navigation._choose("effect_quality", 1)
	assert(game.navigation.choice_buttons["effect_quality"][1].button.button_pressed)
	assert(not game.navigation.choice_buttons["effect_quality"][2].button.button_pressed)
	game.navigation._choose("effect_quality", 2)
	assert(game.navigation.choice_buttons["effect_quality"][2].button.button_pressed)
	game.navigation._change_setting("window_mode", 0)
	assert(game.window_chrome.visible)
	assert(game.header_rail.offset_top > 30.0)
	game.navigation._change_setting("window_mode", 1)
	assert(not game.window_chrome.visible)
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
	game._show_menu(true)
	assert(game.menu_card.get_theme_stylebox("panel") is StyleBoxFlat)
	assert(game.effects_button == null and game.grid_button == null)
	assert(game.pause_world_label.text != "")
	assert(game.menu_card.custom_minimum_size.x >= 700.0)
	game._show_menu(false)
	assert(game.build_dock.inspecting and not game.build_dock._open)
	game._toggle_storage()
	assert(game.storage_view.visible)
	assert(game.storage_view.main_panel.get_theme_stylebox("panel") is StyleBoxFlat)
	assert(game.storage_view.quantities.has("iron_ingot"))
	assert(game.storage_view.quantities.has("gold_ore"))
	assert(game.storage_view.item_rows.get_child_count() >= 6)
	# Actual visual item icons, not the previous flat color swatches.
	var icon: Texture2D = Catalog.icon("iron_ingot")
	assert(icon != null and icon.get_width() >= 96)
	var icon_image: Image = icon.get_image()
	assert(icon_image.get_used_rect().has_area())
	# New future materials must appear automatically, without editing the
	# storage menu or the six original known material list.
	game.model.economy.stock["future_machine_part"] = 17
	game.storage_view._process(0.3)
	assert(game.storage_view.quantities.has("future_machine_part"))
	assert(game.storage_view.quantities["future_machine_part"].text == "17")
	game.storage_view.search_field.text = "gold"
	game.storage_view._rebuild_materials()
	assert(not game.storage_view.quantities.has("iron_ingot"))
	assert(game.storage_view.quantities.has("gold_ore"))
	game.storage_view.search_field.clear()
	game.storage_view._rebuild_materials()
	game.model.economy.stock.erase("future_machine_part")
	game._toggle_storage()
	game._toggle_minimap()
	await process_frame
	assert(game.minimap.visible)
	assert(game.minimap._ore_layers.has("gold"))
	assert(game.minimap._surface.clip_contents)
	assert(game.minimap._map_frame.get_theme_stylebox("panel") is StyleBoxFlat)
	assert(game.minimap._side_panel.get_theme_stylebox("panel") is StyleBoxFlat)
	assert(game.minimap.layer_row.find_child("MapLayer_gold", true, false) != null)
	assert(game.minimap._surface.size.x > 0.0)
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
