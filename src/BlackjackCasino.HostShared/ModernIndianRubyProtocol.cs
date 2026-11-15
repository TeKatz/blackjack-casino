using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.HostShared;

internal sealed record ModernIndianRubyCommand(string RequestId, string Session,
    long ExpectedRevision, string Action, string? Value);
internal sealed record ModernIndianRubyReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernIndianRubyProtocol
{
    internal const int Version = 1;
    internal static bool Trusted(string? source)
        => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == "https" && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.AbsolutePath == "/indian-ruby.html"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static ModernIndianRubyCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version)
                || !version.TryGetInt32(out int value) || value != Version
                || !root.TryGetProperty("expectedRevision", out var revision)
                || !revision.TryGetInt64(out long expected) || expected < 0) return null;
            string? Read(string key) => root.TryGetProperty(key, out var field)
                && field.ValueKind == JsonValueKind.String ? field.GetString() : null;
            string? requestId = Read("requestId"), session = Read("session");
            string? action = Read("action"), parameter = Read("value");
            if (!Guid.TryParseExact(requestId, "D", out _)
                || !Guid.TryParseExact(session, "D", out _)) return null;
            bool valid = action switch
            {
                "spin" => parameter is null,
                "bet.set" => decimal.TryParse(parameter, NumberStyles.None,
                    CultureInfo.InvariantCulture, out decimal bet) && IndianRubyRules.Bets.Contains(bet),
                "sound.set" or "animations.set" => parameter is "true" or "false",
                _ => false,
            };
            return valid ? new(requestId!, session!, expected, action!, parameter) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return null;
        }
    }
}

internal sealed class ModernIndianRubySession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernIndianRubyReply> receipts = new(StringComparer.Ordinal);
    internal ModernIndianRubyReply Execute(ModernIndianRubyCommand command,
        Func<ModernIndianRubyCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Expired Indian Ruby session.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var receipt)) return receipt;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Indian Ruby state changed. Refresh to continue.", Revision);
        if (receipts.Count >= 4096)
            return new(command.RequestId, false, "Please reopen Indian Ruby.", Revision);
        var (ok, message) = commit(command);
        if (ok) Revision++;
        var result = new ModernIndianRubyReply(command.RequestId, ok, message, Revision);
        receipts.Add(command.RequestId, result);
        return result;
    }
}
