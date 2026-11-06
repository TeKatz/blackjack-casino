# Word Search — React migration and expanded vocabulary

**Development branch:** `feature/manor-modernization-phase-1`  
**Presentation:** React in the shared `frontend/lounge.html`  
**Authoritative engine:** `BlackjackCasino.Core.WordSearch`

## Expanded catalog

The catalog previously contained 24 deterministic puzzles per difficulty. It now has 48 in each of the six existing difficulty tiers:

| Difficulty | Grid | Words per grid | Old levels | Added | New total |
| --- | --- | --- | --- | --- | --- |
| Relaxed | 8×8 | 6 | 24 | 24 | 48 |
| Easy | 9×9 | 7 | 24 | 24 | 48 |
| Medium | 10×10 | 8 | 24 | 24 | 48 |
| Hard | 11×11 | 10 | 24 | 24 | 48 |
| Expert | 12×12 | 12 | 24 | 24 | 48 |
| Master | 13×13 | 14 | 24 | 24 | 48 |
| **Total** | | **2,736 target words** | **144** | **144** | **288** |

The **original 144 definitions are not rewritten**: indices 001–024 at every difficulty use the same IDs, original 12 themes, word banks, seeds, generator and completion semantics. This preserves deterministic old puzzles, existing active-grid saves, completed puzzle IDs, earned XP and bankroll receipts.

Indices 025–048 introduce twelve further themes with two deterministic variants each:

Mountain Trails, Garden Paths, Winter Cabin, Desert Journey, Wildlife, Space Lab, Ancient Ruins, Night Market, Lakeside, Workshop, Summer Festival and Mythic Tales.

Every new theme has a dedicated **English and German** word bank with roughly 26 candidate terms. German umlauts are transliterated (for example `UE`, `AE`) so the hidden grids continue to use only uppercase A–Z. The original 12 themes remain playable. Across all themes the total variety expands significantly without introducing network dependencies or making a puzzle depend on the browser's locale after it has been started.

The full 288-puzzle progression remains replayable after completion; the next-unsolved selection walks each difficulty's old puzzles first and then its new ones. Previous challenge IDs, receipts and rewards are retained, while new milestones cover 288 puzzles, 2,736 words, 48 Expert, 48 Master and 200 no-hint completions. The original 144-puzzle target is now a milestone rather than the terminal goal.

## Gameplay

The React interface includes all six difficulties, current progress and reward tiers, a resumable in-progress puzzle, 8×8–13×13 responsive grids, directional selection with mouse/touch dragging or two endpoint clicks (also keyboard activation), native found-word highlighting, a word list that checks off discoveries, starting-letter hint spotlight, a local timer and a completion result. There are no penalties for hint usage or time taken. Motion effects honor `prefers-reduced-motion` and the Lounge animation preference.

The game permits a single active puzzle at a time, exactly like the former WPF implementation. Players can leave and resume it in any chosen UI language; the original generated grid and list are persisted as one coherent state. Completing the puzzle releases the active slot and records its original one-time C# reward.

## Strict browser/native boundary

Browser commands are limited to `word-search.start`, `word-search.resume`, `word-search.select`, `word-search.hint`, `word-search.back`, `word-search.exit`, with a GUID session and request ID, monotonic expected revision, bounded straight-line cell endpoints, and exact packaged origin. No solution, word text, XP, bankroll, game state, time or puzzle seed may be supplied as an authoritative browser input.

Only C# executes `WordSearchGame.TryFind`, chooses hints, generates local deterministic puzzles and stores completion records. Every move is first simulated against a detached state and saved with the existing app preferences writer before the visible native state changes. Failed persistence leaves the live word set and hint count unchanged; retries must be initiated by the player, not automatically by the browser. Completion awards use the existing idempotent progression and wallet path.

## Regression and remaining cleanup

- Native tests verify all English and German grids, 288 unique IDs, 48 per difficulty, stable generation, old save reload, expanded challenge limits and native completion idempotence.
- Protocol and service tests verify rejected inputs, replay/stale-session safety, save-first word discovery, hint persistence, cross-language resume and completion.
- React tests validate state/grid/placement shapes, six 48-level tiers and receipt validation.
- Windows WebView2 startup smoke tests the modern Word Search library, an actual letter grid, native word submission, hint marker, auto-saved resume, all-word completion, XP feedback and return through the modern Lounge.
- Legacy WPF Word Search smoke remains for rollback coverage. Its old screen is no longer on the regular user route, but the shared desktop host remains WPF.

**Open quality gate:** real-device DPI, accessibility, animation, drag/touch and audio review before deleting the WPF fallback implementation. Memory, Jigsaw and Tales from the Backroom have not yet been migrated.
