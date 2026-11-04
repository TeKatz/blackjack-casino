# Midnight Manor 2.0 — Phase 3 technical closeout and player sign-off

**Status:** Phase-3 engineering scope implemented. CI and Development publisher must pass on the final branch HEAD; game-director acceptance remains open.
**Branch:** `feature/manor-modernization-phase-1`. **Release channel:** existing `development`. **Main and WPF baseline:** unchanged.

## Testable deliverables

| Acceptance point | Automated proof | Owner check |
| --- | --- | --- |
| C# owns RNG, wagers, return, bonuses and wallet | Service/protocol, engine and journal tests | Confirm outcomes and bankroll in both frontends |
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
- The original WPF Manor and existing Development updater remain usable as rollback mechanisms.

## Remaining game-director checks (not automatable here)

Install and play from the Development channel, test audio on real speakers/headset and display scaling at 100/125/150/200%. Run for 30 minutes on the target gaming PC, record CPU/GPU/RAM/VRAM and Pixi diagnostics, and try all saved-feature reopen paths. Confirm subjective readability of six rooms, symbols and bonus announcements. If issues arise, keep Phase 3 open for fixes; otherwise approve the pilot's visual/audio quality separately.

**Do not merge to main, retarget the updater, delete the WPF implementation or start the final shell migration without explicit authorization.**
