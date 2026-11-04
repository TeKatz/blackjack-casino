using BlackjackCasino.Core;
using BlackjackCasino.Core.Jigsaw;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class JigsawCentralChallengeTests
{
    [Fact]
    public void CentralCatalogContainsTheSameThirtyJigsawMilestones()
    {
        ChallengeDefinition[] entries = ChallengeRegistry.All
            .Where(challenge => challenge.Category == ChallengeCategory.Jigsaw).ToArray();
        Assert.Equal(30, entries.Length);
        Assert.Equal(30, entries.Select(challenge => challenge.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(JigsawAchievements.All, achievement =>
        {
            ChallengeDefinition entry = Assert.Single(entries,
                challenge => challenge.Id == "jigsaw-" + achievement.Id);
            Assert.Equal(achievement.Goal, entry.Goal);
            Assert.Equal(achievement.CoinReward, entry.CoinReward);
            Assert.Equal(ChallengeMetric.JigsawAchievement, entry.Metric);
            Assert.Equal(achievement.Tier == JigsawAchievementTier.Platinum, entry.IsEndgame);
        });
    }

    [Fact]
    public void ProgressInMainChallengesTracksJigsawCompletionWithoutDuplicateRewards()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        var progression = new JigsawProgression(service);
        var receipt = new JigsawCompletionRecord(
            Guid.NewGuid(), "enchanted-forest", 24, 20, DateTimeOffset.UtcNow);
        _ = progression.Process(receipt);

        ChallengeDefinition sizeChallenge = ChallengeRegistry.TryGet("jigsaw-size-24")!;
        ChallengeDefinition motifChallenge = ChallengeRegistry.TryGet("jigsaw-motif-enchanted-forest")!;
        Assert.True(ChallengeRegistry.Complete(profile, sizeChallenge));
        Assert.True(ChallengeRegistry.IsClaimed(profile, sizeChallenge));
        Assert.True(ChallengeRegistry.IsClaimed(profile, motifChallenge));
        Assert.Equal(1m, ChallengeRegistry.Progress(profile, sizeChallenge));

        long coins = profile.Coins;
        Assert.False(service.ClaimChallenge(sizeChallenge.Id).Success);
        Assert.False(service.ClaimChallenge(motifChallenge.Id).Success);
        Assert.Equal(coins, profile.Coins);

        ChallengeClaimAllResult bulk = service.ClaimAllChallenges();
        Assert.DoesNotContain(bulk.Challenges, challenge => challenge.Category == ChallengeCategory.Jigsaw);
    }

    [Fact]
    public void ExistingAchievementReceiptsAppearClaimedOnMigrationWithoutSecondPayout()
    {
        var profile = new PlayerProfileState();
        profile.Jigsaw.UnlockedAchievementIds.Add("size-1000");
        var service = new ProgressionService(profile);
        ChallengeDefinition achievement = ChallengeRegistry.TryGet("jigsaw-size-1000")!;
        Assert.True(ChallengeRegistry.Complete(profile, achievement));
        Assert.True(ChallengeRegistry.IsClaimed(profile, achievement));
        Assert.False(service.ClaimChallenge(achievement.Id).Success);
        Assert.Equal(0, profile.Coins);
    }

    [Fact]
    public void IncompleteMilestonesRemainVisibleAndCannotBeClaimed()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        ChallengeDefinition achievement = ChallengeRegistry.TryGet("jigsaw-total-100")!;
        Assert.Equal(0, ChallengeRegistry.Progress(profile, achievement));
        Assert.False(ChallengeRegistry.IsClaimed(profile, achievement));
        Assert.False(service.ClaimChallenge(achievement.Id).Success);
    }
}
