# Scratchcard series (Development)

Scratchcards are a **sidebar destination in the casino lobby**, directly after Collections and before Shop. They are not a separate casino-mode tile or a separate WebView2 route. The React page uses the established emerald, brass and etched-pictogram lobby presentation.

## Current gameplay

- Two play-money series: **Lucky Sevens** ($20) and **Gold Rush** ($100). A single arrow beside the first ticket advances to Gold Rush; later series add corresponding arrows in both directions.
- Each ticket has nine covered fields. Three matching symbols **anywhere** win its predetermined prize.
- A single continuous pointer/touch gesture can scratch across **all nine fields** and their gaps. Keyboard controls still reveal individual fields; a **Reveal all** accessibility option is available.
- Only one ticket across all series may be active at once. Navigation may preview another series, but purchasing remains disabled until the active ticket is collected. A ticket may be purchased only when the previous one has been collected. Purchasing debits the shared Bankroll immediately; collecting pays any prize exactly once.
- A purchased but not collected ticket is restored on reopen/restart. The symbol pattern and payout never reroll on a retry.
- Browser preview has no native wallet and never enables real purchases. German and English lobby text are provided.

## Rubbel- und Gewinnpräsentation

The silver coating is removed with a **25-pixel brush** in the ticket's 170 × 105 source-coordinate cells. Completion uses measured transparent-area coverage (at least **50%**), not accumulated cursor travel; repeatedly scratching a small line cannot reveal the field. The grid handles pointer capture and maps each stroke into every intersected field, including fast movements across the spaces between cells. Pointer and touch movement shed lightweight silver particles that fall and fade around the scratching position. Particle emission is throttled, contained within the ticket and disabled by the lobby animation/reduced-motion preference.

Once all nine fields are visible, winning tickets highlight only their **three matching cells** with a gold pulse and animated shine. The result strip, win marquee and matching payout-table row are also emphasized. An original Web Audio victory bell phrase plays once on a user's winning reveal; it can be muted via the scratchcard page's own Sound button. Muting and reduced-motion preferences do not alter tickets, payouts or game state. Browser audio failures are non-fatal.

## Prize distribution

| Lucky Sevens payout | Gold Rush payout | Probability |
| ---: | ---: | ---: |
| $0 | $0 | 50% |
| $10 | $50 | 20% |
| $20 | $100 | 15% |
| $50 | $250 | 10% |
| $100 | $500 | 4% |
| $250 | $1,250 | 0.9% |
| $1,000 | $5,000 | 0.1% |

Lucky Sevens has a $17.25 expected payout per $20 ticket; Gold Rush has an $86.25 expected payout per $100 ticket. Both series have an **86.25% theoretical return**. All currency is virtual offline play money. There are no real-money payments or prizes.

## Ownership and recovery

The C# Core `ScratchcardRules` class holds the authoritative per-game prices and multiplier, and chooses prizes through `RandomNumberGenerator.GetInt32` and arranges a matching or nonmatching board **before** the visual effect. The native controller handles `lobby.scratchcards.open|close|buy|collect` commands with validated GUID ticket IDs. The React/WebView2 bridge is presentation only; it never sets the balance, tickets or prize.

The `native-scratchcards-checkpoint.json` write-ahead journal stores the candidate wallet and ticket receipts before mirroring the wallet to the existing player-state store. Incomplete purchase/collection commits are recovered on startup, and previously collected IDs are replay-safe. An uncertain persistence write requires restart before further scratchcard transactions.

## Responsive presentation and ticket switching

On the standard 1920×1080 casino lobby, the hero, ticket art, win banner, status and action row use compact desktop dimensions so the post-win state fits vertically without requiring page scrolling. Smaller viewports retain a scrollable fallback. Ticket navigation uses animated left/right arrows on either side of the selected card and honors reduced-motion preferences.

The reveal state is paired with an immutable ticket ID. The previous ticket's revealed mask or matching prize row is never rendered during the first frame of a newly bought ticket. The native journal's `GameId` property defaults to `lucky-sevens` when loading old receipts without that field. No save migration or resettlement is performed.

## Verification

Core tests cover payout thresholds, non-accidental losing patterns, valid winning patterns, purchase/collection idempotence, insufficient bankroll, and interrupted commits. Frontend tests cover native message validation and initial/active ticket rendering. Windows host/UI smoke, real pointer interaction at DPI scales and the published Development artifact still require validation on Windows.
