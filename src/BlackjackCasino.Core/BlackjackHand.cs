namespace BlackjackCasino.Core;

public sealed class BlackjackHand
{
    private readonly List<Card> cards = [];

    internal BlackjackHand(decimal bet, bool isSplitHand = false, bool isSplitAces = false)
    {
        if (bet <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "Bet must be positive.");
        }

        Bet = bet;
        IsSplitHand = isSplitHand;
        IsSplitAces = isSplitAces;
    }

    public IReadOnlyList<Card> Cards => cards;

    public decimal Bet { get; internal set; }

    public bool IsSplitHand { get; }

    public bool IsSplitAces { get; }

    public bool IsDoubled { get; internal set; }

    public bool IsSurrendered { get; internal set; }

    public bool IsComplete { get; internal set; }

    public HandValue Value => HandEvaluator.Evaluate(cards);

    public bool IsNaturalBlackjack => !IsSplitHand && cards.Count == 2 && Value.Total == 21;

    internal void Add(Card card) => cards.Add(card);
}
