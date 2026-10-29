# Table, profile and progression refresh

- The decorative chip tray has been removed. Each player hand instead shows its actual wager as denomination-labelled casino chips, including doubles and split hands. Large stacks use a quantity marker to keep rendering bounded. Chips are presentation only; they cannot change wagers, payouts, bankroll or rules. Placement, stake changes and settlement respect the animation preference.
- The player seat has additional clearance from the felt's curved betting line.
- Profile and progression now navigate inside the casino host. Opening progression from the profile returns to the same profile; opening it from the lobby returns to the lobby. The underlying game and its keyboard actions stay covered until the page closes. Escape goes back one page; F11 still controls the main window.
- The profile uses the casino's custom controls and palette, a persistent identity sidebar and a searchable owned portrait collection. Renaming, equipping and JSON progress export remain available.
- Challenges have persistent claim controls, game and status filters, search, and bounded pages. Claim All includes every eligible unclaimed reward, including rewards hidden by the current filters. It saves one batch and rolls back the batch if saving fails.
- The shop has compact preview rows with type, availability/ownership, price, affordability, search, sorting and pages. Owned cosmetics can be equipped from the same collection.
- Overview shows a casino summary and separate Blackjack, Midnight Reels, Midnight Manor and Neon Vault statistics. Earlier combined slot history remains visible and contributes to casino totals. See [tracking migration](progression-tracking-migration.md) for save compatibility.
- The catalog adds 25 endgame milestones plus introductory Neon Vault challenges. Endgame rewards cover long-term Blackjack and slot play, rare features, lifetime progress and Prestige 3/5/10.

## Verification

CI builds and tests the solution on Windows, checks the shared control templates, and runs the casino presentation and Manor runtime smoke programs. Presentation captures cover the embedded profile/progression back stack, lobby tab shortcuts, search and pagination at minimum and regular window sizes. Existing portrait purchase/equip smoke checks locate each portrait through shop search rather than assuming the whole catalog is on one page.
