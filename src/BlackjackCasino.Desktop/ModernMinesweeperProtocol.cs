using System.Text.Json;

namespace BlackjackCasino.Desktop;

internal sealed record ModernMinesweeperCommand(string RequestId, string Session,
    long ExpectedRevision, string Action, int? Index, string? Difficulty);
internal sealed record ModernMinesweeperReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernMinesweeperProtocol
{
    internal static ModernMinesweeperCommand? Parse(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out var type) ||
                type.GetString() != "lounge.minesweeper.command" ||
                !root.TryGetProperty("version", out var version) ||
                !version.TryGetInt32(out int v) || v != 1 ||
                !root.TryGetProperty("expectedRevision", out var revision) ||
                !revision.TryGetInt64(out long expected) || expected < 0) return null;
            string? Str(string name) => root.TryGetProperty(name, out var item) &&
                item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            int? index = null;
            if (root.TryGetProperty("index", out var field))
            {
                if (field.ValueKind != JsonValueKind.Number || !field.TryGetInt32(out int i) ||
                    i < 0 || i >= 256) return null;
                index = i;
            }
            string? id = Str("requestId"), session = Str("session"),
                action = Str("action"), difficulty = Str("difficulty");
            if (!Guid.TryParseExact(id, "D", out _) || !Guid.TryParseExact(session, "D", out _)) return null;
            bool valid = action switch
            {
                "mine.new" => index is null && difficulty is "easy" or "standard" or "expert",
                "mine.reveal" or "mine.flag" or "mine.scan" or "mine.chord" =>
                    index.HasValue && difficulty is null,
                "mine.menu" or "mine.exit" or "mine.resume" => index is null && difficulty is null,
                _ => false,
            };
            return valid ? new(id!, session!, expected, action!, index, difficulty) : null;
        }
        catch (JsonException) { return null; }
        catch (InvalidOperationException) { return null; }
    }
}

internal sealed class ModernMinesweeperSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernMinesweeperReply> receipts = new(StringComparer.Ordinal);
    internal ModernMinesweeperReply Execute(ModernMinesweeperCommand command,
        Func<ModernMinesweeperCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Sweep session expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var previous)) return previous;
        if (command.Action != "mine.exit" && command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Field changed. Refreshing.", Revision);
        if (receipts.Count >= 16_384)
            return new(command.RequestId, false, "Reopen Chill Lounge to continue.", Revision);
        var (success, message) = commit(command);
        if (success) Revision++;
        var reply = new ModernMinesweeperReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
