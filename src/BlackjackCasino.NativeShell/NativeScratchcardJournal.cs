using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Scratchcards;

namespace BlackjackCasino.HostShared;

internal sealed record NativeScratchcardCheckpoint(int Version, bool Pending,
    AppPreferences Preferences, ScratchcardTicket[] Tickets);

/// <summary>
/// Write-ahead journal for purchases and payouts. A ticket ID is immutable,
/// a purchased ticket survives restart, and collecting it twice cannot pay twice.
/// The financial candidate is replayed before loading the normal player state.
/// </summary>
internal sealed class NativeScratchcardJournal
{
    private const int SchemaVersion = 1;
    private readonly string folder;
    private NativeScratchcardCheckpoint? state;
    private string FileName => Path.Combine(folder, "native-scratchcards-checkpoint.json");

    internal NativeScratchcardJournal(string directory)
    {
        folder = directory;
        state = Load();
    }

    internal ScratchcardTicket? Latest => state?.Tickets.LastOrDefault();
    internal int Purchased => state?.Tickets.Length ?? 0;

    internal void Recover()
    {
        NativeScratchcardCheckpoint? pending = Load();
        if (pending is not { Pending: true }) { state = pending; return; }
        PlayerStateStore.SaveOrThrow(pending.Preferences, folder);
        state = pending with { Pending = false };
        Write(state);
    }

    internal ScratchcardTicket Purchase(Guid id, AppPreferences preferences,
        Action<AppPreferences>? mirrorWriter = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Ticket ID is required.", nameof(id));
        ScratchcardTicket? replay = state?.Tickets.FirstOrDefault(ticket => ticket.TicketId == id);
        if (replay is not null) return replay;
        if (state is { Pending: true })
            throw new InvalidOperationException("Scratchcard recovery is required.");
        if (Latest is { Collected: false })
            throw new InvalidOperationException("Collect the previous scratchcard before buying another.");
        if (preferences.Bankroll < ScratchcardRules.TicketPrice)
            throw new InvalidOperationException("Insufficient bankroll for a scratchcard.");

        decimal newBalance = preferences.Bankroll - ScratchcardRules.TicketPrice;
        ScratchcardTicket ticket = ScratchcardRules.Draw(id, newBalance);
        AppPreferences candidate = preferences with { Bankroll = newBalance };
        var tickets = (state?.Tickets ?? []).Append(ticket).ToArray();
        Commit(candidate, tickets, mirrorWriter);
        return ticket;
    }

    internal ScratchcardTicket Collect(Guid id, AppPreferences preferences,
        Action<AppPreferences>? mirrorWriter = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Ticket ID is required.", nameof(id));
        ScratchcardTicket? ticket = state?.Tickets.FirstOrDefault(entry => entry.TicketId == id);
        if (ticket is null) throw new InvalidOperationException("Unknown scratchcard.");
        if (ticket.Collected) return ticket; // Durable idempotent replay.
        if (state is { Pending: true })
            throw new InvalidOperationException("Scratchcard recovery is required.");
        if (Latest?.TicketId != id)
            throw new InvalidOperationException("Only the current ticket can be collected.");

        decimal balance = checked(preferences.Bankroll + ticket.Prize);
        ScratchcardTicket claimed = ticket with { Collected = true, BankrollAfter = balance };
        AppPreferences candidate = preferences with { Bankroll = balance };
        ScratchcardTicket[] tickets = state!.Tickets.Select(entry =>
            entry.TicketId == id ? claimed : entry).ToArray();
        Commit(candidate, tickets, mirrorWriter);
        return claimed;
    }

    private void Commit(AppPreferences candidate, ScratchcardTicket[] tickets,
        Action<AppPreferences>? mirrorWriter)
    {
        var pending = new NativeScratchcardCheckpoint(SchemaVersion, true, candidate, tickets);
        Write(pending);
        (mirrorWriter ?? (value => PlayerStateStore.SaveOrThrow(value, folder)))(candidate);
        var settled = pending with { Pending = false };
        Write(settled);
        state = settled;
    }

    private NativeScratchcardCheckpoint? Load()
    {
        if (!File.Exists(FileName)) return null;
        using var stream = File.OpenRead(FileName);
        NativeScratchcardCheckpoint checkpoint =
            JsonSerializer.Deserialize<NativeScratchcardCheckpoint>(stream)
            ?? throw new InvalidDataException("Empty scratchcard ledger.");
        if (checkpoint.Version != SchemaVersion || checkpoint.Preferences is null
            || checkpoint.Preferences.Bankroll < 0m || checkpoint.Tickets is null)
            throw new InvalidDataException("Invalid scratchcard ledger.");
        var ids = new HashSet<Guid>();
        foreach (ScratchcardTicket ticket in checkpoint.Tickets)
        {
            if (ticket is null || !ScratchcardRules.Valid(ticket) ||
                !ids.Add(ticket.TicketId))
                throw new InvalidDataException("Invalid or duplicated scratchcard receipt.");
        }
        if (checkpoint.Tickets.Count(ticket => !ticket.Collected) > 1
            || checkpoint.Tickets.SkipLast(1).Any(ticket => !ticket.Collected)
            || checkpoint.Pending && checkpoint.Tickets.Length == 0
            || checkpoint.Pending && checkpoint.Tickets[^1].BankrollAfter != checkpoint.Preferences.Bankroll)
            throw new InvalidDataException("Inconsistent scratchcard ledger.");
        return checkpoint;
    }

    private void Write(NativeScratchcardCheckpoint checkpoint)
    {
        Directory.CreateDirectory(folder);
        string temp = FileName + ".tmp";
        using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(file, checkpoint);
            file.Flush(flushToDisk: true);
        }
        File.Move(temp, FileName, overwrite: true);
    }
}
