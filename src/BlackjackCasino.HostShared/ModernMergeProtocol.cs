using System.Text.Json;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

/// <summary>Only intents enter C#. Client-provided item definitions, scores and rewards are never trusted.</summary>
internal sealed record ModernMergeCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, string? ProducerId, string? ItemId, int? Target, string? OrderId, string? Value);
internal sealed record ModernMergeReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernMergeProtocol
{
    internal const int Version = 1;

    internal static bool Trusted(string? source) =>
        Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == Uri.UriSchemeHttps
        && uri.Host == ManorPreviewProtocol.VirtualHost
        && uri.IsDefaultPort && uri.UserInfo.Length == 0
        && uri.AbsolutePath == "/midnight-merge.html"
        && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static ModernMergeCommand? Parse(string json)
    {
        if (json.Length > 3072) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("version", out JsonElement version)
                || !version.TryGetInt32(out int v) || v != Version
                || !root.TryGetProperty("expectedRevision", out JsonElement rev)
                || rev.ValueKind != JsonValueKind.Number
                || !rev.TryGetInt64(out long expected) || expected < 0) return null;

            string? Read(string key) => root.TryGetProperty(key, out JsonElement element)
                && element.ValueKind == JsonValueKind.String ? element.GetString() : null;
            string? session = Read("session"), requestId = Read("requestId");
            if (!Guid.TryParseExact(session, "D", out _) || !Guid.TryParseExact(requestId, "D", out _)) return null;

            string? action = Read("action");
            string? producer = Read("producerId"), item = Read("itemId"), order = Read("orderId"), value = Read("value");
            int? target = null;
            if (root.TryGetProperty("target", out JsonElement targetElement))
            {
                if (targetElement.ValueKind != JsonValueKind.Number || !targetElement.TryGetInt32(out int number)) return null;
                target = number;
            }
            bool valid = action switch
            {
                "spawn" => producer is not null && MergeCatalog.Default.Producers.ContainsKey(producer)
                    && item is null && target is null && order is null && value is null,
                "drop" => Guid.TryParseExact(item, "D", out _) && target is >= 0 and < MergeGameState.Capacity
                    && producer is null && order is null && value is null,
                "deliver" => Guid.TryParseExact(order, "D", out _) && producer is null
                    && item is null && target is null && value is null,
                "sound.set" or "animations.set" => value is "true" or "false"
                    && producer is null && item is null && target is null && order is null,
                _ => false,
            };
            return valid ? new(requestId!, session!, expected, action!, producer, item, target, order, value) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return null; }
    }
}

/// <summary>Receipt ledger survives browser failures; the persisted MergeBoard revision is authoritative.</summary>
internal sealed class ModernMergeSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    private readonly Dictionary<string, ModernMergeReply> receipts = new(StringComparer.Ordinal);

    internal ModernMergeReply Execute(ModernMergeCommand command, long currentRevision,
        Func<ModernMergeCommand, (bool Success, string Message, long Revision)> execute)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Expired Midnight Merge session.", currentRevision);
        if (receipts.TryGetValue(command.RequestId, out ModernMergeReply? saved)) return saved;
        if (command.ExpectedRevision != currentRevision)
            return new(command.RequestId, false, "Board changed. Reload before moving an item.", currentRevision);
        if (receipts.Count >= 8192)
            return new(command.RequestId, false, "Session request limit reached. Return to the lobby and reopen.", currentRevision);

        (bool success, string message, long revision) = execute(command);
        var result = new ModernMergeReply(command.RequestId, success, message, revision);
        receipts.Add(command.RequestId, result);
        return result;
    }
}
