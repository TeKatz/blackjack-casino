using BlackjackCasino.Core;

namespace BlackjackCasino.Tests;

public sealed class HandEvaluatorTests
{
    [Fact]
    public void Ace_IsEleven_WhenItDoesNotBustTheHand()
    {
        HandValue value = HandEvaluator.Evaluate([
            new Card(Rank.Ace, Suit.Spades),
            new Card(Rank.Six, Suit.Hearts),
        ]);

        Assert.Equal(17, value.Total);
        Assert.True(value.IsSoft);
        Assert.False(value.IsBust);
    }

    [Fact]
    public void MultipleAces_AreReducedAsNeeded()
    {
        HandValue value = HandEvaluator.Evaluate([
            new Card(Rank.Ace, Suit.Spades),
            new Card(Rank.Ace, Suit.Hearts),
            new Card(Rank.Nine, Suit.Clubs),
        ]);

        Assert.Equal(21, value.Total);
        Assert.True(value.IsSoft);
    }

    [Fact]
    public void Bust_IsDetected()
    {
        HandValue value = HandEvaluator.Evaluate([
            new Card(Rank.King, Suit.Spades),
            new Card(Rank.Queen, Suit.Hearts),
            new Card(Rank.Two, Suit.Clubs),
        ]);

        Assert.Equal(22, value.Total);
        Assert.True(value.IsBust);
    }
}
