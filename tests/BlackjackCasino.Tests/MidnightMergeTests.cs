using System.Text.Json;
using BlackjackCasino.Core.MidnightMerge;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class MidnightMergeTests
{
    private static MergeItemInstance Chip(int tier, int position) => new(Guid.NewGuid(), $"chip-{tier}", position);
    private static MergeBoard With(params MergeItemInstance[] items) => new(new() { Items = [.. items] });

    [Fact]
    public void MovePreservesIdentityAndRejectsOccupiedOrOutOfRangeCells()
    {
        var a = Chip(1, 0); var b = Chip(2, 1); var board = With(a, b);
        Assert.False(board.CanMove(a.Id, 1));
        Assert.Equal(MergeOutcome.Occupied, board.TryMove(a.Id, 1).Outcome);
        Assert.Equal(MergeOutcome.InvalidPosition, board.TryMove(a.Id, 42).Outcome);
        Assert.True(board.CanMove(a.Id, 12));
        Assert.True(board.TryMove(a.Id, 12).Success);
        Assert.Equal(a with { Position = 12 }, board.Snapshot.Items.Single(x => x.Id == a.Id));
        Assert.Equal(2, board.Snapshot.Items.Count);
    }

    [Theory]
    [InlineData(1, 1, MergeOutcome.Merged)]
    [InlineData(1, 2, MergeOutcome.Incompatible)]
    [InlineData(6, 6, MergeOutcome.MaxTier)]
    public void MergeRulesValidateTierAndReplaceExactlyTwoItems(int a, int b, MergeOutcome outcome)
    {
        var source = Chip(a, 0); var target = Chip(b, 1); var untouched = Chip(4, 7);
        var board = With(source, target, untouched);
        Assert.Equal(outcome == MergeOutcome.Merged, board.CanMerge(source.Id, 1));
        Assert.Equal(outcome, board.TryMerge(source.Id, 1).Outcome);
        if (outcome == MergeOutcome.Merged)
        {
            Assert.Equal(2, board.Snapshot.Items.Count);
            Assert.DoesNotContain(board.Snapshot.Items, x => x.Id == source.Id || x.Id == target.Id);
            Assert.Equal("chip-2", board.Snapshot.Items.Single(x => x.Position == 1).DefinitionId);
            Assert.Equal(untouched, board.Snapshot.Items.Single(x => x.Id == untouched.Id));
            Assert.Equal(MergeOutcome.MissingItem, board.TryMerge(source.Id, 1).Outcome);
        }
        else Assert.Equal(new[] { source, target, untouched }, board.Snapshot.Items);
    }

    [Fact]
    public void DifferentLinesAndSelfMergeAreRejectedByCore()
    {
        var other = MergeCatalog.Default.Items["chip-1"] with { Id = "decor-1", LineId = "decorations", MergeResult = null };
        var catalog = new MergeCatalog(MergeCatalog.Default.Items.Values.Append(other), MergeCatalog.Default.Producers.Values,
            MergeCatalog.Default.Orders, MergeCatalog.Default.Areas);
        var a = Chip(1, 0); var b = new MergeItemInstance(Guid.NewGuid(), "decor-1", 1);
        var board = new MergeBoard(new() { Items = [a, b] }, catalog: catalog);
        Assert.Equal(MergeOutcome.Incompatible, board.TryMerge(a.Id, 1).Outcome);
        Assert.Equal(MergeOutcome.Incompatible, board.TryMerge(a.Id, 0).Outcome);
        Assert.Equal(2, board.Snapshot.Items.Count);
    }

    [Fact]
    public void ProducerFillsOnlyFreeCellsAndFullBoardNeverCreatesDuplicates()
    {
        var board = new MergeBoard();
        Parallel.For(0, 100, _ => board.TrySpawn());
        Assert.Equal(42, board.Snapshot.Items.Count);
        Assert.Equal(42, board.Snapshot.Items.Select(x => x.Position).Distinct().Count());
        Assert.Equal(42, board.Snapshot.Items.Select(x => x.Id).Distinct().Count());
        Assert.All(board.Snapshot.Items, x => Assert.Equal("chip-1", x.DefinitionId));
        Assert.Equal(42, board.Snapshot.Producers.Single(x => x.DefinitionId == "chip-case").SpawnCount);
        Assert.False(board.CanSpawn("chip-case"));
        Assert.Equal(MergeOutcome.BoardFull, board.TrySpawn().Outcome);
    }

    [Fact]
    public void BarSupplyUnlocksAtLobbyCompletionAndCreatesItsOwnMergeLine()
    {
        var locked = new MergeBoard(new() { RenovationPoints = 99 });
        Assert.False(locked.CanSpawn("bar-cart"));
        Assert.Equal(MergeOutcome.Locked, locked.TrySpawn("bar-cart").Outcome);

        var unlocked = new MergeBoard(new() { RenovationPoints = 100 });
        Assert.True(unlocked.CanSpawn("bar-cart"));
        Assert.True(unlocked.TrySpawn("bar-cart").Success);
        Assert.True(unlocked.TrySpawn("bar-cart").Success);
        MergeItemInstance source = unlocked.Snapshot.Items.Single(x => x.Position == 0);
        Assert.True(unlocked.TryMerge(source.Id, 1).Success);
        Assert.Equal("drink-2", unlocked.Snapshot.Items.Single().DefinitionId);
        Assert.Equal(2, MergeCatalog.Default.Items["drink-2"].Tier);
        Assert.Equal(8, MergeCatalog.Default.Items["drink-8"].MaxTier);
    }

    [Fact]
    public void LobbyFlowsIntoLongerBarAreaWithoutMovingOldThresholds()
    {
        MergeCatalog catalog = MergeCatalog.Default;
        Assert.Equal("lobby", MergeProgression.ActiveArea(catalog, 99).Id);
        Assert.Equal("bar", MergeProgression.ActiveArea(catalog, 100).Id);
        MergeArea bar = catalog.Areas.Single(x => x.Id == "bar");
        Assert.Equal(new long[] { 100, 500, 1200, 2500 }, bar.Milestones.Select(x => x.Points));
        Assert.Equal(0, MergeProgression.AreaProgress(bar, 100));
        Assert.Equal(1, MergeProgression.AreaProgress(bar, 2500));
        Assert.Contains(catalog.Orders, x => x.Id == "bar-midnight-reserve" && x.Reward == 900);
    }

    [Fact]
    public void ConcurrentMergeConsumesSourceOnlyOnce()
    {
        var a = Chip(1, 0); var board = With(a, Chip(1, 1), Chip(1, 2));
        Parallel.For(1, 3, target => board.TryMerge(a.Id, target));
        Assert.Equal(2, board.Snapshot.Items.Count);
        Assert.Single(board.Snapshot.Items, x => x.DefinitionId == "chip-2");
        Assert.Equal(1, board.Snapshot.Revision);
    }

    [Fact]
    public void OrdersRejectMissingItemsConsumeExactRequirementsAndAwardOnceEvenAfterReload()
    {
        var a = Chip(2, 0); var b = Chip(2, 1); var keep = Chip(4, 2);
        var board = With(a, b, keep);
        var order = board.Snapshot.Orders.Single(x => x.DefinitionId == "paired-stacks");
        var unavailable = board.Snapshot.Orders.Single(x => x.DefinitionId == "blue-table");
        Assert.Equal(MergeOutcome.MissingItems, board.TryComplete(unavailable.Id).Outcome);
        Parallel.For(0, 10, _ => board.TryComplete(order.Id));
        Assert.Equal(new[] { keep }, board.Snapshot.Items);
        Assert.Equal(22, board.Snapshot.RenovationPoints);
        Assert.Single(board.Snapshot.Receipts);
        Assert.NotEqual(order.Id, board.Snapshot.Orders.Single(x => x.DefinitionId == order.DefinitionId).Id);
        board = new(JsonSerializer.Deserialize<MergeGameState>(JsonSerializer.Serialize(board.Snapshot)));
        Assert.Equal(MergeOutcome.AlreadyCompleted, board.TryComplete(order.Id).Outcome);
        Assert.Equal(22, board.Snapshot.RenovationPoints);
    }

    [Fact]
    public void MilestonesUnlockOnceAndRemainStableAfterNormalize()
    {
        var board = With(Chip(3, 0), Chip(4, 1), Chip(5, 2));
        var result = board.TryComplete(board.Snapshot.Orders.Single(x => x.DefinitionId == "blue-table").Id);
        Assert.Equal(new[] { "lobby/cleanup" }, result.Milestones);
        result = board.TryComplete(board.Snapshot.Orders.Single(x => x.DefinitionId == "violet-table").Id);
        Assert.Equal(new[] { "lobby/lights" }, result.Milestones);
        result = board.TryComplete(board.Snapshot.Orders.Single(x => x.DefinitionId == "gold-table").Id);
        Assert.Equal(new[] { "lobby/ready", "bar/closed" }, result.Milestones);
        var state = board.Snapshot;
        Assert.Equal(170, state.RenovationPoints);
        Assert.Equal("ready", MergeProgression.Current(MergeCatalog.Default.Areas[0], state.RenovationPoints).Id);
        Assert.Equal("bar", MergeProgression.ActiveArea(MergeCatalog.Default, state.RenovationPoints).Id);
        Assert.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(MergeGameState.Normalize(state)));
    }

    [Fact]
    public void SerializedSnapshotAndPreferencesKeepBoardOrdersProducersReceiptsAndIds()
    {
        var board = new MergeBoard();
        board.TrySpawn(); board.TrySpawn();
        var source = board.Snapshot.Items[0]; board.TryMerge(source.Id, 1);
        board.TryComplete(board.Snapshot.Orders.Single(x => x.DefinitionId == "first-stack").Id);
        board.TrySpawn(); board.TryMove(board.Snapshot.Items.Single().Id, 41);
        string before = JsonSerializer.Serialize(board.Snapshot);
        var preferences = new AppPreferences { MidnightMerge = board.Snapshot };
        var loaded = JsonSerializer.Deserialize<AppPreferences>(JsonSerializer.Serialize(preferences))!;
        var resumed = new MergeBoard(loaded.MidnightMerge);
        Assert.Equal(before, JsonSerializer.Serialize(resumed.Snapshot));
        Assert.Equal(before, JsonSerializer.Serialize(MergeGameState.Normalize(resumed.Snapshot)));
        var copy = resumed.Snapshot; copy.Items.Clear(); copy.Receipts.Clear();
        Assert.Equal(before, JsonSerializer.Serialize(resumed.Snapshot));
    }

    [Fact]
    public void AtomicDiskSnapshotKeepsMergeAndBankrollSeparate()
    {
        string folder = Path.Combine(Path.GetTempPath(), "merge-test-" + Guid.NewGuid());
        try
        {
            var board = With(Chip(2, 3));
            board.TryComplete(board.Snapshot.Orders.Single(x => x.DefinitionId == "first-stack").Id);
            var preferences = new AppPreferences { Bankroll = 1234, MidnightMerge = board.Snapshot };
            PlayerStateStore.SaveOrThrow(preferences, folder);
            var saved = PlayerStateStore.Load(folder);
            Assert.Equal(1234, saved.Bankroll);
            Assert.Equal(JsonSerializer.Serialize(board.Snapshot), JsonSerializer.Serialize(saved.MidnightMerge));
            Assert.False(File.Exists(Path.Combine(folder, "player-state.json.tmp")));
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void FailedPersistenceRollsBackSpawnMergeAndOrderAndAllowsRetry()
    {
        bool fail = true;
        var a = Chip(1, 0);
        var board = new MergeBoard(new() { Items = [a, Chip(1, 1), Chip(2, 2)] }, _ => { if (fail) throw new IOException("disk full"); });
        var order = board.Snapshot.Orders.Single(x => x.DefinitionId == "first-stack");
        string before = JsonSerializer.Serialize(board.Snapshot);
        Assert.Throws<IOException>(() => board.TrySpawn());
        Assert.Throws<IOException>(() => board.TryMerge(a.Id, 1));
        Assert.Throws<IOException>(() => board.TryComplete(order.Id));
        Assert.Equal(before, JsonSerializer.Serialize(board.Snapshot));
        fail = false;
        Assert.True(board.TryComplete(order.Id).Success);
        Assert.Equal(10, board.Snapshot.RenovationPoints);
        Assert.Single(board.Snapshot.Receipts);
    }

    [Fact]
    public void NormalizeRepairsConflictsWithoutChangingValidIdsAndRejectsFutureSchemas()
    {
        var a = Chip(1, 0); var b = Chip(3, 0); var c = Chip(6, -8);
        var state = MergeGameState.Normalize(new() { Items = [a, b, c, a], RenovationPoints = -1 });
        Assert.Equal(3, state.Items.Count);
        Assert.Equal(new[] { a.Id, b.Id, c.Id }, state.Items.Select(x => x.Id));
        Assert.Equal(new[] { 0, 1, 2 }, state.Items.Select(x => x.Position));
        Assert.Equal(0, state.RenovationPoints);
        Assert.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(MergeGameState.Normalize(state)));
        Assert.Throws<NotSupportedException>(() => MergeGameState.Normalize(state with { SchemaVersion = 999 }));
    }

    [Fact]
    public void EveryMaxTierItemHasAnOrderSoAFullBoardCanRecover()
    {
        foreach (var item in MergeCatalog.Default.Items.Values.Where(x => x.MergeResult is null))
        {
            var board = new MergeBoard(new MergeGameState
            {
                RenovationPoints = 49800,
                Items = Enumerable.Range(0, 42).Select(p => new MergeItemInstance(Guid.NewGuid(), item.Id, p)).ToList(),
            });
            var order = board.Snapshot.Orders.First(x => MergeOrders.SelectRequired(board.Snapshot, MergeCatalog.Default.Orders.Single(d => d.Id == x.DefinitionId)) is not null);
            Assert.True(board.TryComplete(order.Id).Success);
            Assert.True(board.CanSpawn("chip-case"));
        }
    }

    [Fact]
    public void SynthesizedCuesAreDistinctFiniteAndBounded()
    {
        var clips = Enum.GetValues<MergeCue>().Select(MergeSoundSynthesis.Build).ToArray();
        Assert.Equal(8, clips.Select(x => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Runtime.InteropServices.MemoryMarshal.AsBytes(x.Samples.AsSpan())))).Distinct().Count());
        Assert.All(clips, clip => Assert.All(clip.Samples, sample => Assert.True(float.IsFinite(sample) && Math.Abs(sample) <= .8f)));
    }
}
