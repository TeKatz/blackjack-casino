extends RefCounted

# Hand-authored, locally bundled SVG texture atlases. Godot rasterizes these to
# mipmapped textures on import; all materials remain fully offline.
const ALBEDOS: Dictionary = {
    "wall": preload("res://art/wall.svg"),
    "floor": preload("res://art/floor.svg"),
    "ceiling": preload("res://art/ceiling.svg"),
    "brushed": preload("res://art/brushed.svg"),
    "rubber": preload("res://art/rubber.svg"),
    "fabric": preload("res://art/fabric.svg"),
    "cargo": preload("res://art/cargo.svg"),
    "console": preload("res://art/console.svg"),
}

const PLANET: Texture2D = preload("res://art/planet.svg")
const MOON: Texture2D = preload("res://art/moon.svg")
const SPACE_PANORAMA: Texture2D = preload("res://art/space_panorama.svg")

var _micro_normal: NoiseTexture2D
var _roughness_variation: NoiseTexture2D


func _init() -> void:
    var micro_noise := FastNoiseLite.new()
    micro_noise.seed = 22753
    micro_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
    micro_noise.frequency = 0.1
    micro_noise.fractal_type = FastNoiseLite.FRACTAL_FBM
    micro_noise.fractal_octaves = 3

    _micro_normal = NoiseTexture2D.new()
    _micro_normal.width = 256
    _micro_normal.height = 256
    _micro_normal.seamless = true
    _micro_normal.as_normal_map = true
    _micro_normal.bump_strength = 0.7
    _micro_normal.noise = micro_noise

    var rough_noise := FastNoiseLite.new()
    rough_noise.seed = 78311
    rough_noise.noise_type = FastNoiseLite.TYPE_SIMPLEX
    rough_noise.frequency = 0.032
    _roughness_variation = NoiseTexture2D.new()
    _roughness_variation.width = 256
    _roughness_variation.height = 256
    _roughness_variation.seamless = true
    _roughness_variation.noise = rough_noise


func apply(material: StandardMaterial3D, kind: String, triplanar: bool = true) -> void:
    if not ALBEDOS.has(kind):
        push_error("Unknown Vesper texture: " + kind)
        return
    material.albedo_texture = ALBEDOS[kind]
    material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
    material.texture_repeat = true
    if triplanar:
        material.uv1_triplanar = true
        material.uv1_world_triplanar = true
        material.uv1_triplanar_sharpness = 7.0
        material.uv1_scale = Vector3(0.5, 0.5, 0.5)
    else:
        material.uv1_scale = Vector3.ONE

    # Real roughness and normal-texture channels, not just untextured color.
    # This bump is deliberately subtle so distant lights do not create moire.
    if kind != "console":
        material.normal_enabled = true
        material.normal_texture = _micro_normal
        material.normal_scale = 0.15 if kind == "brushed" else 0.3
        material.roughness_texture = _roughness_variation
        material.roughness_texture_channel = BaseMaterial3D.TEXTURE_CHANNEL_RED


func celestial(texture: Texture2D, rough: float = 1.0) -> StandardMaterial3D:
    var material := StandardMaterial3D.new()
    material.albedo_color = Color.WHITE
    material.albedo_texture = texture
    material.roughness = rough
    material.texture_filter = BaseMaterial3D.TEXTURE_FILTER_LINEAR_WITH_MIPMAPS_ANISOTROPIC
    material.texture_repeat = true
    return material


func sky() -> Sky:
    var material := PanoramaSkyMaterial.new()
    material.panorama = SPACE_PANORAMA
    material.energy_multiplier = 1.0
    var sky_resource := Sky.new()
    sky_resource.sky_material = material
    return sky_resource
