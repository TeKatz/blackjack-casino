# Premium Shop portrait artwork

The original source portraits are stored here for offline use. The **current React Shop and profile** load the corresponding locally bundled web artwork from `frontend/public/shop/portraits/`; the retired WPF `PortraitArtwork` bitmap loader and `tools/ManorSmoke` are not part of the NativeShell application.

The eight original PNG portraits remain identifiable by their stable cosmetic IDs:

| Cosmetic ID | Source file | Palette |
| --- | --- | --- |
| `portrait-night-dealer` | `night-dealer.png` | Midnight navy / copper |
| `portrait-card-shark` | `card-shark.png` | Petrol / platinum |
| `portrait-moon-cat` | `moon-cat.png` | Lavender / silver |
| `portrait-neon-ace` | `neon-ace.png` | Cyan / magenta / black chrome |
| `portrait-lucky-raven` | `lucky-raven.png` | Emerald / gold |
| `portrait-midnight-lady` | `midnight-lady.png` | Burgundy / moonstone |
| `portrait-golden-joker` | `golden-joker.png` | Onyx / amber |
| `portrait-velvet-king` | `velvet-king.png` | Imperial violet / ruby |

Additional JPG portraits for later collections and cosmetic sets are also held in this folder and mirrored in the local web assets. The image assets affect only visual presentation; ownership, purchasing, equipment, reward grants and saved cosmetic IDs remain native C# decisions.

## The Secret Guest

The reward-only `portrait-secret-guest` cosmetic is earned through the Sudoku **Master Key** challenge. The current React Shop displays the corresponding `secret-guest.svg` animated presentation, with the packaged `secret-guest.jpg` still image available for reduced motion. Ownership, reward claims and save compatibility use the existing stable ID.

Portrait resources are packaged offline; they do not require web downloads during gameplay. See [Cosmetic Shop documentation](../../../docs/shop-cosmetics-expansion.md) and [architecture](../../../docs/architecture.md).
