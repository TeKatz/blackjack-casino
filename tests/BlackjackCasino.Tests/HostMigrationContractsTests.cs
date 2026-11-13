using BlackjackCasino.Core.Hosting;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class HostMigrationContractsTests
{
    [Fact]
    public void CanonicalLobbyRoutesAreCompleteAndUnique()
    {
        string[] expected =
        [
            "blackjack", "roulette", "midnight-reels", "neon-vault", "nine-lives",
            "midnight-manor", "casino-empire", "midnight-merge", "chill-lounge",
            "kniffel", "midnight-descent",
        ];
        Assert.Equal(expected, HostMigrationCatalog.CasinoRoutes.Select(route => route.DesktopModeId));
        Assert.Equal(expected.Length,
            HostMigrationCatalog.CasinoRoutes.Select(route => route.DesktopModeId)
                .Distinct(StringComparer.Ordinal).Count());
        Assert.True(HostMigrationCatalog.CasinoRoutes.Single(route =>
            route.DesktopModeId == "casino-empire").HasSecondaryWindow);
        Assert.True(HostMigrationCatalog.CasinoRoutes.Single(route =>
            route.DesktopModeId == "midnight-descent").HasNativeProcess);
    }

    [Theory]
    [InlineData("blackjack", "blackjack.html")]
    [InlineData("roulette", "roulette.html")]
    [InlineData("casino-empire", "empire.html")]
    [InlineData("chill-lounge", "lounge.html")]
    [InlineData("reels", "reels.html")]
    [InlineData("neon-vault", "neon-vault.html")]
    [InlineData("nine-lives", "nine-lives.html")]
    [InlineData("midnight-merge", "midnight-merge.html")]
    [InlineData("midnight-dice", "midnight-dice.html")]
    [InlineData("midnight-manor", "index.html")]
    public void PreviewRoutesHaveExactDocumentAllowlist(string id, string document)
    {
        Assert.True(HostMigrationCatalog.IsPreviewRoute(id));
        Assert.Equal(document, HostMigrationCatalog.PreviewPageFor(id));
    }

    [Fact]
    public void PreviewIsNotPromotedToAProductionLobby()
    {
        Assert.Equal(10, HostMigrationCatalog.PreviewRoutes.Count);
        Assert.True(HostMigrationCatalog.IsPreviewRoute("roulette"));
        Assert.True(HostMigrationCatalog.IsPreviewRoute("casino-empire"));
        Assert.True(HostMigrationCatalog.IsPreviewRoute("chill-lounge"));
        Assert.False(HostMigrationCatalog.IsPreviewRoute("kniffel"));
        Assert.False(HostMigrationCatalog.IsPreviewRoute("../lobby"));
        Assert.False(HostMigrationCatalog.IsPreviewRoute(null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HostMigrationCatalog.PreviewPageFor("lobby"));
        Assert.Equal(8, HostMigrationCatalog.LoungeGameIds.Count);
        Assert.Equal(8, HostMigrationCatalog.LoungeGameIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ReleaseNeedsExplicitParityEvidence()
    {
        Assert.False(HostMigrationCatalog.ApprovedForRelease);
        Assert.Equal(9, HostMigrationCatalog.ReleaseRequirements.Count);
        Assert.All(HostMigrationCatalog.ReleaseRequirements, requirement =>
        {
            Assert.False(requirement.Approved);
            Assert.False(string.IsNullOrWhiteSpace(requirement.Acceptance));
        });
    }

    [Fact]
    public void ExistingCasinoRegistryImplementsNeutralNavigationPort()
    {
        var registry = new CasinoModeRegistry();
        int primary = 0, secondary = 0;
        registry.Register(new CasinoMode("casino-empire", "Empire", "Sub", "Desc",
            "#ABCDEF", "♜", "Casino"), () => primary++,
            secondaryOpen: () => secondary++, secondaryLabel: "DETACH");
        IHostModeNavigator navigator = registry;
        Assert.True(navigator.CanOpen("casino-empire"));
        Assert.True(navigator.CanOpenSecondary("casino-empire"));
        navigator.Open("casino-empire");
        navigator.OpenSecondary("casino-empire");
        navigator.Open("unknown");
        Assert.Equal(1, primary);
        Assert.Equal(1, secondary);
    }

    [Fact]
    public void PreviewStorageRejectsLiveRootAndSiblingSubfolders()
    {
        string local = Path.Combine(Path.GetTempPath(), "host-contract-test", "Local");
        string live = Path.Combine(local, "BlackjackCasino");
        Assert.Throws<InvalidOperationException>(() =>
            HostPreviewStorage.RequireIsolatedDirectory(live, local));
        Assert.Throws<InvalidOperationException>(() =>
            HostPreviewStorage.RequireIsolatedDirectory(Path.Combine(live, "Backups"), local));
        Assert.Throws<InvalidOperationException>(() =>
            HostPreviewStorage.RequireIsolatedDirectory(local, local));
        string traversal = Path.Combine(HostPreviewStorage.DefaultDirectory(local), "..");
        Assert.Throws<InvalidOperationException>(() =>
            HostPreviewStorage.RequireIsolatedDirectory(traversal, local));
    }

    [Fact]
    public void PreviewStorageAcceptsPreviewSubtreeAndIsolatedTestDirectories()
    {
        string local = Path.Combine(Path.GetTempPath(), "host-contract-test", "Local");
        string preview = HostPreviewStorage.DefaultDirectory(local);
        Assert.Equal(Path.GetFullPath(preview),
            HostPreviewStorage.RequireIsolatedDirectory(preview, local));
        Assert.Equal(Path.Combine(preview, "Test"),
            HostPreviewStorage.RequireIsolatedDirectory(Path.Combine(preview, "Test"), local));
        string isolated = Path.Combine(Path.GetTempPath(), "host-contract-test-independent");
        Assert.Equal(Path.GetFullPath(isolated),
            HostPreviewStorage.RequireIsolatedDirectory(isolated, local));
    }
}
