using System.IO.Compression;
using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

public sealed class PackageInstallerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "BlackjackCasino.Tests", Guid.NewGuid().ToString("N"));

    public PackageInstallerTests()
    {
        Directory.CreateDirectory(root);
    }

    [Fact]
    public void InstallFromZip_ReplacesExistingInstallation()
    {
        string target = Path.Combine(root, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "old.txt"), "old");
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old-exe");

        string package = CreatePackage(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "new-exe",
            ["new.txt"] = "new",
            ["tools/updater/BlackjackCasino.Updater.exe"] = "updater",
        });

        PackageInstaller.InstallFromZip(package, target, "BlackjackCasino.Desktop.exe");

        Assert.False(File.Exists(Path.Combine(target, "old.txt")));
        Assert.Equal("new-exe", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.Equal("new", File.ReadAllText(Path.Combine(target, "new.txt")));
    }

    [Fact]
    public void InstallFromZip_InvalidPackageLeavesExistingInstallationUntouched()
    {
        string target = Path.Combine(root, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old-exe");
        File.WriteAllText(Path.Combine(target, "keep.txt"), "keep");

        string package = CreatePackage(new Dictionary<string, string>
        {
            ["not-the-game.exe"] = "invalid",
        });

        Assert.Throws<InvalidDataException>(() =>
            PackageInstaller.InstallFromZip(package, target, "BlackjackCasino.Desktop.exe"));

        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.Equal("keep", File.ReadAllText(Path.Combine(target, "keep.txt")));
    }

    [Fact]
    public void LockedInstallationRoot_FallsBackToJournaledFileUpdates()
    {
        string target = Path.Combine(root, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old");
        File.WriteAllText(Path.Combine(target, "personal-note.txt"), "unchanged");
        string package = CreatePackage(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "new",
            ["modern-frontend.txt"] = "new-ui",
        });
        var stages = new List<string>();
        PackageInstaller.InstallFromZip(package, target, "BlackjackCasino.Desktop.exe",
            onRetry: (stage, _, _) => stages.Add(stage),
            directoryMove: (from, to) =>
            {
                if (string.Equals(from, target, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException("The directory is in use.");
                Directory.Move(from, to);
            },
            delay: _ => { },
            fileMove: null);

        Assert.Contains("fallback-in-place", stages);
        Assert.Equal("new", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.Equal("new-ui", File.ReadAllText(Path.Combine(target, "modern-frontend.txt")));
        Assert.Equal("unchanged", File.ReadAllText(Path.Combine(target, "personal-note.txt")));
        Assert.Empty(Directory.EnumerateDirectories(root, ".app.inplace-backup-*"));
    }

    [Fact]
    public void FailedInPlaceReplacement_RestoresPreviousFilesAndDeletesIntroducedFiles()
    {
        string target = Path.Combine(root, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old-exe");
        File.WriteAllText(Path.Combine(target, "a-config.txt"), "old-settings");
        string package = CreatePackage(new Dictionary<string, string>
        {
            ["a-config.txt"] = "new-settings",
            ["z-fail.txt"] = "never-install",
            ["BlackjackCasino.Desktop.exe"] = "new-exe",
        });

        Assert.Throws<InvalidOperationException>(() =>
            PackageInstaller.InstallFromZip(package, target, "BlackjackCasino.Desktop.exe",
                onRetry: null,
                directoryMove: (from, to) =>
                {
                    if (string.Equals(from, target, StringComparison.Ordinal))
                        throw new IOException("The root directory is locked.");
                    Directory.Move(from, to);
                },
                delay: _ => { },
                fileMove: (from, to, overwrite) =>
                {
                    if (Path.GetFileName(to) == "z-fail.txt")
                        throw new InvalidOperationException("Simulated fault after an earlier replacement.");
                    File.Move(from, to, overwrite);
                }));

        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.Equal("old-settings", File.ReadAllText(Path.Combine(target, "a-config.txt")));
        Assert.False(File.Exists(Path.Combine(target, "z-fail.txt")));
        Assert.Empty(Directory.EnumerateDirectories(root, ".app.inplace-backup-*"));
    }

    [Fact]
    public void InterruptedInPlaceUpdate_IsRolledBackFromDurableJournalBeforeAnotherAttempt()
    {
        string target = Path.Combine(root, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "new-uncommitted-exe");
        File.WriteAllText(Path.Combine(target, "new-file.txt"), "uncommitted");
        string backup = Path.Combine(root, ".app.inplace-backup-crash-fixture");
        string originals = Path.Combine(backup, "originals");
        Directory.CreateDirectory(originals);
        File.WriteAllText(Path.Combine(originals, "BlackjackCasino.Desktop.exe"), "old-exe");
        File.WriteAllText(Path.Combine(backup, "manifest.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                TargetDirectory = target,
                Entries = new[]
                {
                    new { RelativePath = "BlackjackCasino.Desktop.exe", Existed = true },
                    new { RelativePath = "new-file.txt", Existed = false },
                },
            }));
        File.WriteAllText(Path.Combine(backup, "transaction-active"), "1");

        InPlacePackageInstaller.RecoverPending(target);

        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.False(File.Exists(Path.Combine(target, "new-file.txt")));
        Assert.False(Directory.Exists(backup));
        // A second recovery is idempotent.
        InPlacePackageInstaller.RecoverPending(target);
        Assert.Equal("old-exe", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
    }

    [Fact]
    public void WindowsDirectoryHandleWithoutDeleteSharing_DoesNotPreventInPlaceUpdate()
    {
        if (!OperatingSystem.IsWindows()) return;
        string target = Path.Combine(root, "app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old");
        string package = CreatePackage(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "new",
            ["new-ui.txt"] = "works",
        });

        // This is a REAL Windows root-folder lock, not a mocked IOException.
        // A handle without FILE_SHARE_DELETE blocks Directory.Move on the root
        // while still permitting updates to files within that directory.
        using (var lockedRoot = OpenDirectoryWithoutDeleteSharing(target,
            0, 0x00000001 | 0x00000002, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero))
        {
            if (lockedRoot.IsInvalid)
                throw new System.ComponentModel.Win32Exception(
                    System.Runtime.InteropServices.Marshal.GetLastWin32Error());
            Exception? renameError = Record.Exception(() =>
                Directory.Move(target, Path.Combine(root, "unexpected-rename")));
            Assert.True(renameError is IOException or UnauthorizedAccessException,
                "The test must demonstrate the directory rename is actually blocked.");

            PackageInstaller.InstallFromZip(package, target, "BlackjackCasino.Desktop.exe");
            Assert.Equal("new", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
            Assert.Equal("works", File.ReadAllText(Path.Combine(target, "new-ui.txt")));
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll",
        EntryPoint = "CreateFileW", CharSet = System.Runtime.InteropServices.CharSet.Unicode,
        SetLastError = true)]
    private static extern Microsoft.Win32.SafeHandles.SafeFileHandle OpenDirectoryWithoutDeleteSharing(
        string path, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [Fact]
    public void DirectoryMove_TransientWindowsLocksAreRetriedWithoutReextracting()
    {
        string source = Path.Combine(root, "locked-app");
        string destination = Path.Combine(root, "restored-app");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "save-safe.txt"), "preserve");
        int tries = 0;
        int delays = 0;
        PackageInstaller.MoveDirectoryWithRetries(source, destination,
            move: (from, to) =>
            {
                if (++tries < 4) throw new IOException("File is temporarily locked.");
                Directory.Move(from, to);
            },
            delay: _ => delays++,
            maxAttempts: 5);

        Assert.Equal(4, tries);
        Assert.Equal(3, delays);
        Assert.False(Directory.Exists(source));
        Assert.Equal("preserve", File.ReadAllText(Path.Combine(destination, "save-safe.txt")));
    }

    [Fact]
    public void DirectoryMove_LongLivedTransientLockRecoversBeyondOldRetryBudget()
    {
        string source = Path.Combine(root, "slow-release");
        string destination = Path.Combine(root, "after-webview-shutdown");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "existing-save.txt"), "preserved");

        int attempts = 0;
        var delays = new List<TimeSpan>();
        PackageInstaller.MoveDirectoryWithRetries(source, destination,
            move: (from, to) =>
            {
                // The old 12-attempt budget exhausted before this unlock.
                if (++attempts < 19) throw new UnauthorizedAccessException("WebView2 is closing.");
                Directory.Move(from, to);
            },
            delay: delay => delays.Add(delay));

        Assert.Equal(19, attempts);
        Assert.Equal(18, delays.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(200), delays[0]);
        Assert.Equal(TimeSpan.FromSeconds(3), delays[^1]);
        Assert.Equal("preserved", File.ReadAllText(Path.Combine(destination, "existing-save.txt")));
    }

    [Fact]
    public void DirectoryMove_RetryBudgetIsFiniteAndDelayIsCapped()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(200),
            PackageInstaller.GetDirectoryMoveRetryDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(3),
            PackageInstaller.GetDirectoryMoveRetryDelay(15));
        Assert.Equal(TimeSpan.FromSeconds(3),
            PackageInstaller.GetDirectoryMoveRetryDelay(36));
        Assert.Equal(TimeSpan.FromSeconds(87),
            TimeSpan.FromMilliseconds(Enumerable.Range(1, 36)
                .Sum(attempt => PackageInstaller.GetDirectoryMoveRetryDelay(attempt).TotalMilliseconds)));
    }

    [Fact]
    public void DirectoryMove_PermanentLockStopsAfterBoundedRetryBudget()
    {
        string source = Path.Combine(root, "still-locked");
        string destination = Path.Combine(root, "should-not-exist");
        Directory.CreateDirectory(source);
        int attempts = 0;
        Assert.Throws<UnauthorizedAccessException>(() =>
            PackageInstaller.MoveDirectoryWithRetries(source, destination,
                move: (_, _) =>
                {
                    attempts++;
                    throw new UnauthorizedAccessException("Cannot rename.");
                },
                delay: _ => { }, maxAttempts: 3));
        Assert.Equal(3, attempts);
        Assert.True(Directory.Exists(source));
        Assert.False(Directory.Exists(destination));
    }

    [Fact]
    public void DirectoryMove_NonFilesystemErrorIsNotRetried()
    {
        int attempts = 0;
        Assert.Throws<InvalidOperationException>(() =>
            PackageInstaller.MoveDirectoryWithRetries("a", "b",
                move: (_, _) =>
                {
                    attempts++;
                    throw new InvalidOperationException("Invalid update operation.");
                }, delay: _ => { }, maxAttempts: 6));
        Assert.Equal(1, attempts);
    }

    [Theory]
    [InlineData("../outside.exe")]
    [InlineData("folder/../../outside.exe")]
    public void RestartPath_RejectsTraversal(string path)
    {
        Assert.Throws<ArgumentException>(() => PackageInstaller.GetRestartPath(root, path));
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

    private string CreatePackage(IReadOnlyDictionary<string, string> files)
    {
        string source = Path.Combine(root, $"source-{Guid.NewGuid():N}");
        Directory.CreateDirectory(source);

        foreach ((string relativePath, string contents) in files)
        {
            string path = Path.Combine(source, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
        }

        string package = Path.Combine(root, $"package-{Guid.NewGuid():N}.zip");
        ZipFile.CreateFromDirectory(source, package);
        Directory.Delete(source, recursive: true);
        return package;
    }
}
