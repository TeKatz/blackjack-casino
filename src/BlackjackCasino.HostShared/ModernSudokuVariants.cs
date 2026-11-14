using System.Diagnostics;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.HostShared;

/// <summary>Owns every variant board, clock and hint; browser sends intentions only.</summary>
internal sealed class ModernSudokuVariants
{
    private SudokuVariantProgressState progress;
    private readonly Action<SudokuVariantProgressState> persist;
    private readonly Func<SudokuVariantStatistics> statistics;
    private readonly Func<string> rewardNotice;
    private readonly Stopwatch clock = new();
    private readonly Stack<(string Board, int[] Notes)> undo = new();
    private readonly Stack<(string Board, int[] Notes)> redo = new();
    private SudokuVariantPuzzle? puzzle;
    private string board = "";
    private int[] notes = [];
    private long elapsedBase;
    private long lastSavedSeconds;
    private int hintsUsed;
    private string notice = "";

    internal ModernSudokuVariants(SudokuVariantProgressState saved,
        Action<SudokuVariantProgressState> persist, Func<SudokuVariantStatistics> statistics,
        Func<string> rewardNotice)
    {
        progress = SudokuVariantProgressState.Normalize(saved);
        this.persist = persist;
        this.statistics = statistics;
        this.rewardNotice = rewardNotice;
    }

    internal bool InGame => puzzle is not null;
    internal long CurrentSeconds => elapsedBase + (long)Math.Floor(clock.Elapsed.TotalSeconds);

    internal object[] Entries()
    {
        string[] modes = ["x", "killer", "jigsaw", "samurai", "mini"];
        var result = new List<object>();
        foreach (string mode in modes)
            foreach (SudokuDifficulty difficulty in new[] { SudokuDifficulty.Easy, SudokuDifficulty.Medium, SudokuDifficulty.Hard })
                foreach (int size in mode == "mini" ? new[] { 4, 6 } : new[] { 9 })
                {
                    string key = SudokuVariantPuzzle.Key(mode, difficulty, size);
                    bool saved = progress.Games.TryGetValue(key, out SudokuVariantSavedGame? game) && !game.Completed;
                    result.Add(new
                    {
                        Key = key, Mode = mode, Difficulty = difficulty.ToString(), MiniSize = size,
                        CanResume = saved, ElapsedSeconds = saved ? game!.ElapsedSeconds : 0,
                        Completed = statistics().PerDifficulty.GetValueOrDefault(key),
                    });
                }
        return result.ToArray();
    }

    internal object? Snapshot()
    {
        if (puzzle is null) return null;
        SudokuVariantPuzzle current = puzzle;
        return new
        {
            current.Id, current.Mode, Difficulty = current.Difficulty.ToString(),
            current.MiniSize, current.Width, current.Digits, current.Givens,
            Board = board, Notes = notes,
            current.Regions,
            Cages = current.Cages.Select(c => new { c.Sum, c.Cells }).ToArray(),
            Incorrect = Enumerable.Range(0, board.Length)
                .Where(i => current.Active(i) && board[i] != '0' && board[i] != current.Solution[i]).ToArray(),
            Conflicts = Enumerable.Range(0, board.Length).Where(i => current.HasConflict(board, i)).ToArray(),
            ElapsedSeconds = CurrentSeconds, HintsUsed = hintsUsed,
            Completed = board == current.Solution,
            CanUndo = undo.Count > 0, CanRedo = redo.Count > 0,
            CompletionXp = VariantPlayerXp(current),
            CareerXp = SudokuCareer.ExperienceForVariant(current.Mode, current.Difficulty, current.MiniSize),
            Notice = notice, RewardNotice = rewardNotice(),
        };
    }

    internal (bool Success, string Message) Execute(ModernSudokuCommand command)
    {
        if (command.Action is "sudoku.variant.new" or "sudoku.variant.resume")
        {
            if (!SudokuVariantLibrary.TryParseKey(command.Value!, out string mode,
                out SudokuDifficulty difficulty, out int size))
                return (false, "Unknown Sudoku variant.");
            return command.Action == "sudoku.variant.new"
                ? New(mode, difficulty, size) : Resume(mode, difficulty, size);
        }
        if (command.Action == "sudoku.variant.back")
        {
            if (!InGame) return (false, "No variant board is open.");
            SaveElapsed();
            clock.Stop();
            puzzle = null;
            board = "";
            notes = [];
            undo.Clear(); redo.Clear();
            return (true, "Returned to variant selection.");
        }
        if (puzzle is null) return (false, "Choose a Sudoku variant first.");
        if (board == puzzle.Solution)
            return (false, "This puzzle is complete. Return to the variant menu.");

        int index = command.Index ?? -1;
        if (command.Action is "sudoku.variant.undo" or "sudoku.variant.redo")
        {
            Stack<(string Board, int[] Notes)> from =
                command.Action == "sudoku.variant.undo" ? undo : redo;
            Stack<(string Board, int[] Notes)> into =
                command.Action == "sudoku.variant.undo" ? redo : undo;
            if (from.Count == 0) return (false, "Nothing to undo or redo.");
            var previous = from.Peek();
            SaveCandidate(previous.Board, (int[])previous.Notes.Clone(), hintsUsed);
            into.Push((board, (int[])notes.Clone()));
            from.Pop();
            board = previous.Board;
            notes = (int[])previous.Notes.Clone();
            clock.Start();
            return (true, "Board restored.");
        }

        if (command.Action == "sudoku.variant.hint")
        {
            if (index < 0 || !puzzle.Active(index) || puzzle.IsGiven(index) ||
                board[index] == puzzle.Solution[index])
            {
                index = Enumerable.Range(0, board.Length)
                    .FirstOrDefault(i => puzzle.Active(i) && !puzzle.IsGiven(i) &&
                        board[i] != '0' && board[i] != puzzle.Solution[i], -1);
                if (index < 0)
                    index = Enumerable.Range(0, board.Length)
                        .FirstOrDefault(i => puzzle.Active(i) && !puzzle.IsGiven(i) && board[i] == '0', -1);
            }
            if (index < 0) return (false, "No hint is needed.");
            char[] candidate = board.ToCharArray();
            candidate[index] = puzzle.Solution[index];
            int[] newNotes = (int[])notes.Clone();
            newNotes[index] = 0;
            return Commit(new string(candidate), newNotes, hintsUsed + 1,
                "A correct digit was revealed.");
        }

        if (!puzzle.Active(index) || puzzle.IsGiven(index))
            return (false, "Select an editable cell.");
        char[] cells = board.ToCharArray();
        int[] marks = (int[])notes.Clone();
        switch (command.Action)
        {
            case "sudoku.variant.value":
                if (command.Digit is not int value || value < 1 || value > puzzle.Digits)
                    return (false, "That digit is unavailable.");
                cells[index] = (char)('0' + value);
                marks[index] = 0;
                break;
            case "sudoku.variant.note":
                if (command.Digit is not int digit || digit < 1 || digit > puzzle.Digits ||
                    cells[index] != '0')
                    return (false, "Pencil marks require an empty cell.");
                marks[index] ^= 1 << digit;
                break;
            case "sudoku.variant.erase":
                if (cells[index] != '0')
                {
                    cells[index] = '0';
                    marks[index] = 0;
                }
                else marks[index] = 0;
                break;
            default:
                return (false, "Unsupported variant Sudoku action.");
        }
        if (new string(cells) == board && marks.SequenceEqual(notes))
            return (false, "No change was needed.");
        return Commit(new string(cells), marks, hintsUsed, "Move saved.");
    }

    private (bool Success, string Message) New(string mode, SudokuDifficulty difficulty, int size)
    {
        string key = SudokuVariantPuzzle.Key(mode, difficulty, size);
        int sequence = progress.NextSequences.GetValueOrDefault(key);
        SudokuVariantPuzzle next = SudokuVariantLibrary.Create(mode, difficulty, size, sequence);
        var sequences = new Dictionary<string, int>(progress.NextSequences, StringComparer.Ordinal)
        {
            [key] = (sequence + 1) % 10000,
        };
        var games = new Dictionary<string, SudokuVariantSavedGame>(progress.Games, StringComparer.Ordinal);
        var newGame = new SudokuVariantSavedGame
        {
            PuzzleId = next.Id, Mode = mode, Difficulty = difficulty, MiniSize = size, Index = sequence,
            Board = next.Givens, Notes = new int[next.Givens.Length],
        };
        games[key] = newGame;
        SudokuVariantProgressState updated = progress with { Games = games, NextSequences = sequences };
        persist(updated);
        progress = updated;
        Activate(next, newGame);
        return (true, "New Sudoku variant started.");
    }

    private (bool Success, string Message) Resume(string mode, SudokuDifficulty difficulty, int size)
    {
        string key = SudokuVariantPuzzle.Key(mode, difficulty, size);
        if (!progress.Games.TryGetValue(key, out SudokuVariantSavedGame? save) || save.Completed)
            return (false, "No unfinished variant to resume.");
        SudokuVariantPuzzle next = SudokuVariantLibrary.Create(mode, difficulty, size, save.Index);
        if (next.Id != save.PuzzleId) return (false, "Saved puzzle could not be restored.");
        Activate(next, save);
        return (true, "Sudoku variant restored.");
    }

    private void Activate(SudokuVariantPuzzle next, SudokuVariantSavedGame save)
    {
        clock.Stop(); clock.Reset();
        puzzle = next;
        board = save.Board;
        notes = (int[])save.Notes.Clone();
        hintsUsed = save.HintsUsed;
        elapsedBase = save.ElapsedSeconds;
        lastSavedSeconds = elapsedBase;
        undo.Clear(); redo.Clear();
        notice = "Puzzle ready.";
        if (board != next.Solution) clock.Start();
    }

    private (bool Success, string Message) Commit(string candidate, int[] marks, int hints, string message)
    {
        SudokuVariantPuzzle current = puzzle!;
        SaveCandidate(candidate, marks, hints);
        undo.Push((board, (int[])notes.Clone()));
        redo.Clear();
        board = candidate;
        notes = marks;
        hintsUsed = hints;
        if (candidate == current.Solution)
        {
            clock.Stop();
            notice = "Sudoku variant solved! Career rewards are saved.";
        }
        else notice = current.HasConflict(candidate,
            Enumerable.Range(0, board.Length).FirstOrDefault(i => candidate[i] != current.Solution[i] &&
                current.Active(i) && candidate[i] != '0', -1))
            ? "A Sudoku rule is being broken." : message;
        return (true, notice);
    }

    private void SaveCandidate(string candidate, int[] marks, int hints)
    {
        SudokuVariantPuzzle current = puzzle!;
        string key = current.PuzzleKey;
        SudokuVariantSavedGame old = progress.Games[key];
        long seconds = CurrentSeconds;
        SudokuVariantSavedGame saved = old with
        {
            Board = candidate, Notes = (int[])marks.Clone(),
            ElapsedSeconds = seconds, HintsUsed = hints, Completed = candidate == current.Solution,
        };
        var games = new Dictionary<string, SudokuVariantSavedGame>(progress.Games, StringComparer.Ordinal)
        {
            [key] = saved,
        };
        var receipts = new List<SudokuVariantCompletion>(progress.CompletedReceipts);
        if (saved.Completed && !receipts.Any(r => r.PuzzleId == current.Id))
            receipts.Add(new SudokuVariantCompletion(current.Id, current.Mode, current.Difficulty,
                current.MiniSize, seconds, hints));
        SudokuVariantProgressState updated = progress with { Games = games, CompletedReceipts = receipts };
        persist(updated);
        progress = updated;
        lastSavedSeconds = seconds;
    }

    internal void CheckpointClock()
    {
        if (!InGame || !clock.IsRunning || CurrentSeconds - lastSavedSeconds < 15) return;
        SaveElapsed();
    }

    internal void SaveElapsed()
    {
        if (puzzle is null || CurrentSeconds <= lastSavedSeconds) return;
        SaveCandidate(board, notes, hintsUsed);
    }

    internal void Stop()
    {
        SaveElapsed();
        clock.Stop();
    }

    private static long VariantPlayerXp(SudokuVariantPuzzle puzzle)
    {
        long xp = SudokuProgression.PlayerExperience(puzzle.Difficulty);
        if (puzzle.Mode == "samurai") xp *= 2;
        if (puzzle.Mode == "mini") xp /= puzzle.MiniSize == 4 ? 4 : 2;
        if (puzzle.Mode == "killer") xp = xp * 3 / 2;
        return xp;
    }
}
