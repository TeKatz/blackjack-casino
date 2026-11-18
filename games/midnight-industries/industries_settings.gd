extends RefCounted
const PATH := "user://industries-settings.cfg"
const ACTIONS := {
	"menu": KEY_ESCAPE, "pause": KEY_SPACE, "save": KEY_S, "map": KEY_M,
	"inventory": KEY_I, "grid": KEY_G, "casino": KEY_C, "jobs": KEY_J,
	"inspect": KEY_V, "build": KEY_B, "rotate": KEY_R,
	"zoom_in": KEY_EQUAL, "zoom_out": KEY_MINUS,
	"up": KEY_W, "down": KEY_S, "left": KEY_A, "right": KEY_D,
	"tool_1": KEY_1, "tool_2": KEY_2, "tool_3": KEY_3,
	"tool_4": KEY_4, "tool_5": KEY_5,
	"tool_6": KEY_6, "tool_7": KEY_7, "tool_8": KEY_8
}
var values := {"language":"en", "language_explicit":false, "grid":false, "reduced_motion":false,
	"high_contrast":false, "power_lines":"auto", "ui_scale":1.0, "autosave":60, "tooltip_density":1,
	"window_mode":1, "vsync":true, "fps":60, "water_motion":true,
	"effect_quality":2,
	"resolution":"1600x900", "window_configured":false, "master":1.0, "music":1.0, "effects":1.0, "ui":1.0,
	"master_mute":false, "music_mute":false, "effects_mute":false, "ui_mute":false}
var keys: Dictionary = {}

func _init() -> void:
	keys = ACTIONS.duplicate()
	var cfg := ConfigFile.new()
	if cfg.load(PATH) == OK:
		for name in values:
			values[name] = cfg.get_value("game", name, values[name])
		# An old default German value is not a deliberate language choice.
		# Explicit selections made in the new dropdown always persist.
		if not bool(values.language_explicit):
			values.language = "en"
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

func apply(changed: String = "") -> void:
	# IMPORTANT: settings sliders may emit dozens of changes per second.
	# Never force a window resize, mode switch or CanvasLayer rebuild for
	# unrelated settings. This prevents the old full-screen flicker.
	if changed == "" or changed == "fps":
		Engine.max_fps = clampi(int(values.fps), 30, 240)
	if changed == "" or changed == "vsync":
		DisplayServer.window_set_vsync_mode(
			DisplayServer.VSYNC_ENABLED if bool(values.vsync) else DisplayServer.VSYNC_DISABLED)
	if changed == "" or changed in [
		"master", "music", "effects", "ui", "master_mute", "music_mute",
		"effects_mute", "ui_mute"
	]:
		for bus_name in ["Master", "Music", "Effects", "UI"]:
			var key: String = bus_name.to_lower()
			if changed != "" and changed != key and changed != key + "_mute":
				continue
			var bus := AudioServer.get_bus_index(bus_name)
			if bus < 0:
				AudioServer.add_bus()
				bus = AudioServer.bus_count - 1
				AudioServer.set_bus_name(bus, bus_name)
			AudioServer.set_bus_volume_linear(bus, clampf(float(values[key]), 0.0, 1.0))
			AudioServer.set_bus_mute(bus, bool(values[key + "_mute"]))
	if changed in ["resolution", "window_mode"] or (
		changed == "" and bool(values.get("window_configured", false))
	):
		if DisplayServer.get_name() == "headless":
			return
		var window_mode: int = clampi(int(values.window_mode), 0, 2)
		# Windowed mode uses custom, draggable in-game chrome. This removes
		# the white Windows OS title bar without sacrificing resizing.
		DisplayServer.window_set_flag(DisplayServer.WINDOW_FLAG_BORDERLESS, window_mode != 2)
		match window_mode:
			1:
				DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_MAXIMIZED)
			2:
				DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
			_:
				DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)
				# Do not silently create a window larger than the monitor's
				# usable work area (taskbar and desktop panels included).
				var available: Rect2i = DisplayServer.screen_get_usable_rect(
					DisplayServer.window_get_current_screen())
				var dimensions: PackedStringArray = str(values.resolution).split("x")
				var requested := Vector2i(1440, 810)
				if dimensions.size() == 2:
					requested = Vector2i(
						clampi(int(dimensions[0]), 1000, 3840),
						clampi(int(dimensions[1]), 650, 2160))
				if available.size.x > 0 and available.size.y > 0:
					requested = Vector2i(
						mini(requested.x, maxi(900, available.size.x - 40)),
						mini(requested.y, maxi(620, available.size.y - 40)))
				if DisplayServer.window_get_size() != requested:
					DisplayServer.window_set_size(requested)
				if available.size.x > 0 and available.size.y > 0:
					DisplayServer.window_set_position(
						available.position + (available.size - requested) / 2)

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
