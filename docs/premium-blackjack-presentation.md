# Premium Blackjack presentation

The modern Blackjack table is the sole player-facing Blackjack presentation in the Development desktop build. It uses React and CSS for premium gold-trimmed felt, softly illuminated surroundings, raised controls and physical card movement. C# Core remains authoritative for the game, dealer's concealed card, wallet, bets, hand outcomes and the persisted settings.

## Animation contract

`TablePresentation.tableVisualEvent` compares **validated, monotonic C# snapshots**. Fresh rounds increment a UI-only card animation epoch; new cards enter in a short stagger; hidden dealer cards flip in-place when the host reveals their real value. No client guesses the dealer's card. The win plaque appears only when the host reports a new Settled round with **positive TotalNet**. A refreshed snapshot or an already settled round opened from the lobby does not replay a win. The overlay never blocks input and automatically expires.

## Local soft sound

`SoftTableAudio` synthesizes quiet card/felt motion, chip taps and a custom ascending four-note win signature with the standard Web Audio API. Sound is enabled only after a user input has unlocked the browser audio context, and respects the native sound preference. No external audio assets or network access are required. The modern-table native command path no longer duplicates the previous native card sound. On browsers without audio output, game play continues without a sound.

## Visual constraints and acceptance

The gold-rail felt has etched/stitch lines and per-theme green, midnight blue or burgundy coloration, while the scene's background light motes remain subtle and no full-card wiping overlays are used. Shadowed buttons, physical-looking bet chips, and raised hand areas are fully accessible by keyboard. The existing animations toggle and `prefers-reduced-motion` remove continuous movement and dealing transitions.

**Check in the Windows Development build:** normal deal, hit, stand, split, insurance, dealer reveal, natural blackjack, positive win, loss/push, AI seats, both languages, theme switch, animation and sound toggles, back-to-lobby restriction mid-hand, portrait/compact windows and 125/150% DPI. A genuine visual/audio quality pass remains device-specific. No payouts, odds, game state or user save structure were changed.
