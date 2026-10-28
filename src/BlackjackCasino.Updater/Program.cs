using System.Diagnostics;
using System.IO.Compression;
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
            ApplyPackage(options);
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
        }
    }

    private static void ApplyPackage(UpdaterArguments options)
    {
        string target = Path.GetFullPath(options.TargetDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string? parent = Directory.GetParent(target)?.FullName;
        if (string.IsNullOrWhiteSpace(parent))
        {
            throw new InvalidOperationException("Refusing to update a filesystem root.");
        }

        string staging = Path.Combine(parent, $".{Path.GetFileName(target)}.staging-{Guid.NewGuid():N}");
        string backup = Path.Combine(parent, $".{Path.GetFileName(target)}.backup-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}");

        Directory.CreateDirectory(staging);

        try
        {
            ZipFile.ExtractToDirectory(options.PackagePath, staging, overwriteFiles: false);
            ValidateExtractedPackage(staging, options.RestartExecutable);

            if (Directory.Exists(target))
            {
                Directory.Move(target, backup);
            }

            try
            {
                Directory.Move(staging, target);
            }
            catch
            {
                if (Directory.Exists(backup) && !Directory.Exists(target))
                {
                    Directory.Move(backup, target);
                }

                throw;
            }

            TryDeleteDirectory(backup);
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    private static void ValidateExtractedPackage(string staging, string restartExecutable)
    {
        string restartPath = Path.GetFullPath(Path.Combine(staging, restartExecutable));
        if (!restartPath.StartsWith(Path.GetFullPath(staging), StringComparison.OrdinalIgnoreCase)
            || !File.Exists(restartPath))
        {
            throw new InvalidDataException($"The update package does not contain '{restartExecutable}'.");
        }
    }

    private static void Restart(UpdaterArguments options)
    {
        string executable = Path.Combine(options.TargetDirectory, options.RestartExecutable);
        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = options.TargetDirectory,
            UseShellExecute = true,
        });
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
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
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

        if (Path.IsPathRooted(restart) || restart.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("Restart executable must be a safe path relative to the target directory.");
        }

        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.");
        }

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
