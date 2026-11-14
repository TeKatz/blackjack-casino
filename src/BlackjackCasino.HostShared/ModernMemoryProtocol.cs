using System.Text.Json;
using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Desktop;

/// <summary>Allow-listed Memory user intentions. The browser cannot send cards, seed, matches or rewards.</summary>
internal sealed record ModernMemoryCommand(
    string RequestId, string Session, long ExpectedRevision, string Action,
    MemoryDifficulty? Difficulty, string? DeckId, int? Index);

internal sealed record ModernMemoryReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernMemoryProtocol
{
    internal static ModernMemoryCommand? Parse(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String || type.GetString() != "lounge.memory.command"
                || !root.TryGetProperty("version", out JsonElement version)
                || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int v) || v != 1
                || !root.TryGetProperty("expectedRevision", out JsonElement revision)
                || revision.ValueKind != JsonValueKind.Number || !revision.TryGetInt64(out long expected) || expected < 0)
                return null;

            string? Str(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement value)) return null;
                return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            }
            int? Int(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement value)) return null;
                return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int parsed)
                    ? parsed : int.MinValue;
            }
            string? id = Str("requestId"), session = Str("session"), action = Str("action");
            string? difficultyName = Str("difficulty"), deck = Str("deckId");
            int? index = Int("index");
            if (!Guid.TryParseExact(id, "D", out _)
                || !Guid.TryParseExact(session, "D", out _) || index == int.MinValue)
                return null;
            MemoryDifficulty? difficulty = Enum.TryParse(difficultyName, out MemoryDifficulty parsed)
                && Enum.IsDefined(parsed) && parsed.ToString() == difficultyName ? parsed : null;
            bool noDifficulty = difficultyName is null;
            bool noDeck = deck is null;
            bool noIndex = index is null;
            bool valid = action switch
            {
                "memory.deck" => noDifficulty && noIndex && deck is not null
                    && MemoryCatalog.TryGetDeck(deck, out _),
                "memory.start" => difficulty is not null && noDeck && noIndex,
                "memory.flip" => noDifficulty && noDeck && index is >= 0 and < 64,
                "memory.resume" or "memory.again" or "memory.back" or "memory.exit" =>
                    noDifficulty && noDeck && noIndex,
                _ => false,
            };
            return valid ? new(id!, session!, expected, action!, difficulty, deck, index) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return null; }
    }
}

internal sealed class ModernMemorySession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernMemoryReply> receipts = new(StringComparer.Ordinal);

    internal ModernMemoryReply Execute(ModernMemoryCommand request,
        Func<ModernMemoryCommand, (bool Success, string Message)> commit)
    {
        if (request.Session != Id)
            return new(request.RequestId, false, "Memory session expired.", Revision);
        if (receipts.TryGetValue(request.RequestId, out ModernMemoryReply? cached)) return cached;
        if (request.ExpectedRevision != Revision)
            return new(request.RequestId, false, "Memory board changed. Review the current cards.", Revision);
        if (receipts.Count >= 16_384)
            return new(request.RequestId, false, "Please reopen the Lounge to continue.", Revision);

        (bool success, string message) = commit(request);
        if (success) Revision = checked(Revision + 1);
        var receipt = new ModernMemoryReply(request.RequestId, success, message, Revision);
        receipts.Add(request.RequestId, receipt);
        return receipt;
    }
}
