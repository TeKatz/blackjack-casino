# Blackjack table modernization — React primary route

Branch: feature/manor-modernization-phase-1

The React/WebView2 Blackjack table is the normal lobby destination. The former
WPF table is retained solely as an emergency fallback for missing frontend
assets or WebView2 startup/process failure. C# remains authoritative for
economy, RNG, settlement, rules, saved progression and hand history.

Delivered:
- A C#-owned, read-only round snapshot covering table rules, presets, dealer,
  player hands, AI seats, stake, bankroll, legal actions and cosmetics. Monetary
  values are transported as exact invariant decimal strings. The dealer hole
  card is not included until settlement; its face-down placeholder carries
  neither rank nor suit.
- Full native-authoritative custom-table rule editing from React. A strict
  fifteen-field validated rules payload is applied only between hands,
  preserving Core's shoe/insurance/split behavior and persisted preferences.
  The React table also exposes the original play-money bankroll reset.
- Equipped card sets now use the same collection variants and theme colors
  as native WPF cards, including premium dark faces and face-down markings.
  No unrevealed dealer card values are transmitted.
- A versioned intent parser restricted to the exact packaged Blackjack origin,
  finite wager range, legal command names, session epoch, request UUID and
  expected revision. Unique receipts prevent repeated card or money actions
  on duplicate messages. The parser and session are wired to the native
  CasinoSession through the opt-in WebView2 gameplay preview.
- A matching TypeScript contract, rejecting malformed responses and snapshots,
  plus C# and TypeScript boundary regression tests.
- The standalone responsive React table now owns the default Blackjack route
  from both React and native fallback lobbies. The old preview button is removed.
  The original native table is reachable only when assets are missing or the
  WebView2 engine fails; even a mid-hand browser failure preserves the C#
  session and returns players to a playable native table. A no-response
  watchdog prevents permanent black/loading screens. A completed React round
  still uses the original settlement, reward, and persistence services.

Acceptance and remaining work:
1. The primary mode opens the React table twice consecutively without a
   WPF preview button, mouse hover, stale browser session or blank table.
2. The existing Windows smoke suite continues exercising the WPF card renderer
   only through a private, test-only reflection path, not the player route.
3. React startup, custom rules, dealer hole-card secrecy, AI hands and
   settlement remain covered by native and browser regression tests.
4. Keep native WPF emergency fallback until missing-asset and browser
   recovery have been validated on a production Windows install.
5. Later remove WPF visuals and historical tests after true parity is
   established. Do not retire the C# gameplay logic or updater.

No new branches or main merges; Development publishing continues from
feature/manor-modernization-phase-1.
