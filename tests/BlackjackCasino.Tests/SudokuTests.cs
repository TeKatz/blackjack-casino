using BlackjackCasino.Core;
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
        Assert.True(board.IsCorrectAt(editable));
    }

    [Fact]
    public void IncorrectValuesAreDetectedAndHintsCorrectThemBeforeEmptyCells()
    {
        SudokuPuzzle puzzle = SudokuLibrary.Normal[0];
        Assert.True(SudokuSolver.TrySolve(puzzle.Givens, out string solution));
        var board = new SudokuBoard(puzzle);
        int editable = Enumerable.Range(0, 81).First(index => !board.IsGiven(index));
        int correct = solution[editable] - '0';
        int wrong = correct == 9 ? 1 : correct + 1;

        Assert.True(board.SetValue(editable, wrong));
        Assert.True(board.IsIncorrectAt(editable));
        Assert.False(board.IsCorrectAt(editable));

        int hinted = board.ApplyHint();
        Assert.Equal(editable, hinted);
        Assert.Equal(correct, board.ValueAt(editable));
        Assert.True(board.IsCorrectAt(editable));
        Assert.False(board.IsIncorrectAt(editable));
    }

    [Fact]
    public void RepeatedHintsRecoverACompletelyFilledWrongBoardWithoutSoftlock()
    {
        SudokuPuzzle puzzle = SudokuLibrary.Normal[0];
        Assert.True(SudokuSolver.TrySolve(puzzle.Givens, out string solution));
        var board = new SudokuBoard(puzzle);

        foreach (int index in Enumerable.Range(0, 81).Where(index => !board.IsGiven(index)))
        {
            int correct = solution[index] - '0';
            int wrong = correct == 9 ? 1 : correct + 1;
            Assert.True(board.SetValue(index, wrong));
        }

        Assert.False(board.IsComplete);
        Assert.DoesNotContain(Enumerable.Range(0, 81), index => !board.IsGiven(index) && board.ValueAt(index) == 0);

        int guard = 0;
        while (!board.IsComplete && guard < 81)
        {
            Assert.True(board.ApplyHint() >= 0);
            guard++;
        }

        Assert.True(board.IsComplete);
        Assert.InRange(guard, 1, 81);
        Assert.Equal(guard, board.HintsUsed);
    }

    [Fact]
    public void DigitCompletionRequiresEveryOccurrenceToBeCorrectAndNoWrongExtras()
    {
        SudokuPuzzle puzzle = SudokuLibrary.Normal[0];
        Assert.True(SudokuSolver.TrySolve(puzzle.Givens, out string solution));
        var board = new SudokuBoard(puzzle);
        int digit = Enumerable.Range(1, 9).First(candidate =>
            Enumerable.Range(0, 81).Any(index => !board.IsGiven(index) && solution[index] - '0' == candidate));

        Assert.False(board.IsDigitComplete(digit));
        foreach (int index in Enumerable.Range(0, 81).Where(index => !board.IsGiven(index) && solution[index] - '0' == digit))
        {
            Assert.True(board.SetValue(index, digit));
        }
        Assert.True(board.IsDigitComplete(digit));

        int wrongExtra = Enumerable.Range(0, 81).First(index =>
            !board.IsGiven(index) && board.ValueAt(index) == 0 && solution[index] - '0' != digit);
        Assert.True(board.SetValue(wrongExtra, digit));
        Assert.False(board.IsDigitComplete(digit));

        Assert.True(board.SetValue(wrongExtra, 0));
        Assert.True(board.IsDigitComplete(digit));
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

    [Fact]
    public void StatisticsRecordEachPuzzleOnceAndTrackModeDifficultyAndCleanSolves()
    {
        var stats = new SudokuStatistics();
        var clean = new SudokuCompletionRecord("normal-medium-test", SudokuDifficulty.Medium, SudokuPuzzlePool.Normal, 620, 0);
        var daily = new SudokuCompletionRecord("daily-master-test", SudokuDifficulty.Master, SudokuPuzzlePool.Daily, 910, 2);

        Assert.True(stats.Record(clean));
        Assert.False(stats.Record(clean with { ElapsedSeconds = 1 }));
        Assert.True(stats.Record(daily));

        Assert.Equal(2, stats.CompletedPuzzles);
        Assert.Equal(1, stats.FreePlayCompleted);
        Assert.Equal(1, stats.DailyCompleted);
        Assert.Equal(1, stats.NoHintCompletions);
        Assert.Equal(2, stats.HintsUsed);
        Assert.Equal(1530, stats.TotalSolveSeconds);
        Assert.Equal(620, stats.FastestSolveSeconds);
        Assert.Equal(620, stats.FastestNoHintSeconds);
        Assert.Equal(1, stats.MediumCompleted);
        Assert.Equal(1, stats.MasterCompleted);
    }

    [Fact]
    public void SudokuChallengesUseDedicatedStatisticsWithoutSpeedRequirements()
    {
        var profile = new PlayerProfileState();
        profile.Sudoku.CompletedPuzzles = 10;
        profile.Sudoku.DailyCompleted = 1;
        profile.Sudoku.NoHintCompletions = 3;
        profile.Sudoku.ExpertCompleted = 10;
        profile.Sudoku.MasterCompleted = 1;

        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sudoku-10")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sudoku-daily-1")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sudoku-clean-1")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sudoku-expert-10")!));
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("sudoku-master-1")!));
        Assert.DoesNotContain(ChallengeCatalog.For(ChallengeCategory.Sudoku), challenge =>
            challenge.Name.Contains("minute", StringComparison.OrdinalIgnoreCase) ||
            challenge.Description.Contains("under ", StringComparison.OrdinalIgnoreCase));
    }
}
