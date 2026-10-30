using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Kniffel;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class KniffelCasinoTests
{
    private static KniffelState Sheet(int chance, int count = 13, Guid? id = null) => new()
    {
        GameId = id ?? Guid.NewGuid(),
        Entries = KniffelScoring.Categories.Take(count).Select(c => new KniffelEntry(c, c == KniffelCategory.Chance ? chance : 0)).ToArray(),
    };
    private static KniffelTableState Finished(int player, params int[] opponents)
    {
        var state = new KniffelTableState { Stake = 100, Player = Sheet(player), Opponents = opponents.Select(x => Sheet(x)).ToArray() };
        return state with { Settled = true, Placement = KniffelTable.Rank(state), Payout = KniffelTable.Prize(state) };
    }

    [Theory]
    [InlineData(25, 10, 15, 20, 1, 5250)]
    [InlineData(20, 25, 15, 10, 2, 2110)]
    [InlineData(15, 25, 20, 10, 3, 500)]
    [InlineData(10, 25, 20, 15, 4, 250)]
    [InlineData(25, 25, 15, 10, 1, 3680)]
    [InlineData(25, 25, 25, 25, 1, 2027.5)]
    public void PlacementAndOccupiedPrizeSplitsAreCorrect(int player, int a, int b, int c, int rank, double payout)
    {
        KniffelTableState result = Finished(player, a, b, c);
        KniffelTable.Validate(result);
        Assert.Equal(rank, result.Placement);
        Assert.Equal((decimal)payout, result.Payout);
    }

    [Fact]
    public void EntryAndPayoutArePersistedWithCashAndNeverAppliedTwice()
    {
        KniffelCashChange? disk = null;
        KniffelCashChange entry = KniffelCash.Start(10000, null, 100, change => disk = change);
        Assert.Equal(9900, entry.Bankroll);
        Assert.Same(entry, disk);
        var player = Sheet(25, id: entry.Table.Player.GameId);
        var pending = entry.Table with { Player = player, Opponents = entry.Table.Opponents.Select(s => Sheet(0, 12, s.GameId)).ToArray() };
        var finished = pending with { Opponents = pending.Opponents.Select(s => Sheet(5, id: s.GameId)).ToArray(), Settled = true, Placement = 1, Payout = 5250 };
        KniffelCashChange settled = KniffelCash.Apply(entry.Bankroll, pending, finished, change => disk = change);
        Assert.Equal(15150, settled.Bankroll);
        int writes = 0;
        KniffelCashChange duplicate = KniffelCash.Apply(settled.Bankroll, settled.Table, finished, _ => writes++);
        Assert.Equal(settled, duplicate);
        Assert.Equal(0, writes);
        Assert.Equal(15150, disk!.Bankroll);
    }

    [Fact]
    public void RejectedOrUnsavedEntriesAndResultsLeaveCallersStateIntact()
    {
        int writes = 0;
        Assert.Throws<InvalidOperationException>(() => KniffelCash.Start(99, null, 100, _ => writes++));
        Assert.Throws<ArgumentOutOfRangeException>(() => KniffelCash.Start(1000, null, 101, _ => writes++));
        KniffelTableState active = KniffelTable.Create(100);
        Assert.Throws<InvalidOperationException>(() => KniffelCash.Start(1000, active, 100, _ => writes++));
        Assert.Throws<IOException>(() => KniffelCash.Start(1000, null, 100, _ => throw new IOException()));
        Assert.Equal(0, writes);
        KniffelTableState forfeited = KniffelTable.Forfeit(active);
        Assert.Throws<IOException>(() => KniffelCash.Apply(900, active, forfeited, _ => throw new IOException()));
        Assert.False(active.Settled);
        Assert.Equal(0, active.Payout);
    }

    [Fact]
    public void AiUsesLegalHoldsAndKeepsFiveOfAKind()
    {
        foreach (KniffelAiStyle style in Enum.GetValues<KniffelAiStyle>())
        {
            var game = new KniffelGame(() => 6);
            game.Roll();
            KniffelAiChoice choice = KniffelAi.Choose(game, style);
            Assert.True(choice.ScoreNow);
            Assert.Equal(KniffelCategory.Kniffel, choice.Category);
            int rolls = 0;
            var source = new Random(751 + (int)style);
            var normal = new KniffelGame(() => { rolls++; return source.Next(1, 7); });
            KniffelAi.PlayTurn(normal, style);
            Assert.Equal(1, normal.FilledCount);
            Assert.InRange(rolls, 5, 15);
            Assert.Equal(0, normal.RollsUsed);
        }
    }

    [Fact]
    public void AiRecoveryIsDeterministicAndCannotChangePlayerDiceOrSheet()
    {
        var table = KniffelTable.Create(100);
        KniffelGame player = KniffelGame.Restore(table.Player, () => 3);
        player.Roll(); player.Commit(KniffelCategory.Threes);
        table = KniffelTable.WithPlayer(table, player.Snapshot);
        string before = JsonSerializer.Serialize(table.Player);
        KniffelTableState first = KniffelTable.Advance(table);
        KniffelTableState repeated = KniffelTable.Advance(JsonSerializer.Deserialize<KniffelTableState>(JsonSerializer.Serialize(table))!);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(repeated));
        Assert.Equal(before, JsonSerializer.Serialize(first.Player));
        Assert.All(first.Opponents, state => Assert.Single(state.Entries));
        Assert.All(table.Opponents, state => Assert.Empty(state.Entries));
    }

    [Fact]
    public void WholeCasinoGameSettlesOnceAfterThirteenTurnsAndCheckpointReloads()
    {
        var table = KniffelTable.Create(500);
        KniffelGame player = KniffelGame.Restore(table.Player, () => 2);
        foreach (KniffelCategory category in KniffelScoring.Categories)
        {
            player.Roll(); player.Commit(category);
            table = KniffelTable.WithPlayer(table, player.Snapshot);
            Assert.True(KniffelTable.NeedsOpponents(table));
            table = KniffelTable.Advance(table);
            table = JsonSerializer.Deserialize<KniffelTableState>(JsonSerializer.Serialize(table))!;
            KniffelTable.Validate(table);
            Assert.All(table.Opponents, state => Assert.Equal(player.FilledCount, state.Entries.Length));
        }
        Assert.True(table.Settled);
        Assert.False(KniffelTable.NeedsOpponents(table));
        Assert.Equal(KniffelTable.Prize(table), table.Payout);
        Assert.Equal(table, KniffelTable.Advance(table));
    }

    [Fact]
    public void ForfeitCountsStakeAndLossButNeverGrantsXpOrCompletedGameAchievements()
    {
        var service = new ProgressionService(new PlayerProfileState());
        var tracking = new KniffelProgression(service);
        var table = KniffelTable.Create(10000);
        tracking.ProcessTable(table);
        tracking.ProcessTable(table);
        KniffelTableState lost = KniffelTable.Forfeit(table);
        tracking.ProcessTable(lost); tracking.ProcessTable(lost);
        Assert.Equal(10000, service.State.Kniffel.TotalWagered);
        Assert.Equal(-10000, service.State.Kniffel.NetProfit);
        Assert.Equal(1, service.State.Kniffel.Forfeits);
        Assert.Equal(0, service.State.Kniffel.CompletedGames);
        Assert.Equal(0, service.State.LifetimeExperience);
        Assert.Equal(0, ChallengeCatalog.Progress(service.State, ChallengeCatalog.TryGet("dice-table-1")!));
    }

    [Fact]
    public void TableStatisticsRewardsAndAchievementsAreSeparateAndIdempotent()
    {
        using var controller = new PlayerProgressionController(new PlayerProfileState());
        KniffelTableState result = Finished(25, 10, 15, 20);
        ProgressionAward award = controller.ProcessKniffelTable(result);
        Assert.Equal(250, award.PlayerExperience);
        var profile = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(controller.Profile))!;
        Assert.Equal(ProgressionAward.None, new KniffelProgression(new ProgressionService(profile)).ProcessTable(result));
        Assert.Equal(1, profile.Kniffel.CasinoGamesCompleted);
        Assert.Equal(1, profile.Kniffel.CompletedGames);
        Assert.Equal(1, profile.Kniffel.CasinoWins);
        Assert.Equal(100, profile.Kniffel.TotalWagered);
        Assert.Equal(5250, profile.Kniffel.TotalReturned);
        Assert.Equal(0, profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(1, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("dice-win-1")!));
        long coins = controller.Profile.Coins;
        Assert.True(controller.ClaimChallenge("dice-win-1").Success);
        Assert.False(controller.ClaimChallenge("dice-win-1").Success);
        Assert.Equal(coins + 150, controller.Profile.Coins);
    }

    [Fact]
    public void MalformedAndStaleTableCheckpointsAreRejected()
    {
        KniffelTableState state = KniffelTable.Create(100);
        Assert.Throws<ArgumentException>(() => KniffelTable.Validate(state with { Opponents = [] }));
        Assert.Throws<ArgumentException>(() => KniffelTable.Validate(state with { Settled = true, Payout = 1 }));
        Assert.Throws<InvalidOperationException>(() => KniffelCash.Apply(900, state, KniffelTable.Forfeit(KniffelTable.Create(100)), _ => { }));
        Assert.Throws<InvalidOperationException>(() => KniffelTable.WithPlayer(state, new KniffelGame().Snapshot));
    }
}
