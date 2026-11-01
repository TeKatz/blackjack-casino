using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class ProgressionTests
{
    [Fact]
    public void LevelCurve_ReachesLevel100AtFiniteLongTermTarget()
    {
        long total = ProgressionCurve.TotalExperienceToLevel(100);

        Assert.InRange(total, 100_000, 130_000);
        Assert.True(ProgressionCurve.ExperienceForNextLevel(99) > ProgressionCurve.ExperienceForNextLevel(1));
    }

    [Fact]
    public void GrantExperience_LevelUpsAndAwardsCoins()
    {
        var service = new ProgressionService(new PlayerProfileState());
        long required = ProgressionCurve.ExperienceForNextLevel(0);

        ProgressionAward award = service.GrantPlayerExperience(required);

        Assert.Equal(1, service.State.Level);
        Assert.Contains(1, award.LevelsGained);
        Assert.Equal(ProgressionCurve.CoinsForLevel(1), service.State.Coins);
    }

    [Fact]
    public void Prestige_ResetsOnlyLevelProgressAndKeepsEconomyAndStats()
    {
        var profile = new PlayerProfileState { Coins = 123 };
        var service = new ProgressionService(profile);
        service.GrantPlayerExperience(ProgressionCurve.TotalExperienceToLevel(100));
        profile.LifetimeStatistics.RoundsPlayed = 50;

        PrestigeResult result = service.Prestige();

        Assert.True(result.Success);
        Assert.Equal(1, profile.Prestige);
        Assert.Equal(0, profile.Level);
        Assert.Equal(0, profile.LevelExperience);
        Assert.Equal(50, profile.LifetimeStatistics.RoundsPlayed);
        Assert.True(profile.Coins > 123);
        Assert.Contains("title-prestige-1", profile.OwnedCosmeticIds);
    }

    [Fact]
    public void Shop_IsCosmeticOnlyAndUsesEarnedCoins()
    {
        var profile = new PlayerProfileState { Coins = 500 };
        var service = new ProgressionService(profile);

        PurchaseResult purchase = service.PurchaseCosmetic("title-night-owl");

        Assert.True(purchase.Success);
        Assert.Equal(200, profile.Coins);
        Assert.True(service.EquipCosmetic("title-night-owl"));
        Assert.Equal("title-night-owl", profile.EquippedTitleId);
    }

    [Fact]
    public void Portraits_ArePurchasableAndEquippableWithoutAffectingGameplay()
    {
        var profile = new PlayerProfileState { Coins = 1_500 };
        var service = new ProgressionService(profile);

        PurchaseResult purchase = service.PurchaseCosmetic("portrait-night-dealer");

        Assert.True(purchase.Success);
        Assert.True(service.EquipCosmetic("portrait-night-dealer"));
        Assert.Equal("portrait-night-dealer", profile.EquippedPortraitId);
        Assert.Equal(800, profile.Coins);
    }

    [Fact]
    public void ProcessingLossStillAwardsExperienceAndUpdatesStats()
    {
        var service = new ProgressionService(new PlayerProfileState());
        RoundHistoryEntry round = History(HandOutcome.Loss, -25m);

        ProgressionAward award = service.ProcessRound(round);

        Assert.True(award.PlayerExperience > 0);
        Assert.True(award.SeasonExperience > 0);
        Assert.Equal(1, service.State.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(1, service.State.LifetimeStatistics.HandsLost);
        Assert.Equal(-25m, service.State.LifetimeStatistics.NetProfit);
    }

    [Fact]
    public void CurrentSeason_IsThirdLongFormPass()
    {
        SeasonDefinition season = SeasonCatalog.Current;

        Assert.Equal("Neon Dynasty", season.Name);
        Assert.Equal("season-3-neon-dynasty", season.Id);
        Assert.Equal(120, season.Tiers.Count);
        Assert.Equal(232_800, season.Tiers[^1].RequiredExperience);
        Assert.False(season.Archived);
        Assert.True(season.RewardsEnabled);
        Assert.Equal(3, SeasonCatalog.All.Count);
    }

    [Fact]
    public void CurrentSeason_UsesIncreasingTierCostsAbovePreviousPasses()
    {
        SeasonDefinition season = SeasonCatalog.Current;
        long previous = 0;
        var tierCosts = new List<long>();

        foreach (SeasonTierDefinition tier in season.Tiers)
        {
            tierCosts.Add(tier.RequiredExperience - previous);
            previous = tier.RequiredExperience;
        }

        Assert.All(tierCosts, cost => Assert.True(cost > 200));
        Assert.Equal(512, tierCosts[0]);
        Assert.Equal(3_368, tierCosts[^1]);
        Assert.True(tierCosts[^1] > tierCosts[0]);
        Assert.True(season.Tiers[^1].RequiredExperience > SeasonCatalog.Get("season-2-velvet-eclipse").Tiers[^1].RequiredExperience);
    }

    [Fact]
    public void NeonDynasty_UsesThemedCosmeticMilestonesEveryTenTiers()
    {
        SeasonDefinition season = SeasonCatalog.Current;
        var expected = new Dictionary<int, string>
        {
            [10] = "title-neon-initiate",
            [20] = "frame-neon-thread",
            [30] = "portrait-neon-croupier",
            [40] = "title-dynasty-dealer",
            [50] = "frame-dynasty-circuit",
            [60] = "portrait-dynasty-heiress",
            [70] = "title-electric-royalty",
            [80] = "frame-neon-pagoda",
            [90] = "portrait-neon-overseer",
            [100] = "title-neon-sovereign",
            [110] = "frame-dynasty-crown",
            [120] = "portrait-neon-emperor",
        };

        foreach ((int tier, string cosmeticId) in expected)
        {
            Assert.Equal(cosmeticId, season.Tiers[tier - 1].CosmeticId);
            CosmeticDefinition cosmetic = CosmeticCatalog.Get(cosmeticId);
            Assert.False(cosmetic.ShopAvailable);
            Assert.Equal(0, cosmetic.Price);
        }

        Assert.Equal(4, expected.Values.Count(id => CosmeticCatalog.Get(id).Kind == CosmeticKind.Title));
        Assert.Equal(4, expected.Values.Count(id => CosmeticCatalog.Get(id).Kind == CosmeticKind.Frame));
        Assert.Equal(4, expected.Values.Count(id => CosmeticCatalog.Get(id).Kind == CosmeticKind.Portrait));
    }

    [Fact]
    public void NeonDynasty_GrantsCoinsAndThemedCosmeticsAcrossReachedMilestones()
    {
        var profile = new PlayerProfileState { Coins = 321 };
        var service = new ProgressionService(profile);
        SeasonTierDefinition tier30 = SeasonCatalog.Current.Tiers[29];

        ProgressionAward award = service.GrantSeasonExperience(SeasonCatalog.Current.Id, tier30.RequiredExperience);

        Assert.Equal(30, service.GetOrCreateSeasonProgress(SeasonCatalog.Current.Id).Tier);
        Assert.Equal(30, award.SeasonTiersGained);
        Assert.True(award.CoinsEarned > 0);
        Assert.True(profile.Coins > 321);
        Assert.Contains(award.CosmeticsUnlocked, cosmetic => cosmetic.Id == "title-neon-initiate");
        Assert.Contains(award.CosmeticsUnlocked, cosmetic => cosmetic.Id == "frame-neon-thread");
        Assert.Contains(award.CosmeticsUnlocked, cosmetic => cosmetic.Id == "portrait-neon-croupier");
        Assert.Contains("title-neon-initiate", profile.OwnedCosmeticIds);
        Assert.Contains("frame-neon-thread", profile.OwnedCosmeticIds);
        Assert.Contains("portrait-neon-croupier", profile.OwnedCosmeticIds);
    }

    [Fact]
    public void NeonDynasty_FinalTierIsAHighValueSeasonReward()
    {
        SeasonTierDefinition final = SeasonCatalog.Current.Tiers[^1];

        Assert.Equal(120, final.Tier);
        Assert.Equal(600, final.Coins);
        Assert.Equal("portrait-neon-emperor", final.CosmeticId);
        Assert.Equal(CosmeticKind.Portrait, CosmeticCatalog.Get(final.CosmeticId!).Kind);
    }

    [Fact]
    public void VelvetEclipse_RemainsLegacyPassWithOriginalRewards()
    {
        SeasonDefinition season = SeasonCatalog.Get("season-2-velvet-eclipse");

        Assert.Equal("Velvet Eclipse", season.Name);
        Assert.True(season.Archived);
        Assert.True(season.RewardsEnabled);
        Assert.Equal(100, season.Tiers.Count);
        Assert.Equal(120_000, season.Tiers[^1].RequiredExperience);
        Assert.Equal("portrait-eclipse-raven", season.Tiers[14].CosmeticId);
        Assert.Equal("portrait-eclipse-sovereign", season.Tiers[99].CosmeticId);
        Assert.Equal(500, season.Tiers[99].Coins);
    }

    [Fact]
    public void LegacySeason_RemainsSelectableAndKeepsItsProgress()
    {
        var profile = new PlayerProfileState
        {
            ActiveSeasonId = "season-1-midnight-shift",
            Seasons = new Dictionary<string, SeasonProgressState>(StringComparer.OrdinalIgnoreCase)
            {
                ["season-1-midnight-shift"] = new() { SeasonId = "season-1-midnight-shift", Experience = 10_000, Tier = 50 },
            },
        };

        _ = new ProgressionService(profile);

        Assert.Equal("season-1-midnight-shift", profile.ActiveSeasonId);
        Assert.Equal(50, profile.Seasons["season-1-midnight-shift"].Tier);
        Assert.True(SeasonCatalog.Get("season-1-midnight-shift").Archived);
        Assert.True(SeasonCatalog.Get("season-1-midnight-shift").RewardsEnabled);
    }

    [Fact]
    public void SwitchingSeason_PreservesIndependentProgress()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);

        service.GrantSeasonExperience(SeasonCatalog.Current.Id, 1_000);
        int currentTier = service.GetOrCreateSeasonProgress(SeasonCatalog.Current.Id).Tier;

        Assert.True(service.SelectSeason("season-1-midnight-shift"));
        service.GrantSeasonExperience(profile.ActiveSeasonId, 400);

        Assert.Equal("season-1-midnight-shift", profile.ActiveSeasonId);
        Assert.Equal(2, service.GetOrCreateSeasonProgress("season-1-midnight-shift").Tier);
        Assert.Equal(currentTier, service.GetOrCreateSeasonProgress(SeasonCatalog.Current.Id).Tier);
        Assert.True(service.SelectSeason(SeasonCatalog.Current.Id));
        Assert.Equal(SeasonCatalog.Current.Id, profile.ActiveSeasonId);
    }

    [Fact]
    public void ChallengeCatalog_HasRequestedCoverage()
    {
        Assert.True(ChallengeCatalog.For(ChallengeCategory.Blackjack).Count > 20);
        Assert.True(ChallengeCatalog.For(ChallengeCategory.MidnightReels).Count >= 10);
        Assert.True(ChallengeCatalog.For(ChallengeCategory.MidnightManor).Count >= 10);
        Assert.True(ChallengeCatalog.For(ChallengeCategory.NeonVault).Count >= 10);
    }

    [Fact]
    public void CompletedChallenge_CanBeClaimedExactlyOnce()
    {
        var profile = new PlayerProfileState();
        profile.LifetimeStatistics.RoundsPlayed = 10;
        var service = new ProgressionService(profile);
        long before = profile.Coins;

        ChallengeClaimResult first = service.ClaimChallenge("bj-rounds-10");
        ChallengeClaimResult second = service.ClaimChallenge("bj-rounds-10");

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal(before + first.CoinsEarned, profile.Coins);
        Assert.Contains("bj-rounds-10", profile.ClaimedChallengeIds);
    }

    [Fact]
    public void IncompleteChallenge_CannotBeClaimed()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);

        ChallengeClaimResult result = service.ClaimChallenge("bj-rounds-10");

        Assert.False(result.Success);
        Assert.Empty(profile.ClaimedChallengeIds);
    }

    private static RoundHistoryEntry History(HandOutcome outcome, decimal net)
    {
        GameRules rules = TablePresets.Get(TablePresetId.Casual).Rules;
        return new RoundHistoryEntry(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddSeconds(-10),
            DateTimeOffset.UtcNow,
            1_000m,
            1_000m + net,
            25m,
            0m,
            0m,
            net,
            rules,
            [new Card(Rank.Ten, Suit.Spades), new Card(Rank.Seven, Suit.Hearts)],
            [new HistoricalHand(25m, false, false, false, [new Card(Rank.Ten, Suit.Clubs), new Card(Rank.Six, Suit.Diamonds)], outcome, net)],
            [new PlayerActionRecord(0, PlayerAction.Stand)],
            []);
    }
}
