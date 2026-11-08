using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Solitaire;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ModernSolitaireBridgeTests
{
    private static string Command(ModernSolitaireSession session, string action,
        SolitaireVariant? variant = null, int? suits = null, int? deal = null,
        string? source = null, int? index = null, string? target = null,
        long? revision = null, string? id = null) =>
        JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["type"] = "lounge.solitaire.command", ["version"] = 1,
            ["requestId"] = id ?? Guid.NewGuid().ToString("D"),
            ["session"] = session.Id, ["expectedRevision"] = revision ?? session.Revision,
            ["action"] = action,
            ["variant"] = variant?.ToString(), ["spiderSuits"] = suits,
            ["dealNumber"] = deal, ["source"] = source, ["index"] = index, ["target"] = target,
        }.Where(x => x.Value is not null).ToDictionary(x => x.Key, x => x.Value));

    [Fact]
    public void OnlyBoundedGameIntentsCanBeDispatched()
    {
        var session = new ModernSolitaireSession();
        foreach (string valid in new[]
        {
            Command(session, "solitaire.new", SolitaireVariant.Klondike, 1),
            Command(session, "solitaire.new", SolitaireVariant.FreeCell, 1, 617),
            Command(session, "solitaire.new", SolitaireVariant.Spider, 1),
            Command(session, "solitaire.new", SolitaireVariant.Spider, 2),
            Command(session, "solitaire.new", SolitaireVariant.Spider, 4),
            Command(session, "solitaire.resume", SolitaireVariant.TriPeaks, 1),
            Command(session, "solitaire.draw"),
            Command(session, "solitaire.back"),
            Command(session, "solitaire.exit"),
            Command(session, "solitaire.activate", source:"P27", index:0),
            Command(session, "solitaire.auto", source:"T0", index:3),
            Command(session, "solitaire.move", source:"T0", index:2, target:"F-Clubs"),
        }) Assert.NotNull(ModernSolitaireProtocol.Parse(valid));

        foreach (string invalid in new[]
        {
            Command(session, "solitaire.new", SolitaireVariant.Spider, 3),
            Command(session, "solitaire.new", SolitaireVariant.Klondike, 4),
            Command(session, "solitaire.new", SolitaireVariant.Klondike, 1, 9),
            Command(session, "solitaire.new", SolitaireVariant.FreeCell, 1, 0),
            Command(session, "solitaire.resume", SolitaireVariant.FreeCell, 1, 617),
            Command(session, "solitaire.draw", source:"Stock"),
            Command(session, "solitaire.activate", source:"P28", index:0),
            Command(session, "solitaire.auto", source:"T-1", index:2),
            Command(session, "solitaire.move", source:"T0", index:1, target:"T0"),
            Command(session, "solitaire.move", source:"T0", index:104, target:"T1"),
            Command(session, "solitaire.move", source:"FOO", index:0, target:"T0"),
            Command(session, "solitaire.payout"),
            Command(session, "solitaire.draw", revision:-1),
            "{}", "[]", "not-json", new string('X', 2049),
        }) Assert.Null(ModernSolitaireProtocol.Parse(invalid));
    }

    [Fact]
    public void ReplayAndStaleRevisionCannotApplyTwoMoves()
    {
        var session = new ModernSolitaireSession();
        int commits = 0;
        ModernSolitaireCommand first = ModernSolitaireProtocol.Parse(Command(session, "solitaire.draw"))!;
        (bool Success, string Message) Apply(ModernSolitaireCommand _) { commits++; return (true,"Saved"); }
        ModernSolitaireReply receipt = session.Execute(first, Apply);
        Assert.True(receipt.Success);
        Assert.Equal(receipt, session.Execute(first, Apply));
        Assert.Equal(1, commits);
        Assert.Equal(1L, session.Revision);
        Assert.False(session.Execute(first with { RequestId = Guid.NewGuid().ToString("D") }, Apply).Success);
        Assert.Equal(1, commits);
        Assert.False(session.Execute(first with
        { Session = Guid.NewGuid().ToString("D"), RequestId = Guid.NewGuid().ToString("D") }, Apply).Success);
    }

    [Fact]
    public void AllVariantsUseCSharpDealsAndPreserveExistingSaveFormat()
    {
        SolitaireProgressState saved = new();
        var stats = new SolitaireStatistics();
        var service = Create(refSaved => saved = refSaved, () => stats,
            start => stats.RecordStart(start), abandon => stats.RecordAbandon(abandon),
            initial: saved);
        var session = new ModernSolitaireSession();
        var cases = new (SolitaireVariant Variant, int Suits)[]
        {
            (SolitaireVariant.Klondike,1),(SolitaireVariant.FreeCell,1),
            (SolitaireVariant.Spider,1),(SolitaireVariant.Spider,2),(SolitaireVariant.Spider,4),
            (SolitaireVariant.Pyramid,1),(SolitaireVariant.Golf,1),(SolitaireVariant.TriPeaks,1),
        };
        foreach ((SolitaireVariant variant, int suits) in cases)
        {
            ModernSolitaireCommand start = ModernSolitaireProtocol.Parse(
                Command(session,"solitaire.new",variant,suits))!;
            Assert.True(session.Execute(start,service.Execute).Success);
            SolitaireGameState game = Assert.IsType<SolitaireGameState>(service.CurrentGame);
            Assert.Equal(variant,game.Variant);
            Assert.Equal(suits,game.SpiderSuits);
            Assert.Equal(game.GameId,saved.ActiveGames[SolitaireProgressState.GameKey(variant,suits)].GameId);
            Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
                Command(session,"solitaire.back"))!,service.Execute).Success);
        }
        Assert.Equal(8,stats.GamesStarted);
        Assert.Equal(8,saved.ActiveGames.Count);
        Assert.Equal(8,cases.Select(c => SolitaireProgressState.GameKey(c.Variant,c.Suits)).Distinct().Count());
    }

    [Fact]
    public void NumberedFreeCellDealAndFailedWriteAreHandledWithoutChangingTheBoard()
    {
        SolitaireProgressState saved = new();
        var stats = new SolitaireStatistics();
        int writes = 0;
        bool fail = false;
        var service = Create(next =>
        {
            if (fail) throw new IOException("storage offline");
            writes++;
            saved = next;
        }, () => stats, start => stats.RecordStart(start), abandon => stats.RecordAbandon(abandon),
            initial: saved);
        var session = new ModernSolitaireSession();
        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.new",SolitaireVariant.FreeCell,1,617))!,service.Execute).Success);
        SolitaireGameState game = Assert.IsType<SolitaireGameState>(service.CurrentGame);
        Assert.Equal(617, game.DealNumber);
        var replay = SolitaireGame.Create(SolitaireVariant.FreeCell,617);
        Assert.Equal(replay.Piles["C0"].Select(c => c.Card),
            game.Piles["C0"].Select(c => c.Card));
        Assert.Equal("card-emerald-house",JsonSerializer.Serialize(service.Snapshot()).Contains("card-emerald-house")
            ? "card-emerald-house" : "missing");

        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.back"))!,service.Execute).Success);
        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.new",SolitaireVariant.Klondike,1))!,service.Execute).Success);
        Guid id = service.CurrentGame!.GameId;
        int initialStock = service.CurrentGame.Piles["Stock"].Count;
        int initialMoves = service.CurrentGame.Moves;
        int committed = writes;
        fail = true;
        Assert.Throws<IOException>(()=>session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.draw"))!,service.Execute));
        Assert.Equal(id,service.CurrentGame.GameId);
        Assert.Equal(initialStock,service.CurrentGame.Piles["Stock"].Count);
        Assert.Equal(initialMoves,service.CurrentGame.Moves);
        Assert.Equal(initialStock,saved.ActiveGames["klondike"].Piles["Stock"].Count);
        Assert.Equal(committed,writes);
        fail = false;
        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.draw"))!,service.Execute).Success);
        Assert.Equal(initialStock-1,service.CurrentGame.Piles["Stock"].Count);
        Assert.Equal(1,service.CurrentGame.Moves);
        Assert.Equal(committed+1,writes);
    }

    [Fact]
    public void NewDealRecordsAbandonmentButLeavingForCabinetDoesNot()
    {
        SolitaireProgressState saved = new();
        var stats = new SolitaireStatistics();
        var service = Create(next=>saved=next,()=>stats,
            start=>stats.RecordStart(start), abandon=>stats.RecordAbandon(abandon), initial:saved);
        var session = new ModernSolitaireSession();
        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.new",SolitaireVariant.Klondike,1))!,service.Execute).Success);
        Guid old = service.CurrentGame!.GameId;
        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.back"))!,service.Execute).Success);
        Assert.Equal(0,stats.GamesAbandoned);
        Assert.True(session.Execute(ModernSolitaireProtocol.Parse(
            Command(session,"solitaire.new",SolitaireVariant.Klondike,1))!,service.Execute).Success);
        Assert.NotEqual(old,service.CurrentGame!.GameId);
        Assert.Equal(2,stats.GamesStarted);
        Assert.Equal(1,stats.GamesAbandoned);
    }

    private static ModernSolitaireService Create(Action<SolitaireProgressState> save,
        Func<SolitaireStatistics> stats, Action<SolitaireStartRecord> start,
        Action<SolitaireAbandonRecord> abandon, SolitaireProgressState initial) =>
        new(initial, save, stats, () => "card-emerald-house", start, abandon,
            _ => "Saved completion.");
}
