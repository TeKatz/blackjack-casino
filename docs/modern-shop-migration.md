# Modern Cosmetic Shop migration

**Current:** NativeShell + local React/WebView2; Development source: `dev` (Stable is separately tagged from `main`).

## Routing

- Lobby Shop navigation opens ModernShop and requests an inventory snapshot from the native host.
- Progression and lobby routes open the same authoritative React shop without introducing an independent legacy WPF window.
- Back returns to the lobby, or restores the underlying game if Shop was opened from an in-game progression page.
- Album, Season, Challenges and profile views remain available through the current React lobby and native C# bridges.

## Authority and persistence

The React bridge does not calculate prices, balances, prestige gates, owned flags or equipment rights. The native lobby controller builds the inventory snapshot from `CosmeticCatalog` and the live `PlayerProfileState`. Purchase and equip requests are validated against the catalog and sent to `PlayerProgressionController.PurchaseCosmetic`, `EquipCosmetic` or `PurchaseCollectionPack`. These are the same transactional services used by the previous shop and persist the profile normally. After any action, the host sends authoritative state and a result message. Repeated equipment clicks never purchase an already-owned item again.

## Presentation

- All five cosmetic kinds, status/ownership filters, search, sort, color-coded previews, and item detail are presented in responsive, scrollable cards.
- Seventeen original high-resolution portraits have been copied from existing desktop asset blobs into the web frontend; no newly synthesized low-resolution atlases are used.
- Card, dice, frame and title shop previews are vector/CSS representations of the relevant visual family. The gameplay cosmetic renderers remain the definitive source for exact card and dice appearance; this migration does not change those gameplay renderers.
- Five collection pack tiers use the catalog's real guaranteed rarity, ultra-rare probability, price, and inventory count. The existing album handles opening them.

## Tests and safeguards

- `shopBridge.test.ts` tests snapshot validation and filtering, including malformed costs and IDs.
- Native Windows/WebView2 smoke and C# progression tests cover purchase/equip receipts, coin debits, pack inventory and return navigation.
- Windows presentation regression checks navigation and React lobby state restoration.
- The current React lobby owns the visible navigation surface; no WPF lobby is retained.

Do not merge this branch into main merely to ship development updates. The development channel publishes directly from the feature branch, as before.
