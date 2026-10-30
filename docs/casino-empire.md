# Casino Empire: portfolio management

Empire is a separate idle economy. Empire credits fund its construction; the
shared casino bankroll, Coins, player XP and Season XP are independent. The only
outbound connection is a bounded owner cash dividend. Empire never changes
Blackjack rules, dice outcomes, slot odds or RTP. Claimed challenges and completed
game activity can benefit Empire in the other direction.

## Departments, floors and properties

Departments have a category, composable tags, a floor and optional requirements.
The same metadata drives upgrades, research, synergies, managers, events and UI.
`EmpireFloorCatalog` supplies navigation and statistics; no fixed two-floor UI
loop needs extending for another floor.

| Floor | Departments | Unlock |
| --- | --- | --- |
| Main Floor | Slot Gallery, Card Salon, Midnight Bar, Roulette Room, VIP Lounge | Starter estate |
| Upper Floor | High Limit Slots, Poker Room, Fine Dining, Grand Stage, Private Gaming Salon | Empire Prestige I |
| Resort Floor | Luxury Hotel, Spa & Wellness, Sky Bar, Convention Hall, Penthouse Club | Prestige III and permanent Resort Wing project; building it requires reputation 5,000 |

Each department supports levels 0–200, except the starter Slot Gallery always has
at least one operating level. Costs retain the original 18% escalation, rounded
up per purchased level; production doubles every ten levels. Bulk upgrades offer
×1, ×5, ×10, ×25, ×50 and ×100, bounded by level 200. Construction discounts round
each level separately, so bulk and repeated single-level prices agree.

| Property | Specialty | Purchase gate in the funding estate | Cost |
| --- | --- | --- | ---: |
| The Midnight Club | Balanced | Initial property | Free |
| Neon Bay Resort | Slots +25% | Prestige V, reputation 10,000, Skyline Property Office | 250B cr |
| Crown Palace | Tables / VIP +25% | Prestige X, reputation 14,000, Skyline Property Office | 1T cr |
| Aurora International | Hospitality / Entertainment +25% | Prestige XV, reputation 18,000, Skyline Property Office | 5T cr |

A purchase consumes the selected estate's budget and starts the new property
with 150 credits and one Slot Gallery level. Each property has independent credits,
levels, prestige, capital projects, managers, automation, events, contracts and
production statistics. All owned properties continue producing simultaneously,
including while another casino game is open. The property selector switches the
dashboard without moving credits. Legacy research and the owner reserve are
shared. Each property can reach Empire Prestige 100.

## Prestige, reputation and permanent research

Empire Prestige remains distinct from player Prestige. Every reset retains the
original permanent +25% production factor and earns one Legacy Point. Available
points equal the sum of owned properties' prestige minus purchased research ranks;
there is no separate claim receipt to duplicate. Historical prestige earns the
same points during migration.

Reset requirements are all five Main Floor rooms operating, `min(500,250+10P)`
Main Floor levels, and `5,000,000×(P+1)²` credits produced in the current run. The
previous 25-level increase up to 1,000 levels created an excessive late-game wall;
see the balance report. A reset affects only the selected estate's construction
credits, room levels and active event. Projects, research, contracts, contract
buffs, manager assignments, lifetime statistics, owner reserve and other
properties survive. Automation keeps its configuration and resumes at the next
scheduled decision when its budget permits.

Research branches have fixed rank costs and prerequisites:

- **Operations:** Efficient Construction (−2% per rank, three ranks), Night Shift
  (+2 offline hours per rank, two ranks), Compound Growth (+8% per rank at each
  level 50/100/150 threshold), Executive Automation and Board of Directors.
- **Gaming:** Table Authority and Mechanical Empire (+8% per rank, three ranks
  each), High Roller Network (+5% per rank per 50 levels on VIP / High Limit).
- **Hospitality:** Stay Another Night (+8% per rank), Destination Resort
  (Entertainment level 50 supports Gaming by +5% per rank), Premium Guests
  (+5% owner rate and +$40 reserve per rank).

Early Legacy Points cannot buy every branch. Research is permanent and the UI
shows ranks, cost and prerequisites before purchase.

Reputation is derived, never spent: operating rooms ×100, completed ten-level
milestones ×30, capital projects ×250, prestige ×750, synergy tiers ×150, and
completed events/contracts ×10 each (the last two contributions cap at 100 each).
Reputation gates construction and expansion; an acquired floor/project remains
unlocked after a reset lowers the current room contribution.

## Synergies and capital projects

| Synergy | Members | Level 25 / 50 / 100 bonuses |
| --- | --- | --- |
| High Roller Circuit | VIP-tagged departments on the original two floors | +10% / +20% / +35% |
| Night Out | Hospitality / Entertainment on the original two floors | +8% / +16% / +28% |
| Classic Casino | Classic-tagged starter gaming rooms | +10% / +22% / +40% |
| Destination Resort | All Resort departments | +8% / +15% / +25% |

Every member must meet a tier. Tiers replace rather than stack; opening a new
floor does not add members to an existing original-floor circuit. Complete tier-3
circuits get a status mark on affected room cards. The management page lists
members, current levels, active bonus and next threshold.

Capital projects are one-time purchases per estate:

| Project | Effect / gate |
| --- | --- |
| Grand Entrance | +5% global estate production; 250 estate levels |
| Casino Vault | +$160 shared owner reserve; Prestige I |
| Luxury Atrium | Hospitality +20%; original Hospitality rooms at level 50 |
| High Roller Elevator | VIP / High Limit +10%; Circuit effect +25%; Prestige II |
| Executive Offices | Unlock managers; Prestige III, reputation 2,500 |
| Convention Center | Event effects 25% stronger; original Entertainment at level 100 |
| Penthouse Suites | VIP +15%, owner rate +5%; original Main Floor VIP at level 100 |
| Resort Wing | Resort Floor; Prestige III, reputation 5,000, Grand Entrance |
| Skyline Property Office | Multi-property purchases; Prestige V, reputation 10,000, Resort Wing |

Exact costs are displayed before purchase and defined once in the Core catalog.

## Managers and automatic upgrades

Four permanent characters are available after Executive Offices and Prestige III:
Elena Ward gives her assigned department +15% production; Victor Shaw gives VIP /
High Limit +12%; Mara Bell amplifies Hospitality / Entertainment / Resort synergy
bonuses by 25%; Daniel Price reduces his assigned department's upgrade costs by
8%. There is no random hiring, rarity roll or gacha currency. Activate one manager
initially, two at Prestige V, three with Board of Directors research. Assigning,
reassigning and making a manager inactive saves immediately.

Executive Offices plus Executive Automation research unlock automatic upgrades.
Choose a minimum reserve, a 1–50% maximum budget per decision, a level target
1–200, and either cheapest next ten-level milestone or an ordered department
priority list. Priorities can be added, removed and reordered by name. Each
property evaluates one decision every 30 production seconds, with at most 50
upgrade batches. A decision's budget is fixed once, not recalculated after each
purchase. It never crosses the reserve or target, spends on locked floors or
uses casino cash. The same schedule runs during offline catch-up. Disabling
animation has no effect on it.

## Events, contracts and cross-game activity

Events are optional 30-minute Empire production focuses, one active per property:
Blackjack Championship (Tables +40%), Midnight Dice League (Tables / Hospitality /
Entertainment +30%), Neon Nights (High Limit Slots +75%, other VIP +25%), Haunted
Halloween (Hospitality / Entertainment +45%), and Quiet Sunday (Hospitality +30%).
Convention Center amplifies these effects. Overlapping boost selectors use their
strongest bonus, so Neon Nights does not count its VIP boost twice on slots.

Complete the displayed optional game goal during an event to earn one ten-minute
encore: 10 Blackjack rounds, one completed Midnight Dice game, 25 Neon Vault spins,
20 Manor spins or one Sudoku respectively. The existing player progression data
is the activity source. A new event records its baseline at purchase, and an
activity observation never retroactively increases the preceding accrual interval.
Offline simulation splits at event expiry. Expiration is counted once and does
not penalize normal production.

The contract board offers persistent briefs without real-time deadlines:

- Tourist Weekend: produce 5M Hospitality credits after acceptance; reward is
  Hospitality +15% for two production hours.
- High Roller Invitation: produce 20M VIP credits; reward is VIP +15% for two
  production hours.
- Expansion Drive: purchase 50 room levels; reward is construction −5% for two
  production hours.

Choose another brief after claiming. There is one active brief and one reward
buff per property; a new reward replaces the previous buff. Buff time counts
only accrued production time, including capped offline time. No extra currencies,
login streaks, maintenance failures or mandatory negative events are introduced.

Already-claimed challenge IDs remain the sole achievement source. Dice boosts
are `dice-games-50` → Bar +5%, `dice-score-300` → Card Salon +7.5%, `dice-five-50` →
Stage +10%, and `dice-wins-50` → Private Salon +15%. Sudoku adds `sudoku-50` → Spa
+5% and `sudoku-master-25` → Hotel +10%. Empire saves do not duplicate challenge
ownership. Existing Blackjack / Reels / Manor / Neon / Nine Lives boosts remain.

## Time, persistence and numerical bounds

Schema 3 migrates older root fields into The Midnight Club. Property records are
then authoritative; root credit/level/prestige fields are a compatibility
projection of the selected estate. Root lifetime production is portfolio-wide.
Normalization clones nested collections, validates catalog IDs and prerequisites,
bounds balances/levels, removes excess manager slots and prevents research from
spending more points than earned. Snapshots cannot mutate the live service.

The simulation starts on first visit. Offline storage is initially 8h, extended
to 10h/12h through research. A shared high-watermark consumes discarded absence;
repeated accrual and clock rollback cannot replay income. Events, contract buffs
and automation decisions introduce exact simulation boundaries inside that cap.

Base prestige and achievement multipliers retain their established behavior.
New research, property specialty, projects, synergies, manager, event and contract
production additions share one additive bucket capped at +500%. Construction
discounts cap at 25%, credits at a 10²⁷ technical bound (raised from 10²⁴ so Resort level 200 remains purchasable). The owner rate starts
at $2.30/h, scales to a base $40/h, and hospitality effects may raise it up to an
absolute $60/h. The shared reserve starts at $320 and caps at $640 across the
portfolio. Dividend remainder precision and whole-cent claims are retained.

All interactive purchases/configuration changes run on a detached candidate and
flush a complete `player-state.json` replacement before adoption. A failed write
leaves construction credits, Legacy Points and management choices unchanged and
retryable. Dividends atomically commit the increased casino bankroll and consumed
reserve together. Detached claims defer while a live casino game owns the wallet; the reserve remains available. The existing 30-second periodic save, game-close and app-shutdown
saves cover passive production. Embedded and optional detached views use the same
service; management and statistics create no additional native windows.

## Validation

`EmpireTests`, `Empire2Tests` and `EmpireStrategyTests` cover migration, caps,
clock rollback, exact discounts, research, projects, manager slots, tagged
synergies, time segmentation, offline/online automation, activity baselines,
contract receipts, independent properties, snapshot isolation and failed atomic
commits. The Windows startup suite exercises every management page, Resort Floor,
research/project purchases, manager assignment, automation inputs, event/contract
entry, property purchase/switching and portfolio statistics at two sizes. Existing
presentation tests retain upgrade/dividend/navigation checks at three sizes.
See `docs/empire-balancing.md` for simulation results and reproducible commands.
