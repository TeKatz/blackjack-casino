# Midnight Descent — Combat pressure & healing balance patch

## Core balancing changes

This patch intentionally preserves all playable loadouts, relic IDs, synergies, the route graph, and the three bosses.

**Healing budget**: The default Q healing item has **two charges per floor**. It still uses its normal cooldown and heals the established amount (18 HP plus alchemy ranks), but pressing Q with no charges or at full HP spends nothing. Charges refill only at the start of the next dungeon floor (not on room transitions). Other active items keep their existing behavior. The bottom ability slot and pause menu display the healing reserve.

**Enemies**: Movement and action cadence scale modestly with floor and encounter threat. Ranged fighters strafe, reposition if the player closes in, and lead moving targets slightly. Melee enemies flank when approaching. Existing champion affixes now have a singular, visibly telegraphed area-pressure move on a cooldown; other elite enemies are not all converted into area casters. The opening floor-zero room remains at baseline pressure.

**Bosses**: Each of the three bosses retains its identity and phase HP gates, but phase-two/three attack timing is shorter, targeting is slightly predictive, and phase-three attacks add staggered lane, root, or seal pressure. Every new ground attack has a clear warning; no instant unavoidable damage is intended.

## Acceptance checks

The Godot headless smoke suite exercises:
- finite Q healing, cooldown and full-HP behavior, and replenishment on descent;
- the existence and warning time of champion pressure patterns;
- the final boss's third-phase extra tell;
- actual zero-HP transition into the defeat state;
- existing character skill/relic/boss and dungeon traversal tests.

Still verify feel with live controller/mouse gameplay in all three stages, especially high-damage synergy builds and all eight characters; mechanical smoke tests do not measure subjective difficulty or death frequency.
