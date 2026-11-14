using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

/// <summary>Validated browser intents only. Neither results nor wallet amounts originate in JS.</summary>
internal sealed record ModernNineLivesCommand(string RequestId, string Session,
    long ExpectedRevision, string Action, string? Value);
internal sealed record ModernNineLivesReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernNineLivesProtocol
{
    internal const int Version = 1;
    internal static bool Trusted(string? source)
        => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == "https" && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.AbsolutePath == "/nine-lives.html"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static ModernNineLivesCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out JsonElement version)
                || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out int parsedVersion) || parsedVersion != Version
                || !root.TryGetProperty("expectedRevision", out JsonElement revision)
                || revision.ValueKind != JsonValueKind.Number
                || !revision.TryGetInt64(out long expected) || expected < 0) return null;
            string? Read(string key) => root.TryGetProperty(key, out JsonElement value)
                && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            string? requestId = Read("requestId"), session = Read("session");
            string? action = Read("action"), value = Read("value");
            if (!Guid.TryParseExact(requestId, "D", out _) ||
                !Guid.TryParseExact(session, "D", out _)) return null;
            bool valid = action switch
            {
                "spin" => value is null,
                "bet.set" => decimal.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture,
                    out decimal bet) && NineLivesRules.Bets.Contains(bet),
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

/// <summary>Kept outside the WebView so a failed browser cannot replay a committed spin.</summary>
internal sealed class ModernNineLivesSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernNineLivesReply> receipts = new(StringComparer.Ordinal);

    internal ModernNineLivesReply Execute(ModernNineLivesCommand command,
        Func<ModernNineLivesCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Expired Nine Lives session.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernNineLivesReply? saved))
            return saved;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Nine Lives state changed. Refresh the result.", Revision);
        if (receipts.Count >= 4096)
            return new(command.RequestId, false, "Nine Lives request limit reached. Reopen the game.", Revision);
        (bool success, string message) = commit(command);
        if (success) Revision++;
        var reply = new ModernNineLivesReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
