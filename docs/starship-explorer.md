# Vesper — standalone starship exploration prototype

`games/starship-explorer/` is a completely independent Godot 4.5.1 first-person 3D prototype. It is not registered in the Blackjack Casino launcher, uses no casino bankroll or profile services, changes no saved games, and does not modify Midnight Descent or the original first-person room prototype.

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

Smoke tests verify the player camera and controls, generated exterior scene, four doors, cargo light circuit, flashlight acquisition and toggle, and grabbing / dropping physics props. A separate manual Windows test remains necessary to assess visual composition, scene brightness, collision feel, mouse movement, graphics performance, and ship atmosphere.

## Scope and rollback

The ship is intentionally modular and self-contained. The project adds only its own `games/starship-explorer/` folder, `tools/build_starship_explorer.ps1`, `docs/starship-explorer.md`, and `.github/workflows/starship-explorer.yml`. Removing those paths fully reverses it without impacting the casino or Midnight Descent.
