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
