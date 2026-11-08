using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

/// <summary>All seven React destinations are represented without constructing WPF game factories.</summary>
public sealed class ModernLoungeCatalogTests
{
    [Fact]
    public void CatalogIsStableUniqueAndMatchesFrontendProtocolOrder()
    {
        Assert.Equal(1, ModernLoungeCatalog.SnapshotVersion);
        Assert.Equal(new[]
        {
            "sudoku", "solitaire", "word-search", "memory", "jigsaw", "tales-backroom", "tower-defense",
        }, ModernLoungeCatalog.GameIds);
        Assert.Equal(7, ModernLoungeCatalog.GameIds.Distinct(StringComparer.Ordinal).Count());
        Assert.All(ModernLoungeCatalog.GameIds, id => Assert.True(ModernLoungeCatalog.Contains(id)));
        Assert.False(ModernLoungeCatalog.Contains("legacy-sudoku"));
        Assert.False(ModernLoungeCatalog.Contains(null));
    }

    [Fact]
    public void SnapshotEvaluatesOnlyKnownModernRoutesAndNeverCallsLegacyFactory()
    {
        var called = new List<string>();
        IReadOnlyList<ModernLoungeGame> games = ModernLoungeCatalog.Create(id =>
        {
            called.Add(id);
            return id == "sudoku" ? null : "Native status " + id;
        });

        Assert.Equal(ModernLoungeCatalog.GameIds, called);
        Assert.Equal(ModernLoungeCatalog.GameIds, games.Select(game => game.Id));
        Assert.Null(games[0].Status);
        Assert.Equal("Native status tower-defense", games[^1].Status);
        Assert.Throws<ArgumentNullException>(() => ModernLoungeCatalog.Create(null!));
    }

    [Fact]
    public void ModernCatalogCannotBeMutatedByLegacyRegistryRegistration()
    {
        var before = ModernLoungeCatalog.GameIds.ToArray();
        var snapshot = ModernLoungeCatalog.Create(_ => "ready");
        Assert.Equal(before, ModernLoungeCatalog.GameIds);
        Assert.Equal(before, snapshot.Select(game => game.Id));
        Assert.All(snapshot, game => Assert.Equal("ready", game.Status));
    }
}
