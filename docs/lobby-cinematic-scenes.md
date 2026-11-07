# Custom cinematic lobby scenes

**Status:** Modernisation branch, Development-channel candidate. Desktop user-testing remains required.

## Rendering boundary

All nine lobby mode thumbnails are PixiJS textures in one shared WebGL canvas.
React owns accessible labels, interaction, navigation, and the Play/Enter buttons.
Neither WPF vectors nor DOM/CSS replicas of game objects are drawn inside the
tile-art windows. Existing CSS border, shadow, and layout treatments are UI
chrome, not source artwork.

Eight new original offline-only SVG illustration pairs live under
`frontend/public/lobby/custom/`. Every scene has a background and a separately
transformable foreground, allowing depth and parallax without repaints of the
React layout. **Nine Lives deliberately keeps the existing cat image**
(`/lobby/art/nine-lives.jpg`) and receives PixiJS lights and particles.

| Mode | Primary illustrated subject |
| --- | --- |
| Blackjack | Two clearly visible playing cards, chips and a brass 21 medallion |
| Midnight Dice | Five numbered dice and score sheet |
| Chill Lounge | Lamp, warm chair, Sudoku and a steaming drink |
| Casino Empire | Casino building, architectural blueprints, construction lights |
| Midnight Reels | Classic three-reel slot cabinet with 7, fruit and bell |
| Neon Vault | Neon vault wheel and high-roller reels |
| Nine Lives | Preserved black-cat portrait and subtle violet eye atmosphere |
| Midnight Manor | Victorian haunted manor and moonlit fog |
| Midnight Merge | Two chairs combining into a restored luxury chair |

## Animation, performance and accessibility

The scene manager caches browser textures, clips sprites to live DOM thumbnail
rectangles, and reconciles filtered/unmounted modes. Only the Pixi transforms,
ambient light and particles move; game navigation and snapshots remain native.
The shared stage is capped to 30 FPS with animation enabled. With animation
disabled it still renders all artwork, at a 12-FPS geometry-update cadence,
with no moving particles, idle motion or hover parallax. Document visibility
pauses the ticker; non-visible tiles do not animate.

Individual asset loading failures are isolated from the rest of the lobby:
controls remain accessible, but that image window shows the dark fallback
surface. GPU initialization failure also leaves navigation available rather
than taking down the C# game. No game rules, wallets, progression, saved
profiles, localization or updater changes are involved.

## Acceptance criteria on Windows

- Verify nine immediately distinguishable subjects at 100%, 125%, 150% and 200% DPI.
- Verify no text or CTA is covered by the Pixi overlay; test keyboard and mouse.
- Verify hover/idle particles remain inside each scene rectangle while scrolling.
- Verify animation-off and OS reduced-motion preserve artwork without moving FX.
- Test returning from games, minimizing/restoring, resizing and GPU failure.
- Compare CPU/GPU/frame pacing and memory with the prior Development build.
- Verify installation contains every original custom SVG resource.

Frontend regression tests cover the asset manifest, generated files, clipping
math and the absence of DOM object-art overlays in the lobby component.
