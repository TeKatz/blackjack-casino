# Casino presentation contract

## Hover regression prevention

The recurring white/blue dropdown bug came from untemplated ToggleButtons inside
otherwise dark ComboBox templates. A transparent Background alone does not stop
the Windows theme from repainting that inner button on hover, focus or disable.
Earlier Manor-only fixes missed duplicate templates in App.xaml/MainWindow.xaml.

`Presentation/CasinoControls.xaml` now owns shared dark controls and the explicit
`CasinoDropDownHitArea` template. MainWindow uses that global ComboBox rather than
duplicating it. Manor retains its palette but shares the same transparent toggle.
Buttons, checkboxes and edit fields also own their state templates. Button hover
does not resize text independently of its control.

Before changing controls:

1. Edit the shared template or a palette setter; do not paste another ComboBox template.
2. Run `python tools/check_control_templates.py`. A negative check that removes
   `CasinoDropDownHitArea` must fail, proving the guard catches the original error.
3. Run `dotnet run --project tools/PresentationSmoke -c Release` on Windows.
4. Inspect the uploaded screenshots and state evidence for all table themes,
   hover/focus/open/disabled dropdowns and embedded game sizing.

`.github/workflows/presentation-regression.yml` performs steps 2–4 on pushes and
pull requests. The static contract and composed-pixel checks are mandatory;
native cursor hover capability is reported separately for headless runners.
Keep this workflow required in repository branch rules when configuring merge
protection. The workflow itself cannot grant itself required-check status.

## Responsive surfaces

Embedded games occupy only the content row below the shared header. Each authored
Viewbox fits its actual remaining Grid row; no overlay-wide forced Width/Height
may ignore a game's internal header. The Windows check verifies the actual Reels
Spin button stays inside the window at 1080×720, 1440×900, 1920×1080 and 2560×1440.
Blackjack's cards, captions and action deck share one design Viewbox. Decorative
table artwork is drawn separately at native bounds and never intercepts input.

## Extensible lobby and profile

`MainWindow.Lobby` is the composition root for built-in game registrations.
`CasinoModeRegistry` owns IDs, metadata, optional artwork factories and launch
commands. `CasinoLobbyView` renders the registry and responds to registrations;
it contains no switch over built-in IDs. New modes register one descriptor and
command without changing lobby rendering. A regression adds and invokes a fourth
mode at runtime. Cards reflow and scroll as the catalogue grows.

Startup opens the lobby, while returning during an unfinished Blackjack hand is
blocked. Slots keep their existing committed wallet/feature close behavior.
The shared header displays the selected portrait and name, and opens the separate
PlayerProfileWindow. Progression tabs remain available from the lobby/profile.
Profile names are normalized and validated, stored in profile.json and progress
exports, and copied through Manor profile adoption. Old profiles get “Player”
without losing cosmetics, balances or XP. No rules or payout logic changes here.
