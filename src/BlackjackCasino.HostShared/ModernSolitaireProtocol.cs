using System.Text.Json;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Desktop;

/// <summary>Versioned, allow-listed solitaire intentions. The browser never submits cards, outcomes or rewards.</summary>
internal sealed record ModernSolitaireCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, SolitaireVariant? Variant, int SpiderSuits, int? DealNumber,
    string? Source, int? Index, string? Target);
internal sealed record ModernSolitaireReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernSolitaireProtocol
{
    internal const int Version = 1;

    internal static ModernSolitaireCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out JsonElement type) ||
                type.ValueKind != JsonValueKind.String || type.GetString() != "lounge.solitaire.command" ||
                !root.TryGetProperty("version", out JsonElement v) || !v.TryGetInt32(out int version) ||
                version != Version ||
                !root.TryGetProperty("expectedRevision", out JsonElement r) || !r.TryGetInt64(out long revision) ||
                revision < 0) return null;

            string? Read(string key) => root.TryGetProperty(key, out JsonElement item) &&
                item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            int? Number(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement item)) return null;
                return item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int n) ? n : int.MinValue;
            }

            string? requestId = Read("requestId"), session = Read("session"), action = Read("action");
            string? variantName = Read("variant"), source = Read("source"), target = Read("target");
            int? suits = Number("spiderSuits"), deal = Number("dealNumber"), index = Number("index");
            if (!Guid.TryParseExact(requestId, "D", out _) || !Guid.TryParseExact(session, "D", out _) ||
                suits == int.MinValue || deal == int.MinValue || index == int.MinValue) return null;
            SolitaireVariant? variant = Enum.TryParse(variantName, out SolitaireVariant parsed) &&
                Enum.IsDefined(parsed) && parsed.ToString() == variantName ? parsed : null;
            bool variantRequest = variant is not null && source is null && target is null && index is null &&
                suits is 1 or 2 or 4 && (variant == SolitaireVariant.Spider || suits == 1);
            bool valid = action switch
            {
                "solitaire.new" => variantRequest &&
                    ((variant == SolitaireVariant.FreeCell && (deal is null or >= 1 and <= 2_000_000_000)) ||
                     (variant != SolitaireVariant.FreeCell && deal is null)),
                "solitaire.resume" => variantRequest && deal is null,
                "solitaire.draw" or "solitaire.back" or "solitaire.exit" =>
                    variantName is null && suits is null && deal is null && source is null &&
                    target is null && index is null,
                "solitaire.activate" or "solitaire.auto" => variantName is null && suits is null && deal is null &&
                    ValidPile(source) && index is >= 0 and < 104 && target is null,
                "solitaire.move" => variantName is null && suits is null && deal is null &&
                    ValidPile(source) && ValidPile(target) && source != target && index is >= 0 and < 104,
                _ => false,
            };
            return valid ? new ModernSolitaireCommand(requestId!, session!, revision, action!, variant,
                suits ?? 1, deal, source, index, target) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return null; }
    }

    private static bool ValidPile(string? id)
    {
        if (id is "Stock" or "Waste" or "F-Clubs" or "F-Diamonds" or "F-Hearts" or "F-Spades") return true;
        if (id is null) return false;
        if (id.StartsWith("Cell", StringComparison.Ordinal) &&
            int.TryParse(id.AsSpan(4), out int cell) && cell is >= 0 and < 4 &&
            id == $"Cell{cell}") return true;
        if (id.Length > 1 && id[0] is 'T' or 'C' or 'G' or 'P' &&
            int.TryParse(id.AsSpan(1), out int number) && number is >= 0 and < 28 &&
            id == $"{id[0]}{number}") return true;
        return false;
    }
}

/// <summary>Idempotence is owned by .NET, never by the in-browser presentation.</summary>
internal sealed class ModernSolitaireSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernSolitaireReply> receipts = new(StringComparer.Ordinal);
    internal ModernSolitaireReply Execute(ModernSolitaireCommand command,
        Func<ModernSolitaireCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "The Solitaire session has expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernSolitaireReply? saved)) return saved;
        if (Revision != command.ExpectedRevision)
            return new(command.RequestId, false, "The board changed. Review the latest cards.", Revision);
        if (receipts.Count >= 16384)
            return new(command.RequestId, false, "Please reopen the Lounge to continue.", Revision);
        (bool success, string message) = commit(command);
        if (success) Revision = checked(Revision + 1);
        var reply = new ModernSolitaireReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
