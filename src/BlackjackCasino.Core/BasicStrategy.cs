namespace BlackjackCasino.Core;

/// <summary>
/// Deterministic multi-deck S17/DAS-oriented basic strategy used by table AI.
/// It never takes insurance and only returns actions that are currently legal.
/// </summary>
public static class BasicStrategy
{
    public static PlayerAction Decide(
        BlackjackHand hand,
        Card dealerUpCard,
        IReadOnlySet<PlayerAction> availableActions)
    {
        ArgumentNullException.ThrowIfNull(hand);
        ArgumentNullException.ThrowIfNull(availableActions);

        if (availableActions.Count == 0)
        {
            throw new InvalidOperationException("Basic strategy requires at least one legal action.");
        }

        int dealer = DealerValue(dealerUpCard);

        if (availableActions.Contains(PlayerAction.Split)
            && hand.Cards.Count == 2
            && hand.Cards[0].Rank == hand.Cards[1].Rank
            && ShouldSplit(hand.Cards[0].Rank, dealer))
        {
            return PlayerAction.Split;
        }

        HandValue value = hand.Value;

        if (availableActions.Contains(PlayerAction.Surrender)
            && ((value.Total == 16 && dealer >= 9)
                || (value.Total == 15 && dealer == 10)))
        {
            return PlayerAction.Surrender;
        }

        if (value.IsSoft)
        {
            return DecideSoft(value.Total, dealer, availableActions);
        }

        return DecideHard(value.Total, dealer, availableActions);
    }

    private static PlayerAction DecideSoft(int total, int dealer, IReadOnlySet<PlayerAction> available)
    {
        if (total >= 19)
        {
            return StandOrFallback(available);
        }

        if (total == 18)
        {
            if (dealer is >= 3 and <= 6 && available.Contains(PlayerAction.DoubleDown))
            {
                return PlayerAction.DoubleDown;
            }

            return dealer is 2 or 7 or 8
                ? StandOrFallback(available)
                : HitOrFallback(available);
        }

        bool shouldDouble = total switch
        {
            17 => dealer is >= 3 and <= 6,
            15 or 16 => dealer is >= 4 and <= 6,
            13 or 14 => dealer is >= 5 and <= 6,
            _ => false,
        };

        if (shouldDouble && available.Contains(PlayerAction.DoubleDown))
        {
            return PlayerAction.DoubleDown;
        }

        return HitOrFallback(available);
    }

    private static PlayerAction DecideHard(int total, int dealer, IReadOnlySet<PlayerAction> available)
    {
        if (total >= 17)
        {
            return StandOrFallback(available);
        }

        if (total is >= 13 and <= 16)
        {
            return dealer is >= 2 and <= 6
                ? StandOrFallback(available)
                : HitOrFallback(available);
        }

        if (total == 12)
        {
            return dealer is >= 4 and <= 6
                ? StandOrFallback(available)
                : HitOrFallback(available);
        }

        bool shouldDouble = total switch
        {
            11 => dealer is >= 2 and <= 10,
            10 => dealer is >= 2 and <= 9,
            9 => dealer is >= 3 and <= 6,
            _ => false,
        };

        if (shouldDouble && available.Contains(PlayerAction.DoubleDown))
        {
            return PlayerAction.DoubleDown;
        }

        return HitOrFallback(available);
    }

    private static bool ShouldSplit(Rank rank, int dealer) => rank switch
    {
        Rank.Ace or Rank.Eight => true,
        Rank.Nine => dealer is >= 2 and <= 6 or 8 or 9,
        Rank.Seven => dealer is >= 2 and <= 7,
        Rank.Six => dealer is >= 2 and <= 6,
        Rank.Four => dealer is 5 or 6,
        Rank.Two or Rank.Three => dealer is >= 2 and <= 7,
        _ => false,
    };

    private static int DealerValue(Card card) => card.Rank == Rank.Ace ? 11 : card.HardValue;

    private static PlayerAction HitOrFallback(IReadOnlySet<PlayerAction> available)
    {
        if (available.Contains(PlayerAction.Hit))
        {
            return PlayerAction.Hit;
        }

        return StandOrFallback(available);
    }

    private static PlayerAction StandOrFallback(IReadOnlySet<PlayerAction> available)
    {
        if (available.Contains(PlayerAction.Stand))
        {
            return PlayerAction.Stand;
        }

        return available.First();
    }
}
