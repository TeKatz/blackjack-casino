using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

/// <summary>
/// Tests the exact WAL source compiled into both the installed WPF host and
/// NativeShell. A roulette request ID is forever associated with one outcome.
/// </summary>
public sealed class SharedRouletteJournalTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(),
        "casino-shared-roulette-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void OldNativeReceiptIsReadableToWpfAndReusedAcrossRestart()
    {
        Directory.CreateDirectory(directory);
        Guid originalId = Guid.NewGuid();
        var original = new NativeRouletteReceipt(originalId, true, 7, 100m, 0m,
            -100m, 900m);
        File.WriteAllText(Path.Combine(directory, "native-roulette-checkpoint.json"),
            JsonSerializer.Serialize(new NativeRouletteCheckpoint(1, false,
                new AppPreferences { Bankroll = 900m }, [original])));

        // The WPF test assembly calls the SAME linked source type. Its reader
        // must preserve the v1 NativeShell JSON contract unmodified.
        var wpfReader = new NativeRouletteJournal(directory);
        Assert.Equal(1, NativeRouletteJournal.ValidateSettled(directory));
        Assert.True(wpfReader.TryReceipt(originalId, out NativeRouletteReceipt? replay));
        Assert.Equal(original, replay);

        RouletteResult next = Roulette.Settle([new RouletteBet("red", 100m)],
            900m, 1);
        var nextState = new AppPreferences { Bankroll = next.Bankroll };
        Guid nextId = Guid.NewGuid();
        NativeRouletteReceipt added = wpfReader.Commit(nextId, next, nextState);
        Assert.Equal(next.Bankroll, PlayerStateStore.Load(directory).Bankroll);

        var nativeReopen = new NativeRouletteJournal(directory);
        Assert.Equal(2, NativeRouletteJournal.ValidateSettled(directory));
        Assert.True(nativeReopen.TryReceipt(originalId, out replay));
        Assert.Equal(original, replay);
        Assert.True(nativeReopen.TryReceipt(nextId, out replay));
        Assert.Equal(added, replay);
        // Retrying the ORIGINAL id is not a new settlement, even with a
        // different random wheel outcome or current player balance.
        RouletteResult different = Roulette.Settle([new RouletteBet("black", 100m)],
            next.Bankroll, 0);
        NativeRouletteReceipt unchanged =
            nativeReopen.Commit(originalId, different, nextState);
        Assert.Equal(original, unchanged);
        Assert.Equal(next.Bankroll, PlayerStateStore.Load(directory).Bankroll);
    }

    [Fact]
    public void PendingWriteAheadSettlementRecoversExactlyOnceAcrossHosts()
    {
        var journal = new NativeRouletteJournal(directory);
        Guid id = Guid.NewGuid();
        RouletteResult result = Roulette.Settle([new RouletteBet("red", 100m)],
            1000m, 1);
        var state = new AppPreferences { Bankroll = result.Bankroll };
        Assert.Throws<IOException>(() =>
            journal.Commit(id, result, state,
                _ => throw new IOException("Simulated crash before wallet mirror.")));
        Assert.Throws<InvalidDataException>(() =>
        {
            _ = NativeRouletteJournal.ValidateSettled(directory);
        });

        // Either host can resume the same journal, finish the mirror, and
        // recognize the receipt without settling it a second time.
        var restartedWpf = new NativeRouletteJournal(directory);
        restartedWpf.Recover();
        Assert.Equal(result.Bankroll, PlayerStateStore.Load(directory).Bankroll);
        Assert.Equal(1, NativeRouletteJournal.ValidateSettled(directory));
        Assert.True(restartedWpf.TryReceipt(id, out var receipt));
        Assert.Equal(result.Bankroll, receipt!.Bankroll);
        restartedWpf.Recover();
        Assert.Equal(result.Bankroll, PlayerStateStore.Load(directory).Bankroll);
    }

    [Fact]
    public void CorruptOrDuplicateReceiptIdsFailClosed()
    {
        Directory.CreateDirectory(directory);
        Guid duplicate = Guid.NewGuid();
        var same = new NativeRouletteReceipt(duplicate, true, 18, 100m, 0m,
            -100m, 900m);
        File.WriteAllText(Path.Combine(directory, "native-roulette-checkpoint.json"),
            JsonSerializer.Serialize(new NativeRouletteCheckpoint(1, false,
                new AppPreferences { Bankroll = 900m }, [same, same])));
        Assert.Throws<InvalidDataException>(() =>
            new NativeRouletteJournal(directory));
        Assert.Throws<InvalidDataException>(() =>
        {
            _ = NativeRouletteJournal.ValidateSettled(directory);
        });
    }

    public void Dispose()
    {
        try { Directory.Delete(directory, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
