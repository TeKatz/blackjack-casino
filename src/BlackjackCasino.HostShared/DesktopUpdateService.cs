using System.Net.Http;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

/// <summary>
/// WPF-independent desktop update transport. The shell owns prompts, progress display,
/// updater launch and shutdown; this service keeps channel and package verification native.
/// </summary>
internal sealed class DesktopUpdateService : IDisposable
{
    private readonly HttpClient httpClient;
    private readonly bool ownsClient;

    internal DesktopUpdateService(HttpClient? httpClient = null)
    {
        ownsClient = httpClient is null;
        this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    internal static Uri ManifestUri(UpdateChannel channel) => channel switch
    {
        UpdateChannel.Stable => new Uri("https://github.com/TeKatz/blackjack-casino/releases/latest/download/update-manifest.json"),
        UpdateChannel.Development => new Uri("https://github.com/TeKatz/blackjack-casino/releases/download/development/update-manifest.json"),
        _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
    };

    internal static SemanticVersion MinimumUpdaterVersion(UpdateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return SemanticVersion.Parse(manifest.MinimumUpdaterVersion);
    }

    internal Task<UpdateCheckResult> CheckAsync(
        UpdateChannel channel, SemanticVersion currentVersion, CancellationToken cancellationToken = default)
    {
        var source = new HttpManifestUpdateSource(httpClient, ManifestUri(channel));
        var client = new UpdateClient(source, channel.ToManifestValue());
        return client.CheckAsync(currentVersion, cancellationToken);
    }

    internal Task<string> DownloadAsync(
        UpdatePackage package, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        string updateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackjackCasino",
            "Updates");
        return new PackageDownloader(httpClient).DownloadAndVerifyAsync(
            package, updateDirectory, progress, cancellationToken);
    }

    public void Dispose()
    {
        if (ownsClient) httpClient.Dispose();
    }
}
