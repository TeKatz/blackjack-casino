# Blackjack table modernization — gameplay parity phase

Branch: feature/manor-modernization-phase-1

The existing WPF Blackjack table remains the standard route during the
parity and native-host verification phase. The goal is to change presentation without touching
the casino economy, RNG, settlement, rules, saved progression or hand history.

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
- A standalone responsive React Blackjack entry point and a live WebView2
  preview, accessible via the Modern Blackjack (Preview) button on the native
  Blackjack sidebar when assets are installed. It invokes the same native
  CasinoSession, wager actions, preset rules and existing settlement handler.
  The default Blackjack lobby route deliberately remains WPF until parity tests
  and the native custom-rule editor have a safe replacement.

Next implementation milestones:
1. Complete native Windows and browser-level acceptance for custom rules,
   1–5 AI seats, split hands, insurance and European no-hole-card behavior.
2. Verify resize/DPI, save/reload, recovery and keyboard accessibility against
   the existing WPF table, plus any optional nonblocking PixiJS ambience.
3. Change the primary Blackjack lobby route to the React table only after
   regression checks establish parity; retain native table fallback for
   missing/broken browser assets.
4. Remove native WPF controls only after the fallback and smoke suites have
   been migrated to equivalent modern assertions.

No new branches or main merges; Development publishing continues from
feature/manor-modernization-phase-1.
