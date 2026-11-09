using System.Text.Json;

namespace BlackjackCasino.Desktop;

internal sealed record ModernTowerDefenseCommand(string RequestId, string Session,
    long ExpectedRevision, string Action, int? Pad, string? Kind);
internal sealed record ModernTowerDefenseReply(string RequestId, bool Success,
    string Message, long Revision);

internal static class ModernTowerDefenseProtocol
{
    internal static ModernTowerDefenseCommand? Parse(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out var type) ||
                type.ValueKind != JsonValueKind.String ||
                type.GetString() != "lounge.tower-defense.command" ||
                !root.TryGetProperty("version", out var version) ||
                version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out int v) || v != 1 ||
                !root.TryGetProperty("expectedRevision", out var revision) ||
                revision.ValueKind != JsonValueKind.Number ||
                !revision.TryGetInt64(out long expected) || expected < 0)
                return null;
            string? Str(string key) => root.TryGetProperty(key, out var item) &&
                item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            int? pad = null;
            if (root.TryGetProperty("pad", out var field))
            {
                if (field.ValueKind != JsonValueKind.Number || !field.TryGetInt32(out int parsed))
                    return null;
                pad = parsed;
            }
            string? id = Str("requestId"), session = Str("session"),
                action = Str("action"), kind = Str("kind");
            if (!Guid.TryParseExact(id, "D", out _) ||
                !Guid.TryParseExact(session, "D", out _)) return null;
            bool valid = action switch
            {
                "tower.build" => pad is >= 0 and < 10 &&
                    kind is "archer" or "frost" or "cannon" or "ballista" or "flame",
                "tower.upgrade" or "tower.sell" => pad is >= 0 and < 10 && kind is null,
                "tower.play" => pad is null && kind is "forest" or "frost",
                "tower.skill.buy" => pad is null && kind is "capital" or "refund" or "fortify" or "engineer" or "scouting" or "focus" or "last-stand",
                "tower.start" or "tower.pause" or "tower.speed" or "tower.restart" or "tower.exit" or "tower.menu" or "tower.skills" or "tower.endless" or "tower.skill.reset" =>
                    pad is null && kind is null,
                _ => false,
            };
            return valid ? new(id!, session!, expected, action!, pad, kind) : null;
        }
        catch (JsonException) { return null; }
    }
}

internal sealed class ModernTowerDefenseSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernTowerDefenseReply> receipts = new(StringComparer.Ordinal);

    internal ModernTowerDefenseReply Execute(ModernTowerDefenseCommand command,
        Func<ModernTowerDefenseCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Defence session expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var receipt)) return receipt;
        // Exiting is a safe escape hatch even when a stale UI revision was rejected.
        if (command.Action != "tower.exit" && command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Defence changed. Refreshing.", Revision);
        if (receipts.Count >= 16_384)
            return new(command.RequestId, false, "Reopen Chill Lounge to continue.", Revision);
        var (success, message) = commit(command);
        if (success) Revision = checked(Revision + 1);
        var result = new ModernTowerDefenseReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, result);
        return result;
    }
}
