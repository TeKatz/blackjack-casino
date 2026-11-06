# Architecture

The repository is currently split into UI-independent domain logic, WPF desktop presentation and update/install components. Blackjack, slots, progression and persistence share infrastructure where appropriate, but game-specific rules and statistics remain separated.

## `BlackjackCasino.Core`

UI-independent domain logic. It owns:

- Blackjack cards, hands, shoes, rules, legal actions, AI table play and settlement;
- Midnight Reels progression integration contracts;
- Midnight Manor rules, math and session state;
- Neon Vault rules, symbol weighting, 1,024-Ways evaluation, features and jackpot resolution;
- Nine Lives, Midnight Dice, Jigsaw, other Chill Lounge games, Midnight Merge, Empire and adventure rules/state;
- player progression, levels, Prestige, Season state, challenges and cosmetic catalog metadata;
- normalized statistics models and compatibility helpers.

The desktop UI must not reimplement outcome rules. Presentation may animate or anticipate an already-resolved result, but it cannot change Blackjack settlement or slot outcomes.

## Modern Chill Lounge boundary

`MainWindow.Lounge.OpenLoungeCore` normalizes the established C# progress records and routes straight to `OpenModernLounge` when bundled WebView2 is available. `ModernLoungeCatalog` supplies the six stable game IDs and native read-only status summaries; it is independent of all WPF `LoungeHostedGame` factories. The WebView2 host owns only presentation and validated session/revision/receipt commands, with C# services handling every save and reward. A failed or unavailable WebView2 explicitly calls `OpenLegacyLounge`, which is the sole initializer for native WPF Lounge modules and their visual decorators.

The retired native startup-welcome dialog is not shown during normal startup, but remains as regression-reference source. The remaining native WPF game modules provide tested emergency/fallback behavior and should remain until recovery, accessibility and device acceptance is complete. The [legacy inventory](legacy-wpf-inventory.md) describes which files are still reachable and why.

## `BlackjackCasino.Desktop`

The WPF desktop project remains the native host for the React casino lobby and existing game UIs. It owns:

- the casino lobby and mode registry;
- native WebView2 hosting for the fully modern Blackjack table; `BlackjackTableController` supplies UI-independent session state and rules;
- embedded hosting for Midnight Reels, Midnight Manor and Neon Vault;
- profile/progression navigation with an in-app back stack;
- custom casino controls, themes, audio and animation preferences;
- local save stores and migration orchestration;
- bankroll/progression hand-off between the shared shell and individual game sessions.

### Embedded game host

Slot modes are authored as game windows for lifecycle/resource isolation, but their visual content is rehosted inside the main casino window. The hidden shell keeps lifecycle semantics while the player sees only one native application window and one taskbar entry.

The host also:

- preserves game-local resource dictionaries before reparenting;
- disables covered Blackjack actions while another mode is active;
- forwards unconsumed keyboard input to the embedded game;
- restores the lobby/header state when the embedded mode closes;
- keeps presentation scaling bounded across supported window sizes.

### Presentation performance

Dynamic WPF presentation avoids rebuilding large visual trees per frame. Reel symbols and other repeating visuals are reused and updated in place; generated Neon Vault symbol artwork is decoded once, frozen/cached and then reused by the 20 reel cells. Expensive effects are reserved for feature moments rather than continuous reel updates.

## Progression and statistics

Global progression is shared across casino modes, while verified per-game statistics are separated:

- Blackjack;
- Midnight Reels;
- Midnight Manor;
- Neon Vault;
- Nine Lives and additional typed game-mode statistics/achievements where supported.

Older saves created before Reels and Neon Vault were distinguishable may contain pooled slot history. That history is preserved as legacy/earlier-slot history for totals instead of being guessed into one modern slot bucket. See [progression tracking migration](progression-tracking-migration.md).

## Persistence

Player data lives outside the installation directory so application replacement cannot destroy saves. The main stores include:

- `player-state.json` — bankroll and ordinary application/game preferences;
- `profile.json` — progression, cosmetics and aggregate/per-game statistics;
- `hand-history.json` — capped Blackjack round history;
- Midnight Manor persistence for pending feature/session state.

Save compatibility is handled by normalization/migration code rather than by silently discarding unknown or older state.

## `BlackjackCasino.Update`

UI-independent update protocol:

- semantic version parsing/comparison;
- Stable and Development channels;
- JSON update manifest validation;
- HTTPS package download;
- exact size validation;
- SHA-256 integrity verification.

Both channels use the same versioned manifest schema and package contract. Stable resolves through GitHub's latest non-prerelease release. Development resolves through the mutable `development` prerelease.

## `BlackjackCasino.Updater`

Separate Windows process used to replace the installed application after the main process exits. It:

1. waits for the old process;
2. verifies the downloaded package again;
3. extracts into a sibling staging directory;
4. validates that the expected restart executable exists;
5. moves the old installation to a backup directory;
6. moves staging into place;
7. restores the backup if the directory swap fails;
8. restarts the game.

The main game copies the updater executable to `%TEMP%` before launching it, so the helper is not trying to overwrite itself.

## Release contract

Every published channel exposes the same assets:

- `blackjack-casino-win-x64.zip`
- `blackjack-casino-win-x64.zip.sha256`
- `update-manifest.json`

### Stable

A SemVer tag such as `v0.1.0` drives `.github/workflows/release.yml`, which builds, tests, publishes the self-contained Windows application and updater, computes the package hash, writes the manifest and creates an immutable GitHub Release.

The client reads:

```text
https://github.com/TeKatz/blackjack-casino/releases/latest/download/update-manifest.json
```

GitHub excludes prereleases from `latest`, so Development cannot accidentally become Stable.

### Development

`.github/workflows/development-release.yml` publishes successful development builds from the explicitly pinned branch, currently `feature/manor-modernization-phase-1` for the long-lived Manor pilot. Both its push trigger and job guard select that source. Builds use versions such as `0.10.1-dev.N` and replace the assets of the mutable GitHub prerelease tagged `development`.

The client reads:

```text
https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json
```

This gives testers an explicit opt-in channel while keeping Stable deterministic.

## Regression layers

The repository uses several complementary checks:

- unit tests for rules, progression, persistence and update behavior;
- shared-control template guards;
- wager geometry/presentation checks;
- Windows casino presentation regression captures across multiple window sizes;
- Midnight Manor runtime smoke coverage;
- preview and Development publish verification.

These checks intentionally cover both domain correctness and WPF integration because many regressions in a casino UI only appear after controls are rehosted, animated or resized.

## Protocol compatibility

The updater protocol is schema-versioned. `minimumUpdaterVersion` allows an old client/updater to be rejected before it consumes a future incompatible package format.

## Modern presentation and staged shell migration

The staged migration now includes the React casino lobby, playable Midnight Manor, shop, challenges, statistics, Season Pass, profile identity editor, Blackjack hand history, Casino Vault, Collector's Album and Casino Empire. Empire has its own validated, session-deduplicated bridge to the existing transaction/preview services and uses the same modern host in embedded and detached windows; its economy remains in Core. The presentation adapter lives inside the WPF host via WebView2; only after every migrated mode, album, updater and save path passes acceptance would a smaller native .NET host replace WPF. The planned frontend is not allowed to generate outcomes, change the bankroll or own durable rewards. C# Core remains authoritative; any new application/bridge service must have versioned command schemas, origin validation and idempotent settlement. See [modernization roadmap](modernization-roadmap.md) and [agent working agreement](../AGENTS.md).

After consolidation, do not delete branches automatically or change the Development publisher's pin without an explicit release decision. The current cumulative release branch and workflow instructions are documented in [Development delivery](development-update-delivery.md).


## Blackjack native presentation retirement (2026-11-06)

Blackjack has no WPF table, sidebar, custom-rules dialog or WPF wager renderer. The live C# session and table preferences are owned by `BlackjackTableController` and projected into React via the versioned Blackjack protocol. Browser failure closes the modern view without replacing the session; reopening mounts a new React browser against the same Core hand. The outer WPF application remains until the other games and window-level services can be migrated. See [Blackjack modernization](blackjack-modernization.md).

## Nine Lives presentation boundary (2026-11-06)

`NineLivesSession` and `SlotProgression` remain C# authorities. The native `MainWindow.ModernNineLives` adapter runs each spin and commits the shared wallet, saved preferences and rewards before sending a read-only `ModernNineLivesSnapshot`. `ModernNineLivesProtocol` accepts only enumerated wagers, toggles and spin intents from the exact packaged `nine-lives.html` origin, with an idempotent request/epoch/revision ledger kept outside WebView2. Browser failure cannot reroll or drop bonus spins during in-memory recovery. React/PixiJS owns all Nine Lives game visuals. The old Nine Lives WPF Window was removed, while lobby preview art and remaining native host infrastructure are retained.

## Neon Vault UI boundary (2026-11-06)

`NeonVaultSession` is the sole C# RNG/paytable/bonus engine. The `MainWindow.ModernNeonVault` adapter commits each native result to the shared wallet, progression and profile before sending a `ModernNeonVaultSnapshot` to the offline local React page. The validated `ModernNeonVaultProtocol` rejects untrusted origins, invented wagers, stale revisions and duplicate request receipts. Its epoch and session survive WebView2 failure together with pending native free spins. The old WPF Neon Vault window and reel renderer have been removed. Browser feature effects cannot affect outcomes.

## Midnight Merge gameplay/presentation boundary (2026-11-06)

The original `BlackjackCasino.Core.MidnightMerge` catalog, item definitions, orders, 7×6 board and schema-2 progression are unchanged. `MergeBoard` persists the candidate atomic snapshot to the current `PlayerStateStore` **before** publishing a successful mutation; a failure leaves the prior state untouched. The `MainWindow.MidnightMerge` adapter hosts the local React/PixiJS page, commits every spawn/drop/delivery into C#, calls existing profile ledger synchronization and credits milestone bankroll rewards. `ModernMergeSnapshot` is a localized, read-only projection. `ModernMergeProtocol` accepts only exact origin / versioned requests with a session GUID and expected durable board revision; its retained receipt ledger prevents duplicated in-memory WebView commands. No client-provided reward, score, item definition or random outcome is authoritative. On browser loss, the C# board/ledger survive in memory and rehosting sends a new snapshot without a replay. The retired WPF `MidnightMergeView` game renderer is absent from the play route; the WPF emergency lobby preview remains.

## Midnight Dice native rules and React presentation (2026-11-06)

The lobby's Midnight Dice destination is the locally packaged `frontend/midnight-dice.html` page in `ModernDiceView`. React renders the felt table, five original cosmetic palettes, all thirteen selectable scoring categories, settings, separate casual/casino game setup, AI standings and rank receipts. PixiJS is cosmetic only. The existing WPF game components remain in source for controlled rollback but are not a selectable primary route.

The native `MainWindow.ModernDice` adapter exclusively calls `KniffelGame` and, for the paid four-seat mode, `KniffelCash`/`KniffelTable`. Every roll, hold, score or fee/casino settlement checkpoint is committed through the existing atomic player-state persistence before its result is projected. Player and Season XP, leaderboard statistics, challenges and any shared-cash payouts remain C#-authoritative. `ModernDiceSnapshot` is read-only; the React caller can only propose valid roll/hold/category, mode and preference intents. `ModernDiceProtocol` enforces an exact local URL, version, session, command GUID and monotonic revision; a receipt ledger survives a browser failure in the native host. The original schema and old save IDs remain compatible. The view can be reopened against the same committed checkpoint without replaying a dice roll, AI turn, fee or award.
