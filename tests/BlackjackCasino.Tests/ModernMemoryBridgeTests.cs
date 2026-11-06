using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Memory;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernMemoryBridgeTests
{
    private static string Request(ModernMemorySession session, string action,
        MemoryDifficulty? difficulty = null, string? deck = null, int? index = null,
        long? revision = null, string? requestId = null) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["type"] = "lounge.memory.command", ["version"] = 1,
            ["requestId"] = requestId ?? Guid.NewGuid().ToString("D"),
            ["session"] = session.Id, ["expectedRevision"] = revision ?? session.Revision,
            ["action"] = action, ["difficulty"] = difficulty?.ToString(),
            ["deckId"] = deck, ["index"] = index,
        }.Where(pair => pair.Value is not null).ToDictionary(x => x.Key, x => x.Value));

    private static ModernMemoryCommand Command(ModernMemorySession s, string action,
        MemoryDifficulty? difficulty = null, string? deck = null, int? index = null) =>
        ModernMemoryProtocol.Parse(Request(s,action,difficulty,deck,index))!;

    [Fact]
    public void VersionedProtocolRejectsMutationsOutsideWhitelistedMoves()
    {
        var session = new ModernMemorySession();
        foreach (string valid in new[]
        {
            Request(session,"memory.start",MemoryDifficulty.Easy),
            Request(session,"memory.start",MemoryDifficulty.Master),
            Request(session,"memory.deck",deck:"casino-icons"),
            Request(session,"memory.deck",deck:"card-suits"),
            Request(session,"memory.deck",deck:"animals"),
            Request(session,"memory.deck",deck:"fruits"),
            Request(session,"memory.resume"),Request(session,"memory.again"),
            Request(session,"memory.back"),Request(session,"memory.exit"),
            Request(session,"memory.flip",index:0),Request(session,"memory.flip",index:63),
        }) Assert.NotNull(ModernMemoryProtocol.Parse(valid));

        foreach (string invalid in new[]
        {
            Request(session,"memory.start"), Request(session,"memory.start",MemoryDifficulty.Easy,"animals"),
            Request(session,"memory.deck",deck:"nonexistent"),Request(session,"memory.deck"),
            Request(session,"memory.deck",deck:"animals",index:0),
            Request(session,"memory.flip"),Request(session,"memory.flip",index:64),
            Request(session,"memory.flip",index:-1),
            Request(session,"memory.flip",index:0,deck:"animals"),
            Request(session,"memory.reward"),Request(session,"memory.shuffle"),
            Request(session,"memory.flip",revision:-1),
            new string('X',2049),"{}","[]","not json",
        }) Assert.Null(ModernMemoryProtocol.Parse(invalid));
    }

    [Fact]
    public void ReceiptsRejectReplayStaleRevisionAndOtherSessions()
    {
        var session = new ModernMemorySession();
        ModernMemoryCommand cmd = Command(session,"memory.flip",index:0);
        int commits = 0;
        (bool Success,string Message) Commit(ModernMemoryCommand _) { commits++; return (true,"Saved"); }
        ModernMemoryReply first = session.Execute(cmd,Commit);
        Assert.True(first.Success);
        Assert.Equal(1L,session.Revision);
        Assert.Equal(first,session.Execute(cmd,Commit));
        Assert.False(session.Execute(cmd with{ RequestId=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.False(session.Execute(cmd with{
            RequestId=Guid.NewGuid().ToString("D"),Session=Guid.NewGuid().ToString("D")},Commit).Success);
        Assert.Equal(1,commits);
    }

    [Fact]
    public void EveryDifficultyAndDeckStartsInOriginalCSharpSaveSchema()
    {
        foreach (MemoryDifficulty difficulty in Enum.GetValues<MemoryDifficulty>())
        foreach (MemoryDeckDefinition deck in MemoryCatalog.Decks)
        {
            MemoryProgressState saved = new();
            var service = new ModernMemoryService(saved, next=>saved=next,()=>new MemoryStatistics(),
                _=>ProgressionAward.None);
            var session = new ModernMemorySession();
            Assert.True(session.Execute(Command(session,"memory.deck",deck:deck.Id),service.Execute).Success);
            Assert.True(session.Execute(Command(session,"memory.start",difficulty),service.Execute).Success);
            MemoryGameState current = Assert.IsType<MemoryGameState>(service.CurrentGame);
            MemoryDifficultySpec spec = MemoryCatalog.Spec(difficulty);
            Assert.Equal(spec.Cards,current.Cards.Count);
            Assert.Equal(deck.Id,current.DeckId);
            Assert.Equal(current.GameId,saved.ActiveGame!.GameId);
            Assert.Equal(current.Seed,saved.ActiveGame.Seed);
            Assert.Equal(MemoryGame.CreateCards(difficulty,deck.Id,current.Seed),current.Cards);
            Assert.All(current.Cards.GroupBy(x=>x),group=>Assert.Equal(2,group.Count()));
            Assert.True(session.Execute(Command(session,"memory.back"),service.Execute).Success);
            Assert.True(session.Execute(Command(session,"memory.resume"),service.Execute).Success);
            Assert.Equal(current.GameId,service.CurrentGame!.GameId);
        }
    }

    [Fact]
    public void HiddenCardsNeverExposeTheirNativeSymbolOrSeedInBrowserSnapshot()
    {
        MemoryProgressState saved = new();
        var service = new ModernMemoryService(saved,next=>saved=next,()=>new MemoryStatistics(),
            _=>ProgressionAward.None);
        var session = new ModernMemorySession();
        Assert.True(session.Execute(Command(session,"memory.start",MemoryDifficulty.Easy),service.Execute).Success);
        JsonElement snapshot=JsonSerializer.SerializeToElement(service.Snapshot(),
            new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        JsonElement board=snapshot.GetProperty("game");
        Assert.False(board.TryGetProperty("seed",out _));
        Assert.False(board.TryGetProperty("solution",out _));
        JsonElement cards=board.GetProperty("cards");
        Assert.Equal(16,cards.GetArrayLength());
        foreach(JsonElement card in cards.EnumerateArray())
        {
            Assert.False(card.GetProperty("faceUp").GetBoolean());
            Assert.Equal(JsonValueKind.Null,card.GetProperty("glyph").ValueKind);
            Assert.Equal(JsonValueKind.Null,card.GetProperty("name").ValueKind);
            Assert.Equal(JsonValueKind.Null,card.GetProperty("accent").ValueKind);
        }
        Assert.True(session.Execute(Command(session,"memory.flip",index:0),service.Execute).Success);
        snapshot=JsonSerializer.SerializeToElement(service.Snapshot(),
            new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
        cards=snapshot.GetProperty("game").GetProperty("cards");
        Assert.True(cards[0].GetProperty("faceUp").GetBoolean());
        Assert.Equal(JsonValueKind.String,cards[0].GetProperty("glyph").ValueKind);
        Assert.Equal(JsonValueKind.Null,cards[1].GetProperty("glyph").ValueKind);
        Assert.Single(saved.ActiveGame!.FaceUpIndices);
    }

    [Fact]
    public void MismatchIsPersistedResolvedAndDoesNotAcceptThirdFlip()
    {
        MemoryProgressState saved = new();
        var service = new ModernMemoryService(saved,next=>saved=next,()=>new MemoryStatistics(),
            _=>ProgressionAward.None);
        var session = new ModernMemorySession();
        Assert.True(session.Execute(Command(session,"memory.start",MemoryDifficulty.Easy),service.Execute).Success);
        MemoryGameState game = service.CurrentGame!;
        int other=Enumerable.Range(1,game.Cards.Count).First(i=>game.Cards[i]!=game.Cards[0]);
        int third=Enumerable.Range(0,game.Cards.Count).First(i=>i!=0&&i!=other);
        Assert.True(session.Execute(Command(session,"memory.flip",index:0),service.Execute).Success);
        Assert.True(session.Execute(Command(session,"memory.flip",index:other),service.Execute).Success);
        Assert.Equal(2,service.CurrentGame!.FaceUpIndices.Count);
        Assert.Equal(1,service.CurrentGame.WrongMatches);
        Assert.False(session.Execute(Command(session,"memory.flip",index:third),service.Execute).Success);
        Assert.Equal(2,saved.ActiveGame!.FaceUpIndices.Count);
        Assert.True(session.Execute(Command(session,"memory.back"),service.Execute).Success);
        Assert.Empty(saved.ActiveGame!.FaceUpIndices);
        Assert.True(session.Execute(Command(session,"memory.resume"),service.Execute).Success);
        Assert.Equal(1,service.CurrentGame!.WrongMatches);
        Assert.Empty(service.CurrentGame.FaceUpIndices);
    }

    [Fact]
    public void FailedPersistenceCannotMutateTableOrRewardAndPlayerCanRetry()
    {
        MemoryProgressState saved=new();
        bool fail=false;
        int awards=0;
        var service=new ModernMemoryService(saved,next=>{
            if(fail)throw new IOException("storage disconnected");
            saved=next;
        },()=>new MemoryStatistics(),_=>{awards++;return ProgressionAward.None;});
        var session=new ModernMemorySession();
        Assert.True(session.Execute(Command(session,"memory.start",MemoryDifficulty.Easy),service.Execute).Success);
        long revision=session.Revision;
        fail=true;
        ModernMemoryCommand flip=Command(session,"memory.flip",index:0);
        Assert.Throws<IOException>(()=>session.Execute(flip,service.Execute));
        Assert.Equal(revision,session.Revision);
        Assert.Empty(service.CurrentGame!.FaceUpIndices);
        Assert.Empty(saved.ActiveGame!.FaceUpIndices);
        Assert.Equal(0,awards);
        fail=false;
        Assert.True(session.Execute(flip,service.Execute).Success);
        Assert.Single(service.CurrentGame!.FaceUpIndices);
        Assert.Equal(revision + 1,session.Revision);
        Assert.Single(saved.ActiveGame!.FaceUpIndices);
    }

    [Fact]
    public void CompletionRecordsOnceAndAllowsNewDealWithOriginalDifficultyAndDeck()
    {
        MemoryProgressState saved=new();
        var stats=new MemoryStatistics();
        int rewards=0;
        var service=new ModernMemoryService(saved,next=>saved=next,()=>stats,
            receipt=>{rewards++;stats.Record(receipt);return ProgressionAward.None;});
        var session=new ModernMemorySession();
        Assert.True(session.Execute(Command(session,"memory.deck",deck:"fruits"),service.Execute).Success);
        Assert.True(session.Execute(Command(session,"memory.start",MemoryDifficulty.Easy),service.Execute).Success);
        MemoryGameState original=service.CurrentGame!;
        Guid originalId=original.GameId;
        foreach(int[] pair in original.Cards.Select((card,index)=>(card,index))
            .GroupBy(x=>x.card).Select(group=>group.Select(x=>x.index).ToArray()))
        {
            Assert.True(session.Execute(Command(session,"memory.flip",index:pair[0]),service.Execute).Success);
            Assert.True(session.Execute(Command(session,"memory.flip",index:pair[1]),service.Execute).Success);
        }
        Assert.Null(service.CurrentGame);
        Assert.Null(saved.ActiveGame);
        Assert.Single(saved.CompletedGames);
        Assert.Equal(originalId,saved.CompletedGames[0].GameId);
        Assert.True(saved.CompletedGames[0].Perfect);
        Assert.Equal(1,rewards);
        Assert.Equal(1,stats.CompletedGames);
        Assert.True(session.Execute(Command(session,"memory.again"),service.Execute).Success);
        Assert.NotEqual(originalId,service.CurrentGame!.GameId);
        Assert.Equal("fruits",service.CurrentGame.DeckId);
        Assert.Equal(MemoryDifficulty.Easy,service.CurrentGame.Difficulty);
        Assert.Equal(1,rewards);
    }
}
