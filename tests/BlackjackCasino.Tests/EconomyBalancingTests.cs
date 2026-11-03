using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Tests;

public sealed class EconomyBalancingTests
{
    [Fact]
    public void DailyLoginCycleAdvancesWithoutStreakDependency()
    {
        decimal[] expected = [1_000m, 1_500m, 2_000m, 2_500m, 3_000m, 3_500m, 4_000m];

        for (int claim = 0; claim < expected.Length; claim++)
            Assert.Equal(expected[claim], DailyLoginRewardSchedule.GetReward(claim));

        Assert.Equal(1_000m, DailyLoginRewardSchedule.GetReward(expected.Length));
        Assert.Equal(4_000m, DailyLoginRewardSchedule.GetReward(expected.Length * 2 - 1));
    }

    [Fact]
    public void DailyLoginRewardBundlesContainCoinsAndMilestonePacks()
    {
        for (int claim = 0; claim < DailyLoginRewardSchedule.CycleLength; claim++)
        {
            DailyLoginRewardDefinition reward = DailyLoginRewardSchedule.GetDefinition(claim);
            Assert.Equal(claim == 6 ? 80 : 40, reward.CosmeticCoins);
        }

        Assert.Equal(CollectionRarity.Common, DailyLoginRewardSchedule.GetDefinition(0).PackRarity);
        Assert.Null(DailyLoginRewardSchedule.GetDefinition(1).PackRarity);
        Assert.Null(DailyLoginRewardSchedule.GetDefinition(2).PackRarity);
        Assert.Equal(CollectionRarity.Rare, DailyLoginRewardSchedule.GetDefinition(3).PackRarity);
        Assert.Null(DailyLoginRewardSchedule.GetDefinition(4).PackRarity);
        Assert.Null(DailyLoginRewardSchedule.GetDefinition(5).PackRarity);
        Assert.Equal(CollectionRarity.Epic, DailyLoginRewardSchedule.GetDefinition(6).PackRarity);
    }

    [Fact]
    public void DailyLoginClaimWaitsAcrossMissedDaysAndCannotBeClaimedTwice()
    {
        var firstDay = new DateOnly(2026, 11, 1);
        DailyLoginClaim first = Assert.IsType<DailyLoginClaim>(DailyLoginProgress.TryClaim(null, 0, firstDay));
        Assert.Equal(1_000m, first.Reward);
        Assert.Null(DailyLoginProgress.TryClaim(first.ClaimDate, first.TotalClaims, firstDay));

        DateOnly later = firstDay.AddDays(5);
        DailyLoginStatus waiting = DailyLoginProgress.GetStatus(first.ClaimDate, first.TotalClaims, later);
        Assert.True(waiting.CanClaim);
        Assert.Equal(2, waiting.ActiveStep);
        Assert.Equal(1_500m, waiting.ActiveReward);

        DailyLoginClaim second = Assert.IsType<DailyLoginClaim>(DailyLoginProgress.TryClaim(first.ClaimDate, first.TotalClaims, later));
        Assert.Equal(1_500m, second.Reward);
        Assert.Equal(2, second.TotalClaims);
    }

    [Fact]
    public void DailyLoginSeventhClaimRollsIntoFreshCycleNextDay()
    {
        DateOnly today = new(2026, 11, 3);
        DailyLoginClaim seventh = Assert.IsType<DailyLoginClaim>(DailyLoginProgress.TryClaim("2026-11-02", 6, today));
        Assert.Equal(4_000m, seventh.Reward);
        Assert.Equal(7, seventh.Status.CycleCompletedSteps);
        Assert.False(seventh.Status.CanClaim);

        DailyLoginStatus tomorrow = DailyLoginProgress.GetStatus(seventh.ClaimDate, seventh.TotalClaims, today.AddDays(1));
        Assert.True(tomorrow.CanClaim);
        Assert.Equal(1, tomorrow.ActiveStep);
        Assert.Equal(1_000m, tomorrow.ActiveReward);
        Assert.Equal(0, tomorrow.CycleCompletedSteps);
    }

    [Fact]
    public void BlackjackTablesAllowFiveThousandDollarMaximumBet()
    {
        Assert.Equal(5_000m, new GameRules().MaximumBet);
        Assert.All(TablePresets.All, preset => Assert.Equal(5_000m, preset.Rules.MaximumBet));
    }

    [Fact]
    public void CasinoSessionCanCreditValidatedMetaRewards()
    {
        var session = new CasinoSession(new GameRules(), 1_000m);

        session.CreditBankroll(250m);

        Assert.Equal(1_250m, session.Bankroll);
        Assert.Throws<ArgumentOutOfRangeException>(() => session.CreditBankroll(0m));
    }

    [Fact]
    public void CurrentSeasonIntermediateTiersAwardOneHundredBankroll()
    {
        SeasonDefinition season = SeasonCatalog.Current;
        Assert.All(season.Tiers.Where(tier => tier.CosmeticId is null), tier => Assert.Equal(100m, tier.BankrollReward));
        Assert.All(season.Tiers.Where(tier => tier.CosmeticId is not null), tier => Assert.Equal(0m, tier.BankrollReward));

        var profile = new PlayerProfileState { ActiveSeasonId = season.Id };
        var progression = new ProgressionService(profile);
        ProgressionAward award = progression.GrantSeasonExperience(season.Id, season.Tiers[0].RequiredExperience);
        Assert.Equal(100m, award.BankrollEarned);
    }

    [Fact]
    public void ChillLoungeCompletionPaysOncePerRecordedCompletion()
    {
        var progression = new ProgressionService(new PlayerProfileState());
        bool recorded = false;

        ProgressionAward first = LoungeProgression.GrantOnce(progression, () => !recorded && (recorded = true), 0, 0);
        ProgressionAward duplicate = LoungeProgression.GrantOnce(progression, () => false, 0, 0);

        Assert.Equal(100m, first.BankrollEarned);
        Assert.Equal(0m, duplicate.BankrollEarned);
    }

    [Fact]
    public void AlbumCompletionBankrollScalesForUltraRareThemes()
    {
        CollectionTheme standard = CollectionCatalog.Themes.First(theme => !theme.UltraRare);
        CollectionTheme ultraRare = CollectionCatalog.Themes.First(theme => theme.UltraRare);

        Assert.Equal(2_500m, CollectionService.ThemeCompletionReward(standard).Bankroll);
        Assert.Equal(10_000m, CollectionService.ThemeCompletionReward(ultraRare).Bankroll);
    }

    [Theory]
    [InlineData(20, 100)]
    [InlineData(500, 125)]
    [InlineData(1200, 195)]
    [InlineData(2500, 325)]
    public void MergeStageBankrollRewardScalesWithoutBecomingDominant(long points, decimal expected)
    {
        Assert.Equal(expected, MergePlayerProgression.MilestoneBankrollReward(points));
    }
}
