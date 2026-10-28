namespace BlackjackCasino.Core;

public readonly record struct HandValue(int Total, bool IsSoft, bool IsBust)
{
    public bool IsTwentyOne => Total == 21 && !IsBust;
}

public static class HandEvaluator
{
    public static HandValue Evaluate(IEnumerable<Card> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        int total = 0;
        int aces = 0;

        foreach (Card card in cards)
        {
            total += card.HardValue;
            if (card.Rank == Rank.Ace)
            {
                aces++;
            }
        }

        bool soft = false;
        if (aces > 0 && total + 10 <= 21)
        {
            total += 10;
            soft = true;
        }

        return new HandValue(total, soft, total > 21);
    }
}
