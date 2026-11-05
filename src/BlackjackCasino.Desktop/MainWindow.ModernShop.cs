using System.Globalization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    /// <summary>
    /// React displays the inventory; C# owns every price, unlock, balance and save.
    /// This is intentionally derived from the live catalog and active player profile.
    /// </summary>
    private object GetModernShopSnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        return new
        {
            Coins = profile.Coins.ToString("N0", CultureInfo.InvariantCulture),
            profile.Prestige,
            Total = CosmeticCatalog.All.Count,
            Owned = CosmeticCatalog.All.Count(c => profile.OwnedCosmeticIds.Contains(c.Id)),
            Items = CosmeticCatalog.All.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                Kind = c.Kind.ToString(),
                c.VisualVariant,
                c.Price,
                c.ShopAvailable,
                c.RequiredPrestige,
                Owned = profile.OwnedCosmeticIds.Contains(c.Id),
                Equipped = c.Id == (c.Kind switch
                {
                    CosmeticKind.Title => profile.EquippedTitleId,
                    CosmeticKind.Frame => profile.EquippedFrameId,
                    CosmeticKind.Portrait => profile.EquippedPortraitId,
                    CosmeticKind.CardSet => profile.EquippedCardSetId,
                    CosmeticKind.DiceSet => profile.EquippedDiceSetId,
                    _ => string.Empty,
                }),
                CanBuy = c.ShopAvailable && profile.Prestige >= c.RequiredPrestige && profile.Coins >= c.Price,
            }).ToArray(),
            Packs = CollectionCatalog.Packs.Select(pack => new
            {
                Rarity = pack.Rarity.ToString(),
                pack.Price,
                Owned = profile.Collection.Packs.GetValueOrDefault(pack.Rarity),
                CanBuy = profile.Coins >= pack.Price,
                Guaranteed = pack.GuaranteedRarity.ToString(),
                pack.UltraRareChance,
            }).ToArray(),
        };
    }

    private (bool Success, string Message) ModernShopCosmeticAction(string id)
    {
        if (progressionController is null)
            return (false, "Player profile is not available.");
        CosmeticDefinition? cosmetic = CosmeticCatalog.TryGet(id);
        if (cosmetic is null)
            return (false, "Unknown cosmetic item.");
        PlayerProfileState profile = progressionController.Profile;
        bool owned = profile.OwnedCosmeticIds.Contains(id);
        if (cosmetic.Id == (cosmetic.Kind switch
        {
            CosmeticKind.Title => profile.EquippedTitleId,
            CosmeticKind.Frame => profile.EquippedFrameId,
            CosmeticKind.Portrait => profile.EquippedPortraitId,
            CosmeticKind.CardSet => profile.EquippedCardSetId,
            CosmeticKind.DiceSet => profile.EquippedDiceSetId,
            _ => string.Empty,
        })) return (true, cosmetic.Name + " is already equipped.");

        if (!owned)
        {
            PurchaseResult purchase = progressionController.PurchaseCosmetic(id);
            if (!purchase.Success) return (false, purchase.Message);
        }
        bool equipped = progressionController.EquipCosmetic(id);
        RefreshProfileButton();
        RefreshCollectionAlbumHeaderButton();
        return equipped
            ? (true, "Equipped " + cosmetic.Name + ".")
            : (false, cosmetic.Name + " was purchased but could not be equipped.");
    }

    private (bool Success, string Message) ModernShopPackAction(string rarity)
    {
        if (progressionController is null)
            return (false, "Player profile is not available.");
        if (!Enum.TryParse(rarity, ignoreCase: false, out CollectionRarity parsed)
            || !Enum.IsDefined(parsed)
            || !CollectionCatalog.Packs.Any(pack => pack.Rarity == parsed))
            return (false, "Unknown card pack.");
        CollectionActionResult result = progressionController.PurchaseCollectionPack(parsed);
        if (result.Success)
        {
            RefreshProfileButton();
            RefreshCollectionAlbumHeaderButton();
        }
        return (result.Success, result.Message);
    }

    private void OpenModernShopFromProgression()
    {
        if (modernLobby is not { Ready: true } || modernLobby.Failed)
        {
            OpenProgressionPage("Cosmetic Shop");
            return;
        }

        // When the player comes from a native Season/Challenges page, close that
        // page before revealing the existing React compositor. No duplicate WPF
        // shop or second WebView2 is created.
        while (MetaPageOpen)
            CloseMetaPage();
        // Change the React route while the browser is still hidden, then reveal
        // its prepared shop surface. This avoids a single-frame lobby flash.
        modernLobby.ShowShop();
        modernLobby.SetLobbyVisible(true);
        UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
    }

    private void OnModernShopClosed()
    {
        // A shop opened from another game temporarily uses the lobby WebView2
        // above that game. Returning must restore the original game surface.
        if (lobby?.Visibility != System.Windows.Visibility.Visible)
            modernLobby?.SetLobbyVisible(false);
        UpdateHostHeaderVisibility(embeddedGameOverlay?.IsVisible == true);
    }
}
