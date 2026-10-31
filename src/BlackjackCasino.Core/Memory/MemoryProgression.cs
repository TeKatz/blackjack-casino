namespace BlackjackCasino.Core.Memory;

/// <summary>Memory rewards completed boards only. Time and mistakes are statistics, never XP penalties.</summary>
public sealed class MemoryProgression(ProgressionService progression)
{
    public static long PlayerExperience(MemoryDifficulty difficulty) => difficulty switch
    {
        MemoryDifficulty.Easy => 100,
        MemoryDifficulty.Normal => 160,
        MemoryDifficulty.Hard => 260,
        MemoryDifficulty.Expert => 420,
        MemoryDifficulty.Master => 650,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    public static long SeasonExperience(MemoryDifficulty difficulty) => PlayerExperience(difficulty) / 2;

    public ProgressionAward Process(MemoryCompletionRecord completion)
    {
        long xp = PlayerExperience(completion.Difficulty);
        return LoungeProgression.GrantOnce(
            progression,
            () => progression.State.Memory.Record(completion),
            xp,
            SeasonExperience(completion.Difficulty));
    }
}
