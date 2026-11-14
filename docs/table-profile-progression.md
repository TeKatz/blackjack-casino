# Table, profile and progression refresh

## Native Player Profile (post-WPF cutover)

The WPF-free React Player Profile now features an emerald-and-gold identity stage, a shared Prestige crest
in the profile and lobby header, raised statistics tiles, and individual navigation cards. The new
**Data & profile options** drawer uses native Windows actions: a Save File dialog exports the live
profile as JSON and a button opens the currently active native save directory. A JSON profile export
is **not** a complete installation backup (other saves, browser storage and preferences remain separate).
The removed WPF "Export & advanced options" action must not return.

At level 100, the profile exposes a two-step **Ascend to Prestige** confirmation. The existing C#
PrestigeService rules remain authoritative: level resets to zero, accumulated coins and statistics
are preserved, and 500 coins, one Legendary pack and two Epic packs are awarded. The native host
clones the profile and commits the entire promotion through its aggregate journal before sending the
success receipt. A duplicate request fails because the saved level is no longer 100. Only after
acknowledged persistence does React play the special animated Prestige seal/rings ceremony.
Reduced-motion settings and a skip button allow an immediate static result. Smoke tests
exercise the transition, repeated requests, restart and exported JSON on isolated temporary profiles.


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

## November presentation and input fixes

- The lobby's destination hub fills its available height and expands into the existing
  letterbox gutter. Game tiles retain their original grid, scale and position. A single
  scalable emblem set is shared by the hub and Album/Challenges header shortcuts.
- Premium collectible inlays anchor to all four edges of the actual inner card at every
  scale. The inspector uses a full-height archive spread with edition/catalogue/theme
  details and adapts native card dimensions to smaller windows.
- Sudoku uses a bound numeric text renderer. Automatic translation preserves dependency
  property bindings; entering, erasing and undoing digits no longer leaves stale glyphs.
  The Lounge return action lives in the shared header.
- Player Profile owns Statistics and Hand History navigation, and displays owned titles
  with equip actions alongside portraits. Progression & Shop contains rewards and shop
  destinations only. Challenges use a responsive grid whose page size fits the available viewport.
- Presentation settings migrate the previous global values into independent destination
  records on first load. Blackjack keeps its existing controls; slot/Manor controls save
  only their destination settings. A muted embedded page consumes its own menu events
  so its host cannot replay them. Game state, wallets and rewards remain shared.
- Neon Noir, Eclipse Thread, Eclipse Halo and Aurora Glass share one renderer across
  header and shop previews, with distinct geometry, materials and motion. Reduced motion
  is honored; hidden/unloaded frame clocks stop and refreshes replace their lifecycle.

## Lobby progress and menu responsiveness

The hub uses generated RGBA emblems from `Assets/Navigation`, cached once and
shared with the header. Statistics now names the existing lifetime statistics
route. The Season Pass bar measures XP within the current tier interval; the
Challenges bar picks the highest relative progress among incomplete, unclaimed
challenges. The header and lobby share the same data snapshot.

Pack badges are larger and breathe through scale and opacity on the small badge
surface only. Hidden, disabled or unloaded badges stop their clocks; local
reduced-motion preferences disable breathing. Blur radius is never animated.

Progression pages build their selected tab on first display and cache it until
profile changes. Shop/Challenges never initialize the retired album, unrelated
statistics, history or Season reward controls. Translation observes text through self bindings rather than global
dependency-property descriptor subscriptions. Closed menus can be collected, and
temporarily detached game controls still receive immediate localization updates.

Lobby-to-game handoffs keep the lobby visible until the embedded destination
has content and its overlay is visible. Return navigation covers the table in
the reverse order. Runtime smoke checks observe the visibility change directly
for game entry, and record menu-open timings plus off-screen construction checks.
