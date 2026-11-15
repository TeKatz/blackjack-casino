using System.Text.Json;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.HostShared;

/// <summary>Version 2 exclusively supports active clicks, finite construction and prestige.</summary>
internal sealed record EmpireModernCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, string? Id, int Count);
internal sealed record EmpireModernReply(string RequestId, bool Success, string Message, long Revision);

internal static class EmpireModernProtocol
{
    internal const int Version = 2;
    internal static bool Trusted(string? source) => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == Uri.UriSchemeHttps && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.AbsolutePath == "/empire.html" && uri.UserInfo.Length == 0
        && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static EmpireModernCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version)
                || !version.TryGetInt32(out int v) || v != Version) return null;
            string? Read(string key) => root.TryGetProperty(key, out var item)
                && item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            string? request = Read("requestId"), session = Read("session"),
                action = Read("action"), id = Read("id");
            if (!Guid.TryParseExact(request, "D", out _)
                || !Guid.TryParseExact(session, "D", out _)
                || !root.TryGetProperty("expectedRevision", out var revision)
                || !revision.TryGetInt64(out long rev) || rev < 0) return null;
            if (action is not ("tap" or "building.upgrade" or "prestige")) return null;
            int count = 1;
            if (root.TryGetProperty("count", out var rawCount)
                && (!rawCount.TryGetInt32(out count) || count is < 1 or > 20)) return null;
            if (action == "tap" && id is not null) return null;
            if (action == "building.upgrade" && EmpireCatalog.Find(id) is null) return null;
            if (action == "prestige" && id is not null) return null;
            if (action != "tap" && count != 1) return null;
            return new(request!, session!, rev, action!, id, count);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException
            or FormatException or OverflowException) { return null; }
    }

    internal static EmpireActionResult Apply(EmpireService service, EmpireModernCommand command,
        DateTimeOffset now) => command.Action switch
    {
        "tap" => service.Tap(command.Count, now),
        "building.upgrade" => service.Upgrade(command.Id, now),
        "prestige" => service.ResetForPrestige(now),
        _ => new(false, "Unsupported Empire action."),
    };
}

/// <summary>
/// Revision fencing makes replay rejection safe even after old receipts are evicted.
/// Persistence is completed before a successful revision is announced.
/// </summary>
internal sealed class EmpireModernSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, EmpireModernReply> receipts = new(StringComparer.Ordinal);
    private readonly Queue<string> order = new();
    internal EmpireModernReply Execute(EmpireModernCommand command,
        Func<EmpireModernCommand, EmpireActionResult> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "This Empire view has expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var previous)) return previous;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Empire changed; refresh the view.", Revision);
        EmpireActionResult result = commit(command);
        if (result.Success) Revision++;
        var reply = new EmpireModernReply(command.RequestId, result.Success, result.Message, Revision);
        receipts[command.RequestId] = reply;
        order.Enqueue(command.RequestId);
        if (order.Count > 256) receipts.Remove(order.Dequeue());
        return reply;
    }
}
