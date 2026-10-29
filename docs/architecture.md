# Architecture

The repository is split into UI-independent domain logic, desktop presentation and update/install components. Blackjack, slots, progression and persistence share infrastructure where appropriate, but game-specific rules and statistics remain separated.

## `BlackjackCasino.Core`

UI-independent domain logic. It owns:

- Blackjack cards, hands, shoes, rules, legal actions, AI table play and settlement;
- Midnight Reels progression integration contracts;
- Midnight Manor rules, math and session state;
- Neon Vault rules, symbol weighting, 1,024-Ways evaluation, features and jackpot resolution;
- player progression, levels, Prestige, Season state, challenges and cosmetic catalog metadata;
- normalized statistics models and compatibility helpers.

The desktop UI must not reimplement outcome rules. Presentation may animate or anticipate an already-resolved result, but it cannot change Blackjack settlement or slot outcomes.

## `BlackjackCasino.Desktop`

The WPF desktop project is the visible casino shell. It owns:

- the casino lobby and mode registry;
- Blackjack table presentation and configurable table setup;
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
- Neon Vault.

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

`.github/workflows/development-release.yml` publishes successful development builds from `main`, `develop`, active `feature/**` / `fix/**` branches and the retained Midnight Manor slot branch. Builds use versions such as `0.1.0-dev.143` and replace the assets of the mutable GitHub prerelease tagged `development`.

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
