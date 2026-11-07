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
