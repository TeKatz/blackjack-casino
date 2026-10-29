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
    public void CurrentSeason_IsLongFormAndRequiresOneHundredTwentyThousandXp()
    {
        SeasonDefinition season = SeasonCatalog.Current;

        Assert.Equal(100, season.Tiers.Count);
        Assert.Equal(120_000, season.Tiers[^1].RequiredExperience);
        Assert.True(season.Tiers[49].RequiredExperience > 30_000);
    }

    [Fact]
    public void SeasonTrack_GrantsNewTierRewardsWithoutPremiumTrack()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        SeasonTierDefinition tier15 = SeasonCatalog.Current.Tiers[14];

        ProgressionAward award = service.GrantSeasonExperience(SeasonCatalog.Current.Id, tier15.RequiredExperience);

        Assert.Equal(15, service.GetOrCreateSeasonProgress(SeasonCatalog.Current.Id).Tier);
        Assert.Equal(15, award.SeasonTiersGained);
        Assert.Contains("portrait-eclipse-raven", profile.OwnedCosmeticIds);
    }

    [Fact]
    public void LegacySeason_RemainsStoredButArchivedProfileMovesToCurrentSeason()
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

        Assert.Equal(SeasonCatalog.Current.Id, profile.ActiveSeasonId);
        Assert.Equal(50, profile.Seasons["season-1-midnight-shift"].Tier);
        Assert.True(SeasonCatalog.Get("season-1-midnight-shift").Archived);
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
