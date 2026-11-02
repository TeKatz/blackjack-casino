using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class PlayerProfileTransactionSnapshotTests
{
    [Fact]
    public void SolitaireSnapshotIsolatesMutableProgressionStateButSharesUntouchedStatistics()
    {
        var source = new PlayerProfileState();
        _ = new ProgressionService(source);
        SeasonProgressState sourceSeason = source.Seasons[source.ActiveSeasonId];
        sourceSeason.Experience = 123;

        PlayerProfileState candidate = PlayerProfileTransactionSnapshot.Create(
            source,
            RoutineProfileTransactionArea.Solitaire);

        Assert.NotSame(source.Solitaire, candidate.Solitaire);
        Assert.Same(source.Sudoku, candidate.Sudoku);
        Assert.NotSame(source.Collection, candidate.Collection);
        Assert.NotSame(source.Seasons, candidate.Seasons);
        Assert.NotSame(source.OwnedCosmeticIds, candidate.OwnedCosmeticIds);
        Assert.NotSame(source.ClaimedChallengeIds, candidate.ClaimedChallengeIds);

        candidate.Seasons[source.ActiveSeasonId].Experience += 50;
        candidate.OwnedCosmeticIds.Add("transaction-only-marker");

        Assert.Equal(123, source.Seasons[source.ActiveSeasonId].Experience);
        Assert.DoesNotContain("transaction-only-marker", source.OwnedCosmeticIds);
    }

    [Fact]
    public void KniffelSnapshotAlsoIsolatesVaultBecauseSyncMutatesIt()
    {
        var source = new PlayerProfileState();
        _ = new ProgressionService(source);

        PlayerProfileState candidate = PlayerProfileTransactionSnapshot.Create(
            source,
            RoutineProfileTransactionArea.Kniffel);

        Assert.NotSame(source.Kniffel, candidate.Kniffel);
        Assert.NotSame(source.Vault, candidate.Vault);
        Assert.Same(source.Memory, candidate.Memory);
    }

    [Fact]
    public void NormalizedSnapshotServiceDoesNotRunDefaultNormalizationAgain()
    {
        var snapshot = new PlayerProfileState
        {
            OwnedCosmeticIds = [],
            ClaimedChallengeIds = [],
        };

        ProgressionService service = ProgressionService.ForNormalizedSnapshot(snapshot);

        Assert.Same(snapshot, service.State);
        Assert.Empty(snapshot.OwnedCosmeticIds);
    }
}
