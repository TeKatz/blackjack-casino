namespace BlackjackCasino.Core.Collections;

public sealed class CollectionState
{
    public int Version { get; set; }
    public HashSet<string> OwnedCardIds { get; set; } = new(StringComparer.Ordinal);
    public Dictionary<CollectionRarity, int> Packs { get; set; } = [];
    public HashSet<string> ClaimedThemeRewardIds { get; set; } = new(StringComparer.Ordinal);
    public int DuplicatePoints { get; set; }
    public long PacksOpened { get; set; }
    public CollectionOpening? PendingOpening { get; set; }
}

public sealed record CollectionDraw(string CardId, bool IsNew, int DuplicatePoints);
public sealed record CollectionOpening(Guid Id, CollectionRarity PackRarity, IReadOnlyList<CollectionDraw> Cards, int PointsEarned, int PacksEarned);
public sealed record CollectionActionResult(bool Success, string Message, CollectionOpening? Opening = null);
public sealed record CollectionThemeCompletionReward(long Coins, CollectionRarity PackRarity, int PackCount = 1);
public sealed record CollectionThemeRewardClaimResult(
    bool Success,
    string Message,
    string ThemeId,
    long Coins = 0,
    CollectionRarity? PackRarity = null,
    int PackCount = 0);

/// <summary>Profile-owned collection; presentation never rolls cards or awards inventory.</summary>
public static class CollectionService
{
    public static void Normalize(PlayerProfileState profile)
    {
        profile.Collection ??= new();
        CollectionState state = profile.Collection;
        state.OwnedCardIds ??= new(StringComparer.Ordinal);
        state.OwnedCardIds.RemoveWhere(id => id is null || CollectionCatalog.Find(id) is null);
        state.Packs ??= [];
        state.ClaimedThemeRewardIds ??= new(StringComparer.Ordinal);
        var knownThemes = CollectionCatalog.Themes.Select(theme => theme.Id).ToHashSet(StringComparer.Ordinal);
        state.ClaimedThemeRewardIds.RemoveWhere(id => string.IsNullOrWhiteSpace(id) || !knownThemes.Contains(id));
        foreach (CollectionRarity rarity in state.Packs.Keys.ToArray())
        {
            if (!Enum.IsDefined(rarity)) state.Packs.Remove(rarity);
            else state.Packs[rarity] = Math.Max(0, state.Packs[rarity]);
        }
        state.DuplicatePoints = Math.Clamp(state.DuplicatePoints, 0, CollectionCatalog.DuplicatePackCost - 1);
        state.PacksOpened = Math.Max(0, state.PacksOpened);
        if (state.PendingOpening is { } opening && (opening.Id == Guid.Empty || !Enum.IsDefined(opening.PackRarity)
            || opening.Cards is null || opening.Cards.Count != CollectionCatalog.CardsPerPack
            || opening.Cards.Any(c => c is null || c.CardId is null || CollectionCatalog.Find(c.CardId) is null)))
            state.PendingOpening = null;
        if (state.Version < CollectionCatalog.Version)
        {
            // A single welcome gift for both new and migrated profiles; no historical reward replay.
            state.Packs[CollectionRarity.Common] = checked(state.Packs.GetValueOrDefault(CollectionRarity.Common) + 3);
            state.Version = CollectionCatalog.Version;
        }
    }

    public static CollectionThemeCompletionReward ThemeCompletionReward(CollectionTheme theme) => theme.UltraRare
        ? new(750, CollectionRarity.Epic)
        : new(300, CollectionRarity.Rare);

    public static bool IsThemeComplete(PlayerProfileState profile, CollectionTheme theme)
    {
        lock (profile)
        {
            Normalize(profile);
            return theme.Cards.All(card => profile.Collection.OwnedCardIds.Contains(card.Id));
        }
    }

    public static CollectionThemeRewardClaimResult ClaimThemeCompletionReward(PlayerProfileState profile, string themeId)
    {
        lock (profile)
        {
            Normalize(profile);
            CollectionState state = profile.Collection;
            CollectionTheme? theme = CollectionCatalog.Themes.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, themeId, StringComparison.Ordinal));
            if (theme is null) return new(false, "Unknown collection theme.", themeId ?? string.Empty);
            if (state.ClaimedThemeRewardIds.Contains(theme.Id))
                return new(false, "This theme completion reward has already been claimed.", theme.Id);
            if (!theme.Cards.All(card => state.OwnedCardIds.Contains(card.Id)))
                return new(false, $"Collect all {theme.Cards.Count} cards in {theme.Name} before claiming this reward.", theme.Id);

            CollectionThemeCompletionReward reward = ThemeCompletionReward(theme);
            long updatedCoins = checked(profile.Coins + reward.Coins);
            int updatedPacks = checked(state.Packs.GetValueOrDefault(reward.PackRarity) + reward.PackCount);

            profile.Coins = updatedCoins;
            state.Packs[reward.PackRarity] = updatedPacks;
            state.ClaimedThemeRewardIds.Add(theme.Id);
            return new(true,
                $"{theme.Name} complete! +{reward.Coins:N0} Cosmetic Coins and {reward.PackCount} {reward.PackRarity} pack awarded.",
                theme.Id, reward.Coins, reward.PackRarity, reward.PackCount);
        }
    }

    public static void GrantPack(PlayerProfileState profile, CollectionRarity rarity, int count = 1)
    {
        if (!Enum.IsDefined(rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        lock (profile)
        {
            Normalize(profile);
            profile.Collection.Packs[rarity] = checked(profile.Collection.Packs.GetValueOrDefault(rarity) + count);
        }
    }

    public static CollectionActionResult PurchasePack(PlayerProfileState profile, CollectionRarity rarity)
    {
        lock (profile)
        {
            Normalize(profile);
            if (!Enum.IsDefined(rarity)) return new(false, "Unknown pack.");
            CollectionPack pack = CollectionCatalog.Pack(rarity);
            if (profile.Coins < pack.Price) return new(false, $"You need {pack.Price - profile.Coins:N0} more coins.");
            GrantPack(profile, rarity);
            profile.Coins -= pack.Price;
            return new(true, $"Purchased a {rarity} pack. Open it in the Album.");
        }
    }

    public static CollectionActionResult OpenPack(PlayerProfileState profile, CollectionRarity rarity, Random? random = null)
    {
        lock (profile)
        {
            Normalize(profile);
            CollectionState state = profile.Collection;
            if (state.PendingOpening is not null) return new(false, "Finish the current pack reveal first.", state.PendingOpening);
            if (!Enum.IsDefined(rarity) || state.Packs.GetValueOrDefault(rarity) <= 0) return new(false, "You do not own this pack.");
            random ??= Random.Shared;
            CollectionPack pack = CollectionCatalog.Pack(rarity);
            var owned = new HashSet<string>(state.OwnedCardIds, StringComparer.Ordinal);
            var draws = new List<CollectionDraw>(CollectionCatalog.CardsPerPack);
            int points = 0;
            for (int slot = 0; slot < CollectionCatalog.CardsPerPack; slot++)
            {
                CollectionCard card = Draw(pack, slot == 0 ? pack.GuaranteedRarity : CollectionRarity.Common, random);
                bool isNew = owned.Add(card.Id);
                int duplicate = isNew ? 0 : CollectionCatalog.DuplicatePoints(card.Rarity);
                points += duplicate;
                draws.Add(new(card.Id, isNew, duplicate));
            }
            int totalPoints = state.DuplicatePoints + points;
            int earned = totalPoints / CollectionCatalog.DuplicatePackCost;
            var opening = new CollectionOpening(Guid.NewGuid(), rarity, draws.AsReadOnly(), points, earned);
            // Compute all fallible arithmetic before changing the profile.
            long openedCount = checked(state.PacksOpened + 1);
            int commonCount = checked(state.Packs.GetValueOrDefault(CollectionRarity.Common)
                - (rarity == CollectionRarity.Common ? 1 : 0) + earned);
            state.Packs[rarity]--;
            state.Packs[CollectionRarity.Common] = commonCount;
            state.DuplicatePoints = totalPoints % CollectionCatalog.DuplicatePackCost;
            state.OwnedCardIds = owned;
            state.PacksOpened = openedCount;
            state.PendingOpening = opening;
            return new(true, $"Opened five cards · {draws.Count(c => c.IsNew)} new · +{points} duplicate points · {earned} bonus packs.", opening);
        }
    }

    public static bool CompleteOpening(PlayerProfileState profile, Guid id)
    {
        lock (profile)
        {
            if (id == Guid.Empty || profile.Collection.PendingOpening?.Id != id) return false;
            profile.Collection.PendingOpening = null;
            return true;
        }
    }

    private static CollectionCard Draw(CollectionPack pack, CollectionRarity floor, Random random)
    {
        // Chase themes are an independent low-probability roll, never the ordinary Legendary pool.
        bool ultra = random.NextDouble() < pack.UltraRareChance;
        if (ultra)
        {
            CollectionCard[] chase = CollectionCatalog.Themes.Where(t => t.UltraRare).SelectMany(t => t.Cards).ToArray();
            return chase[random.Next(chase.Length)];
        }
        int start = (int)floor;
        int total = pack.Weights.Skip(start).Sum();
        int roll = random.Next(total);
        int rarity = start;
        for (; rarity < pack.Weights.Count - 1; rarity++)
        {
            if (roll < pack.Weights[rarity]) break;
            roll -= pack.Weights[rarity];
        }
        CollectionCard[] pool = CollectionCatalog.Themes.Where(t => !t.UltraRare).SelectMany(t => t.Cards)
            .Where(c => (int)c.Rarity == rarity).ToArray();
        return pool[random.Next(pool.Length)];
    }
}
