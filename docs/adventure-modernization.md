# Tales from the Backroom — React/WebView2 modernization

**Current:** NativeShell + local React/WebView2; Development source: `dev` (Stable is separately tagged from `main`).

## Scope

Three public adventures (Black Stag, Drowned Bell, Dead Man's Hand), with the retired Strangers' Wager preserved solely for old saves and encounter regression. Native AdventureEngine, AdventureProgressState v4, class catalog, character XP, stats, inventory, status effects, D20 checks, caseboard, two tabletop encounters and idempotent AdventureProgression remain unchanged. React now presents the gallery, class picker, narrative choices, sheet, inventory, investigation board, native Blackjack/Five-of-a-Kind, result and soundtrack preferences.

## Native-authoritative protocol

ModernAdventureProtocol validates exact same-origin WebView2 messages, allowlisted actions, request and session GUIDs, expected revisions, field constraints and repeat receipts. The browser does not decide RNG, prizes, evidence discovery or card outcomes. ModernAdventureService operates on detached C# progress copies and persists them before replacing live state; completed adventures reuse existing one-time progression ledgers. Older run IDs and schema remain compatible with WPF fallback.

As with the previous WPF encounter surface, an incomplete ephemeral tabletop hand is not saved as a durable deck on application restart; the original story scene is preserved and can be re-entered safely. This is not a game-state schema migration.

## Automated tests

- ModernAdventureBridgeTests: protocol, stale/replay safety, native start/resume, D20, save rollback, Blackjack with dealer hole card, Five-of-a-Kind AI, secret clue protection.
- adventureBridge.test.ts: snapshots for catalog, character, story, check, caseboard, masked dealer card, completion and receipts.
- ModernAdventureSmoke: real packaged Windows WebView2 start, story, native choice, character sheet, saved resume and UI snapshots.
- The C# adventure engine tests and real Windows WebView2 navigation cover current behavior; WPF-only smoke tools are retired.

## Remaining signoff gates

Verify CI and published Development ZIP at the final SHA, then run device testing for all public campaign endings, two encounter types, 100–200% DPI, keyboard/screenreader caseboard, German translations, soundtrack volume, reduced motion, long sessions and crash/restart. The WPF host and WPF-only UI sources have already been removed.
