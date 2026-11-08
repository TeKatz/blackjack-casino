# Midnight Merge — game and presentation architecture

Midnight Merge is an offline casino restoration/merge game with an authoritative C# engine and a responsive React/PixiJS interface. This document describes the *current* implementation; the historical first WPF prototype was replaced during the November 2026 frontend migration.

## Player loop

The casino lobby opens `frontend/midnight-merge.html` locally inside the existing Windows game window. The workshop contains 42 cells (7 columns × 6 rows). The chip case spawns tier-I casino chips into the first empty cell. Drag a chip to any free position to move it or onto a second identical chip to merge. Select a source and then a destination for keyboard and pointer click access. Moving retains the item GUID; merging consumes two GUIDs and generates a new result GUID.

The bar supply cart unlocks at 100 renovation points and produces a separate eight-tier drinks chain. There are six lobby orders and eight bar orders, including higher tiers, with stable requirement IDs. Each successful delivery consumes the *exact* matching items, grants renovation points, and immediately replenishes the request with a fresh instance ID. There is no stamina, real-money wager, timer, cooldown or transfer of casino bankroll as a cost.

The lobby restores at 0 / 20 / 50 / 100 points. Bar restoration begins at 100 and progresses at 100 / 500 / 1,200 / 2,500 points. Both areas display progress, stage artwork, the next target and translated milestone names. Global XP and season XP come from deliveries and milestones; milestone bank rewards are calculated in C# and credited through the shared native wallet.

## Gameplay and persistence authority

- `BlackjackCasino.Core/MidnightMerge`: `MergeCatalog`, `MergeRules`, `MergeBoard`, `MergeOrders`, `MergeProgression`, typed models and `MergePlayerProgression`. None depend on WPF.
- `MergeBoard`: validates all moves, merges, spawns and order completions under a lock. Each successful candidate is saved **before** replacing the current board. Invalid inputs or failed disk writes do not advance the authoritative board. Completed-order receipts prevent duplicate rewards, including across normal process restarts.
- `MergeGameState.Normalize`: preserves stable GUIDs and player progress, restores invalid cell collisions, keeps current producer and order definitions and earned milestones, and rejects saves from unsupported future schema versions.
- `AppPreferences.MidnightMerge`: the existing schema-v2 atomic player-state document stores producers, items, active orders, receipts, renovation points, completed milestones, lifetime statistics and monotonic board revision. All previously saved states remain compatible.
- `PlayerProgressionController.ProcessMidnightMerge`: synchronizes the existing profile rewards with separate rewarded-order and milestone ledgers; if profile persistence fails after board persistence, a later sync can reconcile the missing award without replaying an order.

## React/PixiJS view and bridge

The new `MainWindow.MidnightMerge` adapter hosts `ModernMergeView` in WebView2 under the existing local virtual host. The client only submits `spawn`, `drop`, `deliver` and per-game presentation toggles. `ModernMergeProtocol` checks exact host and path, version, request and session GUID, producer/target/order syntax and the current C# board revision. `ModernMergeSession` caches request receipts outside the browser, so repeated messages cannot create extra items or payouts. A browser restart reuses the same in-memory C# board and command ledger; a normal return to the lobby closes that session, but the durable board remains saved.

`ModernMergeSnapshot` projects a read-only versioned board, catalog, producer eligibility, order requirements, area milestones, translations and XP feedback. React displays semantic text/buttons, optional click/drop highlighting, complete order lists and scalable SVG chip/drink icons. The restoration panorama changes with the authoritative active area and stage; PixiJS renders a low-cost decorative particle field only. Sound cues reuse native `MergeSoundSynthesis` / `CasinoAudio`. Sound/animation preferences follow the existing per-area preference store. Global language updates from `Localization` refresh the browser without touching game data.

## Verification and limits

`MidnightMergeTests`, `MidnightMergeProgressionTests`, `MidnightMergeUpgradeProgressTests`, `ModernMergeBridgeTests` and `frontend/tests/mergeBridge.test.ts` cover game math, persistence, duplicate prevention and browser schema. `tools/StartupSmoke/MidnightMergeSmoke.cs` runs against the packaged React view to verify two producers, a native tier merge, order completion, the first renovation milestone, click movement, exact reopen/roundtrip persistence, EN/DE resource updates, compact board sizing and forced WebView2 failure/recovery.

Manual Windows device acceptance of perceptual smoothness, long sessions, different DPI and audio mix remains separate from automated correctness. The previous gameplay-only WPF `MidnightMergeView` is retired, though the emergency lobby artwork is retained until the native fallback lobby itself is removed. The shared WPF desktop host remains in use for other unmigrated games.
