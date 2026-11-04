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
        Assert.Equal(2200, first.PlayerExperience);
        Assert.Equal(1100, first.SeasonExperience);
        Assert.Equal(ProgressionAward.None, xp.Process(record));
        Assert.Equal(1, service.State.Jigsaw.CompletedGames);
    }
}
