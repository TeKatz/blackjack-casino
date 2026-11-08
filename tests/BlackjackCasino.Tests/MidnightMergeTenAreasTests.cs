using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Tests;

public sealed class MidnightMergeTenAreasTests
{
    [Fact]
    public void TenDistinctRoomsProgressThroughFortyPersistentRenovationStages()
    {
        MergeCatalog catalog = MergeCatalog.Default;
        Assert.Equal(10, catalog.Areas.Count);
        Assert.Equal(40, catalog.Areas.Sum(area => area.Milestones.Count));
        Assert.Equal(new long[] { 0, 100, 2500, 5600, 9900, 15200, 21800, 29500, 38800, 49800 },
            catalog.Areas.Select(area => area.Milestones[0].Points));
        Assert.Equal("lobby", MergeProgression.ActiveArea(catalog, 0).Id);
        Assert.Equal("bar", MergeProgression.ActiveArea(catalog, 100).Id);
        Assert.Equal("gaming-floor", MergeProgression.ActiveArea(catalog, 2500).Id);
        Assert.Equal("observatory", MergeProgression.ActiveArea(catalog, 49800).Id);
        Assert.Equal("ready", MergeProgression.Current(catalog.Areas[^1], 63600).Id);
        Assert.Null(MergeProgression.Next(catalog.Areas[^1], 63600));
        foreach (MergeArea area in catalog.Areas)
        {
            Assert.Equal(4, area.Milestones.Select(x => x.Id).Distinct().Count());
            Assert.True(area.Milestones.Zip(area.Milestones.Skip(1), (a,b) => a.Points < b.Points).All(x => x));
            Assert.Contains(catalog.Orders, order => order.AreaId == area.Id);
        }
    }

    [Fact]
    public void NewProducerAndDeliveryCannotBypassLockedArea()
    {
        MergeCatalog catalog = MergeCatalog.Default;
        var locked = new MergeBoard(new MergeGameState
        {
            Items = [new(Guid.NewGuid(), "decor-3", 0), new(Guid.NewGuid(), "chip-3", 1)],
        });
        Assert.False(locked.CanSpawn("decor-cabinet"));
        Assert.Equal(MergeOutcome.Locked, locked.TrySpawn("decor-cabinet").Outcome);
        var order = locked.Snapshot.Orders.Single(x => x.DefinitionId == "floor-brass");
        Assert.Equal(MergeOutcome.Locked, locked.TryComplete(order.Id).Outcome);
        Assert.Empty(locked.Snapshot.Receipts);
        Assert.Equal(0, locked.Snapshot.RenovationPoints);

        var unlocked = new MergeBoard(new MergeGameState
        {
            RenovationPoints = 2500,
            Items = [new(Guid.NewGuid(), "decor-3", 0), new(Guid.NewGuid(), "chip-3", 1)],
        });
        Assert.True(unlocked.CanSpawn("decor-cabinet"));
        order = unlocked.Snapshot.Orders.Single(x => x.DefinitionId == "floor-brass");
        Assert.True(unlocked.TryComplete(order.Id).Success);
        Assert.Equal(2840, unlocked.Snapshot.RenovationPoints);
        Assert.Equal(MergeOutcome.AlreadyCompleted, unlocked.TryComplete(order.Id).Outcome);
    }

    [Fact]
    public void ExistingSavesGainNewOrdersAndProducersWithoutLosingIdentitiesOrReceipts()
    {
        var original = new MergeBoard(new MergeGameState
        {
            RenovationPoints = 2700,
            Items = [new(Guid.NewGuid(), "chip-2", 4)],
        }).Snapshot;
        Guid chip = original.Items.Single().Id;
        Guid firstOrder = original.Orders.Single(x => x.DefinitionId == "first-stack").Id;
        var retained = original with
        {
            Orders = original.Orders.Where(x => x.DefinitionId == "first-stack").ToList(),
            Producers = original.Producers.Where(x => x.DefinitionId is "chip-case" or "bar-cart").ToList(),
        };
        MergeGameState migrated = MergeGameState.Normalize(retained);
        Assert.Equal(chip, migrated.Items.Single().Id);
        Assert.Equal(firstOrder, migrated.Orders.Single(x => x.DefinitionId == "first-stack").Id);
        Assert.Equal(2700, migrated.RenovationPoints);
        Assert.Contains(migrated.Orders, x => x.DefinitionId == "observatory-unveiling");
        Assert.Contains(migrated.Producers, x => x.DefinitionId == "sky-kit");
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(migrated),
            System.Text.Json.JsonSerializer.Serialize(MergeGameState.Normalize(migrated)));
    }

    [Fact]
    public void EveryNewMaxTierHasAnAvailableSoloDeliveryOnceItsAreaUnlocks()
    {
        var cases = new[] { ("decor-6", "floor-centerpiece", 2500L), ("garden-6", "garden-living-wall", 5600L),
            ("stage-6", "theater-curtain", 21800L), ("sky-6", "observatory-unveiling", 49800L) };
        foreach(var (item, orderId, points) in cases)
        {
            var board = new MergeBoard(new MergeGameState
            {
                RenovationPoints = points,
                Items = [new(Guid.NewGuid(), item, 0)],
            });
            MergeOrderInstance order = board.Snapshot.Orders.Single(x => x.DefinitionId == orderId);
            Assert.True(board.TryComplete(order.Id).Success);
            Assert.Empty(board.Snapshot.Items);
        }
    }
}
