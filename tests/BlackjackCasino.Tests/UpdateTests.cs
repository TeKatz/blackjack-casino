using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

public sealed class UpdateTests
{
    [Theory]
    [InlineData("1.2.3", "1.2.4", -1)]
    [InlineData("1.2.3-beta.1", "1.2.3", -1)]
    [InlineData("2.0.0", "1.99.99", 1)]
    [InlineData("v1.2.3+build.4", "1.2.3", 0)]
    public void SemanticVersion_OrdersVersionsCorrectly(string left, string right, int expectedSign)
    {
        int comparison = SemanticVersion.Parse(left).CompareTo(SemanticVersion.Parse(right));
        Assert.Equal(expectedSign, Math.Sign(comparison));
    }

    [Fact]
    public async Task UpdateClient_ReportsNewerManifest()
    {
        var client = new UpdateClient(new FakeSource(CreateManifest("0.2.0")));
        UpdateCheckResult result = await client.CheckAsync(SemanticVersion.Parse("0.1.0"));

        Assert.True(result.UpdateAvailable);
        Assert.Equal(SemanticVersion.Parse("0.2.0"), result.LatestVersion);
    }

    [Fact]
    public async Task UpdateClient_RejectsWrongChannel()
    {
        var client = new UpdateClient(new FakeSource(CreateManifest("0.2.0") with { Channel = "beta" }));

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            client.CheckAsync(SemanticVersion.Parse("0.1.0")));
    }

    [Fact]
    public async Task Sha256Verification_RejectsTamperedFile()
    {
        string path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "tampered");
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                PackageDownloader.VerifySha256Async(path,
                    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static UpdateManifest CreateManifest(string version) => new()
    {
        Version = version,
        Channel = "stable",
        PublishedAt = DateTimeOffset.UtcNow,
        MinimumUpdaterVersion = "0.1.0",
        Package = new UpdatePackage
        {
            Url = "https://example.test/blackjack.zip",
            Sha256 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            SizeBytes = 123,
        },
    };

    private sealed class FakeSource(UpdateManifest manifest) : IUpdateSource
    {
        public Task<UpdateManifest> GetLatestAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(manifest);
    }
}
