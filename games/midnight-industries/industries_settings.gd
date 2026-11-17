extends RefCounted
const PATH := "user://industries-settings.cfg"
const ACTIONS := {
	"menu": KEY_ESCAPE, "pause": KEY_SPACE, "save": KEY_S, "map": KEY_M,
	"inventory": KEY_I, "grid": KEY_G, "casino": KEY_C,
	"inspect": KEY_V, "build": KEY_B, "rotate": KEY_R,
	"zoom_in": KEY_EQUAL, "zoom_out": KEY_MINUS,
	"up": KEY_W, "down": KEY_S, "left": KEY_A, "right": KEY_D,
	"tool_1": KEY_1, "tool_2": KEY_2, "tool_3": KEY_3,
	"tool_4": KEY_4, "tool_5": KEY_5
}
var values := {"language":"de", "grid":false, "reduced_motion":false,
	"high_contrast":false, "ui_scale":1.0, "autosave":60, "tooltip_density":1,
	"window_mode":0, "vsync":true, "fps":60, "water_motion":true,
	"effect_quality":2,
	"resolution":"1600x900", "master":1.0, "music":1.0, "effects":1.0, "ui":1.0,
	"master_mute":false, "music_mute":false, "effects_mute":false, "ui_mute":false}
var keys: Dictionary = {}

func _init() -> void:
	keys = ACTIONS.duplicate()
	var cfg := ConfigFile.new()
	if cfg.load(PATH) == OK:
		for name in values:
			values[name] = cfg.get_value("game", name, values[name])
		for name in keys:
			var key := int(cfg.get_value("keys", name, keys[name]))
			if key > 0:
				keys[name] = key
	else:
		var old := ConfigFile.new()
		if old.load("user://industries-visual-settings.cfg") == OK:
			values.grid = bool(old.get_value("display", "show_grid", false))
			save()
	_apply_keys()
	apply()

func save() -> bool:
	var cfg := ConfigFile.new()
	for name in values:
		cfg.set_value("game", name, values[name])
	for name in keys:
		cfg.set_value("keys", name, keys[name])
	return cfg.save(PATH) == OK

func apply() -> void:
	Engine.max_fps = clampi(int(values.fps), 30, 240)
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED if values.vsync else DisplayServer.VSYNC_DISABLED)
	for bus_name in ["Master", "Music", "Effects", "UI"]:
		var bus := AudioServer.get_bus_index(bus_name)
		if bus < 0:
			AudioServer.add_bus()
			bus = AudioServer.bus_count - 1
			AudioServer.set_bus_name(bus, bus_name)
		var key: String = bus_name.to_lower()
		AudioServer.set_bus_volume_linear(bus, clampf(float(values[key]), 0.0, 1.0))
		AudioServer.set_bus_mute(bus, bool(values[key + "_mute"]))
	var dimensions: PackedStringArray = str(values.resolution).split("x")
	if dimensions.size() == 2:
		DisplayServer.window_set_size(Vector2i(clampi(int(dimensions[0]), 1024, 3840),
			clampi(int(dimensions[1]), 720, 2160)))
	DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, int(values.window_mode) == 1)
	match int(values.window_mode):
		1: DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MAXIMIZED)
		2: DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
		_: DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)

func _apply_keys() -> void:
	for name in keys:
		var action: String = "industries_" + str(name)
		if not InputMap.has_action(action):
			InputMap.add_action(action)
		InputMap.action_erase_events(action)
		var event := InputEventKey.new()
		event.keycode = int(keys[name])
		if name == "save":
			event.ctrl_pressed = true
		InputMap.action_add_event(action, event)

func rebind(name: String, key: int) -> String:
	if not keys.has(name) or key <= 0:
		return "invalid"
	for other in keys:
		if other != name and int(keys[other]) == key and other != "save" and name != "save":
			return str(other)
	keys[name] = key
	_apply_keys()
	save()
	return ""

func reset_keys() -> void:
	keys = ACTIONS.duplicate()
	_apply_keys()
	save()
