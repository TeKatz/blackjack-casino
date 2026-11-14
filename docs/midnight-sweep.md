# Midnight Sweep — Chill Lounge

**Route:** `minesweeper` (React Chill Lounge #8). **Current host:** NativeShell; Development source: `dev`.

## Gameplay

Midnight Sweep is a no-wager minefield with a bespoke moonlit observatory presentation:

| Difficulty | Grid | Hazards |
| --- | --- | --- |
| Stargazer / Sternenblick | 8 × 8 | 10 |
| Nightfall / Nachteinbruch | 12 × 12 | 24 |
| Event Horizon / Ereignishorizont | 16 × 16 | 40 |

The first click is guaranteed safe **including its eight neighbors**. Hazards are placed only after the first reveal with cryptographic random placement; the clicked tile is therefore a zero, so the adjacent clear area opens naturally. Opening a zero expands until numbered boundary tiles. Right-click or use the Mark tool to set flags. Clicking an already revealed numbered tile chords the neighborhood **only when the adjacent flag count matches**. A wrong guess can still hit a mine. All non-hazard cells must be opened to win. Flags need not be correct to win.

### Echo Scan

Each run has one optional Echo Scan once the first turn has been made. A scan is safe, does not count as a reveal and does not open a tile. It displays the **inclusive 3 × 3 local hazard count**, including the scanned cell itself when hazardous. This differentiates the ability from ordinary neighboring clues, while preserving uncertain reasoning. A no-scan victory unlocks its own challenge chain.

## Architecture

- `BlackjackCasino.Core/Minesweeper/MinesweeperGame.cs`: validated typed field, hazard generation, flood-fill, flag and chord rules, terminal completion, immutable snapshot boundary, timer, saved-run recovery.
- `BlackjackCasino.Desktop/ModernMinesweeperService.cs`: commit-before-display persistence and reopening terminal receipts.
- `BlackjackCasino.Desktop/ModernMinesweeperProtocol.cs`: allow-listed, revision-guarded, bounded commands and request-id receipts; duplicated requests cannot apply twice.
- `NativeCasinoRuntime` / native Lounge bridge: per-run `AppPreferences.Minesweeper` checkpoint, terminal `PlayerProfileState.Minesweeper` statistics, and correct lifecycle on closing the WebView2 mode.
- `frontend/src/lounge/minesweeperBridge.ts`: validates browser snapshots and refuses unauthorized hazard disclosure.
- `frontend/src/lounge/minesweeper.tsx`, `SweepField.tsx` and `minesweeper.css`: React game menu, accessible grid overlay, procedural vector PixiJS lighting and particle FX, responsive controls, win/loss overlay.
- `frontend/src/lounge/minesweeperAudio.ts`: original local WebAudio synth cues (reveal, mark/unmark, echo, chord, start, win, loss); no network audio and no other mode's soundtrack assets copied.

Hazards are **not sent to React** until the native game has reached `won` or `lost`. Before then the browser sees only revealed numbers, markers and the result of the single Echo Scan. Rules, clocks, outcome and score are never client authoritative.

## Global progression

`SweepStatistics` records wins, losses, wins by difficulty, no-scan wins, best/current win streak, safe tiles revealed in completed runs, Echo Scans used and best win time. Terminal run GUIDs are stored in `CompletedRunIds`, so reloads, retries and delayed UI messages do not multiply milestone progress.

Fourteen global challenges cover first and cumulative wins, expert clears, clean no-scan victories, streaks, and total revealed safe cells. Their names and descriptions are localized in English and German and appear in the existing Challenges and Statistics sections. Midnight Sweep is a relaxed free-play mode; it does not create wagers or pay casino credits for a game result. Existing challenge rewards still follow the global rules.

## Testing and maintenance

C# core tests verify safe first turns, hazard masking, scan semantics, restoration of seeded fields, terminal receipts and challenge progress. Vitest checks the browser-side dimensions, hazard validation, response contract and eighth menu entry. The repository migration audit compares the C# catalog, React menu and TypeScript route list, all eight in the same order.

As with other WebView2 modes, check a published Windows build before accepting graphical performance and audio-device behavior.
