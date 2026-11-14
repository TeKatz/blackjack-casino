# Performance Phase 6 — Hand-history persistence

**Historical performance-phase record.** This page documents a specific optimization and its original measurement scope. It is not a guarantee of current FPS or GPU/CPU use. Consult [current presentation validation](presentation-regression.md) and the [quality roadmap](modernization-roadmap.md).

Phase 6 removes the remaining full hand-history JSON serialization from the routine blackjack round-settlement path while preserving the existing history schema and persistence behavior.

## Previous hot path

`PlayerProgressionController.ProcessRound` calls `SaveAll()` after every newly processed blackjack round. `SaveAll()` persists both the profile and hand history.

Phase 1 already deferred the physical history file write, but `PlayerProfileStore.SaveHistory` still performed all of the following on the caller/UI thread for every round:

1. sort the complete history by `CompletedAt`;
2. retain the newest 1,000 entries;
3. serialize the complete retained history to an indented JSON string;
4. schedule only the final string for deferred file I/O.

As the history grew, the sort, JSON traversal and large UTF-16 string allocation remained part of round settlement.

## Phase 6 design

### Typed debounced persistence

`DebouncedPersistenceWriter<TPayload>` generalizes the existing coalescing writer while the original string writer remains as a compatibility facade for profile/preferences paths.

The hand-history writer now accepts a `RoundHistoryEntry[]` snapshot rather than a serialized string. Repeated saves inside the 350 ms debounce window still coalesce to the newest state.

### Stable caller-thread snapshot

`SaveHistory` creates a bounded stable snapshot before handing work to the background writer. This is intentional: the live controller history and its nested lists are never serialized concurrently.

Each retained round copies:

- `GameRules`;
- dealer cards;
- player hands and their card arrays;
- action history;
- AI seats, hands and card arrays.

This keeps background serialization isolated from any later mutation of caller-owned collections.

### Fast chronological path

The normal progression-controller history is already chronological. Phase 6 detects that ordering with a linear pass and copies only the newest 1,000 entries directly.

Arbitrary unsorted callers retain the previous compatibility behavior: stable `OrderBy(CompletedAt)` followed by the newest 1,000 entries.

### Worker-side streaming JSON

After the debounce delay, the worker serializes the stable snapshot directly to `hand-history.json.tmp` through a `FileStream`, then atomically replaces `hand-history.json`.

This removes the large intermediate JSON string from routine history saves.

`LoadHistory` likewise deserializes directly from a file stream after flushing pending history persistence.

## Preserved behavior

- `hand-history.json` schema is unchanged.
- History remains capped at 1,000 persisted entries.
- Persisted entries remain chronological.
- Unsorted input retains the old ordering semantics.
- Routine history failures remain best-effort and cannot interrupt gameplay.
- Reads and shutdown still flush pending writes first.
- The temporary-file + replace write pattern is retained.

## Regression coverage

Phase 6 adds coverage for:

- typed debounced payload coalescing;
- chronological histories larger than 1,000 entries;
- unsorted-input compatibility ordering;
- isolation of nested history collections after snapshot capture.
