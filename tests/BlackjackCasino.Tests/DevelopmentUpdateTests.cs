using System.Net;
using System.Net.Http.Json;
using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

public sealed class DevelopmentUpdateTests
{
    [Theory]
    [InlineData("0.6.5", true)]
    [InlineData("0.6.5-dev.71", true)]
    [InlineData("0.6.5-dev.0", true)]
    [InlineData("0.7.0-dev.71", true)]
    [InlineData("0.7.0-dev.72", false)]
    [InlineData("0.7.0", false)]
    [InlineData("0.8.0", false)]
    public async Task PublishedManorVersionIsVisibleToExistingClientsWithoutDowngrades(string installed, bool available)
    {
        using var http = new HttpClient(new ManifestHandler());
        var source = new HttpManifestUpdateSource(http, new Uri("https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json"));
        var client = new UpdateClient(source, "development");
        UpdateCheckResult result = await client.CheckAsync(SemanticVersion.Parse(installed));
        Assert.Equal(available, result.UpdateAvailable);
        Assert.Equal(SemanticVersion.Parse("0.7.0-dev.72"), result.LatestVersion);
    }

    [Fact]
    public async Task MutableManifestIsRevalidatedOnEveryCheck()
    {
        var handler = new ManifestHandler();
        using var http = new HttpClient(handler);
        var source = new HttpManifestUpdateSource(http, new Uri("https://updates.example.test/development/update-manifest.json"));
        await source.GetLatestAsync();
        await source.GetLatestAsync();
        Assert.Equal(2, handler.Requests);
    }

    private sealed class ManifestHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.True(request.Headers.CacheControl?.NoCache);
            Assert.True(request.Headers.CacheControl?.NoStore);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UpdateManifest
                {
                    Version = "0.7.0-dev.72",
                    Channel = "development",
                    PublishedAt = DateTimeOffset.UtcNow,
                    Package = new UpdatePackage
                    {
                        Url = "https://github.com/TeKatz/blackjack-casino/releases/download/development/blackjack-casino-win-x64.zip",
                        Sha256 = new string('A', 64),
                        SizeBytes = 123,
                    },
                }),
            });
        }
    }
}
