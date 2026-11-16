# Midnight Industries — interactive factory prototype

Independent Godot 4.5.1 desktop mode, started from The Midnight Club's left navigation **beneath Empire**, without an extra lobby tile. The factory economy and save data remain separate from casino credits.

## Play loop and controls

The main menu is shown when the game opens. **Escape** opens or closes the menu; it does not exit immediately. The menu supports resuming, saving, starting a new factory (with confirmation), toggling decorative animation and **Save & Return to Casino**. Autosave runs every 30 seconds of active simulation and when quitting.

| Control | Effect |
| --- | --- |
| WASD / arrow keys | Move the camera |
| Middle-mouse drag / mouse wheel | Pan / zoom |
| V | Inspect/select mode; left click a building to inspect it |
| B | Construction mode; left click to build |
| 1–4 | Pick Miner, Conveyor, Smelter, Storage; enter construction mode |
| R | Rotate the construction preview in build mode; rotate selected machine in inspect mode |
| Right click | Cancel construction / clear selection (never silently demolish) |
| Space | Pause/resume factory simulation |
| Ctrl+S | Save immediately |
| Escape | Main and pause menu |

In inspect mode, a machine's inspector shows its type, coordinates, output direction, resource/inventory, progress and rate. Inspector buttons rotate or deliberately demolish selected buildings. The construction ghost contains the correct output arrow before placement, including for conveyors.

The start map has iron and copper ore patches, two fixed demonstration lines and starter resource warehouses. A successful iron chain mines, transports, smelts and deposits ingots. Build a copper line to test exploration and routing. The save format remains **version 1**, compatible with original first-prototype progress.

## Rendering and architecture

- `factory_model.gd` runs the authoritative quarter-second tick independent of the graphical framerate. Sorted machine coordinates are cached until construction changes. A transient movement trace supports visual interpolation but is never saved as authoritative state.
- `main.gd` hosts the native Godot HUD, camera, menu and selection/build input. `factory_art.gd` supplies shared machine art and preview arrows.
- `factory_static.gd` draws resource and machine art only when the factory topology changes. A single terrain shader replaces reissuing thousands of background rectangles on every frame.
- `factory_motion.gd` redraws only visible animated machines and goods. Goods interpolate across the complete simulation step to achieve smooth 60 Hz motion at normal display framerates.
- Existing saves, casino wallet and the lobby's tile artwork are unaffected.

A factory of thousands of buildings is a future goal rather than a proven FPS guarantee. A headless benchmark records simulation time on a large grid, but GPU and live machine performance still require testing on Windows hardware.

## Validation

```sh
godot --headless --path games/midnight-industries --editor --import
godot --headless --path games/midnight-industries --script res://smoke_test.gd
godot --headless --path games/midnight-industries --script res://ui_smoke.gd
godot --headless --path games/midnight-industries --script res://performance_smoke.gd
```

CI checks a real headless scene startup, simulation behavior, menu and selection operations, and a 3,000+ building stress sample. Windows packaging independently exports and verifies the shipped Godot executable.
