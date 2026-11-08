using System.Text.Json;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Narrow native-authoritative contract: no browser-supplied dice, deck, check result, reward,
/// inventory state, discovered case-board clue, or encounter outcome can be accepted.
/// </summary>
internal sealed record ModernAdventureCommand(
    string RequestId, string Session, long ExpectedRevision, string Action,
    string? AdventureId, string? ClassId, string? ChoiceId, string? NodeId,
    bool? ConfirmReplace, int? Index, double? X, double? Y,
    double? Zoom, double? PanX, double? PanY, bool? Enabled, double? Volume);

internal sealed record ModernAdventureReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernAdventureProtocol
{
    internal static ModernAdventureCommand? Parse(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out JsonElement type) ||
                type.ValueKind != JsonValueKind.String || type.GetString() != "lounge.adventure.command" ||
                !root.TryGetProperty("version", out JsonElement version) ||
                !version.TryGetInt32(out int number) || number != 1 ||
                !root.TryGetProperty("expectedRevision", out JsonElement revision) ||
                !revision.TryGetInt64(out long expected) || expected < 0) return null;

            static bool Known(string key) => key is "type" or "version" or "requestId" or "session"
                or "expectedRevision" or "action" or "adventureId" or "classId" or "choiceId" or "nodeId"
                or "confirmReplace" or "index" or "x" or "y" or "zoom" or "panX" or "panY" or "enabled" or "volume";
            if (root.EnumerateObject().Any(p => !Known(p.Name) ||
                p.Value.ValueKind == JsonValueKind.Null)) return null;

            string? Str(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement value)) return null;
                return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            }
            int? Int(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement value)) return null;
                return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int parsed)
                    ? parsed : null;
            }
            double? Number(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement value)) return null;
                return value.ValueKind == JsonValueKind.Number &&
                    value.TryGetDouble(out double parsed) && double.IsFinite(parsed) ? parsed : null;
            }
            bool? Bool(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement value)) return null;
                return value.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? value.GetBoolean() : null;
            }

            string? id = Str("requestId"), session = Str("session"), action = Str("action");
            string? tale = Str("adventureId"), classId = Str("classId"),
                choice = Str("choiceId"), node = Str("nodeId");
            bool? confirm = Bool("confirmReplace"), enabled = Bool("enabled");
            int? index = Int("index");
            double? x = Number("x"), y = Number("y"), zoom = Number("zoom"),
                panX = Number("panX"), panY = Number("panY"), volume = Number("volume");
            if (!Guid.TryParseExact(id, "D", out _) || !Guid.TryParseExact(session, "D", out _) ||
                action is null) return null;
            bool Empty(string field) => !root.TryGetProperty(field, out _);
            bool Only(params string[] fields)
            {
                HashSet<string> wanted = fields.ToHashSet(StringComparer.Ordinal);
                return new[] { "adventureId", "classId", "choiceId", "nodeId",
                    "confirmReplace", "index", "x", "y", "zoom", "panX", "panY", "enabled", "volume" }
                    .All(key => wanted.Contains(key) ? !Empty(key) : Empty(key));
            }
            bool Tale() => tale is { Length: > 0 and <= 100 } &&
                AdventureCatalog.TryGet(tale, out AdventureDefinition? definition) && definition is not null;
            bool Choice() => choice is { Length: > 0 and <= 128 } &&
                choice.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
            bool Node() => node is { Length: > 0 and <= 128 } &&
                node.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');

            bool valid = action switch
            {
                "adventure.select" or "adventure.resume" => Only("adventureId") && Tale(),
                "adventure.music" => Only("enabled", "volume") && enabled is not null && volume is >= 0 and <= .32,
                "adventure.start" => Tale() && classId is { Length: > 0 and <= 100 } &&
                    (Only("adventureId", "classId") ||
                    Only("adventureId", "classId", "confirmReplace") && confirm == true),
                "adventure.restart" => Tale() &&
                    (Only("adventureId") ||
                    Only("adventureId", "confirmReplace") && confirm == true),
                "adventure.choice" => Only("choiceId") && Choice(),
                "adventure.board.move" => Only("nodeId", "x", "y") && Node() &&
                    x is >= 0 and <= 5000 && y is >= 0 and <= 5000,
                "adventure.board.view" => Only("zoom", "panX", "panY") &&
                    zoom is >= .2 and <= 4 && panX is >= -5000 and <= 5000 &&
                    panY is >= -5000 and <= 5000,
                "adventure.encounter.hold" => Only("index") && index is >= 0 and < 5,
                "adventure.roll" or "adventure.board.open" or "adventure.board.close"
                    or "adventure.encounter.hit" or "adventure.encounter.stand"
                    or "adventure.encounter.roll" or "adventure.encounter.bank"
                    or "adventure.encounter.finish" or "adventure.back" or "adventure.exit" => Only(),
                _ => false,
            };
            return valid
                ? new(id!, session!, expected, action, tale, classId, choice, node,
                    confirm, index, x, y, zoom, panX, panY, enabled, volume)
                : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }
}

internal sealed class ModernAdventureSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernAdventureReply> receipts = new(StringComparer.Ordinal);

    internal ModernAdventureReply Execute(ModernAdventureCommand command,
        Func<ModernAdventureCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Adventure session expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernAdventureReply? receipt))
            return receipt;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Story changed. Review the current scene.", Revision);
        if (receipts.Count >= 16384)
            return new(command.RequestId, false, "Please reopen the Lounge to continue.", Revision);
        (bool success, string message) = commit(command);
        if (success) Revision = checked(Revision + 1);
        var result = new ModernAdventureReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, result);
        return result;
    }
}
