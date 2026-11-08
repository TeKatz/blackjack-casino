namespace BlackjackCasino.Core.WordSearch;

public enum WordSearchDifficulty
{
    Relaxed,
    Easy,
    Medium,
    Hard,
    Expert,
    Master,
}

public readonly record struct WordSearchCell(int Row, int Column);

public sealed record WordSearchPlacement(
    string Word,
    int StartRow,
    int StartColumn,
    int EndRow,
    int EndColumn)
{
    public IReadOnlyList<WordSearchCell> Cells()
    {
        int rowDelta = Math.Sign(EndRow - StartRow);
        int columnDelta = Math.Sign(EndColumn - StartColumn);
        int length = Math.Max(Math.Abs(EndRow - StartRow), Math.Abs(EndColumn - StartColumn)) + 1;
        return Enumerable.Range(0, length)
            .Select(i => new WordSearchCell(StartRow + i * rowDelta, StartColumn + i * columnDelta))
            .ToArray();
    }
}

public sealed record WordSearchPuzzle(
    string Id,
    WordSearchDifficulty Difficulty,
    string Theme,
    int Size,
    string Grid,
    IReadOnlyList<WordSearchPlacement> Placements)
{
    public char At(int row, int column)
    {
        if (row < 0 || row >= Size || column < 0 || column >= Size)
            throw new ArgumentOutOfRangeException();
        return Grid[row * Size + column];
    }
}

public sealed class WordSearchGameState
{
    public WordSearchPuzzle Puzzle { get; set; } = new("", WordSearchDifficulty.Relaxed, "", 0, "", []);
    public HashSet<string> FoundWords { get; set; } = new(StringComparer.Ordinal);
    public int HintsUsed { get; set; }
    public long ElapsedSeconds { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public bool Completed => Puzzle.Placements.Count > 0 && FoundWords.Count >= Puzzle.Placements.Count;

    public void Normalize()
    {
        FoundWords ??= new(StringComparer.Ordinal);
        FoundWords = FoundWords
            .Where(word => Puzzle.Placements.Any(p => string.Equals(p.Word, word, StringComparison.Ordinal)))
            .ToHashSet(StringComparer.Ordinal);
        HintsUsed = Math.Max(0, HintsUsed);
        ElapsedSeconds = Math.Max(0, ElapsedSeconds);
        if (!Completed) CompletedAt = null;
    }
}

public sealed record WordSearchCompletionRecord(
    string PuzzleId,
    WordSearchDifficulty Difficulty,
    int WordsFound,
    int HintsUsed,
    long ElapsedSeconds,
    DateTimeOffset CompletedAt);

/// <summary>One durable entry per distinct A-Z word, across all puzzles and languages.</summary>
public sealed record WordSearchDiscovery(
    string Word, string Theme, WordSearchDifficulty Difficulty,
    string PuzzleId, DateTimeOffset DiscoveredAt);

public sealed class WordSearchProgressState
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public WordSearchGameState? ActiveGame { get; set; }
    public List<WordSearchCompletionRecord> CompletedGames { get; set; } = [];

    // Additive, optional save field. Schema v1 continues to load older
    // installations and previous completion receipts unchanged.
    public List<WordSearchDiscovery> DiscoveredWords { get; set; } = [];

    public bool Discover(string word, WordSearchPuzzle puzzle, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        if (string.IsNullOrWhiteSpace(word) || word.Length is < 3 or > 13
            || !word.All(c => c is >= 'A' and <= 'Z')
            || !puzzle.Placements.Any(p => p.Word == word))
            throw new ArgumentException("Only a solved puzzle target may enter the album.", nameof(word));
        DiscoveredWords ??= [];
        if (DiscoveredWords.Any(entry => entry.Word == word)) return false;
        DiscoveredWords.Add(new(word, puzzle.Theme, puzzle.Difficulty, puzzle.Id,
            now ?? DateTimeOffset.UtcNow));
        return true;
    }

    public void Normalize()
    {
        if (SchemaVersion > CurrentSchemaVersion)
            throw new NotSupportedException($"Word Search save schema {SchemaVersion} is newer than supported schema {CurrentSchemaVersion}.");
        SchemaVersion = CurrentSchemaVersion;
        CompletedGames ??= [];
        CompletedGames = CompletedGames
            .Where(x => !string.IsNullOrWhiteSpace(x.PuzzleId))
            .GroupBy(x => x.PuzzleId, StringComparer.Ordinal)
            .Select(x => x.OrderByDescending(y => y.CompletedAt).First())
            .OrderBy(x => x.CompletedAt)
            .ToList();
        DiscoveredWords ??= [];
        DiscoveredWords = DiscoveredWords
            .Where(entry => entry is not null
                && entry.Word.Length is >= 3 and <= 13
                && entry.Word.All(c => c is >= 'A' and <= 'Z')
                && entry.Theme is { Length: > 0 and <= 100 }
                && entry.PuzzleId is { Length: > 0 and <= 120 }
                && Enum.IsDefined(entry.Difficulty))
            .GroupBy(entry => entry.Word, StringComparer.Ordinal)
            .Select(group => group.OrderBy(entry => entry.DiscoveredAt).First())
            .OrderBy(entry => entry.DiscoveredAt).ThenBy(entry => entry.Word, StringComparer.Ordinal)
            .ToList();
        ActiveGame?.Normalize();
        if (ActiveGame is { } saved)
        {
            // Earlier saves already know words from an unfinished grid. Import
            // only this verifiable subset, without guessing words in old
            // completed puzzles whose word lists were not persisted.
            foreach (string word in saved.FoundWords.OrderBy(x => x, StringComparer.Ordinal))
                Discover(word, saved.Puzzle, saved.StartedAt);
        }
        if (ActiveGame?.Completed == true) ActiveGame = null;
    }
}

public sealed class WordSearchStatistics
{
    public HashSet<string> RecordedPuzzleIds { get; set; } = new(StringComparer.Ordinal);
    public long CompletedPuzzles { get; set; }
    public long WordsFound { get; set; }
    public long NoHintCompletions { get; set; }
    public long ExpertCompleted { get; set; }
    public long MasterCompleted { get; set; }
    public long TotalSolveSeconds { get; set; }
    public long BestSolveSeconds { get; set; }

    public void Normalize()
    {
        RecordedPuzzleIds ??= new(StringComparer.Ordinal);
        RecordedPuzzleIds = RecordedPuzzleIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.Ordinal);
        CompletedPuzzles = Math.Max(0, CompletedPuzzles);
        WordsFound = Math.Max(0, WordsFound);
        NoHintCompletions = Math.Max(0, NoHintCompletions);
        ExpertCompleted = Math.Max(0, ExpertCompleted);
        MasterCompleted = Math.Max(0, MasterCompleted);
        TotalSolveSeconds = Math.Max(0, TotalSolveSeconds);
        BestSolveSeconds = Math.Max(0, BestSolveSeconds);
    }

    public bool Record(WordSearchCompletionRecord completion)
    {
        Normalize();
        if (!RecordedPuzzleIds.Add(completion.PuzzleId)) return false;
        CompletedPuzzles = checked(CompletedPuzzles + 1);
        WordsFound = checked(WordsFound + Math.Max(0, completion.WordsFound));
        if (completion.HintsUsed == 0) NoHintCompletions = checked(NoHintCompletions + 1);
        if (completion.Difficulty == WordSearchDifficulty.Expert) ExpertCompleted = checked(ExpertCompleted + 1);
        if (completion.Difficulty == WordSearchDifficulty.Master) MasterCompleted = checked(MasterCompleted + 1);
        long seconds = Math.Max(0, completion.ElapsedSeconds);
        TotalSolveSeconds = checked(TotalSolveSeconds + seconds);
        if (seconds > 0 && (BestSolveSeconds == 0 || seconds < BestSolveSeconds)) BestSolveSeconds = seconds;
        return true;
    }
}
