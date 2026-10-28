using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class TableRulesTests
{
    [Fact]
    public void CasualPreset_RemainsForgivingThreeToTwoTable()
    {
        GameRules rules = TablePresets.Get(TablePresetId.Casual).Rules;

        Assert.Equal(6, rules.DeckCount);
        Assert.False(rules.DealerHitsSoft17);
        Assert.Equal(1.5m, rules.BlackjackPayout);
        Assert.Equal(DoubleRule.AnyTwo, rules.DoubleRule);
        Assert.True(rules.AllowLateSurrender);
        Assert.True(rules.DealerTakesHoleCard);
    }

    [Fact]
    public void SixToFivePayout_IsAppliedFromRules()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false, BlackjackPayout = 1.2m }, 10m,
            C(Rank.Ace), C(Rank.Ten), C(Rank.King), C(Rank.Seven));

        round.Start();

        Assert.Equal(12m, round.Result!.TotalNet);
    }

    [Fact]
    public void DoubleRestriction_RemovesDoubleWhenTotalIsOutsideRange()
    {
        var round = RoundWithCards(new GameRules { AllowInsurance = false, DoubleRule = DoubleRule.TenToEleven }, 10m,
            C(Rank.Five), C(Rank.Nine), C(Rank.Four), C(Rank.Seven));

        round.Start();

        Assert.DoesNotContain(PlayerAction.DoubleDown, round.GetAvailableActions());
    }

    [Fact]
    public void EuropeanNoHoleCard_DrawsDealerSecondCardAfterPlayerAction()
    {
        var rules = new GameRules
        {
            AllowInsurance = false,
            DealerTakesHoleCard = false,
            DealerPeeksForBlackjack = false,
        };
        var round = RoundWithCards(rules, 10m,
            C(Rank.Ten), C(Rank.Ace), C(Rank.Six), C(Rank.Five), C(Rank.King));

        round.Start();

        Assert.Single(round.DealerHand.Cards);
        round.Act(PlayerAction.Hit);

        Assert.Equal(21, round.PlayerHands[0].Value.Total);
        Assert.Equal(2, round.DealerHand.Cards.Count);
        Assert.True(round.DealerHand.IsNaturalBlackjack);
        Assert.Equal(HandOutcome.Loss, round.Result!.Hands[0].Outcome);
    }

    [Fact]
    public void NoPeekTable_DoesNotOfferLateSurrenderAgainstTenValueUpCard()
    {
        var rules = new GameRules
        {
            AllowInsurance = false,
            AllowLateSurrender = true,
            DealerPeeksForBlackjack = false,
            DealerTakesHoleCard = false,
        };
        var round = RoundWithCards(rules, 10m,
            C(Rank.Ten), C(Rank.King), C(Rank.Six));

        round.Start();

        Assert.DoesNotContain(PlayerAction.Surrender, round.GetAvailableActions());
    }

    [Fact]
    public void Session_EnforcesConfiguredTableLimits()
    {
        var rules = new GameRules { MinimumBet = 10m, MaximumBet = 100m };
        var session = new CasinoSession(rules, 1_000m, () => new InfiniteShoe());

        Assert.Throws<ArgumentOutOfRangeException>(() => session.StartRound(5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.StartRound(101m));
    }

    [Fact]
    public void CompletedRound_IsCapturedInSessionHistory()
    {
        var rules = new GameRules { AllowInsurance = false };
        var cards = new[] { C(Rank.Ten), C(Rank.Ten), C(Rank.Eight), C(Rank.Seven) };
        var session = new CasinoSession(rules, 1_000m, () => new FixedShoe(cards));

        session.StartRound(25m);
        session.Act(PlayerAction.Stand);

        RoundHistoryEntry entry = Assert.Single(session.History);
        Assert.Equal(1_000m, entry.BankrollBefore);
        Assert.Equal(session.Bankroll, entry.BankrollAfter);
        Assert.Equal(25m, entry.InitialBet);
        Assert.Single(entry.Actions);
        Assert.Equal(PlayerAction.Stand, entry.Actions[0].Action);
        Assert.Equal(2, entry.DealerCards.Count);
    }

    private static BlackjackRound RoundWithCards(GameRules rules, decimal bet, params Card[] cards) =>
        new(rules, new FixedShoe(cards), bet);

    private static Card C(Rank rank) => new(rank, Suit.Spades);

    private sealed class FixedShoe(IEnumerable<Card> cards) : ICardShoe
    {
        private readonly Queue<Card> cards = new(cards);
        public int Remaining => cards.Count;
        public Card Draw() => cards.Count > 0 ? cards.Dequeue() : throw new InvalidOperationException("Fixed test shoe is empty.");
    }

    private sealed class InfiniteShoe : ICardShoe
    {
        public int Remaining => 312;
        public Card Draw() => new(Rank.Ten, Suit.Spades);
    }
}
