# Nine Lives art assets

Nine Lives is authored around high-resolution raster artwork. WPF vector drawings remain only as safe fallbacks so a missing or malformed image can never crash startup.

Expected PNG resources:

- `background.png` — 1536x1024 or larger, night rooftop / moonlit machine atmosphere.
- `yarn.png`
- `fishbone.png`
- `bell.png`
- `tabby-cat.png`
- `white-cat.png`
- `black-cat.png`
- `wild.png`
- `bonus.png`

Symbol requirements:

- square PNG, minimum 512x512; 1024x1024 preferred source.
- no baked-in text labels.
- consistent internal safe area so faces/ears/details are not clipped.
- same gold/violet midnight lighting and framing language across all eight symbols.
- avoid JPEG compression; alpha is allowed.
- artwork is scaled down by the client with HighQuality bitmap filtering.

The loader prefers these resources. If an asset is missing or cannot be decoded, Nine Lives logs the problem and falls back to its built-in vector drawing for that symbol only.
