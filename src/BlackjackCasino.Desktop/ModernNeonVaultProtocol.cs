using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

/// <summary>Strict local intent boundary; results, awards and RNG are never accepted from JS.</summary>
internal sealed record ModernNeonVaultCommand(string RequestId, string Session,
    long ExpectedRevision, string Action, string? Value);
internal sealed record ModernNeonVaultReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernNeonVaultProtocol
{
    internal const int Version = 1;
    internal static bool Trusted(string? source)
        => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == "https" && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.AbsolutePath == "/neon-vault.html"
        && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static ModernNeonVaultCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
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
            if (!Guid.TryParseExact(requestId, "D", out _)
                || !Guid.TryParseExact(session, "D", out _)) return null;
            bool valid = action switch
            {
                "spin" => value is null,
                "bet.set" => decimal.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture,
                    out decimal wager) && NeonVaultRules.Bets.Contains(wager),
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

/// <summary>Lives outside WebView2. Receipts deduplicate spins even if the browser dies.</summary>
internal sealed class ModernNeonVaultSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernNeonVaultReply> receipts = new(StringComparer.Ordinal);

    internal ModernNeonVaultReply Execute(ModernNeonVaultCommand command,
        Func<ModernNeonVaultCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Expired Neon Vault session.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernNeonVaultReply? saved)) return saved;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "Neon Vault state changed. Refresh before another spin.", Revision);
        if (receipts.Count >= 4096)
            return new(command.RequestId, false, "Neon Vault request limit reached. Reopen the game.", Revision);
        (bool success, string message) = commit(command);
        if (success) Revision++;
        var reply = new ModernNeonVaultReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
