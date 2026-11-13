# Midnight Descent – pixel art direction

Classic, modern top-down pixel art. No Hades-style painterly artwork or faux 3D. The game world uses a two-logical-unit pixel grid over the existing 1280×720 combat coordinates (equivalent to 640×360 for world textures); 1920×1080 gives 3 display pixels per source pixel. UI and telegraphs retain the existing high-resolution canvas.

## Asset conventions
- Original source atlases in `games/midnight-descent/art/pixel/`: 32×32 actor cells, four direction rows (south, north, west, east), eight frame columns (two idle, four locomotion, attack, hurt). Boss scales to 128 logical units.
- Vault atlas: four 32px floor variants, masonry, pillar, gate, trim. Four torch frames; six relic icons.
- Nearest texture filtering, no smooth resampling of art. Use dark outlines, restrained cool stone and warm metallic accents. Each enemy has a distinct silhouette and recognizable role.
- `pixel_world.gd` is a presentation layer; the existing run model owns hitboxes, combat, room progression and relic effects. PointLight2D with radial textures, warm torch flicker, projectile and gate light, LightOccluder2D on pillars. Five static lights and a bounded pool of four projectile lights.
- HUD remains crisp and uses meters and relic icons. Active item is marked empty until the system is implemented. No permanent upgrades or new characters added during this visual milestone.

## Verification
Existing headless gameplay smoke additionally asserts the pixel atlas dimensions, light count and initial wall occluders. Windows visual inspection is still required for 1080p scaling, shadow quality, and combat readability.
