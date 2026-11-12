extends Node3D

const RoomScenes = preload("res://room_scenes.gd")
const CAMERA_HOME := Vector3(0, 2.35, 7.5)

# Presentation-only WebGL 2 scene. This project never owns wagers, ghost RNG,
# multipliers, rewards or the Manor save journal.
# The parent React document sends lawful destination choices; C# decides live
# results. Demo choices are simulated only in the parent, not persisted.

@onready var camera: Camera3D = $Camera

const DOOR_POSITIONS: Array[Vector3] = [
    Vector3(-4.9, 0, -8.2),
    Vector3(0, 0, -8.2),
    Vector3(4.9, 0, -8.2),
]

const WARM := Color("#d8ab74")
const MOON := Color("#9caed3")
const BASE := Color("#e1d4c6")

var doors: Array[Node3D] = []
var door_panels: Array[Node3D] = []
var door_colliders: Array[StaticBody3D] = []
var door_names: Array[Label3D] = []
var choice_ids: Array[int] = []
var room_name: Label3D
var subtitle: Label3D
var demo_mode := false
var _web_poll := 0.0
var _selected := false
var _lighting: OmniLight3D
var _initialized_web := false
var _room_builder = RoomScenes.new()
var _room_visuals: Node3D
var _room_id := -1
var _active := true
var _reduced_motion := false
var _breath_clock := 0.0
var _door_tween: Tween
var _options_signature := ""


func _ready() -> void:
    # Fill the actual slot aspect ratio rather than preserving a 16:9 image
    # with black pillarboxing inside a wider WebView2 iframe.
    get_tree().root.content_scale_aspect = Window.CONTENT_SCALE_ASPECT_EXPAND
    _build_environment()
    _build_doors()
    _build_labels()
    _set_room("Entrance Hall", 0)
    if OS.has_feature("web"):
        _active = false
        Engine.max_fps = 8
        _initialize_web_bridge()


func _mat(color: Color, metallic: float = 0.0, roughness: float = 0.75,
        texture: Texture2D = null, emission: Color = Color.BLACK, energy: float = 0.0) -> StandardMaterial3D:
    var m := StandardMaterial3D.new()
    m.albedo_color = color
    m.metallic = metallic
    m.roughness = roughness
    if texture != null:
        m.albedo_texture = texture
        m.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
        m.uv1_triplanar = true
        m.uv1_world_triplanar = true
        m.uv1_scale = Vector3(0.5, 0.5, 0.5)
    if energy > 0.0:
        m.emission_enabled = true
        m.emission = emission
        m.emission_energy_multiplier = energy
    return m


func _cube(parent: Node3D, label: String, at: Vector3, size: Vector3,
        material: Material) -> MeshInstance3D:
    var mesh := BoxMesh.new()
    mesh.size = size
    var node := MeshInstance3D.new()
    node.name = label
    node.position = at
    node.mesh = mesh
    node.material_override = material
    parent.add_child(node)
    return node


func _cylinder(parent: Node3D, label: String, at: Vector3, radius: float,
        height: float, material: Material) -> MeshInstance3D:
    var m := CylinderMesh.new()
    m.top_radius = radius
    m.bottom_radius = radius
    m.height = height
    m.radial_segments = 16
    var node := MeshInstance3D.new()
    node.name = label
    node.mesh = m
    node.position = at
    node.material_override = material
    parent.add_child(node)
    return node


func _sphere(parent: Node3D, label: String, at: Vector3, radius: float,
        material: Material) -> MeshInstance3D:
    var m := SphereMesh.new()
    m.radius = radius
    m.height = radius * 2.0
    m.radial_segments = 12
    m.rings = 6
    var node := MeshInstance3D.new()
    node.name = label
    node.mesh = m
    node.position = at
    node.material_override = material
    parent.add_child(node)
    return node


func _label(parent: Node3D, label_text: String, at: Vector3, size: int = 46,
        color: Color = BASE) -> Label3D:
    var l := Label3D.new()
    l.text = label_text
    l.position = at
    l.font_size = size
    l.pixel_size = 0.004
    l.modulate = color
    l.outline_modulate = Color("#15111c")
    l.outline_size = 4
    l.billboard = BaseMaterial3D.BILLBOARD_DISABLED
    parent.add_child(l)
    return l


func _build_environment() -> void:
    var world := WorldEnvironment.new()
    world.name = "Atmosphere"
    var env := Environment.new()
    env.background_mode = Environment.BG_COLOR
    env.background_color = Color("#080a14")
    env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    env.ambient_light_color = Color("#b7a2a6")
    env.ambient_light_energy = 0.22
    world.environment = env
    add_child(world)

    var moonlight := DirectionalLight3D.new()
    moonlight.name = "Moonlight"
    moonlight.rotation_degrees = Vector3(-50, -20, 0)
    moonlight.light_color = MOON
    moonlight.light_energy = 0.35
    moonlight.shadow_enabled = false
    add_child(moonlight)

    _lighting = OmniLight3D.new()
    _lighting.name = "ChandelierLight"
    _lighting.position = Vector3(0, 4.3, -1.4)
    _lighting.light_color = WARM
    _lighting.light_energy = 2.0
    _lighting.omni_range = 18.0
    _lighting.shadow_enabled = false
    _lighting.shadow_bias = 0.065
    add_child(_lighting)


func _build_room() -> void:
    var wood := _mat(Color("#b5a094"), 0.02, 0.62, preload("res://art/wood.svg"))
    var wallpaper := _mat(Color("#b9b0b6"), 0.02, 0.83, preload("res://art/wallpaper.svg"))
    var marble := _mat(Color("#bfc0c9"), 0.08, 0.4, preload("res://art/marble.svg"))
    var brass := _mat(Color("#d1b98d"), 0.8, 0.3, preload("res://art/brass.svg"))
    var fabric := _mat(Color("#8e6a6e"), 0.03, 0.91, preload("res://art/fabric.svg"))
    var dark_wood := _mat(Color("#66504a"), 0.13, 0.71, preload("res://art/wood.svg"))

    _cube(self, "Parquet", Vector3(0, -0.22, -1), Vector3(18.8, 0.38, 21.0), wood)
    _cube(self, "Ceiling", Vector3(0, 6.1, -1), Vector3(18.8, 0.28, 21), wallpaper)
    _cube(self, "WestWall", Vector3(-9.35, 2.94, -1), Vector3(0.32, 6.0, 21.0), wallpaper)
    _cube(self, "EastWall", Vector3(9.35, 2.94, -1), Vector3(0.32, 6.0, 21.0), wallpaper)
    _cube(self, "BackWall", Vector3(0, 2.94, -11.45), Vector3(18.8, 6.0, 0.32), wallpaper)
    _cube(self, "FrontWall", Vector3(0, 2.94, 9.4), Vector3(18.8, 6.0, 0.32), wallpaper)

    for x in [-9.15, 9.15]:
        _cube(self, "TimberDado", Vector3(x, 1.06, -1.0), Vector3(0.25, 2.1, 20.2), dark_wood)
        _cube(self, "TimberRail", Vector3(x, 2.19, -1.0), Vector3(0.27, 0.14, 20.2), brass)
        for z in [-7.5, -4.2, -0.9, 2.4, 5.7]:
            _cube(self, "WallMoulding", Vector3(x + (-0.055 if x<0 else 0.055), 3.85, z), Vector3(0.08, 2.1, 2.8), dark_wood)
    _cube(self, "RearWainscoting", Vector3(0, 1.02, -11.25), Vector3(18.4, 2.0, 0.16), dark_wood)
    _cube(self, "RearRail", Vector3(0, 2.1, -11.22), Vector3(18.4, 0.16, 0.18), brass)

    # The central carpet has a woven material and an inlaid brass border.
    _cube(self, "Carpet", Vector3(0, 0.025, -0.55), Vector3(4.35, 0.035, 14.2), fabric)
    for x in [-2.15, 2.15]:
        _cube(self, "RunnerBorder", Vector3(x, 0.052, -0.55), Vector3(0.045, 0.015, 14.25), brass)

    for x in [-7.6, -2.7, 2.7, 7.6]:
        for z in [-9.0, 5.6]:
            _cylinder(self, "MarbleColumn", Vector3(x, 2.65, z), 0.36, 5.1, marble)
            _cylinder(self, "ColumnPlinth", Vector3(x, 0.32, z), 0.53, 0.62, dark_wood)
            _cylinder(self, "ColumnCapital", Vector3(x, 5.21, z), 0.55, 0.31, brass)
    for z in [-8.45, -4.7, -1.1, 2.6, 6.3]:
        _cube(self, "CeilingRib", Vector3(0, 5.86, z), Vector3(18.5, 0.24, 0.2), dark_wood)
        _cube(self, "RibGilding", Vector3(0, 5.72, z), Vector3(18.1, 0.03, 0.06), brass)

    # Functional room is kept distinct from the React-side entry animation.
    _cube(self, "RaisedThreshold", Vector3(0, 0.08, -8.0), Vector3(16.3, 0.09, 0.42), marble)


func _build_hall_details() -> void:
    var brass := _mat(Color("#d7b789"), 0.79, 0.28, preload("res://art/brass.svg"))
    var blueglass := _mat(Color("#8fb4d1"), 0.05, 0.22, preload("res://art/glass.svg"), Color("#375b86"), 0.26)
    var deepwood := _mat(Color("#66504a"), 0.08, 0.74, preload("res://art/wood.svg"))
    for x in [-9.07, 9.07]:
        for z in [-5.25, 2.75]:
            _cube(self, "MoonWindow", Vector3(x, 3.86, z), Vector3(0.07, 2.24, 2.05), blueglass)
            for dz in [-0.93, 0.0, 0.93]:
                _cube(self, "WindowMullion", Vector3(x + (-0.08 if x<0 else 0.08), 3.86, z+dz), Vector3(0.16, 2.34, 0.07), brass)
            for dy in [-1.08, 0.0, 1.08]:
                _cube(self, "WindowLead", Vector3(x + (-0.09 if x<0 else 0.09), 3.86+dy, z), Vector3(0.17, 0.06, 2.18), brass)
            var light := OmniLight3D.new()
            light.position = Vector3(x + (1.1 if x<0 else -1.1), 3.85, z)
            light.omni_range = 6.8
            light.light_energy = 0.6
            light.light_color = Color("#91b4df")
            add_child(light)

    # Chandelier is suspended over the camera approach, with emissive candle tips.
    _cylinder(self, "CeilingChain", Vector3(0, 5.22, 0.2), 0.05, 1.65, brass)
    _cylinder(self, "ChandelierCore", Vector3(0, 4.29, 0.2), 0.14, 1.1, brass)
    for i in range(8):
        var angle := TAU * float(i) / 8.0
        var point := Vector3(cos(angle) * 1.4, 4.01, 0.2 + sin(angle) * 1.4)
        var arm := _cube(self, "ChandelierArm", Vector3(point.x*0.5, 4.05, 0.2+sin(angle)*0.7), Vector3(1.35, 0.085, 0.085), brass)
        arm.rotation.y = -angle
        _cylinder(self, "ChandelierCandle", point + Vector3(0, 0.22, 0), 0.072, 0.45, deepwood)
        _sphere(self, "CandleFlame", point + Vector3(0, 0.5, 0), 0.09,
            _mat(Color("#ffeccc"), 0.0, 0.36, null, Color("#ffb87a"), 2.0))
    for x in [-7.45, 7.45]:
        for z in [-3.5, 3.6]:
            _cube(self, "WallSconceBack", Vector3(x, 3.25, z), Vector3(0.25, 0.8, 0.51), brass)
            var sign := OmniLight3D.new()
            sign.position = Vector3(x + (0.7 if x < 0 else -0.7), 3.33, z)
            sign.omni_range = 4.6
            sign.light_energy = 0.6
            sign.light_color = Color("#f2bc89")
            add_child(sign)


func _build_doors() -> void:
    var carved := _mat(Color("#ad9487"), 0.06, 0.58, preload("res://art/wood.svg"))
    var frame := _mat(Color("#e2c19b"), 0.72, 0.29, preload("res://art/brass.svg"))
    var jamb := _mat(Color("#51414b"), 0.12, 0.75, preload("res://art/wood.svg"))
    var velvet := _mat(Color("#965b66"), 0.0, 0.87, preload("res://art/fabric.svg"))

    for i in range(3):
        var at: Vector3 = DOOR_POSITIONS[i]
        var portal := Node3D.new()
        portal.name = "Portal_%d" % i
        portal.position = at
        add_child(portal)
        _cube(portal, "DarkVoid", Vector3(0, 2.24, -0.17), Vector3(3.25, 4.65, 0.24), jamb)
        var hinge := Node3D.new()
        hinge.name = "Hinge_%d" % i
        hinge.position = Vector3(-1.38, 0, 0.0)
        portal.add_child(hinge)
        _cube(hinge, "CarvedDoor", Vector3(1.39, 2.12, 0.03), Vector3(2.76, 4.25, 0.17), carved)
        for dy in [1.1, 2.55, 3.65]:
            _cube(hinge, "InlaidDoorPanel", Vector3(1.39, dy, 0.15), Vector3(2.26, 0.68, 0.12), velvet)
            for dx in [0.33, 2.45]:
                _cube(hinge, "PanelEdge", Vector3(dx, dy, 0.22), Vector3(0.055, 0.78, 0.055), frame)
        _sphere(hinge, "BrassKnob", Vector3(2.33, 2.0, 0.31), 0.11, frame)

        for dx in [-1.6, 1.6]:
            _cube(portal, "GildedJamb", Vector3(dx, 2.36, 0.08), Vector3(0.24, 4.75, 0.42), frame)
        _cube(portal, "DoorHeader", Vector3(0, 4.68, 0.07), Vector3(3.4, 0.23, 0.42), frame)
        _cube(portal, "CarvedTop", Vector3(0, 5.05, 0.13), Vector3(3.03, 0.52, 0.35), carved)
        for dx in [-1.25, 1.25]:
            _cube(portal, "HeaderFinial", Vector3(dx, 5.42, 0.1), Vector3(0.18, 0.43, 0.32), frame)

        # Use a full-size picking shape, but do not expose any C# command here.
        var hit := StaticBody3D.new()
        hit.name = "DoorHit_%d" % i
        hit.position = Vector3(0, 2.2, 0.47)
        hit.collision_layer = 1
        hit.collision_mask = 0
        hit.set_meta("slot", i)
        var shape := BoxShape3D.new()
        shape.size = Vector3(2.85, 4.25, 0.2)
        var collider := CollisionShape3D.new()
        collider.shape = shape
        hit.add_child(collider)
        portal.add_child(hit)
        # Legible illuminated destination plaques are mounted ON the hinged door.
        # These move with the door and sit in front of the carved raised panels.
        _cube(hinge, "ReadableDoorPlaque", Vector3(1.39, 3.03, 0.36),
            Vector3(2.47, 0.69, 0.09), _mat(Color("#241d27"), 0.06, 0.72))
        _cube(hinge, "PlaqueBrassEdge", Vector3(1.39, 3.03, 0.31),
            Vector3(2.55, 0.77, 0.07), frame)
        var l := _label(hinge, "", Vector3(1.39, 3.02, 0.43), 52, Color("#fff2d5"))
        l.pixel_size = 0.010
        l.outline_size = 10
        l.no_depth_test = true
        doors.append(portal)
        door_panels.append(hinge)
        door_colliders.append(hit)
        door_names.append(l)
        choice_ids.append(-1)
    _set_options([
        {"id": 1, "name": "Library"},
        {"id": 7, "name": "Ballroom"},
        {"id": 4, "name": "Candle Landing"},
    ])


func _build_decoration() -> void:
    var wood := _mat(Color("#736264"), 0.06, 0.67, preload("res://art/wood.svg"))
    var brass := _mat(Color("#d2b68c"), 0.73, 0.3, preload("res://art/brass.svg"))
    var marble := _mat(Color("#bcb6bd"), 0.09, 0.48, preload("res://art/marble.svg"))
    for x in [-6.5, 6.5]:
        _cube(self, "ConsoleTable", Vector3(x, 0.95, -0.3), Vector3(1.65, 0.16, 1.5), wood)
        for dx in [-0.62, 0.62]:
            for dz in [-0.55, 0.55]:
                _cylinder(self, "TableLeg", Vector3(x+dx, 0.45, -0.3+dz), 0.07, 0.88, brass)
        _cube(self, "VaseBase", Vector3(x, 1.19, -0.3), Vector3(0.45, 0.32, 0.45), marble)
        _sphere(self, "Vase", Vector3(x, 1.58, -0.3), 0.32, marble)
    _cube(self, "ManorCoatOfArms", Vector3(0, 4.4, -11.12), Vector3(2.8, 1.0, 0.12), brass)
    _cube(self, "ReliefPlaque", Vector3(0, 4.42, -11.02), Vector3(2.42, 0.72, 0.06), wood)


func _build_labels() -> void:
    room_name = _label(self, "ENTRANCE HALL", Vector3(0, 3.3, -10.95), 43, Color("#eed7b1"))
    subtitle = _label(self, "MIDNIGHT MANOR · THE HOUSE IS AWAKE", Vector3(0, 2.78, -10.94), 23, Color("#bfa0b1"))
    # The door instructions belong to React for responsive, readable UI.


func _set_room(name: String, room_id: int = 0) -> void:
    var bounded_id := clampi(room_id, 0, 16)
    if _room_id != bounded_id or _room_visuals == null:
        if _room_visuals != null:
            remove_child(_room_visuals)
            _room_visuals.queue_free()
        _room_visuals = _room_builder.build(bounded_id)
        add_child(_room_visuals)
        _room_id = bounded_id
        if bounded_id in [11, 12, 14]:
            _lighting.light_color = Color("#a4adcb")
            _lighting.light_energy = 0.55
        elif bounded_id in [9, 13, 15, 16]:
            _lighting.light_color = Color("#b1d3ed")
            _lighting.light_energy = 0.7
        else:
            _lighting.light_color = WARM
            _lighting.light_energy = 1.3
    if room_name != null:
        room_name.text = name.to_upper().substr(0, 36)
    if subtitle != null:
        subtitle.text = "ESCAPE THE MANOR"


func _set_options(options: Array) -> void:
    for i in range(3):
        var valid := i < options.size() and options[i] is Dictionary
        var choice: Dictionary = options[i] if valid else {}
        var room_id := int(choice.get("id", -1))
        if room_id < 0 or room_id > 16:
            room_id = -1
        choice_ids[i] = room_id
        var title := str(choice.get("name", "SEALED")) if room_id >= 0 else "SEALED"
        var display := title.to_upper()
        if display.length() > 17 and display.contains(" "):
            var words := display.split(" ")
            var midpoint := int(ceil(float(words.size()) * 0.5))
            var first := " ".join(words.slice(0, midpoint))
            var second := " ".join(words.slice(midpoint))
            display = first + "\n" + second
        door_names[i].text = display.substr(0, 34)
        door_colliders[i].collision_layer = 1 if room_id >= 0 else 0
        door_panels[i].rotation_degrees.y = 0.0
    _selected = false


func choose_slot(slot: int) -> void:
    if not _active or slot < 0 or slot >= choice_ids.size() or choice_ids[slot] < 0 or _selected:
        return
    _selected = true
    if _door_tween != null and _door_tween.is_running():
        _door_tween.kill()
    _door_tween = create_tween()
    _door_tween.set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_IN_OUT)
    var duration := 0.12 if _reduced_motion else 0.65
    _door_tween.tween_property(door_panels[slot], "rotation_degrees:y", -72.0, duration)
    _door_tween.parallel().tween_property(camera, "position:z", 5.5, duration)
    _door_tween.tween_callback(_send_choice.bind(choice_ids[slot]))


func _send_choice(id: int) -> void:
    # Only a proposed destination ID goes to the React/C# bridge. Rewards,
    # balances and ghost movements always stay in the native engine.
    _post({"source": "manor-godot", "type": "choice", "room": id})


func _unhandled_input(event: InputEvent) -> void:
    if not event is InputEventMouseButton or not event.pressed or event.button_index != MOUSE_BUTTON_LEFT:
        return
    var mouse: Vector2 = event.position
    var origin := camera.project_ray_origin(mouse)
    var direction := camera.project_ray_normal(mouse)
    var query := PhysicsRayQueryParameters3D.create(origin, origin + direction * 40.0, 1)
    var hit: Dictionary = get_world_3d().direct_space_state.intersect_ray(query)
    var collider: Object = hit.get("collider")
    if collider is StaticBody3D:
        choose_slot(int((collider as StaticBody3D).get_meta("slot", -1)))


func _initialize_web_bridge() -> void:
    if _initialized_web:
        return
    _initialized_web = true
    JavaScriptBridge.eval("""
      window.__manorBonusQueue = [];
      window.addEventListener('message', function(event) {
        if (event.source !== window.parent || event.origin !== window.location.origin) return;
        const message = event.data;
        if (message && message.source === 'manor-react' &&
            (message.type === 'state' || message.type === 'pick') &&
            window.__manorBonusQueue.length < 16) {
          window.__manorBonusQueue.push(JSON.stringify(message));
        }
      });
    """)
    _post({"source": "manor-godot", "type": "ready"})


func _process(delta: float) -> void:
    # Barely perceptible resting breath, not walking/head-bobbing.
    # Independent of the choice tween's forward Z motion.
    if _active and not _reduced_motion and not _selected:
        _breath_clock += delta
        var breath := sin(_breath_clock * 1.65)
        camera.position.y = CAMERA_HOME.y + breath * 0.018
        camera.rotation.x = breath * 0.0015
    else:
        camera.position.y = lerpf(camera.position.y, CAMERA_HOME.y, minf(1.0, delta * 8.0))
        camera.rotation.x = lerpf(camera.rotation.x, 0.0, minf(1.0, delta * 8.0))

    if not OS.has_feature("web") or not _initialized_web:
        return
    _web_poll += delta
    if _web_poll < 0.12:
        return
    _web_poll = 0.0
    var value: Variant = JavaScriptBridge.eval("window.__manorBonusQueue.shift() || ''")
    if not value is String or value == "":
        return
    var data: Variant = JSON.parse_string(value)
    if not data is Dictionary:
        return
    if data.get("source") != "manor-react":
        return
    if data.get("type") == "pick":
        if _active:
            var slot: int = int(data.get("slot", -1))
            choose_slot(slot)
    elif data.get("type") == "state":
        _apply_web_state(data)


func _apply_web_state(data: Dictionary) -> void:
    if data.get("source") != "manor-react" or data.get("type") != "state":
        return
    var previous_active := _active
    _active = data.get("active", true) == true
    _reduced_motion = data.get("reducedMotion", false) == true
    if OS.has_feature("web"):
        # The engine is kept warm and cached between visits, but idles at eight
        # frames per second while the React slot is visible instead.
        Engine.max_fps = 60 if _active else 8
    if not _active:
        return
    demo_mode = data.get("demo", true) == true
    var next_room: String = str(data.get("roomName", "Entrance Hall"))
    var next_id: int = clampi(int(data.get("roomId", 0)), 0, 16)
    var room_changed := next_id != _room_id
    if room_changed or not previous_active:
        if _door_tween != null and _door_tween.is_running():
            _door_tween.kill()
        _set_room(next_room, next_id)
        camera.position = CAMERA_HOME
        camera.rotation = Vector3.ZERO

    var options: Variant = data.get("options", [])
    if options is Array:
        var signature := JSON.stringify(options)
        if signature != _options_signature or room_changed or not previous_active:
            _options_signature = signature
            _set_options(options)


func _post(data: Dictionary) -> void:
    if not OS.has_feature("web"):
        return
    JavaScriptBridge.eval("window.parent.postMessage(" + JSON.stringify(data) + ", window.location.origin)")
