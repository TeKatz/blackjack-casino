# Midnight Industries — premium-industrial factory prototype

Standalone Godot 4.5.1 Windows mode integrated into The Midnight Club via the **Empire → Midnight Industries** left navigation link, without adding a lobby tile. The mode is offline, keeps its own versioned save file and never changes casino credits.

## Industrial art direction

The visual identity is **Modern Industrial / Premium Automation**. Previous green-teal debug grids, flat rounded button boxes, primitive geometric machine drawings, placeholder ore icons and old selection rectangles are removed from the rendering path.

- **World:** a subdued green meadow with fine seeded variation. Ore patches sit in natural grey-brown stony soil, with noise-broken, smoothly blended margins rather than grid-aligned edges. A single masked terrain shader replaces the former metal floor tiles; machinery stays cached in independently redrawable 16×16-cell chunks.
- **Machines:** dedicated 144-pixel metallic art for drill, conveyor, furnace and storage. The warehouse artwork is aligned left within its logical cell. Terrain deposits now have **eight distinct rasterized crystal-cluster textures per material** with differing silhouettes, veining and facets rather than rotations of one sprite. Belt cargo has a compact contact shadow and raised faceted 2.5D geometry.
- **Production animation:** belts show directional cyan pulses, goods interpolate at the display framerate, drills indicate mechanical motion, active furnaces emit warm glow, sparks and smoke, storage displays a subtle status light.
- **Interaction:** selection uses amber industrial scan brackets; build previews show the final machine graphic and a strong outgoing arrow; invalid placement uses red hazard feedback.
- **UI:** distinct embossed graphite and steel panel families, amber active controls, restrained cyan information channels, a compact HUD and a collapsible idle inspector, a German/English machine catalog with genuine sprite icons, selected-machine telemetry, resource readouts, quick slots, an animated menu and usable typography (Bahnschrift/Segoe UI with fallbacks).

The 144-pixel machinery artwork is shipped as `assets/industrial_hd_atlas.webp`; separate `factory_ore_variants.gd` generates sixteen mineral textures once. `factory_terrain.gd` builds grass/rock detail textures and a resource-aware transition mask once on startup. `factory_skin.gd` caches nine-slice metal UI textures; `factory_chunk.gd` and `factory_static.gd` draw only placed assets and visible resource deposits; `factory_motion.gd` contains animation only. No runtime downloads are required.

Colors: graphite `#111A21`, steel `#273743`, illuminated amber `#F5C04A`, neon cyan `#00D8F4`, furnace orange `#FF982D`, critical red `#FF5B50`. Avoid the old green pixel-fantasy casino panel language.

## Gameplay and controls (unchanged)

| Control | Action |
| --- | --- |
| WASD / arrows | Move camera |
| Left-click and hold + drag (selection mode) | Pan camera; a short click still selects machinery |
| Middle-drag / mouse wheel | Pan (all modes) / zoom |
| V | Inspect/select mode; click to inspect a building |
| B | Construction mode; click to place the active machine |
| 1–4 | Choose mining drill, belt, furnace, warehouse |
| R | Rotate build preview or selected machine |
| Right click | Cancel build / clear selection (never destructive) |
| Space | Pause production |
| Ctrl+S | Save |
| Escape | Open/close the main menu |

The main menu supports continue, save, confirmed new factory, motion setting and **Save & Return to Casino**. A built-in starter iron chain mines, transports, smelts and collects ingots. Copper deposits and additional patches allow building other chains.

**No changes to `factory_model.gd`** are required by this visual package. The machine recipes, ticks, throughput, inventory values, version-1 save shape, old-save compatibility and independent casino profile are unchanged.

## Validation

```sh
godot --headless --path games/midnight-industries --editor --import
godot --headless --path games/midnight-industries --script res://smoke_test.gd
godot --headless --path games/midnight-industries --script res://ui_smoke.gd
godot --headless --path games/midnight-industries --script res://visual_smoke.gd
godot --headless --path games/midnight-industries --script res://performance_smoke.gd
```

The CI workflow validates actual atlas decoding, nine-slice art, machine icons, terrain chunks, building/inspection UI and production/model behavior. The Windows export script additionally validates the premium visual package before packaging. A headless factory benchmark is not a GPU framerate guarantee; device-specific 1080p/1440p quality and performance still require visual testing on a Windows client.

### Current quality/performance pass

- The old repetitive metal platform was replaced with one meadow-to-rock shader surface; ore deposits form softly blended organic rocky patches. No per-frame primitive ground drawing.
- The machine inspector collapses when no building is selected; header, catalog and quickbar are substantially narrower.
- Belts and ore have no oversized drop shadows; transport products now have shaded, raised geometry, while machines retain subtle contact shades.
- Actual Windows FPS remains a device-side acceptance check; headless simulation speed is not a GPU benchmark.
