# Sudoku — React gameplay migration

**Development branch:** `feature/manor-modernization-phase-1`  
**Normal presentation:** React/TypeScript within `frontend/lounge.html`  
**Game rules and persistence:** `BlackjackCasino.Core.Sudoku` and the existing .NET profile store

## Gameplay parity

The React Lounge opens the Sudoku library without detaching or replacing its WebView2. The six normal difficulty pools keep independently resumable boards; a new puzzle removes only unfinished work in the same tier. The daily puzzle uses `SudokuDailySchedule.ForDate` and existing date-to-puzzle assignments. Already completed puzzles remain read-only and their rewards are never issued again.

The board has 81 buttons with real 3×3 block boundaries, given-cell protection, immediate native correctness and conflict feedback, matching-number and row/column/block guides, pencil notes, completed-digit disabling, selected-cell erase, native hint reveal/repair, undo/redo and keyboard controls. Timer visibility and guides use the existing user preferences. Elapsed time comes from the native monotonic stopwatch; checkpoints are saved periodically and on every move, game departure and host close. Reduced-motion CSS is respected.

## Strict frontend/backend boundary

Only `ModernSudokuService` can select puzzles, validate digits, resolve hints, determine completion, advance the timer or persist a board. The browser sends `lounge.sudoku.command` with protocol v1, an ephemeral session GUID, the current revision, a unique request GUID and one whitelisted intent:

- `sudoku.new`, `sudoku.resume` with one named difficulty
- `sudoku.daily`, `sudoku.back`, `sudoku.exit`
- `sudoku.value`, `sudoku.note` with a cell index and a digit
- `sudoku.erase`, `sudoku.hint` with a bounded cell index
- `sudoku.undo`, `sudoku.redo`
- `sudoku.guides`, `sudoku.timer` with a boolean preference

The WebView2 host accepts messages only from the exact local `https://midnight-manor.local/lounge.html` origin, with a 2-KiB limit. Invalid, stale, wrong-session or duplicated requests cannot commit a second move. Results are sent as receipts followed by the new authoritative snapshot. React never transmits its own grid, a solution, currency, XP or a payout.

Board mutations are computed on a cloned `SudokuBoard` that reuses the original native solver result (no repeated solve on each move), then stored through `SudokuProgressState.SaveGame` and the existing app preference persistence before the live board is swapped. An uncertain I/O outcome is never automatically retried by the browser. Puzzle completion is recorded using the existing Sudoku completion ledger; duplicate loads, browser restarts and hints do not repeat player or Season XP.

## Migration verification

- Native xUnit tests: allow-listed protocol validation, revision/session/request replay safety, persistence failure rollback and completion semantics.
- Frontend Vitest: full state and board shape validation, forged-grid and note rejection, receipt shape checks.
- Windows startup smoke: real React Lounge navigation and Sudoku, six difficulties and Daily availability, 81-cell board, notes, undo/redo, native wrong-number/hint feedback, saved resume and return navigation. Screenshots in the startup smoke artifacts.
- Legacy WPF Sudoku smoke continues to check original rollback behavior until native source deletion.

## Remaining quality and cleanup gates

The old `SudokuView`, `SudokuPremiumMenu` and associated native Lounge tests remain for emergency rollback and comparison only; they are not on the normal route. Before deletion, perform manual DPI, mouse/keyboard, accessibility, German/English, long-session and reduced-motion acceptance on actual Windows devices, then remove the WPF source and convert any test coverage not yet exercised against the React DOM. This is separate from the general WPF host removal.
