using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class BrowserRecoverySessionTests
{
    [Theory]
    [InlineData("RetryModern")]
    [InlineData("OpenLegacyGames")]
    [InlineData("ReturnToLobby")]
    public void AvailableAssetsAllowExactlyOneExplicitChoice(string name)
    {
        BrowserRecoveryAction choice = Enum.Parse<BrowserRecoveryAction>(name);
        var session = new BrowserRecoverySession(modernAssetsAvailable: true);
        Assert.False(session.Completed);
        Assert.True(session.TryChoose(choice));
        Assert.True(session.Completed);
        Assert.False(session.TryChoose(BrowserRecoveryAction.OpenLegacyGames));
        Assert.False(session.TryChoose(BrowserRecoveryAction.RetryModern));
    }

    [Fact]
    public void MissingModernAssetsRejectRetryWithoutConsumingLegacyRecovery()
    {
        var session = new BrowserRecoverySession(modernAssetsAvailable: false);
        Assert.False(session.TryChoose(BrowserRecoveryAction.RetryModern));
        Assert.False(session.Completed);
        Assert.True(session.TryChoose(BrowserRecoveryAction.OpenLegacyGames));
        Assert.False(session.TryChoose(BrowserRecoveryAction.ReturnToLobby));
    }

    [Fact]
    public void UnrecognizedActionsCannotConsumeRecovery()
    {
        var session = new BrowserRecoverySession(modernAssetsAvailable: true);
        Assert.False(session.TryChoose((BrowserRecoveryAction)999));
        Assert.False(session.Completed);
        Assert.True(session.TryChoose(BrowserRecoveryAction.ReturnToLobby));
    }
}
