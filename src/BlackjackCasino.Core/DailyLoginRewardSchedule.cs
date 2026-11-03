using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Core;

public sealed record DailyLoginRewardDefinition(
    decimal Bankroll,
    long CosmeticCoins,
    CollectionRarity? PackRarity = null);

public static class DailyLoginRewardSchedule
{
    private static readonly DailyLoginRewardDefinition[] Rewards =
    [
        new(1_000m, 40, CollectionRarity.Common),
        new(1_500m, 40),
        new(2_000m, 40),
        new(2_500m, 40, CollectionRarity.Rare),
        new(3_000m, 40),
        new(3_500m, 40),
        new(4_000m, 80, CollectionRarity.Epic),
    ];

    public static int CycleLength => Rewards.Length;

    public static DailyLoginRewardDefinition GetDefinition(int completedClaims)
    {
        if (completedClaims < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedClaims));
        }

        return Rewards[completedClaims % Rewards.Length];
    }

    public static decimal GetReward(int completedClaims) => GetDefinition(completedClaims).Bankroll;
}
