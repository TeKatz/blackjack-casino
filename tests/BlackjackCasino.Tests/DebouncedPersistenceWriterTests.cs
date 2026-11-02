using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class DebouncedPersistenceWriterTests
{
    [Fact]
    public void FlushCoalescesBurstToLatestPayload()
    {
        var writes = new List<string>();
        using var writer = new DebouncedPersistenceWriter(TimeSpan.FromMinutes(1), writes.Add);

        writer.Schedule("first");
        writer.Schedule("second");
        writer.Schedule("latest");
        writer.Flush();

        Assert.Equal(new[] { "latest" }, writes);
    }

    [Fact]
    public void FlushLeavesNoPayloadForDisposeToRewrite()
    {
        var writes = new List<string>();
        var writer = new DebouncedPersistenceWriter(TimeSpan.FromMinutes(1), writes.Add);

        writer.Schedule("snapshot");
        writer.Flush();
        writer.Dispose();

        Assert.Equal(new[] { "snapshot" }, writes);
    }
}
