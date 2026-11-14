namespace BlackjackCasino.Core.Hosting;

/// <summary>
/// Cross-process, single-writer lease for the installed player's save root.
/// FileShare.None is an OS-enforced lock, unlike a marker/timestamp that can be
/// stale after crashes. Keep it open until all pending saves are flushed.
/// </summary>
public sealed class HostWriterLease : IDisposable
{
    public const string FileName = ".casino-host-writer.lock";

    private readonly FileStream stream;
    private bool disposed;

    private HostWriterLease(FileStream stream) => this.stream = stream;

    public static HostWriterLease Acquire(string playerDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerDirectory);
        string root = Path.GetFullPath(playerDirectory);
        if (Directory.Exists(root) &&
            (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The player-data directory may not be a filesystem link.");

        Directory.CreateDirectory(root);
        string marker = Path.Combine(root, FileName);
        if (File.Exists(marker) &&
            (File.GetAttributes(marker) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The player-data lock may not be a filesystem link.");

        try
        {
            return new HostWriterLease(new FileStream(marker,
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                "The installed casino player data is already in use. "
                + "Close the other casino, updater or migration window before continuing.", ex);
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        stream.Dispose();
    }
}
