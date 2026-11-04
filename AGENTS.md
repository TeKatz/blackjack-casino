# Agent working agreement — The Midnight Club

This file is the repository-level instruction for coding assistants and ChatGPT Work. Read it before modifying code, workflows, releases, or documentation. See [CONTRIBUTING.md](CONTRIBUTING.md) for the human-facing checklist and [modernization roadmap](docs/modernization-roadmap.md) for the frontend direction.

## Responsibilities and product contract

- The product owner is the game director and hands-on game/bug tester; coding agents own technical investigation, implementation, integration, automated testing, documentation and CI. Do not require the owner to edit code, run terminal commands, or understand implementation terminology.
- Translate gameplay and visual feedback into concrete technical acceptance criteria. Ask the owner only for genuine product/design choices, not routine technical decisions.
- This is an offline-first Windows single-player **play-money** casino. Do not introduce real-money gambling, payment integrations, accounts, ads, tracking, or network requirements.
- Preserve existing player saves, rewards, cosmetics, accumulated statistics, stable IDs, languages and existing playable modes. Changes to probabilities, RTP, bet limits, reward math and save schemas require explicit documented intent and tests.

## Before any edit

1. Inspect current repository branches, open PRs, active Development release pin, and the source files involved; never infer the freshest branch from its name.
2. Identify concurrent work and choose a separate branch that does not overwrite it. Do not delete, squash, force-push, reset, merge or repoint active branches without an explicit instruction.
3. Define a bounded scope, acceptance criteria, rollback and the tests that should prove it works. Prefer small reversible changes and incremental PRs.
4. Review architecture and feature-specific docs; update the README and documentation when they are stale.

## Current coordination hold (2026-11-04)

- Jigsaw Puzzle improvements are ongoing. Let that work finish and be validated before branch consolidation or invasive game migrations.
- `feature/casino-quality-phase-4` is the currently pinned Development publisher source. Do not change the release workflow's source branch or the in-app channel during repository preparation.
- This repository/workflow refresh is preparatory only. The first planned rendering migration is **Midnight Manor**, not Roulette; implementation begins only after the Jigsaw work is settled and branches are reconciled. See `docs/modernization-roadmap.md`.
- Refresh these time-sensitive coordination notes when active branches change; do not treat them as permanent rules.

## Architecture and modernization rules

- `BlackjackCasino.Core` remains UI-independent and authoritative for game outcomes, bet settlement, progression and durable state; no WPF/DOM/Pixi types in Core.
- Existing WPF is maintained for bug fixes while modes are migrated. For **new migrated presentation**, prefer TypeScript + React/HTML/CSS for text, controls and layout; PixiJS for sprite-based game visuals. Use WebView2 as the interim host, not a second game-rules engine.
- Build a versioned, validated bridge from frontend commands to the C# application layer. The client does not set balance or payouts. Round IDs and commands must be idempotent, crash-safe and replay/recovery tested.
- RNG/outcome determination precedes visualization; no animation, skip, sound or speed preference may alter outcomes.
- No full-window scaling of text UI as raster content. Check legibility, pointer targets, keyboard behavior, reduced-motion preferences, resize/DPI, and Win10/Win11 behavior where practical.
- Do not move the complete casino to a new shell in one change. Run legacy and migrated modes side-by-side; remove WPF code only after acceptance and save-compatibility verification.

## Quality gates

- Every functional change needs tests at the appropriate layer: unit/contract tests for logic, state and wallet; host/UI smoke and screenshots for rendering; localization parity and German/English review for strings.
- For performance-sensitive UI, measure before/after and record environment, frame times, memory, CPU/GPU observations and resize scenarios. Never claim faster FPS without evidence.
- Run `python tools/check_repo_contract.py` whenever workflow or docs files change.
- On Windows, use `dotnet restore BlackjackCasino.sln`, `dotnet build BlackjackCasino.sln -c Release`, `dotnet test BlackjackCasino.sln -c Release --no-build`, and affected smoke tools. Do not claim unexecuted checks passed.
- Review repository CI and the published artifact after changes; correct broken checks, do not merely disable them.
- Any dependency or SDK upgrade is an isolated, verified change. Prefer reviewed updates over untested blanket version bumps.

## Delivery, releases and reporting

- Leave `main` and the Development publisher protected from unverified updates. The Development workflow intentionally has both a branch push filter and a matching job guard; update **both** only when deliberately switching the channel.
- Do not bypass semver ordering, manifest integrity, updater compatibility or crash-recovery logic. Keep the existing updater and old installation migration path working until replacements pass real upgrade tests.
- A task report should include: changed paths/branch/PR, user-visible behavior, automated checks actually executed, checks not executed, known risks, and precise next steps for the game tester.
- Never describe proposed work as completed. Never silently change game mechanics to match a desired visual presentation.
