# Sudoku Lounge – phase 2

**Historical delivery phase.** The described assets and mechanics have been integrated into the current native React Sudoku game. The numbered phase is not a separate selectable game version. See [Sudoku](sudoku.md) and [current architecture](architecture.md).

## Playable variants

| Mode | Board | Rules | Difficulty |
| --- | --- | --- | --- |
| Classic | 9×9 | Original nine blocks, six legacy difficulties, offline daily puzzle | Relaxed through Master |
| Sudoku X | 9×9 | Original row, column and block rules plus both nine-cell diagonals | Easy / Medium / Hard |
| Killer Sudoku | 9×9 | No duplicate digits in each cage, each cage must sum to its printed total; original row, column and block constraints | Easy / Medium / Hard |
| Jigsaw Sudoku | 9×9 | Every irregular contiguous region contains all nine digits; rows and columns unchanged | Easy / Medium / Hard |
| Samurai Sudoku | 21×21 footprint, 369 active cells | Five intersecting 9×9 Sudoku grids with shared overlapping cells | Easy / Medium / Hard |
| Mini Sudoku | 4×4 or 6×6 | Rows, columns and 2×2 or 2×3 blocks respectively | Easy / Medium / Hard |

Sudoku games are accessible from a single six-tile Sudoku Lounge gallery. At a
1920×1080 desktop viewport the hero and both rows of previews fit the content
viewport without scrolling. The mode tiles have individually authored SVG
illustrations whose digits differ by variant. Clicking Sudoku Career opens a
scrollable 100-level milestone summary, with 10 regular cosmetic titles plus the
additional premium level-90 and ultra-premium level-100 title. Unlocked and
locked statuses come only from the native profile's Sudoku career level.

## Data integrity and gameplay

All puzzles and original artwork are bundled offline. The variant factory
produces deterministic, locally generated Sudoku layouts from valid templates
and rule-preserving transforms. It does not scrape or redistribute third-party
puzzles. X, Jigsaw and Mini puzzles undergo actual constraint-solver uniqueness
checks; the Killer cage generator uses a checked no-duplicate cage partition,
and its resulting puzzles are verified for unique solutions. Samurai grids use
individually solvable original classic seeds with bijective digit alignment in
the four shared regions. The native definitions validate the complete solution
and every row/column/box/region/diagonal/cage group.

The native C# layer owns every solution, move, hint, clock, generated puzzle
index, completion receipt and reward. Only incomplete grid, givens, notes,
regions and cage sums appear in the browser snapshot. The WebView2 protocol
allowlists variant game intentions, enforces session/revision receipts and
restricts cell and digit indices. Moves are persisted before changing the
running game state, allowing a failed save to be retried without awarding
duplicate rewards. Each mode/difficulty/mini-size keeps its own save.

There are variant-appropriate grids, editable cells, notes, erase, undo, redo,
hint, timer, completion feedback, keyboard navigation and a back-to-mode route.
Samurai provides independent zoom and scroll access to the full board.

## Progression, challenges and statistics

Each first-time completed variant puzzle grants separately balanced Casino
Player XP, Season XP and **Sudoku Career XP**; the completion receipt prevents
repeat payments. Sudoku Career advances toward level 100, granting 25 existing
cosmetic points for every level-up. Titles remain non-purchasable in the global
Cosmetic Shop and become globally equippable at their milestone level.

Nineteen localized new global Sudoku achievements track each of the five
variants, 4×4 and 6×6 Mini, all-variant completions, no-hint completions and
career progress through level 100. Existing global Sudoku and Chill Lounge
completion milestones also count newly completed variants.

The statistics page now includes five additional Sudoku variant cards and
adds career XP/level/coins to the Classic Sudoku entry. The committed receipt
store tracks per-mode completion counts, difficulty breakdown, hints and times.

## Verification

- `SudokuVariantTests.cs` validates generators, constraints, clue consistency,
  uniqueness, persistent saves, resumable hints, double-reward prevention and
  native command authorization.
- `sudokuVariantBridge.test.ts` validates browser snapshot constraints and the
  18 mode/difficulty selections.
- `ModernLoungeSmoke.cs` exercises the real Windows WebView2 UI through
  the Sudoku Career modal, Killer cage entry, save/resume and each variant's
  actual board dimensions before proceeding through Classic Sudoku's legacy
  functionality.
