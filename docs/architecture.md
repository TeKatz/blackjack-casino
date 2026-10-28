# Architecture

The repository is deliberately split into independent layers from the first commit.

## `BlackjackCasino.Core`

Pure blackjack domain logic with no UI, networking or filesystem dependencies. The engine owns cards, hand evaluation, shoe behavior, round state, legal player actions and settlement.

The UI must not reimplement blackjack rules. This keeps future table rendering, AI players and training modes on one authoritative rules engine.

## `BlackjackCasino.Update`

UI-independent update protocol:

- semantic version parsing/comparison;
- JSON update manifest validation;
- HTTPS package download;
- exact size validation;
- SHA-256 integrity verification.

The stable manifest is published as a GitHub Release asset named `update-manifest.json`.

## `BlackjackCasino.Updater`

Separate Windows process used to replace the installed application after the main process exits. It:

1. waits for the old process;
2. verifies the downloaded package again;
3. extracts into a sibling staging directory;
4. validates that the expected restart executable exists;
5. moves the old installation to a backup directory;
6. moves staging into place;
7. restores the backup if the swap fails;
8. restarts the game.

The main game copies the updater executable to `%TEMP%` before launching it, so the helper is not trying to overwrite itself.

## `BlackjackCasino.Desktop`

Thin WPF shell for Phase 1. It exists now so the complete release/update pipeline is exercised before the actual casino table UI is added.

Player saves and settings will live outside the installation directory (under the user's application-data directory), so replacing application files cannot destroy player data.

## Release contract

A stable GitHub release contains:

- `blackjack-casino-win-x64.zip`
- `blackjack-casino-win-x64.zip.sha256`
- `update-manifest.json`

A tag such as `v0.1.0` drives `.github/workflows/release.yml`, which builds, tests, publishes the self-contained Windows application and updater, computes the package hash, writes the manifest and creates the GitHub Release.

The updater protocol is schema-versioned. `minimumUpdaterVersion` allows us to stop an old updater from consuming a future incompatible package format.
