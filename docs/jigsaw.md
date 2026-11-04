# Chill Lounge · Jigsaw Puzzle

Jigsaw Puzzle is an offline-first, non-wagering, single-player Lounge game. It is registered as a `LoungeHostedGame` and uses the same illustrated `PremiumLoungeTheme` setup style as other Lounge games. The UI has English/German labels.

## Starting a puzzle

1. Open Chill Lounge, then **Jigsaw Puzzle**.
2. Select any motif from the 12 bundled collection illustrations.
3. Select a piece count independently of the motif: **24 (6×4), 48 (8×6), 96 (12×8), 150 (15×10), 300 (20×15), 500 (25×20), 750 (30×25), 1,000 (40×25)**.
4. Select **Start Puzzle**, or **Resume Puzzle** for the previously saved session. Replacing an unfinished session requires confirmation.

Gameplay is deliberately unhurried: no time limit, scoring penalty, wager or online dependency. The timer is informational. Use the reference toggle to display a faint picture on the target board.

## Controls

- Drag a piece from the paginated tray onto the picture area. A tray item can also be selected and then placed by clicking on the board.
- Drag pieces on the board. Pieces with correctly connected edges move together.
- Release a piece/group near its correct location or matching neighbour to join. Once anchored to the final picture, pieces stay fixed.
- Mouse wheel or zoom buttons: zoom. Middle-mouse drag, or Ctrl/Space + left-drag: pan. **Reset View** restores the overview.
- **Reference** toggles a low-opacity image guide.
- Use the tray arrows or Page Up/Down to browse parts. Escape deselects a tray item.
- Return to the puzzle menu or Lounge hub at any time; the active session resumes later.

## Implementation

- `Core/Jigsaw/JigsawGame.cs` owns the deterministic grid, canonical solved positions, piece state, snapping and connected components. It also includes catalog entries, durable completion receipts, normalization, statistics, and XP policy.
- `Desktop/JigsawPlayView.cs` is one custom WPF drawing surface instead of 1,000 buttons. It caches piece-shaped `StreamGeometry` and `CroppedBitmap` sprites. Rendering and mouse hit tests share the same coordinate transform, independent of screen size or zoom.
- `Desktop/JigsawLoungeModule.cs` draws gallery cards and setup controls using existing Lounge design tokens. Thumbnails use downscaled bundled illustrations; the full-resolution image loads only while playing.
- Piece records store ID, position, whether the piece is out of the tray, and whether its group is locked. Progress lives inside `AppPreferences.Jigsaw` and is normalized on load. Moves and periodic elapsed-time updates trigger a save.
- Completion IDs are GUIDs. Receipts are first recorded in the preferences file; player/Season XP are then granted through the existing transactional progression service. Saved completions with missing profile receipts are retried when Chill Lounge reopens. Repeating the same receipt is a no-op.
- Player XP for 24/48/96/150/300/500/750/1000 pieces is 100/160/260/400/700/1100/1600/2200; Season XP is half. Time and reference use do not alter XP.
- **JigsawTests** cover size grids, position persistence, normalization, free-group movement, 1,000-piece completion and reward idempotence.
- **JigsawSmoke** opens the Lounge tile, checks all motifs and sizes, enters a 1,000-piece game, captures the gallery and board, and verifies resume and Lounge navigation.

The illustrated motifs are reused from the game's existing `Assets/CollectionArt` folder. No internet requests, additional accounts or new external dependencies are introduced.
