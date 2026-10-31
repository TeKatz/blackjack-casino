using BlackjackCasino.Core;
using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Tests;

public sealed class MemoryTests
{
    [Fact]
    public void DifficultySpecsMatchTheFiveLoungeTables()
    {
        Assert.Equal(new MemoryDifficultySpec(4, 4, 8), MemoryCatalog.Spec(MemoryDifficulty.Easy));
        Assert.Equal(new MemoryDifficultySpec(4, 5, 10), MemoryCatalog.Spec(MemoryDifficulty.Normal));
        Assert.Equal(new MemoryDifficultySpec(6, 6, 18), MemoryCatalog.Spec(MemoryDifficulty.Hard));
        Assert.Equal(new MemoryDifficultySpec(6, 8, 24), MemoryCatalog.Spec(MemoryDifficulty.Expert));
        Assert.Equal(new MemoryDifficultySpec(8, 8, 32), MemoryCatalog.Spec(MemoryDifficulty.Master));
    }

    [Fact]
    public void EveryDeckSupportsMasterAndHasStableUniqueSymbols()
    {
        Assert.Equal(8, MemoryCatalog.Decks.Count);
        Assert.Equal(MemoryCatalog.Decks.Count, MemoryCatalog.Decks.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.All(MemoryCatalog.Decks, deck =>
        {
            Assert.True(deck.Symbols.Count >= 32);
            Assert.Equal(deck.Symbols.Count, deck.Symbols.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count());
        });
    }

    [Fact]
    public void SeedReproducesTheExactTableAndEveryCardHasOneMate()
    {
        List<string> first = MemoryGame.CreateCards(MemoryDifficulty.Master, "animals", 12345);
        List<string> second = MemoryGame.CreateCards(MemoryDifficulty.Master, "animals", 12345);
        List<string> other = MemoryGame.CreateCards(MemoryDifficulty.Master, "animals", 54321);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
        Assert.Equal(64, first.Count);
        Assert.All(first.GroupBy(x => x), group => Assert.Equal(2, group.Count()));
    }

    [Fact]
    public void MatchStaysOpenAndBuildsStreak()
    {
        MemoryGameState game = MemoryGame.Start(MemoryDifficulty.Easy, seed: 71);
        int[] pair = Pair(game, 0);
        Assert.Equal(MemoryFlipOutcome.FirstCard, MemoryGame.Flip(game, pair[0]).Outcome);
        MemoryFlipResult result = MemoryGame.Flip(game, pair[1]);
        Assert.Equal(MemoryFlipOutcome.Match, result.Outcome);
        Assert.Equal(1, result.Streak);
        Assert.Equal(1, game.Moves);
        Assert.Equal(2, game.CardsFlipped);
        Assert.Equal(2, game.MatchedIndices.Count);
        Assert.Empty(game.FaceUpIndices);
        Assert.Equal(1, game.CurrentStreak);
        Assert.Equal(1, game.BestStreak);
    }

    [Fact]
    public void MismatchWaitsForResolutionAndBreaksStreak()
    {
        MemoryGameState game = MemoryGame.Start(MemoryDifficulty.Easy, seed: 72);
        int[] pair = Pair(game, 0);
        MemoryGame.Flip(game, pair[0]);
        MemoryGame.Flip(game, pair[1]);
        int first = game.Cards.FindIndex(x => !game.MatchedIndices.Contains(game.Cards.IndexOf(x)));
        int second = Enumerable.Range(0, game.Cards.Count)
            .First(i => !game.MatchedIndices.Contains(i) && i != first && game.Cards[i] != game.Cards[first]);
        MemoryGame.Flip(game, first);
        MemoryFlipResult miss = MemoryGame.Flip(game, second);
        Assert.Equal(MemoryFlipOutcome.Mismatch, miss.Outcome);
        Assert.Equal(1, game.WrongMatches);
        Assert.Equal(0, game.CurrentStreak);
        Assert.Equal(2, game.FaceUpIndices.Count);
        Assert.Equal(MemoryFlipOutcome.WaitingForMismatch, MemoryGame.Flip(game, Enumerable.Range(0, game.Cards.Count).First(i => !game.FaceUpIndices.Contains(i))).Outcome);
        Assert.True(MemoryGame.ResolveMismatch(game));
        Assert.Empty(game.FaceUpIndices);
    }

    [Fact]
    public void PerfectTableCompletesInExactlyOneMovePerPair()
    {
        MemoryGameState game = MemoryGame.Start(MemoryDifficulty.Normal, "gemstones", 314159);
        foreach (IGrouping<string, (string Card, int Index)> group in game.Cards.Select((card, index) => (Card: card, Index: index)).GroupBy(x => x.Card))
        {
            int[] indices = group.Select(x => x.Index).ToArray();
            MemoryGame.Flip(game, indices[0]);
            MemoryGame.Flip(game, indices[1]);
        }
        Assert.True(game.Completed);
        Assert.True(game.Perfect);
        Assert.Equal(game.PairCount, game.Moves);
        Assert.Equal(0, game.WrongMatches);
        Assert.Equal(game.PairCount, game.BestStreak);
        MemoryCompletionRecord completion = MemoryGame.Complete(game);
        Assert.True(completion.Perfect);
        Assert.Equal(game.PairCount, completion.Pairs);
    }

    [Fact]
    public void ProgressStateKeepsResumeTableAndRepairsUnresolvedMismatch()
    {
        MemoryGameState game = MemoryGame.Start(MemoryDifficulty.Hard, "casino-icons", 99);
        int first = 0;
        int second = Enumerable.Range(1, game.Cards.Count).First(i => game.Cards[i] != game.Cards[first]);
        MemoryGame.Flip(game, first);
        MemoryGame.Flip(game, second);
        var progress = new MemoryProgressState { ActiveGame = game, PreferredDeckId = "casino-icons" };
        progress.Normalize();
        Assert.NotNull(progress.ActiveGame);
        Assert.Empty(progress.ActiveGame!.FaceUpIndices);
        Assert.Equal(game.GameId, progress.ActiveGame.GameId);
        Assert.Equal(game.Seed, progress.ActiveGame.Seed);
        Assert.Equal(game.Cards, progress.ActiveGame.Cards);
    }

    [Fact]
    public void StatisticsRecordEachCompletedGameExactlyOnce()
    {
        var stats = new MemoryStatistics();
        var completion = new MemoryCompletionRecord(Guid.NewGuid(), MemoryDifficulty.Expert, "animals", 24, 30, 60, 6, 8, 123, DateTimeOffset.UtcNow);
        Assert.True(stats.Record(completion));
        Assert.False(stats.Record(completion));
        Assert.Equal(1, stats.CompletedGames);
        Assert.Equal(24, stats.PairsFound);
        Assert.Equal(60, stats.CardsFlipped);
        Assert.Equal(6, stats.WrongMatches);
        Assert.Equal(1, stats.ExpertCompleted);
        Assert.Equal(0, stats.MasterCompleted);
        Assert.Equal(8, stats.BestStreak);
        Assert.Equal(30, stats.BestMovesByDifficulty[nameof(MemoryDifficulty.Expert)]);
    }

    [Fact]
    public void ProgressionRewardsCompletedTableOnceAndScalesByDifficulty()
    {
        Assert.True(MemoryProgression.PlayerExperience(MemoryDifficulty.Master) > MemoryProgression.PlayerExperience(MemoryDifficulty.Expert));
        Assert.True(MemoryProgression.PlayerExperience(MemoryDifficulty.Expert) > MemoryProgression.PlayerExperience(MemoryDifficulty.Easy));
        var profile = new PlayerProfileState();
        ProfileNormalization.Normalize(profile);
        var progression = new MemoryProgression(new ProgressionService(profile));
        var completion = new MemoryCompletionRecord(Guid.NewGuid(), MemoryDifficulty.Master, "space", 32, 45, 90, 13, 7, 300, DateTimeOffset.UtcNow);
        ProgressionAward first = progression.Process(completion);
        ProgressionAward second = progression.Process(completion);
        Assert.Equal(MemoryProgression.PlayerExperience(MemoryDifficulty.Master), first.PlayerExperience);
        Assert.Equal(MemoryProgression.SeasonExperience(MemoryDifficulty.Master), first.SeasonExperience);
        Assert.Equal(ProgressionAward.None, second);
        Assert.Equal(1, profile.Memory.CompletedGames);
        Assert.Equal(1, profile.Memory.MasterCompleted);
    }

    private static int[] Pair(MemoryGameState game, int ordinal)
    {
        return game.Cards.Select((card, index) => (card, index))
            .GroupBy(x => x.card)
            .ElementAt(ordinal)
            .Select(x => x.index)
            .ToArray();
    }
}
