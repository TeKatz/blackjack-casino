# Midnight Industries — Factory prototype

Independent Godot 4.5.1 desktop game, launched from The Midnight Club's **left sidebar, directly beneath Empire**. No lobby tile and no shared casino credit economy.

## Prototype gameplay

- Top-down camera: WASD / arrow keys, middle-mouse drag, mouse wheel zoom.
- Four build tools: **1 Miner**, **2 Conveyor**, **3 Smelter**, **4 Warehouse**. R rotates new placements; left click places; right click demolishes (contents discarded).
- Miners must be placed on iron/copper deposits; other buildings require clear ground.
- Conveyors carry one visible item per segment. Smelters convert ore into ingots; warehouses collect them.
- The starting demonstration line produces iron ingots automatically; build your own copper line to explore the systems.
- Space pauses simulation; Ctrl+S saves immediately. The game autosaves every 30 seconds and when quitting.
- Saves reside in Godot's separate per-game user data folder, not in the casino wallet/profile.
- Language is inherited from the casino via `--language=de|en`.

## Architecture

`factory_model.gd` implements a renderer-independent, deterministic quarter-second simulation, two-phase transport, compact serialized state and validated restoration. `main.gd` handles viewport-culling, camera, visuals and UI. This is a deliberately bounded vertical slice, not yet a megafactory performance claim. Large-scale simulation profiling and potential migration of the model to C# are separate steps.

## Validate

```bash
godot --headless --path games/midnight-industries --editor --import
godot --headless --path games/midnight-industries --script res://smoke_test.gd
```

The Windows packaging script invokes these smoke tests before exporting the Godot executable and .pck.
