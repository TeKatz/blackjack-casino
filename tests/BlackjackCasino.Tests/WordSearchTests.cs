using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Tests;

public sealed class WordSearchTests
{
    [Fact]
    public void CatalogContainsTwentyFourDeterministicPuzzlesPerDifficulty()
    {
        Assert.Equal(144, WordSearchCatalog.All.Count);
        foreach (WordSearchDifficulty difficulty in Enum.GetValues<WordSearchDifficulty>())
        {
            var puzzles = WordSearchCatalog.For(difficulty);
            Assert.Equal(24, puzzles.Count);
            Assert.Equal(24, puzzles.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void EveryAuthoredPuzzleGeneratesAValidStableGrid()
    {
        foreach (WordSearchPuzzleDefinition definition in WordSearchCatalog.All)
        {
            WordSearchPuzzle first = WordSearchGenerator.Generate(definition);
            WordSearchPuzzle second = WordSearchGenerator.Generate(definition);
            Assert.Equal(first.Grid, second.Grid);
            Assert.Equal(JsonSerializer.Serialize(first.Placements), JsonSerializer.Serialize(second.Placements));
            Assert.Equal(definition.Size * definition.Size, first.Grid.Length);
            Assert.Equal(definition.WordCount, first.Placements.Count);
            Assert.All(first.Grid, c => Assert.InRange(c, 'A', 'Z'));

            foreach (WordSearchPlacement placement in first.Placements)
            {
                IReadOnlyList<WordSearchCell> cells = placement.Cells();
                Assert.Equal(placement.Word.Length, cells.Count);
                string onGrid = new(cells.Select(c => first.At(c.Row, c.Column)).ToArray());
                Assert.Equal(placement.Word, onGrid);
            }
        }
    }

    [Fact]
    public void RelaxedNeverUsesDiagonalOrBackwardPlacements()
    {
        foreach (WordSearchPuzzleDefinition definition in WordSearchCatalog.For(WordSearchDifficulty.Relaxed))
        foreach (WordSearchPlacement placement in WordSearchGenerator.Generate(definition).Placements)
        {
            int dr = placement.EndRow - placement.StartRow;
            int dc = placement.EndColumn - placement.StartColumn;
            Assert.True((dr == 0 && dc > 0) || (dc == 0 && dr > 0));
        }
    }

    [Fact]
    public void SelectionFindsAWordInEitherDirectionOnlyOnce()
    {
        WordSearchGameState state = WordSearchGame.Start(WordSearchCatalog.For(WordSearchDifficulty.Medium)[0]);
        WordSearchPlacement placement = state.Puzzle.Placements[0];
        var start = new WordSearchCell(placement.StartRow, placement.StartColumn);
        var end = new WordSearchCell(placement.EndRow, placement.EndColumn);

        Assert.True(WordSearchGame.TryFind(state, end, start, out string? found));
        Assert.Equal(placement.Word, found);
        Assert.False(WordSearchGame.TryFind(state, start, end, out _));
        Assert.Single(state.FoundWords);
    }

    [Fact]
    public void InvalidBentSelectionDoesNotFindAWord()
    {
        WordSearchGameState state = WordSearchGame.Start(WordSearchCatalog.For(WordSearchDifficulty.Easy)[0]);
        Assert.Empty(WordSearchGame.SelectionCells(state.Puzzle, new(0, 0), new(1, 3)));
        Assert.False(WordSearchGame.TryFind(state, new(0, 0), new(1, 3), out _));
    }

    [Fact]
    public void HintCountsButDoesNotMarkTheWordFound()
    {
        WordSearchGameState state = WordSearchGame.Start(WordSearchCatalog.For(WordSearchDifficulty.Hard)[0]);
        WordSearchPlacement? hint = WordSearchGame.Hint(state);
        Assert.NotNull(hint);
        Assert.Equal(1, state.HintsUsed);
        Assert.Empty(state.FoundWords);
    }

    [Fact]
    public void ProgressStateRoundTripsActiveGameAndCompletionReceipts()
    {
        WordSearchGameState game = WordSearchGame.Start(WordSearchCatalog.For(WordSearchDifficulty.Relaxed)[0]);
        WordSearchPlacement placement = game.Puzzle.Placements[0];
        Assert.True(WordSearchGame.TryFind(game, new(placement.StartRow, placement.StartColumn), new(placement.EndRow, placement.EndColumn), out _));
        var state = new WordSearchProgressState { ActiveGame = game };

        string json = JsonSerializer.Serialize(state);
        WordSearchProgressState loaded = JsonSerializer.Deserialize<WordSearchProgressState>(json)!;
        loaded.Normalize();
        Assert.NotNull(loaded.ActiveGame);
        Assert.Equal(game.Puzzle.Grid, loaded.ActiveGame!.Puzzle.Grid);
        Assert.Equal(game.FoundWords, loaded.ActiveGame.FoundWords);
    }

    [Fact]
    public void CompletionRewardIsDurableAndIdempotent()
    {
        WordSearchPuzzleDefinition definition = WordSearchCatalog.For(WordSearchDifficulty.Expert)[0];
        WordSearchGameState game = WordSearchGame.Start(definition, DateTimeOffset.UnixEpoch);
        foreach (WordSearchPlacement placement in game.Puzzle.Placements)
            Assert.True(WordSearchGame.TryFind(game, new(placement.StartRow, placement.StartColumn), new(placement.EndRow, placement.EndColumn), out _));
        game.CompletedAt = DateTimeOffset.UnixEpoch.AddMinutes(4);
        WordSearchCompletionRecord receipt = WordSearchGame.Complete(game);

        var profile = new PlayerProfileState();
        var progression = new WordSearchProgression(new ProgressionService(profile));
        ProgressionAward first = progression.Process(receipt);
        ProgressionAward second = progression.Process(receipt);

        Assert.Equal(WordSearchProgression.PlayerExperience(WordSearchDifficulty.Expert), first.PlayerExperience);
        Assert.Equal(ProgressionAward.None, second);
        Assert.Equal(1, profile.WordSearch.CompletedPuzzles);
        Assert.Equal(definition.WordCount, profile.WordSearch.WordsFound);
        Assert.Equal(1, profile.WordSearch.NoHintCompletions);
        Assert.Equal(1, profile.WordSearch.ExpertCompleted);
    }
}
