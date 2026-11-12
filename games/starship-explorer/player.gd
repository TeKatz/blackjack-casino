extends CharacterBody3D

signal use_requested(target: Object)
signal target_changed(target: Object)
signal flashlight_changed(is_on: bool)
signal held_prop_changed(is_held: bool)

const SPEED := 3.7
const SPRINT := 6.0
const MOUSE_SENSITIVITY := 0.00205
const JUMP_SPEED := 4.0
const HOLD_DISTANCE := 1.65

@onready var camera: Camera3D = $Camera
@onready var ray: RayCast3D = $Camera/UseRay

var owns_flashlight := false
var flashlight_on := false
var _flashlight_light: SpotLight3D
var _flashlight_view: Node3D
var held_prop: RigidBody3D
var _held_layer := 4
var _held_mask := 1
var _last_target: Object


func _ready() -> void:
    _setup_keys()
    _build_flashlight()
    if DisplayServer.get_name() != "headless":
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _add_key(action: StringName, key: Key) -> void:
    if not InputMap.has_action(action):
        InputMap.add_action(action)
    var ev := InputEventKey.new()
    ev.physical_keycode = key
    if not InputMap.action_has_event(action, ev):
        InputMap.action_add_event(action, ev)


func _setup_keys() -> void:
    _add_key("ship_forward", KEY_W)
    _add_key("ship_back", KEY_S)
    _add_key("ship_left", KEY_A)
    _add_key("ship_right", KEY_D)
    _add_key("ship_sprint", KEY_SHIFT)
    _add_key("ship_jump", KEY_SPACE)
    _add_key("ship_use", KEY_E)
    _add_key("ship_drop", KEY_G)
    _add_key("ship_flashlight", KEY_F)
    _add_key("ship_language", KEY_F2)


func _build_flashlight() -> void:
    _flashlight_view = Node3D.new()
    _flashlight_view.name = "HeldFlashlight"
    _flashlight_view.position = Vector3(0.36, -0.32, -0.6)
    camera.add_child(_flashlight_view)
    var handle_mesh := CylinderMesh.new()
    handle_mesh.top_radius = 0.07
    handle_mesh.bottom_radius = 0.07
    handle_mesh.height = 0.42
    var handle := MeshInstance3D.new()
    handle.mesh = handle_mesh
    handle.rotation_degrees.x = 90.0
    var metal := StandardMaterial3D.new()
    metal.albedo_color = Color("#3f5262")
    metal.metallic = 0.75
    metal.roughness = 0.27
    handle.material_override = metal
    _flashlight_view.add_child(handle)
    var cap_mesh := CylinderMesh.new()
    cap_mesh.top_radius = 0.11
    cap_mesh.bottom_radius = 0.08
    cap_mesh.height = 0.12
    var cap := MeshInstance3D.new()
    cap.mesh = cap_mesh
    cap.rotation_degrees.x = 90.0
    cap.position = Vector3(0, 0, -0.23)
    cap.material_override = metal
    _flashlight_view.add_child(cap)
    _flashlight_light = SpotLight3D.new()
    _flashlight_light.name = "Beam"
    _flashlight_light.position = Vector3(0, 0, -0.33)
    _flashlight_light.light_color = Color("#eaf3ff")
    _flashlight_light.light_energy = 3.8
    _flashlight_light.spot_range = 19.0
    _flashlight_light.spot_angle = 34.0
    _flashlight_light.spot_angle_attenuation = 0.65
    _flashlight_light.shadow_enabled = true
    _flashlight_view.add_child(_flashlight_light)
    _flashlight_view.visible = false
    _flashlight_light.visible = false


func collect_flashlight() -> void:
    if owns_flashlight:
        return
    owns_flashlight = true
    flashlight_on = false
    _flashlight_view.visible = true
    _flashlight_light.visible = false
    flashlight_changed.emit(false)


func toggle_flashlight() -> void:
    if not owns_flashlight:
        return
    flashlight_on = not flashlight_on
    _flashlight_light.visible = flashlight_on
    flashlight_changed.emit(flashlight_on)


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
        camera.rotation.x = clampf(camera.rotation.x, deg_to_rad(-84), deg_to_rad(84))
        return

    if Input.mouse_mode != Input.MOUSE_MODE_CAPTURED and DisplayServer.get_name() != "headless":
        return

    if event.is_action_pressed("ship_use"):
        if held_prop != null:
            drop_prop()
        else:
            var target := get_focus_target()
            if target is RigidBody3D and target.is_in_group("ship_pickable"):
                grab_prop(target)
            elif target != null:
                use_requested.emit(target)
        get_viewport().set_input_as_handled()
    elif event.is_action_pressed("ship_drop"):
        if held_prop != null:
            drop_prop()
        get_viewport().set_input_as_handled()
    elif event.is_action_pressed("ship_flashlight"):
        toggle_flashlight()
        get_viewport().set_input_as_handled()


func _physics_process(delta: float) -> void:
    var axis := Input.get_vector("ship_left", "ship_right", "ship_forward", "ship_back")
    var dir := (transform.basis * Vector3(axis.x, 0, axis.y)).normalized()
    var speed := SPRINT if Input.is_action_pressed("ship_sprint") else SPEED
    velocity.x = move_toward(velocity.x, dir.x * speed, 22.0 * delta)
    velocity.z = move_toward(velocity.z, dir.z * speed, 22.0 * delta)
    if not is_on_floor():
        velocity.y -= float(ProjectSettings.get_setting("physics/3d/default_gravity")) * delta
    elif Input.is_action_just_pressed("ship_jump"):
        velocity.y = JUMP_SPEED
    else:
        velocity.y = 0.0
    move_and_slide()

    if held_prop != null:
        var hold_point: Vector3 = camera.to_global(Vector3(0, -0.2, -HOLD_DISTANCE))
        held_prop.global_position = held_prop.global_position.lerp(hold_point, minf(1.0, delta * 16.0))
        held_prop.linear_velocity = Vector3.ZERO
        held_prop.angular_velocity = Vector3.ZERO

    var target := get_focus_target()
    if target != _last_target:
        _last_target = target
        target_changed.emit(target)


func get_focus_target() -> Object:
    if not ray.is_colliding():
        return null
    var collider := ray.get_collider()
    if collider == held_prop:
        return null
    if collider is Node and (collider.is_in_group("ship_interactable") or collider.is_in_group("ship_pickable")):
        return collider
    return null


func grab_prop(prop: RigidBody3D) -> void:
    if held_prop != null or prop == null:
        return
    held_prop = prop
    _held_layer = prop.collision_layer
    _held_mask = prop.collision_mask
    prop.freeze = true
    prop.collision_layer = 0
    prop.collision_mask = 0
    held_prop_changed.emit(true)


func drop_prop() -> void:
    if held_prop == null:
        return
    var prop := held_prop
    held_prop = null
    prop.collision_layer = _held_layer
    prop.collision_mask = _held_mask
    prop.freeze = false
    prop.apply_central_impulse(-camera.global_transform.basis.z * 0.8)
    held_prop_changed.emit(false)
