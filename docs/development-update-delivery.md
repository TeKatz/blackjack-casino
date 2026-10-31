# Development update delivery

The in-app Development manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The original Manor PR only uploaded a CI artifact. Its `slots/midnight-manor` branch was outside the Development publisher's triggers, leaving the public channel at `0.6.5-dev.71` from main. A final `0.6.5` also sorts above any `0.6.5-dev.N` under SemVer. Changing a URL or comparison only in a new executable cannot repair an already-installed client.

At the user's request, the Manor branch was explicitly included in the Development workflow, and later feature branches have taken over the same mutable Development channel. The normal main publisher stays enabled; no Stable release is required for Development test delivery.

Before publication, the workflow validates localization resources, builds, runs all tests and the actual Windows WPF smoke, and packages the application with its separate updater. It then verifies the release target and required assets. Package/manifest filenames, SHA-256 verification and updater schema are unchanged. Mutable-manifest requests send Cache-Control: no-cache, no-store to revalidate subsequent checks. The UI shows installed/available versions when no newer update is found.

Versioning rule: start a new Development cycle on a version base strictly above the latest final version. Preserve SemVer ordering; do not offer silent downgrades. PR preview artifacts use dev.0 and are superseded by the published Development run number.

## Current test channel: English / German localization

The shared `development` release is pinned to `feature/localization-en-de`. This branch starts directly from the merged `main` state containing Midnight Solitaire and the in-app Casino Vault. Pushes to the localization branch run the Development publisher automatically; manual dispatch on this workflow revision from another branch is skipped. The in-app manifest URL is unchanged, so installed clients only need to select Development and check for updates.

Versions continue as `0.10.1-dev.N` using the existing increasing workflow run number. The publisher builds and tests the complete application before replacing the mutable release. Package SHA-256, size and application/manifest version checks remain enabled. Release notes and the manifest notes identify the exact source commit.

The localization update introduces a central English fallback resource set and a complete German companion resource set for every currently migrated key. A persistent Language selector is added to the Presentation settings with `English` and `Deutsch`. Changing language applies immediately without restarting and also switches the .NET formatting culture used by localized formatting paths.

During migration, existing WPF controls can resolve their original English text to a localization key at runtime. The bridge also watches later Text/Content changes, so already-visible controls do not silently fall back to English when game state changes. New or substantially edited screens should use localization keys directly rather than adding new hard-coded display strings.

`tools/check_localization.py` is a mandatory CI and Development-publisher gate. Every translated resource file must contain exactly the neutral English key set and every value must be non-empty. English remains the fallback when a runtime key cannot be resolved in a selected translation. Proper product and mode names such as The Midnight Club, Midnight Solitaire, Midnight Manor and Neon Vault remain untranslated unless explicitly changed by product direction.

The source pin lives in the publishing workflow's push trigger and job guard. When changing the active feature branch again, update both fields together on the selected branch. Normal feature-branch CI previews remain available as artifacts and do not replace the shared Development release unless that branch is explicitly pinned here.
