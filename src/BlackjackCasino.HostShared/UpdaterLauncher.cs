using System.Diagnostics;
using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

internal static class UpdaterLauncher
{
    private const string UpdaterRelativePath = @"tools\updater\BlackjackCasino.Updater.exe";

    private static string BundledUpdaterPath => Path.Combine(AppContext.BaseDirectory, UpdaterRelativePath);

    public static bool IsAvailable => File.Exists(BundledUpdaterPath);

    public static SemanticVersion? CurrentVersion
    {
        get
        {
            if (!IsAvailable)
            {
                return null;
            }

            try
            {
                string? productVersion = FileVersionInfo.GetVersionInfo(BundledUpdaterPath).ProductVersion;
                return SemanticVersion.TryParse(productVersion, out SemanticVersion version)
                    ? version
                    : null;
            }
            catch
            {
                return null;
            }
        }
    }

    public static void Launch(string packagePath, string sha256)
    {
        string bundledUpdater = BundledUpdaterPath;
        if (!File.Exists(bundledUpdater))
        {
            throw new FileNotFoundException("The bundled updater executable is missing.", bundledUpdater);
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "BlackjackCasino", "Updater", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string tempUpdater = Path.Combine(tempDirectory, "BlackjackCasino.Updater.exe");
        File.Copy(bundledUpdater, tempUpdater, overwrite: true);

        var startInfo = new ProcessStartInfo
        {
            FileName = tempUpdater,
            UseShellExecute = false,
            WorkingDirectory = tempDirectory,
        };

        startInfo.ArgumentList.Add("--package");
        startInfo.ArgumentList.Add(Path.GetFullPath(packagePath));
        startInfo.ArgumentList.Add("--sha256");
        startInfo.ArgumentList.Add(sha256);
        startInfo.ArgumentList.Add("--target");
        startInfo.ArgumentList.Add(Path.GetFullPath(AppContext.BaseDirectory));
        startInfo.ArgumentList.Add("--restart");
        startInfo.ArgumentList.Add("BlackjackCasino.Desktop.exe");
        startInfo.ArgumentList.Add("--pid");
        startInfo.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        _ = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the updater process.");
    }
}
