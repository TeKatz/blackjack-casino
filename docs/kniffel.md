# Midnight Dice

**Midnight Dice** is the casino's five-dice, Kniffel-style game, playable for relaxation or at a paid table against **three AI opponents**. Its current interface is a local React/PixiJS page in NativeShell, backed by the C# dice rules and saved receipts.

## Game modes

- **Casual:** Play a complete 13-category score sheet without wagering virtual credits.
- **Casino table:** Pay an entry fee, play against three AI players and receive prizes according to the final results. Entries and settlements use the shared casino bankroll.
- **Save and resume:** Existing ongoing games, score sheets, statistics and challenge progress are preserved.

## Playing and scoring

Roll five dice up to three times per turn. Hold selected dice between rolls, then record a legal category or scratch it for zero. The interface shows possible scores and keeps completed boxes locked. The underlying C# rules remain the established German-style Kniffel contract, including its additional-five-of-a-kind scoring.

The shop contains cosmetic dice sets. Equipped visual designs affect the display, not the random rolls or scoring. German and English UI, animation/audio preferences, and completion summaries are supported.

## Safety and testing

The browser sends only validated roll, hold and score intents. C# resolves each result, persists it and returns a receipt; browser refreshes cannot award the casino entry/prize twice. Windows smoke and .NET unit tests cover AI turns, exactly-once cash and XP accounting, saves, forfeit and recovery.

The former WPF dice view and WPF-only feedback smoke were retired. Their design and migration history are preserved in the [historical Midnight Dice implementation](archive/midnight-dice-history.md). See [architecture](architecture.md).
