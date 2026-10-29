# Casino Empire foundation

Casino Empire is available from the existing casino lobby. It is a native WPF
`UserControl` hosted in the main casino surface; it never creates a `Window`,
dialog or HWND. Back and Escape return to the lobby. Covered controls are disabled
and keyboard navigation remains inside the page.

## First playable loop

- Start with 150 Empire credits and one producing Slot Gallery level.
- Select any of five rooms in the furnished 2D floor plan to open or upgrade it:
  Slot Gallery, Card Salon, Midnight Bar, Roulette Room and VIP Lounge.
- Production is automatic, including while playing another casino mode.
- Prices grow by 18% per level (rounded up). Every tenth level doubles that
  room's production and adds richer furnishings/trim. Each room supports 200 levels.
- Existing preferences control subtle cabinet, roulette and production-light
  animations. Animation speed has no effect on the simulation.
- A separate owner reserve earns $2.30/hour initially, growing by $0.30/hour per
  additional room level, capped at $40/hour. Capacity is $320. Claims transfer
  whole cents to the shared play-money bankroll without spending Empire credits.

Empire credits cannot be bought with or converted directly to casino cash.
Profile coins, XP, Blackjack rules and slot odds are independent of this economy.
The initial values are starting balance parameters for a foundation, not a claim
that the final long-term economy has already been tuned.

## Time and persistence

The simulation begins only on the first visit. Subsequent launches catch up a
maximum of eight hours at the saved production rate. All excess absence is
consumed, so reopening cannot repeatedly claim an offline interval. Repeated ticks
and clock rollback do not duplicate income. Missing Empire data in old saves
creates the starter economy; malformed level/currency fields are bounded.

Empire state is included in `player-state.json` with the existing casino wallet.
It saves after purchases, opening/closing the page, on shutdown and every 30
seconds while the business has started. State writes now use flushed temporary
files followed by atomic replacement. A dividend commits the consumed reserve
and increased wallet in that same write **before** updating memory; failed writes
leave the reserve claimable. No separate profile write is needed for this mode.

The simulation stays in Core and uses explicit timestamps. The view owns only
presentation/animation; MainWindow composes it with navigation and persistence.
Snapshots are detached so existing slot controllers cannot mutate live progress.

## Validation and next layers

`EmpireTests` covers starting, upgrades, milestones, online/offline equivalence,
offline caps, clock rollback, malformed and legacy state, snapshot isolation,
dividend retry/repetition and atomic wallet/save round trips. The Windows
presentation smoke opens the real lobby entry, upgrades a room, pays a dividend,
checks three window sizes, returns/reopens, and checks Escape and native-window count.

Managers, research, prestige, additional locations, special events and collection
rewards are future layers. This foundation already provides the independent
economy, room catalog, persistence, host navigation and 2D presentation they can use.
