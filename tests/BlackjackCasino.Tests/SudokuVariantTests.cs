using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class SudokuVariantTests
{
    [Theory]
    [InlineData("x", 9, SudokuDifficulty.Easy, 81)]
    [InlineData("killer", 9, SudokuDifficulty.Hard, 81)]
    [InlineData("jigsaw", 9, SudokuDifficulty.Medium, 81)]
    [InlineData("samurai", 9, SudokuDifficulty.Easy, 441)]
    [InlineData("mini", 4, SudokuDifficulty.Hard, 16)]
    [InlineData("mini", 6, SudokuDifficulty.Medium, 36)]
    public void EveryModeBuildsAValidDeterministicRuleAwarePuzzle(string mode, int size,
        SudokuDifficulty difficulty, int cells)
    {
        SudokuVariantPuzzle first = SudokuVariantLibrary.Create(mode, difficulty, size, 0);
        SudokuVariantPuzzle again = SudokuVariantLibrary.Create(mode, difficulty, size, 0);
        Assert.Equal(first.Id, again.Id);
        Assert.Equal(first.Givens, again.Givens);
        Assert.Equal(first.Solution, again.Solution);
        Assert.Equal(cells, first.Solution.Length);
        Assert.Equal(mode, first.Mode);
        Assert.False(first.Givens == first.Solution);
        Assert.All(first.Groups, group => Assert.Equal(first.Digits, group.Length));
        if (mode is "mini" or "x" or "jigsaw")
            Assert.Equal(1, SudokuVariantSolver.CountSolutions(first, first.Givens));
        if (mode == "samurai")
        {
            Assert.Equal(369, first.Solution.Count(c => c != '-'));
            Assert.Equal(72, first.Solution.Count(c => c == '-'));
        }
        if (mode == "killer")
        {
            Assert.NotEmpty(first.Cages);
            Assert.True(first.Cages.Count > 20);
            Assert.Equal(81, first.Cages.Sum(cage => cage.Cells.Length));
            Assert.Equal(1, SudokuVariantSolver.CountSolutions(first, first.Givens));
        }
        if (mode == "x")
        {
            Assert.Equal(9, Enumerable.Range(0, 9).Select(i => first.Solution[i * 10]).Distinct().Count());
            Assert.Equal(9, Enumerable.Range(0, 9).Select(i => first.Solution[i * 9 + 8 - i]).Distinct().Count());
        }
        if (mode == "jigsaw")
            Assert.Contains(Enumerable.Range(0, 81), i => first.Regions[i] != (i / 27) * 3 + i % 9 / 3);
    }

    [Fact]
    public void EveryModeCanSaveResumeAndCompleteWithServerValidatedHints()
    {
        SudokuVariantProgressState saved = new();
        var service = new ModernSudokuVariants(saved, value => saved = value,
            () => new SudokuVariantStatistics(), () => "XP saved");
        ModernSudokuCommand Send(string action, int? index = null, int? digit = null, string? value = null) =>
            new(Guid.NewGuid().ToString("D"), Guid.NewGuid().ToString("D"), 0,
                action, index, digit, value);
        Assert.True(service.Execute(Send("sudoku.variant.new", value: "mini:Easy:4")).Success);
        Assert.NotNull(service.Snapshot());
        Assert.True(service.Execute(Send("sudoku.variant.hint", index: -1)).Success);
        Assert.True(service.Execute(Send("sudoku.variant.back")).Success);
        Assert.True(service.Execute(Send("sudoku.variant.resume", value: "mini:Easy:4")).Success);
        for (int i = 0; i < 16; i++)
        {
            if (saved.CompletedReceipts.Count > 0) break;
            Assert.True(service.Execute(Send("sudoku.variant.hint", index: -1)).Success);
        }
        Assert.Single(saved.CompletedReceipts);
        Assert.False(service.Execute(Send("sudoku.variant.hint", index: -1)).Success);
        var profile = new PlayerProfileState();
        var reward = new SudokuVariantProgression(new ProgressionService(profile));
        Assert.True(reward.Process(saved.CompletedReceipts[0]).PlayerExperience > 0);
        Assert.Equal(ProgressionAward.None, reward.Process(saved.CompletedReceipts[0]));
        Assert.Equal(1, profile.SudokuVariants.Mini4Completed);
        Assert.True(profile.SudokuCareer.LifetimeExperience > 0);
    }

    [Fact]
    public void InvalidVariantCommandsAreRejectedAtNativeProtocolBoundary()
    {
        var session = new ModernSudokuSession();
        object Make(string action, string? value = null, int? index = null, int? digit = null)
        {
            var message = new Dictionary<string, object>
            {
                ["type"] = "lounge.sudoku.command", ["version"] = 1,
                ["requestId"] = Guid.NewGuid().ToString("D"),
                ["session"] = session.Id, ["expectedRevision"] = session.Revision, ["action"] = action,
            };
            if (value is not null) message["value"] = value;
            if (index.HasValue) message["index"] = index.Value;
            if (digit.HasValue) message["digit"] = digit.Value;
            return message;
        }
        Assert.NotNull(ModernSudokuProtocol.Parse(JsonSerializer.Serialize(Make("sudoku.variant.new", "mini:Easy:4"))));
        Assert.NotNull(ModernSudokuProtocol.Parse(JsonSerializer.Serialize(Make("sudoku.variant.new", "samurai:Hard"))));
        Assert.Null(ModernSudokuProtocol.Parse(JsonSerializer.Serialize(Make("sudoku.variant.new", "classic:Easy"))));
        Assert.Null(ModernSudokuProtocol.Parse(JsonSerializer.Serialize(Make("sudoku.variant.value", index: 441, digit: 1))));
        Assert.Null(ModernSudokuProtocol.Parse(JsonSerializer.Serialize(Make("sudoku.variant.value", index: 1, digit: 10))));
    }
}
