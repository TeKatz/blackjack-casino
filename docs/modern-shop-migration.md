# Modern Cosmetic Shop migration

Integration baseline: `main`; Development source: `develop` (historical implementation branch: `feature/manor-modernization-phase-1`). The player-facing Cosmetic Shop is now an in-place React + TypeScript view in the same WebView2 used by the casino lobby. The retired WPF shop is retained as a fallback when the frontend is not available; it is not a second overlay in the normal client.

## Routing

- Lobby Shop navigation opens ModernShop and requests an inventory snapshot from the native host.
- A native Season Pass / Challenges / Profile progression tab routes to that same view. Any native meta-page stack is unwound *before* the React shop is revealed. The React route is selected while the browser is hidden to avoid one-frame lobby flashes.
- Back returns to the lobby, or restores the underlying game if Shop was opened from an in-game progression page.
- Album/Season/Challenges and other native pages stay native during this phase and can still be accessed from the permanent React sidebar.

## Authority and persistence

The React bridge does not calculate prices, balances, prestige gates, owned flags or equipment rights. `MainWindow.ModernShop.cs` creates the inventory snapshot from `CosmeticCatalog` and the live `PlayerProfileState`. Purchase and equip requests are validated against the catalog and sent to `PlayerProgressionController.PurchaseCosmetic`, `EquipCosmetic` or `PurchaseCollectionPack`. These are the same transactional services used by the previous shop and persist the profile normally. After any action, the host sends authoritative state and a result message. Repeated equipment clicks never purchase an already-owned item again.

## Presentation

- All five cosmetic kinds, status/ownership filters, search, sort, color-coded previews, and item detail are presented in responsive, scrollable cards.
- Seventeen original high-resolution portraits have been copied from existing desktop asset blobs into the web frontend; no newly synthesized low-resolution atlases are used.
- Card, dice, frame and title shop previews are vector/CSS representations of the relevant visual family. The gameplay cosmetic renderers remain the definitive source for exact card and dice appearance; this migration does not change those gameplay renderers.
- Five collection pack tiers use the catalog's real guaranteed rarity, ultra-rare probability, price, and inventory count. The existing album handles opening them.

## Tests and safeguards

- `shopBridge.test.ts` tests snapshot validation and filtering, including malformed costs and IDs.
- `ModernShopSmoke` uses the real packaged React DOM in Windows WebView2 to buy/equip a cosmetic, verify the exact native coin debit, prevent double charging, purchase a pack, verify inventory, capture the shop and return to the lobby.
- The Windows presentation regression tests both sidebar navigation and the native progression-tab redirect.
- The old casino lobby remains transparent/noninteractive while React owns the scene, so it cannot reappear during shop transitions.

Do not merge this branch into main merely to ship development updates. The development channel publishes directly from the feature branch, as before.
