# Midnight Merge — game and presentation architecture

Midnight Merge is an offline casino restoration/merge game with an authoritative C# engine and a responsive React/PixiJS interface. This document describes the *current* implementation; the historical first WPF prototype was replaced during the November 2026 frontend migration.

## Player loop

The casino lobby opens `frontend/midnight-merge.html` locally inside the existing Windows game window. The workshop contains 42 cells (7 columns × 6 rows). The chip case spawns tier-I casino chips into the first empty cell. Drag a chip to any free position to move it or onto a second identical chip to merge. Select a source and then a destination for keyboard and pointer click access. Moving retains the item GUID; merging consumes two GUIDs and generates a new result GUID.

The original bar supply cart unlocks at 100 renovation points and produces the eight-tier drinks chain. Four additional six-tier material chains unlock as the restoration expands: decorative furnishings at 2,500 points, plants at 5,600, theater props at 21,800, and observatory equipment at 38,800. The original six lobby orders and eight bar orders retain their IDs, rewards and requirements; 32 new area-specific recipes bring the catalog to 46 repeatable orders. Successful deliveries consume the *exact* matching items, grant renovation points and replenish the corresponding request with a new instance GUID. Future-room deliveries are locked until their room opens; existing completed delivery receipts cannot be replayed. There is no stamina, real-money wager, timer, cooldown or transfer of casino bankroll as a cost.

## Ten-room renovation path

The casino restoration progresses through ten contiguous rooms. Each area has four named visual stages, with a room's last milestone also unlocking the next room. These thresholds are **cumulative lifetime renovation points** (not spendable currency).

| Room | Start | Intermediate upgrades | Final upgrade |
| --- | ---: | ---: | ---: |
| 1. Lobby | 0 | 20, 50 | 100 |
| 2. Bar | 100 | 500, 1,200 | 2,500 |
| 3. Gaming floor | 2,500 | 3,200, 4,200 | 5,600 |
| 4. Conservatory | 5,600 | 6,500, 8,000 | 9,900 |
| 5. Private lounge | 9,900 | 11,100, 12,800 | 15,200 |
| 6. Art gallery | 15,200 | 16,700, 19,000 | 21,800 |
| 7. Midnight theater | 21,800 | 23,700, 26,300 | 29,500 |
| 8. Sky terrace | 29,500 | 31,700, 35,000 | 38,800 |
| 9. VIP suites | 38,800 | 41,500, 45,300 | 49,800 |
| 10. Rooftop observatory | 49,800 | 53,200, 57,800 | 63,600 |

No legacy milestone threshold or save version changed. The room view is read-only; previewing previously unlocked rooms, expanding the panorama or disabling animations never updates renovation state, rewards or area selection in C#. Existing save files acquire new order instances and producer records during normalization, while retaining existing item/order GUIDs, points and durable receipts.

Global player XP and season XP still come from deliveries and newly reached milestones; milestone bankroll rewards are calculated in C# and credited through the shared native wallet. The final observatory stage completes the current ten-room restoration arc. More rooms can be appended without a save-schema redesign.

## Gameplay and persistence authority

- `BlackjackCasino.Core/MidnightMerge`: `MergeCatalog`, `MergeRules`, `MergeBoard`, `MergeOrders`, `MergeProgression`, typed models and `MergePlayerProgression`. None depend on WPF.
- `MergeBoard`: validates all moves, merges, spawns and order completions under a lock. Each successful candidate is saved **before** replacing the current board. Invalid inputs or failed disk writes do not advance the authoritative board. Completed-order receipts prevent duplicate rewards, including across normal process restarts.
- `MergeGameState.Normalize`: preserves stable GUIDs and player progress, restores invalid cell collisions, keeps current producer and order definitions and earned milestones, and rejects saves from unsupported future schema versions.
- `AppPreferences.MidnightMerge`: the existing schema-v2 atomic player-state document stores producers, items, active orders, receipts, renovation points, completed milestones, lifetime statistics and monotonic board revision. All previously saved states remain compatible.
- `PlayerProgressionController.ProcessMidnightMerge`: synchronizes the existing profile rewards with separate rewarded-order and milestone ledgers; if profile persistence fails after board persistence, a later sync can reconcile the missing award without replaying an order.

## React/PixiJS view and bridge

The new `MainWindow.MidnightMerge` adapter hosts `ModernMergeView` in WebView2 under the existing local virtual host. The client only submits `spawn`, `drop`, `deliver` and per-game presentation toggles. `ModernMergeProtocol` checks exact host and path, version, request and session GUID, producer/target/order syntax and the current C# board revision. `ModernMergeSession` caches request receipts outside the browser, so repeated messages cannot create extra items or payouts. A browser restart reuses the same in-memory C# board and command ledger; a normal return to the lobby closes that session, but the durable board remains saved.

`ModernMergeSnapshot` projects a read-only versioned board, catalog, producer eligibility, order requirements, area milestones, translations and XP feedback. React displays semantic text/buttons, click/drop highlighting, delivery lists and scalable SVG art for chips, drinks and the four new supply categories. A versioned snapshot exposes all ten room summaries alongside the active room. The panorama is drawn from themed layered vector environments that add structures, furniture and lighting at each C# milestone; the ten-stop overview permits read-only revisits of unlocked rooms. On desktop the restored panorama, ten-room roadmap, 7×6 workbench, compact supply sources, reward footer and premium stat cards use a fixed-height single-screen grid. Only the delivery-order list scrolls; narrow windows use a readable adaptive fallback rather than clipping the controls. The expandable panorama changes SVG preserve-aspect-ratio from cropped `slice` to full-scene `meet` inside an overlaid lightbox without altering the board. Finished rooms use architectural depth planes, floor reflections and lightweight room-specific ambient effects (including theater beams, garden movement, polished glints and starfields); all ambient effects are optional. Unchanged panorama SVGs and item artwork are memoized across C# board snapshots to avoid regenerating their painting trees on spawn/merge. PixiJS still renders the low-cost decorative ambient particle field. Reduced-motion and per-mode animation preferences disable scene effects. Sound cues reuse native `MergeSoundSynthesis` / `CasinoAudio`. Sound/animation preferences follow the existing per-area preference store. Global language updates from `Localization` refresh the browser without touching game data.

## Verification and limits

`MidnightMergeTests`, `MidnightMergeProgressionTests`, `MidnightMergeUpgradeProgressTests`, `MidnightMergeTenAreasTests`, `ModernMergeBridgeTests`, `frontend/tests/mergeBridge.test.ts` and `frontend/src/merge/RestorationScene.test.tsx` cover game math, persistence, duplicate prevention and browser schema. `tools/StartupSmoke/MidnightMergeSmoke.cs` runs against the packaged React view to verify the original two producers, a native tier merge, order completion, the first renovation milestone, click movement, exact reopen/roundtrip persistence, EN/DE resource updates, compact board sizing and forced WebView2 failure/recovery.

The `RestorationScene` frontend tests cover all ten scenes, 40 stage combinations, completed-state atmospheres, reduced-animation artwork, complete panorama framing and stable render dependencies. The packaged Windows smoke checks the compact layout and expanded panorama at 1080×720. Manual Windows device acceptance of the ten panoramas, visual upgrade transitions, expanded-room layout, perceptual smoothness, long sessions, different DPI and audio mix remains separate from automated correctness. The new room staging and supply recipes are covered by catalog/contract tests but still require long-session gameplay balancing and device artwork review. The previous gameplay-only WPF `MidnightMergeView` is retired, though the emergency lobby artwork is retained until the native fallback lobby itself is removed. The shared WPF desktop host remains in use for other unmigrated games.
