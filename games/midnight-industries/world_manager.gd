extends RefCounted
## Versioned world envelopes. The factory snapshot remains the authoritative v1 model.
const Model = preload("res://factory_model.gd")
const ROOT := "user://industries-worlds"
const INDEX := ROOT + "/index.json"
const VERSION := 1
const BACKUPS := 3
var worlds: Array = []
var last_id := ""
var legacy_imported := false

func _init() -> void:
	DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(ROOT))
	_read_index()
	_import_legacy()

func _path(id: String) -> String:
	return ROOT + "/" + id + ".json"

func _read_json(path: String) -> Variant:
	if not FileAccess.file_exists(path):
		return null
	var parser := JSON.new()
	if parser.parse(FileAccess.get_file_as_string(path)) != OK:
		return null
	return parser.data

func _atomic(path: String, data: Dictionary, rotate: bool = true) -> bool:
	var absolute := ProjectSettings.globalize_path(path)
	var temporary := absolute + ".tmp"
	var file := FileAccess.open(temporary, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(data))
	file.flush()
	file.close()
	# Never rotate a damaged current file over a healthy backup.
	if rotate and FileAccess.file_exists(path):
		for i in range(BACKUPS - 1, 0, -1):
			var older := absolute + ".bak%d" % i
			var newer := absolute + ".bak%d" % (i + 1)
			if FileAccess.file_exists(older):
				if DirAccess.copy_absolute(older, newer) != OK:
					DirAccess.remove_absolute(temporary)
					return false
		if DirAccess.copy_absolute(absolute, absolute + ".bak1") != OK:
			DirAccess.remove_absolute(temporary)
			return false
	if DirAccess.rename_absolute(temporary, absolute) != OK:
		# Windows may refuse replacing an existing destination. Only remove
		# the old file after a complete temp write and a valid backup exist.
		if not FileAccess.file_exists(absolute + ".bak1") \
			or DirAccess.remove_absolute(absolute) != OK \
			or DirAccess.rename_absolute(temporary, absolute) != OK:
			if not FileAccess.file_exists(absolute) and FileAccess.file_exists(absolute + ".bak1"):
				DirAccess.copy_absolute(absolute + ".bak1", absolute)
			DirAccess.remove_absolute(temporary)
			return false
	return true

func _read_index() -> void:
	var data: Variant = _read_json(INDEX)
	if typeof(data) != TYPE_DICTIONARY:
		for suffix in [".bak1", ".bak2", ".bak3"]:
			data = _read_json(INDEX + suffix)
			if typeof(data) == TYPE_DICTIONARY:
				break
	if typeof(data) == TYPE_DICTIONARY and data.get("version") == VERSION \
		and typeof(data.get("worlds")) == TYPE_ARRAY:
		worlds = data.worlds
		last_id = str(data.get("last_id", ""))
		legacy_imported = bool(data.get("legacy_imported", false))
		for entry in worlds:
			if entry.get("legacy", false):
				legacy_imported = true

func _write_index() -> bool:
	var current: Variant = _read_json(INDEX)
	return _atomic(INDEX, {"version": VERSION, "worlds": worlds,
		"last_id": last_id, "legacy_imported": legacy_imported},
		typeof(current) == TYPE_DICTIONARY and current.get("version") == VERSION)

func _valid_id(id: String) -> bool:
	return id.length() == 32 and id.is_valid_hex_number()

func _new_id() -> String:
	var crypto := Crypto.new()
	return crypto.generate_random_bytes(16).hex_encode()

func _meta(id: String) -> Dictionary:
	for entry in worlds:
		if typeof(entry) == TYPE_DICTIONARY and entry.get("id") == id:
			return entry
	return {}

func create_world(name: String) -> String:
	var id := _new_id()
	var cleaned := name.strip_edges().substr(0, 48)
	if cleaned.is_empty():
		cleaned = "Neue Welt"
	var meta := {"id": id, "name": cleaned, "updated": Time.get_unix_time_from_system(),
		"play_seconds": 0.0, "phase": 1}
	var model = Model.new()
	if not _atomic(_path(id), _envelope(model, {}), false):
		return ""
	worlds.append(meta)
	last_id = id
	if not _write_index():
		worlds.erase(meta)
		last_id = ""
		return ""
	return id

func _envelope(model, view: Dictionary) -> Dictionary:
	return {"version": VERSION, "factory": model.snapshot(), "view": view.duplicate(true)}

func _valid_view(view: Variant) -> bool:
	if typeof(view) != TYPE_DICTIONARY:
		return false
	var pins: Variant = view.get("pins", [])
	var markers: Variant = view.get("markers", [])
	var layers: Variant = view.get("layers", {})
	if typeof(pins) != TYPE_ARRAY or pins.size() > 64 \
		or typeof(markers) != TYPE_ARRAY or markers.size() > 10000 \
		or typeof(layers) != TYPE_DICTIONARY:
		return false
	for pin in pins:
		if typeof(pin) != TYPE_STRING or pin.length() > 64:
			return false
	for marker in markers:
		if typeof(marker) != TYPE_DICTIONARY or typeof(marker.get("name")) != TYPE_STRING \
			or str(marker.name).length() > 80 or not marker.has("x") or not marker.has("y"):
			return false
		var x := float(marker.x)
		var y := float(marker.y)
		if not is_finite(x) or not is_finite(y) or x < 0.0 or y < 0.0 \
			or x >= Model.WIDTH or y >= Model.HEIGHT:
			return false
	for key in layers:
		if typeof(layers[key]) != TYPE_BOOL:
			return false
	for key in ["camera_x", "camera_y", "zoom"]:
		if view.has(key) and (not is_finite(float(view[key])) or float(view[key]) < 0.0):
			return false
	return true

func load_world(id: String) -> Dictionary:
	if not _valid_id(id) or _meta(id).is_empty():
		return {}
	for suffix in ["", ".bak1", ".bak2", ".bak3"]:
		var data: Variant = _read_json(_path(id) + suffix)
		if typeof(data) != TYPE_DICTIONARY or data.get("version") != VERSION \
			or not _valid_view(data.get("view")):
			continue
		var model = Model.new(false)
		if model.restore(data.get("factory")):
			return {"model": model, "view": data.view, "recovered": suffix != ""}
	return {}

func load_backup(id: String, number: int) -> Dictionary:
	if not _valid_id(id) or _meta(id).is_empty() or number < 1 or number > BACKUPS:
		return {}
	var data: Variant = _read_json(_path(id) + ".bak%d" % number)
	if typeof(data) != TYPE_DICTIONARY or data.get("version") != VERSION \
		or not _valid_view(data.get("view")):
		return {}
	var model = Model.new(false)
	if not model.restore(data.get("factory")):
		return {}
	return {"model": model, "view": data.view, "recovered": true}

func save_world(id: String, model, view: Dictionary, elapsed: float = 0.0) -> bool:
	var meta := _meta(id)
	if meta.is_empty() or not _valid_id(id):
		return false
	var path := _path(id)
	# Validate the current envelope before rotating it into a backup.
	var current: Variant = _read_json(path)
	var healthy := false
	if typeof(current) == TYPE_DICTIONARY and current.get("version") == VERSION:
		var probe = Model.new(false)
		healthy = _valid_view(current.get("view")) and probe.restore(current.get("factory"))
	if not _atomic(path, _envelope(model, view), healthy):
		return false
	meta.updated = Time.get_unix_time_from_system()
	meta.play_seconds = float(meta.get("play_seconds", 0.0)) + maxf(0.0, elapsed)
	meta.phase = model.construction.phase_number()
	last_id = id
	return _write_index()

func rename_world(id: String, name: String) -> bool:
	var meta := _meta(id)
	if meta.is_empty() or name.strip_edges().is_empty():
		return false
	meta.name = name.strip_edges().substr(0, 48)
	return _write_index()

func delete_world(id: String) -> bool:
	var meta := _meta(id)
	if meta.is_empty():
		return false
	worlds.erase(meta)
	if last_id == id:
		last_id = str(worlds.back().get("id", "")) if not worlds.is_empty() else ""
	if not _write_index():
		worlds.append(meta)
		return false
	var path := ProjectSettings.globalize_path(_path(id))
	for suffix in ["", ".tmp", ".bak1", ".bak2", ".bak3"]:
		if FileAccess.file_exists(path + suffix):
			DirAccess.remove_absolute(path + suffix)
	return true

func _import_legacy() -> void:
	# A stable marker in the index makes the import idempotent even if the
	# original single save remains in place for manual recovery.
	if legacy_imported:
		return
	var source := Model.SAVE_FILE
	var model = Model.new(false)
	var found := false
	for suffix in ["", ".bak"]:
		var data: Variant = _read_json(source + suffix)
		if model.restore(data):
			found = true
			break
	if not found:
		return
	var id := _new_id()
	if not _atomic(_path(id), _envelope(model, {}), false):
		return
	worlds.append({"id": id, "name": "Bisherige Fabrik", "updated": Time.get_unix_time_from_system(),
		"play_seconds": 0.0, "phase": model.construction.phase_number(), "legacy": true})
	last_id = id
	legacy_imported = true
	_write_index()
