# Performance Phase 5 — Collection artwork memory

**Historical performance-phase record.** This page documents a specific optimization and its original measurement scope. It is not a guarantee of current FPS or GPU/CPU use. Consult [current presentation validation](presentation-regression.md) and the [quality roadmap](modernization-roadmap.md).

Phase 5 bounds the decoded collection-art cache without changing collection IDs, artwork assets, card layouts, ownership, pack contents, rarity effects, or save data.

## Previous behavior

`CollectionIllustrations` kept two process-lifetime dictionaries:

- every decoded `BitmapImage` atlas that had ever been requested;
- every `CroppedBitmap` card image that had ever been requested.

Each collection atlas is 1536 × 1024 pixels. At 32 bits per decoded pixel that is approximately 6 MiB per atlas. With 16 collection themes, visiting the whole catalog could therefore leave roughly 96 MiB of decoded atlas pixels strongly reachable for the remainder of the process, before WPF/GPU overhead. The card-image dictionary also kept each crop alive, which in turn retained its source atlas.

## Phase 5 behavior

Collection artwork now uses a three-atlas LRU cache.

- Each cache entry owns one frozen decoded atlas and the frozen card crops created from that atlas.
- Accessing an atlas moves its entry to the front of the LRU.
- Loading a fourth atlas evicts the least-recently-used atlas bucket, including the cache's strong references to all crops from that atlas.
- Existing WPF `Image` controls remain safe: a visible control keeps its returned `CroppedBitmap` alive, and that crop keeps its source atlas alive for as long as the visual actually needs it.
- Once an evicted theme is no longer referenced by live UI, its decoded atlas and crops can be reclaimed by the GC.
- Returning to an evicted theme reloads it from the packaged resource and rebuilds only that theme's requested crops.

The cache-held decoded atlas ceiling is therefore approximately 18 MiB (3 × 6 MiB), versus approximately 96 MiB after visiting all 16 themes previously. This is a decoded-pixel estimate, not a claim about total process working set; live WPF visuals, graphics-driver resources and other objects can temporarily add memory beyond the cache itself.

## Why the cache is atlas-scoped

Evicting only the `BitmapImage` dictionary would not work. `CroppedBitmap` retains its source image, so a global process-lifetime crop cache would keep old atlases alive indirectly. Atlas and crop lifetime must be managed together.

## Regression coverage

The existing Windows collection-art presentation smoke already walks all 136 card illustrations and then renders all 16 theme galleries. That workload now necessarily crosses the three-atlas cache boundary repeatedly, exercising load, reuse and eviction while still validating unique pixels, card dimensions, high-quality inspector rendering, rarity finishes, corner alignment and animation lifecycle.

No image resampling or decode-size reduction is introduced in this phase. Crop coordinates still use the audited full-resolution manifest, avoiding quality or alignment changes.
