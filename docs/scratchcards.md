# Lucky Sevens scratchcards (Development)

Scratchcards are a **sidebar destination in the casino lobby**, directly after Collections and before Shop. They are not a separate casino-mode tile or a separate WebView2 route. The React page uses the established emerald, brass and etched-pictogram lobby presentation.

## Current gameplay

- One $20 play-money ticket: **Lucky Sevens**.
- Each ticket has nine covered fields. Three matching symbols **anywhere** win its predetermined prize.
- Users scratch each field with pointer/touch input or reveal it using keyboard controls; a **Reveal all** accessibility option is available.
- A ticket may be purchased only when the previous one has been collected. Purchasing debits the shared Bankroll immediately; collecting pays any prize exactly once.
- A purchased but not collected ticket is restored on reopen/restart. The symbol pattern and payout never reroll on a retry.
- Browser preview has no native wallet and never enables real purchases. German and English lobby text are provided.

## Prize distribution

| Payout | Probability |
| ---: | ---: |
| $0 | 50% |
| $10 | 20% |
| $20 | 15% |
| $50 | 10% |
| $100 | 4% |
| $250 | 0.9% |
| $1,000 | 0.1% |

Expected payout is $17.25 per $20 ticket, or **86.25% theoretical return**. All currency is virtual offline play money. There are no real-money payments or prizes.

## Ownership and recovery

The C# Core `ScratchcardRules` class chooses prizes through `RandomNumberGenerator.GetInt32` and arranges a matching or nonmatching board **before** the visual effect. The native controller handles `lobby.scratchcards.open|close|buy|collect` commands with validated GUID ticket IDs. The React/WebView2 bridge is presentation only; it never sets the balance, tickets or prize.

The `native-scratchcards-checkpoint.json` write-ahead journal stores the candidate wallet and ticket receipts before mirroring the wallet to the existing player-state store. Incomplete purchase/collection commits are recovered on startup, and previously collected IDs are replay-safe. An uncertain persistence write requires restart before further scratchcard transactions.

## Verification

Core tests cover payout thresholds, non-accidental losing patterns, valid winning patterns, purchase/collection idempotence, insufficient bankroll, and interrupted commits. Frontend tests cover native message validation and initial/active ticket rendering. Windows host/UI smoke, real pointer interaction at DPI scales and the published Development artifact still require validation on Windows.
