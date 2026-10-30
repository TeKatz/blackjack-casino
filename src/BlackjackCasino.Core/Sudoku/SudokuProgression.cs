namespace BlackjackCasino.Core.Sudoku;

/// <summary>One durable receipt per puzzle, with rewards independent of hints and solve time.</summary>
public sealed class SudokuProgression(ProgressionService progression)
{
    public static long PlayerExperience(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Relaxed => 150,
        SudokuDifficulty.Easy => 250,
        SudokuDifficulty.Medium => 400,
        SudokuDifficulty.Hard => 600,
        SudokuDifficulty.Expert => 850,
        SudokuDifficulty.Master => 1200,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };
    public static long SeasonExperience(SudokuDifficulty difficulty) => PlayerExperience(difficulty) / 2;

    public ProgressionAward Process(SudokuCompletionRecord completion)
    {
        long xp = PlayerExperience(completion.Difficulty);
        if (!progression.State.Sudoku.Record(completion)) return ProgressionAward.None;
        long seasonXp = SeasonExperience(completion.Difficulty);
        ProgressionAward player = progression.GrantPlayerExperience(xp);
        ProgressionAward season = progression.GrantSeasonExperience(progression.State.ActiveSeasonId, seasonXp);
        progression.State.LastPlayedAt = DateTimeOffset.UtcNow;
        return new(xp, seasonXp, player.LevelsGained, season.SeasonTiersGained,
            player.CoinsEarned + season.CoinsEarned, player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());
    }
}
