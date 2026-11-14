# Blackjack

The Midnight Club's Blackjack tables run inside the local **React/WebView2** game page (`frontend/blackjack.html`). The active rules, bets, settlement and saved progress are determined by C#, not by the browser. The retired WPF table and fallback were removed during the native cutover.

The original extraction and retirement steps are retained in the [Blackjack modernization archive](archive/blackjack-wpf-retirement.md).

## Playing Blackjack

- Solo play and AI-opponent tables, with configurable rules and presentation settings.
- Hit, Stand, Double, Split, Surrender and Insurance when legal under the table's rules.
- Configurable deck and dealer rules, table preferences and persistent player statistics.
- Native-authoritative wallet balances, shoe state, outcome calculation and payout receipts.
- Offline browser presentation with local sound, visual feedback and keyboard/pointer input.

## Architecture and recovery

`BlackjackTableController` owns the current `CasinoSession` and table state. `BlackjackModernProtocol` validates local page origin, message version, session revision, action parameters and request IDs. The browser sees only information allowed at the current table state, including the dealer's unrevealed card protection.

A failed WebView2 page cannot manufacture a winning result or settle an earlier wager twice. The casino can return to the lobby and rehost the C# session; it does not start an old WPF game window.

Verification includes Blackjack Core unit tests, native protocol/storage smoke and real Windows WebView2 route tests. See [architecture](architecture.md) and [presentation checks](presentation-regression.md).
