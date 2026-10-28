namespace BlackjackCasino.Core;

public sealed record GameRules
{
    public int DeckCount { get; init; } = 6;
    public bool DealerHitsSoft17 { get; init; }
    public bool DealerPeeksForBlackjack { get; init; } = true;
    public bool AllowInsurance { get; init; } = true;
    public bool AllowLateSurrender { get; init; } = true;
    public bool DoubleAfterSplit { get; init; } = true;
    public bool ResplitAces { get; init; }
    public bool HitSplitAces { get; init; }
    public int MaxSplitHands { get; init; } = 4;
    public decimal BlackjackPayout { get; init; } = 1.5m;
    public double Penetration { get; init; } = 0.75;

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
    }
}
