using System.Diagnostics;
using BlackjackCasino.Update;

namespace BlackjackCasino.Updater;

internal static class Program
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino", "Updates", "updater.log");

    private static async Task<int> Main(string[] args)
    {
        UpdaterArguments? options = null;
        try
        {
            Log("Updater process launched.");
            options = UpdaterArguments.Parse(args);

            if (options.ParentProcessId is int pid)
            {
                Log("Waiting for the casino process to exit.");
                WaitForProcess(pid);
            }

            if (!File.Exists(options.PackagePath))
                throw new FileNotFoundException("Update package was not found.", options.PackagePath);

            Log("Verifying downloaded package SHA-256.");
            await PackageDownloader.VerifySha256Async(options.PackagePath, options.Sha256);
            Log("Verified. Installing staged package.");
            PackageInstaller.InstallFromZip(
                options.PackagePath, options.TargetDirectory, options.RestartExecutable,
                (stage, attempt, error) =>
                {
                    if (stage == "fallback-in-place")
                        Log("The installation folder cannot be renamed. "
                            + "Attempting journaled file-by-file replacement instead.", error);
                    else
                        Log($"Temporary installation lock during {stage}, retry {attempt}: {error.Message}");
                    // A short-lived WebView2 teardown should resolve on its own.
                    // At longer-lasting failures, log evidence rather than simply
                    // extending the retry timer again and again.
                    if (stage == "move-existing" && attempt is 1 or 12 or 36)
                        LogInstallationLockDiagnostics(options.TargetDirectory);
                });

            Log("Package activated. Restarting the casino.");
            Restart(options);
            Log("Casino restart launched successfully.");
            TryDelete(options.PackagePath);
            return 0;
        }
        catch (Exception ex)
        {
            Log("Update failed.", ex);
            if (options is not null && ex is IOException or UnauthorizedAccessException)
                LogInstallationLockDiagnostics(options.TargetDirectory);
            // A failed rename leaves the old install intact; a failed file-level
            // transaction is rolled back. NEVER launch a mixed install if
            // restoration itself failed (AggregateException preserves backups).
            bool recovered = ex is not AggregateException
                && TryRestartAvailableInstallation(options);
            string message = recovered
                ? "The update could not be installed. The available casino installation has been restarted."
                : ex is AggregateException
                    ? "The update could not be safely rolled back. Do not launch the casino yet. "
                      + "Keep the original-file backup listed in Details and contact support."
                    : "The update could not be installed or the casino could not be restarted. "
                      + "Open BlackjackCasino.NativeShell.exe from the installation folder.";
            string advice = ex is IOException or UnauthorizedAccessException
                ? Environment.NewLine + Environment.NewLine
                  + "Windows could not access the installation directory. Another casino instance, "
                  + "a lingering WebView2 process, Windows Explorer or antivirus software may "
                  + "still be using it; folder permissions can also cause this."
                  + Environment.NewLine
                  + "Close all Blackjack Casino windows and any Explorer windows displaying the "
                  + "installation folder, then retry. If the folder stays locked, restart Windows "
                  + "before trying again. Do not delete the installation or your save data."
                : string.Empty;
            ShowFailure(message + advice + Environment.NewLine + Environment.NewLine
                + "Details: " + ex.Message + Environment.NewLine
                + "Diagnostic log: " + LogPath);
            return 1;
        }
    }

    private static bool TryRestartAvailableInstallation(UpdaterArguments? options)
    {
        if (options is null) return false;
        try
        {
            string path = PackageInstaller.GetRestartPath(
                options.TargetDirectory, options.RestartExecutable);
            if (!File.Exists(path))
            {
                Log("No executable at the installation target; recovery launch unavailable.");
                return false;
            }
            Restart(options);
            Log("Recovery restart launched.");
            return true;
        }
        catch (Exception ex)
        {
            Log("Recovery restart failed.", ex);
            return false;
        }
    }

    /// <summary>
    /// A directory rename can fail because an unrelated process still holds a
    /// handle, or because Windows disallows changes in the parent folder.
    /// Log safe, read-only process evidence and a disposable sibling-directory
    /// rename probe. Never kill the other process or change the live install.
    /// </summary>
    private static void LogInstallationLockDiagnostics(string targetDirectory)
    {
        try
        {
            string target = Path.GetFullPath(targetDirectory);
            string targetExecutable = Path.Combine(target, "BlackjackCasino.NativeShell.exe");
            int activeInstances = 0;
            int unreadablePaths = 0;
            foreach (Process process in Process.GetProcessesByName("BlackjackCasino.NativeShell"))
            {
                using (process)
                {
                    try
                    {
                        string? executable = process.MainModule?.FileName;
                        if (executable is not null &&
                            string.Equals(Path.GetFullPath(executable), targetExecutable,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            Log($"Still-running casino instance in the target directory: PID {process.Id}.");
                            activeInstances++;
                        }
                    }
                    catch (Exception error) when (error is System.ComponentModel.Win32Exception
                        or InvalidOperationException or UnauthorizedAccessException)
                    {
                        unreadablePaths++;
                    }
                }
            }

            if (activeInstances == 0)
                Log("No additional readable casino executable process found in the target. "
                    + "Explorer, WebView2, antivirus, or permissions may still block the rename.");
            if (unreadablePaths > 0)
                Log($"Process inspection could not read {unreadablePaths} executable path(s).");

            string parent = Path.GetDirectoryName(target)
                ?? throw new IOException("Installation target has no parent directory.");
            string probe = Path.Combine(parent, ".casino-update-probe-" + Guid.NewGuid().ToString("N"));
            string renamed = probe + "-renamed";
            try
            {
                Directory.CreateDirectory(probe);
                Directory.Move(probe, renamed);
                Log("Sibling-directory rename probe succeeded; the target itself may be locked "
                    + "or have different permissions.");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Log($"Sibling-directory rename probe failed: {error.Message}. "
                    + "Check Desktop protection, parent permissions and security software.");
            }
            finally
            {
                TryDeleteDirectory(renamed);
                TryDeleteDirectory(probe);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException
            or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Log("Installation-lock diagnostics could not complete.", error);
        }
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Never interfere with installation or recovery if the test folder
            // is briefly scanned by security software.
            Log($"Could not clean up update-diagnostic directory: {error.Message}");
        }
    }

    private static void ShowFailure(string message)
    {
        try
        {
            System.Windows.Forms.MessageBox.Show(message, "Blackjack Casino update failed",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
        catch
        {
            // Diagnostics remain available even if Windows cannot display a dialog.
        }
    }

    private static void Log(string message, Exception? exception = null)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTimeOffset.Now:O}] {message}"
                + Environment.NewLine
                + (exception is null ? "" : exception + Environment.NewLine));
        }
        catch
        {
            // Logging is best effort and must never block recovery.
        }
    }

    private static void WaitForProcess(int pid)
    {
        try
        {
            using Process process = Process.GetProcessById(pid);
            process.WaitForExit();
        }
        catch (ArgumentException)
        {
            // The application has already exited.
        }
    }

    private static void Restart(UpdaterArguments options)
    {
        string executable = PackageInstaller.GetRestartPath(options.TargetDirectory, options.RestartExecutable);
        _ = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = options.TargetDirectory,
            UseShellExecute = true,
        }) ?? throw new InvalidOperationException("Failed to restart the application after updating.");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // The package can be cleaned up by a later maintenance pass.
        }
    }
}

internal sealed record UpdaterArguments(
    string PackagePath,
    string Sha256,
    string TargetDirectory,
    string RestartExecutable,
    int? ParentProcessId)
{
    public static UpdaterArguments Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Arguments must be supplied as --name value pairs.");
            }

            values[args[i][2..]] = args[i + 1];
        }

        string package = GetRequired(values, "package");
        string sha256 = GetRequired(values, "sha256");
        string target = GetRequired(values, "target");
        string restart = GetRequired(values, "restart");

        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.");
        }

        // Also validates that this cannot escape the target directory.
        _ = PackageInstaller.GetRestartPath(target, restart);

        int? pid = null;
        if (values.TryGetValue("pid", out string? pidValue))
        {
            if (!int.TryParse(pidValue, out int parsedPid) || parsedPid <= 0)
            {
                throw new ArgumentException("PID must be a positive integer.");
            }

            pid = parsedPid;
        }

        return new UpdaterArguments(
            Path.GetFullPath(package),
            sha256,
            Path.GetFullPath(target),
            restart,
            pid);
    }

    private static string GetRequired(IReadOnlyDictionary<string, string> values, string name)
    {
        return values.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required argument --{name}.");
    }
}
