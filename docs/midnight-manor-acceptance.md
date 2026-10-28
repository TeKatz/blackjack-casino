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
