# Performance update — Phase 1

Phase 1 focuses on routine persistence latency without changing gameplay, save formats, or transactional guarantees.

## Implemented first slice

- Routine `AppPreferences` writes are coalesced for 350 ms.
- The latest state in a burst replaces older pending routine snapshots.
- JSON is serialized before background dispatch so mutable nested preference state is not read concurrently.
- Physical routine file I/O runs outside the UI call path.
- Writes still use temp-file + atomic replace semantics.
- `Load()` flushes pending routine state first, preserving read-after-save behavior.
- `SaveOrThrow()` remains synchronous and durable and flushes older deferred routine state first.
- Application shutdown flushes the final pending preferences snapshot.
- Midnight Manor and other transactional persistence paths retain their existing durable semantics.

## Safety constraints for the remaining Phase 1 work

- Do not change save-file schemas.
- Do not make financial/progression transaction boundaries best-effort.
- Do not background-serialize mutable profile graphs without an explicit stable snapshot.
- Add regression coverage before broadening deferred persistence to additional stores.
