# Midnight Merge foundation

Phase 1 is a playable offline vertical slice, entered directly from the casino lobby. It intentionally ends at a restored lobby; it does not start the resort or other Phase 2 systems.

## Player loop

Open the chip case to spawn a tier I chip in the first free cell. Drag a chip onto a free cell to move it, or onto an identical chip to merge. A click on the source followed by a click on the destination provides the same operations, including keyboard-activated buttons. Click the selected chip again to deselect it. Six original vector chip tiers differ in color, marking and stack height.

Supply orders consume the exact listed chips and award only internal renovation points. Requests for higher tiers remain available below the first three orders, preventing a board full of terminal chips from becoming stuck. Each delivery replaces that order with a new instance of the same curated request. No energy, cooldown, wager, bankroll transfer, Casino Vault contribution or payout exists.

The lobby changes at 0 / 20 / 50 / 100 points: dusty lobby, first cleanup, restored lights, lobby ready. Points continue accumulating after restoration; this is prototype content, not a balanced long-term economy.

## Boundaries

- `BlackjackCasino.Core/MidnightMerge`: typed catalog, lines and item/producer/order instances, board/rules, order consumption, receipts and area/milestone progression. No WPF dependency.
- `MergeBoard`: validates and serializes all mutations under a gate, clones snapshots, saves a candidate before publishing it, and preserves the previous state on persistence failure. A merge consumes two IDs and creates one new ID; moving preserves the instance ID. Stale merge IDs cannot duplicate items.
- `MergeGameState.Normalize`: schema version, stable valid IDs, unique cells, conflict relocation, default producers, active orders and earned milestones. Normalization of a valid save is stable. Future schemas are rejected by the engine.
- `MainWindow.MidnightMerge`: composition and host/persistence wiring only. Uses the existing embedded shell host; no second visible game window.
- `MidnightMergeView` / artwork: stable board cells, drag/drop plus click fallback, selection, matching-target highlights, invalid feedback, spawn/move/merge pulses and travel, order rewards and milestones. Existing global animation and sound preferences govern feedback.
- `MergeSoundSynthesis`: eight original PCM cues, played through the existing casino mixer and released when the view closes.
- `MergeStrings.resx` / `.de.resx`: complete English/German key pairs, live language changes and the normal persistent language preference. Mode registry resource keys are resolved at render time so a German startup can switch back to English.

`AppPreferences.MidnightMerge` persists the board, producer counters, active order instances, completion receipts, points, unlocked milestones and revision in the existing atomic player-state snapshot. Host updates include the current Empire and bankroll snapshots without transferring anything between them and Merge. Initial normalized IDs are saved before opening the view. Every successful operation saves immediately, so closing the view or application requires no deferred Merge transaction.

## Verification

Core tests cover valid/invalid movement and merging, different lines and tiers, max tier, identity/count invariants, simultaneous spawn/merge/order attempts, full-board recovery, exact consumption, duplicate completion after reload, normalization, stable IDs, disk save/load, rollback/retry after a failed save, milestone uniqueness and distinct bounded audio cues.

`tools/StartupSmoke/MidnightMergeSmoke.cs` runs inside the existing Windows acceptance executable. It opens the actual embedded view, spawns and merges, moves a chip, completes orders, checks a milestone, leaves and reopens, compares the full serialized state, performs a disk roundtrip through the real player-state writer/loader, switches to German, checks the minimum window size, restores English, and renders enabled animations. PNGs and `merge-result.txt` are uploaded with `desktop-startup-smoke` by CI and Development Release.

## Delivery

The branch `feature/midnight-merge-foundation` starts at main commit `5915b5c`. Development Release pins both push trigger and job guard to this branch. The established mutable `development` release and in-app manifest URL are reused. Publication runs build, tests and Windows smoke before packaging. The final verification downloads the published manifest/package/checksum and checks source SHA, version, channel, package size and SHA-256. Do not merge this branch automatically.
