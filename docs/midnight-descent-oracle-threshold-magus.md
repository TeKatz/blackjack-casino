# Midnight Descent — Oracle and Threshold Magus

The two new run-scoped Shard sinks do not alter Boss Souls, the permanent skill tree, character loadouts or the tavern's risk/reward Forge.

## Oracle (dungeon map)
The route map remains fogged except for visited and immediately reachable nodes. A compact Oracle action at the bottom of the map buys one additional, currently unrevealed future column of room identities. The reveal persists while on that floor but **does not unlock any route**. Cost starts at **12 Shards** and rises by **8 Shards per consultation** (12, 20, 28, etc.). The map refreshes immediately after purchase, updating available Shards, known icons, names, threat numerals and tooltips. No purchase is possible during combat, when funds are insufficient or when there are no hidden future columns. All revelations reset when descending to the next stage.

## Threshold Magus (boss approach)
A fully illustrated, lit mage stands in the **entrance-side corridor, before the closing boss shutter** of all three stage boss rooms. Walk close and press **E** to talk. Gameplay pauses while the shop is open. ESC or the departure button closes it. Once the player crosses into the arena and the gate starts closing, the mage can no longer be reached or used.

Four **once-per-boss** offers with explicitly balanced per-floor prices:
- **Healing essence** (35 / 45 / 55 Shards): +1 Q heal charge, only if holding the heal item, max 3.
- **Arcane charges** (50 / 62 / 74): your next two special attacks during this boss battle recharge **85% faster**.
- **Protective seal** (65 / 78 / 92): absorbs the next hit during the boss fight.
- **Magus' focus** (80 / 96 / 112): +15% special damage while fighting this boss.

Each purchased offering updates the shop immediately and cannot be bought twice. Discounts and boosts are temporary and reset on descending to the next floor; they do not grant persistent unlocks.

### Validation
Model smoke tests verify that Oracle purchases cannot alter route choices and do not leak across stages, and that each mage is reachable before the actual gate, charges and wards are applied and consumed correctly, and duplicate/late purchases fail. Scene smoke tests exercise the actual map purchase button and the displayed magus UI, artwork and before-gate interaction across all three stages.


### Difficulty and presentation pass (November 2026)
The prior encounter-density upgrade created 70+ kills on stage 1, generating ~250 Shards from a run that should demand difficult pre-boss purchasing choices. The currency economy now awards **1 Shard for an ordinary kill** (2 for elite/champion, +1 at danger III, and 10 for a boss). Bounty and class relic payouts retain their effects at lower magnitudes. Special-combat clear awards are reduced from 14–23 to 3–10 Shards and the incidental event/altar cashouts are also smaller. At stage I, the four mage offers together cost **230 Shards**; the goal is usually to afford one premium preparation or two less expensive ones, not the complete stock.

The Oracle is now a **full-width, bounded footer card under the map** rather than an unbounded HBox: left-aligned heading and readable text, with a fixed-width action button on the right. A live scene test checks real Controls' laid-out widths/heights and ensures the card stays within the viewport, not just that map nodes fit.

The Bounty room exit no longer scales its noisy four-frame animated banner into the little gate. Instead a dedicated engraved iron warrant/target reticle uses a gentle pulse in the open state. The large central trophy banner remains.

To address doubled/jittering GUI glyphs in 1920×1080, all bundled game UI fonts use consistent anti-aliasing, light hinting and **2× oversampling**; large display shadows are removed and custom canvas text is drawn at pixel-rounded positions. Godot's Control snapping is enabled; the moving world camera is not pixel-snapped.
