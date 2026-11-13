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

## Class-exclusive relic pools — November 2026

Four run-scoped loot pools are now built on top of the 48 shared relics. Each class has **six exclusive relics** (24 new items total). Both loadouts of the same class share their exclusive pool. Each three-card reward includes at least one card from the current class; the other offers still draw from shared and class items with the existing rarity rules. No other class' relic can appear. The combination-completion offer mechanic still applies. Rarity is rare for the first two of each class' set and epic for its other four.

| Class | Relics and purpose | Three new two-item synergies |
| --- | --- | --- |
| Warden | Thorn Bastion (retaliation), Oathstone (periodic protective shield), Rally Banner (room-clear heal), Sentinel Lance (piercing special), Living Plate (HP/armor), Shieldbreaker (dash shockwave) | Thorn Fortress, Unbroken Sanctuary, Phalanx Rush |
| Dealer | Marked Deck (fourth main attack card fan), Sleight of Hand (extra wall ricochet), Jinx Coin (curse mark), Smoke Joker (dash smoke damage), Death Ace (fifth special bonus card), House Treasury (extra shards) | Royal Flush, Double Bluff, House Jackpot |
| Arcanist | Primordial Ember (burning hits), Ash Catalyst (burning-death blasts), Glacier Sigil (special chill), Crystal Lens (chill damage), Astral Wisps (orbiting auto-attack sprites), Arcane Dynamo (third-special pulse) | Inferno Chain, Crystal Cataclysm, Astral Convergence |
| Soulbinder | Grim Scythe (extra souls on kills), Soul Brand (spend a soul for cursed projectile), Wraith Lantern (kill spirit shot), Grave Choir (fifth-kill pulse), Blood Vow (souls on damage), Last Breath (low-HP special bonus) | Dark Requiem, Legion of Echoes, Undying Pact |

The **12 class-specific synergies** are defined in `content.gd` alongside the six original shared synergies, and are calculated by the same `run_model.gd` preview/activation API. Their effects change combat (extra projectiles, special mechanics, explosions, damage pulses, shield windows, status spreading), and show on the reward card when the next item completes a pair. Class relic cards are additionally labeled with their owner in an appropriate accent color. Activation still creates the named synergy toast.

### Artwork and sound

`art/pixel/class_relics.svg` is a new **25-cell, 48×48-pixel icon strip** built with the existing dark framed, crisp-edged 2D pixel-art language. Cell zero redesigns Shadow Steps; cells 1–24 map to item indices 48–71. `ui_theme.gd` routes those indices to the new atlas while preserving the old shared icons (0–39) and crown icons (40–47). Gameplay and UI smoke tests inspect atlas dimensions and index ranges.

The combat renderer draws matching class-tinted area pulses, flying card/lance fans, Astral Wisp companions and Black Sun hit sparks. There are 19 new original **synthesized** sound cues in `sound_bank.gd` for the new relic effects and Black Sun impacts.

### Existing-system corrections

- The Dungeon Map uses 92×52 pixel cards in columns spaced 110 px apart, compact two-line danger labels, and clipped text inside its buttons; this fixes overlapping route nodes.
- The Vault Keeper, Root King and Hollow King each have **25% more max HP** than before this change. Mandatory health-gated phases remain intact.
- Black Sun copies now contribute an **additional orbiting, damaging sphere each**, up to eight separate visual orbits. Further stacks continue scaling impact damage beyond that visual limit. The rendered orbit positions and hit calculations both use the run's elapsed time.

These changes introduce no new room types or floors.

## One-way routes, stronger enemies and boss souls

### Irreversible route decisions

The Dungeon Map presents an eight-column branching path plus an initial fight and a boss. A player can now travel **only to a directly linked, unvisited room in a later column**. Previously visited rooms, previous columns and sideways branches in the same column are ineligible, even when their graph edges exist. One reward/service room per branching column is therefore all that can be taken in a run. Route hints explicitly say **No backtracking**, and the existing boss-gate fight/elite requirements still use their guaranteed combat columns. Route checks verify that none of the generated seeds and floors leak a backward or sibling option.

### Increased enemy danger

All enemy-origin damage uses a global **×1.40** multiplier before armor, layered onto the existing floor-depth multiplier. This applies to melee attacks, hostile projectiles, enemy-created spore/volatile hazards and boss telegraphed attacks (including root and royal runes). Ordinary environmental hazards (vault spikes, rotating ambient crown runes and ambient spore pools) retain their old damage. The new Resolve skill provides modest late-game mitigation.

### Boss souls and expansion of the permanent constellation

**Boss souls** are a new persistent currency completely separate from the Soulbinder's in-combat souls and the old expedition **essence**. Every boss defeated in a run grants one soul; a run with two conquered bosses earns two even if it ends in defeat. A boss kill is credited as soon as the boss dies, including when other enemies remain. Souls are committed exactly once upon victory, defeat, or manually abandoning a run after defeating a boss. The save file `user://descent_progress.cfg` now includes `profile/boss_souls`. Older saves load with zero souls and **retain all previously unlocked upgrade ranks and loadouts**, without retroactive refunds or removing previously purchased progress.

Permanent skill-tree upgrades now cost **boss souls instead of essence**. The price of successive ranks is **1, 2, 4, 7, 11** souls, cumulative **25** souls for a complete five-rank path or **14** for a four-rank path. Essence remains available for unlocking the eight character loadouts. The constellation now has **eight** paths: the original Vitality, Alchemy, Power, Arcane Mastery, Agility, Fortune, plus:

- **Resolve / Entschlossenheit:** 3% less enemy damage per rank, up to four ranks (12%). Environmental hazards are not mitigated by Resolve.
- **Battle Recovery / Kampferholung:** +1 HP after clearing each combat room per rank, up to four ranks (+4 HP).

The eight-path tree scrolls vertically to retain the original menu dimensions. Buttons and tooltips show the boss-soul costs for the next rank, while the profile and result overlays separately display the boss-soul balance, earned souls, and existing essence. Boss kills also show a run-soul counter, a distinctive synthesized pickup cue and an acquisition toast. Neither temporary per-run combat souls nor paid casino balances are affected.

### Regression coverage

The automated model tests reject backward or same-column route travel for all three floors, check the enemy vs. environmental damage delta (and Resolve mitigation), verify Recovery healing, test soul payouts on both victory and defeat, prevent duplicate settlement, assert escalating purchase prices and check that prior permanent ranks are retained. Menu and gameplay smoke tests build the expanded constellation and relevant overlays.

## Seven bespoke dungeon chambers (November 2026)

All seven are additional, deterministic room kinds in the existing three-floor / 26-node graph; no new floor and no shared casino currency. They appear in the seeded branch selection while the original mandatory combat/elite columns and boss unlock counts remain valid. The new route map shows custom glyphs and colors. Backtracking remains prohibited. Each completed combat chamber counts toward the regular combat requirement.

### Four new objective-based combats

| Room | Victory, danger, reward |
| --- | --- |
| **Ritual Chamber** (`ritual`) | Destroy three destructible ritual anchors before the 36-second seal completes. Defenders join every ~6.5 seconds. Failure spawns a shielded guardian and removes the premium success reward; the player must then defeat the remaining threats. Successful interruption grants +20 shards and one premium three-card selection. |
| **Survival Chamber** (`survival`) | Survive 30 seconds against periodic reinforcements and telegraphed danger circles; kills alone do not end the room. Completion immediately clears remaining enemies, counts as a combat victory, and offers a premium relic choice. |
| **Bounty Hunt** (`bounty`) | Eliminate the prominently marked, high-HP frenzied champion, not all escorts. Additional enemies may spawn while the target survives. Killing the champion completes the objective, removes escorts, grants +23 shards and a premium choice. The champion **does not** grant a permanent boss soul. |
| **Unstable Chamber** (`unstable`) | A normal eliminate-the-enemies objective under three 4-second-cycle crystal explosions. The blasts hurt **both sides**, allowing the player to weaponize positioning. Clear to gain bonus shards and a premium reward. |

Special combats have dedicated objective/timer readouts. The normal game attacks, hazards and ordinary enemy sprites remain compatible with the new rules. Ritual anchors are special destructible enemies with their own art, no regular kill/shard payout; the bounty champion is tracked separately. Every special win pays only once.

### Three new run-build decision rooms

| Room | Decisions |
| --- | --- |
| **Blood Altar** (`blood_altar`) | Give up 20% of current maximum HP for a three-card premium choice guaranteed to contain at least one epic, or sacrifice 10% maximum HP for +12 main and +9 special damage, or decline for four shards. Max HP changes apply only to the active run. |
| **Forgotten Forge** (`forgotten_forge`) | Spend 12 shards to **temper one** of up to three distinct carried relics; gain an additional functional stack of that relic, including its on-pick stat effect. May decline freely. This is distinct from the entrance forge and the preexisting Royal Forge. |
| **Archive of the Forgotten** (`archive`) | Pick one of up to three carried relics to discard, remove its on-pick flat modifiers, and choose a replacement from three fresh offers; the discarded relic cannot immediately reappear. Synergy status recalculates against the resulting inventory. A player with no relics can leave instead of being trapped. |

Services are one-shot decisions. Reward prompts follow existing three-card UI and return to the route screen. Class-exclusive relic rules apply to all new premium offers.

### Non-reused art, animation and audio

Each new room owns a **separate original SVG sprite sheet** in `games/midnight-descent/art/pixel/special_<room>.svg`, 512×128 with four individually drawn 128×128 frames, and the standalone original `special_room_icons.svg` contains seven 24px map glyphs. The pixel-world renderer draws **room-native** flooring, wall masonry, fixed sconces, obstacles and animated exits rather than copying old vault props. Central landmarks, rune effects, crystal reactions, orbiting pages and sparks animate independently via time-based sprite frames and particle curves. The seven palettes and graphic motifs are intentionally different.

`sound_bank.gd` adds **23 separately synthesized room-only cues** (introductions, charges, ritual interruptions, waves, exchanges, detonations, and completion stingers). The synthesis code specifies a different waveform and rhythmic family for each of the seven rooms; these are not aliases of existing sound IDs. All textures and sounds are generated/embedded in project source, without external stock assets.

Regression tests cover the appearance of all seven kinds in seed generation, combat objectives and failure rules, archive swaps and ability/stat removal, blood sacrifice offers, relic tempering and no duplicate interactions. The scene smoke test also verifies each imported 512×128 sprite, the seven custom map glyphs and nonoverlapping decorative footprints. Windows visual playtesting and difficulty tuning remain useful before release.

## Exploration overhaul: living floor plans and a follow camera

The original 1280×720 fixed combat rectangle is retained only for the **Threshold Tavern, service rooms and seven custom special rooms**. Regular fights, elite fights and boss rooms now use **authored traversable room geometry**, chosen from `room_layouts.gd` using each dungeon node's seeded `layout` value:

- **Floor 1, Iron Vault:** Twin Vaults, Upper Gallery, Broken Bastion, Twin Passage. Heavy mechanical chambers, raised galleries, narrow links and brass load-bearing structures.
- **Floor 2, Elder Grove:** Forked Grove, Serpent Roots, Sunken Thicket, Wildwood Crossing. Connecting root corridors, large forest clearings, damp terraces and branching thickets.
- **Floor 3, Royal Ossuary:** Processional Crypt, Ossuary Cross, Shattered Chapel, Royal Labyrinth. Ossuary halls, joined vaults, chapels and long processional routes.
- **Boss encounters:** Two further authored **entrance-corridor-to-arena** configurations per stage, for **six boss layouts**. Bosses and escorts are present within the far arena but remain dormant and cannot be damaged until the player crosses the arena threshold. Crossing starts a short reveal, illustrated by an animated arch and sigils with a dedicated synthesized low-horn cue. Existing boss HP, loot, soul award and three phases remain in place.

Each layout is made from overlapping `Rect2` walkable regions and a distinct set of solid obstruction rectangles. The full playable geometry is about twice the former stage width, with vertical travel, winding side halls and separated arena spaces. There is no walking across darkness between rooms: player movement, dashes and projectiles check the authored walkable bounds, and dash movement is substepped to avoid going through walls at frame hitches.

All three expanded floors use **independently painted 512×128 four-tile environment atlases**, `expanded_vault.svg`, `expanded_grove.svg` and `expanded_crypt.svg`. The renderer builds stage-distinct architecture, lamps, roots, stone details, ornaments and floor marks, rather than applying a recolor of floor one's textures. Existing special chambers preserve their own exclusive art and sounds.

A smooth camera offset keeps the player close to the center of the 1280×720 screen with a restrained aiming look-ahead. World lights, the pixel-art stage and effects pan together; screen-space HUD and menus do not move. Mouse aiming is converted back to world coordinates, so shots and abilities hit where the cursor points in a scrolling level. Hazard patterns adapt to the player's locality, rather than firing at the old hardcoded vault coordinates. Enemies use a shared 48px flow-field navigation map to pursue through winding corridors instead of running into the walls of a disconnected room.

### Verification

`RoomLayouts.smoke_test()` flood-fills the walkable floor grid for each of the **18 authored layouts**, validating both access to the far exit and legal boss/player spawn positions. Existing model combat, route, reward and boss tests continue to run. The gameplay scene additionally verifies all new stage atlas dimensions, that the camera tracks the player (while the HUD remains static) and that each floor's boss is triggered by walking into its arena. The legacy small-room renderer remains for the seven previous special chambers and the Tavern.

**Future refinement:** Windows playtesting remains essential for camera feel, precise boss encounter pacing, sprite frame visual polish and collision edge cases in tight corridors.

## November 2026 visual and boss-gate quality patch

The first expanded-stage gameplay review identified five defects. This patch addresses them without removing any of the 18 authored stage layouts:

- **Stage 1 floor density:** `art/pixel/expanded_vault.svg` has been repainted with calmer, continuous stone plates. The Vault now uses 85% plain base tile, 11% subtle variation and only 4% vents/gears. Tiles remain aligned to global grid coordinates across every connected room.
- **Merged corridors:** `pixel_world.gd` paints the union of the walkable region rectangles. Connected passages share a single texture coordinate system, with boundary trim drawn **only along exposed exterior edges**. Previous duplicate black outlines and overlapping branch borders are no longer drawn at intersections.
- **Dungeon map:** Combat and Elite tier labels use single-line captions (`Combat II`, `Elite III`) in fixed 92×52 pixel nodes. Captions are individually sized and clipped; node text no longer changes clickable card widths or spills over into neighbors.
- **Reactor chamber:** `art/pixel/reactor_core.svg` is a separate four-frame original animation with a stationary octagonal metal reactor body, cyan plasma center and occasional electrical arcs. The three core colliders have been resized to match the substantial fixed housing.
- **Boss-gate overhaul:** The Vault, Grove and Royal Ossuary have **separate, original 384×384 four-frame gate sprites** (`art/pixel/boss_gate_{vault,grove,crypt}.svg`). Each gate now spans its approach corridor rather than depicting a small floating frame at the arena entrance. On crossing the boss trigger the gate transitions `open → closing → closed`; the closing state immediately enables a real physical blockage. Its complete lock state remains impassable to player movement, dash and projectiles. Hydraulic closing and mechanical latching have two separately synthesized sounds. The gate is placed between the earlier corridor and the arena's overlap so that the player cannot walk around it; the six layouts have their choke positions checked against static pillars.

The regression suite verifies all six initially traversable boss gates, trigger activation, lock timing and blocked return travel, including dash-scale movement. A live Godot scene test checks the new reactor/gate atlas dimensions, and all 26 map nodes must render at fixed width with no wrapped labels. Loading a new expedition clears gate collisions and animations. The workflow now prints a direct source diagnostic when a world script fails to preload, making future Godot parser errors easier to troubleshoot.

## Follow-up quality pass: Objective HUD, service icons and stage II/III

- **Ritual encounters:** The normal combat wave counter and its central Wave banner are limited to real wave-based combat/elite rooms, not ritual, timed survival or reactor objectives. Rituals display only their anchor count and ritual countdown.
- **Survival encounters:** The timer is now **30 seconds**, down from 45. Both the progress bar and model smoke assertions use the shared `SURVIVAL_DURATION` constant. Survival still grants its normal room-clear reward.
- **Dungeon Map:** Shrine, Event and Merchant (internal type `shop`) receive **three separately hand-drawn 24×24 pixel icons** in `art/pixel/service_map_icons.svg`, routed through `UI.room_icon()`. Existing route-node labels stay single-line with clipping.
- **Archive and Forgotten Forge:** Upgrade/replace buttons display the actual equipped relic's 48-pixel icon, using the existing rarity-aware/class-aware relic atlas routing rather than a generic room symbol. The scene test checks that Archive's discard item image matches the item being discarded.
- **Stage II / III floor identity:** The Elder Grove and Royal Ossuary now have repainted **non-recolor** 512×128 tile atlases. Each follows the calmer 85% base tile / 11% subtle / 4% landmark distribution applied to Stage I. Grove uses broad mossy flagstones, embedded roots and rare fungi; the Ossuary uses smooth stone slabs and rare archaic grave seals.
- **Stage II / III boss entrances:** The Grove boss gate is a **living woven root barricade** with a glowing seed-lock. The Ossuary boss gate is a **descending iron/bone portcullis** with a skull latch. Both have original four-frame sprite sheets and independently synthesized close/lock cues. Both use the same physical gate lifecycle and sealed approach geometry verified on all six boss layouts. They are not recolors of the mechanical Vault shutter.
- **Reactor room across all stages:** The reactor shell is stable and physical, while arcs animate. Stages II and III now have separate sprite atlases (`reactor_core_grove.svg` and `reactor_core_crypt.svg`), environmental floor patterns and distinct reaction sounds. Grove contains living-root conduits and an emerald seed crystal; the Ossuary contains a bone-braced rune reliquary and violet aether core. The existing Stage I cyan machine remains unchanged.
- **Regression checks:** Scene smoke checks all service-icon atlas regions, each floor's reactor core dimensions, imported boss-gate animations and the Archive's selected relic icon. Model smoke checks that timed survival does not finish before 30 seconds.

## Build and tests

From a Windows checkout run `pwsh ./tools/build_midnight_descent.ps1 -OutputDirectory artifacts/app/MidnightDescent`. The script downloads the pinned Godot 4.5.1 editor and Windows export template, imports the game, runs smoke tests and exports an executable and PCK alongside the casino application.

Godot CI imports and parses the project and runs menu/model and gameplay-scene smoke tests. The model checks four forge effects, contract selection and payouts, all class-relic offer pools and their isolation, class gameplay synergy samples, existing synergies, Black Sun stack damage, boss HP and phase gates, dash effects, reset behavior, seating collisions, room traversal and combat progression. Visual validation on Windows, including font sizes and longer German strings, is still required.

## Controls

WASD / arrow keys move. In the tavern **E** interacts with the keeper, forge or portal and **Esc** closes an interaction or pauses. In combat the mouse aims; left mouse is the main attack, right mouse the special attack, Space dashes, Q activates the equipped item, and E opens a cleared gate. A relic can be selected with 1–3 or the mouse.
