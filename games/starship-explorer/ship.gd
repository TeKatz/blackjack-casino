extends Node3D

const SurfaceLibrary = preload("res://surface_library.gd")
var _surface_library = SurfaceLibrary.new()

# Standalone exploration prototype: no casino bridge, bankroll or saved progress.
# All ship geometry is original and built with Godot primitives.

@onready var player: CharacterBody3D = $Player

var _doors: Dictionary = {}
var _door_tweens: Dictionary = {}
var _zone_enabled: Dictionary = {"lounge": true, "corridor": true, "cabin": true, "workshop": true, "cargo": false}
var _zone_lights: Dictionary = {"lounge": [], "corridor": [], "cabin": [], "workshop": [], "cargo": []}
var _flashlight_pickup: StaticBody3D
var _shutter: AnimatableBody3D
var _shutter_closed := false
var _shutter_tween: Tween
var _locker: AnimatableBody3D
var _locker_open := false
var _locker_tween: Tween
var _terminal_online := false
var _terminal_display: Label3D
var _lang_de := true
var _title: Label
var _subtitle: Label
var _zone_label: Label
var _prompt: Label
var _hint: Label
var _notice: Label
var _inventory: Label
var _notice_left := 0.0
var _focused: Object
var _last_zone := ""


func _ready() -> void:
    _lang_de = not OS.get_cmdline_user_args().has("--language=en")
    _build_space()
    _build_hull()
    _build_ambient_lights()
    _build_details()
    _build_doors()
    _build_interactables()
    _build_finish_details()
    _build_hud()
    player.connect("use_requested", Callable(self, "_on_use"))
    player.connect("target_changed", Callable(self, "_on_focus"))
    player.connect("flashlight_changed", Callable(self, "_on_flashlight_changed"))
    player.connect("held_prop_changed", Callable(self, "_on_prop_held"))
    _update_language()


func _process(delta: float) -> void:
    if _notice_left > 0.0:
        _notice_left -= delta
        if _notice_left <= 0.0:
            _notice.text = ""
    var p: Vector3 = player.global_position
    var current_zone := "lounge"
    if p.z < -23.5:
        current_zone = "cargo"
    elif p.x < -2.25 and p.z < -9.5 and p.z > -17.5:
        current_zone = "cabin"
    elif p.x > 2.25 and p.z < -9.5 and p.z > -17.5:
        current_zone = "workshop"
    elif p.z < -5.9:
        current_zone = "corridor"
    if current_zone != _last_zone:
        _last_zone = current_zone
        _zone_label.text = _zone_title(current_zone)


func _unhandled_input(event: InputEvent) -> void:
    if event.is_action_pressed("ship_language"):
        _lang_de = not _lang_de
        _update_language()
        get_viewport().set_input_as_handled()


func _tr(de: String, en: String) -> String:
    return de if _lang_de else en


func _mat(tint: Color, metal: float = 0.0, rough: float = 0.68, glow: Color = Color.BLACK, power: float = 0.0, transparent: bool = false, surface: String = "") -> StandardMaterial3D:
    var material := StandardMaterial3D.new()
    material.albedo_color = tint
    material.metallic = metal
    material.roughness = rough
    if surface != "":
        _surface_library.apply(material, surface)
    if power > 0.0:
        material.emission_enabled = true
        material.emission = glow
        material.emission_energy_multiplier = power
    if transparent:
        material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
        material.cull_mode = BaseMaterial3D.CULL_DISABLED
    return material


func _box(parent: Node3D, item_name: String, at: Vector3, dimensions: Vector3, material: Material, solid: bool = false) -> Node3D:
    var holder: Node3D
    if solid:
        var body := StaticBody3D.new()
        body.collision_layer = 1
        body.collision_mask = 0
        holder = body
    else:
        holder = Node3D.new()
    holder.name = item_name
    holder.position = at
    parent.add_child(holder)
    var mesh := BoxMesh.new()
    mesh.size = dimensions
    var visible := MeshInstance3D.new()
    visible.mesh = mesh
    visible.material_override = material
    holder.add_child(visible)
    if solid:
        var shape := BoxShape3D.new()
        shape.size = dimensions
        var collider := CollisionShape3D.new()
        collider.shape = shape
        holder.add_child(collider)
    return holder


func _sphere(parent: Node3D, item_name: String, at: Vector3, radius: float, material: Material) -> MeshInstance3D:
    var mesh := SphereMesh.new()
    mesh.radius = radius
    mesh.height = radius * 2.0
    var object := MeshInstance3D.new()
    object.name = item_name
    object.position = at
    object.mesh = mesh
    object.material_override = material
    parent.add_child(object)
    return object


func _light(zone: String, at: Vector3, color: Color, energy: float, radius: float, shadows: bool = false) -> OmniLight3D:
    var source := OmniLight3D.new()
    source.position = at
    source.light_color = color
    source.light_energy = energy
    source.omni_range = radius
    source.shadow_enabled = shadows
    if shadows:
        source.shadow_bias = 0.065
        source.shadow_normal_bias = 0.8
    source.visible = bool(_zone_enabled.get(zone, true))
    add_child(source)
    if _zone_lights.has(zone):
        var sources: Array = _zone_lights[zone]
        sources.append(source)
    return source


func _sign(text: String, at: Vector3, rotation_y: float = 0.0, tint: Color = Color("#b8d9df"), size: int = 46) -> Label3D:
    var sign := Label3D.new()
    sign.text = text
    sign.position = at
    sign.rotation_degrees.y = rotation_y
    sign.font_size = size
    sign.pixel_size = 0.0036
    sign.modulate = tint
    add_child(sign)
    return sign


func _build_space() -> void:
    var world := Node3D.new()
    world.name = "World"
    add_child(world)
    var we := WorldEnvironment.new()
    we.name = "SpaceEnvironment"
    var env := Environment.new()
    env.background_mode = Environment.BG_COLOR
    env.background_color = Color("#030712")
    env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    env.ambient_light_color = Color("#7794a9")
    env.ambient_light_energy = 0.13
    we.environment = env
    world.add_child(we)

    # Image-backed equirectangular panorama replaces hundreds of giant star spheres.
    env.background_mode = Environment.BG_SKY
    env.sky = _surface_library.sky()
    env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    var panorama := Node3D.new()
    panorama.name = "SpacePanorama"
    world.add_child(panorama)

    var planet := _sphere(world, "Planet", Vector3(-26, 17, 107), 17.0, _surface_library.celestial(_surface_library.PLANET))
    planet.rotation_degrees.y = 16.0
    _sphere(world, "FarMoon", Vector3(53, -9, 148), 7.3, _surface_library.celestial(_surface_library.MOON))
    # Planetary terminators are produced by a distant light, not emissive white balls.
    var sun := DirectionalLight3D.new()
    sun.name = "ExteriorStarlight"
    sun.rotation_degrees = Vector3(-24, -31, -2)
    sun.light_color = Color("#c5d8f9")
    sun.light_energy = 0.38
    sun.shadow_enabled = false
    world.add_child(sun)


func _build_hull() -> void:
    var hull := _mat(Color("#a0b0b9"), 0.42, 0.69, Color.BLACK, 0.0, false, "ceiling")
    var wall := _mat(Color("#b2c0c8"), 0.36, 0.66, Color.BLACK, 0.0, false, "wall")
    var floor_mat := _mat(Color("#8799a2"), 0.41, 0.59, Color.BLACK, 0.0, false, "floor")
    var glass := _mat(Color(0.21, 0.38, 0.48, 0.12), 0.07, 0.12, Color.BLACK, 0.0, true)
    var beam := _mat(Color("#c1cdd4"), 0.78, 0.28, Color.BLACK, 0.0, false, "brushed")

    # Lounge: 16m wide and 15m long, with a broad star-facing aft window.
    _box(self, "LoungeFloor", Vector3(0, -0.25, 1.55), Vector3(16, 0.5, 15.1), floor_mat, true)
    _box(self, "LoungeCeiling", Vector3(0, 4.35, 1.55), Vector3(16, 0.25, 15.1), hull, true)
    _box(self, "LoungePortWall", Vector3(-8, 2.12, 1.55), Vector3(0.32, 4.25, 15.1), wall, true)
    _box(self, "LoungeStarboardWall", Vector3(8, 2.12, 1.55), Vector3(0.32, 4.25, 15.1), wall, true)
    _wall_z_with_opening(-5.95, -8, 8, 3.2, wall, "LoungeForward")
    _box(self, "WindowLowerWall", Vector3(0, 0.53, 9.1), Vector3(16, 1.06, 0.32), wall, true)
    _box(self, "WindowUpperWall", Vector3(0, 3.92, 9.1), Vector3(16, 0.72, 0.32), wall, true)
    _box(self, "WindowLeftWall", Vector3(-6.95, 2.27, 9.1), Vector3(2.1, 2.75, 0.32), wall, true)
    _box(self, "WindowRightWall", Vector3(6.95, 2.27, 9.1), Vector3(2.1, 2.75, 0.32), wall, true)
    _box(self, "ObservationGlass", Vector3(0, 2.27, 9.1), Vector3(11.8, 2.75, 0.06), glass, true)
    for x in [-6.0, -3.0, 0.0, 3.0, 6.0]:
        _box(self, "WindowMullion", Vector3(x, 2.27, 8.92), Vector3(0.11, 2.75, 0.23), beam)
    _box(self, "WindowTopTrim", Vector3(0, 3.68, 8.92), Vector3(12.5, 0.12, 0.25), beam)
    _box(self, "WindowBaseTrim", Vector3(0, 0.9, 8.92), Vector3(12.5, 0.12, 0.25), beam)

    # Central ship spine, connecting cabins, workshop and dim cargo bay.
    _box(self, "SpineFloor", Vector3(0, -0.25, -14.78), Vector3(4.5, 0.5, 17.7), floor_mat, true)
    _box(self, "SpineCeiling", Vector3(0, 4.35, -14.78), Vector3(4.5, 0.25, 17.7), hull, true)
    _wall_x_with_opening(-2.25, -23.6, -5.95, -13.0, 3.2, wall, "SpinePort")
    _wall_x_with_opening(2.25, -23.6, -5.95, -13.0, 3.2, wall, "SpineStarboard")
    _wall_z_with_opening(-23.55, -2.25, 2.25, 3.2, wall, "SpineCargo")

    # Crew cabin to port.
    _box(self, "CabinFloor", Vector3(-6.3, -0.25, -13.0), Vector3(8.1, 0.5, 8.0), floor_mat, true)
    _box(self, "CabinCeiling", Vector3(-6.3, 4.35, -13.0), Vector3(8.1, 0.25, 8.0), hull, true)
    _box(self, "CabinBack", Vector3(-6.3, 2.12, -17), Vector3(8.1, 4.25, 0.32), wall, true)
    _box(self, "CabinFront", Vector3(-6.3, 2.12, -9), Vector3(8.1, 4.25, 0.32), wall, true)
    _box(self, "CabinOutboard", Vector3(-10.35, 2.12, -13), Vector3(0.32, 4.25, 8.0), wall, true)

    # Engineering workshop to starboard.
    _box(self, "WorkshopFloor", Vector3(6.3, -0.25, -13.0), Vector3(8.1, 0.5, 8.0), floor_mat, true)
    _box(self, "WorkshopCeiling", Vector3(6.3, 4.35, -13.0), Vector3(8.1, 0.25, 8.0), hull, true)
    _box(self, "WorkshopBack", Vector3(6.3, 2.12, -17), Vector3(8.1, 4.25, 0.32), wall, true)
    _box(self, "WorkshopFront", Vector3(6.3, 2.12, -9), Vector3(8.1, 4.25, 0.32), wall, true)
    _box(self, "WorkshopOutboard", Vector3(10.35, 2.12, -13), Vector3(0.32, 4.25, 8.0), wall, true)

    # Cargo deck: physically accessible but intentionally almost unlit.
    _box(self, "CargoFloor", Vector3(0, -0.25, -28.1), Vector3(13.2, 0.5, 9.2), floor_mat, true)
    _box(self, "CargoCeiling", Vector3(0, 4.35, -28.1), Vector3(13.2, 0.25, 9.2), hull, true)
    _box(self, "CargoFarWall", Vector3(0, 2.12, -32.7), Vector3(13.2, 4.25, 0.32), wall, true)
    _box(self, "CargoPortWall", Vector3(-6.6, 2.12, -28.1), Vector3(0.32, 4.25, 9.2), wall, true)
    _box(self, "CargoStarboardWall", Vector3(6.6, 2.12, -28.1), Vector3(0.32, 4.25, 9.2), wall, true)
    _wall_z_with_opening(-23.55, -6.6, 6.6, 3.2, wall, "CargoEntrance")

    # Wall relief, raised thresholds, door frames and long floor path lights.
    var teal := _mat(Color("#42d4d2"), 0.1, 0.31, Color("#40d6ce"), 1.6)
    var amber := _mat(Color("#db916b"), 0.15, 0.48, Color("#e2a06c"), 1.25)
    for z in [-4.0, 0.0, 4.0]:
        for x in [-7.75, 7.75]:
            _box(self, "LoungeWallPanel", Vector3(x, 2.2, z), Vector3(0.18, 3.3, 1.75), beam)
            _box(self, "LoungeWallLight", Vector3(x + (0.11 if x < 0 else -0.11), 2.2, z), Vector3(0.03, 2.7, 0.04), teal)
    for z in [-7.0, -10.0, -16.0, -19.0, -22.0]:
        _box(self, "SpineGuideL", Vector3(-1.5, 0.025, z), Vector3(0.06, 0.045, 2.0), teal)
        _box(self, "SpineGuideR", Vector3(1.5, 0.025, z), Vector3(0.06, 0.045, 2.0), teal)
    for x in [-5.0, 5.0]:
        for z in [-25.5, -28.3, -31.0]:
            _box(self, "CargoMark", Vector3(x, 0.024, z), Vector3(0.05, 0.035, 1.7), amber)
    _frame_z(Vector3(0, 1.6, -5.9), beam, teal)
    _frame_z(Vector3(0, 1.6, -23.5), beam, amber)
    _frame_x(Vector3(-2.25, 1.6, -13), beam, teal)
    _frame_x(Vector3(2.25, 1.6, -13), beam, teal)

    _sign("VESPER / OBSERVATION DECK", Vector3(0, 3.2, 8.69), 0, Color("#badbe1"), 47)
    _sign("CREW QUARTERS", Vector3(-6.5, 3.05, -16.79), 0, Color("#a0c8d5"), 44)
    _sign("ENGINEERING", Vector3(6.5, 3.05, -16.79), 0, Color("#b0e5df"), 44)
    _sign("CARGO / DECK 01", Vector3(0, 3.15, -32.39), 0, Color("#d0a078"), 48)


func _wall_z_with_opening(z: float, min_x: float, max_x: float, aperture: float, material: Material, prefix: String) -> void:
    var half := aperture / 2.0
    var left_length := (-half) - min_x
    var right_length := max_x - half
    _box(self, prefix + "Left", Vector3(min_x + left_length / 2.0, 2.12, z), Vector3(left_length, 4.25, 0.34), material, true)
    _box(self, prefix + "Right", Vector3(half + right_length / 2.0, 2.12, z), Vector3(right_length, 4.25, 0.34), material, true)
    _box(self, prefix + "Header", Vector3(0, 3.79, z), Vector3(aperture, 0.95, 0.34), material, true)


func _wall_x_with_opening(x: float, min_z: float, max_z: float, door_z: float, aperture: float, material: Material, prefix: String) -> void:
    var half := aperture / 2.0
    var first_length := (door_z - half) - min_z
    var second_length := max_z - (door_z + half)
    _box(self, prefix + "Near", Vector3(x, 2.12, min_z + first_length / 2.0), Vector3(0.34, 4.25, first_length), material, true)
    _box(self, prefix + "Far", Vector3(x, 2.12, door_z + half + second_length / 2.0), Vector3(0.34, 4.25, second_length), material, true)
    _box(self, prefix + "Header", Vector3(x, 3.79, door_z), Vector3(0.34, 0.95, aperture), material, true)


func _frame_z(at: Vector3, material: Material, accent: Material) -> void:
    for x in [-1.65, 1.65]:
        _box(self, "DoorFrame", Vector3(at.x + x, at.y, at.z + 0.18), Vector3(0.14, 3.2, 0.32), material)
        _box(self, "DoorFrameGlow", Vector3(at.x + x, at.y, at.z + 0.38), Vector3(0.05, 2.7, 0.03), accent)
    _box(self, "DoorFrameTop", Vector3(at.x, 3.2, at.z + 0.18), Vector3(3.5, 0.15, 0.32), material)


func _frame_x(at: Vector3, material: Material, accent: Material) -> void:
    for z in [-1.65, 1.65]:
        _box(self, "SideFrame", Vector3(at.x, at.y, at.z + z), Vector3(0.32, 3.2, 0.14), material)
        _box(self, "SideFrameGlow", Vector3(at.x + (0.22 if at.x < 0 else -0.22), at.y, at.z + z), Vector3(0.03, 2.7, 0.05), accent)
    _box(self, "SideFrameTop", Vector3(at.x, 3.2, at.z), Vector3(0.32, 0.15, 3.5), material)


func _build_ambient_lights() -> void:
    var dark := _mat(Color("#17232e"), 0.4, 0.65)
    var diffuser := _mat(Color("#b5e9e6"), 0.06, 0.3, Color("#9be0e2"), 1.7)
    var warm := _mat(Color("#f7cca1"), 0.03, 0.4, Color("#f4ba88"), 1.5)
    for x in [-4.7, 4.7]:
        for z in [-3.4, 3.0, 6.7]:
            _box(self, "LoungeLightHousing", Vector3(x, 4.16, z), Vector3(2.1, 0.12, 0.68), dark)
            _box(self, "LoungeDiffuser", Vector3(x, 4.075, z), Vector3(1.75, 0.03, 0.36), warm)
            _light("lounge", Vector3(x, 3.92, z), Color("#ffe0bd"), 1.5, 7.0, true)
    for z in [-8.4, -14.7, -20.9]:
        _box(self, "CorridorDiffuser", Vector3(0, 4.07, z), Vector3(1.1, 0.04, 2.0), diffuser)
        _light("corridor", Vector3(0, 3.92, z), Color("#8bced6"), 1.2, 6.8)
    for cabin_light in [[-6.2, "cabin"], [6.2, "workshop"]]:
        var x: float = cabin_light[0]
        var zone: String = cabin_light[1]
        for z in [-11.4, -15.0]:
            _box(self, "CabinDiffuser", Vector3(x, 4.07, z), Vector3(1.7, 0.04, 0.56), diffuser)
            _light(zone, Vector3(x, 3.93, z), Color("#c0e3e6"), 1.35, 6.5, true)
    for z in [-26.0, -30.1]:
        _box(self, "CargoLightHousing", Vector3(0, 4.09, z), Vector3(2.4, 0.12, 0.55), dark)
        _light("cargo", Vector3(0, 3.9, z), Color("#b4e4f0"), 2.3, 7.8, true)
    # Dim fixed emergency markers keep the cargo bay readable without its lights.
    for x in [-5.6, 5.6]:
        for z in [-25.4, -31.0]:
            _box(self, "CargoEmergencyGlow", Vector3(x, 0.38, z), Vector3(0.3, 0.35, 0.04), _mat(Color("#e66b50"), 0.1, 0.5, Color("#dd4e3c"), 1.0))
            var emergency := OmniLight3D.new()
            emergency.position = Vector3(x, 0.56, z)
            emergency.light_color = Color("#ff694d")
            emergency.light_energy = 0.24
            emergency.omni_range = 3.0
            add_child(emergency)


func _rigid_prop(item_name: String, at: Vector3, dimensions: Vector3, material: Material, mass: float = 0.8) -> RigidBody3D:
    var prop := RigidBody3D.new()
    prop.name = item_name
    prop.position = at
    prop.mass = mass
    prop.collision_layer = 4
    prop.collision_mask = 1
    prop.add_to_group("ship_pickable")
    add_child(prop)
    var mesh := BoxMesh.new()
    mesh.size = dimensions
    var drawable := MeshInstance3D.new()
    drawable.mesh = mesh
    drawable.material_override = material
    prop.add_child(drawable)
    var shape := BoxShape3D.new()
    shape.size = dimensions
    var collision := CollisionShape3D.new()
    collision.shape = shape
    prop.add_child(collision)
    _decorate_prop(prop, item_name, dimensions)
    return prop


func _decorate_prop(prop: RigidBody3D, item_name: String, dimensions: Vector3) -> void:
    var bright := _mat(Color("#c9d9df"), 0.73, 0.29, Color.BLACK, 0.0, false, "brushed")
    var rubber := _mat(Color("#8b9fab"), 0.2, 0.63, Color.BLACK, 0.0, false, "rubber")
    var screen := _mat(Color("#4ef0e0"), 0.0, 0.23, Color("#27c4c2"), 1.8, false, "console")
    match item_name:
        "DataSlate", "CrewNotebook":
            _box(prop, "InsetScreen", Vector3(0, dimensions.y * 0.5 + 0.007, 0), Vector3(dimensions.x * 0.76, 0.012, dimensions.z * 0.8), screen)
            _box(prop, "SlateEdge", Vector3(0, 0.006, dimensions.z * 0.5), Vector3(dimensions.x, dimensions.y, 0.024), rubber)
        "MetalMug":
            var handle := TorusMesh.new()
            handle.inner_radius = 0.04
            handle.outer_radius = 0.085
            var mesh := MeshInstance3D.new()
            mesh.mesh = handle
            mesh.rotation_degrees.x = 90
            mesh.position = Vector3(dimensions.x * 0.65, 0.02, 0)
            mesh.material_override = bright
            prop.add_child(mesh)
        "BatteryPack", "DiagnosticBlock":
            _box(prop, "BatteryContact", Vector3(0, dimensions.y * 0.5 + 0.005, 0), Vector3(dimensions.x * 0.74, 0.025, dimensions.z * 0.66), bright)
            _box(prop, "ChargeIndicator", Vector3(0, dimensions.y * 0.5 + 0.026, 0), Vector3(dimensions.x * 0.38, 0.015, dimensions.z * 0.2), screen)
        "SpareFuse":
            _box(prop, "FuseTop", Vector3(0, dimensions.y * 0.51, 0), Vector3(dimensions.x * 1.05, 0.025, dimensions.z * 1.05), bright)
            _box(prop, "FuseBottom", Vector3(0, -dimensions.y * 0.51, 0), Vector3(dimensions.x * 1.05, 0.025, dimensions.z * 1.05), bright)
        "SmallCargoParcel":
            _box(prop, "ParcelStrap", Vector3(0, dimensions.y * 0.5 + 0.007, 0), Vector3(dimensions.x * 0.22, 0.014, dimensions.z), rubber)


func _build_details() -> void:
    var steel := _mat(Color("#bbc9cf"), 0.75, 0.34, Color.BLACK, 0.0, false, "brushed")
    var muted := _mat(Color("#879ca6"), 0.46, 0.57, Color.BLACK, 0.0, false, "rubber")
    var upholstery := _mat(Color("#95b0bd"), 0.08, 0.86, Color.BLACK, 0.0, false, "fabric")
    var display := _mat(Color("#79e0e6"), 0.0, 0.24, Color("#32c4cf"), 0.75, false, "console")
    var amber := _mat(Color("#d9b998"), 0.22, 0.55, Color.BLACK, 0.0, false, "brushed")
    var cargo := _mat(Color("#becbd2"), 0.28, 0.62, Color.BLACK, 0.0, false, "cargo")

    # View-facing lounge furniture and liftable desk objects.
    _box(self, "PanoramaRail", Vector3(0, 0.78, 8.2), Vector3(12.5, 0.09, 0.16), steel)
    for x in [-5.6, 5.6]:
        _box(self, "ObservationBench", Vector3(x, 0.45, 6.65), Vector3(2.5, 0.52, 0.82), upholstery, true)
    _box(self, "SofaSeat", Vector3(-4.6, 0.48, 0.9), Vector3(3.4, 0.72, 1.25), upholstery, true)
    _box(self, "SofaBack", Vector3(-4.6, 1.1, 0.25), Vector3(3.4, 1.12, 0.3), upholstery, true)
    _box(self, "LoungeTable", Vector3(-2.8, 0.71, 2.75), Vector3(2.2, 0.15, 1.3), steel, true)
    _box(self, "LoungeTableBase", Vector3(-2.8, 0.33, 2.75), Vector3(0.2, 0.63, 0.42), muted, true)
    _rigid_prop("DataSlate", Vector3(-3.18, 1.02, 2.7), Vector3(0.38, 0.045, 0.56), display, 0.25)
    _rigid_prop("MetalMug", Vector3(-2.3, 1.02, 2.8), Vector3(0.2, 0.26, 0.2), steel, 0.3)

    _box(self, "EquipmentTable", Vector3(3.6, 0.76, 3.1), Vector3(2.6, 0.14, 1.2), steel, true)
    for x in [2.55, 4.65]:
        for z in [2.7, 3.5]:
            _box(self, "EquipmentTableLeg", Vector3(x, 0.36, z), Vector3(0.13, 0.73, 0.13), muted, true)
    _rigid_prop("BatteryPack", Vector3(4.25, 1.0, 3.1), Vector3(0.31, 0.26, 0.38), amber, 0.75)

    # Crew quarters: bed, personal belongings and a closable locker.
    _box(self, "BunkFrame", Vector3(-7.55, 0.4, -14.65), Vector3(4.4, 0.8, 1.85), steel, true)
    _box(self, "BunkMattress", Vector3(-7.55, 0.88, -14.65), Vector3(4.05, 0.18, 1.58), upholstery, true)
    _box(self, "Pillow", Vector3(-9.1, 1.03, -14.65), Vector3(0.65, 0.13, 1.18), amber)
    _box(self, "CrewShelf", Vector3(-5.15, 1.18, -10.8), Vector3(2.35, 0.13, 0.7), muted, true)
    _rigid_prop("CrewNotebook", Vector3(-5.5, 1.4, -10.8), Vector3(0.42, 0.08, 0.49), display, 0.28)

    # Engineering: workbench, tools, fixed terminal and additional physical props.
    _box(self, "WorkshopBench", Vector3(7.3, 0.82, -14.8), Vector3(4.3, 0.16, 1.7), steel, true)
    for x in [5.4, 9.2]:
        _box(self, "WorkshopBenchLeg", Vector3(x, 0.38, -14.8), Vector3(0.2, 0.76, 1.05), muted, true)
    _box(self, "WorkbenchScreenBack", Vector3(8.5, 1.68, -15.37), Vector3(1.15, 0.92, 0.12), muted)
    _box(self, "WorkbenchDisplay", Vector3(8.5, 1.68, -15.29), Vector3(1.0, 0.75, 0.02), display)
    _rigid_prop("SpareFuse", Vector3(6.35, 1.13, -14.4), Vector3(0.14, 0.28, 0.14), amber, 0.22)
    _rigid_prop("DiagnosticBlock", Vector3(7.1, 1.16, -14.4), Vector3(0.36, 0.38, 0.32), muted, 1.2)

    # Unlit cargo bay: stacked crates and a small handling platform.
    for x in [-4.5, 4.5]:
        for z in [-26.3, -30.0]:
            _box(self, "SecuredCargo", Vector3(x, 0.72, z), Vector3(1.75, 1.44, 1.5), cargo, true)
            _box(self, "CargoLatch", Vector3(x, 0.84, z + 0.77), Vector3(0.9, 0.09, 0.07), amber)
    _box(self, "CargoLoadingPlatform", Vector3(0, 0.21, -29.9), Vector3(2.8, 0.42, 1.8), muted, true)
    _rigid_prop("SmallCargoParcel", Vector3(0.2, 0.67, -29.9), Vector3(0.42, 0.42, 0.45), cargo, 1.25)


func _add_body_mesh(parent: Node3D, dimensions: Vector3, material: Material, center: Vector3 = Vector3.ZERO) -> void:
    var box_mesh := BoxMesh.new()
    box_mesh.size = dimensions
    var mesh_instance := MeshInstance3D.new()
    mesh_instance.mesh = box_mesh
    mesh_instance.position = center
    mesh_instance.material_override = material
    parent.add_child(mesh_instance)


func _add_body_collision(parent: CollisionObject3D, dimensions: Vector3, center: Vector3 = Vector3.ZERO) -> void:
    var shape := BoxShape3D.new()
    shape.size = dimensions
    var collision := CollisionShape3D.new()
    collision.shape = shape
    collision.position = center
    parent.add_child(collision)


func _make_door(id: String, at: Vector3, rotation_y: float, slide: Vector3) -> void:
    var body := AnimatableBody3D.new()
    body.name = "Door_" + id
    body.position = at
    body.rotation_degrees.y = rotation_y
    body.collision_layer = 1
    body.collision_mask = 0
    add_child(body)
    _add_body_mesh(body, Vector3(2.94, 3.12, 0.18), _mat(Color("#c6d1d8"), 0.7, 0.33, Color.BLACK, 0.0, false, "wall"))
    _add_body_mesh(body, Vector3(2.48, 0.07, 0.2), _mat(Color("#8ae5de"), 0.1, 0.25, Color("#64d7d8"), 1.3), Vector3(0, 0.28, 0.02))
    _add_body_mesh(body, Vector3(0.055, 2.8, 0.205), _mat(Color("#172935"), 0.5, 0.52), Vector3(0, 0, 0.02))
    _add_body_collision(body, Vector3(2.94, 3.12, 0.18))
    _doors[id] = {"body": body, "closed": at, "opened": at + slide, "open": false}


func _make_control(item_name: String, at: Vector3, rotation_y: float, kind: String, id: String, color: Color = Color("#53d4d2")) -> StaticBody3D:
    var panel := StaticBody3D.new()
    panel.name = item_name
    panel.position = at
    panel.rotation_degrees.y = rotation_y
    panel.collision_layer = 2
    panel.collision_mask = 0
    panel.add_to_group("ship_interactable")
    panel.set_meta("kind", kind)
    panel.set_meta("id", id)
    add_child(panel)
    _add_body_mesh(panel, Vector3(0.63, 0.85, 0.2), _mat(Color("#9bafb9"), 0.58, 0.43, Color.BLACK, 0.0, false, "console"))
    _add_body_mesh(panel, Vector3(0.44, 0.59, 0.023), _mat(color, 0.08, 0.25, color, 1.65), Vector3(0, 0, 0.113))
    _add_body_collision(panel, Vector3(0.63, 0.85, 0.2))
    return panel


func _build_doors() -> void:
    _make_door("lounge", Vector3(0, 1.56, -5.95), 0, Vector3(3.3, 0, 0))
    _make_door("cabin", Vector3(-2.25, 1.56, -13.0), 90, Vector3(0, 0, -3.3))
    _make_door("workshop", Vector3(2.25, 1.56, -13.0), 90, Vector3(0, 0, -3.3))
    _make_door("cargo", Vector3(0, 1.56, -23.55), 0, Vector3(3.3, 0, 0))
    _make_control("LoungeDoorControl", Vector3(-2.35, 1.6, -5.62), 0, "door", "lounge")
    _make_control("CabinDoorControl", Vector3(-1.91, 1.6, -10.9), 90, "door", "cabin")
    _make_control("WorkshopDoorControl", Vector3(1.91, 1.6, -10.9), -90, "door", "workshop")
    _make_control("CargoDoorControl", Vector3(-2.1, 1.6, -23.24), 0, "door", "cargo", Color("#e0b177"))


func _build_interactables() -> void:
    # A collectible physical flashlight rests on the lounge equipment table.
    _flashlight_pickup = StaticBody3D.new()
    _flashlight_pickup.name = "FlashlightPickup"
    _flashlight_pickup.position = Vector3(3.32, 1.05, 3.1)
    _flashlight_pickup.collision_layer = 2
    _flashlight_pickup.collision_mask = 0
    _flashlight_pickup.add_to_group("ship_interactable")
    _flashlight_pickup.set_meta("kind", "flashlight")
    add_child(_flashlight_pickup)
    var torch_mesh := CylinderMesh.new()
    torch_mesh.top_radius = 0.08
    torch_mesh.bottom_radius = 0.075
    torch_mesh.height = 0.49
    var torch_model := MeshInstance3D.new()
    torch_model.mesh = torch_mesh
    torch_model.rotation_degrees.z = 90.0
    torch_model.material_override = _mat(Color("#b3c2c9"), 0.72, 0.3, Color.BLACK, 0.0, false, "brushed")
    _flashlight_pickup.add_child(torch_model)
    _add_body_collision(_flashlight_pickup, Vector3(0.55, 0.19, 0.22))
    _box(_flashlight_pickup, "TorchTip", Vector3(-0.25, 0, 0), Vector3(0.085, 0.18, 0.2), _mat(Color("#f2f4e7"), 0.0, 0.5, Color("#cce7df"), 0.4))

    # Light breaker at the cargo entrance (accessible with the flashlight).
    _make_control("CargoLightBreaker", Vector3(2.45, 1.55, -24.6), -90, "light", "cargo", Color("#db966a"))
    # Independent lounge light controls.
    _make_control("LoungeLightBreaker", Vector3(-7.58, 1.6, 5.7), 90, "light", "lounge")

    # Engineering terminal: on/off interaction is distinct from door switches.
    _make_control("EngineeringTerminal", Vector3(5.3, 1.58, -14.65), 0, "terminal", "systems", Color("#79c7ff"))
    _terminal_display = _sign("STANDBY\nSYSTEMS READY", Vector3(5.65, 2.52, -16.76), 0, Color("#9ddfd6"), 37)

    # Cabin-facing locker: open front points toward NEGATIVE Z (into the cabin),
    # never into the bulkhead at Z=-9. The empty shell remains visible when opened.
    var locker_frame := _mat(Color("#b7c8d0"), 0.65, 0.39, Color.BLACK, 0.0, false, "brushed")
    _box(self, "LockerBack", Vector3(-9.05, 1.17, -9.87), Vector3(2.07, 2.32, 0.13), locker_frame, true)
    _box(self, "LockerLeft", Vector3(-10.07, 1.17, -10.26), Vector3(0.12, 2.32, 0.83), locker_frame, true)
    _box(self, "LockerRight", Vector3(-8.03, 1.17, -10.26), Vector3(0.12, 2.32, 0.83), locker_frame, true)
    _box(self, "LockerRoof", Vector3(-9.05, 2.32, -10.26), Vector3(2.1, 0.1, 0.83), locker_frame, true)
    _box(self, "LockerFloor", Vector3(-9.05, 0.08, -10.26), Vector3(2.1, 0.16, 0.83), locker_frame, true)
    _box(self, "LockerShelf", Vector3(-9.05, 1.16, -10.25), Vector3(1.89, 0.06, 0.66), locker_frame, true)
    _locker = AnimatableBody3D.new()
    _locker.name = "CabinLockerDoor"
    _locker.position = Vector3(-10.02, 1.19, -10.74)
    _locker.collision_layer = 1
    _locker.collision_mask = 0
    add_child(_locker)
    _add_body_mesh(_locker, Vector3(1.92, 2.12, 0.09), _mat(Color("#c1d0d7"), 0.72, 0.35, Color.BLACK, 0.0, false, "cargo"), Vector3(0.96, 0, 0))
    _add_body_collision(_locker, Vector3(1.92, 2.12, 0.09), Vector3(0.96, 0, 0))
    _box(_locker, "LockerEdge", Vector3(1.72, 0, -0.08), Vector3(0.12, 1.6, 0.06), locker_frame)
    # The clickable latch is mounted on the actual door, so it moves with it.
    var latch := StaticBody3D.new()
    latch.name = "LockerHandle"
    latch.position = Vector3(1.73, 0, -0.15)
    latch.collision_layer = 2
    latch.collision_mask = 0
    latch.add_to_group("ship_interactable")
    latch.set_meta("kind", "locker")
    latch.set_meta("id", "crew")
    _locker.add_child(latch)
    _add_body_mesh(latch, Vector3(0.12, 0.41, 0.18), locker_frame)
    _add_body_collision(latch, Vector3(0.23, 0.5, 0.24))

    # Panoramic window shutters: open on spawn so the view is immediate.
    _shutter = AnimatableBody3D.new()
    _shutter.name = "ObservationShutter"
    _shutter.position = Vector3(0, 6.28, 8.83)
    _shutter.collision_layer = 1
    _shutter.collision_mask = 0
    add_child(_shutter)
    _add_body_mesh(_shutter, Vector3(11.8, 2.76, 0.13), _mat(Color("#afbfc8"), 0.65, 0.32, Color.BLACK, 0.0, false, "wall"))
    _add_body_collision(_shutter, Vector3(11.8, 2.76, 0.13))
    for x in [-4.4, -1.4, 1.6, 4.6]:
        _add_body_mesh(_shutter, Vector3(0.09, 2.6, 0.14), _mat(Color("#bdcad0"), 0.74, 0.35, Color.BLACK, 0.0, false, "brushed"), Vector3(x, 0, 0))
    _make_control("PanoramaShutterControl", Vector3(6.55, 1.66, 8.65), 0, "shutter", "window", Color("#7ac4f1"))


func toggle_door(id: String) -> void:
    if not _doors.has(id):
        return
    var entry: Dictionary = _doors[id]
    var open := not bool(entry["open"])
    entry["open"] = open
    var door: AnimatableBody3D = entry["body"]
    if _door_tweens.has(id):
        var prior: Tween = _door_tweens[id]
        if prior.is_running():
            prior.kill()
    var tween := create_tween()
    tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
    var destination: Vector3 = entry["opened"] if open else entry["closed"]
    tween.tween_property(door, "position", destination, 1.0)
    _door_tweens[id] = tween
    _show_notice(_tr("SCHOTT GEÖFFNET" if open else "SCHOTT GESCHLOSSEN", "BULKHEAD OPEN" if open else "BULKHEAD CLOSED"))
    _on_focus(_focused)


func toggle_zone(id: String) -> void:
    if not _zone_enabled.has(id):
        return
    var is_on := not bool(_zone_enabled[id])
    _zone_enabled[id] = is_on
    var lights: Array = _zone_lights[id]
    for node in lights:
        var light: OmniLight3D = node
        light.visible = is_on
    _show_notice(_tr("BELEUCHTUNG AKTIV" if is_on else "BELEUCHTUNG AUS", "LIGHTS ON" if is_on else "LIGHTS OFF"))
    _on_focus(_focused)


func take_flashlight() -> void:
    if player.get("owns_flashlight") or _flashlight_pickup == null:
        return
    _flashlight_pickup.collision_layer = 0
    _flashlight_pickup.visible = false
    player.call("collect_flashlight")
    _show_notice(_tr("TASCHENLAMPE GEFUNDEN · F ZUM EINSCHALTEN", "FLASHLIGHT COLLECTED · PRESS F TO TURN ON"))
    _update_language()
    _on_focus(null)


func toggle_locker() -> void:
    _locker_open = not _locker_open
    if _locker_tween != null and _locker_tween.is_running():
        _locker_tween.kill()
    _locker_tween = create_tween()
    _locker_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
    _locker_tween.tween_property(_locker, "rotation_degrees:y", 95.0 if _locker_open else 0.0, 0.8)
    _show_notice(_tr("SCHRANK GEÖFFNET" if _locker_open else "SCHRANK GESCHLOSSEN", "LOCKER OPEN" if _locker_open else "LOCKER CLOSED"))
    _on_focus(_focused)


func toggle_shutter() -> void:
    _shutter_closed = not _shutter_closed
    if _shutter_tween != null and _shutter_tween.is_running():
        _shutter_tween.kill()
    _shutter_tween = create_tween()
    _shutter_tween.set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
    _shutter_tween.tween_property(_shutter, "position:y", 2.28 if _shutter_closed else 6.28, 1.5)
    _show_notice(_tr("PANORAMABLENDEN GESCHLOSSEN" if _shutter_closed else "PANORAMABLENDEN GEÖFFNET", "PANORAMA SHUTTERS CLOSED" if _shutter_closed else "PANORAMA SHUTTERS OPEN"))
    _on_focus(_focused)


func toggle_terminal() -> void:
    _terminal_online = not _terminal_online
    _terminal_display.text = "SHIP SYSTEMS\nONLINE" if _terminal_online else "SHIP SYSTEMS\nSTANDBY"
    _terminal_display.modulate = Color("#9effc6") if _terminal_online else Color("#9ddfd6")
    _show_notice(_tr("BORDSYSTEM AKTIV" if _terminal_online else "BORDSYSTEM IM STANDBY", "SHIP SYSTEMS ONLINE" if _terminal_online else "SHIP SYSTEMS STANDBY"))
    _on_focus(_focused)


func _on_use(target: Object) -> void:
    if not target is Node:
        return
    var node: Node = target
    var kind := String(node.get_meta("kind", ""))
    var id := String(node.get_meta("id", ""))
    match kind:
        "door":
            toggle_door(id)
        "light":
            toggle_zone(id)
        "flashlight":
            take_flashlight()
        "terminal":
            toggle_terminal()
        "locker":
            toggle_locker()
        "shutter":
            toggle_shutter()


func _on_focus(target: Object) -> void:
    _focused = target
    if _prompt == null:
        return
    if player.get("held_prop") != null:
        _prompt.text = _tr("[E / G]  Gegenstand ablegen", "[E / G]  Drop held object")
        return
    if target is RigidBody3D and target.is_in_group("ship_pickable"):
        _prompt.text = _tr("[E]  Gegenstand aufnehmen", "[E]  Pick up object")
        return
    if not target is Node:
        _prompt.text = ""
        return
    var node: Node = target
    var kind := String(node.get_meta("kind", ""))
    var id := String(node.get_meta("id", ""))
    match kind:
        "door":
            var is_open := bool(_doors[id]["open"])
            _prompt.text = _tr("[E]  Schott schließen" if is_open else "[E]  Schott öffnen", "[E]  Close bulkhead" if is_open else "[E]  Open bulkhead")
        "flashlight":
            _prompt.text = _tr("[E]  Taschenlampe aufnehmen", "[E]  Pick up flashlight")
        "light":
            var on := bool(_zone_enabled[id])
            _prompt.text = _tr("[E]  Licht ausschalten" if on else "[E]  Licht einschalten", "[E]  Turn lights off" if on else "[E]  Turn lights on")
        "shutter":
            _prompt.text = _tr("[E]  Panoramablenden öffnen" if _shutter_closed else "[E]  Panoramablenden schließen", "[E]  Open panorama shutters" if _shutter_closed else "[E]  Close panorama shutters")
        "locker":
            _prompt.text = _tr("[E]  Schrank schließen" if _locker_open else "[E]  Schrank öffnen", "[E]  Close locker" if _locker_open else "[E]  Open locker")
        "terminal":
            _prompt.text = _tr("[E]  Bordsystem in Standby" if _terminal_online else "[E]  Bordsystem aktivieren", "[E]  Set ship system to standby" if _terminal_online else "[E]  Activate ship system")
        _:
            _prompt.text = ""


func _on_flashlight_changed(is_on: bool) -> void:
    if player.get("owns_flashlight"):
        _inventory.text = _tr("TASCHENLAMPE  [F]   •   AN" if is_on else "TASCHENLAMPE  [F]   •   AUS", "FLASHLIGHT  [F]   •   ON" if is_on else "FLASHLIGHT  [F]   •   OFF")
    if _notice != null and player.get("owns_flashlight"):
        _show_notice(_tr("TASCHENLAMPE AN" if is_on else "TASCHENLAMPE AUS", "FLASHLIGHT ON" if is_on else "FLASHLIGHT OFF"))


func _on_prop_held(is_held: bool) -> void:
    _show_notice(_tr("GEGENSTAND AUFGENOMMEN" if is_held else "GEGENSTAND ABGELEGT", "OBJECT PICKED UP" if is_held else "OBJECT DROPPED"))
    _on_focus(_focused)


func _zone_title(id: String) -> String:
    match id:
        "lounge":
            return _tr("01 / AUSSICHTSDECK", "01 / OBSERVATION DECK")
        "corridor":
            return _tr("02 / ZENTRALGANG", "02 / CENTRAL SPINE")
        "cabin":
            return _tr("03 / MANNSCHAFTSQUARTIER", "03 / CREW QUARTERS")
        "workshop":
            return _tr("04 / MASCHINENRAUM", "04 / ENGINEERING")
        "cargo":
            return _tr("05 / FRACHTRAUM", "05 / CARGO BAY")
    return id



func _build_finish_details() -> void:
    # Architectural detailing deliberately sits away from base surfaces to avoid
    # coplanar geometry and the patterned flicker it can cause at distance.
    var trim := _mat(Color("#b9c8d1"), 0.76, 0.27, Color.BLACK, 0.0, false, "brushed")
    var inset := _mat(Color("#829ba7"), 0.43, 0.61, Color.BLACK, 0.0, false, "wall")
    var grille := _mat(Color("#7d949e"), 0.56, 0.48, Color.BLACK, 0.0, false, "ceiling")
    var rubber := _mat(Color("#9babb2"), 0.09, 0.84, Color.BLACK, 0.0, false, "rubber")
    var crates := _mat(Color("#b2c2ca"), 0.43, 0.51, Color.BLACK, 0.0, false, "cargo")
    var led := _mat(Color("#96fff5"), 0.0, 0.19, Color("#3bf0db"), 1.45)
    var caution := _mat(Color("#f1c498"), 0.2, 0.5, Color("#e69b62"), 0.36)

    # Panel subdivisions, shallow recessed instruments and the non-repeating
    # high-frequency details that were missing from the original blockout.
    for x in [-7.65, 7.65]:
        for z in [-3.0, 0.5, 3.9]:
            _box(self, "LoungeInlaidWall", Vector3(x, 2.1, z), Vector3(0.07, 2.55, 2.65), inset)
            for dz in [-1.17, 1.17]:
                _box(self, "LoungePanelEdge", Vector3(x + (-0.044 if x < 0 else 0.044), 2.1, z + dz), Vector3(0.025, 2.67, 0.035), trim)
            _box(self, "LoungeMaintenanceLine", Vector3(x + (-0.047 if x < 0 else 0.047), 3.03, z), Vector3(0.024, 0.034, 2.21), led)

    # Ventilation and access hatches along the narrow central spine.
    for z in [-8.9, -18.7, -21.2]:
        for x in [-2.02, 2.02]:
            _box(self, "SpineServiceAccess", Vector3(x, 2.1, z), Vector3(0.055, 1.55, 1.2), inset)
            for offset in [-0.44, -0.22, 0.0, 0.22, 0.44]:
                _box(self, "SpineVentSlat", Vector3(x + (-0.039 if x < 0 else 0.039), 2.1, z + offset), Vector3(0.022, 0.95, 0.034), grille)
        _box(self, "CeilingServiceBeam", Vector3(0, 4.09, z), Vector3(4.0, 0.13, 0.17), trim)

    # A continuous handrail indicates traversable ship architecture.
    for x in [-7.75, 7.75]:
        _box(self, "LoungeHandrail", Vector3(x, 1.05, 1.0), Vector3(0.14, 0.12, 11.7), trim)
    for x in [-2.02, 2.02]:
        for z in [-8.3, -18.7]:
            _box(self, "SpineHandrail", Vector3(x, 1.07, z), Vector3(0.12, 0.11, 4.0), trim)

    # Floor inset plates and overhead ribs distinguish each room from plain boxes.
    for x in [-4.6, 0.0, 4.6]:
        for z in [-3.6, 0.0, 4.8]:
            _box(self, "LoungeFloorTread", Vector3(x, 0.041, z), Vector3(3.6, 0.055, 2.5), rubber)
            _box(self, "LoungeTreadTrim", Vector3(x, 0.069, z - 1.18), Vector3(3.6, 0.012, 0.055), trim)
    for z in [-12.2, -15.2]:
        _box(self, "CrewCeilingRib", Vector3(-6.3, 4.08, z), Vector3(7.7, 0.18, 0.2), trim)
        _box(self, "EngineeringCeilingRib", Vector3(6.3, 4.08, z), Vector3(7.7, 0.18, 0.2), trim)
    for z in [-25.2, -29.6]:
        _box(self, "CargoTruss", Vector3(0, 4.03, z), Vector3(12.5, 0.24, 0.31), crates)
        for x in [-4.8, -2.4, 0.0, 2.4, 4.8]:
            _box(self, "CargoTrussBolt", Vector3(x, 3.88, z), Vector3(0.22, 0.08, 0.2), trim)

    # Cargo equipment: grilles, raised warning strips and textured safety ribs.
    for x in [-4.5, 4.5]:
        for z in [-26.3, -30.0]:
            _box(self, "CargoRibTop", Vector3(x, 1.47, z), Vector3(1.55, 0.06, 0.13), trim)
            _box(self, "CargoRibFront", Vector3(x, 0.83, z + 0.765), Vector3(1.55, 1.12, 0.048), crates)
            _box(self, "CargoHazardStripe", Vector3(x, 1.41, z + 0.796), Vector3(1.46, 0.06, 0.02), caution)

    # Visible cable runs and equipment casings give each room an identity.
    for x in [-7.9, -6.9, -5.9]:
        _box(self, "CrewLockerDuct", Vector3(x, 3.88, -15.65), Vector3(0.31, 0.22, 2.1), grille)
    for x in [4.9, 6.7, 8.5]:
        _box(self, "EngineeringPipe", Vector3(x, 3.79, -15.8), Vector3(0.12, 0.12, 1.9), trim)
    _box(self, "EngineeringAccessPanel", Vector3(9.95, 2.04, -13.1), Vector3(0.08, 1.62, 2.32), inset)
    for z in [-13.95, -13.63, -13.31, -12.99, -12.67, -12.35]:
        _box(self, "EngineeringVentSlot", Vector3(9.89, 2.05, z), Vector3(0.08, 0.85, 0.09), grille)
    _sign("VESPER  /  O2  98.6%  /  DECK 01", Vector3(-4.55, 2.3, -5.68), 0, Color("#b5ddd8"), 32)
    _sign("SECURITY  /  STORAGE", Vector3(-3.45, 2.65, -32.47), 0, Color("#e7bb8e"), 36)


func _hud_text(text: String, font_size: int, color: Color) -> Label:
    var lbl := Label.new()
    lbl.text = text
    lbl.add_theme_font_size_override("font_size", font_size)
    lbl.add_theme_color_override("font_color", color)
    lbl.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.85))
    lbl.add_theme_constant_override("shadow_offset_x", 1)
    lbl.add_theme_constant_override("shadow_offset_y", 2)
    lbl.mouse_filter = Control.MOUSE_FILTER_IGNORE
    return lbl


func _build_hud() -> void:
    var canvas := CanvasLayer.new()
    canvas.name = "HUD"
    add_child(canvas)
    var overlay := Control.new()
    overlay.name = "Overlay"
    overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
    canvas.add_child(overlay)
    overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

    var rule := ColorRect.new()
    rule.color = Color("#69cbd0")
    rule.position = Vector2(32, 26)
    rule.size = Vector2(4, 54)
    rule.mouse_filter = Control.MOUSE_FILTER_IGNORE
    overlay.add_child(rule)

    _title = _hud_text("", 24, Color("#e7f5f7"))
    _title.position = Vector2(51, 22)
    overlay.add_child(_title)
    _subtitle = _hud_text("", 14, Color("#9fb5c6"))
    _subtitle.position = Vector2(53, 56)
    overlay.add_child(_subtitle)
    _zone_label = _hud_text("", 16, Color("#c4e6e7"))
    _zone_label.anchor_left = 1.0
    _zone_label.anchor_right = 1.0
    _zone_label.offset_left = -430
    _zone_label.offset_right = -28
    _zone_label.offset_top = 32
    _zone_label.offset_bottom = 70
    _zone_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
    overlay.add_child(_zone_label)

    _inventory = _hud_text("", 16, Color("#b3e8e3"))
    _inventory.position = Vector2(36, 104)
    overlay.add_child(_inventory)
    _notice = _hud_text("", 18, Color("#9df5da"))
    _notice.anchor_left = 0.5
    _notice.anchor_right = 0.5
    _notice.offset_left = -410
    _notice.offset_right = 410
    _notice.offset_top = 116
    _notice.offset_bottom = 154
    _notice.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    overlay.add_child(_notice)

    _prompt = _hud_text("", 20, Color("#fff4d9"))
    _prompt.anchor_left = 0.5
    _prompt.anchor_right = 0.5
    _prompt.anchor_top = 0.7
    _prompt.anchor_bottom = 0.7
    _prompt.offset_left = -420
    _prompt.offset_right = 420
    _prompt.offset_top = 0
    _prompt.offset_bottom = 45
    _prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    overlay.add_child(_prompt)

    _hint = _hud_text("", 15, Color("#d5e5ed"))
    _hint.anchor_top = 1.0
    _hint.anchor_bottom = 1.0
    _hint.offset_left = 35
    _hint.offset_right = 1210
    _hint.offset_top = -45
    _hint.offset_bottom = -12
    overlay.add_child(_hint)

    var crosshair_h := ColorRect.new()
    crosshair_h.color = Color("#e6f8f5")
    crosshair_h.anchor_left = 0.5
    crosshair_h.anchor_right = 0.5
    crosshair_h.anchor_top = 0.5
    crosshair_h.anchor_bottom = 0.5
    crosshair_h.offset_left = -7
    crosshair_h.offset_right = 7
    crosshair_h.offset_top = -1
    crosshair_h.offset_bottom = 1
    crosshair_h.mouse_filter = Control.MOUSE_FILTER_IGNORE
    overlay.add_child(crosshair_h)
    var crosshair_v := ColorRect.new()
    crosshair_v.color = crosshair_h.color
    crosshair_v.anchor_left = 0.5
    crosshair_v.anchor_right = 0.5
    crosshair_v.anchor_top = 0.5
    crosshair_v.anchor_bottom = 0.5
    crosshair_v.offset_left = -1
    crosshair_v.offset_right = 1
    crosshair_v.offset_top = -7
    crosshair_v.offset_bottom = 7
    crosshair_v.mouse_filter = Control.MOUSE_FILTER_IGNORE
    overlay.add_child(crosshair_v)


func _update_language() -> void:
    if _title == null:
        return
    _title.text = "VESPER / STARSHIP EXPLORER"
    _subtitle.text = _tr("UNABHÄNGIGER GODOT-3D-PROTOTYP", "STANDALONE GODOT 3D PROTOTYPE")
    _hint.text = _tr("WASD Laufen   ·   Maus Umsehen   ·   Shift Sprint   ·   E Benutzen / Greifen   ·   F Taschenlampe   ·   G Ablegen   ·   F2 Sprache   ·   Esc Maus / Ende", "WASD Move   ·   Mouse Look   ·   Shift Sprint   ·   E Use / Grab   ·   F Flashlight   ·   G Drop   ·   F2 Language   ·   Esc Mouse / Exit")
    _inventory.text = _tr("TASCHENLAMPE: NOCH NICHT GEFUNDEN", "FLASHLIGHT: NOT COLLECTED")
    if player.get("owns_flashlight"):
        _on_flashlight_changed(bool(player.get("flashlight_on")))
    _zone_label.text = _zone_title(_last_zone if _last_zone != "" else "lounge")
    _on_focus(_focused)


func _show_notice(text: String) -> void:
    if _notice == null:
        return
    _notice.text = text
    _notice_left = 2.4
