# Midnight Manor 2.0 — Phase 3 technical closeout and player sign-off

**Historical phase-three sign-off (2026-11-04):** This document preserves its original test and branch context. The code was later integrated to `main` via PR #36; current Development is sourced from `develop`. Statements about the feature branch or unchanged `main` below reflect the earlier snapshot, not current policy. See [branching policy](branching-and-release-policy.md).

**Status (2026-11-04):** Phase-3 engineering complete; the modern Manor has replaced the legacy WPF game as the single lobby entry in Development. The replacement passed CI and Development package verification on commit `2114b05` (version `0.10.1-dev.1009`). Each newer commit requires its own checks; visual, sound and long-session hardware testing remain ongoing.
**Branch:** `feature/manor-modernization-phase-1`. **Release channel:** existing `development`. **Main:** unchanged. **WPF:** source-only rollback, no lobby option.

## Testable deliverables

| Acceptance point | Automated proof | Manual verification |
| --- | --- | --- |
| C# owns RNG, wagers, return, bonuses and wallet | Service/protocol, engine and journal tests | Confirm outcomes and bankroll after a modern session and restart |
| Playable premium slot | Retained Pixi scene, 14 local vector illustrations, winning-line tests, WebView2 screenshot | Subjective art and animation quality |
| Escape the Manor | 6 SVG chamber views, real map/passages, legal-only room buttons, saved-run continuation | Real decisions, clue readability and route satisfaction |
| Haunted Night and payouts | Old/current paytables, 20 C# paylines, no-retrigger tests | Pause/reopen while free spins remain |
| Language and accessibility | German/English parity, rules dialog, WebView2 snapshots, reduced-motion tests | Real keyboard/screenreader and monitor scaling |
| Sound | Local synthesis constraints, desktop global sound/music preference contract | Listen on own audio device with master mute/volume |
| Render and memory | Observed Pixi ticker diagnostics, compact-window WebView2 smoke, idle RAF/heap evidence | 30-minute GPU/VRAM/CPU/heap trend and DPI monitor switch |
| Stable release and migration | Development package hash, version and manifest verification, C# cross-frontend recovery | Install actual Development update and verify existing user profile |

## Engineering completion gates

- All current-sha workflows pass: **CI**, **Modern Manor frontend**, **Casino presentation regression**, **Repository contract**, **Development Release**. Never infer pass from the previous SHA.
- Development tag target exactly equals the tested feature branch SHA; ZIP, updater manifest and checksum asset match.
- StartupSmoke evidence contains `modern-manor-rules-en.png`, `modern-manor-rules-de.png`, `modern-manor-bilingual-hud.png` or the latest phase-3 PNG, `modern-manor-compact-host.png` and `modern-manor-idle-frame-sample.json`.
- No new RNG/paytable/payout calculation, wager side effect, external media dependency or save-format migration.
- The original WPF Manor remains in source for a controlled rollback build, not as an in-app fallback; the Development updater remains unchanged.

## Remaining manual checks (not automatable here)

Install and play from the Development channel, test audio on real speakers/headset and display scaling at 100/125/150/200%. Run for 30 minutes on the target gaming PC, record CPU/GPU/RAM/VRAM and Pixi diagnostics, and try all saved-feature reopen paths. Confirm subjective readability of six rooms, symbols and bonus announcements. If issues arise, track targeted fixes; otherwise record visual/audio quality as manually verified.

**Scope:** The Manor lobby replacement is included in Development. This does not merge to `main`, retarget release channels, remove legacy WPF source or migrate the rest of the casino shell.
