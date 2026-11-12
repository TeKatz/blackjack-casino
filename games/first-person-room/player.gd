extends CharacterBody3D

signal interact_requested(target: Object)
signal focus_changed(target: Object)

const WALK_SPEED := 4.1
const SPRINT_SPEED := 6.4
const JUMP_SPEED := 4.4
const MOUSE_SENSITIVITY := 0.0021

@onready var camera: Camera3D = $Camera3D
@onready var interact_ray: RayCast3D = $Camera3D/InteractRay

var _focused: Object = null


func _ready() -> void:
    _bind_action("fp_forward", KEY_W)
    _bind_action("fp_back", KEY_S)
    _bind_action("fp_left", KEY_A)
    _bind_action("fp_right", KEY_D)
    _bind_action("fp_interact", KEY_E)
    _bind_action("fp_jump", KEY_SPACE)
    _bind_action("fp_sprint", KEY_SHIFT)
    _bind_action("fp_language", KEY_F2)
    if DisplayServer.get_name() != "headless":
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _bind_action(action: StringName, key: Key) -> void:
    if not InputMap.has_action(action):
        InputMap.add_action(action)
    var event := InputEventKey.new()
    event.physical_keycode = key
    if not InputMap.action_has_event(action, event):
        InputMap.action_add_event(action, event)


func _unhandled_input(event: InputEvent) -> void:
    if event.is_action_pressed("ui_cancel"):
        if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
            Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
        else:
            get_tree().quit()
        get_viewport().set_input_as_handled()
        return

    if event is InputEventMouseButton and event.pressed and Input.mouse_mode == Input.MOUSE_MODE_VISIBLE:
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
        get_viewport().set_input_as_handled()
        return

    if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
        rotate_y(-event.relative.x * MOUSE_SENSITIVITY)
        camera.rotate_x(-event.relative.y * MOUSE_SENSITIVITY)
        camera.rotation.x = clampf(camera.rotation.x, deg_to_rad(-85.0), deg_to_rad(85.0))
        return

    if event.is_action_pressed("fp_interact") and (Input.mouse_mode == Input.MOUSE_MODE_CAPTURED or DisplayServer.get_name() == "headless"):
        var target := get_focus_target()
        if target != null:
            interact_requested.emit(target)
        get_viewport().set_input_as_handled()


func _physics_process(delta: float) -> void:
    var axis := Input.get_vector("fp_left", "fp_right", "fp_forward", "fp_back")
    var direction := (transform.basis * Vector3(axis.x, 0.0, axis.y)).normalized()
    var speed := SPRINT_SPEED if Input.is_action_pressed("fp_sprint") else WALK_SPEED
    velocity.x = move_toward(velocity.x, direction.x * speed, delta * 24.0)
    velocity.z = move_toward(velocity.z, direction.z * speed, delta * 24.0)

    if not is_on_floor():
        velocity.y -= float(ProjectSettings.get_setting("physics/3d/default_gravity")) * delta
    elif Input.is_action_just_pressed("fp_jump"):
        velocity.y = JUMP_SPEED
    else:
        velocity.y = 0.0

    move_and_slide()

    var new_focus := get_focus_target()
    if new_focus != _focused:
        _focused = new_focus
        focus_changed.emit(new_focus)


func get_focus_target() -> Object:
    if not interact_ray.is_colliding():
        return null
    var collider := interact_ray.get_collider()
    if collider is Node and collider.is_in_group("interactable"):
        return collider
    return null
