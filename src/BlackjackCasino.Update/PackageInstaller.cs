using System.IO.Compression;

namespace BlackjackCasino.Update;

public static class PackageInstaller
{
    public static void InstallFromZip(string packagePath, string targetDirectory, string restartExecutable)
        => InstallFromZip(packagePath, targetDirectory, restartExecutable, onRetry: null);

    /// <summary>
    /// Stage and validate the complete package before replacing anything.
    /// The optional callback reports temporary Windows filesystem contention.
    /// </summary>
    public static void InstallFromZip(string packagePath, string targetDirectory,
        string restartExecutable, Action<string, int, Exception>? onRetry)
        => InstallFromZip(packagePath, targetDirectory, restartExecutable, onRetry,
            directoryMove: Directory.Move, delay: Thread.Sleep, fileMove: null);

    // Injectable boundaries make real directory-lock fallback and partial-write rollback testable.
    internal static void InstallFromZip(string packagePath, string targetDirectory,
        string restartExecutable, Action<string, int, Exception>? onRetry,
        Action<string, string> directoryMove, Action<TimeSpan> delay,
        Action<string, string, bool>? fileMove)
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
        // All updater versions after this change serialize installs outside the app directory.
        // A lock left by a crash is harmless: only its OS handle owns the exclusion.
        string lockFile = Path.Combine(parent, $".{name}.update.lock");
        using var updateLock = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        // A previous updater may have crashed in the middle of an in-place swap.
        // Restore its original files before attempting either installation strategy.
        InPlacePackageInstaller.RecoverPending(target);
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
                try
                {
                    // A permanently held directory handle cannot be fixed by 90 seconds
                    // of retrying the same rename. Try briefly, then overlay file-by-file
                    // using a durable recovery journal and original-file backups.
                    MoveDirectoryWithRetries(target, backup,
                        move: directoryMove, delay: delay, maxAttempts: 4,
                        onRetry: (attempt, error) => onRetry?.Invoke("move-existing", attempt, error));
                    movedExistingInstall = true;
                }
                catch (Exception directoryError) when (directoryError is IOException or UnauthorizedAccessException)
                {
                    onRetry?.Invoke("fallback-in-place", 0, directoryError);
                    InPlacePackageInstaller.Apply(staging, target, onRetry, fileMove);
                    return;
                }
            }

            try
            {
                MoveDirectoryWithRetries(staging, target,
                    move: directoryMove, delay: delay,
                    onRetry: (attempt, error) => onRetry?.Invoke("activate-package", attempt, error));
            }
            catch (Exception installError)
            {
                if (movedExistingInstall && Directory.Exists(backup) && !Directory.Exists(target))
                {
                    try
                    {
                        MoveDirectoryWithRetries(backup, target,
                            move: directoryMove, delay: delay,
                            onRetry: (attempt, error) => onRetry?.Invoke("restore-existing", attempt, error));
                    }
                    catch (Exception recoveryError)
                    {
                        // Never delete this backup: it is the last remaining
                        // copy of the old installation if restoration failed.
                        throw new AggregateException(
                            "Update activation and restoration of the previous installation failed. "
                            + "The backup is at " + backup, installError, recoveryError);
                    }
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

    /// <summary>
    /// Retry transient directory locks for the caller's bounded budget.
    /// The standard updater now uses only four rename attempts before switching
    /// to a transactional file-by-file fallback. The longer default remains
    /// available to individual callers and its original retry-budget tests.
    /// </summary>
    internal static void MoveDirectoryWithRetries(string source, string destination,
        Action<string, string>? move = null, Action<TimeSpan>? delay = null,
        int maxAttempts = 37, Action<int, Exception>? onRetry = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxAttempts);
        move ??= Directory.Move;
        delay ??= Thread.Sleep;

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                move(source, destination);
                return;
            }
            catch (Exception ex) when ((ex is IOException or UnauthorizedAccessException)
                && attempt < maxAttempts)
            {
                onRetry?.Invoke(attempt, ex);
                // 36 waits: first 15 grow from 0.2s to 3s, then cap at 3s.
                // Total retry budget is 87s, long enough for slow WebView2
                // teardown without infinite hangs on ACL/permission failures.
                delay(GetDirectoryMoveRetryDelay(attempt));
            }
        }
    }

    internal static TimeSpan GetDirectoryMoveRetryDelay(int attempt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attempt);
        return TimeSpan.FromMilliseconds(Math.Min(3000L, 200L * attempt));
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
