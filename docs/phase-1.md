# Phase 1 — Foundation

## Included in the initial foundation

- .NET 10 solution structure
- deterministic, UI-independent blackjack domain model
- configurable 1–8 deck shoe and penetration/cut-card tracking
- ace/soft-hand evaluation
- hit, stand, double, split, late surrender and insurance
- H17/S17 support
- blackjack 3:2 payout support
- dealer peek configuration
- split-hand tracking so a split 21 is not treated as a natural blackjack
- automated unit tests for core rules and updater version/integrity behavior
- Windows desktop bootstrap shell
- updater manifest protocol
- HTTPS download with SHA-256 and exact-size verification
- out-of-process staged updater with swap rollback
- CI on pushes and pull requests
- tag-driven Windows packaging and GitHub Release creation

## Still Phase 1 follow-up work

Before Phase 2 UI work begins, expand edge-case coverage around:

- resplitting and resplitting aces;
- no-hole-card / European dealer rules;
- double restrictions (9–11, 10–11, any two);
- configurable blackjack payout (6:5 tables);
- bankroll/reservation accounting around split/double/insurance;
- long-running simulation tests to detect statistical regressions;
- updater end-to-end smoke test using a temporary installation directory.

These are deliberately tracked as rule-engine work, not UI work.
