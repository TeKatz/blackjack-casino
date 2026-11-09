using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class ArcadeChallengesTests
{
    [Fact]
    public void ExactlyTenGlobalChallengesExistForEachArcadeGame()
    {
        ChallengeDefinition[] all = ChallengeCatalog.For(ChallengeCategory.Arcade).ToArray();
        Assert.Equal(100, all.Length);
        Assert.Equal(100, all.Select(c => c.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(all, challenge =>
        {
            Assert.StartsWith("arcade-", challenge.Id, StringComparison.Ordinal);
            Assert.True(challenge.Goal > 0);
            Assert.True(challenge.CoinReward > 0);
            Assert.Equal(ChallengeCategory.Arcade, challenge.Category);
            Assert.True(challenge.Description.Length > 15);
        });
        foreach (string gameId in new[]
        {
            "midnight-wings", "cave-escape", "hexagon-hell", "perfect-stack", "spike-rush",
            "blade-timing", "bullet-storm", "neon-breaker", "death-snake", "endless-ascent"
        })
            Assert.Equal(10, all.Count(c => c.Id.StartsWith("arcade-" + gameId + "-", StringComparison.Ordinal)));
    }

    [Fact]
    public void CompletedArcadeReceiptsIncreaseGlobalProgressAndCanBeClaimedOnce()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        ChallengeDefinition first = ChallengeCatalog.TryGet("arcade-midnight-wings-first")!;
        ChallengeDefinition score = ChallengeCatalog.TryGet("arcade-midnight-wings-score-2")!;
        Assert.Equal(0, ChallengeCatalog.Progress(profile, first));
        Guid receiptId = Guid.NewGuid();
        var receipt = new ArcadeRunReceipt(receiptId, "midnight-wings", 21, 48);
        Assert.True(ArcadeProgression.Process(profile.Arcade, receipt));
        Assert.False(ArcadeProgression.Process(profile.Arcade, receipt)); // duplicate WebView receipt
        Assert.True(ChallengeCatalog.Complete(profile, first));
        Assert.True(ChallengeCatalog.Complete(profile, score));
        Assert.Equal(1, ChallengeCatalog.Progress(profile, first));
        Assert.Equal(21, ChallengeCatalog.Progress(profile, score));
        ChallengeClaimResult claim = service.ClaimChallenge(first.Id);
        Assert.True(claim.Success);
        Assert.False(service.ClaimChallenge(first.Id).Success);
        Assert.Equal(first.CoinReward, claim.CoinsEarned);

        PlayerProfileState restored = JsonSerializer.Deserialize<PlayerProfileState>(
            JsonSerializer.Serialize(profile))!;
        ProfileNormalization.Normalize(restored);
        Assert.True(ChallengeCatalog.Complete(restored, first));
        Assert.False(ArcadeProgression.Process(restored.Arcade, receipt));
        Assert.Equal(1, ChallengeCatalog.Progress(restored, first));
        Assert.False(new ProgressionService(restored).ClaimChallenge(first.Id).Success);
    }

    [Fact]
    public void RecordsAreGameSpecificAndTrackRunsTotalsBestAndLongestRun()
    {
        var stats = new ArcadeStatistics();
        string game = "neon-breaker";
        foreach (int score in new[] { 100, 25, 150 })
            Assert.True(ArcadeProgression.Process(stats, new ArcadeRunReceipt(Guid.NewGuid(), game, score, score / 2)));
        Assert.Equal(3, ChallengeCatalog.Progress(new PlayerProfileState { Arcade = stats },
            ChallengeCatalog.TryGet("arcade-neon-breaker-first")!) + 2);
        Assert.Equal(150, stats.Games[game].BestScore);
        Assert.Equal(275, stats.Games[game].TotalScore);
        Assert.Equal(75, stats.Games[game].LongestSeconds);
        Assert.False(stats.Games.ContainsKey("death-snake"));
        Assert.Equal(0, ArcadeChallengeCatalog.Progress(stats,
            ChallengeCatalog.TryGet("arcade-death-snake-score-1")!));
    }

    [Fact]
    public void InvalidArcadeReceiptsNeverProduceProgress()
    {
        var stats = new ArcadeStatistics();
        Assert.False(ArcadeProgression.Process(stats, new ArcadeRunReceipt(Guid.Empty, "death-snake", 10, 10)));
        Assert.False(ArcadeProgression.Process(stats, new ArcadeRunReceipt(Guid.NewGuid(), "unknown", 10, 10)));
        Assert.False(ArcadeProgression.Process(stats, new ArcadeRunReceipt(Guid.NewGuid(), "death-snake", -1, 10)));
        Assert.False(ArcadeProgression.Process(stats, new ArcadeRunReceipt(Guid.NewGuid(), "death-snake", 10, -1)));
        Assert.Empty(stats.Games);
        Assert.Empty(stats.RecordedRuns);
    }
}
