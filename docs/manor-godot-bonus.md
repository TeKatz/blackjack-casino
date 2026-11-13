# Midnight Manor — integrated Godot 3D Escape bonus

## Current gameplay

The **only normal Escape the Manor bonus presentation** is Godot 4.5.1
Compatibility/WebGL 2 embedded in Midnight Manor's existing local React/PixiJS
WebView2 cabinet. Two or more Bonus/Door symbols start the native C# Manor
run, followed by the 3D scene inside the slot viewport. There is no WASD
control, free-roaming pursuit, second 2D bonus or jumpscare system.

All 17 playable canonical room IDs have authored Godot scene variants. Three
physical doors show up to three legal destinations; a persistent, fully
uncovered map in the upper right provides the complete C# room graph,
including a fourth legal exit where applicable. The player chooses a door or
a legal map node. A subtle first-person breathing animation can be disabled
through reduced-motion preferences.

The React overlay shows the authoritative turn, current and banked
multipliers, final gate target, and ghost distance clue. The key unlocks
C#-gated shortcuts. A candle may reveal the actual ghost location without
taking a turn, and a passive charm automatically prevents one catch. The
C# engine owns ghost AI, room collection, checkpoints, maximum turns,
cashout, auto-route and resolution.

A practice button, **Bonus üben · ohne Einsatz / Practise bonus · no wager**,
runs the same C# ManorRun AI, items and room rules in an ephemeral state,
never touching the financial journal, wallet or statistics. Practice can be
restarted or exited. It cannot be used during an unresolved paid cycle.

## Rendering lifecycle and the post-bonus FPS regression

The first implementation kept the Godot iframe mounted and throttled the
inactive Godot runtime to eight frames per second after returning to the
slot. That retained the 3D WebGL context, textures, WebAssembly runtime,
compositor surfaces and a second render loop while the PixiJS slot was
already running. This was identified as the likely cause of severe
persistent slot FPS degradation *after* the first 3D use.

The current lifecycle deliberately trades faster warm re-entry for strict
resource separation:

1. **Reels active:** no Godot iframe exists; only PixiJS draws the slot.
2. **Bonus/practice entered:** mount a fresh Godot iframe. Godot initializes
   using local packaged assets and must send a trusted ready message before
   physical door input is enabled.
3. **Run finished:** C# commits the reward once; React shows its final
   multiplier and paid amount. The iframe is removed as soon as this
   conclusion is displayed, even before the user clicks back to the reels.
4. **Practice exits / result acknowledged:** the bonus React overlay itself
   unmounts. No hidden Godot iframe remains, no inactive 8-FPS render loop
   is kept behind the reels.
5. **Another bonus:** create a new WebGL context. HTTP/browser asset caching
   may reduce transfer time, but the WASM runtime and graphics resources are
   newly initialized. A cold-start loading screen is expected on each visit.

This is a deterministic DOM/WebGL-lifecycle fix, not a claim of a measured
device-specific FPS uplift. **An actual Win10/Win11 WebView2 extended-session
test is required** to quantify recovery of FPS, frame time, CPU and GPU
utilization and memory/VRAM after unloading.

## Native authority, recovery and safety

C# `ManorRun`, `ManorSession`, `ManorModernGameService` and the durable
journal remain authoritative for outcomes, bankroll, payouts, route legality,
ghost moves, inventory and continuation. The iframe only proposes one of
the C#-available destinations. Messages are origin/source checked and
revisioned; the browser never claims a reward.

The old React `ManorBonusStage`, `ManorRunMap`, `ManorRoomScene`, their
tests and 2D-only styles have been removed, as has the switch back to that
view. The independent historical WPF files are not opened by the modern
Manor bonus. They are retained only for the native legacy project and
repository-level compatibility coverage, not as a hidden replacement mode.

**If WebGL cannot initialize:** the user sees a retry action, not the
retired bonus UI. For a genuine unresolved C# run, the same recovery panel
offers a direct C# cashout or a separately confirmed C# automatic resolve,
so an already-committed wager is not stranded. Practice offers exit.
Neither path simulates payouts locally. On retry, the existing failed iframe
is discarded and a fresh iframe/ready handshake is required.

## Presentation and packaging

- `games/manor-bonus-3d/`: 17-room procedural 3D art, lighting and door
  transition, interactive doorway hit areas, seeded scene smoke and geometry
  tests. The scene does not evaluate gambling outcomes.
- `frontend/src/graphics/ManorBonus3D.tsx`: dynamic Godot iframe, permanent
  bonus HUD, fully revealed map, ghost/item feedback, C# settlement receipt
  and WebGL failure recovery.
- `frontend/src/graphics/manorGodotLoadPolicy.ts`: asserts the iframe
  must never exist in ordinary slot mode.
- `frontend/src/graphics/manorGhostClue.ts`: classifies already-authorized
  C# ghost clues for atmospheric feedback, not location prediction.
- `tools/build_manor_bonus_web.ps1`: pinned Godot editor/export templates,
  Godot headless checks and self-contained Web export to
  `frontend/dist/manor3d/` before desktop packaging.

All Web assets are bundled offline under the existing local virtual host;
no separate process is launched. C# paytable, payout probabilities, bet
limits, journal format, protocol version and save migration are unchanged.
No 3D content is preloaded during ordinary reel play.

## Acceptance checklist

Automated checks cover frontend typing and UI/protocol tests, C# wallet and
practice isolation, Godot scene/geometry smoke, Windows desktop packaging,
host startup and presentation regressions.

Manual Windows/WebView2 verification should run at native display scaling
and again at 125%/150%:

- Open Midnight Manor without using the bonus. Verify responsive 60 FPS reel
  rendering on hardware capable of 60 FPS and record CPU/GPU/VRAM baseline.
- Open **practice**, move through several rooms, use a candle, then exit.
  Confirm no Godot iframe remains and gameplay input is immediately usable.
- Repeat practice entry/exit at least three times. Record cold-start delay
  per visit and memory/VRAM before, during and after each run.
- Trigger a real Door bonus or resume a previously journaled bonus. Confirm
  map legality, ghost clues, items, checkpoint cashout and correct receipt.
  The receipt must not issue a second reward.
- Test practice cashout/restart, real cashout, ghost catch, full exit and
  automatic resolution. Confirm the 3D renderer is released while each
  terminal result is displayed and after returning to the reels.
- Simulate missing/unsupported Godot Web assets. Ensure the retry action and
  C# recovery buttons work; no old 2D gameplay screen appears.
- After every bonus return, compare the reel FPS and 99th-percentile frame
  time to its pre-bonus baseline over at least 30 seconds. Repeat with
  sound, reduced-motion, different monitor scales and window sizes.

A clean CI run does not substitute for this actual hardware performance
verification. The prior reported roughly eight-FPS post-bonus behavior is
the regression to validate against.

## Local build (Windows)

```powershell
cd frontend
npm ci
npm test
npm run build
cd ..
./tools/build_manor_bonus_web.ps1 -OutputDirectory frontend/dist/manor3d
dotnet build BlackjackCasino.sln -c Release
dotnet test BlackjackCasino.sln -c Release --no-build
```

The Development workflow performs the full build, Windows smoke and package
validation. The generated Godot web files are packaged, not committed.
