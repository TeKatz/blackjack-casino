# Midnight Industries — premium-industrial factory prototype

Standalone Godot 4.5.1 Windows mode integrated into The Midnight Club via the **Empire → Midnight Industries** left navigation link, without adding a lobby tile. The mode is offline, keeps its own versioned save file and never changes casino credits.

## Industrial art direction

The visual identity is **Modern Industrial / Premium Automation**. Previous green-teal debug grids, flat rounded button boxes, primitive geometric machine drawings, placeholder ore icons and old selection rectangles are removed from the rendering path.

- **World:** a modular charcoal steel platform with offset plates, bolted corners, recessed maintenance hatches, safety-striped panels and warm utility lighting. Each 16×16 tile section is independently cached/cullable.
- **Machines:** dedicated authored metallic art for drill, conveyor, furnace and storage, with dense silhouettes, warm/cool emission and visible functional outputs. Ore and cargo use matching illustrated assets.
- **Production animation:** belts show directional cyan pulses, goods interpolate at the display framerate, drills indicate mechanical motion, active furnaces emit warm glow, sparks and smoke, storage displays a subtle status light.
- **Interaction:** selection uses amber industrial scan brackets; build previews show the final machine graphic and a strong outgoing arrow; invalid placement uses red hazard feedback.
- **UI:** brushed-metal/graphite nine-slice art, amber functional highlights, cyan information channels, a German/English machine catalog with genuine sprite icons, selected-machine telemetry, resource readouts, quick slots, an animated menu and usable typography (Bahnschrift/Segoe UI with fallbacks).

The image atlas is embedded as Godot script resources in `assets/atlas_part_*.gd` and loaded once by `factory_atlas.gd`. No runtime download is necessary. `factory_skin.gd` generates cached high-contrast nine-slice panel and button texture assets once on load. `factory_chunk.gd` and `factory_static.gd` own static world drawing, while `factory_motion.gd` contains visual feedback only.

Colors: graphite `#111A21`, steel `#273743`, illuminated amber `#F5C04A`, neon cyan `#00D8F4`, furnace orange `#FF982D`, critical red `#FF5B50`. Avoid the old green pixel-fantasy casino panel language.

## Gameplay and controls (unchanged)

| Control | Action |
| --- | --- |
| WASD / arrows | Move camera |
| Middle-drag / mouse wheel | Pan / zoom |
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
