# Memory — full React gameplay migration

**Branch:** `feature/manor-modernization-phase-1`  
**Frontend:** `frontend/lounge.html` and `frontend/src/lounge/memory.tsx`  
**Native game:** `BlackjackCasino.Core.Memory`

## Table and deck parity

All five original Memory difficulty levels retain the C# table and reward settings:

| Difficulty | Table size | Cards | Pairs | Player XP |
| --- | --- | --- | --- | --- |
| Easy | 4×4 | 16 | 8 | 100 |
| Normal | 4×5 | 20 | 10 | 160 |
| Hard | 6×6 | 36 | 18 | 260 |
| Expert | 6×8 | 48 | 24 | 420 |
| Master | 8×8 | 64 | 32 | 650 |

Every mode offers the original four decks: **Casino Icons**, **Card Suits**, **Animals**, and **Fruits & Treats**. Each includes 32 distinct paired symbols. The cabinet offers a deck picker, decorative card previews and retained native best-move records; selecting a deck persists that preference without changing any game in progress. Native display-only German translations are reused for the deck descriptions and card names; IDs are not localized.

The game UI shows custom card-back lattice/club branding, themed card faces, face-up and matched states, match-streak feedback, moves, accuracy, misses, elapsed time and found pairs. It supports mouse and keyboard clicks, reduced-motion settings and the existing Lounge sound toggle. Card flip/match/mismatch/perfect-game cues reuse the WPF-era C# synthesis without moving audio authority into JavaScript. Incorrect pairs remain visible for a short moment and are then resolved by the **native** host. There is no countdown or XP penalty for time or mistakes.

## Native authority and persistence

The React browser can only submit these intentions:

- `memory.deck` with an allow-listed deck ID.
- `memory.start` with one of the five native difficulties.
- `memory.resume`, `memory.again`, `memory.back`, `memory.exit`.
- `memory.flip` with a bounded index 0–63.

Each intent requires a valid GUID session and request ID, increasing revision and the exact trusted packaged WebView2 origin. No browser command can submit a shuffled list, hidden card, seed, rule result, match, coin/XP award or player profile.

**Concealed-card privacy:** C# projects the deck with the identifying `glyph`, `name` and `accent` set to `null` for every face-down, unmatched card. Only genuinely opened or matched cards expose these fields. React never receives the full native ordered `Cards` list or the random seed; the list of public preview symbols is independent of game order.

Every first/second flip, completion, chosen deck, mismatch resolution, and elapsed-time checkpoint persists a detached C# copy before replacing the live session. If the preferences writer fails, the previous committed board, found pairs, counters and saved state remain unchanged. One existing saved Memory table can be resumed across language changes without rebuilding/shuffling its cards. Completed games retain the original GameId-based idempotent progression receipt. `MemoryProgression` grants exactly-once player/season XP, and the original bankroll/challenge record system remains unchanged. The React result panel shows perfect/missed status, moves, accuracy, streak, elapsed time and replay.

## Acceptance and remaining work

- C# xUnit: original rule/reward tests plus all 20 difficulty/deck combinations, retained card seeds/IDs, privacy, strict commands and replay/revision rejection, disk-write failure, mismatch resolution and one-time completion.
- TypeScript/Vitest: five table specs, all four decks, hidden card projection, validation of card counts and strict GUID receipts.
- Windows WebView2 smoke: opening the actual Memory cabinet, selecting Animals, starting 4×4, verifying face-down symbols, flipping an initial card, saving/resuming it, finding an actual native pair, counting and resolving a wrong pair, finishing every pair, displaying the completion card and playing again.
- Legacy WPF Memory smokes stay intact for comparison/rollback. Production navigation to Memory now stays in React. The shared WPF host and its emergency fallback have **not** been removed.

**Next Lounge conversions:** Jigsaw and Tales from the Backroom.
