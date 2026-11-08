using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    // Album snapshots contain only catalogue-backed card IDs and display data.
    // The browser never rolls a card, consumes an inventory item or credits a wallet.
    private object GetModernAlbumSnapshot()
    {
        PlayerProfileState profile = progressionController!.Profile;
        CollectionState collection = profile.Collection;
        return new
        {
            TotalCards = CollectionCatalog.Cards.Count,
            OwnedCards = collection.OwnedCardIds.Count,
            CompletedThemes = CollectionCatalog.Themes.Count(theme =>
                theme.Cards.All(card => collection.OwnedCardIds.Contains(card.Id))),
            collection.PacksOpened,
            collection.DuplicatePoints,
            DuplicatePackCost = CollectionCatalog.DuplicatePackCost,
            Coins = profile.Coins.ToString("N0", Localization.Culture),
            Packs = CollectionCatalog.Packs.Select(pack => new
            {
                Rarity = pack.Rarity.ToString(),
                pack.Price,
                Owned = collection.Packs.GetValueOrDefault(pack.Rarity),
                Guaranteed = pack.GuaranteedRarity.ToString(),
                pack.UltraRareChance,
            }).ToArray(),
            Themes = CollectionCatalog.Themes.Select(theme =>
            {
                int owned = theme.Cards.Count(card => collection.OwnedCardIds.Contains(card.Id));
                CollectionThemeCompletionReward reward = CollectionService.ThemeCompletionReward(theme);
                return new
                {
                    theme.Id,
                    theme.Name,
                    theme.Symbol,
                    theme.Accent,
                    theme.UltraRare,
                    Owned = owned,
                    Total = theme.Cards.Count,
                    RewardReady = owned == theme.Cards.Count &&
                        !collection.ClaimedThemeRewardIds.Contains(theme.Id),
                    RewardClaimed = collection.ClaimedThemeRewardIds.Contains(theme.Id),
                    RewardCoins = reward.Coins,
                    RewardBankroll = Money(reward.Bankroll),
                    RewardPack = reward.PackRarity.ToString(),
                    Cards = theme.Cards.Select(card => new
                    {
                        card.Id,
                        card.Name,
                        card.Number,
                        Rarity = card.Rarity.ToString(),
                        Owned = collection.OwnedCardIds.Contains(card.Id),
                        Lore = collection.OwnedCardIds.Contains(card.Id) ? CollectionLore.BodyFor(card) : "",
                    }).ToArray(),
                };
            }).ToArray(),
            Pending = GetModernAlbumPendingSnapshot(),
        };
    }

    // The durable five-card receipt can be returned with the command reply.
    // It must not depend on validating the entire 136-card Album snapshot.
    private object? GetModernAlbumPendingSnapshot()
    {
        CollectionOpening? opening = progressionController?.Profile.Collection.PendingOpening;
        if (opening is null) return null;
        return new
            {
                Id = opening.Id.ToString("N"),
                Rarity = opening.PackRarity.ToString(),
                opening.PointsEarned,
                opening.PacksEarned,
                NewCards = opening.Cards.Count(draw => draw.IsNew),
                Cards = opening.Cards.Select(draw =>
                {
                    CollectionCard card = CollectionCatalog.Find(draw.CardId)!;
                    CollectionTheme theme = CollectionCatalog.Theme(card);
                    return new
                    {
                        card.Id,
                        card.Name,
                        card.Number,
                        Rarity = card.Rarity.ToString(),
                        ThemeId = theme.Id,
                        Theme = theme.Name,
                        theme.Accent,
                        theme.UltraRare,
                        draw.IsNew,
                        draw.DuplicatePoints,
                    };
                }).ToArray(),
            };
    }

    private (bool Success, string Message) ModernAlbumOpen(string rarity)
    {
        if (progressionController is null) return (false, "Player profile unavailable.");
        if (!Enum.TryParse(rarity, ignoreCase: false, out CollectionRarity parsed)
            || !Enum.IsDefined(parsed)
            || !CollectionCatalog.Packs.Any(pack => pack.Rarity == parsed))
            return (false, "Unknown card pack rarity.");
        CollectionActionResult result = progressionController.OpenCollectionPack(parsed);
        if (result.Success)
        {
            RefreshProfileButton();
            RefreshCollectionAlbumHeaderButton();
        }
        return (result.Success, result.Message);
    }

    private (bool Success, string Message) ModernAlbumComplete(string id)
    {
        if (progressionController is null) return (false, "Player profile unavailable.");
        if (!Guid.TryParseExact(id, "N", out Guid receipt))
            return (false, "Invalid pack opening receipt.");
        CollectionActionResult result = progressionController.CompleteCollectionOpening(receipt);
        if (result.Success) RefreshCollectionAlbumHeaderButton();
        return (result.Success, result.Message);
    }

    private (bool Success, string Message) ModernAlbumClaim(string id)
    {
        if (progressionController is null) return (false, "Player profile unavailable.");
        if (!CollectionCatalog.Themes.Any(theme => theme.Id == id))
            return (false, "Unknown collection theme.");
        CollectionThemeRewardClaimResult result = progressionController.ClaimCollectionThemeReward(id);
        if (result.Success)
        {
            if (result.Bankroll > 0m) CreditAlbumCompletionReward(result.Bankroll);
            RefreshProfileButton();
            RefreshCollectionAlbumHeaderButton();
        }
        return (result.Success, result.Message);
    }
}
