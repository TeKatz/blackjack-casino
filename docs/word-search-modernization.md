# Word Search — Words Discovered mode

**Current:** NativeShell + local React/WebView2; Development source: `dev` (Stable is separately tagged from `main`).
**Presentation:** React in the shared `frontend/lounge.html`  
**Authoritative gameplay and persistence:** `BlackjackCasino.Core.WordSearch`

## Puzzle library: 96 per difficulty

The catalog contains **576 deterministic puzzles**, split across six retained
difficulty modes. The grid sizes, per-puzzle word counts and rewards are
unchanged.

| Difficulty | Grid | Target words | Existing puzzles | New puzzles | Total |
| --- | --- | --- | --- | --- | --- |
| Relaxed | 8×8 | 6 | 48 | 48 | 96 |
| Easy | 9×9 | 7 | 48 | 48 | 96 |
| Medium | 10×10 | 8 | 48 | 48 | 96 |
| Hard | 11×11 | 10 | 48 | 48 | 96 |
| Expert | 12×12 | 12 | 48 | 48 | 96 |
| Master | 13×13 | 14 | 48 | 48 | 96 |
| **Total** | | **5,472 placements** | **288** | **288** | **576** |

The previously published **IDs 001–048** at every difficulty retain the
original theme banks, seed formula, deterministic grid algorithm, completion
receipts and XP. Added indices **049–096** are appended after those levels,
using twelve new bilingual themes with four variants per difficulty:
Arctic Expedition, Rainforest, Clockwork City, Treasure Island,
Music Studio, Farmstead, Underwater Kingdom, Grand Library, Robotics Lab,
Spring Meadow, Space Colony and Sky Kingdom. Their banks collectively add
over 770 English and German target candidates. German terms transliterate
umlauts into ASCII (AE/OE/UE); grids use uppercase A–Z. Existing in-progress
grids remain byte-for-byte unchanged when reloaded or switched to a different
UI language, including older releases.

The six old progress counts, challenge IDs, rewards and completed-puzzle
receipts remain stable. Previously final 288-puzzle and 2,736-word milestones remain claimable as
intermediate progression. New bilingual endgame challenges reward 576 solved
puzzles, 5,472 target-word finds, 400 no-hint completions, and 96 Expert /
96 Master completions, without modifying earlier claim IDs or rewards.

## Words Discovered collection

The Word Search library prominently features a **Words Discovered** button.
Its album shows the unique words actually found in puzzles, with the first
known theme, difficulty and date. A search field and theme filter make the
collection browseable. Words discovered in German and English remain in the
form shown when actually found; exact identical A–Z spellings count once,
regardless of language, theme or repeat completions.

An optional additive `DiscoveredWords` list is stored in the existing
`WordSearchProgressState` preferences object. The schema version stays
at 1 for compatibility with old save readers. New unique entries are
recorded **in the same save-first transaction** as `FoundWords`, before
React sees a found-word result. Failed writes neither credit the album nor
play a discovery sound. Completion records and XP remain independently
idempotent. Normalization deduplicates and validates stored album entries.

For previous saves, only the verifiable word list in an active unfinished
puzzle is migrated into the collection. Older completed-game receipts
contain only a word count and puzzle ID, with no original saved language
or word list; claiming old individual discoveries would therefore invent
words. Earlier completion rewards, stats and challenge progress are
preserved unchanged.

The active native service sends a one-time `LastFind` event containing the
saved word, first-discovery flag and event ID. React shows a small animated
purple **New** beside the crossed-out target word when this flag is true;
repeat finds only cross out the word. Events do not replay when revisiting
an older puzzle or refreshing the snapshot. The badge is cosmetic, fading
from view after a short display. The original native PCM audio engine plays
a short, soft pluck for every successful saved word, with a distinct
three-note chime for the first addition. Sound respects the existing
Chill Lounge sound preference, while badge animation obeys reduced-motion
and the lounge animation preference. No browser-created audio device, wallet
command or network dependency is introduced.

## Game integrity and validation

The native service alone confirms selected words, hints, puzzle completion,
album growth, time checkpointing and rewards. Browser commands may only
submit two valid grid endpoints or choose a difficulty; they cannot claim a
word, award XP or add to the album. Browser snapshots validate the album,
find receipt and six 96-level tiers. Native tests cover all 576 English and
German grids, old deterministic IDs/seeds, album deduplication, save
round-trips, failure-before-save, replays and sound-wave quality. Windows
WebView2 smoke covers opening the album, revealing and persisting a new
word, returning/resuming a puzzle, completing it and leaving the Lounge.

Real-device high-DPI and subjective sound/UI balance still require player
review. The old WPF Word Search fallback was retired; the React game and native
C# persistence now own the full play and resume flow.
