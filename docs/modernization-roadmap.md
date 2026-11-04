# Modern casino frontend — staged migration roadmap

**Implementation status (2026-11-04):** The game director authorized the pilot and requested ongoing Development testing directly from the feature branch, without merging into main. Branch `feature/manor-modernization-phase-1` now includes the **Phase-3 engineering-complete premium pilot**: React/TypeScript/PixiJS/WebView2 renders modern reels and six bonus chamber scenes, while a restricted, revisioned C# bridge commits spins, bonus choices and recovery-safe settlements. As of 2026-11-04, the modern Manor is the only lobby-selectable Manor. The WPF implementation remains in source for deliberate rollback only. The frontend does not determine outcomes or directly modify wallets.

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

### M1 — Midnight Manor baseline audit and design (implemented; acceptance retained)

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
- Phase-1 history: WPF initially owned the primary Manor tile and a secondary **MODERN MANOR · PLAYTEST** action launched the alternative. This has been superseded: the primary tile opens modern Manor, with no secondary selection.
- CI, Development and Stable publisher workflows now install/test/build the frontend before dotnet build/publish. No updater protocol changes.
- Remaining before replacement: end-to-end WebView2 smoke with screenshots, locked transitive dependencies, measured performance baseline, complete state/action bridge, full feature screens, graphic assets, accessibility and tester acceptance.

## Phase-2 game integration and tester boundaries

- The gameplay command protocol is v2: `spin.paid` (whole-dollar play-money stake), `spin.free`, `run.move`, `run.candle`, `run.cashout`, `run.follow` and `run.auto`. Read-only commands are `client.ready`, `snapshot.request` and `ui.close`.
- Each mutation requires `expectedRevision` and a unique `requestId`; a committed `Revision` and `LastModernCommandId` are added to the existing JSON game state with backward-compatible defaults. The controller advances the revision for legacy WPF mutations too. Stale commands are rejected; the last acknowledged request can be replayed without repeating its payout.
- The C# controller persists the full wallet/profile/state aggregate through the existing write-ahead checkpoint. If storage fails after a possible journal write, the modern window disables further mutations and requires close/reopen so startup recovery can establish the committed state. Client timeouts refresh the snapshot; they never automatically resend a wager.
- Free Spins and Escape the Manor remain committed, manual-resume features. The modern interface does **not** auto-consume bonuses on startup. Existing paytables, payout bounds, progression and old saved states remain unchanged.
- The test suite covers invalid-origin/version/stake/room messages, deterministic room action integration, replay protection across controller reinstantiation, legacy paytable version and wallet accounting. Windows startup smoke checks that React gameplay controls appear but does not wager with the runner's profile.
- This phase does **not** yet implement reel motion, parallax effects, real symbol artwork, Manor-specific audio, user-facing German strings, the final gameplay rework or a measured 60 FPS guarantee. These belong to later work and player feedback.

### M3 first delivery — retained renderer and reel motion

- The optional Manor playtest keeps one PixiJS Application, scene, masks, and reusable cell graphics across committed C# snapshots. Resize adjusts the renderer without recreating the scene.
- Five columns visibly scroll and stop sequentially using display-only deterministic symbols before revealing the exact committed C# cells. Only acknowledged paid/free spin intents start this animation; reloads, room moves, and ordinary status refreshes do not spin.
- Normal/Fast/Turbo and reduced-motion/off settings change presentation duration only; no new RNG, wagers, payout math, schema changes, or extra round resolution.
- First locally drawn vector renditions of the fourteen symbols and a Gothic-luxury machine frame are in place. These are a **first presentation pass**, not final commissioned/high-resolution symbol art.
- Windows renderer screenshots, measured FPS/VRAM use, sound, complete bonus map, bilingual interface and full Phase-3 art production remain outstanding and must not be marked complete without testing.

### M3 second delivery — C#-authored payoff lines and connected Escape floorplan

- The existing v2 snapshot includes **read-only additive presentation data**: C#-computed winning line numbers, shapes and payouts; actual room passage pairs with authored key locks. Browser commands, wallet, save schema and RNG remain unchanged.
- The Pixi scene traces and rotates committed winning lines **after** the reels settle; reduced motion keeps a static, immediate result. A readable win breakdown can be expanded below the reels.
- Escape the Manor now uses a scrollable Gothic architectural floorplan, with actual corridor connections and distinguishable key-locked shortcuts; legal destination buttons continue to come only from the authoritative C# `Available` flag. Ghost coordinates are not included in the snapshot.
- Added TS shape-validation tests, mapped geometry checks and C# read-only snapshot assertions. This is a presentation milestone, not approval of new bonus odds or any change to the underlying Manor graph.
- High-resolution premium icon paintings, music, complete localization, runtime frame-time benchmarks and final player acceptance remain open.

### M3 third delivery — scalable offline illustration textures and reel-stop light

- Fourteen source-authored Victorian illustration compositions (rank monograms, candlestick, watch, raven, key, portrait, black rose, lady, haunted mirror, moon, door) are decoded locally as **600×320 SVG images**, not fetched from a CDN or stretched from a low-resolution thumbnail.
- The retained Pixi scene keeps the same twenty tile containers, shares fourteen loaded GPU textures and switches references during cosmetic spins. Illustration decode failures leave the previous Graphics symbols available and never block C# snapshots or wagers.
- After the committed reels stop, a subtle staggered glint tracks the five column stops; animation-off/reduced motion suppresses these transient effects. The win-line geometry and original C# outcome remain unchanged.
- The pure artwork suite confirms fourteen unique local compositions, dimensions and no external image dependencies. Final hand-painted/commissioned imagery, calibrated audio, tiered large-win sequences and monitored real-hardware frame timing remain separate acceptance items.

### M3 fourth delivery — consent-gated, local audio and reward presentation

- New Web Audio synthesis plays low-level original cues for accepted spins, sequential reel stops, payout tiers, doors, moons and C#-confirmed Manor actions. No remote sound assets or external services. Sound only initializes after a gameplay/settings user gesture, and audio failures cannot interrupt the settlement path.
- Separate sound-effects, optional subdued ambient hum, and bounded 0–100% volume controls. Audio runs only in the playtest view; it does not change WPF sound preferences or persistent save schemas.
- Distinct presentation-only win tiers use the *already committed* payout and saved stake (25× / 100× display thresholds). Actual credits and the legal result remain exactly as returned by C#.
- The reward ribbon is created only after an acknowledged, strictly newer C# revision and appears after visual reels have settled. Initial loads, ordinary refreshes and replayed responses do not trigger a celebration. Simultaneous Moon/Door grants retain both messages.
- Escape room movement, checkpoint, candle and cash-out use authoritative run messages and legal action acknowledgements, never predicted outcomes or invisible ghost coordinates. Reduced motion removes entry transitions and reel-stop accents.
- The run-ack and spin-ack callbacks are presentation-only; no additional gameplay command is issued. Tests cover sound envelope limits, volume edge cases, bonus combination presentation and payout-tier classification. Manual audio audition (different speakers/headsets and volume settings) remains necessary before final acceptance.

### M3 fifth delivery — discoverable, accessible bonus navigation

- Escape the Manor now presents C#-authorized adjacent room destinations as explicit keyboard-focusable buttons **ahead of** the scrollable floorplan. There is no inferred client pathfinding, no move prediction and no new command type.
- The architectural map recenters on the current saved room when the player enters it; every viewport width remains scrollable. Scrolling is presentation-only and does not advance the ghost.
- Ghost warnings derive exclusively from the public clue string: distant / approaching / close / candle-revealed. No hidden ghost coordinates, reward guarantees or invented risk percentages are exposed.
- Larger map names, larger click targets and a narrow-window single-column destination layout improve tester usability. All action buttons remain disabled while awaiting a committed response.
- Tests cover legal-only destinations, unknown clue handling and viewport-scroll bounds. Manual 125%/150%/200% Windows DPI and keyboard usability remain acceptance tasks; no unmeasured FPS improvement is claimed.

### M3 sixth delivery — native DPI scaling and opt-in frame diagnostics

- The retained Pixi renderer now passes the current 1–2× pixel ratio explicitly to `resize(width, height, resolution)` and listens for both host layout changes and monitor resolution media-query changes, so a move between different Windows display scales does not require recreating the game scene. Stage transforms continue to preserve the authored 960×500 logical coordinate space.
- A tester-enabled renderer diagnostics panel samples real Pixi ticker intervals and reports observed FPS estimate, mean/p95 frame interval, frames above 16.7 ms, CSS viewport and pixel ratio. It is disabled by default, purely local, low-frequency and does not alter the game or upload telemetry.
- With reduced motion enabled, the panel explicitly reports a stopped ticker rather than misleadingly claiming zero FPS. Samples ignore long background-tab pauses. This is **not** GPU profiling or evidence of stable 60 FPS; full session memory/VRAM and real multi-monitor hardware tests remain outstanding.
- Pure regression tests cover layouts at 1.25×, 1.5× and 2×, unusual viewport ratios, bounded pixel ratios and non-invented measurement calculations.

### M3 seventh delivery — authoritative bilingual presentation foundation

- The v2 browser snapshot adds read-only active language and authoritative paytable metadata (versioned symbol credits and all 20 line patterns). Existing mutation contracts and journal schemas do not change.
- The global persisted casino language preference flows from C# to React. The active old paytable is preserved during saved legacy free-spin cycles; otherwise the next paid-spin table is current.
- The browser-only layout preview carries a visibly non-playable table sample. Test coverage guards language whitelist, paytable shape/version, and preservation of the C# save aggregate.

### M3 seventh delivery — complete bilingual playtest controls and C#-authored help

- React controls, HUD, game actions, run choices, diagnostics, audio/display settings and machine labels are available in both German and English. C#'s saved language preference is the default; a local selector temporarily overrides it without touching saves.
- A keyboard-dismissable rules dialog renders the C#-authored *active* table version, all fourteen symbols, their 3/4/5 awards and the twenty actual payline patterns. It clearly distinguishes an in-progress old saved free-spin table from new cycles and documents no-retrigger and player-choice consequences.
- Canonical C# run messages and ghost clues are translated for German presentation only, with unknown text left untouched rather than inventing hidden events. Win/bonus acknowledgements translate on language change without replaying audio or mutating a journal.
- No new RNG, client-side payouts, bridge actions or save schema changes. Manual screenreader, native 150/200% DPI and pronunciation review remain outstanding.

- Windows startup smoke now verifies the real connected WebView2 host, opens the live C# paytable, checks all 14 symbols and 20 lines, takes English/German dialog screenshots, and switches the React HUD language without placing a wager.

### M3 final quality gate — implementation and automated evidence

- Sound-effects mute and optional atmospheric hum now obey the *actual global* saved SoundEnabled, MusicEnabled and MusicVolume preferences. The browser cannot override a desktop-wide mute. No new settings are written to the journal.
- Unmounting an in-flight Pixi spin cannot emit a false win announcement. Cabinet filigree and sixteen retained atmospheric particles add subtle depth without changing reel results, introducing external art or allocating new scene objects on each frame. Reduced motion freezes decorative particles.
- Cross-frontend restart tests cover an older saved active Manor run continued via modern room decisions, re-opening with the shared classic C# controller, one-time cash-out, revision-based replay and preservation of the old free-spin paytable.
- Windows startup smoke now takes a compact-window image and writes a real requestAnimationFrame/JS heap sample from the packaged WebView2 surface, explicitly distinguishing idle host timing from Pixi ticker metrics, GPU time and dedicated-hardware performance. The smoke never wagers on the runner's profile.
- Remaining human-owned acceptance: genuine long-session CPU/GPU/VRAM data at 125/150/200% on the tester's monitor(s), listening to the audio mix, route feel, and explicit permission to replace the fallback. These gates must remain open until actually checked.

### M3 scenic completion — six offline perspective rooms

- The interactive Escape bonus now includes six distinct scalable first-person SVG rooms (Entrance, Library, Ballroom, Corridor, Attic and Crypt), above the engine-authorized architectural route map.
- The room depiction and threat annotation derive strictly from the saved player room and public C# ghost clue. No hidden ghost coordinates, rewards, routing probabilities or new action requests are introduced.
- Atmospheric mist stops when animation is off or reduced motion is requested. The layouts share one React component and no remote or independently licensed image files.
- Unit tests verify all six themes, undefined-area fallback and safe unknown-clue behavior. Final subjective art acceptance remains the game director's decision.

## Phase 3 engineering closeout versus owner sign-off

**Implementation scope completed** on the long-lived Development branch: original local graphics, visually distinct bonus rooms, cosmetic win/bonus animation, engine-authored symbols/lines/map, sound respecting global preferences, English/German UI, keyboard-accessible help and moves, display-resize diagnostics, deterministic bridge/state tests and Windows smoke evidence.

**Only mark automated acceptance green when the relevant SHA has completed its actual CI, presentation-regression, frontend and Development Release checks.** Successful build/package verification does not certify real monitor VRAM/CPU, all audio devices, or the player's visual taste.

Human game-tester sign-off remains a prerequisite to switching the default Manor tile or retiring the WPF implementation. Phase 4 can reuse the pilot once this decision is made; no change of main branch, release guards, stable channel or long-lived Development branch is permitted without explicit approval.

The [Phase-3 sign-off matrix](manor-phase-3-signoff.md) tracks the exact boundary and manual test scenarios.

### M3 playtester findings — viewport, bonus presentation and delayed win

- The game director confirmed the smooth, stable Pixi renderer and significantly improved visual baseline, while identifying page-scroll, the bonus panel below the slot, overlapping symbol labels, premature win/lose information, and weak celebratory animation.
- The correction keeps a *separate last-visible snapshot* while the real C# spin is saved. The browser hides bankroll, new payout, free spins, triggers, feature and win-line counts until the final reel stops; the C# settlement and replay semantics are unchanged.
- Escape runs transition into the **same slot viewport**, with a first-person chamber, authorized room choices, persistent action bar and optional floorplan tab. Reduced motion suppresses the portal movement.
- Symbol captions sit below smaller illustrations, with additional vertical breathing room; tiered win, bonus and loss feedback is time-bounded, and displayed only after the real outcome is presented.
- A compact single-screen cabinet layout uses a folded settings drawer and fixed viewport grid at usable desktop sizes; on extremely small accessibility windows, nonessential information can scroll inside its own region rather than clipping controls.

### M3 owner follow-up: control scale, crisp symbols, understated loss and animated sconces

- Increased the bet field and main spin control while preserving the no-page-scroll cabinet layout, including a compact-height variant.
- SVG symbols are now decoded at 1200×640; Pixi textures are drawn at the source's original 300:160 aspect, centered rather than stretched into 142×66 rectangles. Name labels remain separate and lower in the card.
- Loss no longer creates an overlay over the slot. A short bilingual message temporarily replaces the secondary header alongside “The Haunted Five,” then returns to the 20-line label. Win and bonus overlays remain confined to the slot viewport.
- Both side candles were rebuilt as ornamental gold/wax sconces with layered, ticker-driven flame and glow motion. Two retained container transforms update on the existing ticker; reduced motion freezes the flames without per-frame shape allocations.
