using BlackjackCasino.Core;
namespace BlackjackCasino.Tests;
public sealed class ArchivedSeasonBankrollTests
{
    [Theory]
    [InlineData("season-1-midnight-shift", 50)]
    [InlineData("season-2-velvet-eclipse", 100)]
    public void EveryArchivedTierPaysOneHundredOnce(string id, int count)
    {
        var season = SeasonCatalog.Get(id);
        Assert.Equal(count, season.Tiers.Count);
        Assert.All(season.Tiers, tier => Assert.Equal(100m, tier.BankrollReward));
        var service = new ProgressionService(new PlayerProfileState());
        var award = service.GrantSeasonExperience(id, season.Tiers[^1].RequiredExperience);
        Assert.Equal(count * 100m, award.BankrollEarned);
        Assert.Equal(0m, service.GrantSeasonExperience(id, 1).BankrollEarned);
    }
}
