extends Node3D

# Self-contained 3D sandbox: no wallet, save files, progression or casino RPCs.
# All geometry is assembled from native Godot primitives at runtime.

@onready var player: CharacterBody3D = $Player

var door: AnimatableBody3D
var console: StaticBody3D
var lamp_switch: StaticBody3D
var lamp_light: OmniLight3D
var lamp_bulb_material: StandardMaterial3D
var door_open := false
var lamp_on := true

var _lang_de := true
var _prompt: Label
var _hint: Label
var _title: Label
var _subtitle: Label
var _notice: Label
var _notice_timer := 0.0
var _orb: MeshInstance3D
var _current_focus: Object = null
var _door_tween: Tween


func _ready() -> void:
    _lang_de = not OS.get_cmdline_user_args().has("--language=en")
    _build_environment()
    _build_architecture()
    _build_details()
    _build_door_and_console()
    _build_lamp()
    _build_hud()
    player.interact_requested.connect(_interact)
    player.focus_changed.connect(_focus_changed)
    _refresh_language()


func _unhandled_input(event: InputEvent) -> void:
    if event.is_action_pressed("fp_language"):
        _lang_de = not _lang_de
        _refresh_language()
        get_viewport().set_input_as_handled()


func _process(delta: float) -> void:
    if _orb != null:
        _orb.rotate_y(delta * 0.6)
    if _notice_timer > 0.0:
        _notice_timer -= delta
        if _notice_timer <= 0.0:
            _notice.text = ""


func _tr(de: String, en: String) -> String:
    return de if _lang_de else en


func _material(tint: Color, metal: float = 0.0, rough: float = 0.82, glow: Color = Color.BLACK, power: float = 0.0) -> StandardMaterial3D:
    var result := StandardMaterial3D.new()
    result.albedo_color = tint
    result.metallic = metal
    result.roughness = rough
    if power > 0.0:
        result.emission_enabled = true
        result.emission = glow
        result.emission_energy_multiplier = power
    return result


func _box(parent: Node3D, item_name: String, center: Vector3, dimensions: Vector3, material: Material, solid: bool = false) -> Node3D:
    var holder: Node3D
    if solid:
        var body := StaticBody3D.new()
        body.collision_layer = 1
        body.collision_mask = 0
        holder = body
    else:
        holder = Node3D.new()
    holder.name = item_name
    holder.position = center
    parent.add_child(holder)

    var mesh := BoxMesh.new()
    mesh.size = dimensions
    var visible_mesh := MeshInstance3D.new()
    visible_mesh.mesh = mesh
    visible_mesh.material_override = material
    holder.add_child(visible_mesh)
    if solid:
        var shape := BoxShape3D.new()
        shape.size = dimensions
        var collider := CollisionShape3D.new()
        collider.shape = shape
        holder.add_child(collider)
    return holder


func _light(at: Vector3, color: Color, energy: float, radius: float, shadow: bool = false) -> OmniLight3D:
    var light := OmniLight3D.new()
    light.position = at
    light.light_color = color
    light.light_energy = energy
    light.omni_range = radius
    light.shadow_enabled = shadow
    add_child(light)
    return light


func _build_environment() -> void:
    var world := WorldEnvironment.new()
    world.name = "Environment"
    var env := Environment.new()
    env.background_mode = Environment.BG_COLOR
    env.background_color = Color("#080e16")
    env.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    env.ambient_light_color = Color("#7b94ac")
    env.ambient_light_energy = 0.36
    world.environment = env
    add_child(world)


func _build_architecture() -> void:
    var concrete := _material(Color("#263542"), 0.04, 0.94)
    var floor_mat := _material(Color("#192832"), 0.06, 0.76)
    var steel := _material(Color("#4a626d"), 0.65, 0.32)
    var dark := _material(Color("#101d28"), 0.3, 0.6)
    var cyan := _material(Color("#43e5de"), 0.1, 0.44, Color("#36e0d3"), 1.5)
    var amber := _material(Color("#f3b875"), 0.12, 0.48, Color("#e3a15a"), 1.3)

    _box(self, "MainFloor", Vector3(0, -0.3, 0), Vector3(18, 0.6, 18), floor_mat, true)
    _box(self, "AlcoveFloor", Vector3(0, -0.3, -11), Vector3(6, 0.6, 4.2), floor_mat, true)
    _box(self, "Ceiling", Vector3(0, 4.6, 0), Vector3(18, 0.25, 18), concrete, true)
    _box(self, "AlcoveCeiling", Vector3(0, 4.6, -11), Vector3(6, 0.25, 4.2), concrete, true)

    _box(self, "SouthWall", Vector3(0, 2.25, 9), Vector3(18, 4.5, 0.35), concrete, true)
    _box(self, "WestWall", Vector3(-9, 2.25, 0), Vector3(0.35, 4.5, 18), concrete, true)
    _box(self, "EastWall", Vector3(9, 2.25, 0), Vector3(0.35, 4.5, 18), concrete, true)
    _box(self, "NorthWallLeft", Vector3(-5.15, 2.25, -9), Vector3(7.7, 4.5, 0.35), concrete, true)
    _box(self, "NorthWallRight", Vector3(5.15, 2.25, -9), Vector3(7.7, 4.5, 0.35), concrete, true)
    _box(self, "DoorLintel", Vector3(0, 3.9, -9), Vector3(2.65, 1.2, 0.4), concrete, true)

    _box(self, "AlcoveWest", Vector3(-3, 2.25, -11), Vector3(0.3, 4.5, 4.2), concrete, true)
    _box(self, "AlcoveEast", Vector3(3, 2.25, -11), Vector3(0.3, 4.5, 4.2), concrete, true)
    _box(self, "AlcoveBack", Vector3(0, 2.25, -13), Vector3(6, 4.5, 0.35), concrete, true)

    # Walkway guides, columns, wall relief and a recognizable door frame.
    for z in [-6.5, -3.5, -0.5, 2.5, 5.5]:
        _box(self, "FloorGuideL", Vector3(-1.7, 0.014, z), Vector3(0.055, 0.025, 2.5), cyan)
        _box(self, "FloorGuideR", Vector3(1.7, 0.014, z), Vector3(0.055, 0.025, 2.5), cyan)
    for x in [-7.2, 7.2]:
        for z in [-6.4, 0.0, 6.4]:
            _box(self, "WallRib", Vector3(x, 2.2, z), Vector3(0.42, 4.35, 0.56), steel, true)
            _box(self, "WallRibGlow", Vector3(x + (0.23 if x < 0 else -0.23), 2.05, z), Vector3(0.025, 2.7, 0.08), cyan)
    for x in [-1.43, 1.43]:
        _box(self, "DoorFrameSide", Vector3(x, 1.58, -8.7), Vector3(0.18, 3.15, 0.4), steel)
        _box(self, "DoorFrameLight", Vector3(x + (-0.11 if x < 0 else 0.11), 1.58, -8.46), Vector3(0.05, 2.85, 0.05), amber)
    _box(self, "DoorFrameTop", Vector3(0, 3.18, -8.7), Vector3(3.1, 0.18, 0.4), steel)

    # Four luminous ceiling coffers, each with an actual light.
    for x in [-5.0, 5.0]:
        for z in [-5.0, 5.0]:
            _box(self, "CeilingFixture", Vector3(x, 4.39, z), Vector3(2.2, 0.08, 0.65), dark)
            _box(self, "CeilingDiffuser", Vector3(x, 4.32, z), Vector3(1.8, 0.035, 0.32), amber)
            _light(Vector3(x, 4.13, z), Color("#f7d9b2"), 1.5, 8.0, true)
    _box(self, "AlcoveCeilingDiffuser", Vector3(0, 4.31, -11.1), Vector3(2.0, 0.05, 0.4), cyan)
    _light(Vector3(0, 3.9, -11.1), Color("#77d8de"), 1.4, 5.5)


func _build_details() -> void:
    var steel := _material(Color("#4e616a"), 0.7, 0.32)
    var dark := _material(Color("#10202d"), 0.34, 0.54)
    var gold := _material(Color("#c9a77b"), 0.55, 0.39)
    var blue := _material(Color("#3ccbc9"), 0.25, 0.27, Color("#42dcd7"), 1.7)

    _box(self, "CentralPlatform", Vector3(0, 0.13, -1), Vector3(3.1, 0.26, 3.1), dark, true)
    _box(self, "PlatformInlay", Vector3(0, 0.275, -1), Vector3(2.8, 0.02, 2.8), steel)
    _box(self, "DisplayPlinth", Vector3(0, 0.8, -1), Vector3(0.9, 1.0, 0.9), gold, true)
    var orb_mesh := SphereMesh.new()
    orb_mesh.radius = 0.39
    orb_mesh.height = 0.78
    _orb = MeshInstance3D.new()
    _orb.name = "DisplayCore"
    _orb.position = Vector3(0, 1.72, -1)
    _orb.mesh = orb_mesh
    _orb.material_override = blue
    add_child(_orb)
    _light(Vector3(0, 1.65, -1), Color("#37d6ce"), 0.65, 3.1)

    # Wall consoles and restrained industrial detail.
    for x in [-7.0, 7.0]:
        _box(self, "WallConsole", Vector3(x, 1.75, -3.7), Vector3(0.55, 1.7, 1.8), dark, true)
        _box(self, "WallConsoleTop", Vector3(x + (-0.29 if x < 0 else 0.29), 2.15, -3.7), Vector3(0.04, 0.6, 1.3), blue)

    _box(self, "LampDesk", Vector3(4.5, 0.86, 1.5), Vector3(2.0, 0.17, 1.25), steel, true)
    for x in [3.65, 5.35]:
        for z in [1.0, 2.0]:
            _box(self, "DeskLeg", Vector3(x, 0.41, z), Vector3(0.13, 0.82, 0.13), dark, true)

    var panel := Label3D.new()
    panel.name = "SectorSign"
    panel.text = "SECTOR 01  /  LAB"
    panel.font_size = 52
    panel.pixel_size = 0.0036
    panel.modulate = Color("#bfe7e3")
    panel.position = Vector3(-5.3, 3.0, -8.79)
    add_child(panel)
    var back_sign := Label3D.new()
    back_sign.text = "OBSERVATION"
    back_sign.font_size = 46
    back_sign.pixel_size = 0.004
    back_sign.position = Vector3(0, 2.35, -12.77)
    back_sign.modulate = Color("#c9d5d9")
    add_child(back_sign)


func _build_door_and_console() -> void:
    door = AnimatableBody3D.new()
    door.name = "Door"
    door.position = Vector3(0, 1.56, -8.98)
    door.collision_layer = 1
    door.collision_mask = 0
    add_child(door)
    _box(door, "SlidingDoor", Vector3.ZERO, Vector3(2.5, 3.12, 0.22), _material(Color("#364b5a"), 0.67, 0.37))
    _box(door, "DoorStripe", Vector3(0, 0.18, 0.122), Vector3(1.95, 0.085, 0.03), _material(Color("#e0a566"), 0.16, 0.4, Color("#f8b66a"), 1.1))
    _box(door, "DoorCenterLine", Vector3(0, 0, 0.124), Vector3(0.045, 2.5, 0.03), _material(Color("#192732"), 0.35))
    var shape := BoxShape3D.new()
    shape.size = Vector3(2.5, 3.12, 0.22)
    var collision := CollisionShape3D.new()
    collision.shape = shape
    door.add_child(collision)

    console = StaticBody3D.new()
    console.name = "DoorConsole"
    console.position = Vector3(-2.2, 1.35, -8.64)
    console.collision_layer = 2
    console.collision_mask = 0
    console.add_to_group("interactable")
    console.set_meta("interaction", "door")
    add_child(console)
    _box(console, "Housing", Vector3.ZERO, Vector3(0.72, 0.85, 0.22), _material(Color("#0f222d"), 0.4, 0.4))
    _box(console, "TouchPanel", Vector3(0, 0, 0.132), Vector3(0.52, 0.54, 0.025), _material(Color("#2ac8be"), 0.1, 0.25, Color("#43d9d2"), 1.4))
    var console_shape := BoxShape3D.new()
    console_shape.size = Vector3(0.72, 0.85, 0.22)
    var console_collision := CollisionShape3D.new()
    console_collision.shape = console_shape
    console.add_child(console_collision)


func _build_lamp() -> void:
    lamp_switch = StaticBody3D.new()
    lamp_switch.name = "LampSwitch"
    lamp_switch.position = Vector3(4.5, 1.35, 1.5)
    lamp_switch.collision_layer = 2
    lamp_switch.collision_mask = 0
    lamp_switch.add_to_group("interactable")
    lamp_switch.set_meta("interaction", "lamp")
    add_child(lamp_switch)
    _box(lamp_switch, "LampBase", Vector3(0, -0.25, 0), Vector3(0.6, 0.13, 0.55), _material(Color("#293d4b"), 0.65, 0.3))
    _box(lamp_switch, "LampStem", Vector3(0, 0.02, 0), Vector3(0.12, 0.52, 0.12), _material(Color("#9eabb2"), 0.75, 0.26))
    lamp_bulb_material = _material(Color("#fff0cf"), 0.03, 0.3, Color("#ffc982"), 2.0)
    var globe_mesh := SphereMesh.new()
    globe_mesh.radius = 0.28
    globe_mesh.height = 0.56
    var bulb := MeshInstance3D.new()
    bulb.name = "LampBulb"
    bulb.position = Vector3(0, 0.43, 0)
    bulb.mesh = globe_mesh
    bulb.material_override = lamp_bulb_material
    lamp_switch.add_child(bulb)
    var shape := SphereShape3D.new()
    shape.radius = 0.58
    var collision := CollisionShape3D.new()
    collision.position = Vector3(0, 0.17, 0)
    collision.shape = shape
    lamp_switch.add_child(collision)
    lamp_light = _light(Vector3(4.5, 1.8, 1.5), Color("#ffd49e"), 2.3, 5.5, true)
    lamp_light.name = "DeskLampLight"


func _hud_label(text: String, font_size: int, tint: Color) -> Label:
    var label := Label.new()
    label.text = text
    label.add_theme_font_size_override("font_size", font_size)
    label.add_theme_color_override("font_color", tint)
    label.add_theme_color_override("font_shadow_color", Color(0, 0, 0, 0.8))
    label.add_theme_constant_override("shadow_offset_x", 1)
    label.add_theme_constant_override("shadow_offset_y", 2)
    label.mouse_filter = Control.MOUSE_FILTER_IGNORE
    return label


func _build_hud() -> void:
    var canvas := CanvasLayer.new()
    canvas.name = "HUD"
    add_child(canvas)
    var overlay := Control.new()
    overlay.name = "Overlay"
    overlay.mouse_filter = Control.MOUSE_FILTER_IGNORE
    canvas.add_child(overlay)
    overlay.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)

    _title = _hud_label("", 25, Color("#eaf6f5"))
    _title.position = Vector2(30, 24)
    overlay.add_child(_title)

    _subtitle = _hud_label("", 15, Color("#9ab8c2"))
    _subtitle.position = Vector2(31, 59)
    overlay.add_child(_subtitle)

    _hint = _hud_label("", 16, Color("#c1d2d6"))
    _hint.anchor_top = 1.0
    _hint.anchor_bottom = 1.0
    _hint.offset_left = 30
    _hint.offset_right = 1070
    _hint.offset_top = -48
    _hint.offset_bottom = -12
    overlay.add_child(_hint)

    _prompt = _hud_label("", 21, Color("#fff1cf"))
    _prompt.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    _prompt.anchor_left = 0.5
    _prompt.anchor_right = 0.5
    _prompt.anchor_top = 0.73
    _prompt.anchor_bottom = 0.73
    _prompt.offset_left = -310
    _prompt.offset_right = 310
    _prompt.offset_top = 0
    _prompt.offset_bottom = 42
    overlay.add_child(_prompt)

    _notice = _hud_label("", 19, Color("#9bf0e6"))
    _notice.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    _notice.anchor_left = 0.5
    _notice.anchor_right = 0.5
    _notice.offset_left = -310
    _notice.offset_right = 310
    _notice.offset_top = 100
    _notice.offset_bottom = 142
    overlay.add_child(_notice)

    var cross_vertical := ColorRect.new()
    cross_vertical.color = Color(0.92, 1, 0.99, 0.88)
    cross_vertical.mouse_filter = Control.MOUSE_FILTER_IGNORE
    cross_vertical.anchor_left = 0.5
    cross_vertical.anchor_right = 0.5
    cross_vertical.anchor_top = 0.5
    cross_vertical.anchor_bottom = 0.5
    cross_vertical.offset_left = -1
    cross_vertical.offset_right = 1
    cross_vertical.offset_top = -8
    cross_vertical.offset_bottom = 8
    overlay.add_child(cross_vertical)
    var cross_horizontal := ColorRect.new()
    cross_horizontal.color = cross_vertical.color
    cross_horizontal.mouse_filter = Control.MOUSE_FILTER_IGNORE
    cross_horizontal.anchor_left = 0.5
    cross_horizontal.anchor_right = 0.5
    cross_horizontal.anchor_top = 0.5
    cross_horizontal.anchor_bottom = 0.5
    cross_horizontal.offset_left = -8
    cross_horizontal.offset_right = 8
    cross_horizontal.offset_top = -1
    cross_horizontal.offset_bottom = 1
    overlay.add_child(cross_horizontal)


func _refresh_language() -> void:
    _title.text = _tr("RAUMTEST / EGOPERSPEKTIVE", "ROOM TEST / FIRST PERSON")
    _subtitle.text = _tr("MIDNIGHT CLUB  ·  3D-PROTOTYP", "MIDNIGHT CLUB  ·  3D PROTOTYPE")
    _hint.text = _tr("WASD Laufen   ·   Maus Umsehen   ·   Shift Rennen   ·   Leertaste Springen   ·   E Benutzen   ·   F2 Sprache   ·   Esc Maus frei / Beenden", "WASD Move   ·   Mouse Look   ·   Shift Sprint   ·   Space Jump   ·   E Interact   ·   F2 Language   ·   Esc Release / Exit")
    _focus_changed(_current_focus)


func _focus_changed(target: Object) -> void:
    _current_focus = target
    if target == console:
        _prompt.text = _tr("[E]  Schiebetür schließen" if door_open else "[E]  Schiebetür öffnen", "[E]  Close sliding door" if door_open else "[E]  Open sliding door")
    elif target == lamp_switch:
        _prompt.text = _tr("[E]  Lampe ausschalten" if lamp_on else "[E]  Lampe einschalten", "[E]  Turn lamp off" if lamp_on else "[E]  Turn lamp on")
    else:
        _prompt.text = ""


func _interact(target: Object) -> void:
    if target == console:
        toggle_door()
    elif target == lamp_switch:
        toggle_lamp()


func toggle_door() -> void:
    door_open = not door_open
    if _door_tween != null and _door_tween.is_running():
        _door_tween.kill()
    _door_tween = create_tween()
    _door_tween.set_trans(Tween.TRANS_SINE)
    _door_tween.set_ease(Tween.EASE_IN_OUT)
    _door_tween.tween_property(door, "position:x", 2.75 if door_open else 0.0, 1.1)
    _show_notice(_tr("SCHLEUSE GEÖFFNET" if door_open else "SCHLEUSE GESCHLOSSEN", "AIRLOCK OPEN" if door_open else "AIRLOCK CLOSED"))
    _focus_changed(_current_focus)


func toggle_lamp() -> void:
    lamp_on = not lamp_on
    lamp_light.visible = lamp_on
    lamp_bulb_material.emission_enabled = lamp_on
    _show_notice(_tr("TISCHLAMPE AN" if lamp_on else "TISCHLAMPE AUS", "DESK LAMP ON" if lamp_on else "DESK LAMP OFF"))
    _focus_changed(_current_focus)


func _show_notice(message: String) -> void:
    _notice.text = message
    _notice_timer = 2.0
