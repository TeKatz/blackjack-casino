using System.Diagnostics;
using BlackjackCasino.Update;

namespace BlackjackCasino.Updater;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            UpdaterArguments options = UpdaterArguments.Parse(args);

            if (options.ParentProcessId is int pid)
            {
                WaitForProcess(pid);
            }

            if (!File.Exists(options.PackagePath))
            {
                throw new FileNotFoundException("Update package was not found.", options.PackagePath);
            }

            await PackageDownloader.VerifySha256Async(options.PackagePath, options.Sha256);
            PackageInstaller.InstallFromZip(options.PackagePath, options.TargetDirectory, options.RestartExecutable);
            Restart(options);
            TryDelete(options.PackagePath);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
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
