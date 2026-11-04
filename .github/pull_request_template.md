## What changed / why

Describe user-visible changes and the exact systems affected. Link the issue or game-test feedback when available.

## Scope and compatibility

- Source/base branch:
- Existing saves or migration implications:
- Gameplay math, RTP, reward, odds or balance implications (none / describe):
- UI, animations, languages and audio implications:
- Rollback/fallback plan:

## Verification

- [ ] Repository contract: `python tools/check_repo_contract.py`
- [ ] .NET build/test or reason not run
- [ ] Relevant Windows smoke, DPI/layout checks and screenshot evidence or reason not run
- [ ] Localization parity and German/English review or not applicable
- [ ] Save/restart, interrupted feature and reward-idempotency checks or not applicable
- [ ] Performance baseline/comparison for animation-heavy changes or not applicable
- [ ] README/architecture/game-specific documentation updated or not applicable

**Actual checks run and results:**

**Known limitations, tests not run and follow-up:**

## Release coordination

- [ ] This PR does not silently switch the Development publisher source
- [ ] The ongoing workstreams are respected; no unrelated branch cleanup or deletion
- [ ] No release/update path is changed without verification and deliberate approval
