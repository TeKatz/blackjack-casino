namespace BlackjackCasino.Core;

public static class DailyLoginRewardSchedule
{
    private static readonly decimal[] Rewards =
    [
        100m,
        125m,
        150m,
        175m,
        200m,
        250m,
        500m,
    ];

    public static int CycleLength => Rewards.Length;

    public static decimal GetReward(int completedClaims)
    {
        if (completedClaims < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedClaims));
        }

        return Rewards[completedClaims % Rewards.Length];
    }
}
