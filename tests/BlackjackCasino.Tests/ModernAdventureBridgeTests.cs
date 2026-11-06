using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernAdventureBridgeTests
{
    private static string Request(ModernAdventureSession session, string action,
        string? adventureId = null, string? classId = null, string? choiceId = null,
        string? nodeId = null, bool? replace = null, int? index = null,
        double? x = null, double? y = null, double? zoom = null,
        double? panX = null, double? panY = null,
        string? id = null, long? revision = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["type"] = "lounge.adventure.command", ["version"] = 1,
            ["requestId"] = id ?? Guid.NewGuid().ToString("D"),
            ["session"] = session.Id, ["expectedRevision"] = revision ?? session.Revision,
            ["action"] = action, ["adventureId"] = adventureId, ["classId"] = classId,
            ["choiceId"] = choiceId, ["nodeId"] = nodeId,
            ["confirmReplace"] = replace, ["index"] = index,
            ["x"] = x, ["y"] = y, ["zoom"] = zoom, ["panX"] = panX, ["panY"] = panY,
        };
        return JsonSerializer.Serialize(values.Where(p => p.Value is not null)
            .ToDictionary(p => p.Key, p => p.Value));
    }

    private static ModernAdventureCommand Command(ModernAdventureSession s, string action,
        string? adventureId = null, string? classId = null, string? choiceId = null,
        string? nodeId = null, bool? replace = null, int? index = null,
        double? x = null, double? y = null, double? zoom = null,
        double? panX = null, double? panY = null) =>
        ModernAdventureProtocol.Parse(Request(s, action, adventureId, classId, choiceId, nodeId,
            replace, index, x, y, zoom, panX, panY))!;

    [Fact]
    public void ProtocolForbidsForgedOutcomesDiceAndInventory()
    {
        var s = new ModernAdventureSession();
        foreach (string valid in new[]
        {
            Request(s,"adventure.select",adventureId:"black-stag"),
            Request(s,"adventure.resume",adventureId:"dead-mans-hand"),
            Request(s,"adventure.start",adventureId:"drowned-bell",classId:"rogue"),
            Request(s,"adventure.start",adventureId:"black-stag",classId:"warrior",replace:true),
            Request(s,"adventure.restart",adventureId:"black-stag"),
            Request(s,"adventure.choice",choiceId:"speak-to-stranger"),
            Request(s,"adventure.roll"),
            Request(s,"adventure.board.open"),Request(s,"adventure.board.close"),
            Request(s,"adventure.board.move",nodeId:"some-clue",x:300,y:200),
            Request(s,"adventure.board.view",zoom:1,panX:0,panY:0),
            Request(s,"adventure.encounter.hit"),Request(s,"adventure.encounter.stand"),
            Request(s,"adventure.encounter.roll"),Request(s,"adventure.encounter.bank"),
            Request(s,"adventure.encounter.hold",index:4),
            Request(s,"adventure.encounter.finish"),
            Request(s,"adventure.back"),Request(s,"adventure.exit"),
        }) Assert.NotNull(ModernAdventureProtocol.Parse(valid));

        foreach (string invalid in new[]
        {
            Request(s,"adventure.reward"),Request(s,"adventure.encounter.win"),
            Request(s,"adventure.select",adventureId:"not-a-tale"),
            Request(s,"adventure.start",adventureId:"black-stag"),
            Request(s,"adventure.start",adventureId:"black-stag",classId:"rogue",replace:false),
            Request(s,"adventure.choice",choiceId:"../../save"),
            Request(s,"adventure.choice"),
            Request(s,"adventure.board.move",nodeId:"x",x:-1,y:10),
            Request(s,"adventure.board.view",zoom:5,panX:0,panY:0),
            Request(s,"adventure.encounter.hold",index:5),
            Request(s,"adventure.encounter.hit",index:0),
            Request(s,"adventure.roll",revision:-1),
            Request(s,"adventure.roll").Replace("\"action\":\"adventure.roll\"",
                "\"action\":\"adventure.roll\",\"outcome\":\"Win\""),
            new string('X',2049),"{}","[]","not JSON",
        }) Assert.Null(ModernAdventureProtocol.Parse(invalid));
    }

    [Fact]
    public void ReplaysStaleRevisionsAndOtherSessionsNeverCommit()
    {
        var s = new ModernAdventureSession();
        var request = Command(s,"adventure.select",adventureId:"black-stag");
        int commits=0;
        (bool Success,string Message) Commit(ModernAdventureCommand _)
        { commits++; return (true,"Saved"); }
        ModernAdventureReply first = s.Execute(request,Commit);
        Assert.True(first.Success);
        Assert.Equal(first,s.Execute(request,Commit));
        Assert.False(s.Execute(request with { RequestId=Guid.NewGuid().ToString("D") },Commit).Success);
        Assert.False(s.Execute(request with { RequestId=Guid.NewGuid().ToString("D"),
            Session=Guid.NewGuid().ToString("D") },Commit).Success);
        Assert.Equal(1,commits);
        Assert.Equal(1L,s.Revision);
    }

    [Fact]
    public void EveryCatalogAdventureStartsAndResumesThroughOldSaveFormat()
    {
        foreach (AdventureDefinition tale in AdventureCatalog.All)
        {
            var saved = new AdventureProgressState();
            var service = new ModernAdventureService(saved,
                p=>saved=p, (_,_)=>new AdventureCompletionMetaResult(false,false,"",[],null));
            var s = new ModernAdventureSession();
            string archetype = AdventureCharacterCatalog.AllFor(tale)[0].Id;
            Assert.True(s.Execute(Command(s,"adventure.start",adventureId:tale.Id,classId:archetype),
                service.Execute).Success);
            AdventureRunState game = service.ActiveRun!;
            Assert.Equal(tale.Id,game.AdventureId);
            Assert.Equal(archetype,game.Character.ClassId);
            string id = game.RunId;
            Assert.True(s.Execute(Command(s,"adventure.back"),service.Execute).Success);
            Assert.True(s.Execute(Command(s,"adventure.resume",adventureId:tale.Id),service.Execute).Success);
            Assert.Equal(id,service.ActiveRun!.RunId);
            Assert.Equal(saved.Runs[tale.Id].CurrentSceneId,service.ActiveRun.CurrentSceneId);
            Assert.Equal(tale.Id,ModernAdventureService.Clone(saved).Runs[tale.Id].AdventureId);
        }
    }

    [Fact]
    public void NativeStoryChoiceAndD20AreNotComputedInJavascript()
    {
        var saved = new AdventureProgressState();
        var service = new ModernAdventureService(saved,p=>saved=p,
            (_,_)=>new AdventureCompletionMetaResult(false,false,"",[],null));
        var s = new ModernAdventureSession();
        Assert.True(s.Execute(Command(s,"adventure.start",adventureId:"black-stag",classId:"rogue"),
            service.Execute).Success);
        var engine = new AdventureEngine(AdventureCatalog.BlackStag,service.ActiveRun!);
        AdventureChoiceDefinition first = engine.AvailableChoices().First();
        Assert.True(s.Execute(Command(s,"adventure.choice",choiceId:first.Id),service.Execute).Success);
        Assert.Equal(1,saved.Runs["black-stag"].Statistics.ChoicesMade);
        Assert.False(s.Execute(Command(s,"adventure.choice",choiceId:"not-in-scene"),service.Execute).Success);
        if (saved.Runs["black-stag"].PendingChoiceId is not null)
        {
            Assert.True(s.Execute(Command(s,"adventure.roll"),service.Execute).Success);
            Assert.Equal(1,saved.Runs["black-stag"].RollsMade);
        }
        Assert.Equal(service.ActiveRun!.RunId,saved.Runs["black-stag"].RunId);
    }

    [Fact]
    public void PersistenceFailureNeverMutatesLastCommittedStory()
    {
        var saved = new AdventureProgressState();
        bool fail=false;
        var service = new ModernAdventureService(saved,p=>{
            if(fail)throw new IOException("Disk unavailable");
            saved=p;
        },(_,_)=>new AdventureCompletionMetaResult(false,false,"",[],null));
        var s = new ModernAdventureSession();
        Assert.True(s.Execute(Command(s,"adventure.start",adventureId:"black-stag",classId:"rogue"),
            service.Execute).Success);
        string scene = service.ActiveRun!.CurrentSceneId;
        string choice = new AdventureEngine(AdventureCatalog.BlackStag,
            ModernAdventureService.CloneState(service.ActiveRun)).AvailableChoices()[0].Id;
        fail=true;
        Assert.Throws<IOException>(()=>s.Execute(Command(s,"adventure.choice",choiceId:choice),
            service.Execute));
        Assert.Equal(scene,service.ActiveRun!.CurrentSceneId);
        Assert.Equal(scene,saved.Runs["black-stag"].CurrentSceneId);
        Assert.Equal(0,saved.Runs["black-stag"].Statistics.ChoicesMade);
        Assert.Equal(1,s.Revision);
    }

    private static AdventureProgressState StagedEncounter(string adventureId, string sceneId)
    {
        Assert.True(AdventureCatalog.TryGet(adventureId, out AdventureDefinition? found));
        AdventureDefinition tale = found!;
        AdventureRunState run = AdventureEngine.StartNew(tale,
            AdventureCharacterCatalog.Create(tale,AdventureCharacterCatalog.AllFor(tale)[0].Id)).State;
        run.CurrentSceneId = sceneId;
        return new AdventureProgressState { LastAdventureId = adventureId,
            Runs = new Dictionary<string,AdventureRunState> { [adventureId] = run } };
    }

    [Fact]
    public void RealBlackjackHandSettlesAndRecordsOutcomeOnce()
    {
        var saved = StagedEncounter("strangers-wager","blackjack-table");
        int completions=0;
        var service = new ModernAdventureService(saved,p=>saved=p,
            (_,_)=>{completions++;return new AdventureCompletionMetaResult(false,false,"",[],null);});
        var s = new ModernAdventureSession();
        Assert.True(s.Execute(Command(s,"adventure.resume",adventureId:"strangers-wager"),
            service.Execute).Success);
        Assert.Equal("encounter",service.Screen);
        JsonElement snapshot = JsonSerializer.SerializeToElement(service.Snapshot(),
            new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        JsonElement blackjack = snapshot.GetProperty("encounter").GetProperty("blackjack");
        if (blackjack.GetProperty("dealerTotal").ValueKind==JsonValueKind.Null)
        {
            Assert.Equal(JsonValueKind.Null,blackjack.GetProperty("dealer")[1].GetProperty("rank").ValueKind);
            Assert.True(s.Execute(Command(s,"adventure.encounter.stand"),service.Execute).Success);
        }
        Assert.True(s.Execute(Command(s,"adventure.encounter.finish"),service.Execute).Success);
        Assert.NotEqual("blackjack-table",saved.Runs["strangers-wager"].CurrentSceneId);
        Assert.Equal(1,saved.Runs["strangers-wager"].Statistics.EncountersPlayed);
        Assert.False(s.Execute(Command(s,"adventure.encounter.finish"),service.Execute).Success);
        Assert.Equal(1,saved.Runs["strangers-wager"].Statistics.EncountersPlayed);
        Assert.InRange(completions,0,1);
    }

    [Fact]
    public void FiveDiceHandUsesOriginalScoringAndOpponentAi()
    {
        var saved = StagedEncounter("strangers-wager","five-kind-table");
        var service = new ModernAdventureService(saved,p=>saved=p,
            (_,_)=>new AdventureCompletionMetaResult(false,false,"",[],null));
        var s = new ModernAdventureSession();
        Assert.True(s.Execute(Command(s,"adventure.resume",adventureId:"strangers-wager"),
            service.Execute).Success);
        Assert.Equal("encounter",service.Screen);
        Assert.False(s.Execute(Command(s,"adventure.encounter.bank"),service.Execute).Success);
        Assert.True(s.Execute(Command(s,"adventure.encounter.roll"),service.Execute).Success);
        Assert.True(s.Execute(Command(s,"adventure.encounter.bank"),service.Execute).Success);
        Assert.True(s.Execute(Command(s,"adventure.encounter.finish"),service.Execute).Success);
        Assert.Equal(1,saved.Runs["strangers-wager"].Statistics.EncountersPlayed);
        Assert.NotEqual("five-kind-table",saved.Runs["strangers-wager"].CurrentSceneId);
    }

    [Fact]
    public void CaseBoardRefusesUndiscoveredClues()
    {
        AdventureDefinition tale = AdventureCatalog.DeadMansHand;
        var saved = new AdventureProgressState();
        var service = new ModernAdventureService(saved,p=>saved=p,
            (_,_)=>new AdventureCompletionMetaResult(false,false,"",[],null));
        var s = new ModernAdventureSession();
        Assert.True(s.Execute(Command(s,"adventure.start",adventureId:tale.Id,
            classId:AdventureCharacterCatalog.AllFor(tale)[0].Id),service.Execute).Success);
        Assert.True(s.Execute(Command(s,"adventure.board.open"),service.Execute).Success);
        CaseNodeDefinition hidden = tale.CaseBoard!.Nodes.First(n=>
            !service.ActiveRun!.CaseBoard.Nodes[n.Id].IsKnown);
        Assert.False(s.Execute(Command(s,"adventure.board.move",nodeId:hidden.Id,x:200,y:300),
            service.Execute).Success);
        Assert.True(s.Execute(Command(s,"adventure.board.view",zoom:1.5,panX:120,panY:60),
            service.Execute).Success);
        Assert.Equal(1.5,saved.Runs[tale.Id].CaseBoard.Layout.Zoom);
    }
}
