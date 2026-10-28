using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

public sealed class PackageDownloaderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "BlackjackCasino.Tests", Guid.NewGuid().ToString("N"));

    public PackageDownloaderTests()
    {
        Directory.CreateDirectory(root);
    }

    [Fact]
    public async Task DownloadAndVerifyAsync_ReleasesDownloadedFileBeforeVerificationAndReturn()
    {
        byte[] payload = "blackjack-update-payload"u8.ToArray();
        string sha256 = Convert.ToHexString(SHA256.HashData(payload));

        using var httpClient = new HttpClient(new StaticResponseHandler(payload));
        var downloader = new PackageDownloader(httpClient);
        var package = new UpdatePackage
        {
            Url = "https://updates.example.test/blackjack.zip",
            Sha256 = sha256,
            SizeBytes = payload.Length,
        };

        string path = await downloader.DownloadAndVerifyAsync(package, root);

        Assert.Equal(payload, await File.ReadAllBytesAsync(path));

        await using FileStream exclusive = new(
            path,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        Assert.Equal(payload.Length, exclusive.Length);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch
        {
        }
    }

    private sealed class StaticResponseHandler(byte[] payload) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(payload),
            };
            response.Content.Headers.ContentLength = payload.Length;
            return Task.FromResult(response);
        }
    }
}
