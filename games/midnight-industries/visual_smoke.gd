extends SceneTree
## Visual asset + UI contract; verifies illustrated graphics really ship and render.
const Atlas = preload("res://factory_atlas.gd")
const UiSkin = preload("res://factory_skin.gd")
const Art = preload("res://factory_art.gd")
const BeltArt = preload("res://factory_belt_art.gd")
const OreVariants = preload("res://factory_ore_variants.gd")
const DepotArt = preload("res://factory_depot_art.gd")
const PowerArt = preload("res://factory_power_art.gd")
const ConstructorArt = preload("res://factory_constructor_art.gd")
const ItemCatalog = preload("res://item_catalog.gd")

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var texture: Texture2D = Atlas.get_texture()
	if texture == null or texture.get_width() != 576 or texture.get_height() != 288:
		_fail("Illustrated atlas is missing or incorrectly sized")
		return
	var panel: StyleBoxTexture = UiSkin.panel("header")
	var button: StyleBoxTexture = UiSkin.button("button_active")
	if panel.texture == null or button.texture == null:
		_fail("Metal panel or button asset is missing")
		return
	var game: Variant = load("res://main.tscn").instantiate()
	root.add_child(game)
	await process_frame
	await process_frame
	if not game.navigation.start_screen.visible:
		_fail("Recovered world selection must appear on first launch")
		return
	game.navigation.name_edit.text = "Visual Smoke Factory"
	game.navigation._create_world()
	var world_id: String = game.current_world
	if world_id.is_empty():
		_fail("World selection did not create a playable factory")
		return
	if not game.model.place(Vector2i(16, 17), "miner", 0):
		_fail("Initial free miner could not be placed")
		return
	if game.static_view.chunks.size() != 154:
		_fail("Expanded 224x176 factory must have 154 cached 16x16 chunks")
		return
	var biome: Node = game.get_node_or_null("MeadowAndOreBiome")
	if biome == null or biome.get_node_or_null("SoftMeadowAndOreBiomes") == null:
		_fail("Seamless meadow/stone biome is missing")
		return
	var material: ShaderMaterial = biome.meadow_rect.material
	var rock_map: Texture2D = material.get_shader_parameter("rock_influence")
	var grass_detail: Texture2D = material.get_shader_parameter("terrain_detail")
	if rock_map == null or grass_detail == null:
		_fail("Rock transition or meadow detail textures were not built")
		return
	var influence: Image = rock_map.get_image()
	if influence.get_pixel(32, 34).r < 0.82 or influence.get_pixel(2, 2).r > 0.12:
		_fail("The biome does not distinguish mineral ground from open meadow")
		return
	var stone_detail: Texture2D = material.get_shader_parameter("stone_detail")
	if grass_detail.get_width() < 1024 or grass_detail.get_height() < 1024 or stone_detail == null:
		_fail("Meadow detail was not upgraded to 1024px")
		return
	if stone_detail.get_width() < 1024 or stone_detail.get_height() < 1024:
		_fail("Rock detail was not upgraded to 1024px")
		return
	# Guarantee these are the two requested Poly Haven originals and not
	# synthetic noise or accidentally swapped placeholder ground maps.
	var expected_grass: Texture2D = load("res://assets/grass_ground_diff_1k.jpg")
	var expected_stones: Texture2D = load("res://assets/rocks_ground_05_diff_1k.jpg")
	if expected_grass == null or expected_stones == null:
		_fail("User-selected CC0 Poly Haven diffuse textures missing from packaged game")
		return
	if grass_detail != expected_grass or stone_detail != expected_stones:
		_fail("World shader does not use the original grass/rock diffuse textures")
		return
	var grass_image: Image = grass_detail.get_image()
	var stone_image: Image = stone_detail.get_image()
	if grass_image == null or stone_image == null:
		_fail("Poly Haven ground textures cannot be decoded")
		return
	if grass_image.get_data() == stone_image.get_data():
		_fail("Grass and ore-ground textures are identical")
		return
	if not material.shader.code.contains("grass_primary") \
		or not material.shader.code.contains("rock_primary"):
		_fail("Terrain shader does not sample the requested RGB materials")
		return
	var water_texture: Texture2D = material.get_shader_parameter("water_info")
	if water_texture == null or water_texture.get_width() != 224 or water_texture.get_height() != 176:
		_fail("Terrain water/directional flow texture is missing")
		return
	var water_image: Image = water_texture.get_image()
	for cell in game.model.water:
		if water_image.get_pixel(cell.x, cell.y).r < 0.9:
			_fail("River and lake ground shader disagree with model collision")
			return
	# Keep the ORIGINAL ore and ingot atlas source art. Only ground
	# placement/cluster silhouette varies.
	for item in ["iron_ore", "copper_ore", "iron_ingot", "copper_ingot"]:
		var item_index: int = ["iron_ore", "copper_ore", "iron_ingot", "copper_ingot"].find(item)
		if Art.cargo_source(item) != Atlas.region(item_index, 1):
			_fail("Cargo was changed away from the approved resource atlas: " + item)
			return
	if Art.ore_layout(0) == Art.ore_layout(1) or Art.ore_layout(3) == Art.ore_layout(7):
		_fail("Ground ore clusters lack distinct arrangements")
		return
	for index in range(4):
		var approved: Image = Atlas.get_texture().get_image()
		var isolated: Image = Atlas.sprite(index, 1).get_image()
		if isolated.get_pixel(72, 72) != approved.get_pixel(index * 144 + 72, 144 + 72):
			_fail("Source mineral or ingot artwork was altered")
			return
	# These are actual different rock silhouettes, not a transformed source pile.
	var deposits: Texture2D = OreVariants.texture()
	if deposits == null or deposits.get_width() != 12 * 112 or deposits.get_height() != 3 * 112:
		_fail("Twelve iron and copper ore formations must be available")
		return
	var deposit_image: Image = deposits.get_image()
	for mineral in ["iron", "copper", "gold"]:
		var first: Image = deposit_image.get_region(Rect2i(
			OreVariants.region(mineral, 0).position, Vector2i(112, 112)))
		var different := 0
		for variant in range(1, OreVariants.VARIANTS):
			var picture: Image = deposit_image.get_region(Rect2i(
				OreVariants.region(mineral, variant).position, Vector2i(112, 112)))
			if first.get_data() != picture.get_data():
				different += 1
		if different != 11:
			_fail("Ore field art still reuses repeated deposit bitmaps: " + mineral)
			return
		if OreVariants.cargo_texture(mineral) == null:
			_fail("Physical belt cargo rock art is missing: " + mineral)
			return
	# All four orientations must resolve independently; left must not be a
	# full sprite rotation, preserving the original steel mounting geometry.
	var right_belt: Texture2D = BeltArt.texture(0)
	var down: Texture2D = BeltArt.texture(1)
	var left_belt: Texture2D = BeltArt.texture(2)
	var north_belt: Texture2D = BeltArt.texture(3)
	for belt in [right_belt, down, left_belt, north_belt]:
		if belt == null or belt.get_width() != 144 or belt.get_height() != 144:
			_fail("A directional conveyor is missing or undersized")
			return
	if down.get_image().get_data() == north_belt.get_image().get_data():
		_fail("North/south belts use the same directional sprite")
		return
	if left_belt.get_image().get_data() == right_belt.get_image().get_data():
		_fail("Left conveyor did not receive a dedicated reversed deck")
		return
	var warehouse_rect: Rect2 = Art.machine_destination("storage", Vector2.ZERO)
	if warehouse_rect.position.x < 0.0 or warehouse_rect.end.x > 48.0:
		_fail("Warehouse sprite draws over its left or right neighbor")
		return
	var warehouse: Texture2D = Atlas.sprite(3)
	var isolated_belt: Texture2D = Atlas.sprite(1)
	if warehouse == null or isolated_belt == null:
		_fail("Machine sprites are not isolated from neighboring atlas cells")
		return
	if warehouse.get_width() != 144 or isolated_belt.get_width() != 144:
		_fail("Isolated machine sprites lost their 144px source detail")
		return
	var original_storage: Image = Atlas.get_texture().get_image()
	var repaired_storage: Image = warehouse.get_image()
	for y in [25, 72, 120]:
		if repaired_storage.get_pixel(0, y).a > 0.02:
			_fail("Warehouse has a left-edge sprite bleed fringe")
			return
		# The damaged original left silhouette must be rebuilt from its
		# intact opposite side, not just cropped by three source pixels.
		if repaired_storage.get_pixel(26, y) != original_storage.get_pixel(3 * 144 + 143 - 26, y):
			_fail("Warehouse left-side geometry was not actually repaired")
			return
	var depot_art: Texture2D = DepotArt.texture()
	if depot_art == null or depot_art.get_width() != 144:
		_fail("Dedicated illustrated depot machine texture is unavailable")
		return
	for name in ["water_wheel", "power_pole"]:
		var illustration: Texture2D = PowerArt.texture(name)
		if illustration == null or illustration.get_width() != 144 			or illustration.get_image().get_used_rect().size.x < 75:
			_fail("Dedicated high-detail generator/mast art missing: " + name)
			return
	# All four shore orientations use distinct two-tile artwork, and the
	# logical drawing rectangle matches the submerged Land/River footprint.
	for direction in range(4):
		var wheel: Texture2D = PowerArt.wheel_world(direction)
		var horizontal: bool = direction % 2 == 0
		var expected: Vector2i = Vector2i(288, 144) if horizontal else Vector2i(144, 288)
		if wheel == null or wheel.get_size() != Vector2(expected):
			_fail("Rotated two-tile waterwheel art has incorrect resolution")
			return
		var rect: Rect2 = Art.machine_destination("water_wheel",
			Vector2.ZERO, direction)
		var expected_extent: Vector2 = Vector2(96, 48) if horizontal else Vector2(48, 96)
		if rect.size != expected_extent:
			_fail("Waterwheel sprite rectangle does not cover exactly two tiles")
			return
		if wheel.get_image().get_used_rect().size.y < (220 if not horizontal else 90):
			_fail("Waterwheel sprite does not contain both land and water machinery")
			return
	for direction in range(4):
		var constructor: Texture2D = ConstructorArt.texture(direction)
		if constructor == null or constructor.get_size() != Vector2(144, 144):
			_fail("Constructor must have a 144px illustration in every facing direction")
			return
		if constructor.get_image().get_used_rect().size.x < 85:
			_fail("Constructor illustration is blank or too small")
			return
	for plate_id in ["iron_plate", "copper_plate"]:
		var sheet: Texture2D = ItemCatalog.icon(plate_id)
		if sheet == null or sheet.get_width() < 128:
			_fail("High-detail metal plate icon missing: " + plate_id)
			return
	if Art.machine_destination("smelter", Vector2.ZERO).position.y >= 0.0:
		_fail("Furnace outlet was not moved up to belt height")
		return
	var dock: Control = game.find_child("FactoryBuildDock", true, false)
	var goals: Control = game.find_child("ObjectivesDrawer", true, false)
	var inspect_panel: Control = game.find_child("IndustrialMachineConsole", true, false)
	var header: Control = game.find_child("BrushedSteelCommandRail", true, false)
	if dock == null or goals == null or inspect_panel == null or header == null:
		_fail("New header, objectives, inspector or build dock missing")
		return
	if game.find_child("IndustrialBuildCatalog", true, false) != null \
		or game.find_child("IndustrialQuickAccessRail", true, false) != null:
		_fail("Old build sidebar/bottom debug rail must be removed")
		return
	if header.size.y > 98 or inspect_panel.size.x > 410 \
		or dock.category_panel.size.y > 80:
		_fail("New HUD panels are too large at the test resolution")
		return
	if inspect_panel.visible:
		_fail("Inspector must be collapsed without selection")
		return
	if dock._open:
		_fail("Build shelf must be closed while inspecting")
		return
	game._set_mode("build")
	if not dock._open or not dock.machine_panel.visible:
		_fail("Category machine shelf must expand for building")
		return
	game._set_mode("inspect")
	if dock._open:
		_fail("Machine shelf must retract in inspect mode")
		return
	for kind in ["miner", "belt", "smelter", "storage", "depot", "power_pole", "water_wheel", "constructor"]:
		if game._machine_icon(kind) == null \
			or game._machine_icon(kind).get_width() < 64:
			_fail("Machine illustration missing for " + kind)
			return
	game._set_mode("inspect")
	if dock.category_buttons.size() < 5:
		_fail("Catalog categories are absent")
		return
	if game.header_buttons.size() != 6:
		_fail("New command header must expose six interactive actions")
		return
	if game.header_rail.find_child("CommandActionsScroll", true, false) != null:
		_fail("Header contains forbidden horizontal scrollbar")
		return
	if game.find_child("CompactGameStatus", true, false) != null:
		_fail("Orphaned blank status box still exists")
		return
	if game.production != null:
		_fail("Build stock ticker must not occupy the header")
		return
	if not game.build_dock.category_panel.get_theme_stylebox("panel") is StyleBoxEmpty \
		or not game.build_dock.machine_panel.get_theme_stylebox("panel") is StyleBoxEmpty:
		_fail("The build dock still has the oversized background frames")
		return
	for action in game.header_buttons.values():
		if not action.is_visible_in_tree():
			_fail("One or more header actions are hidden")
			return
		if action.get_global_rect().end.x > game.get_viewport_rect().size.x + 1:
			_fail("Header action clipped beyond viewport; no scroll fallback allowed")
			return
	if game.header_buttons["pause"].text.contains("Ⅱ"):
		_fail("Pause action still has duplicate Unicode icon")
		return
	if game.menu_card.get_theme_stylebox("panel") is not StyleBoxFlat:
		_fail("The native old menu surface has not been fully replaced")
		return
	game._show_menu(false)
	game._inspect(Vector2i(16, 17))
	await process_frame
	await process_frame
	if not inspect_panel.visible:
		_fail("Selecting a machine must open its inspector")
		return
	var inspector_rect: Rect2 = inspect_panel.get_global_rect()
	var shelf_top: float = dock.get_global_rect().position.y + 14.0
	print("Midnight Industries HUD layout: inspector ", inspector_rect,
		", build shelf begins at ", shelf_top, ", viewport ",
		game.get_viewport_rect().size)
	if inspector_rect.end.y > shelf_top - 14.0:
		_fail("Inspector overlaps the bottom machine shelf")
		return
	if game.inspector_portrait.texture == null:
		_fail("Selected machine has no high-detail inspector art")
		return
	if game.find_child("SectorProductionReadout", true, false) != null:
		_fail("Obsolete sector overview panel is still taking screen space")
		return
	var optional_grid: Node = game.get_node_or_null("OptionalPlacementGrid")
	if optional_grid == null:
		_fail("Optional placement grid was not created")
		return
	var initial_grid: bool = game.show_grid
	game._toggle_grid()
	if game.show_grid == initial_grid or optional_grid.visible != game.show_grid:
		_fail("Grid option failed to change the overlay")
		return
	game._toggle_grid()
	if game.show_grid != initial_grid or optional_grid.visible != initial_grid:
		_fail("Grid option failed to restore its original setting")
		return
	# A drag must move the camera without selecting a new machine. A short
	# press/release still selects a machine at the original pressed world tile.
	game._inspect(Vector2i(-1, -1))
	var camera_before: Vector2 = game.camera.position
	game._begin_select_drag(Vector2(800, 450))
	var drag := InputEventMouseMotion.new()
	drag.position = Vector2(841, 467)
	drag.relative = Vector2(41, 17)
	game._input(drag)
	if not game.selection_dragging or game.camera.position == camera_before:
		_fail("Selection-mode hold-and-drag does not pan the camera")
		return
	var up := InputEventMouseButton.new()
	up.button_index = MOUSE_BUTTON_LEFT
	up.pressed = false
	up.position = Vector2(841, 467)
	game._input(up)
	if game.selection_pending or game.inspected != Vector2i(-1, -1):
		_fail("A drag incorrectly selects a machine or remains captured")
		return
	game._begin_select_drag(Vector2(800, 450))
	game.selection_press_cell = Vector2i(16, 17)
	up.position = Vector2(800, 450)
	game._input(up)
	if game.inspected != Vector2i(16, 17):
		_fail("A short selection click no longer inspects machinery")
		return
	game.return_to_main()
	if not game.worlds.delete_world(world_id):
		_fail("Temporary visual test world could not be removed")
		return
	print("Midnight Industries industrial visual asset smoke PASS")
	game.queue_free()
	await process_frame
	quit()

func _fail(reason: String) -> void:
	push_error(reason)
	quit(2)
