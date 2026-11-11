# Midnight Descent — Nocturne UI (v2)

## Design direction

The world, sprites and relic art remain RPG pixel art. **Interface typography is not pixel art.** Do not restore the previous purple/black nested rectangles, corner studs, 1px pixel fonts or noisy HUD borders. The fixed HUD layout (top character/floor/economy and bottom abilities/relics) is intentionally preserved.

- Surface: nocturnal ink and teal slate, with restrained translucent layering.
- Accent: antique brass for loot/priority, jade for action/readiness, crimson for health danger.
- Borders: thin continuous material edges, anti-aliased rounded corners and soft depth.
- Type: Cinzel for large dramatic headings, Alegreya Sans Bold for figures and compact HUD, Alegreya Sans Regular for descriptions and longer text.
- Icons: gameplay sprites are pixel art; UI text stays smoothly rendered at desktop resolution.
- Never make every panel glow or pulse continuously. Idle state should be calm.

## Screens using the shared theme

Entry/gate, expedition, eight loadouts, character unlocks, permanent talent constellation, codex/archive, settings, pause, map, merchant, event, shrine, special services, forge, hub keeper, reward selections, victory/defeat, run relic inspection and in-game HUD all use `ui_theme.gd`.

## Gameplay-driven feedback

- Damage: sharp crimson seam on the health module, a brief edge response.
- Healing: mint seam and health-fill illumination; animated health value trails actual HP.
- Shards: gold glint and visible response on the shard module.
- Kill counter: short warm-color response.
- Ability ready: light pulse on the appropriate slot. Active cooldown: a mask drains from the icon and a timer remains visible.
- Critical HP (<26%): rhythmic red tint and screen-edge flashes, with intensity rising as HP approaches zero.
- Boss/synergy/relic/event announcements: dramatic display type and a single contained accent.
- Hover/focus: gentle highlight of buttons and reward cards, never aggressive whole-screen illumination.

## Technical rules

Use `UI.install_theme(root)` for native Control trees and `UI.BOLD_FONT`/`UI.DISPLAY_FONT` for immediate CanvasItem text. Avoid `ThemeDB.fallback_font` for game UI. Keep the licensed TTF assets and both OFL notices in `fonts/`. Godot smoke tests on `develop` parse both scenes and run their scripted scenarios; visual review should still be performed at 1920×1080.
