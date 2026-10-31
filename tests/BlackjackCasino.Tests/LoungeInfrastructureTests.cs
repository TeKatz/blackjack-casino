using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class LoungeInfrastructureTests
{
    [Fact]
    public void LoungeProgressionGrantsPlayerAndSeasonRewardsOnlyAfterDurableReceipt()
    {
        var state = new PlayerProfileState();
        var progression = new ProgressionService(state);
        bool recorded = false;

        ProgressionAward first = LoungeProgression.GrantOnce(
            progression,
            () => !recorded && (recorded = true),
            playerExperience: 400,
            seasonExperience: 200);

        Assert.Equal(400, first.PlayerExperience);
        Assert.Equal(200, first.SeasonExperience);
        Assert.Equal(400, state.LifetimeExperience);
        Assert.Equal(200, progression.GetOrCreateSeasonProgress(state.ActiveSeasonId).Experience);

        ProgressionAward duplicate = LoungeProgression.GrantOnce(
            progression,
            () => !recorded && (recorded = true),
            playerExperience: 400,
            seasonExperience: 200);

        Assert.Equal(ProgressionAward.None, duplicate);
        Assert.Equal(400, state.LifetimeExperience);
        Assert.Equal(200, progression.GetOrCreateSeasonProgress(state.ActiveSeasonId).Experience);
    }

    [Fact]
    public void LoungeProgressionRejectsNegativeRewardsBeforeRecordingCompletion()
    {
        var progression = new ProgressionService(new PlayerProfileState());
        bool recorded = false;

        Assert.Throws<ArgumentOutOfRangeException>(() => LoungeProgression.GrantOnce(
            progression,
            () => recorded = true,
            playerExperience: -1,
            seasonExperience: 0));

        Assert.False(recorded);
    }
}
