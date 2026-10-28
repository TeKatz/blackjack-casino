using System.Net;
using System.Net.Http;
using System.Windows;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

public partial class MainWindow : Window
{
    private static readonly Uri ManifestUri = new(
        "https://github.com/TeKatz/blackjack-casino/releases/latest/download/update-manifest.json");

    private readonly HttpClient httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };

    private UpdateManifest? pendingManifest;

    public MainWindow()
    {
        InitializeComponent();
        VersionText.Text = AppVersion.Current.ToString();
        Loaded += async (_, _) => await CheckForUpdatesAsync(silent: true);
        Closed += (_, _) => httpClient.Dispose();
    }

    private async void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(silent: false);
    }

    private async void InstallUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (pendingManifest is null)
        {
            return;
        }

        try
        {
            SetBusy(true);
            UpdateStatusText.Text = "Downloading update…";

            string updateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BlackjackCasino",
                "Updates");

            var progress = new Progress<double>(value =>
                UpdateStatusText.Text = $"Downloading update… {value:P0}");

            var downloader = new PackageDownloader(httpClient);
            string packagePath = await downloader.DownloadAndVerifyAsync(
                pendingManifest.Package,
                updateDirectory,
                progress);

            UpdateStatusText.Text = "Update verified. Restarting…";
            UpdaterLauncher.Launch(packagePath, pendingManifest.Package.Sha256);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            UpdateStatusText.Text = $"Update failed: {ex.Message}";
            SetBusy(false);
        }
    }

    private async Task CheckForUpdatesAsync(bool silent)
    {
        try
        {
            SetBusy(true);
            if (!silent)
            {
                UpdateStatusText.Text = "Checking…";
            }

            var source = new HttpManifestUpdateSource(httpClient, ManifestUri);
            var client = new UpdateClient(source);
            UpdateCheckResult result = await client.CheckAsync(AppVersion.Current);

            if (!result.UpdateAvailable)
            {
                pendingManifest = null;
                UpdateStatusText.Text = "Up to date";
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            SemanticVersion minimumUpdater = SemanticVersion.Parse(result.Manifest.MinimumUpdaterVersion);
            if (minimumUpdater > AppVersion.Current)
            {
                pendingManifest = null;
                UpdateStatusText.Text = $"Update {result.LatestVersion} requires a newer updater.";
                InstallUpdateButton.IsEnabled = false;
                return;
            }

            pendingManifest = result.Manifest;
            UpdateStatusText.Text = $"Version {result.LatestVersion} is available";
            InstallUpdateButton.IsEnabled = UpdaterLauncher.IsAvailable;

            if (!UpdaterLauncher.IsAvailable)
            {
                UpdateStatusText.Text += " (updater helper missing in this development build)";
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            pendingManifest = null;
            UpdateStatusText.Text = "No published release channel yet";
            InstallUpdateButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            pendingManifest = null;
            InstallUpdateButton.IsEnabled = false;
            UpdateStatusText.Text = silent ? "Update check unavailable" : $"Update check failed: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        CheckUpdateButton.IsEnabled = !busy;
        if (busy)
        {
            InstallUpdateButton.IsEnabled = false;
        }
        else if (pendingManifest is not null)
        {
            InstallUpdateButton.IsEnabled = UpdaterLauncher.IsAvailable;
        }
    }
}
