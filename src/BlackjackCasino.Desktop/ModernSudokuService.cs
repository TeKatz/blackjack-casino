using System.Diagnostics;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Sudoku application boundary shared by the React game and the existing C# save
/// and reward services. A candidate board is persisted before replacing live state.
/// The old game schema is never changed and the browser cannot pick puzzle IDs.
/// </summary>
internal sealed class ModernSudokuService
{
    private SudokuProgressState progress;
    private readonly Action<SudokuProgressState> persist;
    private readonly Func<bool> timerVisible;
    private readonly Action<bool> setTimerVisible;
    private readonly Func<bool> guidesVisible;
    private readonly Action<bool> setGuidesVisible;
    private readonly Func<string> rewardNotice;
    private readonly Stack<SudokuBoardFrame> undo = new();
    private readonly Stack<SudokuBoardFrame> redo = new();
    private readonly Stopwatch elapsed = new();
    private SudokuPuzzle? puzzle;
    private SudokuBoard? board;
    private DateOnly? dailyDate;
    private long elapsedBase, lastPersistedSeconds;
    private string notice = "";

    internal ModernSudokuService(SudokuProgressState saved, Action<SudokuProgressState> persist,
        Func<bool> timerVisible, Action<bool> setTimerVisible,
        Func<bool> guidesVisible, Action<bool> setGuidesVisible,
        Func<string> rewardNotice)
    {
        progress = SudokuProgressState.Normalize(saved);
        this.persist = persist;
        this.timerVisible = timerVisible;
        this.setTimerVisible = setTimerVisible;
        this.guidesVisible = guidesVisible;
        this.setGuidesVisible = setGuidesVisible;
        this.rewardNotice = rewardNotice;
    }

    internal bool InGame => board is not null;
    internal string? PuzzleId => puzzle?.Id;
    internal long CurrentSeconds => elapsedBase + (long)Math.Floor(elapsed.Elapsed.TotalSeconds);

    internal object Snapshot()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        SudokuPuzzle daily = progress.TryGetDaily(today, out SudokuPuzzle? assigned) && assigned is not null
            ? assigned : SudokuDailySchedule.ForDate(today);

        var entries = Enum.GetValues<SudokuDifficulty>().Select(difficulty =>
        {
            bool resumable = TryResume(difficulty, out SudokuPuzzle? found, out SudokuGameState? state);
            return new
            {
                Id = difficulty.ToString(), CanResume = resumable,
                ElapsedSeconds = resumable ? state!.ElapsedSeconds : 0L,
                Completed = progress.CompletedPuzzleIds.Count(id =>
                    SudokuLibrary.TryGet(id, out SudokuPuzzle? p) &&
                    p.Pool == SudokuPuzzlePool.Normal && p.Difficulty == difficulty),
            };
        }).ToArray();

        object? game = null;
        if (puzzle is not null && board is not null)
        {
            SudokuBoard current = board;
            game = new
            {
                PuzzleId = puzzle.Id,
                Difficulty = puzzle.Difficulty.ToString(),
                IsDaily = puzzle.Pool == SudokuPuzzlePool.Daily,
                Givens = puzzle.Givens,
                Board = current.CurrentGrid,
                Notes = Enumerable.Range(0, 81).Select(current.NotesAt).ToArray(),
                Incorrect = Enumerable.Range(0, 81).Where(current.IsIncorrectAt).ToArray(),
                Conflicts = Enumerable.Range(0, 81).Where(current.HasConflictAt).ToArray(),
                CompletedDigits = Enumerable.Range(1, 9).Where(current.IsDigitComplete).ToArray(),
                ElapsedSeconds = CurrentSeconds, HintsUsed = current.HintsUsed,
                Completed = current.IsComplete, CanUndo = undo.Count > 0,
                CanRedo = redo.Count > 0,
                CompletionXp = SudokuProgression.PlayerExperience(puzzle.Difficulty),
                CompletionSeasonXp = SudokuProgression.SeasonExperience(puzzle.Difficulty),
            };
        }
        return new
        {
            Screen = InGame ? "game" : "library",
            ShowTimer = timerVisible(), ShowGuides = guidesVisible(),
            Entries = entries,
            NormalCompleted = progress.CompletedPuzzleIds.Count(id =>
                SudokuLibrary.TryGet(id, out SudokuPuzzle? p) && p.Pool == SudokuPuzzlePool.Normal),
            NormalTotal = SudokuLibrary.Normal.Count,
            DailyCompleted = progress.CompletedPuzzleIds.Count(id =>
                SudokuLibrary.TryGet(id, out SudokuPuzzle? p) && p.Pool == SudokuPuzzlePool.Daily),
            Daily = new { Date = today.ToString("yyyy-MM-dd"),
                Difficulty = daily.Difficulty.ToString(),
                Completed = progress.CompletedPuzzleIds.Contains(daily.Id),
                CanResume = progress.Games.TryGetValue(daily.Id, out SudokuGameState? saved) && !saved.Completed },
            Game = game, Notice = notice, RewardNotice = rewardNotice(),
        };
    }

    internal (bool Success, string Message) Execute(ModernSudokuCommand command)
    {
        switch (command.Action)
        {
            case "sudoku.new":
                return StartNew(Enum.Parse<SudokuDifficulty>(command.Value!));
            case "sudoku.resume":
                return Resume(Enum.Parse<SudokuDifficulty>(command.Value!));
            case "sudoku.daily":
                return Daily();
            case "sudoku.back":
                if (!InGame) return (false, "No Sudoku board is open.");
                SaveElapsed();
                elapsed.Stop(); board = null; puzzle = null; dailyDate = null;
                undo.Clear(); redo.Clear();
                return (true, "Returned to Sudoku library.");
            case "sudoku.exit":
                if (InGame) return (false, "Return to the Sudoku library first.");
                return (true, "Returned to Lounge.");
            case "sudoku.guides":
                setGuidesVisible(command.Value == "true");
                return (true, "Guide preference saved.");
            case "sudoku.timer":
                setTimerVisible(command.Value == "true");
                return (true, "Timer preference saved.");
        }

        if (board is null || puzzle is null)
            return (false, "Choose a Sudoku puzzle first.");
        if (board.IsComplete && command.Action is not ("sudoku.undo" or "sudoku.redo"))
            return (false, "This Sudoku is complete.");

        switch (command.Action)
        {
            case "sudoku.value":
                if (!CanEdit(command.Index!.Value) || board.IsDigitComplete(command.Digit!.Value))
                    return (false, "Choose an editable cell and an available digit.");
                return Mutate(candidate => candidate.SetValue(command.Index!.Value, command.Digit!.Value),
                    "Number saved.", command.Index);
            case "sudoku.note":
                if (!CanEdit(command.Index!.Value) || board.ValueAt(command.Index!.Value) != 0 ||
                    board.IsDigitComplete(command.Digit!.Value))
                    return (false, "Notes require an empty editable cell.");
                return Mutate(candidate => candidate.ToggleNote(command.Index!.Value, command.Digit!.Value),
                    "Pencil mark saved.");
            case "sudoku.erase":
                if (!CanEdit(command.Index!.Value)) return (false, "This cell cannot be erased.");
                return Mutate(candidate => Erase(candidate, command.Index!.Value), "Cell cleared.");
            case "sudoku.hint":
            {
                int revealed = -1;
                var result = Mutate(candidate =>
                {
                    revealed = candidate.ApplyHint(command.Index!.Value);
                    return revealed >= 0;
                }, "One incorrect or empty cell was revealed.");
                return result;
            }
            case "sudoku.undo":
                if (undo.Count == 0) return (false, "Nothing to undo.");
                return RestoreHistory(undo, redo, "Undo saved.");
            case "sudoku.redo":
                if (redo.Count == 0) return (false, "Nothing to redo.");
                return RestoreHistory(redo, undo, "Redo saved.");
        }
        return (false, "Unsupported Sudoku action.");
    }

    private bool CanEdit(int index) => board is not null && index is >= 0 and < 81 &&
        !board.IsGiven(index) && !board.IsComplete;

    private (bool, string) StartNew(SudokuDifficulty difficulty)
    {
        IReadOnlyList<SudokuPuzzle> candidates = SudokuLibrary.ForDifficulty(difficulty);
        if (candidates.Count == 0) return (false, "No puzzles are available.");
        var completed = new HashSet<string>(progress.CompletedPuzzleIds, StringComparer.Ordinal);
        var seen = new HashSet<string>(progress.Games.Keys, StringComparer.Ordinal);
        _ = TryResume(difficulty, out SudokuPuzzle? active, out _);
        SudokuPuzzle chosen = candidates.FirstOrDefault(p => !completed.Contains(p.Id) && !seen.Contains(p.Id))
            ?? candidates.FirstOrDefault(p => !completed.Contains(p.Id) && p.Id != active?.Id)
            ?? candidates.FirstOrDefault(p => !completed.Contains(p.Id))
            ?? candidates[0];

        var games = new Dictionary<string, SudokuGameState>(progress.Games, StringComparer.Ordinal);
        var abandoned = new HashSet<string>(StringComparer.Ordinal);
        foreach ((string id, SudokuGameState state) in progress.Games)
        {
            if (!SudokuLibrary.TryGet(id, out SudokuPuzzle? existing) ||
                existing.Pool != SudokuPuzzlePool.Normal || existing.Difficulty != difficulty) continue;
            if (!SudokuGameState.Normalize(existing, state).Completed)
            {
                games.Remove(id);
                abandoned.Add(id);
            }
        }
        SudokuProgressState replaced = progress with
        {
            Games = games,
            ActiveNormalPuzzleId = abandoned.Contains(progress.ActiveNormalPuzzleId ?? "")
                ? null : progress.ActiveNormalPuzzleId,
        };
        return Load(chosen, null, SudokuGameState.Create(chosen), replaced);
    }

    private (bool, string) Resume(SudokuDifficulty difficulty)
    {
        if (!TryResume(difficulty, out SudokuPuzzle? found, out SudokuGameState? state) ||
            found is null || state is null) return (false, "No unfinished puzzle to resume.");
        return Load(found, null, state, progress);
    }

    private (bool, string) Daily()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        SudokuPuzzle chosen = progress.TryGetDaily(today, out SudokuPuzzle? found) && found is not null
            ? found : SudokuDailySchedule.ForDate(today);
        SudokuProgressState assigned = progress.AssignDaily(today, chosen);
        SudokuGameState saved = assigned.Games.TryGetValue(chosen.Id, out SudokuGameState? existing)
            ? SudokuGameState.Normalize(chosen, existing) : SudokuGameState.Create(chosen);
        return Load(chosen, today, saved, assigned);
    }

    private (bool, string) Load(SudokuPuzzle chosen, DateOnly? day, SudokuGameState state,
        SudokuProgressState baseProgress)
    {
        SudokuBoard nextBoard = new(chosen, state);
        SudokuProgressState saved = baseProgress.SaveGame(chosen, nextBoard.Snapshot(state.ElapsedSeconds), day);
        persist(saved); // Throws before replacing any live board if disk storage fails.
        progress = saved;
        elapsed.Stop(); elapsed.Reset();
        puzzle = chosen; board = nextBoard; dailyDate = day;
        elapsedBase = state.ElapsedSeconds;
        lastPersistedSeconds = elapsedBase;
        undo.Clear(); redo.Clear();
        if (!nextBoard.IsComplete) elapsed.Start();
        notice = nextBoard.IsComplete ? "Puzzle completed." : "Puzzle loaded and saved.";
        return (true, notice);
    }

    private bool TryResume(SudokuDifficulty difficulty,
        out SudokuPuzzle? found, out SudokuGameState? state)
    {
        if (progress.ActiveNormalPuzzleId is string active &&
            TryRestore(active, difficulty, out found, out state)) return true;
        foreach (string id in progress.Games.Keys.Reverse())
            if (TryRestore(id, difficulty, out found, out state)) return true;
        found = null; state = null; return false;
    }

    private bool TryRestore(string id, SudokuDifficulty difficulty,
        out SudokuPuzzle? found, out SudokuGameState? state)
    {
        found = null; state = null;
        if (!SudokuLibrary.TryGet(id, out SudokuPuzzle? p) ||
            p.Pool != SudokuPuzzlePool.Normal || p.Difficulty != difficulty ||
            !progress.Games.TryGetValue(id, out SudokuGameState? saved)) return false;
        SudokuGameState clean = SudokuGameState.Normalize(p, saved);
        if (clean.Completed) return false;
        found = p; state = clean; return true;
    }

    private (bool, string) Mutate(Func<SudokuBoard, bool> apply, string message, int? checkedIndex = null)
    {
        SudokuBoard candidate = board!.Clone();
        if (!apply(candidate)) return (false, "No Sudoku change was needed.");
        SaveCandidate(candidate);
        undo.Push(board.CaptureFrame()); redo.Clear();
        board = candidate;
        if (candidate.IsComplete)
        {
            elapsed.Stop();
            notice = "Sudoku complete. Your reward was recorded in C#.";
        }
        else notice = checkedIndex is >= 0 and < 81 && candidate.IsIncorrectAt(checkedIndex.Value)
            ? "This number does not belong in this cell." : message;
        return (true, notice);
    }

    private (bool, string) RestoreHistory(Stack<SudokuBoardFrame> from, Stack<SudokuBoardFrame> into,
        string message)
    {
        SudokuBoard candidate = board!.Clone();
        candidate.RestoreFrame(from.Peek());
        SaveCandidate(candidate);
        into.Push(board.CaptureFrame());
        from.Pop();
        board = candidate;
        if (candidate.IsComplete) elapsed.Stop();
        else if (!elapsed.IsRunning) elapsed.Start();
        notice = message;
        return (true, message);
    }

    private void SaveCandidate(SudokuBoard candidate)
    {
        long seconds = CurrentSeconds;
        SudokuProgressState next = progress.SaveGame(puzzle!, candidate.Snapshot(seconds), dailyDate);
        persist(next);
        progress = next;
        lastPersistedSeconds = seconds;
    }

    internal void SaveElapsed()
    {
        if (board is null || puzzle is null) return;
        long seconds = CurrentSeconds;
        if (seconds <= lastPersistedSeconds) return;
        SudokuProgressState next = progress.SaveGame(puzzle, board.Snapshot(seconds), dailyDate);
        persist(next);
        progress = next;
        lastPersistedSeconds = seconds;
    }

    internal void CheckpointClock()
    {
        if (!InGame || !elapsed.IsRunning || CurrentSeconds - lastPersistedSeconds < 15) return;
        SaveElapsed();
    }

    internal void Stop()
    {
        SaveElapsed();
        elapsed.Stop();
    }

    private static bool Erase(SudokuBoard candidate, int index)
    {
        if (candidate.ValueAt(index) != 0) return candidate.SetValue(index, 0);
        int notes = candidate.NotesAt(index);
        if (notes == 0) return false;
        for (int digit = 1; digit <= 9; digit++)
            if ((notes & (1 << digit)) != 0) candidate.ToggleNote(index, digit);
        return true;
    }
}
