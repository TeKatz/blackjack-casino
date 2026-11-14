using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

// Browser messages are intents only; no outcome, balance or XP is accepted from JS.
internal sealed record ModernReelsCommand(string RequestId, string Session,
    long ExpectedRevision, string Action, string? Value);
internal sealed record ModernReelsReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernReelsProtocol
{
    internal const int Version = 1;
    internal static bool Trusted(string? source)
        => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == "https" && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.AbsolutePath == "/reels.html"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static ModernReelsCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version)
                || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out int protocol) || protocol != Version
                || !root.TryGetProperty("expectedRevision", out var revision)
                || revision.ValueKind != JsonValueKind.Number
                || !revision.TryGetInt64(out long expected) || expected < 0) return null;
            string? Read(string key) => root.TryGetProperty(key, out var e)
                && e.ValueKind == JsonValueKind.String ? e.GetString() : null;
            string? requestId = Read("requestId"), session = Read("session");
            string? action = Read("action"), value = Read("value");
            if (!Guid.TryParseExact(requestId, "D", out _) ||
                !Guid.TryParseExact(session, "D", out _)) return null;
            bool valid = action switch
            {
                "spin" => value is null,
                "bet.set" => value is "1" or "5" or "25" or "100" or "500" or "1000",
                "sound.set" or "animations.set" => value is "true" or "false",
                _ => false,
            };
            return valid ? new(requestId!, session!, expected, action!, value) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}

internal sealed class ModernReelsSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernReelsReply> receipts = new(StringComparer.Ordinal);

    internal ModernReelsReply Execute(ModernReelsCommand command,
        Func<ModernReelsCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Expired reels session.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var receipt)) return receipt;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Reels state changed. Review the latest result.", Revision);
        if (receipts.Count >= 4096)
            return new(command.RequestId, false, "Reopen Midnight Reels to continue.", Revision);
        (bool success, string message) = commit(command);
        if (success) Revision++;
        var reply = new ModernReelsReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
