using System.Text.Json;

namespace BlackjackCasino.HostShared;

/// <summary>
/// The browser can request only Sudoku intentions. It never submits boards, solutions,
/// rewards, timestamps or profile changes; validation takes place before game dispatch.
/// </summary>
internal sealed record ModernSudokuCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, int? Index, int? Digit, string? Value);
internal sealed record ModernSudokuReply(string RequestId, bool Success, string Message, long Revision);

internal static class ModernSudokuProtocol
{
    internal const int Version = 1;

    internal static ModernSudokuCommand? Parse(string json)
    {
        if (json.Length > 2048) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out JsonElement type) || type.GetString() != "lounge.sudoku.command" ||
                !root.TryGetProperty("version", out JsonElement version) ||
                !version.TryGetInt32(out int v) || v != Version ||
                !root.TryGetProperty("expectedRevision", out JsonElement revision) ||
                !revision.TryGetInt64(out long expected) || expected < 0) return null;
            string? Read(string key) => root.TryGetProperty(key, out JsonElement entry) &&
                entry.ValueKind == JsonValueKind.String ? entry.GetString() : null;
            string? requestId = Read("requestId"), session = Read("session");
            string? action = Read("action"), value = Read("value");
            if (!Guid.TryParseExact(requestId, "D", out _) ||
                !Guid.TryParseExact(session, "D", out _)) return null;
            int? index = ReadIndex("index"), digit = ReadIndex("digit");
            if (index == int.MinValue || digit == int.MinValue) return null;
            bool blank = index is null && digit is null && value is null;
            bool valid = action switch
            {
                "sudoku.daily" or "sudoku.back" or "sudoku.exit"
                    or "sudoku.undo" or "sudoku.redo"
                    or "sudoku.variant.back" or "sudoku.variant.undo" or "sudoku.variant.redo" => blank,
                "sudoku.variant.new" or "sudoku.variant.resume" => index is null && digit is null &&
                    value is not null && BlackjackCasino.Core.Sudoku.SudokuVariantLibrary.TryParseKey(value,
                        out _, out _, out _),
                "sudoku.variant.value" or "sudoku.variant.note" => index is >= 0 and < 441 &&
                    digit is >= 1 and <= 9 && value is null,
                "sudoku.variant.erase" => index is >= 0 and < 441 && digit is null && value is null,
                "sudoku.variant.hint" => index is >= -1 and < 441 && digit is null && value is null,
                "sudoku.new" or "sudoku.resume" => index is null && digit is null &&
                    value is "Relaxed" or "Easy" or "Medium" or "Hard" or "Expert" or "Master",
                "sudoku.value" or "sudoku.note" => index is >= 0 and < 81 &&
                    digit is >= 1 and <= 9 && value is null,
                "sudoku.erase" => index is >= 0 and < 81 && digit is null && value is null,
                "sudoku.hint" => index is >= -1 and < 81 && digit is null && value is null,
                "sudoku.guides" or "sudoku.timer" => index is null && digit is null &&
                    value is "true" or "false",
                _ => false,
            };
            return valid ? new(requestId!, session!, expected, action!, index, digit, value) : null;

            int? ReadIndex(string key)
            {
                if (!root.TryGetProperty(key, out JsonElement entry)) return null;
                return entry.ValueKind == JsonValueKind.Number && entry.TryGetInt32(out int n)
                    ? n : int.MinValue;
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException) { return null; }
    }
}

internal sealed class ModernSudokuSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, ModernSudokuReply> receipts = new(StringComparer.Ordinal);

    internal ModernSudokuReply Execute(ModernSudokuCommand command,
        Func<ModernSudokuCommand, (bool Success, string Message)> commit)
    {
        if (command.Session != Id)
            return new(command.RequestId, false, "Sudoku session expired.", Revision);
        if (receipts.TryGetValue(command.RequestId, out ModernSudokuReply? saved)) return saved;
        if (command.ExpectedRevision != Revision)
            return new(command.RequestId, false, "The puzzle changed. Inspect the current board.", Revision);
        if (receipts.Count >= 8192)
            return new(command.RequestId, false, "Please reopen the Lounge to continue.", Revision);

        (bool success, string message) = commit(command);
        if (success) Revision = checked(Revision + 1);
        var receipt = new ModernSudokuReply(command.RequestId, success, message, Revision);
        receipts.Add(command.RequestId, receipt);
        return receipt;
    }
}
