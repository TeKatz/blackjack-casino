# Development update delivery

The in-app Development manifest remains:

`https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json`

The original Manor PR only uploaded a CI artifact. Its `slots/midnight-manor` branch was outside the Development publisher's triggers, leaving the public channel at `0.6.5-dev.71` from main. A final `0.6.5` also sorts above any `0.6.5-dev.N` under SemVer. Changing a URL or comparison only in a new executable cannot repair an already-installed client.

At the user's request, the Manor branch is explicitly included in the Development workflow, and the next feature version is `0.7.0-dev.N`. Both installed final 0.6.5 and its Development builds detect this through their existing comparator and URL. The normal main publisher stays enabled; no main merge or Stable release is needed for this Development test delivery.

Before publication, the workflow builds, runs all tests and the actual Windows WPF smoke, and packages the application with its separate updater. It then verifies the release target and required assets. Package/manifest filenames, SHA-256 verification and updater schema are unchanged. Mutable-manifest requests now send Cache-Control: no-cache, no-store to revalidate subsequent checks. The UI shows installed/available versions when no newer update is found.

Versioning rule: start a new Development cycle on a version base strictly above the latest final version (e.g. 0.7.0-dev.N after final 0.6.5). Preserve SemVer ordering; do not offer silent downgrades. PR preview artifacts use dev.0 and are superseded by the published Development run number.

## Current test channel: Casino Empire

The shared `development` release is temporarily pinned to
`feature/casino-empire-foundation`. The publisher runs automatically only for
that branch; manual dispatch from another branch is skipped. Main and other
active branches retain normal CI but cannot replace this test channel.
Workflow concurrency is scoped by branch so unrelated/skipped manual runs do
not cancel the selected publisher. The existing client manifest URL stays the
same; installed clients only need to select Development and check for updates.

The next published version uses the existing monotonically increasing workflow
run number (`0.10.1-dev.N`). The release target and manifest notes identify the
exact Casino Empire commit, and package SHA-256 and size checks remain enabled.
To move the shared channel later, update the pinned trigger and job guard in
the publishing workflow on main and any active source branches.

The Casino Empire presentation update starts at `0.10.1-dev.N` so clients that
installed the parallel Nine Lives `0.10.0-dev.211` build can detect it through
normal SemVer ordering. The user explicitly selected Casino Empire as the
current shared Development test build.
