using BlackjackCasino.Core.Hosting;
using BlackjackCasino.HostShared;

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
        Assert.Equal(expected, NativeRouteCatalog.CasinoRoutes.Select(route => route.DesktopModeId));
        Assert.Equal(expected.Length,
            NativeRouteCatalog.CasinoRoutes.Select(route => route.DesktopModeId)
                .Distinct(StringComparer.Ordinal).Count());
        Assert.True(NativeRouteCatalog.CasinoRoutes.Single(route =>
            route.DesktopModeId == "casino-empire").HasSecondaryWindow);
        Assert.True(NativeRouteCatalog.CasinoRoutes.Single(route =>
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
    public void NativeWebRoutesHaveExactDocumentAllowlist(string id, string document)
    {
        Assert.True(NativeRouteCatalog.IsWebRoute(id));
        Assert.Equal(document, NativeRouteCatalog.WebPageFor(id));
    }

    [Fact]
    public void NativeRouteAllowlistRejectsUnknownBrowserModes()
    {
        Assert.Equal(10, NativeRouteCatalog.WebRoutes.Count);
        Assert.True(NativeRouteCatalog.IsWebRoute("roulette"));
        Assert.True(NativeRouteCatalog.IsWebRoute("casino-empire"));
        Assert.True(NativeRouteCatalog.IsWebRoute("chill-lounge"));
        Assert.False(NativeRouteCatalog.IsWebRoute("kniffel"));
        Assert.False(NativeRouteCatalog.IsWebRoute("../lobby"));
        Assert.False(NativeRouteCatalog.IsWebRoute(null));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NativeRouteCatalog.WebPageFor("lobby"));
        Assert.Equal(8, NativeRouteCatalog.LoungeGameIds.Count);
        Assert.Equal(8, NativeRouteCatalog.LoungeGameIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void NativeRouteInventoryNeverOffersUnmappedOrDuplicateModes()
    {
        Assert.Equal(11, NativeRouteCatalog.CasinoRoutes.Count);
        Assert.Equal(10, NativeRouteCatalog.WebRoutes.Count);
        Assert.All(NativeRouteCatalog.CasinoRoutes, route =>
        {
            Assert.False(string.IsNullOrWhiteSpace(route.DesktopModeId));
            Assert.True(route.HasWebPage ^ route.HasNativeProcess);
            if (route.HasWebPage)
                Assert.Equal(route.WebDocument,
                    NativeRouteCatalog.WebPageFor(route.WebModeId!));
        });
        Assert.Equal(NativeRouteCatalog.WebRoutes.Count,
            NativeRouteCatalog.WebRoutes
                .Select(route => route.WebModeId)
                .Distinct(StringComparer.Ordinal).Count());
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
