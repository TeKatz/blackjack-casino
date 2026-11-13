# Midnight Descent: Twelve standard combat rooms and stage-specific enemy rosters

There are **four fixed, authored combat plans per stage**, each with different physical regions, collision walls, scene decoration and a deterministic stage-specific variant assignment. The seed rotates the sequence, so a run visits different designs without getting stuck in one repeated layout. Special-service and special-combat chambers retain their dedicated scenery. Two boss approach plans per stage also remain.

| Stage | Room 0 | Room 1 | Room 2 | Room 3 |
| --- | --- | --- | --- | --- |
| Iron Vault | Twin Vaults | Upper Gallery | Broken Bastion | Twin Passage |
| Elder Grove | Forked Grove | Serpent Roots | Sunken Thicket | Wildwood Crossing |
| Royal Ossuary | Processional Crypt | Ossuary Cross | Shattered Chapel | Royal Labyrinth |

**Common enemy pool (all floors):** Stalker, Caster, Brute, Archer, Shieldbearer.

**New floor-exclusive specialists:** 
- Iron Vault: **Gearhound** (telegraphed mechanical leap), **Coilgunner** (dual spread volley).
- Elder Grove: **Briarling** (predictive root snare), **Sporewitch** (poison pool and projectile).
- Royal Ossuary: **Bone Lancer** (three-spear fan), **Crypt Oracle** (predictive seal and soul bolt).

Elder Grove also retains its established Vine, Mushroom and Spore enemies; Royal Ossuary retains Revenant, Hexer and Sentinel. The six new specialists have their own pixel sprite sheets, attack timing and stats. At least one of each floor's new specialists is guaranteed per full-sized standard encounter. Bosses gain a native specialist add.

**Encounter density:** normal large-room fights begin at about 8 in stage 1, 13 in stage 2 and 18 in stage 3, increasing with threat, elite status and depth; the entrance fight retains a short onboarding count of 6/9/12. Additional waves scale separately, capped to avoid unbounded enemy entities. Special combat rooms receive moderate floor-based increases without converting them to large rooms. Kill shard rewards were adjusted for the higher number of kills to avoid runaway buying power.

**Testing:** Godot smoke checks connectivity of all authored layouts and stage-exclusive roster/scaling; gameplay headless smoke exercises the live world scene, collision, stage geometry, and proper animation sprites.
