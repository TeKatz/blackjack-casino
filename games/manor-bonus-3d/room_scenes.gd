extends RefCounted

# Procedural, deterministic room set for the Manor WebGL vertical slice.
# All furnishing nodes live under RoomVisuals and are replaced between rooms;
# the three interaction portals belong to the main scene and stay stable.
const TEXTURE_WOOD: Texture2D = preload("res://art/wood.svg")
const TEXTURE_WALLPAPER: Texture2D = preload("res://art/wallpaper.svg")
const TEXTURE_MARBLE: Texture2D = preload("res://art/marble.svg")
const TEXTURE_BRASS: Texture2D = preload("res://art/brass.svg")
const TEXTURE_FABRIC: Texture2D = preload("res://art/fabric.svg")
const TEXTURE_GLASS: Texture2D = preload("res://art/glass.svg")
const TEXTURE_STONE: Texture2D = preload("res://art/stone.svg")
const TEXTURE_GREEN: Texture2D = preload("res://art/foliage.svg")
const TEXTURE_BOOKS: Texture2D = preload("res://art/books.svg")

var m: Dictionary = {}


func _init() -> void:
    m = {
        "wood": _material(Color("#c2a69d"), TEXTURE_WOOD, 0.10, 0.65),
        "darkwood": _material(Color("#634749"), TEXTURE_WOOD, 0.22, 0.64),
        "wallpaper": _material(Color("#aaa1ad"), TEXTURE_WALLPAPER, 0.04, 0.9),
        "redwall": _material(Color("#b17b85"), TEXTURE_WALLPAPER, 0.03, 0.84),
        "bluewall": _material(Color("#718baf"), TEXTURE_WALLPAPER, 0.04, 0.79),
        "plaster": _material(Color("#d0c6ba"), TEXTURE_MARBLE, 0.02, 0.91),
        "marble": _material(Color("#cec9d1"), TEXTURE_MARBLE, 0.1, 0.43),
        "darkmarble": _material(Color("#797a8d"), TEXTURE_MARBLE, 0.15, 0.48),
        "brass": _material(Color("#d1b384"), TEXTURE_BRASS, 0.74, 0.3),
        "velvet": _material(Color("#ab6571"), TEXTURE_FABRIC, 0.02, 0.88),
        "bluevelvet": _material(Color("#788baa"), TEXTURE_FABRIC, 0.02, 0.91),
        "stone": _material(Color("#b2afb4"), TEXTURE_STONE, 0.02, 0.89),
        "cryptstone": _material(Color("#636b7b"), TEXTURE_STONE, 0.05, 0.94),
        "tile": _material(Color("#c9cad4"), TEXTURE_MARBLE, 0.15, 0.38),
        "glass": _material(Color("#7aacc1"), TEXTURE_GLASS, 0.12, 0.25),
        "plant": _material(Color("#759e7e"), TEXTURE_GREEN, 0.0, 0.82),
        "leaves": _material(Color("#4e8369"), TEXTURE_GREEN, 0.0, 0.9),
        "book": _material(Color("#af7e70"), TEXTURE_BOOKS, 0.05, 0.83),
        "paper": _material(Color("#efdfb6"), TEXTURE_MARBLE, 0.0, 0.93),
        "black": _material(Color("#221c24"), TEXTURE_WOOD, 0.08, 0.8),
        "flame": _material(Color("#f9ddba"), null, 0.0, 0.4, Color("#ffb577"), 2.0),
    }


func _material(color: Color, texture: Texture2D = null, metal: float = 0.0,
        rough: float = 0.75, glow: Color = Color.BLACK, power: float = 0.0) -> StandardMaterial3D:
    var mat := StandardMaterial3D.new()
    mat.albedo_color = color
    mat.metallic = metal
    mat.roughness = rough
    if texture != null:
        mat.albedo_texture = texture
        mat.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
        mat.uv1_triplanar = true
        mat.uv1_world_triplanar = true
        mat.uv1_scale = Vector3(0.62, 0.62, 0.62)
    if power > 0.0:
        mat.emission_enabled = true
        mat.emission = glow
        mat.emission_energy_multiplier = power
    return mat


func _box(p: Node3D, name: String, pos: Vector3, size: Vector3, mat: Material) -> MeshInstance3D:
    var box := BoxMesh.new()
    box.size = size
    var node := MeshInstance3D.new()
    node.name = name
    node.mesh = box
    node.position = pos
    node.material_override = mat
    p.add_child(node)
    return node


func _cyl(p: Node3D, name: String, pos: Vector3, radius: float, height: float,
        mat: Material, segments: int = 12) -> MeshInstance3D:
    var cylinder := CylinderMesh.new()
    cylinder.top_radius = radius
    cylinder.bottom_radius = radius
    cylinder.height = height
    cylinder.radial_segments = segments
    var node := MeshInstance3D.new()
    node.name = name
    node.mesh = cylinder
    node.position = pos
    node.material_override = mat
    p.add_child(node)
    return node


func _ball(p: Node3D, name: String, pos: Vector3, radius: float, mat: Material) -> MeshInstance3D:
    var sphere := SphereMesh.new()
    sphere.radius = radius
    sphere.height = radius * 2.0
    sphere.radial_segments = 10
    sphere.rings = 6
    var node := MeshInstance3D.new()
    node.name = name
    node.mesh = sphere
    node.position = pos
    node.material_override = mat
    p.add_child(node)
    return node


func _light(p: Node3D, name: String, pos: Vector3, color: Color, energy: float,
        distance: float) -> void:
    var lamp := OmniLight3D.new()
    lamp.name = name
    lamp.position = pos
    lamp.light_color = color
    lamp.light_energy = energy
    lamp.omni_range = distance
    lamp.shadow_enabled = false
    p.add_child(lamp)


func _theme(room_id: int) -> String:
    match room_id:
        0, 3:
            return "entrance"
        1, 2:
            return "corridor"
        4, 5, 6:
            return "library"
        7, 8:
            return "ballroom"
        9:
            return "garden"
        10, 13:
            return "attic"
        11, 12, 14:
            return "crypt"
        15, 16:
            return "outdoor"
    return "entrance"


func build(room_id: int) -> Node3D:
    var visual := Node3D.new()
    visual.name = "RoomVisuals"
    var family := _theme(room_id)
    _shell(visual, family, room_id)
    match room_id:
        0:
            _entrance(visual)
        1:
            _servants(visual)
        2:
            _landing(visual)
        3:
            _stairs(visual)
        4:
            _library(visual)
        5:
            _reading(visual)
        6:
            _study(visual)
        7:
            _ballroom(visual)
        8:
            _music(visual)
        9:
            _garden(visual)
        10:
            _attic(visual)
        11:
            _forbidden(visual)
        12:
            _crypt(visual)
        13:
            _moonlit_landing(visual)
        14:
            _secret(visual)
        15:
            _courtyard(visual)
        16:
            _moonlit_gate(visual)
    return visual


func _shell(p: Node3D, family: String, room_id: int) -> void:
    var floor_mat: Material = m["wood"]
    var wall_mat: Material = m["wallpaper"]
    var trim: Material = m["darkwood"]
    var height := 6.0
    var width := 18.8
    match family:
        "corridor":
            wall_mat = m["plaster"]
            floor_mat = m["darkwood"]
            width = 15.4
            height = 5.2
        "library":
            wall_mat = m["redwall"]
            floor_mat = m["darkwood"]
            height = 5.6
        "ballroom":
            wall_mat = m["bluewall"]
            floor_mat = m["tile"]
            height = 6.2
        "garden":
            wall_mat = m["glass"]
            floor_mat = m["tile"]
            height = 6.4
        "attic":
            wall_mat = m["plaster"]
            floor_mat = m["wood"]
            height = 4.9
        "crypt":
            wall_mat = m["cryptstone"]
            floor_mat = m["stone"]
            height = 5.0
            width = 16.4
            trim = m["stone"]
        "outdoor":
            wall_mat = m["stone"]
            floor_mat = m["tile"]
            height = 7.4
            trim = m["stone"]

    _box(p, "Floor", Vector3(0, -0.18, -1), Vector3(width, 0.36, 20.9), floor_mat)
    if family != "outdoor":
        _box(p, "Ceiling", Vector3(0, height, -1), Vector3(width, 0.24, 20.9), wall_mat)
    else:
        # The outdoor rooms have genuinely open skies; no fake dark ceiling.
        var panorama := _material(Color("#111e39"), TEXTURE_GLASS, 0.0, 0.95)
        _box(p, "MoonlitBackdrop", Vector3(0, 9.3, -1), Vector3(24.0, 0.14, 23.0), panorama)
    for side in [-1, 1]:
        var x := float(side) * (width * 0.5)
        _box(p, "SideWall", Vector3(x, (height - 0.2) * 0.5, -1),
            Vector3(0.35, height, 20.9), wall_mat)
        _box(p, "Dado", Vector3(x - float(side) * 0.20, 0.95, -1),
            Vector3(0.17, 1.85, 20.4), trim)
        _box(p, "ChairRail", Vector3(x - float(side) * 0.24, 1.95, -1),
            Vector3(0.1, 0.1, 20.4), m["brass"])
    _box(p, "RearWall", Vector3(0, (height - 0.2) * 0.5, -11.41),
        Vector3(width, height, 0.24), wall_mat)
    _box(p, "FrontWall", Vector3(0, (height - 0.2) * 0.5, 9.41),
        Vector3(width, height, 0.24), wall_mat)
    for j in range(4):
        var z := -8.7 + float(j) * 4.9
        if family != "outdoor":
            _box(p, "CrownMoulding", Vector3(0, height - 0.16, z),
                Vector3(width - 0.6, 0.16, 0.18), trim)
        if family == "crypt":
            _box(p, "HeavyVault", Vector3(0, height - 0.31, z),
                Vector3(width - 0.7, 0.54, 0.46), m["cryptstone"])

    if family in ["entrance", "library", "ballroom"]:
        _box(p, "Runner", Vector3(-0.38, 0.022, -0.45),
            Vector3(3.5, 0.027, 13.0), m["velvet"] if family != "ballroom" else m["bluevelvet"])
    if family == "corridor":
        _box(p, "NarrowRunner", Vector3(0.35, 0.02, 0.0),
            Vector3(2.4, 0.022, 13.5), m["velvet"])
    if family == "outdoor":
        for x in [-7.9, -4.6, 0.5, 6.1]:
            _box(p, "GardenPavingJoint", Vector3(x, 0.007, -0.9),
                Vector3(0.055, 0.017, 19.5), m["cryptstone"])
    # The room label, doors, and interactive collision shapes are independent
    # from the visual room rebuild and never receive gameplay authority.


func _pillar(p: Node3D, x: float, z: float, y: float = 2.8, stone: bool = false) -> void:
    var material: Material = m["stone"] if stone else m["marble"]
    _cyl(p, "ColumnShaft", Vector3(x, y, z), 0.31, y * 1.76, material)
    _cyl(p, "ColumnPlinth", Vector3(x, 0.24, z), 0.47, 0.47, m["darkwood"] if not stone else m["cryptstone"])
    _cyl(p, "Capital", Vector3(x, y * 1.87, z), 0.49, 0.29, m["brass"] if not stone else material)


func _window(p: Node3D, x: float, z: float) -> void:
    var side := signf(x)
    var glass_x := x - side * 0.27
    _box(p, "MoonGlass", Vector3(glass_x, 3.68, z), Vector3(0.065, 2.3, 1.85), m["glass"])
    for dz in [-0.86, 0.0, 0.86]:
        _box(p, "WindowTracery", Vector3(glass_x - side * 0.075, 3.68, z+dz),
            Vector3(0.13, 2.36, 0.064), m["brass"])
    for dy in [-0.95, 0.0, 0.95]:
        _box(p, "WindowLead", Vector3(glass_x - side * 0.084, 3.68+dy, z),
            Vector3(0.14, 0.055, 1.98), m["brass"])


func _sconce(p: Node3D, x: float, z: float, y: float = 3.15) -> void:
    # Wall-mounted arm: the base actually touches the wall. No floating
    # opaque orange rectangular blocks in front of the windows.
    var side := signf(x)
    var width := 9.4 if absf(x) > 8.0 else absf(x) + 0.1
    var wall_x := side * (width - 0.2)
    var tip_x := wall_x - side * 0.5
    _box(p, "SconceWallPlate", Vector3(wall_x, y, z),
        Vector3(0.1, 0.44, 0.26), m["darkwood"])
    _box(p, "SconceArm", Vector3((wall_x + tip_x) * 0.5, y - 0.13, z),
        Vector3(absf(tip_x-wall_x), 0.075, 0.09), m["brass"])
    _cyl(p, "CandleHolder", Vector3(tip_x, y + 0.15, z), 0.1, 0.23, m["brass"])
    _cyl(p, "WaxCandle", Vector3(tip_x, y + 0.43, z), 0.07, 0.36, m["paper"], 9)
    _ball(p, "CandleFlame", Vector3(tip_x, y + 0.69, z), 0.08, m["flame"])


func _table(p: Node3D, x: float, z: float, length: float = 2.3) -> void:
    _box(p, "WritingDesk", Vector3(x, 0.9, z), Vector3(length, 0.15, 1.28), m["wood"])
    for dx in [-length*0.40, length*0.40]:
        for dz in [-0.49, 0.49]:
            _box(p, "CarvedLeg", Vector3(x+dx, 0.43, z+dz),
                Vector3(0.14, 0.82, 0.14), m["darkwood"])


func _books(p: Node3D, x: float, z: float, index: int) -> void:
    _box(p, "BookcaseBack", Vector3(x, 2.47, z), Vector3(2.15, 4.4, 0.33), m["darkwood"])
    for shelf in range(4):
        var yy := 0.56 + shelf * 1.05
        _box(p, "Bookshelf", Vector3(x, yy, z + 0.27),
            Vector3(2.25, 0.11, 0.73), m["wood"])
        for b in range(7):
            var height := 0.51 + float((index+b+shelf)%4)*0.09
            var shade: Material = m["book"] if (b+shelf)%2 == 0 else m["velvet"]
            _box(p, "BookSpine", Vector3(x - 0.84 + float(b)*0.29,
                yy + height*0.5+0.075, z + 0.44),
                Vector3(0.19+0.03*float(b%2), height, 0.31), shade)


func _plant(p: Node3D, x: float, z: float, height: float) -> void:
    _cyl(p, "Planter", Vector3(x, 0.37, z), 0.34, 0.65, m["stone"])
    _cyl(p, "Trunk", Vector3(x, 0.72+height*0.35, z), 0.08, height*0.7, m["darkwood"], 8)
    for i in range(5):
        var a := float(i) * TAU/5.0
        _ball(p, "Foliage", Vector3(x+cos(a)*0.36, 0.92+height*0.70 + 0.12*float(i%2),
            z+sin(a)*0.36), 0.4, m["plant"] if i%2 == 0 else m["leaves"])


func _candles(p: Node3D, x: float, z: float, count: int) -> void:
    for i in range(count):
        var offset := float(i) * 0.28
        var candle_height := 0.32+float(i%3)*0.14
        _cyl(p, "Candle", Vector3(x+offset, 0.52+candle_height*0.5, z),
            0.055, candle_height, m["paper"], 8)
        _ball(p, "CandleGlow", Vector3(x+offset, 0.58+candle_height, z),
            0.055, m["flame"])


func _portrait(p: Node3D, side: float, z: float, crooked: float = 0.0) -> void:
    var x := side * 9.12
    var frame := _box(p, "GiltPortrait", Vector3(x, 3.68, z),
        Vector3(0.14, 2.05, 1.55), m["brass"])
    frame.rotation.x = crooked
    _box(p, "PortraitCanvas", Vector3(x-side*0.095, 3.68, z),
        Vector3(0.07, 1.75, 1.30), m["black"])
    _box(p, "PortraitSilhouette", Vector3(x-side*0.14, 3.7, z),
        Vector3(0.035, 1.03, 0.41), m["bluevelvet"])


func _entrance(p: Node3D) -> void:
    # Intentionally asymmetrical: a staggered grand staircase on the west,
    # one portrait on the east, an unequal group of sconces, and a luggage desk.
    for x in [-6.9, 6.9]:
        _pillar(p, x, -7.5)
    for s in range(8):
        _box(p, "WestGrandStair", Vector3(-6.15, 0.13+float(s)*0.23,
            4.9-float(s)*0.48), Vector3(2.55, 0.25, 0.49), m["marble"])
    _box(p, "WestStairRail", Vector3(-4.86, 1.34, 2.95),
        Vector3(0.10, 2.22, 4.5), m["brass"])
    _portrait(p, 1.0, -1.1, 0.08)
    _table(p, 6.2, 2.35, 2.8)
    _plant(p, 7.25, 0.7, 1.6)
    _sconce(p, -9.4, -4.3)
    _sconce(p, 9.4, 1.7)
    _sconce(p, 9.4, -5.5)
    _light(p, "GrandChandelier", Vector3(-0.8, 4.9, 0.5), Color("#efc390"), 2.6, 16.5)
    _chandelier(p, -0.8, 0.5, 4.65)
    _window(p, -9.4, -3.2)


func _chandelier(p: Node3D, x: float, z: float, y: float) -> void:
    _cyl(p, "Suspension", Vector3(x, y + 0.6, z), 0.045, 1.3, m["brass"], 8)
    _cyl(p, "ChandelierHub", Vector3(x, y, z), 0.25, 0.22, m["brass"])
    for i in range(6):
        var a := TAU*float(i)/6.0
        var dx := cos(a)*1.0
        var dz := sin(a)*1.0
        var arm := _box(p, "ChandelierArm", Vector3(x+dx*0.5, y, z+dz*0.5),
            Vector3(0.95, 0.065, 0.065), m["brass"])
        arm.rotation.y = -a
        _cyl(p, "ChandelierWax", Vector3(x+dx, y+0.27, z+dz), 0.065, 0.37, m["paper"], 8)
        _ball(p, "WarmFlame", Vector3(x+dx, y+0.5, z+dz), 0.08, m["flame"])


func _servants(p: Node3D) -> void:
    for z in [-4.4, 0.0, 3.8]:
        _box(p, "ServiceCrossbeam", Vector3(0, 4.92, z),
            Vector3(15.0, 0.3, 0.35), m["darkwood"])
    _box(p, "ToolRack", Vector3(-6.78, 2.23, -1.5), Vector3(0.2, 2.8, 3.1), m["wood"])
    for z in [-2.3, -1.8, -1.3, -0.8]:
        _box(p, "HangingTool", Vector3(-6.62, 2.25, z),
            Vector3(0.16, 1.06, 0.1), m["brass"])
    for z in [-1.8, 1.1, 3.6]:
        _cyl(p, "SupplyBarrel", Vector3(6.4, 0.55, z),
            0.47, 1.08, m["wood"])
    _table(p, -4.8, 3.5, 2.1)
    _candles(p, -4.4, 3.5, 2)
    _sconce(p, 7.7, 0.4)
    _light(p, "ServiceLantern", Vector3(2.2, 4.1, 1.3), Color("#e4aa6a"), 1.7, 12)


func _landing(p: Node3D) -> void:
    _box(p, "CandleStoneDais", Vector3(-5.5, 0.42, -0.1),
        Vector3(2.7, 0.8, 2.2), m["marble"])
    for x in [-6.3, -5.6, -4.6]:
        _candles(p, x, 0.2, 4)
    _portrait(p, 1.0, 1.0, -0.07)
    _window(p, -7.7, -4.0)
    _box(p, "LandingBench", Vector3(5.1, 0.65, 3.5),
        Vector3(2.1, 1.0, 0.7), m["darkwood"])
    _light(p, "CandleLandingLight", Vector3(-4.9, 3.6, -0.2),
        Color("#e8a874"), 1.6, 12)


func _stairs(p: Node3D) -> void:
    for s in range(13):
        var xx := -5.7 + float(s)*0.58
        _box(p, "GrandStaircaseSteps", Vector3(xx, 0.10+float(s)*0.27,
            1.0), Vector3(0.58, 0.28, 3.4), m["marble"])
    _box(p, "GrandStairRail", Vector3(-2.0, 2.1, -0.65),
        Vector3(7.6, 0.15, 0.16), m["brass"])
    _pillar(p, -7.1, -4.8)
    _portrait(p, 1, 1.2, 0.06)
    _light(p, "StairwellLamps", Vector3(1.3, 5.1, 2.3), Color("#e9c1a5"), 2.0, 14)


func _library(p: Node3D) -> void:
    for x in [-6.9, -4.2, 4.35, 7.05]:
        _books(p, x, -3.95, int(x*4.0))
    _table(p, -4.0, 2.1, 3.05)
    _candles(p, -4.0, 1.98, 2)
    _box(p, "ReadingChairSeat", Vector3(-3.0, 0.48, 4.2),
        Vector3(1.18, 0.27, 1.2), m["velvet"])
    _box(p, "ReadingChairBack", Vector3(-3.0, 1.03, 4.69),
        Vector3(1.23, 1.1, 0.23), m["velvet"])
    _light(p, "LibraryAmber", Vector3(-3.0, 4.6, -0.2), Color("#ffc18a"), 2.25, 14)
    _portrait(p, 1, 3.65, 0.04)


func _reading(p: Node3D) -> void:
    _books(p, -6.7, -2.8, 13)
    _books(p, 6.7, -4.2, 21)
    for x,z in [[-3.9, 1.6], [3.6, 3.5]]:
        var xx := float(x)
        var zz := float(z)
        _box(p, "VelvetReadingSeat", Vector3(xx, 0.50, zz),
            Vector3(1.6, 0.32, 1.3), m["bluevelvet"])
        _box(p, "VelvetChairBack", Vector3(xx, 1.07, zz+0.57),
            Vector3(1.6, 1.0, 0.28), m["bluevelvet"])
    _table(p, -0.6, -0.65, 2.0)
    _candles(p, -0.5, -0.55, 3)
    _light(p, "ReadingLamplight", Vector3(2.5, 4.4, 0.5), Color("#ead6a7"), 1.8, 13)


func _study(p: Node3D) -> void:
    _table(p, 3.3, 0.8, 4.4)
    _box(p, "StudyMap", Vector3(3.3, 1.005, 0.8),
        Vector3(2.8, 0.03, 0.9), m["paper"])
    _books(p, 6.7, -3.8, 22)
    _books(p, -6.7, -4.8, 23)
    _cyl(p, "GlobeStand", Vector3(-4.1, 0.8, 1.1), 0.11, 1.6, m["brass"])
    _ball(p, "AntiqueGlobe", Vector3(-4.1, 1.63, 1.1), 0.55, m["glass"])
    _portrait(p, -1.0, -0.8, 0.11)
    _light(p, "StudyDeskLight", Vector3(3.9, 3.9, 0.5), Color("#ffd2a0"), 2.2, 14)


func _ballroom(p: Node3D) -> void:
    for x in [-7.1, 7.1]:
        for z in [-4.5, 3.1]:
            _pillar(p, x, z)
    for x in [-8.85, 8.85]:
        for z in [-5.2, -0.6, 4.0]:
            _box(p, "BallroomDrapery", Vector3(x, 3.5, z),
                Vector3(0.3, 3.6, 1.5), m["velvet"])
    _chandelier(p, 0.0, 1.9, 5.2)
    _light(p, "BallroomChandelier", Vector3(0.0, 5.1, 1.9),
        Color("#f4d2b6"), 2.9, 18)
    _box(p, "GrandMirror", Vector3(9.05, 3.15, 1.6),
        Vector3(0.13, 2.8, 2.8), m["brass"])
    _box(p, "MirrorGlass", Vector3(8.96, 3.15, 1.6),
        Vector3(0.055, 2.45, 2.4), m["glass"])


func _music(p: Node3D) -> void:
    _box(p, "GrandPianoBody", Vector3(-4.1, 1.08, 1.35),
        Vector3(3.5, 0.38, 2.2), m["black"])
    for x in [-5.43, -2.72]:
        for z in [0.6, 2.1]:
            _cyl(p, "PianoLeg", Vector3(x, 0.47, z), 0.11, 0.89, m["darkwood"])
    _box(p, "PianoKeyboard", Vector3(-4.0, 1.16, 2.55),
        Vector3(2.8, 0.09, 0.52), m["paper"])
    for k in range(13):
        _box(p, "BlackPianoKey", Vector3(-5.25+float(k)*0.205, 1.23, 2.6),
            Vector3(0.09, 0.055, 0.28), m["black"])
    _box(p, "PianoRaisedLid", Vector3(-4.8, 2.1, 0.4),
        Vector3(0.12, 1.8, 2.9), m["black"])
    for z in [-3.0, -0.1, 2.6]:
        _box(p, "MusicStand", Vector3(5.5, 1.8, z),
            Vector3(1.6, 0.12, 1.03), m["brass"])
        _box(p, "SheetMusic", Vector3(5.5, 1.89, z),
            Vector3(1.3, 0.03, 0.85), m["paper"])
    _light(p, "MusicRoomSpotlight", Vector3(-3.8, 4.6, 1.2),
        Color("#d6cae8"), 2.0, 13)
    _portrait(p, 1, -6.0)


func _garden(p: Node3D) -> void:
    for x in [-9.15, 9.15]:
        for z in [-5.4, -1.4, 3.0]:
            _window(p, x, z)
    for x,z,h in [[-6.3,2.9,2.0], [6.7,0.7,2.4], [-7.1,-5.6,1.4], [5.2,-6.0,1.8]]:
        _plant(p, float(x), float(z), float(h))
    _cyl(p, "GardenFountainBasin", Vector3(-0.2, 0.33, 1.0),
        1.85, 0.65, m["marble"])
    _cyl(p, "GardenFountainColumn", Vector3(-0.2, 1.18, 1.0),
        0.31, 1.3, m["stone"])
    _ball(p, "GardenFountainGlass", Vector3(-0.2, 1.85, 1.0),
        0.34, m["glass"])
    _light(p, "MoonlitGlasshouse", Vector3(3.2, 4.9, -1.4),
        Color("#a6cddb"), 2.1, 16)


func _attic(p: Node3D) -> void:
    for z in [-5.5, -1.4, 2.7, 6.0]:
        for side in [-1, 1]:
            var beam := _box(p, "AtticRafter",
                Vector3(float(side)*4.0, 4.26, z),
                Vector3(8.1, 0.2, 0.21), m["darkwood"])
            beam.rotation.z = float(side)*0.15
    for x,z in [[-5.8,-4.1], [6.1,1.1], [-4.1,4.4]]:
        var xx := float(x)
        var zz := float(z)
        _box(p, "AntiqueTrunk", Vector3(xx, 0.53, zz),
            Vector3(1.7, 1.01, 1.15), m["darkwood"])
        _box(p, "TrunkBrassBand", Vector3(xx, 1.07, zz),
            Vector3(1.74, 0.09, 1.17), m["brass"])
    _light(p, "AtticOilLamp", Vector3(-4.8, 3.9, 1.7),
        Color("#eab58a"), 1.55, 11)


func _moonlit_landing(p: Node3D) -> void:
    _attic(p)
    _window(p, -9.15, -4.0)
    _window(p, 9.15, 2.8)
    _box(p, "BalconyRail", Vector3(0.2, 1.2, 1.4),
        Vector3(7.5, 0.16, 0.16), m["brass"])
    for x in [-3.2, -1.1, 1.2, 3.3]:
        _box(p, "RailPost", Vector3(x, 0.68, 1.4),
            Vector3(0.09, 1.28, 0.09), m["brass"])
    _light(p, "Moonlight", Vector3(0, 4.2, 0.0), Color("#91bae1"), 0.7, 14)


func _forbidden(p: Node3D) -> void:
    _box(p, "BrokenAltar", Vector3(-4.2, 0.88, -0.4),
        Vector3(2.6, 1.7, 1.8), m["cryptstone"])
    _box(p, "FallenBeam", Vector3(5.7, 0.48, 2.2),
        Vector3(0.36, 0.48, 4.7), m["stone"])
    _box(p, "BrokenWallRelief", Vector3(7.8, 3.7, -2.3),
        Vector3(0.21, 1.8, 2.65), m["black"])
    _light(p, "ColdForbiddenLight", Vector3(2.3, 4.2, -0.9),
        Color("#819cbc"), 1.15, 14)
    _candles(p, -5.0, -0.4, 3)


func _crypt(p: Node3D) -> void:
    for z in [-4.5, 0.3, 4.2]:
        _pillar(p, -6.9, z, 2.5, true)
    _box(p, "Sarcophagus", Vector3(-3.3, 0.61, 1.7),
        Vector3(3.05, 1.15, 1.35), m["cryptstone"])
    _box(p, "SarcophagusLid", Vector3(-3.3, 1.22, 1.7),
        Vector3(3.2, 0.19, 1.45), m["stone"])
    _box(p, "CryptObelisk", Vector3(5.3, 2.15, 0.0),
        Vector3(1.15, 4.23, 1.05), m["stone"])
    _candles(p, 4.2, -0.2, 5)
    _light(p, "CryptCandleGlow", Vector3(3.3, 3.4, 1.0),
        Color("#e1a56a"), 1.35, 12)


func _secret(p: Node3D) -> void:
    _box(p, "NarrowPassageLeft", Vector3(-6.0, 2.5, -0.3),
        Vector3(0.5, 4.7, 16), m["cryptstone"])
    _box(p, "NarrowPassageRight", Vector3(6.0, 2.5, -0.3),
        Vector3(0.5, 4.7, 16), m["cryptstone"])
    for z in [-5.3, -1.4, 2.7, 6.4]:
        _box(p, "PassageBeam", Vector3(0, 4.55, z),
            Vector3(12.0, 0.26, 0.31), m["stone"])
    _candles(p, -5.4, 3.0, 3)
    _light(p, "SecretNiche", Vector3(-4.2, 3.0, 2.8),
        Color("#d7a37d"), 1.1, 11)


func _courtyard(p: Node3D) -> void:
    for x in [-7.7, 7.7]:
        for z in [-4.0, 1.5]:
            _pillar(p, x, z, 2.5, true)
    for x,z,h in [[-6.4,4.0,2.4], [7.2,2.1,3.3], [5.9,-5.4,2.5]]:
        _plant(p, float(x), float(z), float(h))
    _cyl(p, "CourtyardWell", Vector3(-3.2, 0.66, 0.3),
        1.2, 1.28, m["stone"])
    _box(p, "CourtyardBench", Vector3(4.2, 0.5, 4.4),
        Vector3(2.8, 0.27, 0.85), m["stone"])
    _light(p, "NightGardenLight", Vector3(0.0, 6.7, 1.4),
        Color("#9bb7e0"), 1.6, 20)


func _moonlit_gate(p: Node3D) -> void:
    for x in [-6.7, 6.7]:
        _pillar(p, x, -7.3, 3.4, true)
    _box(p, "MoonGateLintel", Vector3(0, 6.7, -7.45),
        Vector3(15.4, 0.6, 1.1), m["stone"])
    for i in range(8):
        var a := PI * float(i) / 7.0
        _box(p, "GateMoonCrest",
            Vector3(cos(a)*4.7, 4.8+sin(a)*2.5, -7.1),
            Vector3(0.44, 0.42, 0.32), m["brass"])
    _box(p, "GateStarlight", Vector3(0, 2.2, -10.1),
        Vector3(7.0, 4.7, 0.2), m["glass"])
    _plant(p, -7.4, 2.6, 2.2)
    _light(p, "GateOfMoon", Vector3(0.0, 5.4, -5.4),
        Color("#9fd5dc"), 2.3, 16)
