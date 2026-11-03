# Nine Lives — HD symbol artwork

The eight Nine Lives symbols were regenerated as a cohesive midnight-casino
illustration set with gold ornaments and violet lighting.

Packaged WPF resources (1024 × 1024 pixels each):

| File | NineLivesSymbol |
| --- | --- |
| `black-cat.jpg` | BlackCat |
| `white-cat.jpg` | WhiteCat |
| `tabby-cat.jpg` | TabbyCat |
| `fishbone.jpg` | Fishbone |
| `yarn.jpg` | Yarn |
| `bell.jpg` | Bell |
| `wild.jpg` | Wild |
| `bonus.jpg` | Bonus |

`NineLivesArtwork` loads each resource from its pack URI, decodes at full
resolution, freezes and caches it once, and uses WPF high-quality
`Stretch.Uniform` scaling at the reel. Source art (1254 × 1254 PNG) is kept in
the accompanying artwork package, not duplicated in the production app.

The former 112-pixel embedded symbol JPEG strings and yarn concept were
removed. Vector drawings remain as safe fallbacks for missing/corrupt assets.
The existing background artwork in `NineLivesArtData.Background*.cs` is
intentionally retained as it is separate from the symbol replacement.
