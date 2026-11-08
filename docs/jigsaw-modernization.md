# Jigsaw Puzzle — Modern React/PixiJS migration

Integrated baseline: `main`; Development source: `develop` (historical implementation branch: `feature/manor-modernization-phase-1`).
Normal presentation: frontend/lounge.html
Authoritative rules: BlackjackCasino.Core.Jigsaw
Status: modern gameplay integrated; latest CI and manual device/visual acceptance still required.

## Implemented

The twelve original offline collection motifs and all eight original sizes (24, 48, 96, 150, 300, 500, 750 and 1,000 pieces) are unchanged. The React gallery has a saved motif/size selector, timer toggle and thirty native achievement records. The game renders a PixiJS sprite board in the original 960 by 600 logical coordinates, with a 32-piece paginated tray, pointer placement and drag, native connected-group movement/snap, right-click tray return, mouse-wheel zoom, Ctrl/middle-button pan, reset and reference display. Result and Resume screens reuse original saves and rewards. Reduced-motion styles are included.

The frontend currently uses rectangular atlas sprites for piece artwork. **Interlocking jigsaw silhouettes still require parity with the former WPF rendering before final visual signoff.** That change must not alter C# coordinate or achievement semantics.

## Native protocol and save compatibility

The C# ModernJigsawService retains JigsawGame, JigsawProgressState and JigsawProgression. It validates allow-listed game intents and rejects invalid or stale revisions, non-GUID session and request IDs, invalid coordinates, unknown pieces and extra wallet/results. The WebView2 host additionally validates the exact local page origin. Browser data cannot set awards, completion or save schema.

Each legal move mutates a detached C# copy and is persisted before it becomes live. Rejected or failed saves keep the last committed game. Completed puzzles persist their original completion receipt before granting player XP, Season XP, coins and achievements; the existing native idempotency and re-entry recovery continue to apply. The legacy WPF Jigsaw implementation remains for fallback comparison until full acceptance.

## Reproducible builds

A complete frontend/package-lock.json is committed. CI, presentation regression, Modern Frontend, Development Release and Stable Release workflows use npm ci with Node 22 and pinned dependencies. No application update manifest or save migration is required.

## Automated verification

- Core JigsawTests retain the 1,000-piece grid, grouping and one-time progression checks.
- ModernJigsawBridgeTests cover bounded protocol, stale/replayed requests, all eight sizes, native snap/tray, persistence failure rollback, completion and state serialization.
- Frontend jigsawBridge.test.ts checks the twelve original offline images, eight sizes, thirty achievements, malformed piece snapshots and receipts.
- ModernJigsawSmoke executes real WebView2/PixiJS Jigsaw opening, 24-piece board, native piece placement, return and resume; it adds screenshots to the startup-smoke artifact.
- The previous WPF JigsawSmoke remains a rollback benchmark.

## Remaining device-level acceptance

Test keyboard and pointer accessibility, canvas interlocking outlines, 1,000 simultaneous pieces, DPI 100/125/150/200 percent, GPU/VRAM/CPU usage, long sessions, reference clarity and screenreader navigation. Once accepted, migrate any remaining legacy test coverage and remove the old WPF Jigsaw view without modifying C# game logic.
