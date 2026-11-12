# Midnight Descent — Oracle and Threshold Magus

The two new run-scoped Shard sinks do not alter Boss Souls, the permanent skill tree, character loadouts or the tavern's risk/reward Forge.

## Oracle (dungeon map)
The route map remains fogged except for visited and immediately reachable nodes. A compact Oracle action at the bottom of the map buys one additional, currently unrevealed future column of room identities. The reveal persists while on that floor but **does not unlock any route**. Cost starts at **12 Shards** and rises by **8 Shards per consultation** (12, 20, 28, etc.). The map refreshes immediately after purchase, updating available Shards, known icons, names, threat numerals and tooltips. No purchase is possible during combat, when funds are insufficient or when there are no hidden future columns. All revelations reset when descending to the next stage.

## Threshold Magus (boss approach)
A fully illustrated, lit mage stands in the **entrance-side corridor, before the closing boss shutter** of all three stage boss rooms. Walk close and press **E** to talk. Gameplay pauses while the shop is open. ESC or the departure button closes it. Once the player crosses into the arena and the gate starts closing, the mage can no longer be reached or used.

Four **once-per-boss** offers (base price +6 Shards per later stage):
- **Healing essence** (20 / 26 / 32 Shards): +1 Q heal charge, only if holding the heal item, max 3.
- **Arcane charges** (25 / 31 / 37): your next two special attacks during this boss battle recharge **85% faster**.
- **Protective seal** (35 / 41 / 47): absorbs the next hit during the boss fight.
- **Magus' focus** (45 / 51 / 57): +15% special damage while fighting this boss.

Each purchased offering updates the shop immediately and cannot be bought twice. Discounts and boosts are temporary and reset on descending to the next floor; they do not grant persistent unlocks.

### Validation
Model smoke tests verify that Oracle purchases cannot alter route choices and do not leak across stages, and that each mage is reachable before the actual gate, charges and wards are applied and consumed correctly, and duplicate/late purchases fail. Scene smoke tests exercise the actual map purchase button and the displayed magus UI, artwork and before-gate interaction across all three stages.
