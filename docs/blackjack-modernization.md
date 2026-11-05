# Blackjack table modernization — first implementation slice

Branch: feature/manor-modernization-phase-1

The existing WPF Blackjack table remains the normal player route during the
first migration slice. The goal is to change presentation without touching
the casino economy, RNG, settlement, rules, saved progression or hand history.

Delivered:
- A C#-owned, read-only round snapshot covering table rules, presets, dealer,
  player hands, AI seats, stake, bankroll, legal actions and cosmetics. Monetary
  values are transported as exact invariant decimal strings. The dealer hole
  card is not included until settlement; its face-down placeholder carries
  neither rank nor suit.
- A versioned intent parser restricted to the exact packaged Blackjack origin,
  finite wager range, legal command names, session epoch, request UUID and
  expected revision. Unique receipts prevent repeated card or money actions
  on duplicate messages. The parser and session are NOT YET wired to the game.
- A matching TypeScript contract, rejecting malformed responses and snapshots,
  plus C# and TypeScript boundary regression tests.
- A standalone responsive React Blackjack entry point and a live WebView2
  preview, accessible via the Modern Blackjack (Preview) button on the native
  Blackjack sidebar when assets are installed. It invokes the same native
  CasinoSession, wager actions, preset rules and existing settlement handler.
  The default Blackjack lobby route deliberately remains WPF until parity tests
  and the native custom-rule editor have a safe replacement.

Next implementation milestones:
1. Connect the existing CasinoSession and table rule/preset preferences to
   a WebView2 host. Keep processing completed rounds through the original
   progression services. Reject actions which Core says are illegal.
2. Author the playable React table with dealer and player hands, insurance,
   split hands and up to five AI seats. Render optional PixiJS atmosphere
   separately from the DOM-ready event, to avoid the old Empire startup bug.
3. Support the existing custom-rule editor, themes, card-set cosmetics,
   keyboard input, sound and reduced-motion preference.
4. Verify startup/return navigation, 1080x720 and higher DPI windows,
   saved balances and rewards, split/insurance/European no-hole-card paths,
   and no hidden dealer information before switching the default route.
5. Remove native WPF Blackjack visuals only after parity and Windows
   presentation acceptance.

No new branches or main merges; Development publishing continues from
feature/manor-modernization-phase-1.
