# Agent working agreement — The Midnight Club

This file is the repository-level instruction for coding assistants and ChatGPT Work. Read it before modifying code, workflows, releases, or documentation. See [CONTRIBUTING.md](CONTRIBUTING.md) for the human-facing checklist and the [documentation index](docs/index.md) for current game, architecture and historical guides.

## Responsibilities and product contract

- The product owner is the game director and hands-on game/bug tester; coding agents own technical investigation, implementation, integration, automated testing, documentation and CI. Do not require the owner to edit code, run terminal commands, or understand implementation terminology.
- Translate gameplay and visual feedback into concrete technical acceptance criteria. Ask the owner only for genuine product/design choices, not routine technical decisions.
- This is an offline-first Windows single-player **play-money** casino. Do not introduce real-money gambling, payment integrations, accounts, ads, tracking, or network requirements.
- Preserve existing player saves, rewards, cosmetics, accumulated statistics, stable IDs, languages and existing playable modes. Changes to probabilities, RTP, bet limits, reward math and save schemas require explicit documented intent and tests.

## Before any edit

1. Inspect current repository branches, open PRs, active Development release pin, and the source files involved; never infer the freshest branch from its name.
2. Identify concurrent work and base focused feature/fix changes on `dev` rather than the historical Manor migration branch. Do not delete, squash, force-push, reset, merge or repoint active branches without an explicit instruction.
3. Define a bounded scope, acceptance criteria, rollback and the tests that should prove it works. Prefer small reversible changes and incremental PRs.
4. Review architecture and feature-specific docs; update the README and documentation when they are stale. Write from the perspective of the project, not from a transcript of decisions.

## Current coordination status (2026-11-14)

- **`dev`** is the permanent Development publisher and base for new work; `main` is the reviewed Stable integration baseline. The WPF host and its first-run cutover have been completed on `dev`, not merely piloted. `main` retains the earlier host until a separately verified Stable integration. See [branching policy](docs/branching-and-release-policy.md).
- The Development publisher's push filter and job guard both select `dev`; the in-app update-manifest URL, channel and Stable tag requirements remain unchanged. Feature branches cannot publish through the Development workflow.
- Modern React/TypeScript/PixiJS local WebView2 surfaces cover the lobby and all casino games. The WinForms/WebView2 `BlackjackCasino.NativeShell` is the native Development release host. C# continues to own outcomes, payouts, progression and persistent receipts. On `dev`, the WPF project, XAML and legacy WPF game fallbacks have been retired.
- The four historical cutover/rollback tools live in `tools/NativeMigrationDiagnostics/` and run against synthetic data in CI; **they are not included in the shipped casino**. Existing player-data folders and the WPF-free compatibility launcher remain stable.
- Device-specific visual/audio quality, DPI, extended performance and remaining localization still require validation.

## Architecture and modernization rules

- `BlackjackCasino.Core` remains UI-independent and authoritative for game outcomes, bet settlement, progression and durable state; no WPF/DOM/Pixi types in Core.
- Use TypeScript + React/HTML/CSS for text, controls and layout; PixiJS for sprite-based visuals, with local WebView2 in the native WinForms host. Do not reintroduce WPF, XAML or a second game-rules engine.
- Build a versioned, validated bridge from frontend commands to the C# application layer. The client does not set balance or payouts. Round IDs and commands must be idempotent, crash-safe and replay/recovery tested.
- RNG/outcome determination precedes visualization; no animation, skip, sound or speed preference may alter outcomes.
- No full-window scaling of text UI as raster content. Check legibility, pointer targets, keyboard behavior, reduced-motion preferences, resize/DPI, and Win10/Win11 behavior where practical.
- Maintain the WPF-free NativeShell, protected player-data import, receipt journals and old-updater-compatible executable alias. Do not reintroduce a silent legacy fallback or let a fresh import overwrite an existing native profile.

## Documentation style

- Document the **current product state, architecture, release policy, verified behavior, and outstanding work**. Historical sections must be clearly identified as such.
- Avoid conversational provenance and attributions of product decisions to individual participants. State what is implemented, enabled, approved for a channel, or awaiting verification without retelling who asked for it.
- Keep the README a concise entry point; place detailed milestones and technical decisions in linked documentation.
- The repository contract scans Markdown files for conversation-style directive phrasing. Resolve failures by rewriting the passage, not by excluding the affected file.

## Quality gates

- Every functional change needs tests at the appropriate layer: unit/contract tests for logic, state and wallet; host/UI smoke and screenshots for rendering; localization parity and German/English review for strings.
- For performance-sensitive UI, measure before/after and record environment, frame times, memory, CPU/GPU observations and resize scenarios. Never claim faster FPS without evidence.
- Run `python tools/check_repo_contract.py` whenever workflow or docs files change.
- On Windows, use `python tools/check_native_cutover.py`, `dotnet restore BlackjackCasino.sln`, `dotnet build BlackjackCasino.sln -c Release`, `dotnet test BlackjackCasino.sln -c Release --no-build`, `dotnet run --project src/BlackjackCasino.NativeShell -- --smoke` and `--web-smoke`, `dotnet run --project tools/NativeMigrationDiagnostics -- --rehearsal-smoke` (synthetic data only) plus affected frontend tests. Do not claim unexecuted checks passed.
- Review repository CI and the published artifact after changes; correct broken checks, do not merely disable them.
- Any dependency or SDK upgrade is an isolated, verified change. Prefer reviewed updates over untested blanket version bumps.

## Delivery, releases and reporting

- Leave `main` and `dev` protected from unverified updates. The Development workflow intentionally has both a branch push filter and a matching job guard; update **both** only when deliberately switching the channel.
- Do not bypass semver ordering, manifest integrity, updater compatibility or crash-recovery logic. Keep the existing updater and old installation migration path working until replacements pass real upgrade tests.
- A task report should include: changed paths/branch/PR, user-visible behavior, automated checks actually executed, checks not executed, known risks, and precise next steps for the game tester.
- Never describe proposed work as completed. Never silently change game mechanics to match a desired visual presentation.
