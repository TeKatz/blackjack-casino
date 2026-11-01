using BlackjackCasino.Core;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Tests;

public sealed class MidnightMergeProgressionTests
{
    [Fact]
    public void BoardTracksMergesAndHighestTierAndMigratesSchemaOneSaves()
    {
        var board = new MergeBoard();
        Assert.True(board.TrySpawn().Success);
        Assert.True(board.TrySpawn().Success);
        Guid source = board.Snapshot.Items.Single(x => x.Position == 0).Id;
        Assert.True(board.TryMerge(source, 1).Success);

        MergeGameState state = board.Snapshot;
        Assert.Equal(MergeGameState.CurrentSchemaVersion, state.SchemaVersion);
        Assert.Equal(1, state.TotalMerges);
        Assert.Equal(2, state.HighestTierCreated);

        var legacy = new MergeGameState
        {
            SchemaVersion = 1,
            Items = [new MergeItemInstance(Guid.NewGuid(), "chip-2", 0)],
            Producers = [new MergeProducerState("chip-case", 2)],
        };
        MergeGameState migrated = MergeGameState.Normalize(legacy);
        Assert.Equal(2, migrated.SchemaVersion);
        Assert.Equal(1, migrated.TotalMerges);
        Assert.Equal(2, migrated.HighestTierCreated);
    }

    [Fact]
    public void ProfileSyncRewardsDurableMergeProgressExactlyOnce()
    {
        var board = new MergeBoard(new MergeGameState
        {
            Items = [new MergeItemInstance(Guid.NewGuid(), "chip-6", 0)],
        });
        MergeOrderInstance vip = board.Snapshot.Orders.Single(x => x.DefinitionId == "vip-table");
        Assert.True(board.TryComplete(vip.Id).Success);
        Assert.Equal(210, board.Snapshot.RenovationPoints);

        var profile = new PlayerProfileState();
        var progression = new MergePlayerProgression(new ProgressionService(profile));
        MergeProfileSyncResult first = progression.Sync(board.Snapshot);

        Assert.True(first.Changed);
        Assert.Equal(1, first.OrdersRewarded);
        Assert.Equal(3, first.MilestonesRewarded);
        Assert.Equal(1, first.AreasRewarded);
        Assert.Equal(2095, first.Award.PlayerExperience);
        Assert.Equal(1047, first.Award.SeasonExperience);
        Assert.Equal(1, profile.MidnightMerge.OrdersCompleted);
        Assert.Equal(210, profile.MidnightMerge.RenovationPointsEarned);
        Assert.Equal(3, profile.MidnightMerge.MilestonesReached);
        Assert.Equal(1, profile.MidnightMerge.AreasCompleted);
        Assert.Equal(6, profile.MidnightMerge.HighestTierCreated);

        long lifetimeXp = profile.LifetimeExperience;
        MergeProfileSyncResult second = progression.Sync(board.Snapshot);
        Assert.False(second.Changed);
        Assert.Equal(0, second.Award.PlayerExperience);
        Assert.Equal(lifetimeXp, profile.LifetimeExperience);
    }

    [Fact]
    public void MergeChallengesReadFromTheGlobalPlayerProfile()
    {
        var profile = new PlayerProfileState
        {
            MidnightMerge = new MidnightMergeStatistics
            {
                Merges = 120,
                OrdersCompleted = 26,
                RenovationPointsEarned = 140,
                HighestTierCreated = 6,
                MilestonesReached = 3,
                AreasCompleted = 1,
            },
        };

        ChallengeDefinition firstMerge = ChallengeCatalog.TryGet("merge-first")!;
        ChallengeDefinition reliableSupplier = ChallengeCatalog.TryGet("merge-order-25")!;
        ChallengeDefinition grandReopening = ChallengeCatalog.TryGet("merge-area-1")!;

        Assert.Equal(ChallengeCategory.MidnightMerge, firstMerge.Category);
        Assert.True(ChallengeCatalog.Complete(profile, firstMerge));
        Assert.True(ChallengeCatalog.Complete(profile, reliableSupplier));
        Assert.True(ChallengeCatalog.Complete(profile, grandReopening));
        Assert.Equal(16, ChallengeCatalog.For(ChallengeCategory.MidnightMerge).Count);
    }
}
