# Chill Lounge: Sudoku

Sudoku is the first game in the Chill Lounge and is deliberately offline-first.

## Puzzle policy

- The application never generates a player-facing Sudoku at runtime.
- Free Play and Daily use completely separate puzzle pools.
- Every bundled puzzle must have exactly one solution before it is accepted.
- Difficulty is metadata on the bundled puzzle; selecting a difficulty never mutates or procedurally derives a grid.
- The initial source catalog in this branch is deliberately small enough to review while the gameplay and persistence contract stabilise. The intended content target is thousands of puzzles in each pool.

The preferred bulk source is the Sudoku Exchange Puzzle Bank (`grantm/sudoku-exchange-puzzle-bank`), which is dedicated to the public domain. Its puzzles were generated ahead of time with QQWing, checked for a unique solution and graded with Sukaku Explainer. Importing that bank into this repository is a content-build step, not runtime generation. The final bundled subset must still pass the repository's own uniqueness validator.

## Offline Daily

The Daily is selected from the dedicated Daily pool using only the local calendar date and a deterministic permutation of that pool. No server or network request is required.

The current schedule epoch is `2026-10-30`. Within one complete Daily pool cycle, the schedule does not repeat a puzzle. A daily assignment is also persisted by date once the player opens it, so later changes to scheduling code do not rewrite that player's recorded assignment.

Clock manipulation is intentionally not treated as cheating. The Lounge has no competitive online rank and should not add intrusive anti-cheat logic for local calendar changes.

## Save model

`SudokuProgressState` is stored with the rest of local `AppPreferences`.

Each saved puzzle records:

- puzzle ID
- full current board
- pencil-note bitmasks
- elapsed time
- number of hints used
- completion state

Puzzle givens are authoritative when a save is restored. Corrupt or stale values cannot overwrite a given.

## Current first slice

The first slice includes:

- Lounge hub entry
- six Free Play difficulty labels: Relaxed, Easy, Medium, Hard, Expert and Master
- separate Free Play and Daily catalogs
- 9×9 play board
- mouse and keyboard number entry
- row/column/box highlighting
- conflict highlighting
- pencil marks
- erase
- undo/redo
- one-cell hints
- informational timer
- autosave after moves
- resume support
- deterministic offline Daily selection
- solver/uniqueness regression tests

Later slices should expand the curated catalog substantially, add Daily history/calendar browsing, completion statistics and progression rewards without making solve time part of the reward formula.

## Lounge menu and completion XP

The Chill Lounge opens a game-selection menu before the Sudoku library. `LoungeGame`
registrations supply an ID, text, artwork factory and route; further games can add a
registration in `LoungeWindow` without changing the menu renderer. The header's
Lounge Games button preserves and disposes any active Sudoku before returning.

Completed puzzles now earn player XP / active-season XP:

| Difficulty | Player XP | Season XP |
| --- | ---: | ---: |
| Relaxed | 150 | 75 |
| Easy | 250 | 125 |
| Medium | 400 | 200 |
| Hard | 600 | 300 |
| Expert | 850 | 425 |
| Master | 1200 | 600 |

Hints, timer visibility and solve speed do not reduce rewards. Free Play and Daily
use the same difficulty scale. The completion receipt and XP are saved atomically
in the profile. Already recorded puzzles do not pay again, including historical
completions recorded before this update. An unsuccessful profile write remains
retryable on reopening the lounge. Ten additional Sudoku challenges bring the
category to 21; all goals remain free of speed or streak pressure.

The original 74 BPM **Velvet Sidewalks** score replaces Quiet Pages: swung hats,
dusty kick/snare, soft electric keys, sub bass and tape texture, synthesized offline.
