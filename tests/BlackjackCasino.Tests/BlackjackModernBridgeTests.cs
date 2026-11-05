using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class BlackjackModernBridgeTests
{
    private static string Message(BlackjackModernSession session, string action = "deal", string? value = null)
        => JsonSerializer.Serialize(new
        {
            version = 1, session = session.Id, expectedRevision = session.Revision,
            requestId = Guid.NewGuid().ToString("D"), action, value,
        });

    [Theory]
    [InlineData("https://midnight-manor.local/blackjack.html", true)]
    [InlineData("https://midnight-manor.local/blackjack.html?debug=1", false)]
    [InlineData("https://midnight-manor.local/lobby.html", false)]
    [InlineData("https://attacker.local/blackjack.html", false)]
    [InlineData("http://midnight-manor.local/blackjack.html", false)]
    [InlineData("https://midnight-manor.local:8888/blackjack.html", false)]
    public void AcceptsOnlyPackagedBlackjackOrigin(string origin, bool expected)
        => Assert.Equal(expected, BlackjackModernProtocol.Trusted(origin));

    [Fact]
    public void RejectsUnexpectedActionsAndInvalidNumericInputs()
    {
        var epoch = new BlackjackModernSession();
        Assert.NotNull(BlackjackModernProtocol.Parse(Message(epoch)));
        Assert.NotNull(BlackjackModernProtocol.Parse(Message(epoch, "bet.set", "25.50")));
        foreach (string bad in new[]
        {
            Message(epoch, "bet.set", "NaN"), Message(epoch, "bet.set", "-1"),
            Message(epoch, "bet.set", "1,5"), Message(epoch, "bet.set", "5001"),
            Message(epoch, "hit", "extra"), Message(epoch, "payout", "100"),
            Message(epoch, "mode.set", "admin"), Message(epoch, "seats.set", "6"),
            "[]", "invalid json", new string('x', 4097),
        })
            Assert.Null(BlackjackModernProtocol.Parse(bad));
    }

    [Fact]
    public void SameReceiptCannotMutateAHandTwiceAndStaleSessionsFail()
    {
        var epoch = new BlackjackModernSession();
        var command = BlackjackModernProtocol.Parse(Message(epoch))!;
        int calls = 0;
        (bool, string) Commit(BlackjackModernCommand _) { calls++; return (true, "Dealt"); }
        var accepted = epoch.Execute(command, Commit);
        Assert.True(accepted.Success);
        Assert.Equal(1, epoch.Revision);
        Assert.Equal(accepted, epoch.Execute(command, Commit));
        Assert.Equal(1, calls);
        Assert.False(epoch.Execute(command with { RequestId = Guid.NewGuid().ToString("D") }, Commit).Success);
        Assert.False(new BlackjackModernSession().Execute(command, Commit).Success);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ProjectionNeverExposesDealerHoleCardOrChangesRound()
    {
        var shoe = new FixedShoe(
            new Card(Rank.Ten, Suit.Spades), new Card(Rank.Six, Suit.Clubs),
            new Card(Rank.Eight, Suit.Hearts), new Card(Rank.King, Suit.Diamonds),
            new Card(Rank.Five, Suit.Clubs));
        var session = new CasinoSession(new GameRules(), 1000m, () => shoe);
        session.StartRound(25m);
        string before = JsonSerializer.Serialize(session.CurrentRound!.DealerHand.Cards);
        JsonElement running = Snapshot(session);
        JsonElement dealer = running.GetProperty("round").GetProperty("dealerCards");
        Assert.Equal("Six", dealer[0].GetProperty("rank").GetString());
        Assert.True(dealer[1].GetProperty("faceDown").GetBoolean());
        Assert.Equal(JsonValueKind.Null, dealer[1].GetProperty("rank").ValueKind);
        Assert.Equal(JsonValueKind.Null, dealer[1].GetProperty("suit").ValueKind);
        Assert.Equal(JsonValueKind.Null, running.GetProperty("round").GetProperty("dealerValue").ValueKind);
        Assert.Equal(before, JsonSerializer.Serialize(session.CurrentRound.DealerHand.Cards));
        session.Act(PlayerAction.Stand);
        JsonElement settled = Snapshot(session);
        Assert.Equal("King", settled.GetProperty("round").GetProperty("dealerCards")[1]
            .GetProperty("rank").GetString());
        Assert.Equal("Settled", settled.GetProperty("round").GetProperty("phase").GetString());
    }

    private static JsonElement Snapshot(CasinoSession session) =>
        JsonSerializer.SerializeToElement(BlackjackModernSnapshot.Create(session, 25m, false, 0,
            "Casual", "ClassicGreen", "card-classic", true, true, "Ready"),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    private sealed class FixedShoe(params Card[] values) : ICardShoe
    {
        private readonly Queue<Card> deck = new(values);
        public int Remaining => deck.Count;
        public Card Draw() => deck.Dequeue();
    }
}
