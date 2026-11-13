using System.IO.Compression;
using System.Security.Cryptography;
using BlackjackCasino.Update;

namespace BlackjackCasino.Tests;

/// <summary>
/// Offline reversible host-switch rehearsal using fake installations and player
/// data; this does not approve any release-channel switch.
/// </summary>
public sealed class HostCutoverRehearsalTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(),
        "blackjack-host-cutover-" + Guid.NewGuid().ToString("N"));

    public HostCutoverRehearsalTests() => Directory.CreateDirectory(root);

    [Fact]
    public void DualHostUpgradeAndLegacyRollbackPreservePlayerData()
    {
        string target = Path.Combine(root, "casino");
        string userData = WriteUserData();
        string fingerprint = Fingerprint(userData);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old-wpf");
        File.WriteAllText(Path.Combine(target, "old-art.txt"), "old-ui");

        string dualHost = Package(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "new-wpf-fallback",
            ["BlackjackCasino.NativeShell.exe"] = "new-native",
            ["ModernFrontend/lobby.html"] = "react-ui",
            ["MidnightDescent/MidnightDescent.exe"] = "godot",
            ["MidnightDescent/MidnightDescent.pck"] = "godot-data",
            ["tools/updater/BlackjackCasino.Updater.exe"] = "updater",
        });
        PackageInstaller.InstallFromZip(dualHost, target, "BlackjackCasino.Desktop.exe");
        Assert.Equal("new-wpf-fallback", File.ReadAllText(
            PackageInstaller.GetRestartPath(target, "BlackjackCasino.Desktop.exe")));
        Assert.Equal("new-native", File.ReadAllText(
            PackageInstaller.GetRestartPath(target, "BlackjackCasino.NativeShell.exe")));
        Assert.True(File.Exists(Path.Combine(target, "ModernFrontend", "lobby.html")));
        Assert.Equal(fingerprint, Fingerprint(userData));

        string rollback = Package(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "old-wpf",
            ["old-art.txt"] = "old-ui",
            ["tools/updater/BlackjackCasino.Updater.exe"] = "old-updater",
        });
        PackageInstaller.InstallFromZip(rollback, target, "BlackjackCasino.Desktop.exe");
        Assert.Equal("old-wpf", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.False(File.Exists(Path.Combine(target, "BlackjackCasino.NativeShell.exe")));
        Assert.False(File.Exists(Path.Combine(target, "ModernFrontend", "lobby.html")));
        Assert.Equal(fingerprint, Fingerprint(userData));
    }

    [Fact]
    public void LegacyUpdaterRejectsNativeOnlyPayloadBeforeReplacingExistingInstall()
    {
        string target = Path.Combine(root, "casino");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "old-wpf");
        string invalid = Package(new Dictionary<string, string>
        {
            ["BlackjackCasino.NativeShell.exe"] = "native-only",
        });
        Assert.Throws<InvalidDataException>(() => PackageInstaller.InstallFromZip(
            invalid, target, "BlackjackCasino.Desktop.exe"));
        Assert.Equal("old-wpf", File.ReadAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.False(File.Exists(Path.Combine(target, "BlackjackCasino.NativeShell.exe")));
    }

    [Fact]
    public void FaultDuringNativeBinaryReplacementRollsBackBothHostsAndPreservesSaves()
    {
        string target = Path.Combine(root, "casino");
        string userData = WriteUserData();
        string fingerprint = Fingerprint(userData);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.Desktop.exe"), "fallback-before");
        File.WriteAllText(Path.Combine(target, "BlackjackCasino.NativeShell.exe"), "native-before");
        string candidate = Package(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "fallback-after",
            ["BlackjackCasino.NativeShell.exe"] = "native-after",
            ["z-fail-after-native.txt"] = "must-rollback",
        });
        Assert.Throws<InvalidOperationException>(() => PackageInstaller.InstallFromZip(
            candidate, target, "BlackjackCasino.Desktop.exe", onRetry: null,
            directoryMove: (from, to) =>
            {
                if (string.Equals(from, target, StringComparison.Ordinal))
                    throw new IOException("Simulated installation root lock");
                Directory.Move(from, to);
            },
            delay: _ => { },
            fileMove: (from, to, overwrite) =>
            {
                if (Path.GetFileName(to) == "z-fail-after-native.txt")
                    throw new InvalidOperationException("Simulated partial cutover");
                File.Move(from, to, overwrite);
            }));
        Assert.Equal("fallback-before", File.ReadAllText(
            Path.Combine(target, "BlackjackCasino.Desktop.exe")));
        Assert.Equal("native-before", File.ReadAllText(
            Path.Combine(target, "BlackjackCasino.NativeShell.exe")));
        Assert.False(File.Exists(Path.Combine(target, "z-fail-after-native.txt")));
        Assert.Empty(Directory.EnumerateDirectories(root, ".casino.inplace-backup-*"));
        Assert.Equal(fingerprint, Fingerprint(userData));
    }

    [Fact]
    public void ExplicitNativeRestartRequiresTheNativeExecutable()
    {
        string target = Path.Combine(root, "casino");
        string dual = Package(new Dictionary<string, string>
        {
            ["BlackjackCasino.Desktop.exe"] = "wpf-rollback",
            ["BlackjackCasino.NativeShell.exe"] = "native-selected",
        });
        PackageInstaller.InstallFromZip(dual, target, "BlackjackCasino.NativeShell.exe");
        Assert.Equal("native-selected", File.ReadAllText(
            PackageInstaller.GetRestartPath(target, "BlackjackCasino.NativeShell.exe")));
        Assert.Equal("wpf-rollback", File.ReadAllText(
            PackageInstaller.GetRestartPath(target, "BlackjackCasino.Desktop.exe")));
    }

    private string WriteUserData()
    {
        string directory = Path.Combine(root, "user-data", "BlackjackCasino");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "player-state.json"), "{\"Bankroll\":9876.50}");
        File.WriteAllText(Path.Combine(directory, "profile.json"), "{\"DisplayName\":\"Cutover Test\"}");
        string browser = Path.Combine(directory, "WebView2", "Default", "Local Storage");
        Directory.CreateDirectory(browser);
        File.WriteAllBytes(Path.Combine(browser, "arcade-records"), [0, 5, 19, 255]);
        return directory;
    }

    private static string Fingerprint(string directory)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                     .OrderBy(x => x, StringComparer.Ordinal))
        {
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(
                Path.GetRelativePath(directory, file).Replace('\\', '/')));
            hash.AppendData(File.ReadAllBytes(file));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private string Package(IReadOnlyDictionary<string, string> entries)
    {
        string folder = Path.Combine(root, "package-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        foreach (var entry in entries)
        {
            string file = Path.Combine(folder,
                entry.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, entry.Value);
        }
        string zip = folder + ".zip";
        ZipFile.CreateFromDirectory(folder, zip);
        Directory.Delete(folder, recursive: true);
        return zip;
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
