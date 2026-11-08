# Premium Shop visuals and animation

The browser-owned cosmetic Shop displays genuine user-owned items from the native C# bridge. Product prices, inventory, equipment, prestige, purchase permissions and cosmetic variants are never inferred from visuals.

## Presentation
- **Frames** render the existing shared ProfileFrame with an object-specific crest, faceted trim, inset gems, localized energy glows and orbital sparks. Neon, Eclipse, Ember, Aurora, Jade, Obsidian and Gold have distinct rim structures.
- **Cards** display separate decorated backs and front faces with variant motifs, filigree, limited foil reflection, embossing and depth. The entire card/product does not receive a repeating wipe.
- **Dice** keep the native variant palettes and actual pips but show distinct Obsidian, Porcelain, Jade, Crystal/Aurora, Sapphire and metallic optical materials, bevels and specular reflections.
- **Portraits** use existing character artwork with animated arc, rim, ambient backlight and crown. No image duplication or animated face manipulation.
- Item quality controls *visual intensity only*. The shop bridge does not currently expose rarity, so UI does not pretend to know an item's rarity or grant privileges from styling.

## Rendering/performance
All product particles share **the same existing PixiJS WebGL canvas** used by the lobby. `ShopFxEngine` clips the effects to the cosmetic object (not the product card); only visible items are active (max 18). Offscreen items, category changes and a purchase dialog cause scenes to be destroyed. `prefers-reduced-motion` and the existing animation toggle disable all cosmetic motion, while the browser app and the C# engine remain functional without WebGL.

## Scope
Removed repeating full-tile sweep elements from the Lobby and did not add them to the Shop. The rest of the game, economy, persistence and native protocol remain unchanged. Browser SSR tests verify all four key item types and theme mapping. Desktop validation should include 1080p/1440p, 125–150% DPI, filtering a large collection, hovering purchases, disabling animations, and browser GPU initialization fallback.
