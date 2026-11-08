# Midnight Solitaire — React gameplay migration

**Integrated baseline:** `main` · **Development source:** `develop` (historical implementation: `feature/manor-modernization-phase-1`)  
**Presentation:** React in `frontend/lounge.html`  
**Game rules, saves, achievements and payouts:** Existing `BlackjackCasino.Core.Solitaire` and progression services

## Complete Lounge variants

The normal Midnight Solitaire entry now renders a single React cabinet with eight individual play surfaces covering six rule families:

| Variant | Existing rule and layout |
| --- | --- |
| Klondike | Seven cascading columns, draw-one stock and waste, four suit foundations |
| FreeCell | Eight open columns, four free cells, four foundations; reproducible numbered deals |
| Spider | Ten cascading columns, stock deals ten cards at a time; 1, 2 or 4 suits, completed King–Ace runs |
| Pyramid | 28-card triangle, exposed 13-value pairs, exposed kings removed alone, stock/waste |
| Golf | Seven five-card columns, one-higher-or-lower waste matching, no Ace/King wrapping |
| TriPeaks | Three overlapping peaks, uncovered one-higher-or-lower waste matching |

Cards are real controls with selected runs, gold selection outlines, readable suit/rank symbols, face-down artwork, drag/drop and click-target movement, and double-click-to-foundation on supported variants. The current collection card set supplies the face/back/border palette. Responsive layouts scroll at narrow widths without losing card positions. The design respects `prefers-reduced-motion`.

The native C# `SolitaireGame` remains the only authority for shuffle, move capacity, exposing covered cards, legal runs, stock recycling, completions and full victory. The React implementation performs **no** independent rule or payout calculation.

## Persistent boundary

The browser sends only `lounge.solitaire.command` with a GUID request/session, monotonically increasing revision, and a whitelisted intent: `new`, `resume`, `draw`, `activate`, `auto`, `move`, `back`, `exit`. All pile IDs and card indices are bounded. The host accepts only the exact packaged Lounge origin and never receives a forged card, shuffle, game state, XP award or currency value from JavaScript. Duplicate requests return the original native receipt, stale sessions are rejected.

A move is first applied to a deeply cloned `SolitaireGameState`, saved through the pre-existing `SolitaireProgressState` and the regular preferences writer, and only then installed as the live board. Unsuccessful I/O leaves both board and save unmodified. A completed or abandoned deal uses the existing C# idempotent progression records, preserving achievements, wallet rewards and statistics. The stopwatch checkpoints elapsed seconds at ten-second intervals and when leaving a game.

The save schema and normal/deterministic deal numbers are unchanged. All eight table choices have independent active slots.

## Acceptance and remaining cleanup

- Vitest validates the native snapshots, card/rank/suit bounds, the eight table configurations and receipt shapes.
- Native xUnit checks the intent allow-list, one-time receipts, stale revision and session defense, full six-variant C# execution, numbered FreeCell determinism, save-failure rollback and abandonment.
- Actual WebView2 startup smoke opens the React Solitaire cabinet, checks Klondike stock draw and durable resume, starts numbered FreeCell #617, exercises all Spider suit counts, Pyramid, Golf, and TriPeaks, captures table screenshots and navigates back through the React Lounge.
- The earlier WPF Solitaire smoke remains as comparison/rollback coverage, **not** as the normal route.

The old `SolitaireLoungeModule`, `SolitaireHubControl`, `SolitaireView`, WPF table art and their dependency chain are still retained for emergency fallback and historical comparison. Removing them requires a separate, validated cleanup task; the shared WPF host itself also remains. This phase does **not** claim WPF is eliminated from the app.

## Central challenges and statistics (2026-11-07)

Midnight Solitaire now participates in the central, claimable Challenge Board.
The 22 additional milestones use only existing durable Solitaire statistics:
distinct started games, receipt-verified wins, individual variants, four-suit
Spider mastery, the six-variant collection and longest winning streak. Wins
from a repeated completion receipt do not count again. The older standalone
Solitaire achievements remain available and are not silently converted to coins.

The modern Statistics screen exposes the accumulated win rate, win/abandon
counts, all six variants, 1/2/4-suit Spider results, records and completion
time. Statistics are read directly from `PlayerProfileState.Solitaire`, not
reconstructed from local browser state. Existing history counts retroactively.
