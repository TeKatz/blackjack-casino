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
    var world_env := ship.get_node_or_null("World/SpaceEnvironment") as WorldEnvironment
    if world_env == null or world_env.environment == null:
        _fail("World environment missing")
        return
    if world_env.environment.background_mode != Environment.BG_SKY:
        _fail("Image-backed panoramic space sky is not active")
        return
    var sky := world_env.environment.sky
    if sky == null or not sky.sky_material is PanoramaSkyMaterial:
        _fail("Panorama sky material missing")
        return
    if (sky.sky_material as PanoramaSkyMaterial).panorama == null:
        _fail("Actual sky panorama texture missing")
        return
    if ship.get_node_or_null("World/SpaceStars") != null:
        _fail("Old giant sphere stars still used")
        return

    var planet := ship.get_node_or_null("World/Planet") as MeshInstance3D
    if planet == null or not planet.material_override is StandardMaterial3D:
        _fail("Textured planet missing")
        return
    if (planet.material_override as StandardMaterial3D).albedo_texture == null:
        _fail("Planet still has no surface texture")
        return

    var main_floor := ship.get_node_or_null("LoungeFloor/MeshInstance3D") as MeshInstance3D
    var lounge_wall := ship.get_node_or_null("LoungePortWall/MeshInstance3D") as MeshInstance3D
    if main_floor == null or lounge_wall == null:
        _fail("Ship room mesh missing")
        return
    for surface in [main_floor, lounge_wall]:
        var material := surface.material_override as StandardMaterial3D
        if material == null or material.albedo_texture == null or not material.normal_enabled:
            _fail("Floor/wall PBR texturing missing")
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
    var lamp: SpotLight3D = fp.get_node("Camera/HeldFlashlight/Beam")
    if lamp.shadow_enabled:
        _fail("Flashlight shadow-map acne regression")
        return

    var locker := ship.get_node_or_null("CabinLockerDoor") as AnimatableBody3D
    var handle := ship.get_node_or_null("LockerHandle") as StaticBody3D
    if locker == null or handle == null:
        _fail("Cabin locker geometry missing")
        return
    # The hatch must face into the cabin, not towards the adjacent wall.
    if handle.position.z >= locker.position.z:
        _fail("Locker handle still faces the wall")
        return
    ship.toggle_locker()
    if ship.get("_locker_open") != true:
        _fail("Locker did not open")
        return

    var prop := props[0] as RigidBody3D
    var layer := prop.collision_layer
    var mask := prop.collision_mask
    fp.call("grab_prop", prop)
    if fp.get("held_prop") != prop or not prop.freeze:
        _fail("Physics pickup failed")
        return
    if prop.collision_layer != layer or prop.collision_mask != mask:
        _fail("Collision disappeared while carrying object")
        return
    if prop.freeze_mode != RigidBody3D.FREEZE_MODE_KINEMATIC:
        _fail("Carried prop is not in kinematic collision mode")
        return
    # Exercise the real swept collision code; the physical shape remains active.
    fp.call("_move_held_prop", 0.016)
    if prop.collision_layer != layer or prop.collision_mask != mask:
        _fail("Collision sweep disabled carried body")
        return
    fp.call("drop_prop")
    if fp.get("held_prop") != null or prop.freeze:
        _fail("Physics drop failed")
        return
    if prop.collision_layer != layer or prop.collision_mask != mask:
        _fail("Collision layers not restored on release")
        return

    print("Starship explorer smoke PASS")
    quit(0)


func _fail(message: String) -> void:
    push_error("Starship explorer smoke FAIL: " + message)
    quit(1)
