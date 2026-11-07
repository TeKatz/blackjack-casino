# Premium Blackjack presentation

The modern Blackjack table is the sole player-facing Blackjack presentation in the Development desktop build. It uses React and CSS for premium gold-trimmed felt, softly illuminated surroundings, raised controls and physical card movement. C# Core remains authoritative for the game, dealer's concealed card, wallet, bets, hand outcomes and the persisted settings.

## Animation contract

`TablePresentation.tableVisualEvent` compares **validated, monotonic C# snapshots**. Fresh rounds increment a UI-only card animation epoch; new cards enter in a short stagger; hidden dealer cards flip in-place when the host reveals their real value. No client guesses the dealer's card. The win plaque appears only when the host reports a new Settled round with **positive TotalNet**. A refreshed snapshot or an already settled round opened from the lobby does not replay a win. The overlay never blocks input and automatically expires.

## Local soft sound

`SoftTableAudio` synthesizes quiet card/felt motion, chip taps and a custom ascending four-note win signature with the standard Web Audio API. Sound is enabled only after a user input has unlocked the browser audio context, and respects the native sound preference. No external audio assets or network access are required. The modern-table native command path no longer duplicates the previous native card sound. On browsers without audio output, game play continues without a sound.

## Visual constraints and acceptance

The gold-rail felt has etched/stitch lines and per-theme green, midnight blue or burgundy coloration, while the scene's background light motes remain subtle and no full-card wiping overlays are used. Shadowed buttons, physical-looking bet chips, and raised hand areas are fully accessible by keyboard. The existing animations toggle and `prefers-reduced-motion` remove continuous movement and dealing transitions.

**Check in the Windows Development build:** normal deal, hit, stand, split, insurance, dealer reveal, natural blackjack, positive win, loss/push, AI seats, both languages, theme switch, animation and sound toggles, back-to-lobby restriction mid-hand, portrait/compact windows and 125/150% DPI. A genuine visual/audio quality pass remains device-specific. No payouts, odds, game state or user save structure were changed.

## Table refinement: collector decks, controls, chips and browser dropdowns

The visual table rail is intentionally stationary. The previous inherited `bj-ambience` animation on the rail pseudo-element has been removed; only separate ambient background lighting continues to move. The actions are raised from the bottom stitching by increased felt padding and a second DEAL entry in the action rail. The original sidebar DEAL remains for keyboard and smoke-test compatibility, and both dispatch the same native `deal` command with the unchanged C# affordability and round guard.

### Collector cards

Midnight Sovereign uses cool onyx faces, thin royal-gold etched diamonds and a dark sovereign back. Solar Eclipse uses warmer amethyst/obsidian faces, a copper/amber corona around the central suit, and a circular eclipse back. Both remove the generic oversized white specular overlay. Playing cards use the native equipped `cardVariant` as a CSS identity, with no change to ranks, suits, hidden cards or C# hands.

### Decorative wager chips

`TableWagerChips` renders non-interactive green, burgundy and champagne-chip stacks near the player's seat. The stack heights are a bounded logarithmic function of the *confirmed* native `selectedBet` snapshot; they never supply an amount to the engine and cannot be clicked, dragged or exchanged. Chip entrance animations follow the animation preference and reduced-motion accessibility setting.

### Premium dropdowns and input stability

`PremiumSelect` replaces native browser/Windows dropdown popups on the table sidebar and the full custom-rules modal. Its portal-mounted listbox stays visible outside scroll containers, chooses above/below placement according to viewport space and supports keyboard arrows, Home/End, Enter, Space, Escape and Tab, disabled options, focus return, and screen-reader labels. A keydown guard prevents Blackjack gameplay hotkeys from leaking into the dropdown.

The browser command-pending state remains functionally locked against duplicate purchases, bet changes or actions, but avoids the previous one-frame opacity/hover flashes. Checkbox appearance remains OS-native and the controls preserve focus-visible outlines for keyboard access.

### Validation

Frontend unit tests cover unique visual card identities, material-specific variants, non-interactive wager stacks, scaling bounds, dropdown positioning and modal migration. Windows presentation smoke checks the new DEAL action, table chips and custom dropdown presence without weakening the native command or account safety checks. Desktop screenshot, DPI, pointer and audio quality still require an installed Windows build.

## Collector alignment, premium wagers and stable command UI

The desktop Blackjack collector decks use absolutely positioned rank indices, small corner pips and a **geometrically centered** principal suit. The card-face decoration is a non-interactive, vector-engraved ornament layer, never an extra CSS grid child. Midnight Sovereign uses precisely centered onyx/gold diamond filigree, while Solar Eclipse has a distinct radial solar corona. Card scale adapts to AI mini cards and compact Windows layouts.

The wager display is a scalable, multi-material SVG sculpture with three physically layered chip denominations and an engraved gold-facing coin. The stacks still derive only from the authoritative C# selectedBet through the bounded `stackHeights` presentation mapping; they cannot be clicked or used to settle a bet. A small lazy PixiJS WebGL renderer creates masked/local sparkles and two slowly animated rings around the sculpture, capped at 30 FPS. Unavailable GPU devices fall back to the full SVG artwork, and reduced-motion settings suppress the animations. There are no fullscreen wipe effects.

**Command UI flicker:** A command's in-flight guard continues to reject duplicate actions synchronously through `pendingRef`, but does **not** flip all other buttons' HTML `disabled` attributes. Native availability and round phase govern the actual disabled state. Temporary command-lock state uses accessibility `aria-disabled` and a visually stable busy guard on custom selectors; the wager text field is temporarily read-only. The Windows WebView2 smoke now observes a native bet change through a DOM MutationObserver and rejects unrelated transient disabled-state mutations. Keyboard focus and no-GPU table navigation remain available.

The original Blackjack core, shuffle, payouts, purchases and save format are unchanged. Validation includes frontend SSR component/regression tests, the Windows host presentation smoke, CI and the Development release workflow; subjective GPU/animation and visual quality still require running the published Windows app.
