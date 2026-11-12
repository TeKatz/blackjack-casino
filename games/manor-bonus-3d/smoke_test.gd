extends SceneTree

func _initialize() -> void:
    call_deferred("_run")


func _run() -> void:
    var packed := load("res://main.tscn") as PackedScene
    if packed == null:
        _fail("Godot 3D bonus scene could not load")
        return
    var scene := packed.instantiate()
    root.add_child(scene)
    await process_frame
    await physics_frame

    var camera := scene.get_node_or_null("Camera") as Camera3D
    if camera == null or not camera.current:
        _fail("The bonus viewport has no camera")
        return
    if root.content_scale_aspect != Window.CONTENT_SCALE_ASPECT_EXPAND:
        _fail("The WebGL camera still pillarboxes non-16:9 slot views")
        return
    if scene.get_node_or_null("Atmosphere") == null:
        _fail("The Manor has no configured lighting")
        return

    var doors: Array = scene.get("doors")
    if doors.size() != 3:
        _fail("Expected three physical 3D doors")
        return
    var first := scene.get_node_or_null("Portal_0/DoorHit_0") as StaticBody3D
    if first == null or first.collision_layer == 0:
        _fail("First door is not interactable")
        return
    var plaque := scene.get_node_or_null("Portal_0/Hinge_0/ReadableDoorPlaque") as MeshInstance3D
    if plaque == null:
        _fail("Door has no dark, front-facing readable plaque")
        return
    var labels: Array = scene.get("door_names")
    if labels.size() != 3 or (labels[0] as Label3D).pixel_size < 0.009:
        _fail("Door labels are still too small")
        return

    scene.call("_set_options", [
        {"id": 3, "name": "Grand Staircase"},
        {"id": 5, "name": "Reading Room"},
    ])
    var ids: Array = scene.get("choice_ids")
    if ids != [3, 5, -1]:
        _fail("Incoming authoritative room list did not determine doors")
        return
    var third := scene.get_node_or_null("Portal_2/DoorHit_2") as StaticBody3D
    if third == null or third.collision_layer != 0:
        _fail("Sealed door still accepts clicks")
        return

    # All authored locations must actually rebuild the room geometry, not
    # merely switch a string printed on the unchanged entrance hall.
    var signatures := {}
    var dressing: Dictionary = {
        0: "WestGrandStair", 1: "SupplyBarrel", 2: "CandleStoneDais",
        3: "GrandStaircaseSteps", 4: "BookcaseBack", 5: "VelvetReadingSeat",
        6: "AntiqueGlobe", 7: "BallroomDrapery", 8: "GrandPianoBody",
        9: "GardenFountainBasin", 10: "AntiqueTrunk", 11: "BrokenAltar",
        12: "Sarcophagus", 13: "BalconyRail", 14: "NarrowPassageLeft",
        15: "CourtyardWell", 16: "MoonGateLintel",
    }
    for room_id in range(17):
        scene.call("_set_room", "Room %d" % room_id, room_id)
        var visuals := scene.get_node_or_null("RoomVisuals") as Node3D
        if visuals == null or not is_instance_valid(visuals):
            _fail("RoomVisuals not created for id %d" % room_id)
            return
        if visuals.find_child(dressing[room_id], true, false) == null:
            _fail("Room %d has no distinct landmark (%s)" % [room_id, dressing[room_id]])
            return
        var signature := "%s/%d" % [dressing[room_id], visuals.get_child_count()]
        signatures[signature] = true
    if signatures.size() < 13:
        _fail("Too many rooms share the same decoration silhouette")
        return

    scene.call("_set_room", "Entrance Hall", 0)
    var entry := scene.get_node_or_null("RoomVisuals") as Node3D
    if entry == null:
        _fail("Entrance Hall did not restore")
        return
    if entry.find_child("WallSconceBack", true, false) != null:
        _fail("Old floating orange wall sconces still present")
        return
    if entry.find_child("SconceWallPlate", true, false) == null:
        _fail("Wall-attached candle sconces are missing")
        return

    # First-person breathing is slight and disabled by the host preference.
    camera.position = Vector3(0, 2.35, 7.5)
    scene.call("_process", 0.92)
    if absf(camera.position.y - 2.35) < 0.006:
        _fail("Idle first-person breathing is missing")
        return
    scene.call("_apply_web_state", {
        "source": "manor-react", "type": "state",
        "active": true, "demo": true, "reducedMotion": true,
        "roomId": 4, "roomName": "Library",
        "options": [{"id": 5, "name": "Reading Room"}]
    })
    if scene.get("_room_id") != 4:
        _fail("State did not change real 3D location")
        return
    scene.call("_process", 0.92)
    if absf(camera.position.y - 2.35) > 0.002:
        _fail("Reduced motion did not settle the camera")
        return
    # Ghost proximity is a read-only presentation band delivered by React.
    # It cannot choose a route or reveal which doorway the ghost occupies.
    scene.call("_apply_web_state", {
        "source": "manor-react", "type": "state",
        "active": true, "demo": false, "reducedMotion": false,
        "roomId": 4, "roomName": "Library", "ghostBand": "danger",
        "options": [{"id": 5, "name": "Reading Room"}]
    })
    var ambient_light := scene.get("_lighting") as OmniLight3D
    if ambient_light == null or absf(ambient_light.light_energy - 0.55) > 0.02:
        _fail("Danger status does not affect whole-room ghost lighting")
        return
    scene.call("_apply_web_state", {
        "source": "manor-react", "type": "state",
        "active": true, "demo": false, "reducedMotion": true,
        "roomId": 4, "roomName": "Library", "ghostBand": "revealed",
        "options": [{"id": 5, "name": "Reading Room"}]
    })
    if absf(ambient_light.light_energy - 0.92) > 0.02:
        _fail("Candle reveal does not produce subtle cold atmospheric light")
        return
    if scene.get("choice_ids") != [5, -1, -1]:
        _fail("Ghost lighting modified the authoritative door choices")
        return

    scene.call("_set_options", [
        {"id": 3, "name": "Grand Staircase"},
        {"id": 5, "name": "Reading Room"},
    ])
    scene.call("choose_slot", 0)
    if not scene.get("_selected"):
        _fail("Clicking a legal door did not begin the camera transition")
        return
    scene.call("_apply_web_state", {
        "source": "manor-react", "type": "state",
        "active": true, "demo": true, "roomId": 8,
        "roomName": "Music Room",
        "options": [{"id": 7, "name": "Ballroom"}]
    })
    if scene.get("_selected") or scene.get("_room_id") != 8:
        _fail("New room state did not reset transition and change visuals")
        return
    scene.call("_apply_web_state", {
        "source": "manor-react", "type": "state",
        "active": false, "demo": true, "roomId": 0,
        "roomName": "Entrance Hall", "options": []
    })
    scene.call("choose_slot", 0)
    if scene.get("_selected"):
        _fail("Hidden prewarmed iframe could still trigger a choice")
        return

    print("Manor 3D web bonus smoke PASS")
    quit(0)


func _fail(message: String) -> void:
    push_error("Manor 3D web bonus smoke FAIL: " + message)
    quit(1)
