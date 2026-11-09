namespace BlackjackCasino.Core.Sudoku;

public sealed record SudokuVariantCompletion(
    string PuzzleId, string Mode, SudokuDifficulty Difficulty, int MiniSize,
    long ElapsedSeconds, int HintsUsed);

/// <summary>Durable receipts for the five additional Sudoku modes.</summary>
public sealed class SudokuVariantStatistics
{
    public long CompletedPuzzles { get; set; }
    public long NoHintCompletions { get; set; }
    public long HintsUsed { get; set; }
    public long TotalSolveSeconds { get; set; }
    public long FastestSolveSeconds { get; set; }
    public long ExpertLevelCompleted { get; set; }
    public long XCompleted { get; set; }
    public long KillerCompleted { get; set; }
    public long JigsawCompleted { get; set; }
    public long SamuraiCompleted { get; set; }
    public long MiniCompleted { get; set; }
    public long Mini4Completed { get; set; }
    public long Mini6Completed { get; set; }
    public Dictionary<string, long> PerDifficulty { get; set; } = new(StringComparer.Ordinal);
    public HashSet<string> RecordedPuzzleIds { get; set; } = new(StringComparer.Ordinal);

    public void Normalize()
    {
        RecordedPuzzleIds ??= new(StringComparer.Ordinal);
        PerDifficulty ??= new(StringComparer.Ordinal);
        foreach (string key in PerDifficulty.Keys.ToArray())
            PerDifficulty[key] = Math.Max(0L, PerDifficulty[key]);
    }

    public long ForMode(string mode) => mode switch
    {
        "x" => XCompleted, "killer" => KillerCompleted, "jigsaw" => JigsawCompleted,
        "samurai" => SamuraiCompleted, "mini" => MiniCompleted, _ => 0L,
    };

    public bool Record(SudokuVariantCompletion result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(result.PuzzleId) ||
            !SudokuVariantLibrary.Supports(result.Mode, result.Difficulty, result.MiniSize))
            throw new ArgumentException("Invalid completed variant Sudoku.", nameof(result));
        Normalize();
        if (!RecordedPuzzleIds.Add(result.PuzzleId)) return false;
        CompletedPuzzles++;
        if (result.HintsUsed == 0) NoHintCompletions++;
        HintsUsed += Math.Max(0, result.HintsUsed);
        long seconds = Math.Max(0, result.ElapsedSeconds);
        TotalSolveSeconds += seconds;
        if (seconds > 0 && (FastestSolveSeconds == 0 || seconds < FastestSolveSeconds))
            FastestSolveSeconds = seconds;
        if (result.Difficulty == SudokuDifficulty.Hard) ExpertLevelCompleted++;
        switch (result.Mode)
        {
            case "x": XCompleted++; break;
            case "killer": KillerCompleted++; break;
            case "jigsaw": JigsawCompleted++; break;
            case "samurai": SamuraiCompleted++; break;
            case "mini":
                MiniCompleted++;
                if (result.MiniSize == 4) Mini4Completed++; else Mini6Completed++;
                break;
        }
        string key = SudokuVariantPuzzle.Key(result.Mode, result.Difficulty, result.MiniSize);
        PerDifficulty[key] = PerDifficulty.GetValueOrDefault(key) + 1;
        return true;
    }
}

public sealed class SudokuVariantProgression(ProgressionService progression)
{
    public ProgressionAward Process(SudokuVariantCompletion result)
    {
        long xp = SudokuProgression.PlayerExperience(result.Difficulty);
        if (result.Mode == "samurai") xp = checked(xp * 2);
        if (result.Mode == "mini") xp /= result.MiniSize == 4 ? 4 : 2;
        if (result.Mode == "killer") xp = checked(xp * 3 / 2);
        return LoungeProgression.GrantOnce(progression, () =>
        {
            if (!progression.State.SudokuVariants.Record(result)) return false;
            SudokuCareer.GrantVariant(progression.State, result.Mode, result.Difficulty, result.MiniSize);
            return true;
        }, xp, xp / 2);
    }
}
