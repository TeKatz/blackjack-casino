using System.Net;
using System.Net.Http.Json;
using BlackjackCasino.Desktop;
using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

public sealed class DesktopUpdateServiceTests
{
    [Fact]
    public void ManifestUrisRemainPinnedToTheirReleaseChannels()
    {
        Assert.Equal(
            "https://github.com/TeKatz/blackjack-casino/releases/latest/download/update-manifest.json",
            DesktopUpdateService.ManifestUri(UpdateChannel.Stable).AbsoluteUri);
        Assert.Equal(
            "https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json",
            DesktopUpdateService.ManifestUri(UpdateChannel.Development).AbsoluteUri);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DesktopUpdateService.ManifestUri((UpdateChannel)999));
    }

    [Theory]
    [InlineData("0.1.0", "0.1.0", true)]
    [InlineData("0.8.0", "0.7.9", false)]
    [InlineData("0.7.9", "0.8.0", true)]
    public void MinimumUpdaterVersionIsPreserved(string minimum, string installed, bool compatible)
    {
        UpdateManifest manifest = Manifest("development") with { MinimumUpdaterVersion = minimum };
        bool actual = DesktopUpdateService.MinimumUpdaterVersion(manifest)
            <= SemanticVersion.Parse(installed);
        Assert.Equal(compatible, actual);
    }

    [Fact]
    public async Task DevelopmentCheckUsesSelectedChannelAndPreservesNativeValidation()
    {
        var handler = new ManifestHandler();
        using var http = new HttpClient(handler);
        using var service = new DesktopUpdateService(http);
        UpdateCheckResult result = await service.CheckAsync(
            UpdateChannel.Development, SemanticVersion.Parse("0.6.0"));

        Assert.True(result.UpdateAvailable);
        Assert.Equal("development", result.Manifest.Channel);
        Assert.Equal(DesktopUpdateService.ManifestUri(UpdateChannel.Development), handler.LastUri);
    }

    private static UpdateManifest Manifest(string channel) => new()
    {
        Version = "0.7.0-dev.72",
        Channel = channel,
        Package = new UpdatePackage
        {
            Url = "https://example.test/blackjack-casino-win-x64.zip",
            Sha256 = new string('A', 64),
            SizeBytes = 42,
        },
    };

    private sealed class ManifestHandler : HttpMessageHandler
    {
        internal Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Manifest("development")),
            });
        }
    }
}
