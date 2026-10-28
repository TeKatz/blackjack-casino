using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class FullTableTests
{
    private static readonly GameRules Rules = new()
    {
        DeckCount = 6,
        DealerHitsSoft17 = false,
        AllowInsurance = true,
        AllowLateSurrender = true,
        DoubleAfterSplit = true,
        MaxSplitHands = 4,
    };

    [Fact]
    public void SoloSession_HasNoAiSeats()
    {
        var session = CreateSession([], 1_000m,
            C(Rank.Ten, Suit.Spades),
            C(Rank.Six, Suit.Clubs),
            C(Rank.Nine, Suit.Hearts),
            C(Rank.Ten, Suit.Diamonds));

        session.StartRound(100m);

        Assert.Empty(session.CurrentRound!.AiSeats);
        Assert.Equal(0, session.AiPlayerCount);
    }

    [Fact]
    public void FullTable_InitialDealUsesOneShoeInSeatOrder()
    {
        AiPlayerProfile[] ai =
        [
            new("Alex", 25m),
            new("Morgan", 50m),
        ];

        var session = CreateSession(ai, 1_000m,
            C(Rank.Ten, Suit.Spades),      // player card 1
            C(Rank.Five, Suit.Clubs),      // Alex card 1
            C(Rank.Ace, Suit.Hearts),      // Morgan card 1
            C(Rank.Six, Suit.Diamonds),    // dealer up card
            C(Rank.Nine, Suit.Spades),     // player card 2
            C(Rank.Six, Suit.Clubs),       // Alex card 2
            C(Rank.King, Suit.Hearts),     // Morgan card 2
            C(Rank.Ten, Suit.Diamonds));   // dealer hole card

        session.StartRound(100m);
        BlackjackRound round = session.CurrentRound!;

        Assert.Equal(Rank.Ten, round.PlayerHands[0].Cards[0].Rank);
        Assert.Equal(Rank.Nine, round.PlayerHands[0].Cards[1].Rank);
        Assert.Equal(Rank.Five, round.AiSeats[0].Hands[0].Cards[0].Rank);
        Assert.Equal(Rank.Six, round.AiSeats[0].Hands[0].Cards[1].Rank);
        Assert.Equal(Rank.Ace, round.AiSeats[1].Hands[0].Cards[0].Rank);
        Assert.Equal(Rank.King, round.AiSeats[1].Hands[0].Cards[1].Rank);
        Assert.Equal(Rank.Six, round.DealerHand.Cards[0].Rank);
        Assert.Equal(Rank.Ten, round.DealerHand.Cards[1].Rank);
    }

    [Fact]
    public void AiDecisionsConsumeTheSharedShoeBeforeDealerDraws()
    {
        AiPlayerProfile[] ai = [new("Alex", 25m)];

        var session = CreateSession(ai, 1_000m,
            C(Rank.Ten, Suit.Spades),      // player 1
            C(Rank.Five, Suit.Clubs),      // AI 1
            C(Rank.Six, Suit.Diamonds),    // dealer 1
            C(Rank.Ten, Suit.Hearts),      // player 2 -> 20
            C(Rank.Six, Suit.Clubs),       // AI 2 -> 11
            C(Rank.Ten, Suit.Diamonds),    // dealer 2 -> 16
            C(Rank.Ten, Suit.Clubs),       // AI doubles -> 21
            C(Rank.Five, Suit.Spades));    // dealer hits -> 21

        session.StartRound(100m);
        session.Act(PlayerAction.Stand);

        BlackjackRound round = session.CurrentRound!;
        BlackjackHand aiHand = round.AiSeats[0].Hands[0];

        Assert.True(aiHand.IsDoubled);
        Assert.Equal(50m, aiHand.Bet);
        Assert.Equal(3, aiHand.Cards.Count);
        Assert.Equal(Rank.Ten, aiHand.Cards[2].Rank);
        Assert.Equal(3, round.DealerHand.Cards.Count);
        Assert.Equal(Rank.Five, round.DealerHand.Cards[2].Rank);
        Assert.Equal(RoundPhase.Settled, round.Phase);
        Assert.Equal(900m, session.Bankroll);
    }

    [Fact]
    public void FullTable_SupportsFiveAiSeats()
    {
        AiPlayerProfile[] ai =
        [
            new("A", 10m), new("B", 10m), new("C", 10m), new("D", 10m), new("E", 10m),
        ];

        var cards = Enumerable.Repeat(C(Rank.Ten, Suit.Clubs), 14).ToArray();
        var session = CreateSession(ai, 1_000m, cards);

        session.StartRound(25m);

        Assert.Equal(5, session.CurrentRound!.AiSeats.Count);
    }

    private static CasinoSession CreateSession(
        IReadOnlyList<AiPlayerProfile> ai,
        decimal bankroll,
        params Card[] cards)
    {
        return new CasinoSession(Rules, bankroll, () => new TestShoe(cards), ai);
    }

    private static Card C(Rank rank, Suit suit) => new(rank, suit);

    private sealed class TestShoe(IEnumerable<Card> cards) : ICardShoe
    {
        private readonly Queue<Card> queue = new(cards);

        public int Remaining => queue.Count;

        public Card Draw() => queue.Count > 0
            ? queue.Dequeue()
            : throw new InvalidOperationException("Test shoe is empty.");
    }
}
