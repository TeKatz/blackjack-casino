# English and German localization

The casino supports **English and German** for the native host, game controls and major interfaces. Some long-form adventure and older content is still being expanded; passing core UI checks is not a claim that every story paragraph has a finished translation.

This page summarizes the present process. The original WPF-era localization rollout, including superseded XAML and feature-branch instructions, is available in the [historical localization plan](archive/localization-wpf-era.md).

## Current source and acceptance

- English resource catalogs and their `.de.resx` counterparts live under `src/BlackjackCasino.HostShared/Resources/`.
- Localized React interface strings and labels are bundled into the offline frontend; no runtime translation service is required.
- `python tools/check_localization.py` verifies resource-key parity and nonempty values.
- `python tools/check_localization_coverage.py --strict-shell` checks the designated shell coverage contract.
- `python tools/check_adventure_localization.py` checks known adventure-language constraints.

When adding gameplay, ensure that both languages are reviewed in the actual interface for overflow, truncation and mixed-language errors. Maintain stable internal localization keys and save IDs. Product/game names such as Midnight Manor, Nine Lives and The Midnight Club ordinarily remain unchanged.

## Outstanding work

Review remaining story prose, item descriptions, older catalogs and device-specific layout behavior as content evolves. Localized UI and full story translation are distinct acceptance criteria; track them independently.

See [contribution guide](../CONTRIBUTING.md), [architecture](architecture.md) and [Tales](adventure-engine.md).
