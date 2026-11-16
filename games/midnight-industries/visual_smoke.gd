extends SceneTree
## Visual asset + UI contract; verifies illustrated graphics really ship and render.
const Atlas = preload("res://factory_atlas.gd")
const UiSkin = preload("res://factory_skin.gd")

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
	if game.static_view.chunks.size() != 48:
		_fail("Industrial deck must be split into 48 visible chunks")
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
	var rocks: Texture2D = preload("res://factory_ore_variants.gd").texture()
	if rocks == null or rocks.get_width() != 768 or rocks.get_height() != 192:
		_fail("The 8+8 high-detail ore cluster variants were not generated")
		return
	var sprite_sheet: Image = rocks.get_image()
	if sprite_sheet.get_region(Rect2i(0, 0, 96, 96)).get_data() == sprite_sheet.get_region(Rect2i(96, 0, 96, 96)).get_data():
		_fail("Iron ore variants are only duplicate sprites")
		return
	if sprite_sheet.get_region(Rect2i(0, 96, 96, 96)).get_data() == sprite_sheet.get_region(Rect2i(96, 96, 96, 96)).get_data():
		_fail("Copper ore variants are only duplicate sprites")
		return
	var side_panel: Control = game.find_child("IndustrialBuildCatalog", true, false)
	var inspect_panel: Control = game.find_child("IndustrialMachineConsole", true, false)
	var footer: Control = game.find_child("IndustrialQuickAccessRail", true, false)
	var header: Control = game.find_child("BrushedSteelCommandRail", true, false)
	if side_panel == null or inspect_panel == null or footer == null or header == null:
		_fail("Modern industrial HUD panels missing")
		return
	if side_panel.size.x > 280 or inspect_panel.size.x > 345 or footer.size.y > 80 or header.size.y > 85:
		_fail("HUD panels obstruct too much of the factory")
		return
	if inspect_panel.visible:
		_fail("Inspector should collapse without a selection")
		return
	if side_panel.visible:
		_fail("Full build catalog must be hidden in selection mode")
		return
	game._set_mode("build")
	if not side_panel.visible:
		_fail("Build catalog must be visible when constructing")
		return
	game._set_mode("inspect")
	if side_panel.visible:
		_fail("Build catalog must retract when returning to selection")
		return
	if game.tool_buttons.size() != 4 or game.quick_buttons.size() != 4:
		_fail("Visual machine catalog or quick access deck is incomplete")
		return
	for kind in ["miner", "belt", "smelter", "storage"]:
		var icon: Texture2D = game.tool_buttons[kind].icon
		if icon == null or icon.get_width() != 144:
			_fail("Illustrated machine is missing from the build catalog: " + kind)
			return
	if game.menu_card.get_theme_stylebox("panel") is not StyleBoxTexture:
		_fail("The native old menu surface has not been fully replaced")
		return
	game._show_menu(false)
	game._inspect(Vector2i(16, 17))
	if not inspect_panel.visible:
		_fail("Selecting a machine must open its inspector")
		return
	if game.inspector_portrait.texture == null:
		_fail("Selected machine has no high-detail inspector art")
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
	print("Midnight Industries industrial visual asset smoke PASS")
	game.queue_free()
	await process_frame
	quit()

func _fail(reason: String) -> void:
	push_error(reason)
	quit(2)
