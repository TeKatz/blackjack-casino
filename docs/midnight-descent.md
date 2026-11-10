# Midnight Descent — Godot foundation

A dedicated offline, 2D action roguelite for The Midnight Club. The desktop casino remains WPF/React/PixiJS. This mode uses Godot 4.5.1 (GDScript, Compatibility renderer) in a separate native process. The casino launcher passes only the UI language, minimizes the casino until the process exits, and never sends balances or player data to Godot.

## Foundation scope

- Twelve lobby activities on the existing 4×3 desktop grid; Arcade and Descent occupy one tile each.
- Original local dungeon key art, German/English menu, keyboard/mouse navigation, character preview, Codex, fullscreen toggle and return to casino.
- New expedition opens character selection; the run action is intentionally disabled. There are no combat, procedural levels, rewards, progression or save systems in this milestone.
- The Godot editor is a build-time dependency, not a player prerequisite.

## Build

From a Windows checkout run `pwsh ./tools/build_midnight_descent.ps1 -OutputDirectory artifacts/app/MidnightDescent`. The script downloads the pinned official editor and Windows export template, imports the project, exercises the menu routes headlessly, and exports the EXE and PCK beside the casino application. CI preview and both release workflows call the same script. For local desktop development export to the MidnightDescent folder next to the built desktop executable. For standalone editing, open games/midnight-descent/project.godot in Godot 4.5.1.

## Next milestone

Add a dedicated gameplay scene, input/combat, seeded room graph, data-driven characters and relics, save schema and native receipt protocol before any shared casino rewards. Keep simulation inside Godot; do not route frame updates through WPF or WebView2.
