# Collector's Album

The **Album** tab in Progression contains 136 permanent collectible cards, independent of playable card-set cosmetics. Twelve regular themes have ten cards each (four Common, three Uncommon, one Rare, one Epic, one Legendary). Four ultra rare themes contain four Legendary cards each. Every stable card ID has its own painted illustration, backed by 16 embedded theme atlases. Missing slots remain visible; each theme tracks completion.

## Illustrated editions

All 136 subjects are individually assigned in `Assets/CollectionArt/manifest.json`; `prompts.json` preserves the image-generation briefs. The 12 regular atlases use five columns and two rows; the four ultra rare atlases use two columns and two rows with larger portrait illustrations. No card shares an illustration region. Atlas SHA-256 checksums, unique pixel hashes and non-overlapping crop coordinates guard against missing or accidentally reused art.

Cards use full-art scenes, engraved metal borders and readable shaded captions. Rare, Epic and Legendary cards add a gentle moving foil reflection; ultra rare editions have stronger finishing. Reflections stop when hidden or unloaded and respect the animation preference. Selecting an owned card opens a larger inspector. Escape or the close button returns to the same album card, including inside the main casino window; undiscovered illustrations stay hidden.

## Packs and odds

Every pack contains exactly five random cards; repeats in the same pack are possible. Pack rarity guarantees that **one slot** is at least that rarity. Its regular rarity distribution is the same weights conditioned on that floor. The other four regular slots use these weights:

| Pack | Coin price | Common | Uncommon | Rare | Epic | Legendary | Ultra rare theme chance per slot |
|---|---:|---:|---:|---:|---:|---:|---:|
| Common | 100 | 62% | 25% | 9% | 3.5% | 0.5% | 0.25% |
| Uncommon | 200 | 35% | 42% | 17% | 5% | 1% | 0.5% |
| Rare | 400 | 20% | 34% | 34% | 10% | 2% | 1% |
| Epic | 800 | 10% | 25% | 39% | 21% | 5% | 2% |
| Legendary | 1,600 | 5% | 18% | 35% | 30% | 12% | 3% |

Each slot **first** rolls the independent ultra rare theme chance. Success selects uniformly among the 16 ultra rare cards; otherwise the regular rarity distribution applies, followed by uniform selection among regular cards of that rarity. Legendary guarantees therefore do not bypass the ultra rare gate. Pack tooltips show these rules. All purchases use existing Cosmetic Coins, not the play-money bankroll.

## Sources

- New and existing profiles receive three Common welcome packs once on collection schema migration.
- Each newly claimed challenge awards one pack: Common below 40 reward coins, Uncommon from 40, Rare from 100. Single and Claim All use the same rule.
- Each newly crossed Season Pass milestone awards one pack: every fifth tier Common, every tenth Rare, every twenty-fifth Epic, tier 100 Legendary (highest matching rule only). Both existing passes use the same rules; completed historical tiers are not replayed.
- Full Casino Vaults give one pack matching their tier: Bronze → Common, Silver → Uncommon, Gold → Rare, Platinum → Epic, Diamond → Legendary. The pack is part of the durable prepare receipt; payout recovery never grants another.
- Successful player Prestige gives one Legendary and two Epic packs. Collection cards, unopened packs, points and an unfinished reveal survive the level reset.
- All five pack types can be purchased in the existing Cosmetic Shop.

## Duplicate recycling

A first copy becomes permanent album ownership. Each subsequent copy, including a repeat in that same pack, gives points: Common **5**, Uncommon **10**, Rare **25**, Epic **75**, Legendary **200**. Every **500** points automatically grants a Common pack; surplus points carry over. Multiple conversions in a single opening are supported. Bonus packs stay unopened in inventory.

## Save and presentation behavior

Profile schema 6 adds a separately versioned collection. Normalization is idempotent, restores missing/null fields and sanitizes invalid inventory. There is no retrospective challenge, season, vault or prestige reward replay.

Opening uses a cloned candidate profile. Pack consumption, all five cards, duplicate conversion and a pending opening receipt are saved together **before** presentation. Only a successful write is adopted. A failed save leaves the live profile unchanged. The pending receipt survives navigation, application closure and reload, preventing rerolls and double spending. Continue clears only that receipt; cards were already committed. Challenge claims and Prestige similarly save the candidate before adopting rewards. Vault preparation rolls back collection inventory too if its initial save fails.

Packs have five distinct frames, ornament counts and glow strengths. Opening adds a foil-tear sound and rising sweep, pack shake and staged card reveals. Higher rarity cards add longer chords, stronger frame glow and foil sweeps; ultra rare theme cards have additional glow. Cards can be revealed by keyboard/click or all at once. Global sound and animation preferences are respected. Navigation cancels pending presentation and audio without discarding the receipt. Album labels and summaries are available in English and German.

## Validation

`CollectionTests` covers catalog shape, five-card draws for all pack qualities, rarity guarantees, ultra rare pool reachability, in-pack duplicates, point carry and multiple conversions, serialization/resume, concurrent opens, prices, all reward sources, Prestige retention, failed-write rollback/retry and synthesized audio bounds.

`tools/check_collection_art.py` verifies all 136 assignments, all 16 atlas checksums, crop bounds and uniqueness. The Windows presentation smoke additionally checks decoded pixels and perceptual hashes, captures every theme and rarity finish, opens the actual inspector at 1080×720 and 1440×900, verifies Escape/focus restoration, and checks foil animation visibility/unload behavior. It also exercises all theme pages, pack purchases, five-card reveal, duplicate click protection and resumed receipts. The Windows runtime is required to execute WPF; Linux can compile the complete desktop project and execute the domain/persistence/audio tests.
