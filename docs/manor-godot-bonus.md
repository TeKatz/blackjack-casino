# Midnight Manor — embedded Godot 3D bonus experiment

## Status and scope

An opt-in, **presentation-only Godot 4.5.1 WebGL 2 prototype** exists within the normal React/PixiJS Midnight Manor window. The 3D view is a cinematic, fixed-position door-selection experience with distinct room dressings, **not** an FPS, a free-roaming chase or a jumpscare game. The same Window and local WebView2 virtual host remain active. Godot runs as single-threaded WebAssembly inside the slot viewport, without a separate executable.

Open Midnight Manor, expand **Display Settings / Anzeigeeinstellungen** and press **Test Bonus Game**. The reels transition to an embedded 3D entrance hall; click one of the physical doors or its high-contrast, keyboard-accessible React label to preview a room change. Subsequent locations have different architecture, furnishings, materials, and lighting. **Back to the slot / Zurück zum Automaten** ends the demonstration. The preview is entirely local React state. **No wager, balance update, payout, experience, or profile save occurs.** The trigger does not require a Door combination.

For a genuine C#-journaled run, the **Experimental 3D view for real bonus rounds** option is an explicit session-only opt-in; it is off by default, and the existing 2D map/room interface remains available. In a real bonus, the choices Godot renders are supplied by C# through `ManorPreviewProtocol.RunSnapshot.Rooms[].Available`. Incoming iframe messages are limited to `ready` and bounded `choice` actions, checked against the exact iframe source and same origin, and revalidated against the C#-derived legal room set. The established `run.move` command and the native journal own all transitions. Godot cannot alter the bonus multiplier, request a paid spin, or credit any result. The complete 2D bonus remains available using **Standard 2D view**.

## Integration and packaging

- `games/manor-bonus-3d/`: Godot 4.5.1 Compatibility/WebGL 2 with 17 deterministic room dressings: library shelves and books, study desk/globe, ballroom drapes, music room piano, winter garden plants/fountain, attic rafters, crypt tombs, outdoor gate, and individual corridor/landing details. A larger front-facing plaque and a React HTML label make each legal destination readable. The camera remains first-person but fixed, with a barely perceptible sinusoidal resting/breathing motion; reduced-motion disables this.
- `frontend/src/graphics/ManorBonus3D.tsx`: same-origin iframe overlay in the slot viewport. The iframe now preloads after the PixiJS UI is ready and stays mounted behind the reels between previews. While inactive, the WebGL process is throttled to eight frames per second and cannot accept gameplay actions. A return to the bonus reuses the same initialized WebAssembly engine, rather than cold-starting the iframe; the first visit can still require initialization. No additional browser or Godot executable is launched.
- `frontend/src/graphics/manorGodotProtocol.ts`: message validation, legal room choice projection and independent test map.
- `tools/build_manor_bonus_web.ps1`: pinned Godot editor and official single-threaded Web export templates, actual scene smoke test, then HTML/JS/WASM/PCK export into `frontend/dist/manor3d/`.
- `ci.yml` and `development-release.yml`: export the Godot files after Vite and before .NET build packaging. Development verifies the published paths. Web and Windows Godot templates are shared for the subsequent Midnight Descent export.

Godot is packaged by the desktop project as `ModernFrontend/manor3d/` and served by the existing local virtual HTTPS host. Nothing is fetched from external game servers. **No C# Manor paytable, RTP, wallet, bet, checkpoint, save schema, or host protocol version changed.** The React 2D bonus and legacy WPF source remain intact.

## Limitations and acceptance

The fixed first-person camera requires door clicks: there is no WASD, continuous pursuit or horror jumpscare system. All 17 playable canonical room IDs now select dedicated architectural furnishing variants, though the prototype still reuses the same three-door portal configuration and does not physically link the rooms in continuous space. This remains a hosting and cinematic transition **vertical slice**, not the finished 3D bonus. At most three legal destinations can be offered in the 3D scene; the 2D view is the complete alternative. Camera movement is very subtle and independent of the payout model.

Godot Web uses the Compatibility renderer and can be slower than its native Windows build. The iframe initializes during ordinary reel play after the main renderer is ready, then stays alive between entries. This aims to remove the repeated approximately ten-second wait when opening the bonus after warmup. Cold starts may still take several seconds depending on WebView2 and the machine. No measured speedup is claimed until tested on actual Windows hardware. Godot's stretch aspect uses Expand so the scene covers non-16:9 slot areas instead of black side bars. The first Windows test still requires manual WebView2 checks for GPU support, iframe input focus, actual door selection, visual appearance, resizing, monitor scaling, audio handoff, cold/warm transition timings, the ambient impact of preloading, and frame-rate performance. If the WebGL content does not initialize, an explicit fallback/exit appears instead of changing any real game state.

## Room layout and WebGL material correction

The second Manor 3D quality pass responds to tester reports for Candle Landing, Reading Room, Library, Grand Staircase, Moonlit Landing, Forbidden Wing, Courtyard, Secret Passage, Crypt, Winter Garden, Music Room, Servants' Corridor, and Moonlit Gate.

- **Material fix:** All primitive geometry now uses the mesh's explicit repeating UVs, not world-triplanar sampling (which is visually expensive and produced inconsistent wall/stone/floor appearance under the Compatibility renderer). New tiled plaster, paving and cracked crypt-floor artwork complements the existing wood, stone and wallpaper resources. Materials are shared by material ID and tile scale rather than recreated for every box.
- **Support heights:** Candle placement requires the real height of its tabletop/altar/shelf/plinth. Visible holders sit on the support and wax bodies rise above them. The new geometry test checks every candle base in all 17 locations.
- **Navigable views:** Bookshelves face into the room and are anchored to the side walls, not sitting across the approach to doors. The Grand Staircase moves to a west-side stairwell, and the Secret Passage's two walls move outside the door clearance.
- **Distinct interiors:** Moonlit Landing is a blue marble balcony with an orrery and open windows, not the attic reused with extra railings. The Forbidden Wing's dangling black rectangle becomes a carved wall relief with a supporting sill; stone vault braces are separated from the textured ceiling.
- **Actual greenhouse windows:** The Winter Garden and Moonlit Landing side walls are segmented above, below and between the stained-glass openings. Glazing and leadwork now occupy real wall holes, not the face of opaque blocks.
- **Supported instruments:** Three Music Room sheet stands have readable scores, legs and floor tripods; all remain outside the door route.
- **Outdoor masonry:** Courtyard and Moonlit Gate paving, stone columns, parapets and gate lintels all have repeating, image-backed materials.

`room_geometry_test.gd` is executed in the Windows CI *before* exporting the Godot Web package. It checks texture binding and non-triplanar UV mapping in all 17 rooms; supports and bottom heights for every candle; representative textured props; side shelves; glass openings; door clearance; vault/ceiling separation; Moonlit Landing uniqueness; and the existence of music stands' legs. The C# payout system and bankroll remain unchanged.

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
