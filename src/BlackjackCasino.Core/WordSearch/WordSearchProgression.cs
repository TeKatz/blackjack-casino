namespace BlackjackCasino.Core.WordSearch;

/// <summary>One reward per authored puzzle. Hints and solve time never reduce XP in the Chill Lounge.</summary>
public sealed class WordSearchProgression(ProgressionService progression)
{
    public static long PlayerExperience(WordSearchDifficulty difficulty) => difficulty switch
    {
        WordSearchDifficulty.Relaxed => 100,
        WordSearchDifficulty.Easy => 160,
        WordSearchDifficulty.Medium => 240,
        WordSearchDifficulty.Hard => 350,
        WordSearchDifficulty.Expert => 500,
        WordSearchDifficulty.Master => 700,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    public static long SeasonExperience(WordSearchDifficulty difficulty) => PlayerExperience(difficulty) / 2;

    public ProgressionAward Process(WordSearchCompletionRecord completion)
    {
        long xp = PlayerExperience(completion.Difficulty);
        return LoungeProgression.GrantOnce(
            progression,
            () => progression.State.WordSearch.Record(completion),
            xp,
            SeasonExperience(completion.Difficulty));
    }
}
