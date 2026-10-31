# Development update delivery

The in-app Development manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The original Manor PR only uploaded a CI artifact. Its `slots/midnight-manor` branch was outside the Development publisher's triggers, leaving the public channel at `0.6.5-dev.71` from main. A final `0.6.5` also sorts above any `0.6.5-dev.N` under SemVer. Changing a URL or comparison only in a new executable cannot repair an already-installed client.

At the user's request, the Manor branch is explicitly included in the Development workflow, and the next feature version is `0.7.0-dev.N`. Both installed final 0.6.5 and its Development builds detect this through their existing comparator and URL. The normal main publisher stays enabled; no main merge or Stable release is needed for this Development test delivery.

Before publication, the workflow builds, runs all tests and the actual Windows WPF smoke, and packages the application with its separate updater. It then verifies the release target and required assets. Package/manifest filenames, SHA-256 verification and updater schema are unchanged. Mutable-manifest requests now send Cache-Control: no-cache, no-store to revalidate subsequent checks. The UI shows installed/available versions when no newer update is found.

Versioning rule: start a new Development cycle on a version base strictly above the latest final version (e.g. 0.7.0-dev.N after final 0.6.5). Preserve SemVer ordering; do not offer silent downgrades. PR preview artifacts use dev.0 and are superseded by the published Development run number.

## Current test channel: Midnight Solitaire

The shared `development` release is pinned to `feature/chill-lounge-solitaire`. This branch starts from the completed Casino Vault branch after the shared Chill Lounge infrastructure was merged and adds the first reusable hosted Lounge game on top. Pushes to this branch run the Development publisher automatically; manual dispatch on this workflow revision from another branch is skipped. The in-app manifest URL is unchanged, so installed clients only need to select Development and check for updates.

Versions continue as `0.10.1-dev.N` using the existing increasing workflow run number. The publisher builds and tests the complete application before replacing the mutable release. Package SHA-256, size and application/manifest version checks remain enabled. Release notes and the manifest notes identify the exact source commit.

Midnight Solitaire contains six variants: Klondike, FreeCell, Spider, Pyramid, Golf and TriPeaks. FreeCell uses reproducible numbered deals; Spider supports one-, two- and four-suit play. Running games autosave and resume, while starting a replacement deal records an abandonment instead of treating a normal Lounge exit as a loss. Career statistics, XP, Season XP and permanent achievements use durable completion receipts so a reload cannot duplicate rewards.

Solitaire reuses the existing card presentation and equipped card-set cosmetics. Time and move counts are personal statistics only and do not increase XP or create time-pressure rewards. The mode has no wagers and does not change Casino RNG, RTP, Vault contribution rules or payouts.

The existing Sudoku content remains part of this Development build, as do Casino Empire, Casino Vault and all previously integrated casino modes. The new hosted-game registry is the intended integration seam for the next Chill Lounge modes so their rule engines and typed save formats do not need to be coupled to the Sudoku-specific Lounge shell.

The source pin lives in the publishing workflow's push trigger and job guard. When changing the active feature branch again, update both fields together on the selected branch. Normal feature-branch CI previews remain available as artifacts and do not replace the shared Development release unless that branch is explicitly pinned here.
