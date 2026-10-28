using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class CasinoSessionTests
{
    private static readonly GameRules Rules = new()
    {
        DeckCount = 6,
        DealerHitsSoft17 = false,
        AllowInsurance = true,
        AllowLateSurrender = true,
    };

    [Fact]
    public void StartingRound_ReservesInitialBet()
    {
        var session = CreateSession(1_000m,
            C(Rank.Ten, Suit.Spades),
            C(Rank.Nine, Suit.Clubs),
            C(Rank.Seven, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds));

        session.StartRound(100m);

        Assert.Equal(900m, session.Bankroll);
        Assert.Equal(RoundPhase.PlayerTurn, session.CurrentRound!.Phase);
    }

    [Fact]
    public void Win_ReturnsStakeAndProfit()
    {
        var session = CreateSession(1_000m,
            C(Rank.Ten, Suit.Spades),
            C(Rank.Nine, Suit.Clubs),
            C(Rank.King, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds),
            C(Rank.Ten, Suit.Clubs));

        session.StartRound(100m);
        session.Act(PlayerAction.Stand);

        Assert.Equal(RoundPhase.Settled, session.CurrentRound!.Phase);
        Assert.Equal(1_100m, session.Bankroll);
    }

    [Fact]
    public void DoubleDown_ReservesSecondStakeAndPaysCorrectly()
    {
        var session = CreateSession(1_000m,
            C(Rank.Five, Suit.Spades),
            C(Rank.Six, Suit.Clubs),
            C(Rank.Six, Suit.Hearts),
            C(Rank.Ten, Suit.Diamonds),
            C(Rank.Ten, Suit.Spades),
            C(Rank.Ten, Suit.Clubs));

        session.StartRound(100m);
        session.Act(PlayerAction.DoubleDown);

        Assert.Equal(1_200m, session.Bankroll);
        Assert.True(session.CurrentRound!.PlayerHands[0].IsDoubled);
        Assert.Equal(200m, session.CurrentRound.PlayerHands[0].Bet);
    }

    [Fact]
    public void Split_IsUnavailableWithoutEnoughUncommittedBankroll()
    {
        var session = CreateSession(100m,
            C(Rank.Eight, Suit.Spades),
            C(Rank.Six, Suit.Clubs),
            C(Rank.Eight, Suit.Hearts),
            C(Rank.Ten, Suit.Diamonds));

        session.StartRound(100m);

        Assert.DoesNotContain(PlayerAction.Split, session.GetAvailableActions());
        Assert.DoesNotContain(PlayerAction.DoubleDown, session.GetAvailableActions());
    }

    [Fact]
    public void Blackjack_PaysThreeToTwo()
    {
        var session = CreateSession(1_000m,
            C(Rank.Ace, Suit.Spades),
            C(Rank.Nine, Suit.Clubs),
            C(Rank.King, Suit.Hearts),
            C(Rank.Seven, Suit.Diamonds));

        session.StartRound(100m);

        Assert.Equal(RoundPhase.Settled, session.CurrentRound!.Phase);
        Assert.Equal(1_150m, session.Bankroll);
    }

    [Fact]
    public void Insurance_ReservesWagerAndPaysTwoToOneProfit()
    {
        var session = CreateSession(1_000m,
            C(Rank.Ten, Suit.Spades),
            C(Rank.Ace, Suit.Clubs),
            C(Rank.Nine, Suit.Hearts),
            C(Rank.King, Suit.Diamonds));

        session.StartRound(100m);
        Assert.Equal(RoundPhase.InsuranceDecision, session.CurrentRound!.Phase);

        session.TakeInsurance();

        Assert.Equal(RoundPhase.Settled, session.CurrentRound.Phase);
        Assert.Equal(1_000m, session.Bankroll);
    }

    private static CasinoSession CreateSession(decimal bankroll, params Card[] cards)
    {
        return new CasinoSession(Rules, bankroll, () => new TestShoe(cards));
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
