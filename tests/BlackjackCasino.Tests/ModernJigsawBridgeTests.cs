using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Jigsaw;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernJigsawBridgeTests
{
    private static string JsonRequest(ModernJigsawSession session, string action, string? motifId = null,
        int? pieceCount = null, int? index = null, double? x = null, double? y = null,
        bool? enabled = null, bool? confirmReplace = null, string? requestId = null, long? revision = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = "lounge.jigsaw.command", ["version"] = 1,
            ["session"] = session.Id, ["expectedRevision"] = revision ?? session.Revision,
            ["requestId"] = requestId ?? Guid.NewGuid().ToString("D"),
            ["action"] = action, ["motifId"] = motifId, ["pieceCount"] = pieceCount,
            ["index"] = index, ["x"] = x, ["y"] = y, ["enabled"] = enabled,
            ["confirmReplace"] = confirmReplace,
        };
        return JsonSerializer.Serialize(payload.Where(pair => pair.Value is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Value));
    }
    private static ModernJigsawCommand Command(ModernJigsawSession session, string action,
        string? motifId = null, int? pieceCount = null, int? index = null,
        double? x = null, double? y = null, bool? enabled = null, bool? confirmReplace = null)
        => ModernJigsawProtocol.Parse(JsonRequest(session, action, motifId, pieceCount, index, x, y,
            enabled, confirmReplace))!;

    [Fact]
    public void ProtocolAllowsOnlyValidNativePuzzleIntents()
    {
        var session = new ModernJigsawSession();
        foreach (string good in new[]
        {
            JsonRequest(session, "jigsaw.motif", motifId:"sakura-garden"),
            JsonRequest(session, "jigsaw.size", pieceCount:1000),
            JsonRequest(session, "jigsaw.timer", enabled:false),
            JsonRequest(session, "jigsaw.start"), JsonRequest(session, "jigsaw.start", confirmReplace:true),
            JsonRequest(session, "jigsaw.resume"), JsonRequest(session, "jigsaw.back"),
            JsonRequest(session, "jigsaw.exit"), JsonRequest(session, "jigsaw.achievements"),
            JsonRequest(session, "jigsaw.place", index:999, x:600, y:400),
            JsonRequest(session, "jigsaw.move", index:0, x:-400, y:1000),
            JsonRequest(session, "jigsaw.tray", index:24),
        }) Assert.NotNull(ModernJigsawProtocol.Parse(good));
        foreach (string bad in new[]
        {
            JsonRequest(session,"jigsaw.spin"),JsonRequest(session,"jigsaw.reward"),
            JsonRequest(session,"jigsaw.size",pieceCount:17),
            JsonRequest(session,"jigsaw.motif",motifId:"unknown"),
            JsonRequest(session,"jigsaw.start",confirmReplace:false),
            JsonRequest(session,"jigsaw.move",index:1000,x:0,y:0),
            JsonRequest(session,"jigsaw.move",index:0,x:0,y:0).Replace("\"x\":0", "\"x\":\"NaN\""),
            JsonRequest(session,"jigsaw.place",index:0,x:1400,y:0),
            JsonRequest(session,"jigsaw.tray",index:0,x:10),
            JsonRequest(session,"jigsaw.timer"),
            JsonRequest(session,"jigsaw.resume",revision:-1),
            JsonRequest(session,"jigsaw.place",index:0,x:0,y:0).Replace("\"action\":\"jigsaw.place\"",
                "\"action\":\"jigsaw.place\",\"bankroll\":999999"),
            new string('X',2049), "[]", "not JSON",
        }) Assert.Null(ModernJigsawProtocol.Parse(bad));
    }

    [Fact]
    public void SessionDoesNotReplayStaleOrCrossSessionCommands()
    {
        var session = new ModernJigsawSession();
        ModernJigsawCommand command = Command(session,"jigsaw.start");
        int commits = 0;
        (bool Success,string Message) Commit(ModernJigsawCommand _) { commits++; return (true,"Saved"); }
        ModernJigsawReply first = session.Execute(command, Commit);
        Assert.True(first.Success);
        Assert.Equal(1L,session.Revision);
        Assert.Equal(first,session.Execute(command,Commit));
        Assert.False(session.Execute(command with {RequestId=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.False(session.Execute(command with {RequestId=Guid.NewGuid().ToString("D"),
            Session=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.Equal(1, commits);
    }

    [Fact]
    public void AllEightSizesKeepOriginalGameSchemaAndResume()
    {
        foreach (JigsawSize size in JigsawCatalog.Sizes)
        {
            var saved = new JigsawProgressState();
            var native = new ModernJigsawService(saved, p=>saved=p, ()=>new JigsawStatistics(),
                _=>ProgressionAward.None);
            var session = new ModernJigsawSession();
            Assert.True(session.Execute(Command(session,"jigsaw.size",pieceCount:size.Count),native.Execute).Success);
            Assert.True(session.Execute(Command(session,"jigsaw.start"),native.Execute).Success);
            Assert.Equal(size.Count,native.CurrentGame!.Pieces.Count);
            Assert.Equal(native.CurrentGame.GameId,saved.ActiveGame!.GameId);
            Assert.True(session.Execute(Command(session,"jigsaw.back"),native.Execute).Success);
            Assert.True(session.Execute(Command(session,"jigsaw.resume"),native.Execute).Success);
            Assert.Equal(saved.ActiveGame.GameId,native.CurrentGame!.GameId);
        }
    }

    [Fact]
    public void NativeSnapAndTrayRulesPersistOnDetachedCopies()
    {
        var saved = new JigsawProgressState { SelectedPieceCount=48 };
        var native = new ModernJigsawService(saved,p=>saved=p,()=>new JigsawStatistics(),
            _=>ProgressionAward.None);
        var session = new ModernJigsawSession();
        Assert.True(session.Execute(Command(session,"jigsaw.start"),native.Execute).Success);
        Assert.True(session.Execute(Command(session,"jigsaw.place",index:0,x:500,y:400),native.Execute).Success);
        Assert.True(saved.ActiveGame!.Pieces[0].OnBoard);
        Assert.True(session.Execute(Command(session,"jigsaw.tray",index:0),native.Execute).Success);
        Assert.False(saved.ActiveGame!.Pieces[0].OnBoard);
        Assert.True(session.Execute(Command(session,"jigsaw.place",index:0,x:0,y:0),native.Execute).Success);
        Assert.True(saved.ActiveGame!.Pieces[0].Locked);
        Assert.False(session.Execute(Command(session,"jigsaw.tray",index:0),native.Execute).Success);
        Assert.Equal(1,saved.ActiveGame.LockedCount);
    }

    [Fact]
    public void PersistenceFailurePreservesLastCommittedBoardAndRevision()
    {
        var saved = new JigsawProgressState { SelectedPieceCount=24 };
        bool fail = false;
        var native = new ModernJigsawService(saved, p=>
        {
            if(fail)throw new IOException("Disk unavailable");
            saved=p;
        },()=>new JigsawStatistics(),_=>ProgressionAward.None);
        var session = new ModernJigsawSession();
        Assert.True(session.Execute(Command(session,"jigsaw.start"),native.Execute).Success);
        var initial = native.CurrentGame!.Pieces[3];
        fail=true;
        var command=Command(session,"jigsaw.place",index:3,x:200,y:200);
        Assert.Throws<IOException>(()=>session.Execute(command,native.Execute));
        Assert.False(native.CurrentGame!.Pieces[3].OnBoard);
        Assert.False(saved.ActiveGame!.Pieces[3].OnBoard);
        Assert.Equal(1L,session.Revision);
        Assert.Equal(initial.Id,native.CurrentGame.Pieces[3].Id);
    }

    [Fact]
    public void CompletedPuzzleIsSavedBeforeSingleRewardAndCanBeReopened()
    {
        var saved = new JigsawProgressState { SelectedPieceCount=24 };
        int rewards=0;
        var native = new ModernJigsawService(saved,p=>saved=p,()=>new JigsawStatistics(),
            _=>{rewards++;return ProgressionAward.None;});
        var session = new ModernJigsawSession();
        Assert.True(session.Execute(Command(session,"jigsaw.start"),native.Execute).Success);
        Guid gameId=native.CurrentGame!.GameId;
        for(int i=0;i<24;i++)
        {
            (double x,double y)=JigsawGame.Target(native.CurrentGame!,i);
            Assert.True(session.Execute(Command(session,"jigsaw.place",index:i,x:x,y:y),native.Execute).Success);
        }
        Assert.Null(saved.ActiveGame);
        Assert.Single(saved.CompletedGames);
        Assert.Equal(gameId,saved.CompletedGames[0].GameId);
        Assert.Equal(1,rewards);
        Assert.True(session.Execute(Command(session,"jigsaw.back"),native.Execute).Success);
        Assert.False(session.Execute(Command(session,"jigsaw.resume"),native.Execute).Success);
        saved.Normalize();
        Assert.Single(saved.CompletedGames);
    }

    [Fact]
    public void SnapshotContainsRealCoordinatesButNoPayoutAuthority()
    {
        var saved = new JigsawProgressState();
        var service = new ModernJigsawService(saved, p=>saved=p,()=>new JigsawStatistics(),
            _=>ProgressionAward.None);
        var session = new ModernJigsawSession();
        Assert.True(session.Execute(Command(session,"jigsaw.start"),service.Execute).Success);
        JsonElement s=JsonSerializer.SerializeToElement(service.Snapshot(),
            new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        JsonElement game=s.GetProperty("game");
        Assert.Equal(96,game.GetProperty("pieces").GetArrayLength());
        Assert.Equal(12,s.GetProperty("motifs").GetArrayLength());
        Assert.Equal(30,s.GetProperty("achievements").GetArrayLength());
        Assert.False(game.TryGetProperty("reward",out _));
        Assert.False(game.TryGetProperty("solution",out _));
        Assert.False(game.TryGetProperty("wallet",out _));
    }
}
