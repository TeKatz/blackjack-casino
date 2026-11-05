# Casino Empire: portfolio management

Empire is a separate idle economy. Empire credits fund its construction; the
shared casino bankroll, Coins, player XP and Season XP are independent. The only
outbound connection is a bounded owner cash dividend. Empire never changes
Blackjack rules, dice outcomes, slot odds or RTP. Claimed challenges and completed
game activity can benefit Empire in the other direction.

## Modern estate command center

`frontend/empire.html` is the sole normal Empire presentation in embedded and
detached windows. The WPF shell owns lifecycle and the original `EmpireService`;
React owns the gallery/inspector and the Estate, Development, Operations,
Opportunities and Statistics navigation. Development continues from
`feature/manor-modernization-phase-1` with the existing release guards.

The gallery shows five departments per floor at standard desktop widths. Floors,
operating/closed filters, room selection, bulk quantities (1/5/10/25/50/100),
production breakdowns and milestone progress remain accessible. Estate overview
contains the prestige action with a confirmation dialog. Research keeps all
three branches, rank costs and prerequisite links. Each specialist can be
assigned to any operating department or made inactive. Automation keeps editable
reserve, budget, target, milestone strategy and ordered room priorities without
replacing unsaved input on production ticks. Contracts show progress and claims;
events show their goal, end time and encore. Statistics include portfolio and
property lifetime totals, tag production, construction, contracts, events,
achievements, owner cash and offline capacity. Startup catch-up retains per-room
and per-property earnings.

One Pixi application stays mounted across section and room changes. It animates
retained light beams and particles over the original room artwork. Gallery cards
use static high-resolution backgrounds and stable React room references; there
is no renderer per card and no per-frame shape redraw. DPR is bounded at 2,
resize and monitor-DPI changes update the existing renderer, and decorative
motion stops on hidden documents, hidden/minimized native windows, management
routes, reduced motion or the Empire animation preference. GPU initialization
failure keeps the DOM controls and static artwork usable. Assets are copied from
`Assets/Empire` to `ModernFrontend/empire-art` at build/publish time; browser-only
Vite previews need those local files for artwork.

## Loading and return-to-estate lifecycle

The first embedded visit creates one dedicated WebView2 control in a separate,
permanent overlay child. Returning to the lobby collapses that child without
disposing its browser. Other games continue to use their independent shared
embedded-game host, so the cached Empire visual is never reparented through their
content controls. The retained browser is reused on later visits; a failed host
is disposed and rebuilt on retry or next entry. Detached windows retain their
own independent lifecycle and are disposed when closed. The cached embedded
browser is disposed with the main window, not persisted across application
restarts. This policy trades one resident idle browser for faster repeated visits;
memory and long-session resource use still need measurement on player hardware.

A loading surface appears immediately, with browser setup and estate handshake
phases, a lobby return action, a 20-second timeout and an explicit retry path.
Startup milestones are recorded relative to the native opening action for WebView2
initialization, navigation, bridge readiness and DOM usability. A per-visit ID
travels with the latest authoritative C# snapshot; React acknowledges it after
committing the management DOM. Until that acknowledgment, the hosted browser is
not input-enabled, including on a warm return. This prevents old displayed values
from accepting commands. PixiJS is imported in a separate, lazy frontend chunk:
static room artwork and management controls do not wait for the GPU renderer.
Visual effects only animate while the host is visible and animation is enabled.
No invented percent progress or asset-wide preloading is used.

The Windows Empire smoke records cold and warm phase timings to
`empire-startup-timings.json`, verifies browser identity across two visits,
asserts the hidden Pixi ticker has stopped, and exercises the detached window
separately. These CI timings are observed test-runner milestones, not a universal
end-user speed guarantee. A retained WebView2 can use more idle private memory;
verify CPU, memory and resume behavior during extended native testing.

## Bridge and transaction boundary

`EmpireModernProtocol` version 1 accepts intents only from the exact packaged
HTTPS `/empire.html` origin. Catalog IDs, quantities, policy bounds, UUID request
IDs, session epoch and expected revision are validated. All production, prices,
quotes, eligibility and preview outcomes come from C#. Credit amounts cross the
bridge as invariant decimal strings; display abbreviation never determines a
purchase. Policy fields support German decimal commas in the UI and are sent
as exact invariant decimal strings.

Mutations use the existing `MutateEmpire` / `EmpireService.Transact` path.
Dividends keep the original atomic shared-wallet callback and active-game guard.
No Core economy formula, balance, save schema or release pin changes. Each
mounted host has an unpredictable epoch and monotonic command revision; it
retains up to 4,096 receipts without eviction, then requires reopening. Explicit
user actions increment that revision, while independent deterministic accrual
continues at 1 Hz and Core revalidates affordability at commit. A new browser
cannot replay an old epoch. This is session deduplication, not a new durable
command journal; durable correctness remains in the existing save transaction.

Previews run actual commands against `EmpireService.Preview`'s isolated copy,
never persistence or wallet callbacks. Explicit previews are available alongside
upgrade hover/focus previews and show construction budget, portfolio/room output,
levels and resulting circuit tiers. Section/room/filter changes, edits and
commits clear the preview. A command timeout requests a read-only snapshot and
blocks further mutations until its receipt arrives or the view is reopened.
Snapshots carry the last receipt so a missing acknowledgement can be recovered
without automatically resending an action.

## Verification and performance evidence

`EmpireModernBridgeTests` covers the trusted origin, validation, revisions,
duplicate requests, expired epochs, failed saves, exact German policy decimals,
read-only snapshots and previews versus committed Core results. Existing Empire
math, strategy, offline and persistence tests remain applicable. Frontend tests
cover snapshot/receipt validation, decimal policy editing and retained room data.
The Windows presentation and startup smokes exercise the actual WebView2 bridge,
all management routes, upgrades, research, projects, specialists, automation,
events, contracts, properties, dividend settlement, prestige, three exact CSS viewport
sizes through WebView2 device emulation, observed native host dimensions, and
detached/minimized hosting.

The startup smoke also runs sequential read-only WPF and modern fixtures at
requested native window sizes of 1080×720, 1366×768 and 1920×1080: one property, all 15 rooms at level 100, Prestige
25, Resort Floor with Luxury Hotel selected, animation enabled and 1 Hz economy.
It writes `empire-presentation-performance.json` with mean/p95 host composition
intervals, modern browser rAF intervals, CPU and memory. CPU/memory include the
host and all processes in the shared WebView2 environment, including the hidden
lobby when alive, in both cases. The runner may clamp native windows to its
virtual monitor; recorded actual client dimensions are authoritative. Exact CSS
viewports are tested separately and never represented as physical monitor tests.
Reduced motion is disabled for the modern benchmark and its ticker must actually
be running. The 500 ms warm-up and four-second samples are short runner
diagnostics. Composition/rAF/ticker clocks have different semantics; none is GPU
frame time. GPU utilization/VRAM and long-session behavior remain unmeasured.
No performance improvement is asserted without inspecting an actual result file.

### Published single-file resource regression

Empire PNGs remain WPF `Resource` items. Their browser copies use MSBuild `None`
items with output/publish target paths; the source PNGs must never also be
registered as WPF `Content`. Associated-content metadata takes priority over
embedded pack resources and can resolve through an absent assembly directory in
a single-file executable, crashing the legacy lobby artwork during startup.

The artwork integrity check rejects overlapping `Resource` / `Content` source
files. CI and both publishers also launch the actual published executable with
`--packaged-artwork-smoke` from an unrelated temporary working directory. This
isolated diagnostic starts before normal windows/player stores, decodes all six
atlas crops and ten upper/resort rooms through the same WPF loader, and verifies
all eleven loose browser images and `empire.html`. It writes
`published-artwork-smoke/packaged-artwork.json`; publishing is blocked on failure.
Build-directory startup tests alone do not cover this regression.

### Recorded Windows runner comparison — 2026-11-05

[Raw measurements](performance/empire-presentation-a37e354.json) retain the full
unrounded data and source commit/run. The four-core Windows 10.0.26100 runner
measured the same late-career fixture sequentially with animations verified on.
The actual native client sizes below reflect its virtual-monitor limits.

| Actual client | WPF CPU | Modern CPU | Private memory WPF / modern (MiB) | Host callback p95 WPF / modern |
| --- | ---: | ---: | ---: | ---: |
| 1042×684 | 3.69% | 0.97% | 733.1 / 768.6 | 31.25 / 15.62 ms |
| 1042×732 | 5.06% | 0.78% | 726.2 / 761.2 | 31.25 / 15.62 ms |
| 1042×752 | 7.28% | 1.27% | 724.4 / 765.8 | 31.25 / 15.62 ms |

These four-second samples show lower CPU use and roughly 35–41 MiB more private
memory for the modern surface in this fixture. Host composition callbacks are
not GPU frame times or a universal FPS guarantee. Modern browser rAF p95 was
15.7 ms; raw mean/p95/count values and process accounting remain in the JSON.
Long-session CPU/memory, GPU/VRAM and native high-DPI acceptance remain open.
Exact 1080×720, 1366×768 and 1920×1080 CSS layouts are tested separately through
WebView2 emulation; these measurements do not pretend the runner has those
physical monitors.

Manual acceptance remains required for German/English text and keyboard use,
125/150/200% DPI and monitor moves, initial loading and artwork quality, a
30-minute detached session beside a live game (including blocked dividend
collection), offline save/reopen and a simulated failed save. Compare baseline
and modern CPU/private-memory trends on the same machine, graphics device,
viewport, fixture and duration. Retiring the rollback source is a separate
post-acceptance cleanup.

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
circuits get a status mark on affected room cards. The Synergies section lists
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
entry, property purchase/switching and portfolio statistics at two sizes. The
command center adds three-resolution visibility checks, every research branch,
prerequisite connectors, catalog pagination and input stability across refreshes. Existing
presentation tests retain upgrade/dividend/navigation checks at three sizes.
See `docs/empire-balancing.md` for simulation results and reproducible commands.

