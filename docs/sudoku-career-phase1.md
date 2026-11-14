# Sudoku Lounge – phase 1

**Historical delivery phase.** The described assets and mechanics have been integrated into the current native React Sudoku game. The numbered phase is not a separate selectable game version. See [Sudoku](sudoku.md) and [current architecture](architecture.md).

- Six custom offline SVG illustrations: Classic, X, Killer, Jigsaw, Samurai, Mini. All six modes are now playable after the completed Phase 2; each preview features a distinct number arrangement.
- Classic remains fully playable with six difficulties and the existing daily Sudoku.
- Separate 100-level Sudoku career starting at level 1; XP curve per step: 100 + 9n + floor(n²/8), n = current level - 1.
- Only unique, committed Sudoku completions grant career XP; normal casino player and seasonal XP remain separate.
- On every level-up, 25 existing cosmetic coins are earned (99 ascents, 2,475 coins total).
- Ten milestone titles at levels 10 through 100, plus a separate level-90 animated premium and level-100 animated ultra-premium title (12 in total).
- All twelve titles appear in the regular Cosmetic Shop with their Sudoku-level requirement but cannot be bought. After unlocking they can be equipped throughout the casino.
- Phase 2 is implemented: five rule-aware offline engines, resumable puzzles, global challenges and per-variant statistics. The full specification is in [sudoku-variants-phase2.md](sudoku-variants-phase2.md).
