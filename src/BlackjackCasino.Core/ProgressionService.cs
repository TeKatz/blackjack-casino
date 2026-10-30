namespace BlackjackCasino.Core;

public sealed class ProgressionService
{
    private readonly HashSet<Guid> processedRounds = [];

    public ProgressionService(PlayerProfileState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        EnsureDefaults();
    }

    public PlayerProfileState State { get; }

    public StatisticsSnapshot SessionStatistics { get; } = new();

    public bool CanPrestige => State.Level >= ProgressionCurve.MaximumLevel;

    public long ExperienceToNextLevel => State.Level >= ProgressionCurve.MaximumLevel
        ? 0
        : ProgressionCurve.ExperienceForNextLevel(State.Level);

    public ProgressionAward ProcessRound(RoundHistoryEntry round)
    {
        ArgumentNullException.ThrowIfNull(round);
        if (!processedRounds.Add(round.RoundId))
        {
            return ProgressionAward.None;
        }

        UpdateStatistics(State.LifetimeStatistics, round);
        UpdateStatistics(SessionStatistics, round);

        string tableKey = ResolveTableKey(round.Rules);
        if (!State.TableStatistics.TryGetValue(tableKey, out StatisticsSnapshot? tableStats))
        {
            tableStats = new StatisticsSnapshot();
            State.TableStatistics[tableKey] = tableStats;
        }
        UpdateStatistics(tableStats, round);

        long playerXp = CalculatePlayerExperience(round);
        long seasonXp = CalculateSeasonExperience(round);
        ProgressionAward levelAward = GrantPlayerExperience(playerXp);
        ProgressionAward seasonAward = GrantSeasonExperience(State.ActiveSeasonId, seasonXp);

        State.LastPlayedAt = DateTimeOffset.UtcNow;
        return MergeAwards(levelAward, seasonAward, playerXp, seasonXp);
    }

    public ProgressionAward GrantPlayerExperience(long experience)
    {
        if (experience <= 0)
        {
            return ProgressionAward.None;
        }

        State.LifetimeExperience += experience;
        if (State.Level >= ProgressionCurve.MaximumLevel)
        {
            return new ProgressionAward(experience, 0, [], 0, 0, []);
        }

        State.LevelExperience += experience;
        var levels = new List<int>();
        long coins = 0;

        while (State.Level < ProgressionCurve.MaximumLevel)
        {
            long requirement = ProgressionCurve.ExperienceForNextLevel(State.Level);
            if (State.LevelExperience < requirement)
            {
                break;
            }

            State.LevelExperience -= requirement;
            State.Level++;
            levels.Add(State.Level);
            long reward = ProgressionCurve.CoinsForLevel(State.Level);
            State.Coins += reward;
            coins += reward;
        }

        if (State.Level >= ProgressionCurve.MaximumLevel)
        {
            State.Level = ProgressionCurve.MaximumLevel;
            State.LevelExperience = 0;
        }

        return new ProgressionAward(experience, 0, levels, 0, coins, []);
    }

    public ProgressionAward GrantSeasonExperience(string seasonId, long experience)
    {
        if (experience <= 0)
        {
            return ProgressionAward.None;
        }

        SeasonDefinition season = SeasonCatalog.Get(seasonId);
        SeasonProgressState progress = GetOrCreateSeasonProgress(season.Id);
        progress.Experience += experience;

        int previousTier = progress.Tier;
        int targetTier = season.Tiers
            .Where(x => progress.Experience >= x.RequiredExperience)
            .Select(x => x.Tier)
            .DefaultIfEmpty(0)
            .Max();

        long coins = 0;
        var unlocked = new List<CosmeticDefinition>();
        for (int tier = previousTier + 1; tier <= targetTier; tier++)
        {
            SeasonTierDefinition reward = season.Tiers[tier - 1];
            State.Coins += reward.Coins;
            coins += reward.Coins;

            if (reward.CosmeticId is not null)
            {
                CosmeticDefinition cosmetic = CosmeticCatalog.Get(reward.CosmeticId);
                if (EnsureOwned(cosmetic.Id))
                {
                    unlocked.Add(cosmetic);
                }
            }
        }

        progress.Tier = targetTier;
        return new ProgressionAward(0, experience, [], targetTier - previousTier, coins, unlocked);
    }

    public PurchaseResult PurchaseCosmetic(string cosmeticId)
    {
        CosmeticDefinition? cosmetic = CosmeticCatalog.TryGet(cosmeticId);
        if (cosmetic is null || !cosmetic.ShopAvailable)
        {
            return new PurchaseResult(false, "This cosmetic is not available in the shop.");
        }

        if (State.OwnedCosmeticIds.Contains(cosmetic.Id, StringComparer.Ordinal))
        {
            return new PurchaseResult(false, "You already own this cosmetic.", cosmetic);
        }

        if (State.Prestige < cosmetic.RequiredPrestige)
        {
            return new PurchaseResult(false, $"Reach Player Prestige {cosmetic.RequiredPrestige} to unlock {cosmetic.Name}.", cosmetic);
        }

        if (State.Coins < cosmetic.Price)
        {
            return new PurchaseResult(false, $"You need {cosmetic.Price - State.Coins:N0} more coins.", cosmetic);
        }

        State.Coins -= cosmetic.Price;
        EnsureOwned(cosmetic.Id);
        return new PurchaseResult(true, $"Purchased {cosmetic.Name}.", cosmetic);
    }

    public bool EquipCosmetic(string cosmeticId)
    {
        if (!State.OwnedCosmeticIds.Contains(cosmeticId, StringComparer.Ordinal))
        {
            return false;
        }

        CosmeticDefinition? cosmetic = CosmeticCatalog.TryGet(cosmeticId);
        if (cosmetic is null)
        {
            return false;
        }

        switch (cosmetic.Kind)
        {
            case CosmeticKind.Title:
                State.EquippedTitleId = cosmetic.Id;
                return true;
            case CosmeticKind.Frame:
                State.EquippedFrameId = cosmetic.Id;
                return true;
            case CosmeticKind.Portrait:
                State.EquippedPortraitId = cosmetic.Id;
                return true;
            case CosmeticKind.DiceSet:
                State.EquippedDiceSetId = cosmetic.Id;
                return true;
            case CosmeticKind.CardSet:
                State.EquippedCardSetId = cosmetic.Id;
                return true;
            default:
                return false;
        }
    }

    public ChallengeClaimResult ClaimChallenge(string challengeId)
    {
        lock (State)
        {
            ChallengeDefinition? challenge = ChallengeCatalog.TryGet(challengeId);
            if (challenge is null)
            {
                return new ChallengeClaimResult(false, "Unknown challenge.");
            }

            if (State.ClaimedChallengeIds.Contains(challenge.Id, StringComparer.Ordinal))
            {
                return new ChallengeClaimResult(false, "Challenge reward already claimed.", challenge);
            }

            decimal progress = ChallengeCatalog.Progress(State, challenge);
            if (progress < challenge.Goal)
            {
                return new ChallengeClaimResult(false, "Challenge is not complete yet.", challenge);
            }

            long balance = checked(State.Coins + challenge.CoinReward);
            State.ClaimedChallengeIds.Add(challenge.Id);
            State.Coins = balance;
            return new ChallengeClaimResult(true, $"Claimed {challenge.CoinReward:N0} coins for {challenge.Name}.", challenge, challenge.CoinReward);
        }
    }

    /// <summary>Claims one snapshot of earned rewards, with no partial mutation or duplicate payment.</summary>
    public ChallengeClaimAllResult ClaimAllChallenges()
    {
        // A profile can be shared by multiple services. Single and bulk claims use the same gate.
        lock (State)
        {
            var claimed = State.ClaimedChallengeIds.ToHashSet(StringComparer.Ordinal);
            ChallengeDefinition[] available = ChallengeCatalog.All
                .Where(challenge => !claimed.Contains(challenge.Id) && ChallengeCatalog.Complete(State, challenge))
                .ToArray();
            if (available.Length == 0)
            {
                return new(false, "No completed challenge rewards to claim.", []);
            }

            // Check both sums before touching either the wallet or the claim ledger.
            long coins = available.Aggregate(0L, (total, challenge) => checked(total + challenge.CoinReward));
            long balance = checked(State.Coins + coins);
            var updatedClaims = new List<string>(State.ClaimedChallengeIds);
            updatedClaims.AddRange(available.Select(challenge => challenge.Id));
            State.ClaimedChallengeIds = updatedClaims;
            State.Coins = balance;
            return new(true, $"Claimed {coins:N0} coins from {available.Length:N0} challenges.", Array.AsReadOnly(available), coins);
        }
    }

    public PrestigeResult Prestige()
    {
        if (!CanPrestige)
        {
            return new PrestigeResult(false, State.Prestige, 0, [], "Reach level 100 before entering Prestige.");
        }

        State.Prestige++;
        State.Level = 0;
        State.LevelExperience = 0;

        const long prestigeCoins = 500;
        State.Coins += prestigeCoins;

        var unlocked = new List<CosmeticDefinition>();
        foreach (CosmeticDefinition reward in CosmeticCatalog.PrestigeRewards(State.Prestige))
        {
            if (EnsureOwned(reward.Id))
            {
                unlocked.Add(reward);
            }
        }

        string rewardText = unlocked.Count == 0
            ? string.Empty
            : $" Unlocked {string.Join(", ", unlocked.Select(x => x.Name))}.";
        return new PrestigeResult(true, State.Prestige, prestigeCoins, unlocked, $"Prestige {State.Prestige} reached. Level reset to 0.{rewardText}");
    }

    public void AddPlaytime(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        State.TotalPlaytimeSeconds += (long)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero);
        State.LastPlayedAt = DateTimeOffset.UtcNow;
    }

    public SeasonProgressState GetOrCreateSeasonProgress(string seasonId)
    {
        if (!State.Seasons.TryGetValue(seasonId, out SeasonProgressState? progress))
        {
            progress = new SeasonProgressState { SeasonId = seasonId };
            State.Seasons[seasonId] = progress;
        }

        return progress;
    }

    private void EnsureDefaults()
    {
        ProfileNormalization.Normalize(State);
        EnsureOwned(CosmeticCatalog.DefaultTitleId);
        EnsureOwned(CosmeticCatalog.DefaultFrameId);
        EnsureOwned(CosmeticCatalog.DefaultPortraitId);
        EnsureOwned(CosmeticCatalog.DefaultCardSetId);
        EnsureOwned(CosmeticCatalog.DefaultDiceSetId);
        State.ClaimedChallengeIds ??= [];
        State.ClaimedChallengeIds = State.ClaimedChallengeIds
            .Where(id => ChallengeCatalog.TryGet(id) is not null)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (CosmeticCatalog.TryGet(State.EquippedTitleId) is not { Kind: CosmeticKind.Title }
            || !State.OwnedCosmeticIds.Contains(State.EquippedTitleId, StringComparer.Ordinal))
        {
            State.EquippedTitleId = CosmeticCatalog.DefaultTitleId;
        }

        if (CosmeticCatalog.TryGet(State.EquippedFrameId) is not { Kind: CosmeticKind.Frame }
            || !State.OwnedCosmeticIds.Contains(State.EquippedFrameId, StringComparer.Ordinal))
        {
            State.EquippedFrameId = CosmeticCatalog.DefaultFrameId;
        }

        if (CosmeticCatalog.TryGet(State.EquippedPortraitId) is not { Kind: CosmeticKind.Portrait }
            || !State.OwnedCosmeticIds.Contains(State.EquippedPortraitId, StringComparer.Ordinal))
        {
            State.EquippedPortraitId = CosmeticCatalog.DefaultPortraitId;
        }

        if (CosmeticCatalog.TryGet(State.EquippedCardSetId) is not { Kind: CosmeticKind.CardSet }
            || !State.OwnedCosmeticIds.Contains(State.EquippedCardSetId, StringComparer.Ordinal))
        {
            State.EquippedCardSetId = CosmeticCatalog.DefaultCardSetId;
        }

        if (CosmeticCatalog.TryGet(State.EquippedDiceSetId) is not { Kind: CosmeticKind.DiceSet }
            || !State.OwnedCosmeticIds.Contains(State.EquippedDiceSetId, StringComparer.Ordinal))
            State.EquippedDiceSetId = CosmeticCatalog.DefaultDiceSetId;

        SeasonDefinition? active = SeasonCatalog.All.FirstOrDefault(x => string.Equals(x.Id, State.ActiveSeasonId, StringComparison.Ordinal));
        if (active is null || active.Archived)
        {
            // Season 1 remains stored and owned rewards are never revoked; existing saves simply
            // begin earning the new long-form current season from their next completed cycle.
            State.ActiveSeasonId = SeasonCatalog.Current.Id;
        }

        _ = GetOrCreateSeasonProgress(State.ActiveSeasonId);
        State.DisplayName = ProfileIdentity.Restore(State.DisplayName);
    }

    private bool EnsureOwned(string cosmeticId)
    {
        if (State.OwnedCosmeticIds.Contains(cosmeticId, StringComparer.Ordinal))
        {
            return false;
        }

        State.OwnedCosmeticIds.Add(cosmeticId);
        return true;
    }

    private static ProgressionAward MergeAwards(
        ProgressionAward player,
        ProgressionAward season,
        long playerExperience,
        long seasonExperience) => new(
            playerExperience,
            seasonExperience,
            player.LevelsGained,
            season.SeasonTiersGained,
            player.CoinsEarned + season.CoinsEarned,
            player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());

    private static long CalculatePlayerExperience(RoundHistoryEntry round)
    {
        long xp = 20;
        xp += Math.Max(0, round.PlayerHands.Count - 1) * 5L;
        xp += round.PlayerHands.Count(x => x.Outcome is HandOutcome.Win or HandOutcome.Blackjack) * 5L;
        xp += round.PlayerHands.Count(x => x.Outcome == HandOutcome.Blackjack) * 10L;
        xp += round.PlayerHands.Count(x => x.IsDoubled && x.Outcome is HandOutcome.Win or HandOutcome.Blackjack) * 5L;
        return xp;
    }

    private static long CalculateSeasonExperience(RoundHistoryEntry round)
    {
        long xp = 15;
        if (round.TotalNet > 0)
        {
            xp += 5;
        }
        xp += round.PlayerHands.Count(x => x.Outcome == HandOutcome.Blackjack) * 5L;
        return xp;
    }

    private static void UpdateStatistics(StatisticsSnapshot stats, RoundHistoryEntry round)
    {
        stats.RoundsPlayed++;
        stats.HandsPlayed += round.PlayerHands.Count;
        stats.HandsWon += round.PlayerHands.Count(x => x.Outcome is HandOutcome.Win or HandOutcome.Blackjack);
        stats.HandsLost += round.PlayerHands.Count(x => x.Outcome is HandOutcome.Loss or HandOutcome.Surrender);
        stats.Pushes += round.PlayerHands.Count(x => x.Outcome == HandOutcome.Push);
        stats.Blackjacks += round.PlayerHands.Count(x => x.Outcome == HandOutcome.Blackjack);
        stats.Doubles += round.PlayerHands.Count(x => x.IsDoubled);
        stats.DoubleWins += round.PlayerHands.Count(x => x.IsDoubled && x.Outcome is HandOutcome.Win or HandOutcome.Blackjack);
        stats.Splits += round.Actions.Count(x => x.Action == PlayerAction.Split);
        stats.Surrenders += round.PlayerHands.Count(x => x.IsSurrendered);
        if (round.InsuranceBet > 0)
        {
            stats.InsuranceTaken++;
        }

        stats.TotalWagered += round.PlayerHands.Sum(x => x.Bet) + round.InsuranceBet;
        stats.NetProfit += round.TotalNet;
        stats.InsuranceNet += round.InsuranceNet;
        stats.BiggestWin = Math.Max(stats.BiggestWin, Math.Max(0m, round.TotalNet));
        stats.BiggestLoss = Math.Max(stats.BiggestLoss, Math.Max(0m, -round.TotalNet));

        if (stats.RoundsPlayed == 1)
        {
            stats.HighestBankroll = Math.Max(round.BankrollBefore, round.BankrollAfter);
            stats.LowestBankroll = Math.Min(round.BankrollBefore, round.BankrollAfter);
        }
        else
        {
            stats.HighestBankroll = Math.Max(stats.HighestBankroll, round.BankrollAfter);
            stats.LowestBankroll = Math.Min(stats.LowestBankroll, round.BankrollAfter);
        }

        if (round.TotalNet > 0)
        {
            stats.CurrentWinStreak++;
            stats.CurrentLossStreak = 0;
            stats.BestWinStreak = Math.Max(stats.BestWinStreak, stats.CurrentWinStreak);
        }
        else if (round.TotalNet < 0)
        {
            stats.CurrentLossStreak++;
            stats.CurrentWinStreak = 0;
            stats.WorstLossStreak = Math.Max(stats.WorstLossStreak, stats.CurrentLossStreak);
        }
        else
        {
            stats.CurrentWinStreak = 0;
            stats.CurrentLossStreak = 0;
        }
    }

    private static string ResolveTableKey(GameRules rules)
    {
        TablePreset? preset = TablePresets.All.FirstOrDefault(x => x.Rules == rules);
        return preset?.Name ?? "Custom";
    }
}
