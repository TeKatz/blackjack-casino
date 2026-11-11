# Midnight Descent — Godot roguelite

Midnight Descent is an offline 2D action roguelite launched by The Midnight Club desktop application. It runs in a separate Godot 4.5.1 process (Compatibility renderer); neither casino balances nor payment data are passed to Godot. Run combat, rewards, hazards, seeded maps and rooms are simulated in `games/midnight-descent/run_model.gd`. Godot is a build dependency, not a prerequisite for players.

## Runs and the Threshold Tavern

Every new expedition begins in the **Threshold Tavern**, outside the dungeon graph. The portal remains usable immediately. Optional preparation does not advance the dungeon, award free casino currency, or require repeated dialogue.

- **Tavern keeper:** Choose **one** of three seed-selected contracts per run. Defeat 12 enemies, clear two combat rooms without losing HP, hold 30 unspent shards while on the first floor, or cast 12 special attacks. Each completed contract automatically grants one existing relic and applies its stat effect once. Contract progress is displayed during combat, and the reward is retained until the run ends. Declining a contract costs nothing.
- **Fate forge:** Choose **one** of three seed-selected, high-impact modifications drawn from four possible builds. **Echo Anvil** duplicates primary attacks as delayed spectral shots (85% damage), increasing special cooldown by 40%. **Stormheart** adds five projectiles to every special, reducing maximum HP by 30%. **Void Stride** halves dash cooldown and grants a damaging shockwave with each dash, reducing maximum HP by 25%. **Iron Aegis** blocks the first two hits of each combat room and releases a shockwave on break, reducing movement speed by 25%.
- Each modification is mutually exclusive for the run and is discarded on restart. The forge service rooms deeper in the dungeon retain their existing shard-costed upgrades.
- All five barstools can be occupied by holding upward movement against a stool for one second; releasing and then moving again stands up. The two card-playing guests, tavern props and portal remain visual/environmental features; a playable card wager or alternate portal mode is **not** included in this milestone.
- The tavern keeper and forge have keyboard-E proximity prompts, bilingual menus and Escape exit. The Godot run scene uses a fixed 1280×720 world layout with window scaling.

## Dungeon systems

The current branching dungeon features three floors, 26 generated room nodes per floor, combat waves, elites, bosses, service rooms, seeded layouts, hazards, relics and character loadouts. Running a dungeon does not require selecting a tavern contract or a forge blessing. Persistent character progression is separate from temporary run-state benefits.

## Relic synergy and boss phase overhaul

Six two-relic combat transformations are available. They are derived from the currently owned relic IDs and reset with the expedition:

- **Toxic Jackpot** (`venom` + `crit`): critical hits on already poisoned enemies cause a poisonous area burst.
- **Wildfire Circuit** (`ember` + `storm`): hits on burning enemies chain increased fire-lightning damage to up to three foes.
- **Permafrost Burst** (`frost` + `shatter`): chilled enemies shatter into more projectiles on death and freeze nearby enemies.
- **Inferno Rush** (`dashfire` + `nova`): dash ends in a damaging explosion that applies burning.
- **Ricochet Storm** (`pierce` + `bounce`): enemy hits split projectiles into two limited child ricochets that cannot split again.
- **Soul Requiem** (`soulstorm` + `leech`): kills unleash six soul bolts and restore extra HP.

Relic selection cards highlight a **SYNERGY READY** completion in green, with its gameplay effect. Uncompleted pieces are marked as combo pieces. If at least one half is owned, the reward generator can place a matching half into one of the three offers, without making selection compulsory. The paused-run Relics window lists active and partial combinations. A newly completed combination triggers a named animated announcement and sound.

All three bosses have mandatory phases at **68%** and **32%** HP. A large hit cannot cross a phase threshold, and a short transformation period prevents one-shot phase skipping. Telegraphs precede their attack patterns:

- **Vault Keeper:** aimed fan volleys, then sweeping danger lanes and radial volleys, then a blast-zone and denser crossfire.
- **Root King:** targeted roots, expanding snare clusters, and radial root volleys.
- **Hollow King:** rotating spectral volleys and increasingly dangerous rune lanes, culminating in crossed seal patterns.

The pixel-world sprites gain phase-dependent halos. Boss phase, charge and release have distinct effects and audio. The HUD shows **PHASE 1/3–3/3**. Each class dash generates a distinct burst, ongoing trail and landing effect; Nova, Cinder Steps, Void Stride and Inferno Rush display their bursts at the actual activation/impact positions. The gameplay model remains authoritative for collisions and damage.

No additional room objectives or floors were introduced in this iteration.

## Build and tests

From a Windows checkout run `pwsh ./tools/build_midnight_descent.ps1 -OutputDirectory artifacts/app/MidnightDescent`. The script downloads the pinned Godot 4.5.1 editor and Windows export template, imports the game, runs smoke tests and exports an executable and PCK alongside the casino application.

Godot CI imports and parses the project and runs menu/model and gameplay-scene smoke tests. The model checks four forge effects, contract selection and payouts, synergy readiness and several combat effects, boss phase thresholds/telegraphs, dash effects, reset behavior, seating collisions, room traversal and combat progression. Visual validation on Windows, including font sizes and longer German strings, is still required.

## Controls

WASD / arrow keys move. In the tavern **E** interacts with the keeper, forge or portal and **Esc** closes an interaction or pauses. In combat the mouse aims; left mouse is the main attack, right mouse the special attack, Space dashes, Q activates the equipped item, and E opens a cleared gate. A relic can be selected with 1–3 or the mouse.
