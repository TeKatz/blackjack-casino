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
