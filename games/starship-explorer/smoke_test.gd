extends SceneTree

func _initialize() -> void:
    call_deferred("_execute")


func _execute() -> void:
    var packed := load("res://ship.tscn") as PackedScene
    if packed == null:
        _fail("Scene load failed")
        return
    var ship := packed.instantiate()
    root.add_child(ship)
    await process_frame
    await physics_frame
    if ship.get_node_or_null("Player/Camera/UseRay") == null:
        _fail("First-person camera missing")
        return

    for action in ["ship_forward", "ship_back", "ship_left", "ship_right", "ship_use", "ship_drop", "ship_flashlight", "ship_sprint", "ship_jump", "ship_language"]:
        if not InputMap.has_action(action):
            _fail("Missing input action: " + action)
            return

    var fp: CharacterBody3D = ship.get_node("Player")
    if fp.get("owns_flashlight") != false:
        _fail("Flashlight already owned on spawn")
        return
    if ship.get_node_or_null("World/SpaceStars") == null:
        _fail("Exterior starfield is missing")
        return
    if ship.get_node_or_null("World/Planet") == null:
        _fail("Visible planet is missing")
        return
    if not ship.has_method("toggle_door") or not ship.has_method("toggle_zone"):
        _fail("Door and light controls are missing")
        return

    var doors: Dictionary = ship.get("_doors")
    if doors.size() < 4:
        _fail("Ship has fewer than four doors")
        return
    ship.toggle_door("lounge")
    var state: Dictionary = ship.get("_doors")
    if not state["lounge"]["open"]:
        _fail("Main airlock failed to open")
        return
    ship.toggle_door("lounge")
    state = ship.get("_doors")
    if state["lounge"]["open"]:
        _fail("Main airlock failed to close")
        return

    var zones: Dictionary = ship.get("_zone_enabled")
    if zones["cargo"] != false:
        _fail("Cargo should start dark")
        return
    ship.toggle_zone("cargo")
    zones = ship.get("_zone_enabled")
    if zones["cargo"] != true:
        _fail("Cargo lighting could not be restored")
        return

    ship.take_flashlight()
    if fp.get("owns_flashlight") != true:
        _fail("Flashlight pickup did not work")
        return
    fp.call("toggle_flashlight")
    if fp.get("flashlight_on") != true:
        _fail("Flashlight could not be switched on")
        return
    fp.call("toggle_flashlight")
    if fp.get("flashlight_on") != false:
        _fail("Flashlight could not be switched off")
        return

    var props := get_nodes_in_group("ship_pickable")
    if props.size() < 3:
        _fail("Insufficient physics props")
        return
    var prop := props[0] as RigidBody3D
    fp.call("grab_prop", prop)
    if fp.get("held_prop") != prop or not prop.freeze:
        _fail("Physics pickup failed")
        return
    fp.call("drop_prop")
    if fp.get("held_prop") != null or prop.freeze:
        _fail("Physics drop failed")
        return

    print("Starship explorer smoke PASS")
    quit(0)


func _fail(message: String) -> void:
    push_error("Starship explorer smoke FAIL: " + message)
    quit(1)
