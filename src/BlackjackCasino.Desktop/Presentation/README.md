# Presentation layer

Phase 4 owns the desktop game's visual and audio polish here. Casino audio is generated deterministically in `CasinoFeedback.cs` as project-owned PCM WAV data at runtime, so the game uses no Windows system sounds and has no external audio/license dependency.

The current cues cover card movement, chip placement, win, loss and push. They are intentionally small and replaceable if we later add recorded studio assets.
