using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.HostShared;

/// <summary>
/// Stable cross-version roulette receipt with browser-compatible JSON names.
/// </summary>
internal sealed record NativeRouletteReceipt(Guid RoundId, bool Success,
    int Number, decimal Stake, decimal Return, decimal Net, decimal Bankroll);

internal sealed record NativeRouletteCheckpoint(int Version, bool Pending,
    AppPreferences Preferences, NativeRouletteReceipt[] Receipts);

/// <summary>
/// Journals the financial snapshot before modifying player-state.json on the native host.
/// A pending checkpoint replays the same candidate after an interruption;
/// all request IDs remain retained for cross-restart idempotency.
/// </summary>
internal sealed class NativeRouletteJournal
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Json = CreateJson();
    private readonly string folder;
    private readonly Dictionary<Guid, NativeRouletteReceipt> receipts = new();
    private string PathName => Path.Combine(folder, "native-roulette-checkpoint.json");

    internal NativeRouletteJournal(string directory)
    {
        folder = directory;
        var checkpoint = Load();
        if (checkpoint is not null)
            foreach (NativeRouletteReceipt receipt in checkpoint.Receipts)
                if (!receipts.TryAdd(receipt.RoundId, receipt))
                    throw new InvalidDataException("Duplicate roulette journal receipt.");
    }

    internal bool TryReceipt(Guid id, out NativeRouletteReceipt? receipt) =>
        receipts.TryGetValue(id, out receipt);

    /// <summary>
    /// Validate an imported ledger without modifying or replaying its wallet.
    /// Pending records must first be recovered by the source host.
    /// </summary>
    internal static int ValidateSettled(string directory)
    {
        var journal = new NativeRouletteJournal(directory);
        NativeRouletteCheckpoint? state = journal.Load();
        if (state is { Pending: true })
            throw new InvalidDataException(
                "Roulette has an unfinished financial settlement.");
        return journal.receipts.Count;
    }

    internal void Recover()
    {
        NativeRouletteCheckpoint? checkpoint = Load();
        if (checkpoint is not { Pending: true }) return;
        PlayerStateStore.SaveOrThrow(checkpoint.Preferences, folder);
        Write(checkpoint with { Pending = false });
    }

    internal NativeRouletteReceipt Commit(Guid id, RouletteResult result,
        AppPreferences candidate, Action<AppPreferences>? mirrorWriter = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Missing roulette receipt ID.", nameof(id));
        if (receipts.TryGetValue(id, out NativeRouletteReceipt? replay)) return replay;
        if (result.Bankroll != candidate.Bankroll || result.Stake < Roulette.Minimum
            || result.Number is < 0 or > 36)
            throw new InvalidDataException("Invalid roulette settlement checkpoint.");

        var receipt = new NativeRouletteReceipt(id, true, result.Number,
            result.Stake, result.Return, result.Net, result.Bankroll);
        var allReceipts = new NativeRouletteReceipt[receipts.Count + 1];
        receipts.Values.CopyTo(allReceipts, 0);
        allReceipts[^1] = receipt;
        var checkpoint = new NativeRouletteCheckpoint(
            SchemaVersion, Pending: true, candidate, allReceipts);

        // Once this write succeeds, any subsequent I/O error is uncertain.
        // The caller MUST stop all additional money transactions until restart.
        Write(checkpoint);
        (mirrorWriter ?? (state => PlayerStateStore.SaveOrThrow(state, folder)))(candidate);
        Write(checkpoint with { Pending = false });
        receipts.Add(id, receipt);
        return receipt;
    }

    private NativeRouletteCheckpoint? Load()
    {
        if (!File.Exists(PathName)) return null;
        using var stream = File.OpenRead(PathName);
        var checkpoint = JsonSerializer.Deserialize<NativeRouletteCheckpoint>(stream, Json)
            ?? throw new InvalidDataException("Empty roulette journal.");
        if (checkpoint.Version != SchemaVersion || checkpoint.Preferences is null
            || checkpoint.Receipts is null || checkpoint.Preferences.Bankroll < 0)
            throw new InvalidDataException("Unsupported roulette journal.");
        foreach (NativeRouletteReceipt receipt in checkpoint.Receipts)
            if (receipt is null || receipt.RoundId == Guid.Empty || !receipt.Success
                || receipt.Stake < Roulette.Minimum || receipt.Bankroll < 0
                || receipt.Number is < 0 or > 36 || receipt.Net != receipt.Return - receipt.Stake)
                throw new InvalidDataException("Invalid roulette journal receipt.");
        if (checkpoint.Pending && (checkpoint.Receipts.Length == 0
            || checkpoint.Receipts[^1].Bankroll != checkpoint.Preferences.Bankroll))
            throw new InvalidDataException("Inconsistent pending roulette balance.");
        return checkpoint;
    }

    private void Write(NativeRouletteCheckpoint checkpoint)
    {
        Directory.CreateDirectory(folder);
        string temp = PathName + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(file, checkpoint, Json);
            file.Flush(flushToDisk: true);
        }
        File.Move(temp, PathName, overwrite: true);
    }

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions { WriteIndented = false };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
