using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class BlackjackRoundTests
{
    [Fact]
    public void NaturalBlackjack_PaysThreeToTwo()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false }, 10m,
            C(Rank.Ace), C(Rank.Ten), C(Rank.King), C(Rank.Seven));

        round.Start();

        Assert.Equal(RoundPhase.Settled, round.Phase);
        Assert.Equal(HandOutcome.Blackjack, round.Result!.Hands[0].Outcome);
        Assert.Equal(15m, round.Result.TotalNet);
    }

    [Fact]
    public void DealerBlackjack_BeatsNonNaturalHand()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false }, 10m,
            C(Rank.Seven), C(Rank.Ace), C(Rank.Seven), C(Rank.King));

        round.Start();

        Assert.Equal(RoundPhase.Settled, round.Phase);
        Assert.Equal(HandOutcome.Loss, round.Result!.Hands[0].Outcome);
        Assert.Equal(-10m, round.Result.TotalNet);
    }

    [Fact]
    public void EqualTotals_Push()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false }, 10m,
            C(Rank.Ten), C(Rank.Ten), C(Rank.Eight), C(Rank.Eight));

        round.Start();
        round.Act(PlayerAction.Stand);

        Assert.Equal(HandOutcome.Push, round.Result!.Hands[0].Outcome);
        Assert.Equal(0m, round.Result.TotalNet);
    }

    [Fact]
    public void DoubleDown_DoublesBetAndDrawsExactlyOneCard()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false }, 10m,
            C(Rank.Five), C(Rank.Nine), C(Rank.Six), C(Rank.Seven), C(Rank.Ten), C(Rank.Queen));

        round.Start();
        round.Act(PlayerAction.DoubleDown);

        Assert.True(round.PlayerHands[0].IsDoubled);
        Assert.Equal(20m, round.PlayerHands[0].Bet);
        Assert.Equal(21, round.PlayerHands[0].Value.Total);
        Assert.Equal(20m, round.Result!.TotalNet);
    }

    [Fact]
    public void Split_CreatesTwoIndependentHandsWithMatchingBets()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false }, 25m,
            C(Rank.Eight), C(Rank.Ten), C(Rank.Eight), C(Rank.Six), C(Rank.Three), C(Rank.Two), C(Rank.Five));

        round.Start();
        round.Act(PlayerAction.Split);

        Assert.Equal(2, round.PlayerHands.Count);
        Assert.All(round.PlayerHands, hand => Assert.Equal(25m, hand.Bet));
        Assert.All(round.PlayerHands, hand => Assert.True(hand.IsSplitHand));
        Assert.Equal(11, round.PlayerHands[0].Value.Total);
        Assert.Equal(10, round.PlayerHands[1].Value.Total);
    }

    [Fact]
    public void Surrender_LosesHalfTheBet()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false, AllowLateSurrender = true }, 10m,
            C(Rank.Ten), C(Rank.Ten), C(Rank.Six), C(Rank.Seven));

        round.Start();
        round.Act(PlayerAction.Surrender);

        Assert.Equal(HandOutcome.Surrender, round.Result!.Hands[0].Outcome);
        Assert.Equal(-5m, round.Result.TotalNet);
    }

    [Fact]
    public void DealerHitsSoftSeventeen_WhenConfigured()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false, DealerHitsSoft17 = true }, 10m,
            C(Rank.Ten), C(Rank.Ace), C(Rank.Eight), C(Rank.Six), C(Rank.Four));

        round.Start();
        round.Act(PlayerAction.Stand);

        Assert.Equal(21, round.DealerHand.Value.Total);
        Assert.Equal(HandOutcome.Loss, round.Result!.Hands[0].Outcome);
    }

    [Fact]
    public void Insurance_OffsetsMainBetWhenDealerHasBlackjack()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = true }, 10m,
            C(Rank.Ten), C(Rank.Ace), C(Rank.Nine), C(Rank.King));

        round.Start();
        Assert.Equal(RoundPhase.InsuranceDecision, round.Phase);

        round.TakeInsurance();

        Assert.Equal(-10m, round.Result!.Hands[0].Net);
        Assert.Equal(10m, round.Result.InsuranceNet);
        Assert.Equal(0m, round.Result.TotalNet);
    }

    private static BlackjackRound RoundWithCards(GameRules rules, decimal bet, params Card[] cards) =>
        new(rules, new FixedShoe(cards), bet);

    private static Card C(Rank rank) => new(rank, Suit.Spades);

    private sealed class FixedShoe(IEnumerable<Card> cards) : ICardShoe
    {
        private readonly Queue<Card> cards = new(cards);

        public int Remaining => cards.Count;

        public Card Draw() => cards.Count > 0
            ? cards.Dequeue()
            : throw new InvalidOperationException("Fixed test shoe is empty.");
    }
}
