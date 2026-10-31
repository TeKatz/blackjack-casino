using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Tests;

public sealed class SolitaireTests
{
    [Theory]
    [InlineData(SolitaireVariant.Klondike, 52)]
    [InlineData(SolitaireVariant.FreeCell, 52)]
    [InlineData(SolitaireVariant.Spider, 104)]
    [InlineData(SolitaireVariant.Pyramid, 52)]
    [InlineData(SolitaireVariant.Golf, 52)]
    [InlineData(SolitaireVariant.TriPeaks, 52)]
    public void EveryVariantDealsTheExpectedNumberOfCards(SolitaireVariant variant, int expected)
    {
        SolitaireGameState game = SolitaireGame.Create(variant, 42, spiderSuits: 4);
        int cardsInPiles = game.Piles.Values.Sum(p => p.Count);
        int removedRuns = variant == SolitaireVariant.Spider ? game.CompletedRuns * 13 : 0;
        Assert.Equal(expected, cardsInPiles + removedRuns);
    }

    [Fact]
    public void InitialLayoutsMatchTheirRuleFamilies()
    {
        SolitaireGameState klondike = SolitaireGame.Create(SolitaireVariant.Klondike, 1);
        Assert.Equal(24, klondike.Piles["Stock"].Count);
        Assert.Equal([1, 2, 3, 4, 5, 6, 7], Enumerable.Range(0, 7).Select(i => klondike.Piles[$"T{i}"].Count));
        Assert.All(Enumerable.Range(0, 7), i => Assert.True(klondike.Piles[$"T{i}"][^1].FaceUp));

        SolitaireGameState spider = SolitaireGame.Create(SolitaireVariant.Spider, 1, 2);
        Assert.Equal(50, spider.Piles["Stock"].Count);
        Assert.Equal([6, 6, 6, 6, 5, 5, 5, 5, 5, 5], Enumerable.Range(0, 10).Select(i => spider.Piles[$"T{i}"].Count));

        SolitaireGameState pyramid = SolitaireGame.Create(SolitaireVariant.Pyramid, 1);
        Assert.Equal(24, pyramid.Piles["Stock"].Count);
        Assert.Equal(28, Enumerable.Range(0, 28).Count(i => pyramid.Piles[$"P{i}"].Count == 1));

        SolitaireGameState golf = SolitaireGame.Create(SolitaireVariant.Golf, 1);
        Assert.Equal(16, golf.Piles["Stock"].Count);
        Assert.Single(golf.Piles["Waste"]);

        SolitaireGameState peaks = SolitaireGame.Create(SolitaireVariant.TriPeaks, 1);
        Assert.Equal(23, peaks.Piles["Stock"].Count);
        Assert.Single(peaks.Piles["Waste"]);
    }

    [Fact]
    public void NumberedFreeCellDealsAreDeterministicAndDistinct()
    {
        SolitaireGameState first = SolitaireGame.Create(SolitaireVariant.FreeCell, 617);
        SolitaireGameState replay = SolitaireGame.Create(SolitaireVariant.FreeCell, 617);
        SolitaireGameState other = SolitaireGame.Create(SolitaireVariant.FreeCell, 618);

        string Signature(SolitaireGameState game) => string.Join('|', Enumerable.Range(0, 8)
            .SelectMany(i => game.Piles[$"C{i}"])
            .Select(c => $"{(int)c.Card.Rank}:{(int)c.Card.Suit}"));

        Assert.Equal(Signature(first), Signature(replay));
        Assert.NotEqual(Signature(first), Signature(other));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    public void SpiderDifficultyUsesRequestedSuitCount(int suits, int expectedDistinctSuits)
    {
        SolitaireGameState game = SolitaireGame.Create(SolitaireVariant.Spider, 99, suits);
        Assert.Equal(expectedDistinctSuits, game.Piles.Values.SelectMany(p => p).Select(c => c.Card.Suit).Distinct().Count());
    }

    [Fact]
    public void PyramidClearsExposedKingWithoutSpeedOrScoreDependency()
    {
        SolitaireGameState game = SolitaireGame.Create(SolitaireVariant.Pyramid, 2);
        for (int i = 0; i < 28; i++) game.Piles[$"P{i}"].Clear();
        game.Piles["P21"].Add(new SolitaireCardState(new Card(Rank.King, Suit.Spades), true));

        Assert.True(SolitaireGame.TryActivateCard(game, "P21", 0));
        Assert.Empty(game.Piles["P21"]);
        Assert.Equal(1, game.Moves);
        Assert.True(game.Won);
    }

    [Fact]
    public void GolfAcceptsAdjacentRanksAndDoesNotWrapAceToKing()
    {
        SolitaireGameState game = SolitaireGame.Create(SolitaireVariant.Golf, 3);
        foreach (int i in Enumerable.Range(0, 7)) game.Piles[$"G{i}"].Clear();
        game.Piles["G0"].Add(new SolitaireCardState(new Card(Rank.Seven, Suit.Clubs), true));
        game.Piles["Waste"].Clear();
        game.Piles["Waste"].Add(new SolitaireCardState(new Card(Rank.Six, Suit.Hearts), true));
        Assert.True(SolitaireGame.TryActivateCard(game, "G0", 0));

        game.Won = false;
        game.Piles["G0"].Add(new SolitaireCardState(new Card(Rank.King, Suit.Clubs), true));
        game.Piles["Waste"].Clear();
        game.Piles["Waste"].Add(new SolitaireCardState(new Card(Rank.Ace, Suit.Hearts), true));
        Assert.False(SolitaireGame.TryActivateCard(game, "G0", 0));
    }

    [Fact]
    public void SolitaireProgressionRecordsStartWinAndRewardExactlyOnceAcrossReloads()
    {
        var profile = new PlayerProfileState();
        var progression = new SolitaireProgression(new ProgressionService(profile));
        Guid id = Guid.NewGuid();
        var start = new SolitaireStartRecord(id, SolitaireVariant.FreeCell, 1, 1234);
        var win = new SolitaireCompletionRecord(id, SolitaireVariant.FreeCell, 1, 1234, 900, 112);

        Assert.True(progression.ProcessStart(start));
        ProgressionAward first = progression.ProcessWin(win);
        Assert.Equal(400, first.PlayerExperience);
        Assert.Equal(200, first.SeasonExperience);
        Assert.Equal(1, profile.Solitaire.GamesStarted);
        Assert.Equal(1, profile.Solitaire.Wins);
        Assert.Equal(112, profile.Solitaire.FewestMoves);
        Assert.Equal(900, profile.Solitaire.FastestWinSeconds);
        Assert.Equal(ProgressionAward.None, progression.ProcessWin(win));

        PlayerProfileState restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        ProfileNormalization.Normalize(restored);
        ProgressionAward duplicateAfterReload = new SolitaireProgression(new ProgressionService(restored)).ProcessWin(win);
        Assert.Equal(ProgressionAward.None, duplicateAfterReload);
        Assert.Equal(1, restored.Solitaire.Wins);
        Assert.Equal(400, restored.LifetimeExperience);
    }

    [Fact]
    public void AbandonIsDurableButLeavingAnActiveDealIsNotAutomaticallyALoss()
    {
        var stats = new SolitaireStatistics();
        Guid id = Guid.NewGuid();
        Assert.True(stats.RecordStart(new(id, SolitaireVariant.Klondike, 1, 7)));
        Assert.Equal(0, stats.GamesAbandoned);
        Assert.True(stats.RecordAbandon(new(id, SolitaireVariant.Klondike, 1, 7, 120, 20)));
        Assert.Equal(1, stats.GamesAbandoned);
        Assert.False(stats.RecordAbandon(new(id, SolitaireVariant.Klondike, 1, 7, 150, 25)));
    }

    [Fact]
    public void FullCabinetAchievementRequiresAllSixVariants()
    {
        var stats = new SolitaireStatistics
        {
            KlondikeWins = 1, FreeCellWins = 1, SpiderWins = 1, PyramidWins = 1, GolfWins = 1, TriPeaksWins = 1,
        };
        SolitaireAchievementDefinition full = SolitaireAchievementCatalog.All.Single(a => a.Id == "solitaire-all");
        Assert.True(full.Complete(stats));
    }
}
