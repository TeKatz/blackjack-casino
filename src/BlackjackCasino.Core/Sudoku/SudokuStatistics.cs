namespace BlackjackCasino.Core.Sudoku;

public sealed record SudokuCompletionRecord(
    string PuzzleId,
    SudokuDifficulty Difficulty,
    SudokuPuzzlePool Pool,
    long ElapsedSeconds,
    int HintsUsed);

public sealed class SudokuStatistics
{
    public long CompletedPuzzles { get; set; }
    public long FreePlayCompleted { get; set; }
    public long DailyCompleted { get; set; }
    public long NoHintCompletions { get; set; }
    public long HintsUsed { get; set; }
    public long TotalSolveSeconds { get; set; }
    public long FastestSolveSeconds { get; set; }
    public long FastestNoHintSeconds { get; set; }
    public long RelaxedCompleted { get; set; }
    public long EasyCompleted { get; set; }
    public long MediumCompleted { get; set; }
    public long HardCompleted { get; set; }
    public long ExpertCompleted { get; set; }
    public long MasterCompleted { get; set; }
    public HashSet<string> RecordedPuzzleIds { get; set; } = new(StringComparer.Ordinal);

    public bool Record(SudokuCompletionRecord completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        if (string.IsNullOrWhiteSpace(completion.PuzzleId))
        {
            throw new ArgumentException("Puzzle ID is required.", nameof(completion));
        }

        RecordedPuzzleIds ??= new HashSet<string>(StringComparer.Ordinal);
        if (!RecordedPuzzleIds.Add(completion.PuzzleId))
        {
            return false;
        }

        long seconds = Math.Max(0L, completion.ElapsedSeconds);
        int hints = Math.Max(0, completion.HintsUsed);
        CompletedPuzzles++;
        if (completion.Pool == SudokuPuzzlePool.Daily) DailyCompleted++;
        else FreePlayCompleted++;
        if (hints == 0) NoHintCompletions++;
        HintsUsed += hints;
        TotalSolveSeconds += seconds;
        if (seconds > 0 && (FastestSolveSeconds == 0 || seconds < FastestSolveSeconds))
        {
            FastestSolveSeconds = seconds;
        }
        if (hints == 0 && seconds > 0 && (FastestNoHintSeconds == 0 || seconds < FastestNoHintSeconds))
        {
            FastestNoHintSeconds = seconds;
        }

        switch (completion.Difficulty)
        {
            case SudokuDifficulty.Relaxed: RelaxedCompleted++; break;
            case SudokuDifficulty.Easy: EasyCompleted++; break;
            case SudokuDifficulty.Medium: MediumCompleted++; break;
            case SudokuDifficulty.Hard: HardCompleted++; break;
            case SudokuDifficulty.Expert: ExpertCompleted++; break;
            case SudokuDifficulty.Master: MasterCompleted++; break;
        }
        return true;
    }
}
