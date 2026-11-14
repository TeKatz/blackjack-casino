# Current UI and presentation regression contract

The player-facing casino runs in the native WinForms/WebView2 host using **local React, TypeScript and PixiJS**. The former WPF/XAML control-template regressions are retained in the [historical presentation archive](archive/presentation-wpf-era.md); those files and controls are no longer built.

## Lobby and game navigation

- Verify all 11 top-level casino route identities, native C# availability decisions, the separate Midnight Descent process and the detachable Casino Empire view.
- The lobby must preserve existing PixiJS tile objects across category or tab changes. **No visible morph from interim tile geometry** should occur on return or tab switching.
- Hover, click, focus, touch targets and tab selection must not resize or scramble unrelated tiles.
- Opening and returning from every local game must preserve the settled C# state, running wallet, audio settings and animations.
- On missing WebView2 assets or a browser failure, the native recovery UI must offer safe actions without enabling a removed WPF fallback.

## Window, rendering and accessibility

- **F11** enters and leaves fullscreen; restore size and focus without corrupting saved window placement.
- Verify scaling at common Windows DPI and viewport sizes, including long labels and both languages; avoid clipped text and blurry icons.
- Respect disabled animations, reduced-motion preferences and hidden tabs; avoid unnecessary PixiJS work.
- Check startup and page-transition performance as well as long-running sessions, particularly after entering and exiting the Godot Manor bonus.
- Confirm audio cues and muted preferences across all slots, Blackjack, Dice and Lounge activities.

## Automated evidence

- Frontend test/build, including `frontend` layout components.
- Windows `--web-smoke` validates real native WebView2 startup, F11, lobby tile DOM identity, game pages and return navigation.
- Native `--smoke` validates protocol and player-data recovery, alongside .NET unit tests.
- Package gates verify local frontend assets and executable/artwork integrity.

A green CI run **does not replace** real-Windows manual inspection for hover quality, sound, device-specific DPI, focus and extended-session performance. See the [architecture guide](architecture.md) and [contribution checklist](../CONTRIBUTING.md).
