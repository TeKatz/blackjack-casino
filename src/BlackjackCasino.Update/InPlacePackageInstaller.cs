using System.Security.Cryptography;
using System.Text.Json;

namespace BlackjackCasino.Update;

/// <summary>
/// Fallback for Windows installations whose ROOT directory cannot be renamed
/// (Explorer/legacy WebView2 handles, security scanners). It updates only package
/// files, never unrecognized user files, and preserves an on-disk rollback journal
/// OUTSIDE the installation until all files are committed.
/// </summary>
internal static class InPlacePackageInstaller
{
    private const string ActiveMarker = "transaction-active";
    private const string JournalFile = "manifest.json";
    private const string OriginalFolder = "originals";

    private sealed record Entry(string RelativePath, bool Existed);
    private sealed record Journal(string TargetDirectory, Entry[] Entries);

    internal static void RecoverPending(string targetDirectory)
    {
        string target = Path.GetFullPath(targetDirectory);
        string parent = Path.GetDirectoryName(target)
            ?? throw new InvalidOperationException("An installation root cannot be updated.");
        string prefix = "." + Path.GetFileName(target) + ".inplace-backup-";

        foreach (string backup in Directory.EnumerateDirectories(parent)
                     .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            if (!File.Exists(Path.Combine(backup, ActiveMarker)))
                continue; // Preparation never reached the first file replacement.
            try
            {
                Journal journal = ReadJournal(backup, target);
                RestoreOriginalFiles(backup, target, journal.Entries);
                File.Delete(Path.Combine(backup, ActiveMarker));
                TryDeleteBackup(backup);
            }
            catch (Exception error)
            {
                throw new AggregateException(
                    "An interrupted update could not be rolled back. The untouched original-file "
                    + "backups are preserved at " + backup + ". Do not launch a mixed installation.",
                    error);
            }
        }
    }

    internal static void Apply(string stagedDirectory, string targetDirectory,
        Action<string, int, Exception>? onRetry,
        Action<string, string, bool>? fileMove)
    {
        string staging = Path.GetFullPath(stagedDirectory);
        string target = Path.GetFullPath(targetDirectory);
        string parent = Path.GetDirectoryName(target)
            ?? throw new InvalidOperationException("An installation root cannot be updated.");
        string backup = Path.Combine(parent,
            "." + Path.GetFileName(target) + ".inplace-backup-" + Guid.NewGuid().ToString("N"));
        fileMove ??= (from, to, overwrite) => File.Move(from, to, overwrite);
        Directory.CreateDirectory(backup);

        bool transactionActive = false;
        try
        {
            Entry[] entries = Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(staging, file))
                .OrderBy(path => string.Equals(path, "BlackjackCasino.Desktop.exe",
                    StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(path => path, StringComparer.Ordinal)
                .Select(relative =>
                {
                    string installed = SafePath(target, relative);
                    if (Directory.Exists(installed))
                        throw new IOException("A directory conflicts with the new file: " + installed);
                    return new Entry(relative, File.Exists(installed));
                })
                .ToArray();

            if (entries.Select(item => item.RelativePath)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Length)
                throw new InvalidDataException("The update contains conflicting file names.");

            // Before altering ANY installed byte, snapshot every file that will be
            // replaced. A blocked file here aborts with the original install intact.
            foreach (Entry entry in entries.Where(item => item.Existed))
            {
                string original = SafePath(target, entry.RelativePath);
                string copy = SafePath(Path.Combine(backup, OriginalFolder), entry.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(original, copy, overwrite: false);
            }

            string journalPath = Path.Combine(backup, JournalFile);
            File.WriteAllText(journalPath, JsonSerializer.Serialize(new Journal(target, entries)));
            // A crash after this marker is written MUST trigger rollback next time.
            // Flushing it before the first replace avoids an unjournaled mixed install.
            using (var marker = new FileStream(Path.Combine(backup, ActiveMarker),
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                marker.WriteByte(1);
                marker.Flush(flushToDisk: true);
            }
            transactionActive = true;

            foreach (Entry entry in entries)
            {
                string from = SafePath(staging, entry.RelativePath);
                string to = SafePath(target, entry.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(to)!);
                MoveFileWithShortRetry(from, to, entry.RelativePath, fileMove, onRetry);
            }

            File.Delete(Path.Combine(backup, ActiveMarker));
            transactionActive = false;
            TryDeleteBackup(backup);
        }
        catch (Exception installationError)
        {
            if (transactionActive)
            {
                try
                {
                    Journal journal = ReadJournal(backup, target);
                    RestoreOriginalFiles(backup, target, journal.Entries);
                    File.Delete(Path.Combine(backup, ActiveMarker));
                    TryDeleteBackup(backup);
                }
                catch (Exception recoveryError)
                {
                    throw new AggregateException(
                        "In-place update and rollback failed. Keep the original-file backup at "
                        + backup + "; do not start a possibly mixed installation.",
                        installationError, recoveryError);
                }
            }
            else
            {
                // An incomplete preflight backup has never changed the target.
                // If a marker exists due to a partial marker write, keep it for
                // the next updater's recovery attempt.
                if (!File.Exists(Path.Combine(backup, ActiveMarker)))
                    TryDeleteBackup(backup);
            }

            throw;
        }
    }

    private static void MoveFileWithShortRetry(string from, string to, string relative,
        Action<string, string, bool> move, Action<string, int, Exception>? onRetry)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                move(from, to, true);
                return;
            }
            catch (Exception error) when ((error is IOException or UnauthorizedAccessException) && attempt < 6)
            {
                onRetry?.Invoke("replace-file:" + relative, attempt, error);
                Thread.Sleep(TimeSpan.FromMilliseconds(attempt * 200));
            }
        }
    }

    private static void RestoreOriginalFiles(string backup, string target, Entry[] entries)
    {
        // Retain the originals until the ENTIRE rollback is successful. If rollback
        // is interrupted, the same journal can safely be replayed on the next run.
        foreach (Entry entry in entries.Reverse())
        {
            string destination = SafePath(target, entry.RelativePath);
            if (!entry.Existed)
            {
                if (File.Exists(destination)) File.Delete(destination);
                continue;
            }

            string original = SafePath(Path.Combine(backup, OriginalFolder), entry.RelativePath);
            if (!File.Exists(original))
                throw new InvalidDataException("Missing original in update backup: " + original);
            if (File.Exists(destination) && SameContents(original, destination))
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            string recoveryCopy = Path.Combine(backup, "restore-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.Copy(original, recoveryCopy);
                File.Move(recoveryCopy, destination, overwrite: true);
            }
            finally
            {
                if (File.Exists(recoveryCopy)) File.Delete(recoveryCopy);
            }
        }
    }

    private static bool SameContents(string first, string second)
    {
        if (new FileInfo(first).Length != new FileInfo(second).Length) return false;
        using var a = new FileStream(first, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var b = new FileStream(second, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        return SHA256.HashData(a).SequenceEqual(SHA256.HashData(b));
    }

    private static Journal ReadJournal(string backup, string expectedTarget)
    {
        string path = Path.Combine(backup, JournalFile);
        Journal journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(path))
            ?? throw new InvalidDataException("The update recovery journal is empty.");
        if (!string.Equals(Path.GetFullPath(journal.TargetDirectory), expectedTarget,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)
            || journal.Entries is null || journal.Entries.Length > 100_000)
            throw new InvalidDataException("The update recovery journal does not match its installation.");

        foreach (Entry entry in journal.Entries)
        {
            _ = SafePath(expectedTarget, entry.RelativePath);
            _ = SafePath(Path.Combine(backup, OriginalFolder), entry.RelativePath);
        }
        return journal;
    }

    private static string SafePath(string basePath, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)
            || relative.Split(['/', '\\']).Any(segment => segment is "" or "." or ".."))
            throw new InvalidDataException("Unsafe update file path: " + relative);
        string full = Path.GetFullPath(Path.Combine(basePath, relative));
        string root = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException("Update file escaped its target directory.");
        return full;
    }

    private static void TryDeleteBackup(string backup)
    {
        try { if (Directory.Exists(backup)) Directory.Delete(backup, recursive: true); }
        catch (IOException) { /* Backup cleanup is best effort after a committed transaction. */ }
        catch (UnauthorizedAccessException) { /* Keep stale backup instead of failing an update. */ }
    }
}
