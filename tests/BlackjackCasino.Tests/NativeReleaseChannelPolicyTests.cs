using BlackjackCasino.Update;
using Xunit;

namespace BlackjackCasino.Tests;

public sealed class NativeReleaseChannelPolicyTests
{
    [Theory]
    [InlineData("0.11.0-dev.40", UpdateChannel.Development, true)]
    [InlineData("0.11.0-dev.40", UpdateChannel.Stable, false)]
    [InlineData("0.11.0", UpdateChannel.Stable, true)]
    [InlineData("0.11.0", UpdateChannel.Development, false)]
    public void InstallerNeverCrossesAReleaseChannelSilently(
        string version, UpdateChannel channel, bool allowed)
    {
        Assert.True(SemanticVersion.TryParse(version, out SemanticVersion installed));
        Assert.Equal(allowed, NativeReleaseChannelPolicy.MayInstall(installed, channel));
    }

    [Fact]
    public void DevelopmentPrereleaseNeverTriggersStableDowngrade()
    {
        SemanticVersion current = SemanticVersion.Parse("0.12.0-dev.55");
        Assert.Equal(UpdateChannel.Development, NativeReleaseChannelPolicy.InstalledChannel(current));
        Assert.False(NativeReleaseChannelPolicy.MayInstall(current, UpdateChannel.Stable));
    }
}
