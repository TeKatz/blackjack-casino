using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Version-one, strictly read-only rendering contract. It does not accept bets, results,
/// or wallet changes from JavaScript; all money remains in the existing Manor journal.
/// </summary>
internal static class ManorPreviewProtocol
{
    internal const int Version = 1;
    internal const string VirtualHost = "midnight-manor.local";
    internal const string EntryPoint = "https://midnight-manor.local/index.html";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal sealed record Command(string Type, string RequestId);
    internal sealed record Snapshot(
        string Bankroll,
        string Bet,
        string CycleReturned,
        string Payout,
        string[] Cells,
        int[] WinningCells,
        bool CycleActive,
        int FreeSpinsRemaining,
        string Feature,
        bool AnimationsEnabled,
        bool HasCommittedSpin);

    internal static bool IsTrustedSource(string? source)
    {
        return Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Equals(VirtualHost, StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort
            && uri.AbsolutePath.Equals("/index.html", StringComparison.Ordinal)
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }

    internal static bool TryParseCommand(string? source, string? body, out Command? command)
    {
        command = null;
        if (!IsTrustedSource(source) || string.IsNullOrWhiteSpace(body) || body.Length > 2048)
            return false;
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("protocolVersion", out JsonElement version)
                || version.ValueKind != JsonValueKind.Number
                || !version.TryGetInt32(out int number)
                || number != Version
                || !root.TryGetProperty("type", out JsonElement type)
                || type.ValueKind != JsonValueKind.String
                || !root.TryGetProperty("requestId", out JsonElement request)
                || request.ValueKind != JsonValueKind.String)
                return false;

            string? name = type.GetString();
            string? id = request.GetString();
            if (id is null || id.Length is < 1 or > 64
                || id.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
                return false;
            if (name is not ("client.ready" or "snapshot.request" or "ui.close"))
                return false;
            command = new Command(name, id);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static Snapshot CreateSnapshot(ManorSessionState state, bool animationsEnabled)
    {
        ArgumentNullException.ThrowIfNull(state);
        ManorSpin? spin = state.LastSpin;
        string[] cells = spin?.Cells is { Length: 15 } real
            ? real.Select(symbol => symbol.ToString()).ToArray()
            : Enumerable.Range(0, 5)
                .SelectMany(reel => Enumerable.Range(0, 3).Select(row => ManorRules.SymbolAt(reel, reel * 5, row).ToString()))
                .ToArray();

        var winningCells = new HashSet<int>();
        if (spin is not null)
        {
            foreach (ManorLineWin win in spin.Wins)
            {
                if (win.Line is < 0 or >= 20 || win.Count is < 0 or > 5)
                    continue;
                for (int reel = 0; reel < win.Count; reel++)
                    winningCells.Add(reel * 3 + ManorRules.Paylines[win.Line][reel]);
            }
        }
        string feature = state.Run is { } run
            ? "Escape the Manor · " + run.Status
            : state.FreeSpinsRemaining > 0
                ? "Haunted Night · " + state.FreeSpinsRemaining + " free spins saved"
                : state.CycleActive
                    ? "Pending saved cycle"
                    : "The house awaits";

        return new Snapshot(
            Money(state.Bankroll),
            Money(state.Bet),
            Money(state.CycleReturned),
            Money(spin?.Payout ?? 0m),
            cells,
            winningCells.Order().ToArray(),
            state.CycleActive,
            state.FreeSpinsRemaining,
            feature,
            animationsEnabled,
            spin is not null);
    }

    internal static string SerializeSnapshot(Snapshot snapshot, string requestId) =>
        JsonSerializer.Serialize(new
        {
            protocolVersion = Version,
            type = "snapshot",
            requestId,
            payload = snapshot
        }, Json);

    private static string Money(decimal amount) => "$" + amount.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
}
