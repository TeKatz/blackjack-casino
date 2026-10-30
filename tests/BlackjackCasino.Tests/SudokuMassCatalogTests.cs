using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Tests;

public sealed class SudokuMassCatalogTests
{
    [Fact]
    public void CatalogHasDiscussedMassCountsAndBalancedDifficultyBuckets()
    {
        Assert.Equal(9000, SudokuLibrary.Normal.Count);
        Assert.Equal(4002, SudokuLibrary.Daily.Count);
        Assert.Equal(13002, SudokuLibrary.All.Count);

        foreach (SudokuDifficulty difficulty in Enum.GetValues<SudokuDifficulty>())
        {
            Assert.Equal(1500, SudokuLibrary.Normal.Count(puzzle => puzzle.Difficulty == difficulty));
            Assert.Equal(667, SudokuLibrary.Daily.Count(puzzle => puzzle.Difficulty == difficulty));
        }
    }

    [Fact]
    public void EveryGridAndIdIsGloballyUniqueAcrossBothPools()
    {
        Assert.Equal(SudokuLibrary.All.Count,
            SudokuLibrary.All.Select(puzzle => puzzle.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(SudokuLibrary.All.Count,
            SudokuLibrary.All.Select(puzzle => puzzle.Givens).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void LegacyPuzzleIdsRemainStableForExistingSavesAndDailyAssignments()
    {
        Assert.Equal("256000703000000010000200084000360020000800000498072000060007300102043508080005900",
            SudokuLibrary.Get("normal-relaxed-001").Givens);
        Assert.Equal("070000080006100300900070004500040000030002000001600000700090003080005040004800200",
            SudokuLibrary.Get("daily-master-004").Givens);
    }

    [Fact]
    public void DailyCycleNowRunsForMoreThanTenYearsBeforeRepeating()
    {
        Assert.Equal(4002, SudokuLibrary.Daily.Count);
        DateOnly epoch = SudokuDailySchedule.Epoch;
        string[] cycle = Enumerable.Range(0, SudokuLibrary.Daily.Count)
            .Select(day => SudokuDailySchedule.ForDate(epoch.AddDays(day)).Id)
            .ToArray();

        Assert.Equal(4002, cycle.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(cycle[0], SudokuDailySchedule.ForDate(epoch.AddDays(4002)).Id);
    }
}
