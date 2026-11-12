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
    var doors: Array = scene.get("doors")
    if doors.size() != 3:
        _fail("Expected three physical 3D doors")
        return
    if scene.get_node_or_null("Atmosphere") == null:
        _fail("The manor has no configured lighting")
        return
    var first := scene.get_node_or_null("Portal_0/DoorHit_0") as StaticBody3D
    if first == null or first.collision_layer == 0:
        _fail("First door is not interactable")
        return
    scene.call("_set_options", [
        {"id": 3, "name": "Grand Staircase"},
        {"id": 5, "name": "Reading Room"},
    ])
    var ids: Array = scene.get("choice_ids")
    if ids != [3, 5, -1]:
        _fail("Incoming state did not determine legal doors")
        return
    var third := scene.get_node_or_null("Portal_2/DoorHit_2") as StaticBody3D
    if third == null or third.collision_layer != 0:
        _fail("Sealed door still accepts clicks")
        return
    scene.call("choose_slot", 0)
    if not scene.get("_selected"):
        _fail("Clicking a legal door did not begin a camera transition")
        return
    scene.call("_apply_web_state", {
        "source": "manor-react",
        "type": "state",
        "demo": true,
        "roomName": "Library",
        "options": [{"id": 4, "name": "Library"}]
    })
    if scene.get("_selected"):
        _fail("New room state did not reset the transition")
        return
    print("Manor 3D web bonus smoke PASS")
    quit(0)


func _fail(message: String) -> void:
    push_error("Manor 3D web bonus smoke FAIL: " + message)
    quit(1)
