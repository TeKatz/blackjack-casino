# Midnight Manor — embedded Godot 3D bonus experiment

## Status and scope — integrated 3D bonus

**Escape the Manor now opens in embedded Godot 3D automatically** whenever
the reel outcome has at least two Bonus/Door symbols and C# starts a Manor run.
The fixed-position cinematic experience uses 17 authored rooms and three
physical doors per view. The complete always-revealed map offers every legal
connection, including a fourth exit when appropriate. No free WASD movement
or jumpscares are part of this bonus.

The native C# `ManorRun` and `ManorSession` remain the sole authority for
wagers, room adjacency, ghost routing and speed, room rewards, items,
checkpoints, timeout, settlement and balance. Every real move, candle action,
recommended step, cashout and confirmed automatic resolution follows the
revision-checked WebView2 protocol and the durable financial journal.

A separate **Bonus üben · ohne Einsatz / Practise bonus · no wager** button
under Display Settings opens an ephemeral C# practice run. The same
`ManorRun` movement, ghost and item logic executes there, but the practice
instance is never passed to the session's financial settlement, wallet,
statistics, experience or journal. Practice offers restart and exit, and is
not available during an unresolved real-money cycle.

The original React 2D bonus remains a session-only fallback if Godot WebGL
cannot start or the player deliberately chooses the standard view. The
first 3D load occurs on explicit entry; subsequent entries reuse the iframe.
No external game-server requests or separate native Godot process are used.

## Integration and packaging

- `games/manor-bonus-3d/`: Godot 4.5.1 Compatibility/WebGL 2 with 17 deterministic room dressings: library shelves and books, study desk/globe, ballroom drapes, music room piano, winter garden plants/fountain, attic rafters, crypt tombs, outdoor gate, and individual corridor/landing details. A larger front-facing plaque and a React HTML label make each legal destination readable. The camera remains first-person but fixed, with a barely perceptible sinusoidal resting/breathing motion; reduced-motion disables this.
- `frontend/src/graphics/ManorBonus3D.tsx`: same-origin iframe and permanent HUD. Godot first loads on explicit 3D bonus/practice entry and stays mounted for subsequent visits. Inactive WebGL runs at eight frames per second. All door and inventory commands are merely intent; actual C# snapshots drive the scene, map, multipliers, ghost warning, pickups and final C#-settled payout screen.
- `frontend/src/graphics/manorGodotProtocol.ts`: same-origin message validation, strict legal room choice projection and read-only ghost proximity band for Godot lighting.
- `tools/build_manor_bonus_web.ps1`: pinned Godot editor and official single-threaded Web export templates, actual scene smoke test, then HTML/JS/WASM/PCK export into `frontend/dist/manor3d/`.
- `ci.yml` and `development-release.yml`: export the Godot files after Vite and before .NET build packaging. Development verifies the published paths. Web and Windows Godot templates are shared for the subsequent Midnight Descent export.

Godot is packaged by the desktop project as `ModernFrontend/manor3d/` and served by the existing local virtual HTTPS host. Nothing is fetched from external game servers. **No C# Manor paytable, RTP, wallet, bet, checkpoint, financial save schema, or host protocol version changed.** Practice and final receipt are additive optional snapshot fields within protocol V2. The React 2D bonus and legacy WPF source remain intact.

## Limitations and acceptance

The fixed first-person camera requires door clicks: there is no WASD, continuous pursuit or horror jumpscare system. All 17 playable canonical room IDs now select dedicated architectural furnishing variants, though the prototype still reuses the same three-door portal configuration and does not physically link the rooms in continuous space. The three physical doors are a presentation limit; the expanded map exposes every C#-legal exit. The standard 2D view remains a fallback. Camera movement is very subtle and independent of the payout model.

Godot Web uses the Compatibility renderer and can be slower than its native Windows build. The iframe initializes only on the first explicit real bonus or practice request, then stays alive between entries. This aims to remove the repeated approximately ten-second wait when opening the bonus after warmup. Cold starts may still take several seconds depending on WebView2 and the machine. No measured speedup is claimed until tested on actual Windows hardware. Godot's stretch aspect uses Expand so the scene covers non-16:9 slot areas instead of black side bars. The first Windows test still requires manual WebView2 checks for GPU support, iframe input focus, actual door selection, visual appearance, resizing, monitor scaling, audio handoff, cold/warm transition timings, the ambient impact of preloading, and frame-rate performance. If the WebGL content does not initialize, an explicit fallback/exit appears instead of changing any real game state.

## Deferred WebGL startup and portal transitions (November 2026)

The Godot WebAssembly runtime previously started automatically **1.1 seconds
after Pixi reported its first frame**. Initializing single-threaded Godot in
a hidden WebView2 iframe could monopolize the renderer's input thread for
several seconds despite the slot's background animation continuing.

**New load policy:** do not mount the Godot iframe while playing the ordinary
Midnight Manor slot. Only an explicit no-wager practice request or an active
real 3D bonus mounts it. Once mounted, the instance
stays alive for subsequent entries and runs with its existing reduced inactive
frame cap. The first bonus visit may therefore have an initial WebGL loading
screen; normal slot startup no longer pays that cost. This removes the
avoidable eager load, but an exact startup latency improvement must be
measured in a real Windows WebView2 run rather than assumed.

**Entry and exit:** once Godot sends its readiness handshake, a lightweight
0.84-second CSS/DOM wooden-door opening and perspective transition reveals
the first-person room. Leaving the demo, switching to the classic 2D bonus,
or finishing a live C# run preserves the previous HUD/frame for a 0.64-second
closing-door transition while the reels or standard bonus return behind it.
Transitions have no JavaScript physics or WebGL shader overhead; movement
controls are locked during them, and the reduced-motion setting skips both
effects. Game commands are never issued by the transition itself.

The new `shouldMountManorGodot` contract is covered by tests to prevent
regressions where routine slot opening accidentally starts the hidden
WebAssembly engine again.

## Permanent full floorplan and live bonus HUD (November 2026)

The existing Godot 3D geometry and three-door model remain unchanged. All
new presentation surfaces are React elements over the **same slot viewport**,
placed using the Windows screenshot as the reference for clear sightlines.

- **Upper-left:** room and authoritative turn (maximum 24), live current
  and checkpoint multipliers, exit target, and the localized C# ghost clue
  with distant/approaching/near/revealed severity. A short, time-limited
  narration panel appears below the status when an acknowledged C# run
  message changes. This area is kept away from the physical door labels.
- **Upper-right:** a persistent *fully uncovered* graph of all 17 playable
  rooms and their 23 connections. It is rendered from the C# run snapshot,
  not calculated by Godot. The player marker follows confirmed moves;
  visited and legally available rooms update; locked shortcuts show a
  lock symbol. Door hover highlights the same target on the map.
- **Expanded floorplan:** offers complete room names, accessible route
  choices, legend and key warnings. Certain nodes (for example Servants'
  Corridor) have four valid exits but only three physical 3D door objects.
  The fourth can be selected on this full map through the normal C#
  revision-checked `run.move` command.
- **Lower-left:** a compact inventory for acquired key, candle and charm.
  The candle is clickable only when the authoritative run reports it
  usable; charm and key are passive displays.
- **Lower-right:** cash out with the current C# multiplier, open
  **More actions** for one-step recommended route or explicitly confirmed
  auto-resolve, or fall back to standard 2D. Practice gets the same
  decisions but no financial result.
  The three original door-choice buttons remain centered at the bottom.

Exact ghost positions appear on the map only when the live C# clue is
`Candle reveals: ROOM.`. A revealed ghost in an uncharted entrance node
(e.g. Rear Foyer) is named without inventing an on-map position. Normal
proximity/direction messaging never implies knowledge of an exact ghost
room. No new ghost AI or payout probability was introduced.

The old local, simulated no-ghost Test Bonus Game has been removed.
A no-wager **C# practice run** now has the real 17-room graph, pursuit
behavior, key-gated shortcuts, collected candles, passive charm and
checkpoint rewards. It may be repeated with a new seed, but it never
credits a bankroll or saves a checkpoint. The practice status is clearly
labelled as no wager; fake payout figures are not shown as real winnings.

A real bonus can now end in Escape, Catch, Cash-out or Exhaustion. Once the
native journal has committed the actual award, the 3D result screen shows
the C# multiplier and credited play-money amount. The player acknowledges
this screen to return to the reels; no additional payout command is sent.
Ghost proximity yields whole-room cold lighting and a restrained vignette;
an item pickup, checkpoint or consumed charm receives event feedback.
A candle reveals the exact position only when the native C# clue says so.

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
