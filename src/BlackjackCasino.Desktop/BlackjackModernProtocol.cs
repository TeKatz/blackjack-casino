using System.Globalization;
using System.Text.Json;

namespace BlackjackCasino.Desktop;

// User intents only. Card order, payouts, eligibility and bankroll remain C#-owned.
internal sealed record BlackjackModernCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, string? Value);
internal sealed record BlackjackModernReply(string RequestId, bool Success, string Message, long Revision);

internal static class BlackjackModernProtocol
{
    internal const int Version = 1;

    internal static bool Trusted(string? source) => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == "https" && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.AbsolutePath == "/blackjack.html"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static BlackjackModernCommand? Parse(string json)
    {
        if (json.Length > 4096) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out var version)
                || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out int v) || v != Version) return null;

            string? Read(string key) => root.TryGetProperty(key, out var p) &&
                p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            string? requestId = Read("requestId"), session = Read("session");
            string? action = Read("action"), value = Read("value");
            if (!Guid.TryParseExact(requestId, "D", out _) || !Guid.TryParseExact(session, "D", out _)
                || !root.TryGetProperty("expectedRevision", out var rev)
                || rev.ValueKind != JsonValueKind.Number || !rev.TryGetInt64(out long expected)
                || expected < 0) return null;

            bool valid = action switch
            {
                "bet.set" => value is not null &&
                    decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                        out decimal amount) && amount >= 0m && amount <= 5000m,
                "mode.set" => value is "solo" or "full",
                "seats.set" => value is "1" or "2" or "3" or "4" or "5",
                "preset.set" => value is "Casual" or "LasVegasStrip" or "DowntownVegas"
                    or "AtlanticCity" or "European" or "SingleDeck" or "Custom",
                "theme.set" => value is "ClassicGreen" or "MidnightBlue" or "Burgundy",
                "sound.set" or "animations.set" => value is "true" or "false",
                "bet.clear" or "deal" or "hit" or "stand" or "double" or "split"
                    or "surrender" or "insurance.take" or "insurance.decline" => value is null,
                _ => false,
            };
            return valid ? new(requestId!, session!, expected, action!, value) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            return null;
        }
    }
}

// Every acknowledged action is unique within one mounted WebView epoch.
internal sealed class BlackjackModernSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, BlackjackModernReply> receipts = new(StringComparer.Ordinal);

    internal BlackjackModernReply Execute(BlackjackModernCommand command,
        Func<BlackjackModernCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id) return new(command.RequestId, false, "Expired Blackjack session.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var previous)) return previous;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Table state changed. Review the latest cards.", Revision);
        if (receipts.Count >= 4096)
            return new(command.RequestId, false, "Reopen the Blackjack table to continue.", Revision);

        (bool success, string message) = commit(command);
        if (success) Revision++;
        var reply = new BlackjackModernReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
