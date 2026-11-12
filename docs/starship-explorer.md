# Vesper — standalone starship exploration prototype

`games/starship-explorer/` is a completely independent Godot 4.5.1 first-person 3D prototype. It is not registered in the Blackjack Casino launcher, uses no casino bankroll or profile services, changes no saved games, and does not modify Midnight Descent or the original first-person room prototype.

## Visual Foundation (0.2)

The ship now ships with **original, local SVG textures** rather than flat-colored primitive meshes. Godot imports them as filtered/mipmapped textures, and `surface_library.gd` configures PBR albedo/normal/roughness, metallic response and mostly triplanar mapping for large modular walls. Surfaces include separate designs for wall panels, antislip floor plates, vented ceiling tiles, brushed steel, rubber, crew fabric, cargo crates and terminals. The existing architecture receives physical trim, vents, floor inserts, equipment details, cargo restraints and a functional inward-facing cabin locker.

The view through the observation window uses a **painted panoramic starfield**, not the previous hundreds of sphere-shaped "stars". The nearby planet and moon remain geometrical spheres as expected for celestial bodies, but now use authored map textures. The panorama, planets and all surface artwork are bundled offline under `games/starship-explorer/art/`; no external asset requests or licensing dependencies are introduced.

**Lighting:** the moving flashlight's Compatibility-renderer shadow map caused distant patterned artifacts. Its shadow map is disabled; the cone is shorter/softer and its range stops at the first solid surface found by a forward ray. This removes shadow-map acne, at the cost of not casting hard flashlight shadows. Ambient shadow bias is also tuned. Manual GPU acceptance is still needed to verify perceived brightness, light leakage and any residual moiré on a real Windows display.

**Carried-object collisions:** pickup no longer zeros the object's collision layers or masks. Held rigid bodies use kinematic freeze mode, keep their physical shape and are moved via shape-cast sweeps against walls, furniture and other physical props. Only collisions with the player are temporarily excepted. Drop restores the original freeze mode and player collision. The smoke test checks that a held object cannot pass through a specific lounge bulkhead.

**Locker:** the cabin storage unit faces into the room, and the interactive latch moves with the hinged door instead of remaining in the wall.

## Ship environment

The **Vesper** contains five connected spaces: observation lounge, central corridor, crew cabin, engineering workshop and a dark cargo bay. The lounge's panoramic window looks into a real 3D exterior starfield with a distant planet and moon. Native geometry, metallic finishes, light trims and scene lights establish the spaceship's atmosphere without loading external art or requiring connectivity.

| Control | Action |
| --- | --- |
| WASD / mouse | Walk / free look |
| Hold Shift / Space | Sprint / jump |
| E | Use control or pick up / put down a physics prop |
| G | Drop a held object |
| F | Toggle flashlight after collecting it |
| F2 | Switch German / English UI |
| Escape | Release mouse, press again to exit |
| Mouse click | Recapture the pointer |

**Things to try:**

- Start in the lounge facing the observation window; look at the planet and stars.
- Find the flashlight on the equipment table, aim at it, press E, and use F to activate its real 3D spotlight.
- Use nearby control panels to open/close the lounge, crew, engineering and cargo bulkhead doors.
- Walk into the cargo bay with the flashlight; find the cargo lighting breaker and switch the overhead lighting on.
- Pick up and set down small physical objects such as a mug, slate, battery and cargo parcel.
- Use the engineering terminal, crew locker, lounge light breaker and panorama shutter controls.

All these interactions are local to the Godot scene. There is no narrative, inventory persistence, enemies, ship piloting or external online services.

## Play and export

With Godot 4.5.1 installed, open `games/starship-explorer/project.godot` and press F5. For a prebuilt Windows x64 test, open the dedicated **Starship Explorer 3D prototype (Windows)** GitHub Actions run and download the `starship-explorer-windows` artifact. Extract `StarshipExplorer.exe` and `StarshipExplorer.pck` into the same folder, then start the executable.

The isolated Windows build can also be generated with:

```powershell
./tools/build_starship_explorer.ps1 -OutputDirectory ./artifacts/StarshipExplorer
```

The build script pins Godot 4.5.1, imports the project, executes the actual scene's headless interaction smoke test, and exports an independent Windows application. Neither Development nor Stable release workflows have been changed.

Run the smoke test locally after importing the Godot project:

```powershell
godot --headless --path games/starship-explorer --script res://smoke_test.gd
```

Smoke tests verify the player camera and controls, imported image-based panoramic sky and textured planetary/room materials, four doors, cargo light circuit, flashlight acquisition and toggle, flashlight shadow settings, the inward-facing locker latch, grabbing / dropping physics props and a held-object sweep against the lounge bulkhead. A separate manual Windows test remains necessary to assess visual composition, scene brightness, collision feel, mouse movement, graphics performance, and ship atmosphere.

## Scope and rollback

The ship is intentionally modular and self-contained. The project adds only its own `games/starship-explorer/` folder, `tools/build_starship_explorer.ps1`, `docs/starship-explorer.md`, and `.github/workflows/starship-explorer.yml`. Removing those paths fully reverses it without impacting the casino or Midnight Descent.
