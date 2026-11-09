using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Minesweeper;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class MinesweeperTests
{
    [Theory]
    [InlineData("easy", 8, 8, 10)]
    [InlineData("standard", 12, 12, 24)]
    [InlineData("expert", 16, 16, 40)]
    public void FirstClickAndAllEightNeighboursAreGuaranteedSafe(
        string difficulty, int width, int height, int mines)
    {
        var game = new MinesweeperGame();
        Assert.True(game.Execute("mine.new", difficulty: difficulty).Success);
        int click = width * (height / 2) + width / 2;
        Assert.True(game.Execute("mine.reveal", click).Success);
        SweepRun run = game.Save().Run;
        Assert.Equal(width, run.Width);
        Assert.Equal(height, run.Height);
        Assert.Equal(mines, run.Cells.Count(c => c.Mine));
        Assert.Equal(run.Cells.Count(c => c.Revealed && !c.Mine), run.SafeRevealed);
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            var cell = run.Cells[click + dy * width + dx];
            Assert.False(cell.Mine);
        }
        Assert.Equal(0, run.Cells[click].Adjacent);
        Assert.NotEqual("lost", run.Phase);
    }

    [Fact]
    public void MinesAreNeverLeakedToBrowserBeforeGameEnds()
    {
        var game = new MinesweeperGame();
        Assert.True(game.Execute("mine.new", difficulty: "standard").Success);
        Assert.True(game.Execute("mine.reveal", index: 78).Success);
        var saved = game.Save().Run;
        Assert.Equal(24, saved.Cells.Count(c => c.Mine));
        JsonElement snapshot = JsonSerializer.SerializeToElement(game.Snapshot(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        JsonElement cells = snapshot.GetProperty("cells");
        for (int i = 0; i < cells.GetArrayLength(); i++)
        {
            SweepCell native = saved.Cells[i];
            JsonElement browser = cells[i];
            if (!native.Revealed)
            {
                Assert.Equal(-1, browser.GetProperty("adjacent").GetInt32());
                Assert.False(browser.GetProperty("mine").GetBoolean());
                Assert.Equal(-1, browser.GetProperty("scanCount").GetInt32());
            }
        }
    }

    [Fact]
    public void OneEchoScanShowsLocalHazardCountWithoutOpeningOrDetonatingTheCell()
    {
        var game = new MinesweeperGame();
        Assert.True(game.Execute("mine.new", difficulty: "expert").Success);
        Assert.True(game.Execute("mine.reveal", index: 8 * 16 + 8).Success);
        SweepRun run = game.Save().Run;
        int index = run.Cells.FindIndex(c => !c.Revealed);
        Assert.True(index >= 0);
        int before = run.SafeRevealed;
        Assert.True(game.Execute("mine.scan", index).Success);
        Assert.False(game.Execute("mine.scan", index).Success);
        int another = game.Save().Run.Cells.FindIndex((c) => !c.Revealed && !c.Scanned);
        Assert.False(game.Execute("mine.scan", another).Success);
        run = game.Save().Run;
        Assert.Equal(1, run.ScansUsed);
        Assert.Equal(before, run.SafeRevealed);
        Assert.True(run.Cells[index].Scanned);
        Assert.False(run.Cells[index].Revealed);
        JsonElement data = JsonSerializer.SerializeToElement(game.Snapshot(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        int count = data.GetProperty("cells")[index].GetProperty("scanCount").GetInt32();
        Assert.Equal(run.Cells[index].Adjacent + (run.Cells[index].Mine ? 1 : 0), count);
    }

    [Fact]
    public void NativeSaveRestoresExactFieldAndNeverReshufflesMines()
    {
        var game = new MinesweeperGame();
        Assert.True(game.Execute("mine.new", difficulty: "standard").Success);
        Assert.True(game.Execute("mine.reveal", index: 17).Success);
        int index = game.Save().Run.Cells.FindIndex(c => !c.Revealed);
        Assert.True(game.Execute("mine.flag", index).Success);
        var checkpoint = game.Save();
        var restored = new MinesweeperGame(checkpoint);
        Assert.Equal(checkpoint.Run.Id, restored.Save().Run.Id);
        Assert.Equal(checkpoint.Run.Cells.Select(c => c.Mine),
            restored.Save().Run.Cells.Select(c => c.Mine));
        Assert.True(restored.Save().Run.Cells[index].Flagged);
        checkpoint.Run.Cells[index].Flagged = false;
        Assert.True(restored.Save().Run.Cells[index].Flagged);
    }

    [Fact]
    public void TerminalStatsAreIdempotentAndCountLossesAndWinStreaks()
    {
        var stats = new SweepStatistics();
        var win = SweepRun.New("expert") with { Phase = "won", SafeRevealed = 216, Seconds = 42 };
        Assert.True(stats.Record(win));
        Assert.False(stats.Record(win));
        Assert.Equal(1, stats.Wins);
        Assert.Equal(1, stats.ExpertWins);
        Assert.Equal(1, stats.NoScanWins);
        Assert.Equal(216, stats.SafeCellsRevealed);
        Assert.Equal(42, stats.BestWinSeconds);
        var second = SweepRun.New("easy") with { Phase = "won", ScansUsed = 1, SafeRevealed = 54, Seconds = 24 };
        Assert.True(stats.Record(second));
        Assert.Equal(2, stats.Wins);
        Assert.Equal(2, stats.BestWinStreak);
        Assert.Equal(1, stats.NoScanWins);
        Assert.Equal(24, stats.BestWinSeconds);
        var lost = SweepRun.New("standard") with { Phase = "lost", ScansUsed = 1, SafeRevealed = 35 };
        Assert.True(stats.Record(lost));
        Assert.False(stats.Record(lost));
        Assert.Equal(1, stats.Losses);
        Assert.Equal(0, stats.CurrentWinStreak);
        Assert.Equal(305, stats.SafeCellsRevealed);
        Assert.Equal(2, stats.ScansUsed);
        var copy = stats.Copy();
        Assert.False(copy.Record(win));
        Assert.Equal(stats.CompletedRunIds, copy.CompletedRunIds);
    }

    [Fact]
    public void OnlyTerminalReceiptsAdvanceGlobalMilestones()
    {
        var profile = new PlayerProfileState();
        var game = new MinesweeperGame();
        Assert.True(game.Execute("mine.new", difficulty: "easy").Success);
        Assert.True(game.Execute("mine.reveal", index: 27).Success);
        Assert.Equal(0, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("sweep-first")!));
        var completed = SweepRun.New("expert") with { Phase = "won", SafeRevealed = 216, Seconds = 90 };
        Assert.True(profile.Minesweeper.Record(completed));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sweep-first")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sweep-noscan-1")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sweep-expert-1")!));
        Assert.Equal(216, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("sweep-safe-500")!));
        Assert.True(ChallengeCatalog.For(ChallengeCategory.Minesweeper).Count >= 14);
    }

    [Fact]
    public void BrowserProtocolRejectsBadArgumentsAndReplaysProduceSameReceipt()
    {
        var session = new ModernMinesweeperSession();
        string id = Guid.NewGuid().ToString("D");
        string json = JsonSerializer.Serialize(new
        {
            type = "lounge.minesweeper.command", version = 1,
            requestId = id, session = session.Id, expectedRevision = 0,
            action = "mine.new", difficulty = "expert",
        });
        var request = ModernMinesweeperProtocol.Parse(json);
        Assert.NotNull(request);
        Assert.Equal("expert", request.Difficulty);
        int calls = 0;
        var result = session.Execute(request, _ => { calls++; return (true, "Ready"); });
        Assert.True(result.Success);
        Assert.Equal(1, result.Revision);
        var repeated = session.Execute(request, _ => { calls++; return (true, "Bad"); });
        Assert.Equal(result, repeated);
        Assert.Equal(1, calls);
        var stale = request with { RequestId = Guid.NewGuid().ToString("D") };
        Assert.False(session.Execute(stale, _ => (true, "Bad")).Success);
        Assert.Null(ModernMinesweeperProtocol.Parse(json.Replace("mine.new", "mine.hack")));
        Assert.Null(ModernMinesweeperProtocol.Parse(json.Replace("expert", "impossible")));
        Assert.Null(ModernMinesweeperProtocol.Parse(json.Replace("expectedRevision\":0", "expectedRevision\":-1")));
    }
}
