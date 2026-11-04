# Chill Lounge · Jigsaw Puzzle

Jigsaw Puzzle is an offline-first, non-wagering, single-player Lounge game. It is registered as a `LoungeHostedGame` and uses the same illustrated `PremiumLoungeTheme` setup style as other Lounge games. The UI has English/German labels.

## Starting a puzzle

1. Open Chill Lounge, then **Jigsaw Puzzle**.
2. Select any motif from the 12 bundled collection illustrations in the **fixed four-column, three-row gallery**; the entire selection screen fits without scrolling.
3. Select a piece count in the **gold-bordered difficulty panel below Statistics**, independently of the motif: **24 (6×4), 48 (8×6), 96 (12×8), 150 (15×10), 300 (20×15), 500 (25×20), 750 (30×25), 1,000 (40×25)**.
4. Select **Start Puzzle**, or **Resume Puzzle** for the previously saved session. Replacing an unfinished session requires confirmation.

Gameplay is deliberately unhurried: no time limit, scoring penalty, wager or online dependency. The timer can be **turned fully off** from the game setup or in the board controls: when off, elapsed time does not advance, the accumulated measurement is cleared, and the timer is hidden. Use the reference toggle to display a faint picture on the target board.

## Controls

- Drag a piece from the paginated tray onto the picture area. A tray item can also be selected and then placed by clicking on the board.
- Drag pieces on the board. Pieces with correctly connected edges move together. Drag loose pieces/groups back onto the tray, or right-click them, to put them away.
- Release a piece/group near its correct location or matching neighbour to join. Once anchored to the final picture, pieces stay fixed.
- Mouse wheel or zoom buttons: zoom. Middle-mouse drag, or Ctrl/Space + left-drag: pan. **Reset View** restores the overview.
- **Reference** toggles a low-opacity image guide.
- The tray is shuffled deterministically for each game, not sorted by solved location. Use the tray arrows or Page Up/Down to browse pieces. Escape deselects a tray item.
- Return to the puzzle menu or Lounge hub at any time; the active session resumes later.

## Implementation

- `Core/Jigsaw/JigsawGame.cs` owns the deterministic grid, canonical solved positions, piece state, snapping and connected components. It also includes catalog entries, durable completion receipts, normalization, statistics, and XP policy.
- `Desktop/JigsawPlayView.cs` is one custom WPF drawing surface instead of 1,000 buttons. It caches piece-shaped `StreamGeometry` and `CroppedBitmap` sprites and **retained `DrawingGroup` layers** for resting and actively dragged groups. During dragging, only the group transform changes; the component is identified once per drag instead of traversed on every mouse move. Rendering and hit tests share the same transform, independent of screen size or zoom. The board host applies a rounded clipping geometry to remove corner artifacts.
- `Desktop/JigsawLoungeModule.cs` draws the four-column, three-row motif gallery with scrolling disabled, a padded gold-bordered piece-count selector, and separated Timer/Achievements controls. The left feature heading and subtitle use a symmetrical, centered layout without changing other Lounge games. Thumbnails use downscaled bundled illustrations; the full-resolution image loads only while playing.
- Piece records store ID, position, whether the piece is out of the tray, and whether its group is locked. Progress lives inside `AppPreferences.Jigsaw` and is normalized on load. The per-puzzle clock setting and global timer preference persist with the session. Moves and periodic elapsed-time updates trigger a save.
- On completion the board transitions to a dedicated result screen with the finished full picture, completion statistics, XP/coin rewards, newly unlocked achievements, and Play Again/Menu buttons.
- **30 tiered achievements** are tracked across all 8 puzzle sizes, 8 completion-count milestones, 6 total-piece milestones, 4 scene-variety milestones and 4 special motifs. Bronze/Silver/Gold/Platinum award 70/160/400/1,000 player XP, half as much Season XP, and 100/250/600/1,500 coins respectively, in addition to standard completion rewards. The profile stores unique achievement IDs so rewards cannot be granted twice.
- Completion IDs are GUIDs. Receipts are first recorded in the preferences file; player/Season XP are then granted through the existing transactional progression service. Saved completions with missing profile receipts are retried when Chill Lounge reopens. Repeating the same receipt is a no-op.
- Player XP for 24/48/96/150/300/500/750/1000 pieces is 100/160/260/400/700/1100/1600/2200; Season XP is half. Time and reference use do not alter XP.
- The **same 30 achievements** are exposed as `Jigsaw Puzzle` in the shared Challenges tab. Their progress is sourced from `JigsawStatistics`, and auto-paid achievements appear as **Awarded** instead of being claimable again. The lobby's ready-challenge summary and Claim All also respect the already-awarded state; no additional packs or duplicate coins are promised.
- **JigsawTests** and **JigsawCentralChallengeTests** cover size grids, positions, cached group movement, timer persistence, shared challenge lookup, exact-once rewards, 30 achievements, 1,000-piece completion, and reward idempotence.
- **JigsawSmoke** opens the Lounge tile, asserts that the selection view has no visible scrollbar, checks the fixed four-column/three-row motif layout, button spacing, timer toggle and achievements gallery, enters a 1,000-piece game, captures the gallery/board/result screen, and verifies resume and Lounge navigation.

The illustrated motifs are reused from the game's existing `Assets/CollectionArt` folder. No internet requests, additional accounts or new external dependencies are introduced.
