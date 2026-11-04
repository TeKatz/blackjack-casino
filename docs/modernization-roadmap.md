# Modern casino frontend — staged migration roadmap

**Implementation status (2026-11-04):** The game director authorized the pilot and requested ongoing Development testing directly from the feature branch, without merging into main. Branch `feature/manor-modernization-phase-1` now includes the **Phase-2 playable alternative**: React/TypeScript/PixiJS/WebView2 renders the Manor, and a restricted, revisioned C# bridge commits paid spins, free spins and room decisions. The WPF Manor remains the primary path and fallback. The frontend does not determine outcomes or directly modify wallets.

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
- Keep the cumulative main baseline unchanged, and publish ongoing test builds from the dedicated Manor feature branch without merges.

**Exit:** successful CI and Development manifest published from the Manor feature branch, no unintended merge, saves preserved.

### M1 — Midnight Manor baseline audit and design (in progress)

- After branch consolidation and Jigsaw tester acceptance, inventory `ManorController`, `ManorPersistence`, `ManorWindow`, the Manor Core engine, effects, assets, saves, current RTP/versioning and CI evidence.
- Capture current screen states, visual defects, frame-time/performance baseline and save recovery tests.
- Propose any changed rules/odds as a separate reviewed game-design change, never incidental to rendering.

**Exit:** baseline screenshots, behavior matrix, freeze of save compatibility, approved visual/gameplay specification.

### M2 — Bridge and rendering vertical slice (Phase-2 playable slice)

- Introduce a versioned C# application interface: open/close, game snapshot, bet, spin, feature choice, sound/presentation preferences, recover.
- Add typed request/response/event schemas and a WebView2 adapter. Enforce origin, version, command validation and once-only settlement.
- Build React HUD and a PixiJS Manor reel scene loading exclusively from local assets.

**Implemented:** The optional modern surface accepts paid spin, free spin, room move, candle, cash out, follow policy and auto-resolve intents. Commands require a durable revision; the C# service commits once to the existing Manor checkpoint, exposes the authoritative snapshot, and rejects stale replay. The browser may not provide outcomes or payouts. C# and TypeScript contract tests plus Windows smoke and checkpoint recovery tests validate this slice. **Still pending tester acceptance:** full animation flow, premium art, sound, multilingual copy and performance baselines.

### M3 — Manor premium presentation and mechanics

- Replace scaled WPF text with sharp DOM UI; implement responsive layout, themed art, effects, reel animation, accessible controls, sound and animation settings.
- Upgrade Escape the Manor visual presentation and feature clarity. Expand gameplay only after math, backward compatibility and statistics implications are tested.
- Record FPS/frame-time percentiles, CPU/GPU and RAM before/after on named reference hardware.

**Exit:** gameplay parity or explicitly approved rule changes, save/restart and recovery tests, visual acceptance and no major performance regression.

### M4 — Pilot release and adoption

- Integrate lobby navigation, progression/challenge/album updates, German/English strings, no duplicate audio and crash-safe pending features.
- Keep WPF Manor as rollback until the modern experience passes Windows end-to-end and actual updater tests.
- Keep Development on the **single long-lived Manor migration branch** until the user explicitly authorizes a merge or channel change.

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
- Phase 1 sent only read-only snapshots. Phase 2 additionally accepts strictly validated user *intents*. Money is formatted and settled only in C#, with no browser balance or result authority. Empty saves use fixed, non-random symbols.
- The existing Midnight Manor primary action remains fully playable; a secondary **MODERN MANOR · PLAYTEST** action opens the Phase-2 alternative only when compiled frontend files exist.
- CI, Development and Stable publisher workflows now install/test/build the frontend before dotnet build/publish. No updater protocol changes.
- Remaining before replacement: end-to-end WebView2 smoke with screenshots, locked transitive dependencies, measured performance baseline, complete state/action bridge, full feature screens, graphic assets, accessibility and tester acceptance.

## Phase-2 game integration and tester boundaries

- The gameplay command protocol is v2: `spin.paid` (whole-dollar play-money stake), `spin.free`, `run.move`, `run.candle`, `run.cashout`, `run.follow` and `run.auto`. Read-only commands are `client.ready`, `snapshot.request` and `ui.close`.
- Each mutation requires `expectedRevision` and a unique `requestId`; a committed `Revision` and `LastModernCommandId` are added to the existing JSON game state with backward-compatible defaults. The controller advances the revision for legacy WPF mutations too. Stale commands are rejected; the last acknowledged request can be replayed without repeating its payout.
- The C# controller persists the full wallet/profile/state aggregate through the existing write-ahead checkpoint. If storage fails after a possible journal write, the modern window disables further mutations and requires close/reopen so startup recovery can establish the committed state. Client timeouts refresh the snapshot; they never automatically resend a wager.
- Free Spins and Escape the Manor remain committed, manual-resume features. The modern interface does **not** auto-consume bonuses on startup. Existing paytables, payout bounds, progression and old saved states remain unchanged.
- The test suite covers invalid-origin/version/stake/room messages, deterministic room action integration, replay protection across controller reinstantiation, legacy paytable version and wallet accounting. Windows startup smoke checks that React gameplay controls appear but does not wager with the runner's profile.
- This phase does **not** yet implement reel motion, parallax effects, real symbol artwork, Manor-specific audio, user-facing German strings, the final gameplay rework or a measured 60 FPS guarantee. These belong to later work and player feedback.
