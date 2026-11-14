using BlackjackCasino.Core.Hosting;

namespace BlackjackCasino.Tests;

public sealed class HostWriterLeaseTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(),
        "casino-writer-lease-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void FirstOwnerBlocksSecondWriterBeforeAnySaveIsRead()
    {
        using var first = HostWriterLease.Acquire(root);
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => HostWriterLease.Acquire(root));
        Assert.Contains("already in use", error.Message);
        Assert.True(File.Exists(Path.Combine(root, HostWriterLease.FileName)));
    }

    [Fact]
    public void ReleasingAWriterLetsTheNextHostOpenTheSameProfile()
    {
        using (HostWriterLease.Acquire(root)) { }
        using var next = HostWriterLease.Acquire(root);
        Assert.True(File.Exists(Path.Combine(root, HostWriterLease.FileName)));
    }

    [Fact]
    public void LockFileMayPersistAfterCleanShutdownWithoutPreventingStartup()
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, HostWriterLease.FileName), "");
        using var lease = HostWriterLease.Acquire(root);
        Assert.Throws<InvalidOperationException>(() => HostWriterLease.Acquire(root));
    }

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
