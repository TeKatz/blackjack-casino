# Phase 4 — Casino Presentation & Polish

Phase 4 moved the project from a functional Blackjack table toward a desktop casino experience that is comfortable to keep open and play for longer sessions. The original Phase 4 goals are now integrated and later work has extended the same presentation principles across the lobby, profile/progression and slot modes.

## Implemented presentation foundation

- three live Blackjack table themes: Classic Green, Midnight Blue and Burgundy;
- card entrance animations with staggered dealing motion;
- denomination-labelled wager chip presentation for normal, doubled and split hands;
- custom casino sound feedback and dedicated background music identities;
- F11 fullscreen toggle;
- persistent presentation/audio preferences;
- persistent Solo / Full Table choice, AI seat count and selected bet;
- persistent Stable / Development update channel;
- persistent window size/maximized state;
- custom dark casino controls instead of relying on default Windows styling.

All presentation effects are optional. Disabling sound or animations does not change game state, shoe behavior, wagers or slot outcomes.

## Casino-wide presentation work added later

The presentation layer now also includes:

- a unified casino lobby and mode registry;
- embedded game hosting so slots/profile/progression stay inside the main application window;
- Midnight Reels, Midnight Manor and Neon Vault visual identities;
- searchable/paged profile, challenges and shop screens;
- custom portrait artwork and cosmetic presentation;
- Windows presentation regression captures across several resolutions;
- explicit regression coverage for hover states, clipping, embedded navigation and wager geometry.

Neon Vault further exercises the Phase 4 approach with cached custom symbol artwork, targeted candidate/win highlighting, dimming of irrelevant cells and presentation-only anticipation/Near Miss sequences. See [Neon Vault](neon-vault.md).

## Performance rule

Presentation polish must not be implemented by recreating large WPF visual trees every animation tick. Repeating controls and decoded image assets are reused where possible, while heavier effects are reserved for transitions and feature moments.

For the broader current architecture see [architecture.md](architecture.md), and for the automated UI protection see [presentation-regression.md](presentation-regression.md).
