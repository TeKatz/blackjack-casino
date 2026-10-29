using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class PlaytimeAccrualTests
{
    [Fact]
    public void FrequentAccrualKeepsFractionsAndCountsOnlyWholeElapsedSessionSeconds()
    {
        var clock = new ManualTimeProvider();
        var profile = new PlayerProfileState { TotalPlaytimeSeconds = 100 };
        var controller = new PlayerProgressionController(profile, timeProvider: clock);

        for (int i = 0; i < 17; i++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(100));
            controller.AccruePlaytime();
            controller.SaveAll();
        }

        Assert.Equal(101, profile.TotalPlaytimeSeconds);
        Assert.Equal(TimeSpan.FromMilliseconds(1700), controller.SessionElapsed);
        Assert.Equal(TimeSpan.FromMilliseconds(700), controller.UnrecordedSessionElapsed);
        Assert.Equal(TimeSpan.FromMilliseconds(101700), controller.TotalPlaytime);
        clock.Advance(TimeSpan.FromMilliseconds(300));
        controller.SaveAll();
        Assert.Equal(102, profile.TotalPlaytimeSeconds);
        Assert.Equal(TimeSpan.Zero, controller.UnrecordedSessionElapsed);
        Assert.Equal(TimeSpan.FromSeconds(102), controller.TotalPlaytime);
    }

    [Fact]
    public void TotalPlaytimeIsStableAcrossAccrualAndDoesNotAddTheFullSessionTwice()
    {
        var clock = new ManualTimeProvider();
        var profile = new PlayerProfileState { TotalPlaytimeSeconds = 3600 };
        var controller = new PlayerProgressionController(profile, timeProvider: clock);
        clock.Advance(TimeSpan.FromMilliseconds(5750));
        TimeSpan before = controller.TotalPlaytime;

        controller.AccruePlaytime();
        controller.AccruePlaytime();

        Assert.Equal(3605, profile.TotalPlaytimeSeconds);
        Assert.Equal(TimeSpan.FromMilliseconds(750), controller.UnrecordedSessionElapsed);
        Assert.Equal(TimeSpan.FromMilliseconds(3605750), before);
        Assert.Equal(before, controller.TotalPlaytime);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleAndBulkClaimsRecognizePlaytimeThresholdDuringTheCurrentSession(bool bulk)
    {
        var clock = new ManualTimeProvider();
        ChallengeDefinition challenge = ChallengeCatalog.TryGet("lifetime-hours-250")!;
        var profile = new PlayerProfileState { TotalPlaytimeSeconds = (long)challenge.Goal - 1 };
        var controller = new PlayerProgressionController(profile, timeProvider: clock);
        clock.Advance(TimeSpan.FromMilliseconds(900));
        Assert.False(bulk ? controller.ClaimAllChallenges().Success : controller.ClaimChallenge(challenge.Id).Success);

        clock.Advance(TimeSpan.FromMilliseconds(100));
        bool claimed = bulk ? controller.ClaimAllChallenges().Success : controller.ClaimChallenge(challenge.Id).Success;

        Assert.True(claimed);
        Assert.Equal((long)challenge.Goal, profile.TotalPlaytimeSeconds);
        Assert.Contains(challenge.Id, profile.ClaimedChallengeIds);
        Assert.Equal(challenge.CoinReward, profile.Coins);
        Assert.False(bulk ? controller.ClaimAllChallenges().Success : controller.ClaimChallenge(challenge.Id).Success);
    }

    [Fact]
    public void AccrualBeforeOpeningMetaMakesPlaytimeChallengeCompleteWithoutClaimingOrSaving()
    {
        var clock = new ManualTimeProvider();
        ChallengeDefinition challenge = ChallengeCatalog.TryGet("lifetime-hours-250")!;
        var profile = new PlayerProfileState { TotalPlaytimeSeconds = (long)challenge.Goal - 1 };
        int saves = 0;
        var controller = new PlayerProgressionController(profile, _ => saves++, clock);
        clock.Advance(TimeSpan.FromSeconds(1));

        controller.AccruePlaytime();

        Assert.True(ChallengeCatalog.Complete(profile, challenge));
        Assert.Empty(profile.ClaimedChallengeIds);
        Assert.Equal(0, saves);
        Assert.Equal(TimeSpan.FromSeconds((long)challenge.Goal), controller.TotalPlaytime);
    }

    [Fact]
    public void FailedBulkSaveKeepsActualAccruedTimeAndRetrySavesItOnceWithRewards()
    {
        var clock = new ManualTimeProvider();
        ChallengeDefinition challenge = ChallengeCatalog.TryGet("lifetime-hours-250")!;
        var profile = new PlayerProfileState { TotalPlaytimeSeconds = (long)challenge.Goal - 1 };
        int saves = 0;
        string? saved = null;
        var controller = new PlayerProgressionController(profile, state =>
        {
            saves++;
            if (saves == 1) throw new IOException("test save failure");
            saved = JsonSerializer.Serialize(state);
        }, clock);
        clock.Advance(TimeSpan.FromMilliseconds(1200));

        Assert.False(controller.ClaimAllChallenges().Success);

        Assert.Equal((long)challenge.Goal, profile.TotalPlaytimeSeconds);
        Assert.Empty(profile.ClaimedChallengeIds);
        Assert.Equal(0, profile.Coins);
        Assert.True(controller.ClaimAllChallenges().Success);
        Assert.False(controller.ClaimAllChallenges().Success);
        Assert.Equal(2, saves);
        Assert.NotNull(saved);
        PlayerProfileState persisted = JsonSerializer.Deserialize<PlayerProfileState>(saved)!;
        Assert.Equal((long)challenge.Goal, persisted.TotalPlaytimeSeconds);
        Assert.Equal(challenge.CoinReward, persisted.Coins);
        Assert.Contains(challenge.Id, persisted.ClaimedChallengeIds);
    }

    [Fact]
    public void RepeatedDisposeAndSaveDoNotReAddAccruedTimeOrExtendAnEndedSession()
    {
        var clock = new ManualTimeProvider();
        var profile = new PlayerProfileState { TotalPlaytimeSeconds = 100 };
        var controller = new PlayerProgressionController(profile, timeProvider: clock);
        clock.Advance(TimeSpan.FromMilliseconds(2200));
        controller.SaveAll();
        clock.Advance(TimeSpan.FromMilliseconds(1400));

        controller.Dispose();

        Assert.Equal(103, profile.TotalPlaytimeSeconds);
        Assert.Equal(TimeSpan.FromMilliseconds(3600), controller.SessionElapsed);
        Assert.Equal(TimeSpan.FromMilliseconds(103600), controller.TotalPlaytime);
        clock.Advance(TimeSpan.FromSeconds(20));
        controller.AccruePlaytime();
        controller.SaveAll();
        controller.Dispose();
        Assert.Equal(103, profile.TotalPlaytimeSeconds);
        Assert.Equal(TimeSpan.FromMilliseconds(3600), controller.SessionElapsed);
        Assert.Equal(TimeSpan.FromMilliseconds(103600), controller.TotalPlaytime);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public void Advance(TimeSpan duration) => timestamp += duration.Ticks;
    }
}
