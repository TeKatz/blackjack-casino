using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

/// <summary>A narrow, versioned intent boundary. Dice, previews, fees, XP and payouts never enter from the browser.</summary>
internal sealed record ModernDiceCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, int? Index, string? Category, string? Value);
internal sealed record ModernDiceReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernDiceProtocol
{
    internal const int Version = 1;
    internal static bool Trusted(string? source) =>
        Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == Uri.UriSchemeHttps && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.UserInfo.Length == 0 && uri.AbsolutePath == "/midnight-dice.html"
        && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static ModernDiceCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out JsonElement v) || !v.TryGetInt32(out int version) || version != Version
                || !root.TryGetProperty("expectedRevision", out JsonElement r) || !r.TryGetInt64(out long revision) || revision < 0)
                return null;
            string? Read(string key) => root.TryGetProperty(key, out JsonElement element)
                && element.ValueKind == JsonValueKind.String ? element.GetString() : null;
            string? requestId = Read("requestId"), session = Read("session"), action = Read("action");
            string? category = Read("category"), value = Read("value");
            int? index = null;
            if (root.TryGetProperty("index", out JsonElement entry))
            {
                if (entry.ValueKind != JsonValueKind.Number || !entry.TryGetInt32(out int i)) return null;
                index = i;
            }
            if (!Guid.TryParseExact(requestId, "D", out _) || !Guid.TryParseExact(session, "D", out _)) return null;
            bool noArgs = index is null && category is null && value is null;
            bool valid = action switch
            {
                "roll" or "casual.resume" or "casual.start" or "casino.resume" => noArgs,
                "hold" => index is >= 0 and < 5 && category is null && value is null,
                "score" or "scratch" => index is null && value is null && Enum.TryParse(category, out KniffelCategory parsed)
                    && Enum.IsDefined(parsed) && parsed.ToString() == category,
                "casino.start" => index is null && category is null
                    && decimal.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out decimal stake)
                    && KniffelTable.Stakes.Contains(stake),
                "casino.forfeit" => noArgs,
                "sound.set" or "animations.set" or "music.set" => index is null && category is null && value is "true" or "false",
                _ => false,
            };
            return valid ? new ModernDiceCommand(requestId!, session!, revision, action!, index, category, value) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return null; }
    }
}

/// <summary>Lives in the native host, not WebView2. Receipts survive a browser restart.</summary>
internal sealed class ModernDiceSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernDiceReply> receipts = new(StringComparer.Ordinal);

    internal ModernDiceReply Execute(ModernDiceCommand command,
        Func<ModernDiceCommand, (bool Success, string Message)> execute)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Expired Midnight Dice session.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernDiceReply? saved)) return saved;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "The dice table changed. Refresh your sheet.", Revision);
        if (receipts.Count >= 8192)
            return new(command.RequestId, false, "Session request limit reached. Reopen Midnight Dice.", Revision);
        (bool success, string message) = execute(command);
        if (success) Bump();
        var receipt = new ModernDiceReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, receipt);
        return receipt;
    }
    internal void Bump() => Revision = checked(Revision + 1);
}
