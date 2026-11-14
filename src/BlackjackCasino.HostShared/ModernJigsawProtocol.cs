using System.Text.Json;
using BlackjackCasino.Core.Jigsaw;

namespace BlackjackCasino.Desktop;

/// <summary>Only bounded player intentions cross WebView2. No solution, reward or save may be supplied.</summary>
internal sealed record ModernJigsawCommand(
    string RequestId, string Session, long ExpectedRevision, string Action,
    string? MotifId, int? PieceCount, int? Index, double? X, double? Y, bool? Enabled, bool? ConfirmReplace);

internal sealed record ModernJigsawReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernJigsawProtocol
{
    internal static ModernJigsawCommand? Parse(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out JsonElement type) || type.GetString() != "lounge.jigsaw.command"
                || !root.TryGetProperty("version", out JsonElement version) || !version.TryGetInt32(out int v) || v != 1
                || !root.TryGetProperty("expectedRevision", out JsonElement rev)
                || !rev.TryGetInt64(out long revision) || revision < 0)
                return null;
            string? Str(string key) => root.TryGetProperty(key, out JsonElement e) && e.ValueKind == JsonValueKind.String
                ? e.GetString() : null;
            int? Int(string key) => root.TryGetProperty(key, out JsonElement e) && e.ValueKind == JsonValueKind.Number
                && e.TryGetInt32(out int result) ? result : null;
            double? Number(string key) => root.TryGetProperty(key, out JsonElement e) && e.ValueKind == JsonValueKind.Number
                && e.TryGetDouble(out double result) && double.IsFinite(result) ? result : null;
            bool? Bool(string key) => root.TryGetProperty(key, out JsonElement e) && e.ValueKind is
                (JsonValueKind.True or JsonValueKind.False) ? e.GetBoolean() : null;
            string? id = Str("requestId"), session = Str("session"), action = Str("action");
            string? motif = Str("motifId");
            int? size = Int("pieceCount"), index = Int("index");
            double? x = Number("x"), y = Number("y");
            bool? enabled = Bool("enabled"), confirm = Bool("confirmReplace");
            if (!Guid.TryParseExact(id, "D", out _) || !Guid.TryParseExact(session, "D", out _)) return null;

            // Reject malformed, unrecognized and incorrectly typed properties, including unwanted outcome fields.
            var known = new HashSet<string>(StringComparer.Ordinal)
            { "type", "version", "requestId", "session", "expectedRevision", "action",
              "motifId", "pieceCount", "index", "x", "y", "enabled", "confirmReplace" };
            if (root.EnumerateObject().Any(p => !known.Contains(p.Name)
                || p.Value.ValueKind == JsonValueKind.Null)) return null;

            bool valid = action switch
            {
                "jigsaw.motif" => motif is not null && JigsawCatalog.Motifs.Any(m => m.Id == motif)
                    && size is null && index is null && x is null && y is null && enabled is null && confirm is null,
                "jigsaw.size" => size is not null && JigsawCatalog.Sizes.Any(s => s.Count == size)
                    && motif is null && index is null && x is null && y is null && enabled is null && confirm is null,
                "jigsaw.timer" => enabled is not null && motif is null && size is null && index is null
                    && x is null && y is null && confirm is null,
                "jigsaw.start" => motif is null && size is null && index is null && x is null && y is null
                    && enabled is null && (confirm is null or true),
                "jigsaw.place" or "jigsaw.move" => index is >= 0 and < 1000 && x is >= -400 and <= 1360
                    && y is >= -400 and <= 1000 && motif is null && size is null && enabled is null && confirm is null,
                "jigsaw.tray" => index is >= 0 and < 1000 && x is null && y is null
                    && motif is null && size is null && enabled is null && confirm is null,
                "jigsaw.resume" or "jigsaw.back" or "jigsaw.exit" or "jigsaw.achievements" =>
                    motif is null && size is null && index is null && x is null && y is null && enabled is null && confirm is null,
                _ => false,
            };
            return valid ? new(id!, session!, revision, action!, motif, size, index, x, y, enabled, confirm) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException) { return null; }
    }
}

internal sealed class ModernJigsawSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernJigsawReply> receipts = new(StringComparer.Ordinal);

    internal ModernJigsawReply Execute(ModernJigsawCommand command,
        Func<ModernJigsawCommand, (bool Success, string Message)> execute)
    {
        if (command.Session != Id) return new(command.RequestId, false, "Jigsaw session expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernJigsawReply? previous)) return previous;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Puzzle changed. Review the latest board.", Revision);
        if (receipts.Count >= 16384)
            return new(command.RequestId, false, "Please reopen Jigsaw to continue.", Revision);
        (bool success, string message) = execute(command);
        if (success) Revision = checked(Revision + 1);
        var reply = new ModernJigsawReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
