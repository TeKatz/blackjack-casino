namespace BlackjackCasino.Core;

public enum DoubleRule
{
    AnyTwo,
    NineToEleven,
    TenToEleven,
}

public sealed record GameRules
{
    public int DeckCount { get; init; } = 6;
    public bool DealerHitsSoft17 { get; init; }
    public bool DealerPeeksForBlackjack { get; init; } = true;
    public bool DealerTakesHoleCard { get; init; } = true;
    public bool AllowInsurance { get; init; } = true;
    public bool AllowLateSurrender { get; init; } = true;
    public DoubleRule DoubleRule { get; init; } = DoubleRule.AnyTwo;
    public bool DoubleAfterSplit { get; init; } = true;
    public bool ResplitAces { get; init; }
    public bool HitSplitAces { get; init; }
    public int MaxSplitHands { get; init; } = 4;
    public decimal BlackjackPayout { get; init; } = 1.5m;
    public double Penetration { get; init; } = 0.75;
    public decimal MinimumBet { get; init; } = 1m;
    public decimal MaximumBet { get; init; } = 1_000m;

    public bool IsDoubleAllowed(int total) => DoubleRule switch
    {
        DoubleRule.AnyTwo => true,
        DoubleRule.NineToEleven => total is >= 9 and <= 11,
        DoubleRule.TenToEleven => total is 10 or 11,
        _ => false,
    };

    public void Validate()
    {
        if (DeckCount is < 1 or > 8)
        {
            throw new InvalidOperationException("DeckCount must be between 1 and 8.");
        }

        if (MaxSplitHands is < 1 or > 8)
        {
            throw new InvalidOperationException("MaxSplitHands must be between 1 and 8.");
        }

        if (BlackjackPayout <= 0)
        {
            throw new InvalidOperationException("BlackjackPayout must be positive.");
        }

        if (Penetration is <= 0 or >= 1)
        {
            throw new InvalidOperationException("Penetration must be between 0 and 1 (exclusive).");
        }

        if (MinimumBet <= 0)
        {
            throw new InvalidOperationException("MinimumBet must be positive.");
        }

        if (MaximumBet < MinimumBet)
        {
            throw new InvalidOperationException("MaximumBet must be greater than or equal to MinimumBet.");
        }
    }
}
