# German localization overhaul — three phases

This plan is cumulative on `feature/casino-quality-phase-4`. The Development
channel continues to use the existing release manifest URL and publisher.

## Phase 1 — shared foundation and casino shell
- Add an explicit bilingual `ShellStrings` catalog, including window controls,
  the welcome dashboard, player profile and table-rule setup.
- Replace runtime-composed shell copy with stable `Localization.T/F` keys.
- Identify blackjack table-rule labels by WPF control name instead of matching
  English text, so rule changes still work after switching language.
- Add a source-coverage audit: migrated shell surfaces fail CI when an English
  literal has no resource; other screens generate a debt report.
- Verify language switching, the welcome window and table-rule behavior with
  Windows smoke tests.

## Phase 2 — gameplay and casino management
- Midnight Dice, Midnight Reels, Midnight Manor, Neon Vault and Nine Lives;
  also Midnight Merge, Solitaire, Sudoku, Memory, Word Search UI and Empire.
- Localize XAML, runtime labels, scoring, errors, instructions, tooltips,
  menus and accessibility names; extend strict CI coverage per completed module.
- Preserve all gameplay IDs, save schemas, visual assets, rules and payouts.

## Phase 3 — large content catalogs and full acceptance
- Translate all authored Tales from the Backroom scenes, choices, locations,
  character classes, encounter results and investigation content. Translate
  204 challenge names/descriptions, cosmetic descriptions, all 84 existing
  album lore texts, season copy and Memory labels.
- Create language-specific Word Search **puzzle grids and solutions**, not only
  translated target lists; maintain save-state compatibility across languages.
- Localize generated release notes (or ship bilingual editorial entries);
  changing the UI language must not show raw commit subjects in German.
- Move all completed modules from report-only to strict coverage and run
  a comprehensive German+English Windows UI regression suite.

## Auditing
`python tools/check_localization.py` verifies key parity and non-empty values.
`python tools/check_adventure_localization.py` covers basic adventure sanity.
`python tools/check_localization_coverage.py --strict-shell` checks the
**migrated** Phase-1 shell and lists unlocalized legacy candidates. A green
Phase-1 coverage check does **not** mean the whole program is fully translated.
Do not put names, save identifiers, paths, non-display literals or gameplay
rules into resource lookup. The source locale remains English, with de-DE as
an explicit translation. Preset, scene, choice and item references must use
stable IDs rather than their translated display names.
