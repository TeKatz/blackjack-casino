# Midnight Manor acceptance and delivery

> The sections referring to six SVG/WPF room renderings document historical automated presentation coverage. The live React slot now uses the 17-room Godot 3D bonus exclusively. For current post-bonus performance, load/unload, payout and recovery acceptance, see [3D Manor bonus](manor-godot-bonus.md).

## Automated coverage

- Baseline before edits: 72 existing tests passed on main `378a896`.
- Pure engine tests: published paytable, Wild substitution, feature separation/spacing, all stake/RNG boundaries, bankroll conservation, all 2/3/4/5 Door tiers and 3/4/5 Moon tiers, free-spin stake/2× modifier/no retriggers, no overlapping paid cycles, completion XP deduplication, old-profile defaults and serialized continuation.
- Manor tests: connected reachable graph, actual route/cash-out effect, deterministic pursuit, visible distance bands, key locks, candles, charm edge movement, checkpoints, capture, finite turns, exact interactive/reference-policy parity for every seed and tier, scaled two-Door rewards, legacy winning checkpoints/free-spin continuation without table-version fields, legacy runs missing the scale field, and simultaneous two-Door/three-Moon awards.
- Exact math enumerates all 14^5 line symbol combinations plus all 16 Manor seeds. A seeded 100,000-paid-cycle simulation checks exposure, financial conservation and feature frequencies.
- Write-ahead recovery tests interrupt projections and verify idempotent replay; invalid checkpoints do not overwrite valid balances. Audio tests inspect every generated WAV's format, peak, RMS and transient bounds.
- Shared play-control tests cover finite/unlimited auto, cancellation while paused, no overlapping spins, stop conditions and pre-wager bankroll floor.

## Actual Windows WPF smoke

`dotnet run --project tools/ManorSmoke -c Release -- artifacts/manor-windows-smoke`

CI runs this on `windows-latest` after the full suite. It creates an isolated profile/checkpoint (never the user's save), opens the real Manor window, exercises actual routed button/key events, renders all six room variants, captures the cabinet at normal/minimum dimensions plus a 2× export, and drives normal/Fast/Turbo spins, animations-off Free Spins, interactive bonus pause, closing/resuming a run, optional auto-resolution, Auto cancellation and closing during a committed animation. The artifact `manor-windows-runtime-evidence` contains PNGs and pass/failure evidence. The playtest regression sequence also checks both dropdown templates and actual rendered brightness while idle/disabled in-spin, opens the real popup, captures spinning light streaks and two moments of bright winning traces/particles, and enters the real two-Door smaller run. Audio is muted in the automated runner.

A 2× bitmap export verifies rendering resolution; it does **not** prove actual monitor DPI switching. Automated routed input does not replace a human keyboard/usability check.

## Manual Windows checks before Stable rollout

- Listen to effects and long-session ambience at the actual saved music volume; verify soft mix, no clipping, readable stops and anticipation ducking.
- Play on a normal monitor and at 125/150/200% Windows scaling; move between monitors and check text, map routes, reel alignment and focus.
- Play normal Blackjack and Midnight Reels before/after a Manor visit, including a pending Manor feature kept across a Blackjack bankroll change.
- Try different manual routes, key route, reveal candle, charm, checkpoint/catch and Gate; verify decisions and ghost cues feel understandable.
- Close the application during each feature phase, restart and resume; verify balance/XP once, saved speed/interaction/auto settings, and no autoplay startup.
- Exercise mouse/keyboard, sound/music off, animations off, each auto stop condition and manual auto cancellation.

## Scope and deliberate math decisions

No pick-three feature, premade paid asset pack, real-money currency or monetization. All art is original WPF geometry; all audio is synthesized. This revision uses a fixed graph with deterministic seeded ghost pace. Free Spins use a session-wide Haunted Night 2× line multiplier; no sticky symbols or retriggers are advertised. The auto policy is available through the interactive recommendation button; other player strategies may change RTP. The conservative cycle limit is a safety bound, not a promised achievable jackpot.

Midnight Reels is left byte-for-byte intact. Shared speed/auto infrastructure is reusable but applied to Manor only in this change to avoid altering the classic machine's established behavior.

## Release gate

Modern Manor is part of the integrated `main` baseline; new in-app Development builds publish from `develop`. That workflow builds, tests and runs Windows startup and Manor smoke tests before publishing. The manifest URL and updater protocol remain unchanged. This section records earlier WPF acceptance work; the current source pin and SemVer policy are documented in [Development update delivery](development-update-delivery.md). A Stable rollout remains a separate gate requiring package, checksum, installed-updater and player-save verification.

## Modern Manor Phase 3 evidence and human gate

The modern Manor interface has independent coverage for the authoritative C# line table, active legacy-paytable display, bilingual HUD/help, frame/DPI math, six SVG room vignettes, local Web Audio and global sound/mute constraints. New controller tests take a **stored active run** from the classic shared journal through a modern room choice and cash-out, reopen it through the same classic C# controller and verify no duplicate reward. Another test resumes a v1 free-spin table and confirms replay idempotency after restart.

The Windows StartupSmoke tool opens the **packaged** WebView2 modern UI, verifies that its C# snapshot is connected, opens the real 14-symbol/20-line payout dialog in English and German, captures screenshots, verifies the compact host's responsive reel aspect ratio and records `modern-manor-idle-frame-sample.json`. The sample is explicitly **idle WebView2 requestAnimationFrame timing and optional JS heap only**, not measured animated Pixi frames, GPU timestamps or long-session VRAM. No tests trigger paid spins against the runner profile. Existing ManorSmoke and financial recovery tests continue to cover older WPF flows.

**Manual checks for the current Development release and any later Stable rollout:**

1. Install the latest passing Development package using the actual updater from an older installation. Confirm old profile balance, Coins, season and wardrobe remain unchanged.
2. Listen to reel stops, awards, free spins and optional ambient hum on speakers and headset, with global sound/music toggled off and volume near zero. The engine must remain playable when audio is unavailable.
3. Play at 100%, 125%, 150% and 200% scaling; move the window between real monitors, check all original bonus chambers, visible font clipping, keyboard focus and route controls.
4. Leave the modern view active for at least 30 minutes with multiple paid spins and bonuses. Note PC/GPU model, resolution, average/p95 frame intervals from the optional Pixi diagnostics panel, Task Manager RAM/VRAM, and any cumulative increase.
5. Start a free-spin cycle and an Escape run, exit to the lobby, reopen Manor through its single modern entry, close/restart Windows, then verify exact bankroll, final reward and completed XP with no extra wager. Include candle/key/checkpoint/ghost warnings.
6. Compare animation enabled, fast/turbo, reduced-motion and all six room perspectives; evaluate visual quality and audio mix. The old WPF interface is a source-level rollback only.

**Rollout state:** Modern Manor is already the sole selectable Manor game in Development, and its replacement commit passed CI and package validation. Manual hardware, audio, long-session performance and older-installation upgrade checks remain separate from this engineering validation. Neither a merge to `main` nor deletion of the WPF rollback source is part of the Development rollout.


## Current Development presentation and controls

The single Manor lobby tile opens the modern presentation. The previous WPF cabinet remains in source for controlled rollback, not as a selectable game. The Manor-specific play-money stake range is **$1–$5,000**, with quick choices **$50 / $100 / $250 / $1,000 / $2,500 / $5,000**. Automatic paid spins are offered in batches of **10 / 25 / 50 / 100**, processed sequentially through the normal C# command journal. A bonus, saved free spins, low bankroll, uncertain response or Stop command prevents the next paid spin. No precommitted batch of wagers is stored.

The modern host plays the existing original Manor soundtrack through the casino's Windows audio mixer. WebView sound effects and ambient visuals are separate, so the real music/sound switches remain authoritative. A native branded cover remains visible while WebView2 initializes, and only reveals the illustrated Pixi stage after complete readiness. The Escape bonus adds a perspective pull-in and animated chamber transitions while keeping every room decision and reward authorized by C#.

### Manual visual and sound checks for this pass

Check first-open cold starts, the soundtrack with music on/off and varying master volume, all six stake buttons, stopping an auto-run during a spin, auto-run stopping on either bonus, the six rooms and their entrance/transition feedback, reduced-motion behavior, and compact 960×680/1080×720 layouts. Confirm no page scrolling or clipped controls, and measure actual CPU/GPU/RAM/VRAM on the target Windows PC. Automated smoke and contract tests do not substitute for these observations.
