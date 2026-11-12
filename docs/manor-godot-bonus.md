# Midnight Manor — embedded Godot 3D bonus experiment

## Status and scope

An opt-in, **presentation-only Godot 4.5.1 WebGL 2 prototype** exists within the normal React/PixiJS Midnight Manor window. The 3D hall is a small cinematic door-selection scene, **not** an FPS, a free-roaming chase or a jumpscare game. The same Window and local WebView2 virtual host remain active. Godot runs as single-threaded WebAssembly inside the slot viewport, without a separate executable.

Open Midnight Manor, expand **Display Settings / Anzeigeeinstellungen** and press **Test Bonus Game**. The reels transition to an embedded 3D entrance hall; click one of its doors to preview a room change. **Back to the slot / Zurück zum Automaten** ends the demonstration. The preview is entirely local React state. **No wager, balance update, payout, experience, or profile save occurs.** The trigger does not require a Door combination.

For a genuine C#-journaled run, the **Experimental 3D view for real bonus rounds** option is an explicit session-only opt-in; it is off by default, and the existing 2D map/room interface remains available. In a real bonus, the choices Godot renders are supplied by C# through `ManorPreviewProtocol.RunSnapshot.Rooms[].Available`. Incoming iframe messages are limited to `ready` and bounded `choice` actions, checked against the exact iframe source and same origin, and revalidated against the C#-derived legal room set. The established `run.move` command and the native journal own all transitions. Godot cannot alter the bonus multiplier, request a paid spin, or credit any result. The complete 2D bonus remains available using **Standard 2D view**.

## Integration and packaging

- `games/manor-bonus-3d/`: Godot 4.5.1 Compatibility/WebGL 2 hall with fixed camera, clickable carved doors, marble columns, chandelier, parquet floor, wallpaper and lighting. Door labels and target IDs come from React state, not Godot-generated outcomes.
- `frontend/src/graphics/ManorBonus3D.tsx`: same-origin iframe overlay in the slot viewport, demo-only navigation and optional live dispatch, explicit exit and fallback controls.
- `frontend/src/graphics/manorGodotProtocol.ts`: message validation, legal room choice projection and independent test map.
- `tools/build_manor_bonus_web.ps1`: pinned Godot editor and official single-threaded Web export templates, actual scene smoke test, then HTML/JS/WASM/PCK export into `frontend/dist/manor3d/`.
- `ci.yml` and `development-release.yml`: export the Godot files after Vite and before .NET build packaging. Development verifies the published paths. Web and Windows Godot templates are shared for the subsequent Midnight Descent export.

Godot is packaged by the desktop project as `ModernFrontend/manor3d/` and served by the existing local virtual HTTPS host. Nothing is fetched from external game servers. **No C# Manor paytable, RTP, wallet, bet, checkpoint, save schema, or host protocol version changed.** The React 2D bonus and legacy WPF source remain intact.

## Limitations and acceptance

The fixed camera requires door clicks: there is no WASD, continuous pursuit or horror jumpscare system. The current scene is one decorated hall with three door targets whose titles and camera animation change by room, not a fully modeled 19-room map. This build is a hosting and transition **vertical slice**, not the finished 3D bonus. At most three legal destinations can be offered in the 3D scene; the 2D view is the complete alternative.

Godot Web uses the Compatibility renderer and can be slower than its native Windows build. The first visit may display a loading veil while WebAssembly initializes, rather than an instantaneous cold transition. The first Windows test still requires manual WebView2 checks for GPU support, iframe input focus, actual door selection, visual appearance, resizing, monitor scaling, audio handoff and transition performance. If the WebGL content does not initialize, an explicit fallback/exit appears instead of changing any real game state.

## Local build sequence (Windows)

```powershell
cd frontend
npm ci
npm run build
cd ..
./tools/build_manor_bonus_web.ps1 -OutputDirectory frontend/dist/manor3d
dotnet build BlackjackCasino.sln -c Release
```

Development Release performs these steps automatically. The generated export must remain under `frontend/dist`, not committed as source.
