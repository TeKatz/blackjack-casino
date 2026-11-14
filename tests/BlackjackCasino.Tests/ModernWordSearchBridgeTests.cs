using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.WordSearch;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class ModernWordSearchBridgeTests
{
    private static string Request(ModernWordSearchSession s, string action,
        WordSearchDifficulty? difficulty = null,
        int? sr = null, int? sc = null, int? er = null, int? ec = null,
        long? revision = null, string? id = null) =>
        JsonSerializer.Serialize(new Dictionary<string,object?>
        {
            ["type"] = "lounge.word-search.command", ["version"] = 1,
            ["requestId"] = id ?? Guid.NewGuid().ToString("D"),
            ["session"] = s.Id, ["expectedRevision"] = revision ?? s.Revision,
            ["action"] = action, ["difficulty"] = difficulty?.ToString(),
            ["startRow"] = sr, ["startColumn"] = sc, ["endRow"] = er, ["endColumn"] = ec,
        }.Where(pair => pair.Value is not null).ToDictionary(pair => pair.Key, pair => pair.Value));

    [Fact]
    public void ProtocolAllowsOnlyBoundedLinesAndSupportedGameIntents()
    {
        var s = new ModernWordSearchSession();
        foreach (string valid in new[]
        {
            Request(s,"word-search.start",WordSearchDifficulty.Relaxed),
            Request(s,"word-search.start",WordSearchDifficulty.Master),
            Request(s,"word-search.resume"),Request(s,"word-search.hint"),
            Request(s,"word-search.back"),Request(s,"word-search.exit"),
            Request(s,"word-search.select",sr:0,sc:0,er:0,ec:7),
            Request(s,"word-search.select",sr:12,sc:12,er:0,ec:0),
            Request(s,"word-search.select",sr:0,sc:12,er:12,ec:0),
        }) Assert.NotNull(ModernWordSearchProtocol.Parse(valid));

        foreach (string invalid in new[]
        {
            Request(s,"word-search.start"),
            Request(s,"word-search.hint",WordSearchDifficulty.Easy),
            Request(s,"word-search.select",sr:-1,sc:0,er:0,ec:0),
            Request(s,"word-search.select",sr:0,sc:0,er:13,ec:0),
            Request(s,"word-search.select",sr:0,sc:0,er:2,ec:3),
            Request(s,"word-search.select",sr:0,sc:0,er:0,ec:0),
            Request(s,"word-search.select",sr:0,sc:0,er:1),
            Request(s,"word-search.reward"), "{}", "[]", "bad-json", new string('X',2049),
            Request(s,"word-search.resume",revision:-1),
        }) Assert.Null(ModernWordSearchProtocol.Parse(invalid));
    }

    [Fact]
    public void ReplayAndStaleSessionCannotCommitTwoWords()
    {
        var s = new ModernWordSearchSession();
        var cmd=ModernWordSearchProtocol.Parse(Request(s,"word-search.resume"))!;
        int writes=0;
        (bool Success,string Message) Commit(ModernWordSearchCommand _) { writes++;return(true,"Saved"); }
        ModernWordSearchReply first=s.Execute(cmd,Commit);
        Assert.True(first.Success);
        Assert.Equal(1L,s.Revision);
        Assert.Equal(first,s.Execute(cmd,Commit));
        Assert.False(s.Execute(cmd with{ RequestId=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.False(s.Execute(cmd with{ RequestId=Guid.NewGuid().ToString("D"),Session=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.Equal(1,writes);
    }

    [Fact]
    public void NativeWordFindAndHintSaveAfterCommitAndResumeWithoutRegeneration()
    {
        WordSearchProgressState saved = new();
        int writes=0;
        var service=new ModernWordSearchService(saved,next=>{saved=next;writes++;},
            ()=>new WordSearchStatistics(),_=>ProgressionAward.None,()=> "de");
        var s=new ModernWordSearchSession();
        void Execute(string action,WordSearchDifficulty? difficulty=null,int? sr=null,int? sc=null,int? er=null,int? ec=null)
        {
            var command=ModernWordSearchProtocol.Parse(Request(s,action,difficulty,sr,sc,er,ec))!;
            Assert.True(s.Execute(command,service.Execute).Success);
        }

        Execute("word-search.start",WordSearchDifficulty.Relaxed);
        WordSearchGameState first=service.CurrentGame!;
        Assert.Equal(8,first.Puzzle.Size);
        Assert.Equal(6,first.Puzzle.Placements.Count);
        Assert.Equal(first.Puzzle.Grid,saved.ActiveGame!.Puzzle.Grid);
        WordSearchPlacement target=first.Puzzle.Placements[0];
        Execute("word-search.select",sr:target.EndRow,sc:target.EndColumn,er:target.StartRow,ec:target.StartColumn);
        Assert.Single(saved.ActiveGame!.FoundWords);
        Assert.Single(saved.DiscoveredWords);
        Assert.Equal(target.Word, saved.DiscoveredWords[0].Word);
        Execute("word-search.hint");
        Assert.Equal(1,service.CurrentGame!.HintsUsed);
        Execute("word-search.back");
        Assert.NotNull(saved.ActiveGame);
        Execute("word-search.resume");
        Assert.Equal(first.Puzzle.Grid,service.CurrentGame!.Puzzle.Grid);
        Assert.Equal(1,service.CurrentGame.HintsUsed);
        Assert.Single(service.CurrentGame.FoundWords);
        Assert.True(writes>=3);
    }

    [Fact]
    public void FailedWritePreservesNativePuzzleAndCompletionReceiptIsGrantedOnlyOnce()
    {
        WordSearchProgressState saved = new();
        bool fail=false;
        int rewards=0;
        int cues=0;
        var service=new ModernWordSearchService(saved,next=>{
            if(fail)throw new IOException("Disk offline");
            saved=next;
        },()=>new WordSearchStatistics(),_=>{rewards++;return ProgressionAward.None;},()=> "en",_=>cues++);
        var s=new ModernWordSearchSession();
        ModernWordSearchReply Do(string action,WordSearchDifficulty? d=null,WordSearchPlacement? p=null)
        {
            var cmd=ModernWordSearchProtocol.Parse(Request(s,action,d,
                p?.StartRow,p?.StartColumn,p?.EndRow,p?.EndColumn))!;
            return s.Execute(cmd,service.Execute);
        }
        Assert.True(Do("word-search.start",WordSearchDifficulty.Relaxed).Success);
        var placements=service.CurrentGame!.Puzzle.Placements.ToArray();
        fail=true;
        Assert.Throws<IOException>(()=>Do("word-search.select",p:placements[0]));
        Assert.Empty(service.CurrentGame!.FoundWords);
        Assert.Empty(saved.ActiveGame!.FoundWords);
        Assert.Empty(saved.DiscoveredWords);
        Assert.Equal(0,cues);
        fail=false;
        foreach(var placement in placements)Assert.True(Do("word-search.select",p:placement).Success);
        Assert.True(service.CurrentGame!.Completed);
        Assert.Null(saved.ActiveGame);
        Assert.Single(saved.CompletedGames);
        Assert.Equal(1,rewards);
        Assert.Equal(placements.Length,cues);
        Assert.Equal(placements.Length,saved.DiscoveredWords.Count);
        Assert.True(Do("word-search.back").Success);
        Assert.False(Do("word-search.resume").Success);
    }

    [Fact]
    public void FirstDiscoveryAndRepeatDiscoveryHaveDistinctNativeReceiptsAndCues()
    {
        WordSearchGameState active = WordSearchGame.Start(WordSearchCatalog.For(WordSearchDifficulty.Relaxed)[0]);
        WordSearchPlacement target = active.Puzzle.Placements[0];
        var saved = new WordSearchProgressState { ActiveGame = active };
        var cues = new List<bool>();
        var service = new ModernWordSearchService(saved, state=>saved=state,
            ()=>new WordSearchStatistics(),_=>ProgressionAward.None,()=>"en",fresh=>cues.Add(fresh));
        var session = new ModernWordSearchSession();
        bool Send(string action,WordSearchPlacement? selected=null)
        {
            var req = ModernWordSearchProtocol.Parse(Request(session,action,
                sr:selected?.StartRow,sc:selected?.StartColumn,er:selected?.EndRow,ec:selected?.EndColumn))!;
            return session.Execute(req,service.Execute).Success;
        }
        Assert.True(Send("word-search.resume"));
        Assert.True(Send("word-search.select",target));
        Assert.Equal([true],cues);
        JsonElement first = JsonSerializer.SerializeToElement(service.Snapshot(),new JsonSerializerOptions
        {PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        Assert.True(first.GetProperty("lastFind").GetProperty("new").GetBoolean());
        Assert.Equal(target.Word,first.GetProperty("lastFind").GetProperty("word").GetString());
        Assert.Single(saved.DiscoveredWords);
        // Duplicate target selections cannot play an additional cue.
        Assert.False(Send("word-search.select",target));
        Assert.Single(cues);
        // A second puzzle with the same target is a successful ordinary find,
        // but not a fresh addition to the album.
        var second = WordSearchCatalog.All.Select(d=>WordSearchGame.Start(d))
            .First(g=>g.Puzzle.Id!=active.Puzzle.Id &&
                g.Puzzle.Placements.Any(p=>p.Word==target.Word));
        saved.ActiveGame=second;
        var again = new ModernWordSearchService(saved,state=>saved=state,
            ()=>new WordSearchStatistics(),_=>ProgressionAward.None,()=>"en",fresh=>cues.Add(fresh));
        var nextSession = new ModernWordSearchSession();
        Assert.True(nextSession.Execute(ModernWordSearchProtocol.Parse(Request(nextSession,"word-search.resume"))!,again.Execute).Success);
        WordSearchPlacement repeated=second.Puzzle.Placements.First(p=>p.Word==target.Word);
        Assert.True(nextSession.Execute(ModernWordSearchProtocol.Parse(Request(nextSession,"word-search.select",
            sr:repeated.StartRow,sc:repeated.StartColumn,er:repeated.EndRow,ec:repeated.EndColumn))!,again.Execute).Success);
        Assert.Equal([true,false],cues);
        Assert.Single(saved.DiscoveredWords);
    }

    [Fact]
    public void StartingAnotherPuzzleReplacesOnlyActiveSaveAndCannotDuplicateRewards()
    {
        WordSearchProgressState saved = new();
        int rewards = 0, writes = 0;
        bool fail = false;
        var service = new ModernWordSearchService(saved, next =>
        {
            if (fail) throw new IOException("Simulated save failure");
            saved = next; writes++;
        }, () => new WordSearchStatistics(), _ => { rewards++; return ProgressionAward.None; }, () => "en");
        var session = new ModernWordSearchSession();
        bool Do(string action, WordSearchDifficulty? difficulty = null, WordSearchPlacement? placement = null)
        {
            var command = ModernWordSearchProtocol.Parse(Request(session, action, difficulty,
                placement?.StartRow, placement?.StartColumn, placement?.EndRow, placement?.EndColumn))!;
            return session.Execute(command, service.Execute).Success;
        }
        Assert.True(Do("word-search.start", WordSearchDifficulty.Relaxed));
        WordSearchGameState first = service.CurrentGame!;
        Assert.True(Do("word-search.select", placement: first.Puzzle.Placements[0]));
        Assert.Single(saved.DiscoveredWords);
        Assert.False(Do("word-search.start", WordSearchDifficulty.Easy)); // Must leave the grid first.
        Assert.True(Do("word-search.back"));
        fail = true;
        Assert.Throws<IOException>(() => Do("word-search.start", WordSearchDifficulty.Relaxed));
        Assert.Equal(first.Puzzle.Id, saved.ActiveGame!.Puzzle.Id);
        fail = false;
        Assert.True(Do("word-search.start", WordSearchDifficulty.Relaxed));
        Assert.NotEqual(first.Puzzle.Id, service.CurrentGame!.Puzzle.Id);
        Assert.Empty(service.CurrentGame.FoundWords);
        Assert.Single(saved.DiscoveredWords);
        Assert.Empty(saved.CompletedGames);
        Assert.Equal(0, rewards);
        Assert.True(writes >= 3);
    }

    [Fact]
    public void PreviouslySavedEnglishGridRemainsIdenticalIfUserResumesInGerman()
    {
        WordSearchGameState original=WordSearchGame.Start(WordSearchCatalog.For(WordSearchDifficulty.Hard)[0],language:"en");
        original.FoundWords.Add(original.Puzzle.Placements[0].Word);
        var progress=new WordSearchProgressState{ActiveGame=original};
        var service=new ModernWordSearchService(progress,_=>{},()=>new WordSearchStatistics(),
            _=>ProgressionAward.None,()=> "de");
        var s=new ModernWordSearchSession();
        var cmd=ModernWordSearchProtocol.Parse(Request(s,"word-search.resume"))!;
        Assert.True(s.Execute(cmd,service.Execute).Success);
        Assert.Equal(original.Puzzle.Grid,service.CurrentGame!.Puzzle.Grid);
        Assert.Equal(original.Puzzle.Theme,service.CurrentGame.Puzzle.Theme);
        Assert.Contains(original.Puzzle.Placements[0].Word,service.CurrentGame.FoundWords);
    }
}
