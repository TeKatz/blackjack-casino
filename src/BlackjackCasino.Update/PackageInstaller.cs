using System.IO.Compression;

namespace BlackjackCasino.Update;

public static class PackageInstaller
{
    public static void InstallFromZip(string packagePath, string targetDirectory, string restartExecutable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(restartExecutable);

        string package = Path.GetFullPath(packagePath);
        if (!File.Exists(package))
        {
            throw new FileNotFoundException("Update package was not found.", package);
        }

        EnsureSafeRelativePath(restartExecutable, nameof(restartExecutable));

        string target = Path.GetFullPath(targetDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        string? parent = Directory.GetParent(target)?.FullName;
        if (string.IsNullOrWhiteSpace(parent))
        {
            throw new InvalidOperationException("Refusing to update a filesystem root.");
        }

        Directory.CreateDirectory(parent);

        string name = Path.GetFileName(target);
        string staging = Path.Combine(parent, $".{name}.staging-{Guid.NewGuid():N}");
        string backup = Path.Combine(parent, $".{name}.backup-{Guid.NewGuid():N}");

        Directory.CreateDirectory(staging);

        try
        {
            ZipFile.ExtractToDirectory(package, staging, overwriteFiles: false);
            ValidateExtractedPackage(staging, restartExecutable);

            bool movedExistingInstall = false;
            if (Directory.Exists(target))
            {
                Directory.Move(target, backup);
                movedExistingInstall = true;
            }

            try
            {
                Directory.Move(staging, target);
            }
            catch
            {
                if (movedExistingInstall && Directory.Exists(backup) && !Directory.Exists(target))
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

    public static string GetRestartPath(string targetDirectory, string restartExecutable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(restartExecutable);
        EnsureSafeRelativePath(restartExecutable, nameof(restartExecutable));
        return Path.GetFullPath(Path.Combine(targetDirectory, restartExecutable));
    }

    private static void ValidateExtractedPackage(string stagingDirectory, string restartExecutable)
    {
        string restartPath = GetRestartPath(stagingDirectory, restartExecutable);
        string relative = Path.GetRelativePath(stagingDirectory, restartPath);

        if (Path.IsPathRooted(relative)
            || relative.Equals("..", StringComparison.Ordinal)
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || !File.Exists(restartPath))
        {
            throw new InvalidDataException($"The update package does not contain a safe '{restartExecutable}'.");
        }
    }

    private static void EnsureSafeRelativePath(string path, string parameterName)
    {
        if (Path.IsPathRooted(path))
        {
            throw new ArgumentException("Path must be relative to the application directory.", parameterName);
        }

        string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        foreach (string segment in normalized.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or "..")
            {
                throw new ArgumentException("Path may not contain traversal segments.", parameterName);
            }
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
            // A stale backup is preferable to making an otherwise successful update fail.
        }
    }
}
