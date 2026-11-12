# First-person room — isolated Godot 3D prototype

The independent Godot 4.5.1 project in `games/first-person-room/` lets players test first-person movement and interaction inside a 3D environment. It is not registered in the casino lobby, does not access Casino Core, player profiles, wallet data or other game saves, and leaves Midnight Descent unchanged.

## Experience

The player starts in a lit observation chamber with a raised display plinth, floor markings, a sliding doorway, and a small alcove behind it. The geometry, lighting and materials use native Godot 3D primitives; no third-party models, textures, network access or external services are required at play time.

| Action | Input |
| --- | --- |
| Walk | WASD |
| Look around | Mouse |
| Sprint | Hold Shift |
| Jump | Space |
| Interact with the object under the crosshair (up to 3 m) | E |
| Switch between German and English | F2 |
| Release cursor | Escape |
| Capture cursor again | Click the game window |
| Exit | Escape again after releasing cursor |

**Two functional interactions:** Aim at the illuminated control panel to the left of the north door and press E to slide it open or closed. The door moves with a physics collider. Aim at the table lamp to the right and press E to turn the actual scene light and bulb emission on or off. A central prompt identifies the focused object and action feedback appears on screen.

## Start in the Godot editor

Open `games/first-person-room/project.godot` in Godot **4.5.1**, then press F5 to run its main scene. No casino executable, frontend toolchain or .NET SDK is required. The test runs as a separate window. The Compatibility renderer supports a broad range of Windows graphics hardware; this is a functional demonstration, not a full Forward+ graphics benchmark.

## Windows test build

The dedicated `First person 3D room (standalone Windows test)` GitHub Actions workflow publishes an artifact named `first-person-room-windows` containing `FirstPersonRoom.exe`, `FirstPersonRoom.pck`, and Godot redistribution notices. Extract the files into the same directory, then double-click `FirstPersonRoom.exe`.

Windows build command:

```powershell
./tools/build_first_person_room.ps1 -OutputDirectory ./artifacts/FirstPersonRoom
```

The build pins Godot 4.5.1, imports the project, runs the actual scene and interaction smoke test headlessly, and exports a Windows x64 executable. It does not alter the normal casino Development/Stable release workflows or their updater.

Independent smoke test after importing the Godot project:

```powershell
godot --headless --path games/first-person-room --script res://smoke_test.gd
```

Smoke coverage includes scene load, camera and raycast, both interactable objects, key bindings, initial lamp and door states, door activation, and lamp off/on. Manual Windows playtesting is still required for visual quality, keyboard/mouse capture, collisions, lighting, resolution handling and frame pacing.

## Deliberate limits

This is a small first-person interaction study, not yet a complete game: there are no enemies, inventory, quests, animated characters, save data, casino rewards, or shared wallet. The project is isolated enough to remove by deleting its folder, dedicated documentation, build helper and workflow.
