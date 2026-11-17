extends SceneTree
const Manager = preload("res://world_manager.gd")
const Model = preload("res://factory_model.gd")
const Settings = preload("res://industries_settings.gd")
const Catalog = preload("res://item_catalog.gd")

func _initialize() -> void:
	call_deferred("_run")

func _run() -> void:
	var manager = Manager.new()
	var made_legacy := false
	var imported_id := ""
	var has_import := false
	for entry in manager.worlds:
		if entry.get("legacy", false):
			has_import = true
	if not has_import and not manager.legacy_imported and not FileAccess.file_exists(Model.SAVE_FILE):
		var legacy = Model.new()
		legacy.economy.credit("gold_ingot", 19)
		var original := FileAccess.open(Model.SAVE_FILE, FileAccess.WRITE)
		assert(original != null)
		original.store_string(JSON.stringify(legacy.snapshot()))
		original.close()
		made_legacy = true
		manager = Manager.new()
		for entry in manager.worlds:
			if entry.get("legacy", false):
				imported_id = str(entry.id)
		assert(imported_id != "")
		assert(manager.load_world(imported_id).model.economy.available("gold_ingot") == 19)
		assert(FileAccess.file_exists(Model.SAVE_FILE))
		var again_import = Manager.new()
		assert(again_import.worlds.size() == manager.worlds.size())
	var a: String = manager.create_world("A / iron")
	var b: String = manager.create_world("B / copper")
	assert(a != "" and b != "" and a != b)
	var first: Dictionary = manager.load_world(a)
	var second: Dictionary = manager.load_world(b)
	assert(not first.is_empty() and not second.is_empty())
	first.model.economy.credit("iron_ingot", 31)
	var view := {"pins":["iron_ingot", ""], "markers":[{"x":90, "y":60, "name":"Depot", "icon":"◆", "color":"#F5C04A"}],
		"layers":{"ore":false}, "camera_x":1200.0, "camera_y":1500.0, "zoom":1.5}
	assert(manager.save_world(a, first.model, view, 45.0))
	assert(manager.save_world(a, first.model, view, 2.0))
	assert(manager.load_world(b).model.economy.available("iron_ingot") != 31)
	var again: Dictionary = manager.load_world(a)
	assert(again.model.economy.available("iron_ingot") == 31)
	assert(again.view.pins == view.pins and again.view.markers[0].name == "Depot")
	assert(int(again.view.markers[0].x) == 90 and again.view.layers.ore == false)
	assert(int(manager._meta(a).play_seconds) == 47)
	var path: String = manager._path(a)
	var file := FileAccess.open(path, FileAccess.WRITE)
	file.store_string("{interrupted")
	file.close()
	var recovered: Dictionary = manager.load_world(a)
	assert(recovered.recovered and recovered.model.economy.available("iron_ingot") == 31)
	assert(manager.load_backup(a, 1).model.economy.available("iron_ingot") == 31)
	# An incomplete current save may never displace a valid rotated backup.
	assert(manager.save_world(a, recovered.model, view))
	assert(manager.load_world(a).model.economy.available("iron_ingot") == 31)
	assert(manager.rename_world(b, "Renamed") and manager._meta(b).name == "Renamed")
	assert(Catalog.name_for("copper_ore", true) == "Kupfererz")
	var options = Settings.new()
	assert(options.rebind("map", KEY_ESCAPE) == "menu")
	assert(options.rebind("map", KEY_K) == "")
	assert(options.keys.map == KEY_K)
	assert(Settings.new().keys.map == KEY_K)
	options.reset_keys()
	assert(options.keys.map == KEY_M)
	assert(manager.delete_world(a) and manager.delete_world(b))
	if made_legacy:
		assert(manager.delete_world(imported_id))
		var after_delete = Manager.new()
		assert(after_delete.worlds.size() == manager.worlds.size())
		DirAccess.remove_absolute(ProjectSettings.globalize_path(Model.SAVE_FILE))
	print("Midnight Industries multiworld, recovery, settings and catalog PASS")
	quit()
