using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Tests;

public sealed class SudokuTests
{
    [Fact]
    public void BundledCatalogUsesSeparateUniquePoolsAndUniqueSolutions()
    {
        Assert.NotEmpty(SudokuLibrary.Normal);
        Assert.NotEmpty(SudokuLibrary.Daily);

        Assert.Equal(SudokuLibrary.All.Count, SudokuLibrary.All.Select(puzzle => puzzle.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(SudokuLibrary.Normal.Select(puzzle => puzzle.Givens)
            .Intersect(SudokuLibrary.Daily.Select(puzzle => puzzle.Givens), StringComparer.Ordinal));

        foreach (SudokuPuzzle puzzle in SudokuLibrary.All)
        {
            Assert.Equal(81, puzzle.Givens.Length);
            Assert.Equal(1, SudokuSolver.CountSolutions(puzzle.Givens));
            Assert.True(SudokuSolver.TrySolve(puzzle.Givens, out string solution));
            Assert.True(SudokuSolver.IsValidCompletedGrid(solution));
        }
    }

    [Fact]
    public void EveryDifficultyHasFreePlayAndDailyContent()
    {
        foreach (SudokuDifficulty difficulty in Enum.GetValues<SudokuDifficulty>())
        {
            Assert.Contains(SudokuLibrary.Normal, puzzle => puzzle.Difficulty == difficulty);
            Assert.Contains(SudokuLibrary.Daily, puzzle => puzzle.Difficulty == difficulty);
        }
    }

    [Fact]
    public void DailyScheduleIsOfflineDeterministicAndDoesNotRepeatWithinCycle()
    {
        DateOnly epoch = SudokuDailySchedule.Epoch;
        string[] firstPass = Enumerable.Range(0, SudokuLibrary.Daily.Count)
            .Select(offset => SudokuDailySchedule.ForDate(epoch.AddDays(offset)).Id)
            .ToArray();
        string[] secondPass = Enumerable.Range(0, SudokuLibrary.Daily.Count)
            .Select(offset => SudokuDailySchedule.ForDate(epoch.AddDays(offset)).Id)
            .ToArray();

        Assert.Equal(firstPass, secondPass);
        Assert.Equal(SudokuLibrary.Daily.Count, firstPass.Distinct(StringComparer.Ordinal).Count());
        Assert.All(firstPass, id => Assert.Equal(SudokuPuzzlePool.Daily, SudokuLibrary.Get(id).Pool));
    }

    [Fact]
    public void BoardProtectsGivensAndSupportsNotesUndoFramesAndHints()
    {
        SudokuPuzzle puzzle = SudokuLibrary.Normal[0];
        var board = new SudokuBoard(puzzle);
        int given = Enumerable.Range(0, 81).First(index => board.IsGiven(index));
        int editable = Enumerable.Range(0, 81).First(index => !board.IsGiven(index));

        Assert.False(board.SetValue(given, 9));
        Assert.True(board.ToggleNote(editable, 3));
        Assert.NotEqual(0, board.NotesAt(editable));

        SudokuBoardFrame frame = board.CaptureFrame();
        Assert.True(board.SetValue(editable, 4));
        Assert.Equal(4, board.ValueAt(editable));
        board.RestoreFrame(frame);
        Assert.Equal(0, board.ValueAt(editable));
        Assert.NotEqual(0, board.NotesAt(editable));

        int hinted = board.ApplyHint(editable);
        Assert.Equal(editable, hinted);
        Assert.Equal(1, board.HintsUsed);
        Assert.NotEqual(0, board.ValueAt(editable));
    }

    [Fact]
    public void ProgressKeepsDailyAssignmentsAndNormalProgressSeparate()
    {
        DateOnly day = new(2026, 10, 30);
        SudokuPuzzle daily = SudokuDailySchedule.ForDate(day);
        SudokuPuzzle normal = SudokuLibrary.Normal[0];

        SudokuProgressState progress = new SudokuProgressState().AssignDaily(day, daily);
        progress = progress.SaveGame(normal, SudokuGameState.Create(normal));
        progress = progress.SaveGame(daily, SudokuGameState.Create(daily), day);
        progress = SudokuProgressState.Normalize(progress);

        Assert.Equal(normal.Id, progress.ActiveNormalPuzzleId);
        Assert.Equal(daily.Id, progress.ActiveDailyPuzzleId);
        Assert.True(progress.TryGetDaily(day, out SudokuPuzzle? restored));
        Assert.Equal(daily.Id, restored?.Id);
        Assert.NotEqual(normal.Id, daily.Id);
    }

    [Fact]
    public void InvalidOrForeignSavedCellsCannotOverwritePuzzleGivens()
    {
        SudokuPuzzle puzzle = SudokuLibrary.Normal[0];
        char[] corrupted = puzzle.Givens.ToCharArray();
        int given = Array.FindIndex(corrupted, value => value != '0');
        corrupted[given] = corrupted[given] == '9' ? '8' : '9';

        SudokuGameState restored = SudokuGameState.Normalize(puzzle, new SudokuGameState
        {
            PuzzleId = puzzle.Id,
            Board = new string(corrupted),
            Notes = Enumerable.Repeat(0x3FE, 81).ToArray(),
            ElapsedSeconds = -42,
            HintsUsed = -3,
        });

        Assert.Equal(puzzle.Givens[given], restored.Board[given]);
        Assert.Equal(0, restored.Notes[given]);
        Assert.Equal(0, restored.ElapsedSeconds);
        Assert.Equal(0, restored.HintsUsed);
    }
}
