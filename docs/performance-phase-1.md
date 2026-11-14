# Performance update — Phase 1

**Historical performance-phase record.** This page documents a specific optimization and its original measurement scope. It is not a guarantee of current FPS or GPU/CPU use. Consult [current presentation validation](presentation-regression.md) and the [quality roadmap](modernization-roadmap.md).

Phase 1 focuses on routine persistence latency without changing gameplay, save formats, or transactional guarantees.

## Implemented first slice

- Routine `AppPreferences` writes are coalesced for 350 ms.
- Routine player-profile and hand-history writes use independent 350 ms debounce queues.
- The latest state in a burst replaces older pending routine snapshots.
- JSON is serialized before background dispatch so mutable nested preference/profile state is not read concurrently.
- Physical routine file I/O runs outside the UI call path.
- Writes still use temp-file + atomic replace semantics.
- `Load()` / `LoadProfile()` / `LoadHistory()` flush their respective pending routine state first, preserving read-after-save behavior.
- Critical `SaveOrThrow()` / `SaveProfileOrThrow()` paths bypass deferred persistence and cannot be overwritten later by an older pending snapshot.
- Application shutdown flushes final pending preferences, profile, and hand-history snapshots.
- Midnight Manor and other transactional persistence paths retain their existing commit semantics.

## Safety constraints for the remaining Phase 1 work

- Do not change save-file schemas.
- Do not make financial/progression transaction boundaries best-effort.
- Do not background-serialize mutable profile graphs without an explicit stable snapshot.
- Add regression coverage before broadening deferred persistence to additional stores.
