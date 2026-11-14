using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class LobbyProgressTests
{
    [Fact]
    public void SeasonProgressMeasuresTheCurrentTierIntervalRatherThanLifetimeSeasonXp()
    {
        var profile = new PlayerProfileState();
        SeasonDefinition season = SeasonCatalog.Get(profile.ActiveSeasonId);
        long floor = season.Tiers[4].RequiredExperience;
        long width = season.Tiers[5].RequiredExperience - floor;
        profile.Seasons[season.Id] = new SeasonProgressState { SeasonId = season.Id, Tier = 5, Experience = floor + width / 2 };
        LobbyProgressSnapshot progress = LobbyProgressSnapshot.From(profile);
        Assert.Equal(5, progress.SeasonTier); Assert.Equal(6, progress.SeasonNextTier);
        Assert.InRange(progress.SeasonFraction, .49, .51);
        profile.Seasons[season.Id].Experience = season.Tiers[^1].RequiredExperience;
        Assert.True(LobbyProgressSnapshot.From(profile).SeasonComplete);
        Assert.Equal(1, LobbyProgressSnapshot.From(profile).SeasonFraction);
    }

    [Fact]
    public void ChallengeSelectionUsesHighestRelativeIncompleteProgressAndSkipsClaimedRewards()
    {
        var profile = new PlayerProfileState { LifetimeStatistics = new StatisticsSnapshot { RoundsPlayed = 912, HandsPlayed = 912, HandsWon = 403, Blackjacks = 41 } };
        var claimed = profile.ClaimedChallengeIds.ToHashSet();
        ChallengeDefinition expected = ChallengeCatalog.All.Where(c => !claimed.Contains(c.Id) && ChallengeCatalog.Progress(profile, c) < c.Goal)
            .OrderByDescending(c => ChallengeCatalog.Progress(profile, c) / c.Goal).First();
        LobbyProgressSnapshot progress = LobbyProgressSnapshot.From(profile);
        Assert.Equal(expected.Id, progress.ChallengeId);
        Assert.InRange(progress.ChallengeFraction, 0, .999999);
        profile.ClaimedChallengeIds.Add(expected.Id);
        Assert.NotEqual(expected.Id, LobbyProgressSnapshot.From(profile).ChallengeId);
        foreach (ChallengeDefinition challenge in ChallengeCatalog.All) profile.ClaimedChallengeIds.Add(challenge.Id);
        Assert.Null(LobbyProgressSnapshot.From(profile).ChallengeId);
        Assert.Equal(0, LobbyProgressSnapshot.From(profile).ReadyCount);
    }
}
