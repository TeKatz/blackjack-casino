# Modern casino frontend — staged migration roadmap

**Implementation status (2026-11-04):** The game director authorized the pilot after the green main consolidation. Branch `feature/manor-modernization-phase-1` begins the migration with a **read-only, optional modern Manor preview**. The existing WPF Manor remains the only playable path. React/TypeScript/PixiJS/WebView2 are introduced for visual inspection, but no JavaScript action can place a bet, change money or resolve a feature.

## Why this direction

The current Windows UI is WPF. Gameplay engines are primarily in `BlackjackCasino.Core`, and the WPF desktop project provides orchestration, persistence and visual state. The planned frontend uses:

- **React + TypeScript + HTML/CSS** for navigation, game HUD, responsive text and controls;
- **PixiJS** for GPU-oriented 2D game scenes, reels, symbols, lighting and animation;
- **C#/.NET** as the authority for game rules, payouts, progression, persistent receipts and balance;
- **WebView2** hosted initially inside the WPF shell, later inside a smaller dedicated .NET desktop host if tests support the transition;
- local assets and offline operation, with no player web server requirement.

Do **not** rewrite existing engines merely to port the UI. Use adapters and explicit domain/application contracts. Reject outcomes and wallet changes sent by the frontend.

## Milestones and gates

### M0 — Repository and workflow readiness (this preparation)

- Refresh user-facing README, architecture and active branch documentation.
- Add repo-level agent instructions, contribution/PR process, and checks preventing broken documentation and mismatched Development branch guards.
- Make no gameplay or updater protocol changes; bring validated cumulative work into `main`, then move the Development branch source to `main`.

**Exit:** reviewed consolidation PR, CI green, actual Development manifest published from `main`, save compatibility preserved.

### M1 — Midnight Manor baseline audit and design (in progress)

- After branch consolidation and Jigsaw tester acceptance, inventory `ManorController`, `ManorPersistence`, `ManorWindow`, the Manor Core engine, effects, assets, saves, current RTP/versioning and CI evidence.
- Capture current screen states, visual defects, frame-time/performance baseline and save recovery tests.
- Propose any changed rules/odds as a separate reviewed game-design change, never incidental to rendering.

**Exit:** baseline screenshots, behavior matrix, freeze of save compatibility, approved visual/gameplay specification.

### M2 — Bridge and rendering vertical slice (read-only preview foundation)

- Introduce a versioned C# application interface: open/close, game snapshot, bet, spin, feature choice, sound/presentation preferences, recover.
- Add typed request/response/event schemas and a WebView2 adapter. Enforce origin, version, command validation and once-only settlement.
- Build React HUD and a PixiJS Manor reel scene loading exclusively from local assets.

**Initial sub-milestone:** the prototype shows committed `ManorSessionState` through a strictly allow-listed, origin-checked WebView2 protocol. Navigation has a separate optional lobby action; the legacy Manor remains primary. The snapshot is read-only. **Full M2 exit remains:** existing Core resolves real paid spins from validated commands, the frontend renders results and pending bonus choices, and recovery is tested.

### M3 — Manor premium presentation and mechanics

- Replace scaled WPF text with sharp DOM UI; implement responsive layout, themed art, effects, reel animation, accessible controls, sound and animation settings.
- Upgrade Escape the Manor visual presentation and feature clarity. Expand gameplay only after math, backward compatibility and statistics implications are tested.
- Record FPS/frame-time percentiles, CPU/GPU and RAM before/after on named reference hardware.

**Exit:** gameplay parity or explicitly approved rule changes, save/restart and recovery tests, visual acceptance and no major performance regression.

### M4 — Pilot release and adoption

- Integrate lobby navigation, progression/challenge/album updates, German/English strings, no duplicate audio and crash-safe pending features.
- Keep WPF Manor as rollback until the modern experience passes Windows end-to-end and actual updater tests.
- Keep Development publishing from `main`; coordinate pilot landing and rollback without repinning temporary branches.

**Exit:** tester-approved Development build, documented fallback path and measured results.

### M5 — Incremental casino-wide rollout

- Migrate common lobby/navigation and progression UX, then appropriate table/slot modes and remaining Lounge/Empire/adventure views.
- Replace existing WPF surfaces individually; do not execute a one-shot rewrite.
- Only retire WPF when all modes, profile actions, save migrations and updater restart paths pass acceptance.

**Exit:** one modern shell, unchanged player entitlements/history, reliable install/update/rollback.

## Non-negotiable invariants

1. No real money, accounts, advertising, telemetry or network requirement.
2. Core/engine RNG and settlement are authoritative; visuals never change outcomes.
3. Player bankroll and rewards are exactly-once across crashes/retries.
4. Persistent game state loads from older versions without silently losing data.
5. The entire frontend is local and can run offline.
6. Reduced animation and audio settings are respected.
7. Test evidence must distinguish measured improvement from visual aspirations.

See [AGENTS.md](../AGENTS.md), [architecture](architecture.md), [Midnight Manor design](midnight-manor-design.md), [Manor acceptance](midnight-manor-acceptance.md) and [Development release delivery](development-update-delivery.md).

## Phase-1 implementation and safety boundary

- Added `frontend/` with TypeScript, React, PixiJS, Vite and Vitest. Top-level dependencies are exact-version pinned; **transitive lockfile still required** before full rollout.
- WPF WebView2 preview maps a local folder to `https://midnight-manor.local/index.html`. Navigation and messages are origin-checked. Allowed command types: `client.ready`, `snapshot.request`, `ui.close`.
- Only read-only `ManorSessionState` values are sent to JavaScript. Money is rendered as formatted strings, never settled in the browser. Empty saves display fixed, non-random symbols.
- The existing Midnight Manor lobby action remains fully playable; a secondary button opens the preview only when compiled frontend files exist.
- CI, Development and Stable publisher workflows now install/test/build the frontend before dotnet build/publish. No updater protocol changes.
- Remaining before replacement: end-to-end WebView2 smoke with screenshots, locked transitive dependencies, measured performance baseline, complete state/action bridge, full feature screens, graphic assets, accessibility and tester acceptance.
