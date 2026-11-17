extends CharacterBody3D

const WALK_SPEED := 4.0
const MOUSE_SENSITIVITY := 0.002
const GRAVITY := 18.0

@onready var camera: Camera3D = $Camera3D

func _ready() -> void:
    Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _unhandled_input(event: InputEvent) -> void:
    if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
        rotate_y(-event.relative.x * MOUSE_SENSITIVITY)
        camera.rotate_x(-event.relative.y * MOUSE_SENSITIVITY)
        camera.rotation.x = clamp(camera.rotation.x, -1.25, 1.25)
    elif event is InputEventKey and event.pressed and event.keycode == KEY_ESCAPE:
        Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
    elif event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
        Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

func _physics_process(delta: float) -> void:
    if not is_on_floor():
        velocity.y -= GRAVITY * delta
    elif velocity.y < 0.0:
        velocity.y = 0.0

    var axis := Vector2.ZERO
    if Input.is_physical_key_pressed(KEY_W):
        axis.y -= 1.0
    if Input.is_physical_key_pressed(KEY_S):
        axis.y += 1.0
    if Input.is_physical_key_pressed(KEY_A):
        axis.x -= 1.0
    if Input.is_physical_key_pressed(KEY_D):
        axis.x += 1.0
    var direction := (basis * Vector3(axis.x, 0.0, axis.y)).normalized()
    velocity.x = direction.x * WALK_SPEED
    velocity.z = direction.z * WALK_SPEED
    move_and_slide()

func _exit_tree() -> void:
    Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
