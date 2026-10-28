namespace BlackjackCasino.Core;

public static class ProgressionCurve
{
    public const int MaximumLevel = 100;

    public static long ExperienceForNextLevel(int level)
    {
        if (level < 0 || level >= MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        return 100L + (8L * level) + ((long)level * level / 5L);
    }

    public static long TotalExperienceToLevel(int level)
    {
        if (level < 0 || level > MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        long total = 0;
        for (int current = 0; current < level; current++)
        {
            total += ExperienceForNextLevel(current);
        }

        return total;
    }

    public static long CoinsForLevel(int level)
    {
        if (level <= 0 || level > MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        long reward = 25;
        if (level % 10 == 0)
        {
            reward += 100;
        }
        else if (level % 5 == 0)
        {
            reward += 25;
        }

        if (level == MaximumLevel)
        {
            reward += 400;
        }

        return reward;
    }
}
