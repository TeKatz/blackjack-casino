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
var _held_mask := 5
var _previous_freeze_mode := RigidBody3D.FREEZE_MODE_STATIC
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
    metal.albedo_texture = load("res://art/brushed.svg") as Texture2D
    metal.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
    handle.material_override = metal
    _flashlight_view.add_child(handle)
    # Knurled grip and separate machined cap give the held item a real silhouette.
    var grip_mesh := CylinderMesh.new()
    grip_mesh.top_radius = 0.074
    grip_mesh.bottom_radius = 0.074
    grip_mesh.height = 0.27
    var grip := MeshInstance3D.new()
    grip.mesh = grip_mesh
    grip.rotation_degrees.x = 90.0
    grip.position.z = 0.07
    var grip_mat := StandardMaterial3D.new()
    grip_mat.albedo_color = Color("#8da0aa")
    grip_mat.albedo_texture = load("res://art/rubber.svg") as Texture2D
    grip_mat.roughness = 0.86
    grip.material_override = grip_mat
    _flashlight_view.add_child(grip)
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
    var lens_mesh := CylinderMesh.new()
    lens_mesh.top_radius = 0.085
    lens_mesh.bottom_radius = 0.085
    lens_mesh.height = 0.015
    var lens := MeshInstance3D.new()
    lens.mesh = lens_mesh
    lens.rotation_degrees.x = 90.0
    lens.position.z = -0.298
    var lens_mat := StandardMaterial3D.new()
    lens_mat.albedo_color = Color("#d5f2f4")
    lens_mat.metallic = 0.15
    lens_mat.roughness = 0.18
    lens.material_override = lens_mat
    _flashlight_view.add_child(lens)
    _flashlight_light = SpotLight3D.new()
    _flashlight_light.name = "Beam"
    _flashlight_light.position = Vector3(0, 0, -0.33)
    _flashlight_light.light_color = Color("#eaf3ff")
    _flashlight_light.light_energy = 4.0
    _flashlight_light.spot_range = 14.0
    _flashlight_light.spot_angle = 29.0
    _flashlight_light.spot_angle_attenuation = 0.78
    # Compatibility renderer produces patterned shadow-map acne on distant
    # surfaces with this narrow moving light. Disable its shadowmap and cap
    # its beam at the first solid surface instead. No stippled distant walls.
    _flashlight_light.shadow_enabled = false
    _flashlight_light.light_specular = 0.62
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
        _move_held_prop(delta)
    if flashlight_on:
        _update_flashlight_occlusion()

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


func _move_held_prop(delta: float) -> void:
    # Retain the original collision layers and sweep the carried shape through
    # the world instead of teleporting through walls, furniture and other props.
    if held_prop == null:
        return
    var target: Vector3 = camera.to_global(Vector3(0, -0.2, -HOLD_DISTANCE))
    var motion: Vector3 = (target - held_prop.global_position) * minf(1.0, delta * 15.0)
    if motion.length_squared() < 0.000001:
        return

    var shape_node := held_prop.get_node_or_null("CollisionShape3D") as CollisionShape3D
    if shape_node == null or shape_node.shape == null:
        return
    var query := PhysicsShapeQueryParameters3D.new()
    query.shape = shape_node.shape
    query.transform = held_prop.global_transform * shape_node.transform
    query.motion = motion
    query.margin = 0.015
    query.collision_mask = 5 # world (1) and other physics props (4)
    query.exclude = [held_prop.get_rid(), get_rid()]
    var fractions: PackedFloat32Array = get_world_3d().direct_space_state.cast_motion(query)
    var safe_fraction := fractions[0] if fractions.size() >= 1 else 0.0
    held_prop.global_position += motion * clampf(safe_fraction - 0.01, 0.0, 1.0)
    held_prop.linear_velocity = Vector3.ZERO
    held_prop.angular_velocity = Vector3.ZERO


func _update_flashlight_occlusion() -> void:
    var origin: Vector3 = camera.global_position
    var endpoint: Vector3 = origin - camera.global_transform.basis.z * 14.0
    var query := PhysicsRayQueryParameters3D.create(origin, endpoint, 1, [get_rid()])
    var result: Dictionary = get_world_3d().direct_space_state.intersect_ray(query)
    if result.is_empty():
        _flashlight_light.spot_range = 14.0
    else:
        var distance := origin.distance_to(result["position"])
        _flashlight_light.spot_range = clampf(distance + 0.5, 1.2, 14.0)


func grab_prop(prop: RigidBody3D) -> void:
    if held_prop != null or prop == null:
        return
    held_prop = prop
    _held_layer = prop.collision_layer
    _held_mask = prop.collision_mask
    _previous_freeze_mode = prop.freeze_mode
    # Kinematic freeze mode preserves physical collision responses when moved.
    # The player alone is excluded from the carried body's collision.
    prop.add_collision_exception_with(self)
    prop.freeze_mode = RigidBody3D.FREEZE_MODE_KINEMATIC
    prop.freeze = true
    prop.linear_velocity = Vector3.ZERO
    prop.angular_velocity = Vector3.ZERO
    held_prop_changed.emit(true)


func drop_prop() -> void:
    if held_prop == null:
        return
    var prop := held_prop
    held_prop = null
    prop.collision_layer = _held_layer
    prop.collision_mask = _held_mask
    prop.freeze = false
    prop.freeze_mode = _previous_freeze_mode
    prop.remove_collision_exception_with(self)
    prop.apply_central_impulse(-camera.global_transform.basis.z * 0.5)
    held_prop_changed.emit(false)
