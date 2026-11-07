# Migration regression repairs

The Development presentation uses React, DOM/SVG artwork and PixiJS. C# retains game rules, round settlement, durable saves and authoritative rewards. The changes below target the migrated presentation; the detached investigation board uses a WebView2 host with the same React board and native command session.

## Implemented behavior

| Area | Behavior |
| --- | --- |
| Lobby | Midnight Merge uses an ordinary tile below Midnight Reels at both grid breakpoints. Game previews reuse mode artwork and renderers; Nine Lives retains its cat illustration. A divider follows Empire. Empire credits use K/M/B/T notation. |
| News | A focus-isolated React dialog shows the installed build's bundled release notes once per process startup. News is the first navigation entry. The updater remains a separate panel. |
| Identity | Header and profile show the equipped portrait and frame. Native title colors and gradients apply to previews and equipped titles. Prestige has a larger metallic badge. |
| Shop | Card previews use the actual card-skin palette. Dice use the native DiceAppearance values; frames use catalog variants, distinct outlines, glow and motion. |
| Vault | Tier-specific metal doors and icons replace the generic emblem. The authoritative payout receipt triggers the wheel, door and reward entrance animation. |
| Collections | Denser headers, pack choices and card galleries reduce scrolling. The card inspector uses the browser top layer, traps focus and makes underlying controls inert; background scrolling is locked until dismissal. |
| Season Pass | Every Midnight Shift and Velvet Eclipse tier grants $100 in play money, including cosmetic tiers. Already completed tiers are not paid a second time. Existing IDs, XP thresholds and other rewards remain intact. |
| Blackjack | The table fills the available viewport. Hand headings have more room, new cards animate in, Deal/Hit use a softer cue, +1,000/+2,500 chips are available, and an available Split has a highlighted border. |
| Midnight Dice | A staggered toss replaces the old repeated shake. Die numbering above the dice is removed. Score/scratch confirms with a sound only after native persistence succeeds. |
| Reels | Slot cue synthesis is lazy and prewarmed away from the opening path. The Spin control has explicit line height and room for its text. The cabinet fits supported desktop viewports. |
| Neon Vault / Nine Lives | Spin animations preserve sharp symbols. Height-constrained boards fill the viewport, with neon cabinet lighting and rooftop ambience respectively. |
| Manor | Stake selection uses preset chips with a read-only amount. Normal spins have a longer visual timeline, Pixi labels use higher text resolution, and the Spin control has consistent sizing. Existing atmospheric layers remain active. |
| Merge | Board dimensions follow the measured workspace height. Cells and help stay inside their panels; Supply Orders retain their own scrolling. |
| Lounge | Compact game previews fit a six-game overview. Main casino music pauses while the Lounge owns the soundtrack. Temporary save locks use stable controls with aria-disabled state, avoiding repeated dimming and focus loss; native command guards still serialize actions. Missing acknowledgements refresh state without retrying a move. |
| Sudoku | Erase sends the selected cell index. The automatic hint sentinel is never sent as an erase target. Invalid selection does not dispatch a command. |
| Memory | Each absolute-positioned preview face is contained by its own preview card, preventing a fruit face from covering the cabinet. |
| Solitaire | Tables fit both viewport dimensions. The club ornament sits below the playing area. Long card fans compress within the table instead of extending beyond it. |
| Jigsaw | Complementary curved masks replace rectangular cuts. The shuffled tray supports direct drag/drop, selection, placed-piece movement, snap feedback and sound. Reference is off by default. Menu and board are compact. Native placement, grouping and completion remain authoritative. |
| Tales | Scene transitions, a faceted animated D20, authored event hints, animated feedback and relic highlights enrich the existing game. Board drags retain their local position until the saved layout arrives; Fit View reveals distant clues. A separate React board window can coexist with the story. |
| Motion | Ambient effects and small page transitions respect system reduced motion and application preferences. |

## Validation and acceptance

Local validation uses Node 24, Chromium with software rendering and .NET SDK 10.0.100 on Linux. The native Windows host is cross-compiled; Linux cannot execute WebView2/Windows interaction or device audio acceptance.

- Vitest covers transport contracts, Sudoku edit dispatch and complementary puzzle geometry/tray ordering.
- .NET tests cover the two archived reward tracks, exactly-once tier progression and detached-board access without revealing undiscovered clues.
- StartupSmoke adds real WebView2 assertions for startup News, contained Memory previews and Sudoku erase/unlock. Existing game, save and payout smokes remain active.
- Browser checks exercise News dismissal, equal Merge tiles, selected-cell erase, Memory start dispatch, deck-preview bounds, puzzle layout and desktop viewport fit. Browser mocks validate presentation and transport shape, not native gameplay completion.
- The slot-audio change removes eager synthesis from construction. A Windows before/after loading-time measurement remains an acceptance check; no FPS or loading-speed gain is asserted without measurement.

Windows acceptance should include a fresh launch and News reopening, Sudoku erase then continued play, fruit-deck Memory start/flip/resume, full Vault opening, all Solitaire variants with long fans, 1,000-piece Jigsaw placement, Tales rolls and both investigation views, audio ownership on repeated mode changes, 125–200% display scaling and reduced motion. Visual feel, sound quality and hardware performance require device review.

## Rollback and save compatibility

The repair can be reverted as a source change. It introduces no save schema migration, new game RNG, payout override or replacement IDs. Existing saves and native completion receipts remain authoritative. The new $100 archived-season rewards apply only when a tier is newly completed; rollback does not revoke previously granted play money.

## P2 Batch 1 — lobby navigation, Collections and Vault (2026-11-07)

The migrated lobby uses semantic, inline gold-stroke navigation SVGs instead of deriving menu icons from atlas positions. The shared styling stays emerald/gold, while Daily Login is visually separated in indigo and champagne tones. The Midnight Dice artwork no longer receives a duplicate dice pseudo-layer. The hover presentation now uses an isolated, non-scaling preview layer with a covered art/info seam.

The collector archive keeps the actual cropped card illustrations and inspector, but the browse grid uses smaller cards and the 16-theme collection picker grows inside the page's single scroll surface. The old nested Collections scrollbar is deliberately removed; pack-opening animations and their persisted receipts are untouched in this batch.

Casino Vault has a larger rendered safe with layered metal, hinges, rivets, a tier-specific accent and visible bronze/silver/gold/platinum/diamond milestones. Locked and ready actions retain their original state constraints. No contribution rate, payout formula, vault progression, rewarded pack, wallet or persisted state changed. The larger facade is CSS/DOM, not a new 3D engine.

**Acceptance:** inspect 1080p, 1440p, compact and DPI-scaled Windows presentations; hover over the first four tiles without an art/info color stripe; confirm icon semantics after navigating and locale switching; verify that the theme picker does not scroll independently; inspect cards and the full card detail dialog; test locked/ready/pending/reward Vault states, including reduced motion. Automated frontend, .NET and real WebView2 CI smoke remain required; subjective quality depends on user device review.
