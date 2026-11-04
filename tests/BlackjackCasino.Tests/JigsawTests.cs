using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Jigsaw;

namespace BlackjackCasino.Tests;

public sealed class JigsawTests
{
    [Fact]
    public void EverySupportedPieceCountMakesAnExactGrid()
    {
        Assert.Equal(new[] { 24, 48, 96, 150, 300, 500, 750, 1000 },
            JigsawCatalog.Sizes.Select(x => x.Count));
        foreach (JigsawSize size in JigsawCatalog.Sizes)
        {
            Assert.Equal(size.Count, size.Columns * size.Rows);
            var game = JigsawGame.Start("neon-city", size.Count);
            Assert.Equal(size.Count, game.Pieces.Count);
            Assert.Equal(size.Count, game.Pieces.Select(x => x.Id).Distinct().Count());
        }
    }

    [Fact]
    public void ThousandPiecesCanBeCompletedAndRecordedOnce()
    {
        var game = JigsawGame.Start("sakura-garden", 1000);
        for (int id = 0; id < game.PieceCount; id++)
        {
            (double x, double y) = JigsawGame.Target(game, id);
            Assert.True(JigsawGame.Place(game, id, x, y));
            Assert.Equal(id == 999, JigsawGame.Snap(game, id));
        }
        Assert.True(game.Completed);
        Assert.Equal(1000, game.LockedCount);
        Assert.False(JigsawGame.Move(game, 0, 10, 20));
        var progress = new JigsawProgressState { ActiveGame = game };
        progress.Normalize();
        Assert.Null(progress.ActiveGame);
        Assert.Single(progress.CompletedGames);
        progress.Normalize();
        Assert.Single(progress.CompletedGames);
    }

    [Fact]
    public void FreePiecesConnectAndMoveTogetherAwayFromTheirTargets()
    {
        var game = JigsawGame.Start("frost-kingdom", 96);
        double width = JigsawGame.BoardWidth / JigsawCatalog.Size(96).Columns;
        Assert.True(JigsawGame.Place(game, 0, 500, 400));
        Assert.True(JigsawGame.Place(game, 1, 500 + width + 2, 401));
        Assert.False(JigsawGame.Snap(game, 1));
        Assert.Equal(2, JigsawGame.Connected(game, 0).Count);
        Assert.False(game.Pieces[1].Locked);
        double x = game.Pieces[1].X;
        Assert.True(JigsawGame.Move(game, 0, 17, 13));
        Assert.Equal(x + 17, game.Pieces[1].X, 4);
    }

    [Fact]
    public void InvalidSaveIsNormalizedWithoutGrantingFreeLocks()
    {
        var game = new JigsawGameState
        {
            PieceCount = 24,
            MotifId = "unknown",
            Pieces =
            [
                new() { Id = 0, X = double.NaN, OnBoard = true },
                new() { Id = 1, X = 100, Y = 100, OnBoard = true, Locked = true },
                new() { Id = 1, X = 100, Y = 100, OnBoard = true },
            ],
        };
        game.Normalize();
        Assert.Equal(24, game.Pieces.Count);
        Assert.False(game.Pieces[0].OnBoard);
        Assert.False(game.Pieces[1].Locked);
        Assert.Equal(JigsawCatalog.Motifs[0].Id, game.MotifId);
    }

    [Fact]
    public void SessionCoordinatesSurviveJsonRoundTrip()
    {
        var progress = new JigsawProgressState
        {
            SelectedPieceCount = 500,
            ActiveGame = JigsawGame.Start("pirate-cove", 500),
        };
        Assert.True(JigsawGame.Place(progress.ActiveGame, 33, 410, 220));
        var restored = JsonSerializer.Deserialize<JigsawProgressState>(JsonSerializer.Serialize(progress))!;
        restored.Normalize();
        Assert.Equal(500, restored.ActiveGame!.PieceCount);
        Assert.Equal(410, restored.ActiveGame.Pieces[33].X);
        Assert.Equal(220, restored.ActiveGame.Pieces[33].Y);
    }

    [Fact]
    public void RepeatedCompletionNeverAwardsXpTwice()
    {
        var service = new ProgressionService(new PlayerProfileState());
        var xp = new JigsawProgression(service);
        var record = new JigsawCompletionRecord(Guid.NewGuid(), "neon-city", 1000, 250000,
            DateTimeOffset.UtcNow);
        var first = xp.Process(record);
        // First 1,000-piece completion also unlocks the size, first-completion and piece milestones.
        Assert.True(first.PlayerExperience > 2200);
        Assert.True(first.SeasonExperience > 1100);
        Assert.Contains("size-1000", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Contains("total-1", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Equal(ProgressionAward.None, xp.Process(record));
        Assert.Equal(1, service.State.Jigsaw.CompletedGames);
    }
    [Fact]
    public void LooseGroupsCanReturnToTrayButAnchoredPiecesCannot()
    {
        var game = JigsawGame.Start("ocean-depths", 48);
        double width = JigsawGame.BoardWidth / JigsawCatalog.Size(48).Columns;
        Assert.True(JigsawGame.Place(game, 0, 500, 400));
        Assert.True(JigsawGame.Place(game, 1, 500 + width + 2, 400));
        Assert.False(JigsawGame.Snap(game, 1));
        Assert.Equal(2, JigsawGame.Connected(game, 0).Count);
        Assert.True(JigsawGame.ReturnToTray(game, 0));
        Assert.Equal(0, game.OnBoardCount);

        (double x, double y) = JigsawGame.Target(game, 0);
        Assert.True(JigsawGame.Place(game, 0, x, y));
        Assert.False(JigsawGame.Snap(game, 0));
        Assert.True(game.Pieces[0].Locked);
        Assert.False(JigsawGame.ReturnToTray(game, 0));
        Assert.Equal(1, game.OnBoardCount);
    }

    [Fact]
    public void ThirtyAchievementsHaveUniqueIdsAndIncreasingDifficultyRewards()
    {
        Assert.Equal(30, JigsawAchievements.All.Count);
        Assert.Equal(30, JigsawAchievements.All.Select(a => a.Id).Distinct(StringComparer.Ordinal).Count());
        var ranks = JigsawAchievements.All.GroupBy(a => a.Tier)
            .ToDictionary(group => group.Key, group => group.First());
        Assert.True(ranks[JigsawAchievementTier.Bronze].ExperienceReward <
                    ranks[JigsawAchievementTier.Silver].ExperienceReward);
        Assert.True(ranks[JigsawAchievementTier.Silver].CoinReward <
                    ranks[JigsawAchievementTier.Gold].CoinReward);
        Assert.True(ranks[JigsawAchievementTier.Gold].CoinReward <
                    ranks[JigsawAchievementTier.Platinum].CoinReward);
    }

    [Fact]
    public void EveryAchievementIsClaimedOnceWhenItsMilestoneIsReached()
    {
        var service = new ProgressionService(new PlayerProfileState());
        var progression = new JigsawProgression(service);
        var one = new JigsawCompletionRecord(Guid.NewGuid(), "enchanted-forest", 24, 0, DateTimeOffset.UtcNow);
        ProgressionAward reward = progression.Process(one);
        Assert.Contains("size-24", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Contains("total-1", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Contains("motif-enchanted-forest", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Equal(3, service.State.Jigsaw.UnlockedAchievementIds.Count);
        Assert.True(reward.CoinsEarned >= 300);
        Assert.Equal(ProgressionAward.None, progression.Process(one));
        Assert.Equal(3, service.State.Jigsaw.UnlockedAchievementIds.Count);
        Assert.Equal(1, service.State.Jigsaw.CompletedBySize[24]);
        Assert.Equal(1, service.State.Jigsaw.CompletedByMotif["enchanted-forest"]);

        var two = new JigsawCompletionRecord(Guid.NewGuid(), "ocean-depths", 96, 0, DateTimeOffset.UtcNow);
        _ = progression.Process(two);
        Assert.Contains("scenes-2", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Contains("motif-ocean-depths", service.State.Jigsaw.UnlockedAchievementIds);
        Assert.Equal(2, service.State.Jigsaw.CompletedByMotif.Count);
    }

    [Fact]
    public void ClockPreferencePersistsAndCanDisableTimingCompletely()
    {
        var progress = new JigsawProgressState
        {
            TimerEnabled = false,
            ActiveGame = JigsawGame.Start("neon-city", 48),
        };
        progress.ActiveGame.TimerEnabled = false;
        var restored = JsonSerializer.Deserialize<JigsawProgressState>(JsonSerializer.Serialize(progress))!;
        restored.Normalize();
        Assert.False(restored.TimerEnabled);
        Assert.False(restored.ActiveGame!.TimerEnabled);
    }

    [Fact]
    public void MovingCachedGroupGivesTheSameResultAsConnectedMovement()
    {
        var game = JigsawGame.Start("ocean-depths", 48);
        double width = JigsawGame.BoardWidth / JigsawCatalog.Size(48).Columns;
        Assert.True(JigsawGame.Place(game, 0, 500, 400));
        Assert.True(JigsawGame.Place(game, 1, 500 + width, 400));
        int[] group = JigsawGame.Connected(game, 0).ToArray();
        Assert.Equal(2, group.Length);
        Assert.True(JigsawGame.MoveGroup(game, group, 17, -3));
        Assert.Equal(517, game.Pieces[0].X);
        Assert.Equal(500 + width + 17, game.Pieces[1].X);
    }

}
