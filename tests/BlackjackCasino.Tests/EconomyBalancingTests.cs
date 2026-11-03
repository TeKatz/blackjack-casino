using BlackjackCasino.Core;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Tests;

public sealed class EconomyBalancingTests
{
    [Fact]
    public void DailyLoginCycleAdvancesWithoutStreakDependency()
    {
        decimal[] expected = [100m, 125m, 150m, 175m, 200m, 250m, 500m];

        for (int claim = 0; claim < expected.Length; claim++)
            Assert.Equal(expected[claim], DailyLoginRewardSchedule.GetReward(claim));

        Assert.Equal(100m, DailyLoginRewardSchedule.GetReward(expected.Length));
        Assert.Equal(500m, DailyLoginRewardSchedule.GetReward(expected.Length * 2 - 1));
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
