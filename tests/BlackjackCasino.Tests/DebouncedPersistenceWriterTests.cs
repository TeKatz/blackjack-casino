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

    [Fact]
    public void GenericWriterCoalescesReferencePayloads()
    {
        var writes = new List<Payload>();
        using var writer = new DebouncedPersistenceWriter<Payload>(TimeSpan.FromMinutes(1), writes.Add);

        writer.Schedule(new Payload(1));
        writer.Schedule(new Payload(2));
        writer.Schedule(new Payload(3));
        writer.Flush();

        Payload written = Assert.Single(writes);
        Assert.Equal(3, written.Value);
    }

    private sealed record Payload(int Value);
}
