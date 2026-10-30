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
