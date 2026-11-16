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
	var deck: Texture2D = preload("res://factory_deck.gd").texture()
	if deck == null or deck.get_width() != 384 or deck.get_height() != 48:
		_fail("Pre-baked industrial floor tiles missing")
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
	print("Midnight Industries industrial visual asset smoke PASS")
	game.queue_free()
	await process_frame
	quit()

func _fail(reason: String) -> void:
	push_error(reason)
	quit(2)
