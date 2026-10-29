# Premium shop portrait artwork

The eight approved original portraits are bundled as WPF resources. Each source is a
1254 × 1254 PNG; no network access or external image file is needed at runtime.

| Cosmetic ID | Resource | Palette |
| --- | --- | --- |
| portrait-night-dealer | night-dealer.png | Midnight navy / copper |
| portrait-card-shark | card-shark.png | Petrol / platinum |
| portrait-moon-cat | moon-cat.png | Lavender / moon silver |
| portrait-neon-ace | neon-ace.png | Cyan / magenta / black chrome |
| portrait-lucky-raven | lucky-raven.png | Emerald / gold |
| portrait-midnight-lady | midnight-lady.png | Burgundy / moonstone |
| portrait-golden-joker | golden-joker.png | Onyx / amber / gold |
| portrait-velvet-king | velvet-king.png | Imperial violet / ruby / gold |

`PortraitArtwork` loads and freezes one cached 512px bitmap per image. The shop
(78 DIP), owned portraits (104 DIP) and equipped portrait (112 DIP) share that
source. `CreatePortraitVisual` uses a centred, uniform ImageBrush inside an Ellipse:
corners are truly clipped without stretching faces, with a thin outer rim and
high-quality scaling. The source composition retains its own ornamental ring.
Default and Season portraits continue to use their existing presentation.

`tools/ManorSmoke` checks the actual WPF resources, distinct images, cache reuse,
transparent circle corners, real shop purchase/equip and the Player view. Its
runtime evidence includes eight circular previews and shop/player screenshots.
