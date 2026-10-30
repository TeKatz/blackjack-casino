namespace BlackjackCasino.Core.Sudoku;

public enum SudokuDifficulty
{
    Relaxed,
    Easy,
    Medium,
    Hard,
    Expert,
    Master,
}

public enum SudokuPuzzlePool
{
    Normal,
    Daily,
}

public sealed record SudokuPuzzle
{
    public SudokuPuzzle(string id, SudokuDifficulty difficulty, SudokuPuzzlePool pool, string givens, double rating = 0d)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Puzzle ID is required.", nameof(id));
        }
        SudokuSolver.ValidateGrid(givens, allowEmpty: true);
        if (givens.All(character => character is '0' or '.'))
        {
            throw new ArgumentException("Puzzle must contain at least one given.", nameof(givens));
        }

        Id = id;
        Difficulty = difficulty;
        Pool = pool;
        Givens = givens.Replace('.', '0');
        Rating = rating;
    }

    public string Id { get; }
    public SudokuDifficulty Difficulty { get; }
    public SudokuPuzzlePool Pool { get; }
    public string Givens { get; }
    public double Rating { get; }
}

public sealed record SudokuGameState
{
    public string PuzzleId { get; init; } = string.Empty;
    public string Board { get; init; } = string.Empty;
    public int[] Notes { get; init; } = new int[81];
    public long ElapsedSeconds { get; init; }
    public int HintsUsed { get; init; }
    public bool Completed { get; init; }

    public static SudokuGameState Create(SudokuPuzzle puzzle) => new()
    {
        PuzzleId = puzzle.Id,
        Board = puzzle.Givens,
    };

    public static SudokuGameState Normalize(SudokuPuzzle puzzle, SudokuGameState? state)
    {
        if (state is null || !string.Equals(state.PuzzleId, puzzle.Id, StringComparison.Ordinal))
        {
            return Create(puzzle);
        }

        string board;
        try
        {
            SudokuSolver.ValidateGrid(state.Board, allowEmpty: true);
            board = state.Board.Replace('.', '0');
        }
        catch (ArgumentException)
        {
            board = puzzle.Givens;
        }

        char[] cells = board.ToCharArray();
        for (int index = 0; index < 81; index++)
        {
            char given = puzzle.Givens[index];
            if (given != '0')
            {
                cells[index] = given;
            }
        }
        board = new string(cells);

        int[] notes = state.Notes is { Length: 81 }
            ? (int[])state.Notes.Clone()
            : new int[81];
        for (int index = 0; index < notes.Length; index++)
        {
            notes[index] &= 0x3FE;
            if (board[index] != '0')
            {
                notes[index] = 0;
            }
        }

        return new SudokuGameState
        {
            PuzzleId = puzzle.Id,
            Board = board,
            Notes = notes,
            ElapsedSeconds = Math.Max(0L, state.ElapsedSeconds),
            HintsUsed = Math.Max(0, state.HintsUsed),
            Completed = SudokuSolver.IsValidCompletedGrid(board),
        };
    }
}

public sealed record SudokuProgressState
{
    public Dictionary<string, SudokuGameState> Games { get; init; } = [];
    public List<string> CompletedPuzzleIds { get; init; } = [];
    public Dictionary<string, string> DailyAssignments { get; init; } = [];
    public string? ActiveNormalPuzzleId { get; init; }
    public string? ActiveDailyPuzzleId { get; init; }

    public static SudokuProgressState Normalize(SudokuProgressState? state)
    {
        if (state is null)
        {
            return new SudokuProgressState();
        }

        var games = new Dictionary<string, SudokuGameState>(StringComparer.Ordinal);
        foreach ((string id, SudokuGameState raw) in state.Games ?? [])
        {
            if (SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle))
            {
                games[id] = SudokuGameState.Normalize(puzzle, raw);
            }
        }

        var completed = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in state.CompletedPuzzleIds ?? [])
        {
            if (SudokuLibrary.TryGet(id, out _))
            {
                completed.Add(id);
            }
        }
        foreach ((string id, SudokuGameState game) in games)
        {
            if (game.Completed)
            {
                completed.Add(id);
            }
        }

        var assignments = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string date, string id) in state.DailyAssignments ?? [])
        {
            if (DateOnly.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _) &&
                SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) &&
                puzzle.Pool == SudokuPuzzlePool.Daily)
            {
                assignments[date] = id;
            }
        }

        string? activeNormal = IsPool(state.ActiveNormalPuzzleId, SudokuPuzzlePool.Normal)
            ? state.ActiveNormalPuzzleId
            : null;
        string? activeDaily = IsPool(state.ActiveDailyPuzzleId, SudokuPuzzlePool.Daily)
            ? state.ActiveDailyPuzzleId
            : null;

        return new SudokuProgressState
        {
            Games = games,
            CompletedPuzzleIds = completed.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            DailyAssignments = assignments,
            ActiveNormalPuzzleId = activeNormal,
            ActiveDailyPuzzleId = activeDaily,
        };
    }

    public SudokuProgressState SaveGame(SudokuPuzzle puzzle, SudokuGameState game, DateOnly? dailyDate = null)
    {
        SudokuProgressState normalized = Normalize(this);
        SudokuGameState clean = SudokuGameState.Normalize(puzzle, game);
        var games = new Dictionary<string, SudokuGameState>(normalized.Games, StringComparer.Ordinal)
        {
            [puzzle.Id] = clean,
        };
        var completed = new HashSet<string>(normalized.CompletedPuzzleIds, StringComparer.Ordinal);
        if (clean.Completed)
        {
            completed.Add(puzzle.Id);
        }

        var assignments = new Dictionary<string, string>(normalized.DailyAssignments, StringComparer.Ordinal);
        if (puzzle.Pool == SudokuPuzzlePool.Daily && dailyDate is DateOnly date)
        {
            assignments[DateKey(date)] = puzzle.Id;
        }

        return normalized with
        {
            Games = games,
            CompletedPuzzleIds = completed.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            DailyAssignments = assignments,
            ActiveNormalPuzzleId = puzzle.Pool == SudokuPuzzlePool.Normal ? puzzle.Id : normalized.ActiveNormalPuzzleId,
            ActiveDailyPuzzleId = puzzle.Pool == SudokuPuzzlePool.Daily ? puzzle.Id : normalized.ActiveDailyPuzzleId,
        };
    }

    public SudokuProgressState AssignDaily(DateOnly date, SudokuPuzzle puzzle)
    {
        if (puzzle.Pool != SudokuPuzzlePool.Daily)
        {
            throw new ArgumentException("Daily assignments must use the Daily puzzle pool.", nameof(puzzle));
        }

        SudokuProgressState normalized = Normalize(this);
        var assignments = new Dictionary<string, string>(normalized.DailyAssignments, StringComparer.Ordinal)
        {
            [DateKey(date)] = puzzle.Id,
        };
        return normalized with
        {
            DailyAssignments = assignments,
            ActiveDailyPuzzleId = puzzle.Id,
        };
    }

    public bool TryGetDaily(DateOnly date, out SudokuPuzzle? puzzle)
    {
        SudokuProgressState normalized = Normalize(this);
        if (normalized.DailyAssignments.TryGetValue(DateKey(date), out string? id) &&
            SudokuLibrary.TryGet(id, out puzzle) &&
            puzzle.Pool == SudokuPuzzlePool.Daily)
        {
            return true;
        }

        puzzle = null;
        return false;
    }

    private static bool IsPool(string? id, SudokuPuzzlePool pool) =>
        id is not null && SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle) && puzzle.Pool == pool;

    private static string DateKey(DateOnly date) => date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
