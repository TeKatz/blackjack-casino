using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class CasinoModeRegistryTests
{
    private static CasinoMode Mode(string id) =>
        new(id, "Title", "Subtitle", "Description", "#123456", "♠", "Table games");

    [Fact]
    public void RegistrationAndNavigationDoNotNeedWpfArtworkOrGameViews()
    {
        var registry = new CasinoModeRegistry();
        int changes = 0, opens = 0;
        registry.Changed += () => changes++;
        registry.Register(Mode("blackjack"), () => opens++);
        Assert.Single(registry.Modes);
        Assert.Equal("blackjack", registry.Modes[0].Id);
        Assert.True(registry.CanOpen("blackjack"));
        Assert.False(registry.CanOpen("unknown"));
        registry.Open("blackjack");
        registry.Open("unknown");
        Assert.Equal(1, opens);
        Assert.Equal(1, changes);
        registry.NotifyStateChanged();
        Assert.Equal(2, changes);
    }

    [Fact]
    public void UnavailablePrimaryAndSecondaryActionsDoNotRun()
    {
        var registry = new CasinoModeRegistry();
        bool allowed = false;
        int primary = 0, secondary = 0;
        registry.Register(Mode("casino-empire"), () => primary++,
            canOpen: () => allowed,
            secondaryOpen: () => secondary++,
            secondaryLabel: "OPEN SEPARATE WINDOW",
            canSecondaryOpen: () => allowed);
        Assert.True(registry.HasSecondary("casino-empire"));
        Assert.False(registry.CanOpen("casino-empire"));
        Assert.False(registry.CanOpenSecondary("casino-empire"));
        registry.Open("casino-empire");
        registry.OpenSecondary("casino-empire");
        Assert.Equal(0, primary);
        Assert.Equal(0, secondary);
        allowed = true;
        registry.Open("casino-empire");
        registry.OpenSecondary("casino-empire");
        Assert.Equal(1, primary);
        Assert.Equal(1, secondary);
    }

    [Theory]
    [InlineData("")]
    [InlineData("../unsafe")]
    [InlineData("slot with space")]
    public void InvalidModeIdentifiersAreRejected(string id)
    {
        var registry = new CasinoModeRegistry();
        Assert.Throws<ArgumentException>(() => registry.Register(Mode(id), () => { }));
    }

    [Fact]
    public void DuplicateIdsAndIncompleteSecondaryActionsAreRejected()
    {
        var registry = new CasinoModeRegistry();
        registry.Register(Mode("blackjack"), () => { });
        Assert.Throws<ArgumentException>(() => registry.Register(Mode("blackjack"), () => { }));
        Assert.Throws<ArgumentException>(() =>
            registry.Register(Mode("nine-lives"), () => { }, secondaryLabel: "OPEN"));
        Assert.Throws<ArgumentException>(() =>
            registry.Register(Mode("nine-lives"), () => { }, secondaryOpen: () => { }));
    }

    [Fact]
    public void ResourceKeysRemainPresentationMetadataWithoutWpfDependencies()
    {
        var registry = new CasinoModeRegistry();
        registry.Register(Mode("midnight-merge") with { UseResourceKeys = true }, () => { });
        Assert.True(registry.Modes.Single().UseResourceKeys);
        Assert.DoesNotContain(typeof(CasinoMode).GetProperties(),
            property => property.PropertyType.FullName?.Contains("System.Windows", StringComparison.Ordinal) == true);
    }
}
