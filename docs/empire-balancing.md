# Empire economy validation

The balance harness uses the same Core catalogs, exact rounded upgrade quotes,
prestige factors and strategic production engine as the application. It runs
Prestige 0 / 1 / 5 / 10 / 25 / 50 with seven build states: the legacy production
counterfactual, balanced, Operations, Gaming, Hospitality, all claimed achievements,
and an established managed estate. Each research build spends only points earned
at that prestige and obeys its prerequisites. The managed scenario assumes that
Grand Entrance, Executive Offices and High Roller Elevator were purchased in an
earlier run; their construction cost is not charged to the new reset.

## Reproducible commands

```sh
dotnet run --project tools/EmpireEconomy -c Release -- docs/empire-economy-before.csv --legacy-requirements
dotnet run --project tools/EmpireEconomy -c Release -- docs/empire-economy.csv
dotnet run --project tools/EmpireEconomy -c Release -- docs/empire-economy-mixed.csv --all-floors
```

Each run also writes `empire-break-even.csv` for all 15 departments at levels
1, 9, 25, 49, 50, 99, 100, 149, 150 and 199. The harness compares single upgrades
and the next ten-level milestone by marginal-production cost, then advances to
the next affordable purchase. This is a deterministic greedy policy comparison,
not a proof of a globally optimal multi-department purchase schedule. Main-Floor
focus is intentionally a conservative scenario. The mixed-floor file lets
unlocked upper-floor departments compete for investment and reveals cases where
short-term ROI detours delay the next prestige.

The time model assumes continuous purchase decisions at affordability boundaries;
it is not a promise about real elapsed time for an unattended player. Accrual is
chunked within the storage cap. Clock rollback, offline storage, event boundaries,
contract-buff expiry and the fixed automation schedule have separate unit tests.

## Measured hours to the next prestige

Original legacy requirements grow by 25 Main Floor levels per prestige to a
1,000-level ceiling. The shipped requirement grows by 10 levels to a 500-level
ceiling. The 18% cost curve, level-200 ceiling and ten-level production doubling
remain intact. Run-credit requirements retain their quadratic progression.

| Prestige | Original legacy curve | New balanced | Operations | Gaming | Hospitality | Managed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 2.898 | 2.846 | 2.846 | 2.846 | 2.846 | 2.846 |
| 1 | 3.570 | 2.297 | 2.250 | 2.233 | 2.273 | 2.250 |
| 5 | 7.587 | 2.161 | 1.990 | 1.989 | 2.108 | 1.808 |
| 10 | 27.840 | 2.630 | 2.187 | 2.364 | 2.524 | 2.001 |
| 25 | 11884.982 | 12.639 | 9.724 | 10.304 | 11.691 | 8.940 |
| 50 | 92261.020 | 6.788 | 5.222 | 5.534 | 6.278 | 4.801 |

The original main-floor path at Prestige 25 exceeded 11,000 hours; at Prestige 50
it exceeded 92,000 hours. A few extra production percentages could not repair that
wall. The revised prestige target removes it while preserving long-term level-200
projects and expansion goals. The first prestige stays around 2.85 hours without
achievement boosts; the all-claimed-achievement scenario is about 2.25 hours.

Operations helps the reset sprint through construction discounts and level-50
milestones. Gaming improves production-focused rooms. Hospitality trades some
reset speed for owner income and broader estate synergies. The mixed-floor
heuristic is not always faster: investing heavily in higher rooms can overbuild
relative to the next prestige requirement, which is an intentional strategic
tradeoff rather than an unexplained optimality claim.

## Multipliers, break-even and early-room relevance

Legacy prestige and claimed-achievement factors remain separate. All new
production additions share one additive bucket capped at +500%; this avoids
unbounded products of project × manager × event × research × synergy factors.
Construction discounts cap at 25%. Classic Casino specifically keeps the three
starter Gaming rooms relevant at higher tiers. The break-even CSV exposes the
large improvement at a ten-level crossing and the declining marginal efficiency
of isolated high levels.

Resort construction has higher base prices and meaningful specialty production.
The technical credit bound is raised from 10²⁴ to 10²⁷: every department's final
single-level quote must remain below the cap, including Penthouse Club 199→200.
This is checked directly by a test. It does not introduce another currency.

## Dividend, storage and portfolio limits

The base owner rate still caps at $40/h. Hospitality research and Penthouse Suites
can improve it, with an absolute $60/h guard. Four properties do not create four
cash reserves: one shared reserve caps at $640. Vault/research capacity upgrades
are bounded even when bought in several estates. A maximal four-property,
Prestige-100, all-research, all-projects, level-200 test accrues a long absence and
verifies finite credit bounds and the shared cash caps without overflow.

Storage is 8h / 10h / 12h according to Night Shift research. Offline reports show
per-property earnings and the selected property's room breakdown. Event boosts
are integrated only until expiry; contract buffs use paid production seconds;
automatic decisions occur every 30 production seconds. Frequent online calls and
one equivalent offline call reach identical balances and upgrade states in tests.

The harness also runs 10,000 half-second simulation ticks. Local runs completed
in well under one second for the single-estate stress fixture; this is diagnostic
output rather than a hardware-dependent CI threshold. The production view's
500-ms refresh does not rebuild the management forms or reset unsaved inputs.
