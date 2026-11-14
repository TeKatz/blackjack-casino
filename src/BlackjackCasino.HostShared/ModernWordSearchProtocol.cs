using System.Text.Json;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.HostShared;

/// <summary>Strict intent-only boundary: the React view never submits letters, solutions, saves or rewards.</summary>
internal sealed record ModernWordSearchCommand(
    string RequestId, string Session, long ExpectedRevision, string Action,
    WordSearchDifficulty? Difficulty, int? StartRow, int? StartColumn, int? EndRow, int? EndColumn);

internal sealed record ModernWordSearchReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernWordSearchProtocol
{
    internal static ModernWordSearchCommand? Parse(string payload)
    {
        if (payload.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out JsonElement type) ||
                type.ValueKind != JsonValueKind.String || type.GetString() != "lounge.word-search.command" ||
                !root.TryGetProperty("version", out JsonElement version) ||
                version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int v) || v != 1 ||
                !root.TryGetProperty("expectedRevision", out JsonElement revision) ||
                revision.ValueKind != JsonValueKind.Number || !revision.TryGetInt64(out long expected) ||
                expected < 0) return null;

            string? Str(string field)
            {
                if (!root.TryGetProperty(field, out JsonElement item)) return null;
                return item.ValueKind == JsonValueKind.String ? item.GetString() : null;
            }
            int? Int(string field)
            {
                if (!root.TryGetProperty(field, out JsonElement item)) return null;
                return item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out int value) ? value : int.MinValue;
            }

            string? requestId = Str("requestId"), session = Str("session"), action = Str("action");
            string? difficultyName = Str("difficulty");
            int? sr = Int("startRow"), sc = Int("startColumn"), er = Int("endRow"), ec = Int("endColumn");
            if (!Guid.TryParseExact(requestId, "D", out _) || !Guid.TryParseExact(session, "D", out _) ||
                sr == int.MinValue || sc == int.MinValue || er == int.MinValue || ec == int.MinValue) return null;
            WordSearchDifficulty? difficulty = Enum.TryParse(difficultyName, out WordSearchDifficulty parsed) &&
                Enum.IsDefined(parsed) && parsed.ToString() == difficultyName ? parsed : null;
            bool noCoordinates = sr is null && sc is null && er is null && ec is null;
            bool valid = action switch
            {
                "word-search.start" => difficulty is not null && noCoordinates,
                "word-search.resume" or "word-search.hint" or "word-search.back" or "word-search.exit" =>
                    difficultyName is null && noCoordinates,
                "word-search.select" => difficultyName is null &&
                    sr is >= 0 and < 13 && sc is >= 0 and < 13 &&
                    er is >= 0 and < 13 && ec is >= 0 and < 13 &&
                    (sr != er || sc != ec) &&
                    (sr == er || sc == ec || Math.Abs(sr.Value - er.Value) == Math.Abs(sc.Value - ec.Value)),
                _ => false,
            };
            return valid ? new ModernWordSearchCommand(requestId!, session!, expected, action!,
                difficulty, sr, sc, er, ec) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return null; }
    }
}

internal sealed class ModernWordSearchSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernWordSearchReply> receipts = new(StringComparer.Ordinal);

    internal ModernWordSearchReply Execute(ModernWordSearchCommand request,
        Func<ModernWordSearchCommand, (bool Success, string Message)> commit)
    {
        if (request.Session != Id)
            return new(request.RequestId, false, "Word Search session expired.", Revision);
        if (receipts.TryGetValue(request.RequestId, out ModernWordSearchReply? previous)) return previous;
        if (request.ExpectedRevision != Revision)
            return new(request.RequestId, false, "The puzzle has changed. Review the latest letters.", Revision);
        if (receipts.Count >= 16_384)
            return new(request.RequestId, false, "Please reopen the Lounge to continue.", Revision);
        (bool success, string message) = commit(request);
        if (success) Revision = checked(Revision + 1);
        var reply = new ModernWordSearchReply(request.RequestId, success, message, Revision);
        receipts.Add(request.RequestId, reply);
        return reply;
    }
}
