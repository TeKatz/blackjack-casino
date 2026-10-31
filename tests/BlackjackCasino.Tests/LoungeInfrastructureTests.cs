using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class LoungeInfrastructureTests
{
    [Fact]
    public void LoungeProgressionGrantsPlayerAndSeasonRewardsOnlyAfterDurableReceipt()
    {
        var state = new PlayerProfileState();
        var progression = new ProgressionService(state);
        bool recorded = false;

        ProgressionAward first = LoungeProgression.GrantOnce(
            progression,
            () => !recorded && (recorded = true),
            playerExperience: 400,
            seasonExperience: 200);

        Assert.Equal(400, first.PlayerExperience);
        Assert.Equal(200, first.SeasonExperience);
        Assert.Equal(400, state.LifetimeExperience);
        Assert.Equal(200, progression.GetOrCreateSeasonProgress(state.ActiveSeasonId).Experience);

        ProgressionAward duplicate = LoungeProgression.GrantOnce(
            progression,
            () => !recorded && (recorded = true),
            playerExperience: 400,
            seasonExperience: 200);

        Assert.Equal(ProgressionAward.None, duplicate);
        Assert.Equal(400, state.LifetimeExperience);
        Assert.Equal(200, progression.GetOrCreateSeasonProgress(state.ActiveSeasonId).Experience);
    }

    [Fact]
    public void LoungeProgressionRejectsNegativeRewardsBeforeRecordingCompletion()
    {
        var progression = new ProgressionService(new PlayerProfileState());
        bool recorded = false;

        Assert.Throws<ArgumentOutOfRangeException>(() => LoungeProgression.GrantOnce(
            progression,
            () => recorded = true,
            playerExperience: -1,
            seasonExperience: 0));

        Assert.False(recorded);
    }

    [Fact]
    public void LoungeCatalogSortsGamesAndRejectsDuplicateIds()
    {
        LoungeGame later = new("mahjong", "Mahjong", "Tiles", "Match pairs.", () => { }, SortOrder: 20);
        LoungeGame first = new("solitaire", "Solitaire", "Cards", "Play cards.", () => { }, SortOrder: 10);

        IReadOnlyList<LoungeGame> games = LoungeGameCatalog.Normalize([later, first]);

        Assert.Equal(["solitaire", "mahjong"], games.Select(game => game.Id));
        Assert.Throws<InvalidOperationException>(() => LoungeGameCatalog.Normalize([
            first,
            first with { Title = "Duplicate" },
        ]));
    }

    [Fact]
    public void LoungeCatalogAllowsDynamicStatusWithoutCouplingMenuToGameState()
    {
        int completed = 12;
        LoungeGame game = new(
            "word-search",
            "Word Search",
            "Find your pace.",
            "Search themed boards.",
            () => { },
            Status: () => $"{completed} completed");

        LoungeGame normalized = Assert.Single(LoungeGameCatalog.Normalize([game]));
        Assert.Equal("12 completed", normalized.Status?.Invoke());

        completed = 13;
        Assert.Equal("13 completed", normalized.Status?.Invoke());
    }
}
