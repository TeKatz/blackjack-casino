# Midnight Manor acceptance and delivery

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

## Remaining human Windows checks before merge

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

Human acceptance remains required for merge and Stable release. The user subsequently requested delivery through the in-app Development updater, so the feature branch is now explicitly opted into `development-release.yml`. That publisher builds/tests/runs the Windows WPF smoke before publishing. `main` remains in its push list; `release.yml` is unchanged. Asset names and the existing `development` manifest URL are retained. Version 0.7.0-dev.N is newer than both 0.6.5 and 0.6.5-dev.N, so even existing clients can detect it. See [Development update delivery](development-update-delivery.md). After an authorized merge, verify the three release assets, SHA-256, manifest URL and final main target before deleting branches.

## Modern Manor Phase 3 evidence and human gate

The optional modern interface now has independent coverage for the authoritative C# line table, active legacy-paytable display, bilingual HUD/help, frame/DPI math, six SVG room vignettes, local Web Audio and global sound/mute constraints. New controller tests take a **stored active run** from the classic shared journal through a modern room choice and cash-out, reopen it through the same classic C# controller and verify no duplicate reward. Another test resumes a v1 free-spin table and confirms replay idempotency after restart.

The Windows StartupSmoke tool opens the **packaged** WebView2 modern UI, verifies that its C# snapshot is connected, opens the real 14-symbol/20-line payout dialog in English and German, captures screenshots, verifies the compact host's responsive reel aspect ratio and records `modern-manor-idle-frame-sample.json`. The sample is explicitly **idle WebView2 requestAnimationFrame timing and optional JS heap only**, not measured animated Pixi frames, GPU timestamps or long-session VRAM. No tests trigger paid spins against the runner profile. Existing ManorSmoke and financial recovery tests continue to cover older WPF flows.

**Human checks still mandatory before adoption / Phase-4 default switch:**

1. Install the latest passing Development package using the actual updater from an older installation. Confirm old profile balance, Coins, season and wardrobe remain unchanged.
2. Listen to reel stops, awards, free spins and optional ambient hum on speakers and headset, with global sound/music toggled off and volume near zero. The engine must remain playable when audio is unavailable.
3. Play at 100%, 125%, 150% and 200% scaling; move the window between real monitors, check all original bonus chambers, visible font clipping, keyboard focus and route controls.
4. Leave the modern view active for at least 30 minutes with multiple paid spins and bonuses. Note PC/GPU model, resolution, average/p95 frame intervals from the optional Pixi diagnostics panel, Task Manager RAM/VRAM, and any cumulative increase.
5. Start a free-spin cycle and an Escape run, exit to the lobby, reopen Modern Manor and classic Manor, close/restart Windows, then verify exact bankroll, final reward and completed XP with no extra wager. Include candle/key/checkpoint/ghost warnings.
6. Compare animation enabled, fast/turbo, reduced-motion, the six room perspectives and the old Manor fallback; decide whether the visual quality and audio mix meet the desired product standard.

**Sign-off state:** Engineering implementation and CI are independently verifiable. Owner hardware, perception, installation and default-switch authorization remain **open** until explicitly reported. This document does not authorize merging, deleting the WPF fallback or switching the Development channel.
