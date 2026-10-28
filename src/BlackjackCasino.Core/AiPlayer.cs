namespace BlackjackCasino.Core;

public sealed record AiPlayerProfile
{
    public AiPlayerProfile(string name, decimal bet)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (bet <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bet), "AI bet must be positive.");
        }

        Name = name.Trim();
        Bet = bet;
    }

    public string Name { get; }

    public decimal Bet { get; }
}

public sealed class AiSeatState
{
    private readonly List<BlackjackHand> hands;

    internal AiSeatState(AiPlayerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Name = profile.Name;
        InitialBet = profile.Bet;
        hands = [new BlackjackHand(profile.Bet)];
    }

    public string Name { get; }

    public decimal InitialBet { get; }

    public IReadOnlyList<BlackjackHand> Hands => hands;

    public IReadOnlyList<HandResult> Results { get; internal set; } = [];

    public decimal TotalNet => Results.Sum(result => result.Net);

    internal List<BlackjackHand> MutableHands => hands;
}
