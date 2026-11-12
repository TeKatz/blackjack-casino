extends SceneTree

# Run with Godot 4.5.1:
# godot --headless --path games/first-person-room --script res://smoke_test.gd

func _initialize() -> void:
    call_deferred("_run")


func _run() -> void:
    var scene := load("res://main.tscn") as PackedScene
    if scene == null:
        _fail("main.tscn cannot load")
        return
    var room := scene.instantiate()
    root.add_child(room)
    await process_frame
    await physics_frame

    if room.get_node_or_null("Player/Camera3D/InteractRay") == null:
        _fail("FPS camera or interaction ray missing")
        return
    if room.get_node_or_null("Door") == null or room.get_node_or_null("DoorConsole") == null:
        _fail("Door or console missing")
        return
    if room.get_node_or_null("LampSwitch") == null or room.get_node_or_null("DeskLampLight") == null:
        _fail("Lamp interaction missing")
        return
    for action in ["fp_forward", "fp_back", "fp_left", "fp_right", "fp_interact", "fp_jump", "fp_sprint", "fp_language"]:
        if not InputMap.has_action(action):
            _fail("Missing action: " + action)
            return

    if room.get("door_open") != false or room.get("lamp_on") != true:
        _fail("Initial room states are wrong")
        return
    room.call("toggle_door")
    if room.get("door_open") != true:
        _fail("Door did not toggle")
        return
    room.call("toggle_lamp")
    if room.get("lamp_on") != false or (room.get_node("DeskLampLight") as OmniLight3D).visible:
        _fail("Lamp did not toggle")
        return
    room.call("toggle_lamp")
    if room.get("lamp_on") != true:
        _fail("Lamp could not turn back on")
        return

    print("First person room smoke PASS")
    quit(0)


func _fail(message: String) -> void:
    push_error("First person room smoke FAIL: " + message)
    quit(1)
