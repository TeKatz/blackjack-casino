# Modern Blackjack — WPF table retirement

**Development branch:** `feature/manor-modernization-phase-1`

Blackjack is presented exclusively in React/TypeScript inside the packaged local WebView2 host. The former native WPF Blackjack table, sidebar, controls, table renderer, wager-chip visuals, rule dialog, and visual-fallback routing have been removed. This is the first game area to finish both its *presentation replacement* and the removal of its obsolete WPF presentation. The wider casino shell and other legacy games still use WPF.

## Runtime boundaries

- `BlackjackTableController` owns the live `CasinoSession`, play mode, AI seats, bet, active preset/custom rules, theme, sound and animation settings without any WPF types. Recreating a WebView does not replace the game session.
- `BlackjackCasino.Core` remains authoritative for shoe, game rules, legal actions, RNG, payout and bankroll changes.
- `MainWindow.ModernBlackjack.cs` is a thin native host adapter: it creates the WebView, routes already-validated commands into the controller, commits existing settings/progression, and handles browser disposal.
- `BlackjackModernProtocol` validates exact offline origin, version, bounded parameters, unique request IDs and a session revision. A replay cannot cause a second wager.
- `BlackjackModernSnapshot` projects only visible information, never leaking the dealer's unrevealed card.
- The global native host keeps lifecycle, shared wallet, progression and updater integration until the **remaining games** are migrated.

## Recovery and safeguards

A missing Blackjack frontend cannot fall back to an obsolete game. The lobby stays accessible and the user receives an installation error. If WebView2 fails during play, the host returns to the lobby **without replacing the live C# CasinoSession**. Opening Blackjack again mounts a fresh React host against the preserved hand. No wager is automatically replayed. On actual Windows builds, the native host presents a failure explanation; the automated smoke deliberately avoids blocking on this modal dialog.

This preserves **in-memory** round continuity across a browser-host failure. It does **not** introduce new durable persistence of an unfinished Blackjack hand across application termination; the pre-existing profile and completed-hand save guarantees are unchanged.

## Automated regression

- Dedicated `BlackjackTableControllerTests` cover persisted preset settings, full tables, native rules, bankroll, table reconfiguration, and invalid mutations during a hand.
- `BlackjackModernBridgeTests` cover origin, intent, receipts, custom rule validation and concealed dealer information.
- Windows `ModernBlackjackWebSmoke` exercises the real React route, custom rules, live C# hand settlement, lobby return, re-entry, simulated WebView failure, and a second React re-entry with the same C# session.
- Startup and presentation smoke verify that the retired Blackjack XAML controls are absent.

The legacy table screenshots and wager-chip tests are no longer quality gates for the React implementation; they have been removed with their source and replaced by modern Blackjack coverage. Real Windows keyboard/DPI/audio, long-session WebView memory, and a user-driven error-recovery pass remain manual acceptance items.

This work does **not** remove WPF from the wider casino shell, change game odds, or migrate saved player data to a new format.
