using System.Globalization;
using System.Text.Json;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

// Presentation intents only. No client money, rates, outcomes or timestamps are accepted.
internal sealed record EmpireModernCommand(string RequestId, string Session, long ExpectedRevision,
    string Action, string? Id, string? RoomId, int Levels, bool Preview, EmpireAutomationState? Automation);
internal sealed record EmpireModernReply(string RequestId, bool Success, string Message, long Revision, object? Preview = null);

internal static class EmpireModernProtocol
{
    internal const int Version = 1;
    internal static bool Trusted(string? source) => Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
        && uri.Scheme == "https" && uri.Host == ManorPreviewProtocol.VirtualHost && uri.IsDefaultPort
        && uri.AbsolutePath == "/empire.html" && uri.UserInfo.Length == 0 && uri.Query.Length == 0 && uri.Fragment.Length == 0;

    internal static EmpireModernCommand? Parse(string json)
    {
        if (json.Length > 8192) return null;
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("version", out var version)
                || !version.TryGetInt32(out int v) || v != Version) return null;
            string? Read(string key) => root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
            string? request = Read("requestId"), session = Read("session"), action = Read("action"), id = Read("id"), room = Read("roomId");
            if (!Guid.TryParseExact(request, "D", out _) || !Guid.TryParseExact(session, "D", out _)
                || !root.TryGetProperty("expectedRevision", out var revision) || !revision.TryGetInt64(out long rev) || rev < 0) return null;
            if (action is not ("upgrade" or "research" or "project" or "property.buy" or "property.select"
                or "manager" or "automation" or "event" or "contract.accept" or "contract.claim" or "prestige" or "dividend")) return null;
            if (id is not null && (id.Length > 80 || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-'))) return null;
            if (room is not null && EmpireCatalog.Find(room) is null) return null;
            int levels = 1;
            if (root.TryGetProperty("levels", out var rawLevels) && (!rawLevels.TryGetInt32(out levels) || levels is < 1 or > 100)) return null;
            bool preview = root.TryGetProperty("preview", out var p) && p.ValueKind == JsonValueKind.True;
            if (root.TryGetProperty("preview", out p) && p.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
            if (preview && action is "dividend" or "prestige" or "property.select") return null;
            bool validId = action switch
            {
                "upgrade" => EmpireCatalog.Find(id ?? "") is not null,
                "research" => EmpireResearchCatalog.Find(id ?? "") is not null,
                "project" => EmpireProjectCatalog.Find(id ?? "") is not null,
                "property.buy" or "property.select" => EmpirePropertyCatalog.Find(id ?? "") is not null,
                "manager" => EmpireManagerCatalog.Find(id ?? "") is not null,
                "event" => EmpireEventCatalog.Find(id ?? "") is not null,
                "contract.accept" => EmpireContractCatalog.Find(id ?? "") is not null,
                _ => id is null,
            };
            if (!validId) return null;
            EmpireAutomationState? automation = null;
            if (action == "automation")
            {
                if (!root.TryGetProperty("automation", out var config) || config.ValueKind != JsonValueKind.Object) return null;
                if (!config.TryGetProperty("reserve", out var reserve) || reserve.ValueKind != JsonValueKind.String
                    || !decimal.TryParse(reserve.GetString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal cash)
                    || cash < 0 || cash > EmpireRules.MaximumCredits) return null;
                if (!config.TryGetProperty("spendPercent", out var percent) || percent.ValueKind != JsonValueKind.String
                    || !decimal.TryParse(percent.GetString(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal spend)
                    || spend is < .01m or > .50m) return null;
                if (!config.TryGetProperty("targetLevel", out var target) || !target.TryGetInt32(out int level) || level is < 1 or > 200) return null;
                if (!config.TryGetProperty("enabled", out var enabled) || enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                    || !config.TryGetProperty("cheapestMilestone", out var cheapest) || cheapest.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return null;
                if (!config.TryGetProperty("priorities", out var priorities) || priorities.ValueKind != JsonValueKind.Array || priorities.GetArrayLength() > 15) return null;
                var ids = new List<string>();
                foreach (var item in priorities.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String || EmpireCatalog.Find(item.GetString() ?? "") is null) return null;
                    ids.Add(item.GetString()!);
                }
                automation = new() { Enabled = enabled.GetBoolean(), Reserve = cash, SpendPercent = spend,
                    TargetLevel = level, CheapestMilestone = cheapest.GetBoolean(), Priorities = ids.ToArray() };
            }
            return new(request!, session!, rev, action, id, room, levels, preview, automation);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException or OverflowException) { return null; }
    }

    internal static EmpireActionResult Apply(EmpireService service, EmpireModernCommand command, DateTimeOffset at) => command.Action switch
    {
        "upgrade" => service.UpgradeMany(command.Id!, command.Levels, at) ? new(true, "Department upgrade saved.") : new(false, "Upgrade requirements or construction budget are not met."),
        "research" => service.Research(command.Id!, at),
        "project" => service.BuildProject(command.Id!, at),
        "property.buy" => service.BuyProperty(command.Id!, at),
        "property.select" => service.SelectProperty(command.Id!, at),
        "manager" => service.AssignManager(command.Id!, command.RoomId, at),
        "automation" => service.ConfigureAutomation(command.Automation!, at),
        "event" => service.StartEvent(command.Id!, at),
        "contract.accept" => service.AcceptContract(command.Id!, at),
        "contract.claim" => service.ClaimContract(at),
        "prestige" => Prestige(service, at),
        _ => new(false, "Unsupported Empire action."),
    };
    private static EmpireActionResult Prestige(EmpireService service, DateTimeOffset at)
    {
        var result = service.PrestigeEmpire(at);
        return new(result.Success, result.Message);
    }
}

// A fresh epoch per mounted browser prevents stale/reloaded documents from replaying
// an old purchase. No durable economy or save schema is added by presentation.
internal sealed class EmpireModernSession
{
    internal string Id { get; } = Guid.NewGuid().ToString("D");
    internal long Revision { get; private set; }
    private readonly Dictionary<string, EmpireModernReply> receipts = new(StringComparer.Ordinal);
    internal EmpireModernReply Execute(EmpireModernCommand command, Func<EmpireModernCommand, EmpireActionResult> commit,
        Func<EmpireModernCommand, object> preview)
    {
        if (command.Session != Id) return new(command.RequestId, false, "This Empire view has expired. Refresh before continuing.", Revision);
        if (receipts.TryGetValue(command.RequestId, out var previous)) return previous;
        if (command.ExpectedRevision != Revision) return new(command.RequestId, false, "Empire changed. Review the refreshed state before continuing.", Revision);
        if (command.Preview) return new(command.RequestId, true, "Preview only", Revision, preview(command));
        // Never evict receipts in a live epoch: an evicted request could purchase twice.
        if (receipts.Count >= 4096) return new(command.RequestId, false, "Reopen Empire to continue this long session.", Revision);
        EmpireActionResult result = commit(command);
        if (result.Success) Revision++;
        var reply = new EmpireModernReply(command.RequestId, result.Success, result.Message, Revision);
        receipts.Add(command.RequestId, reply);
        return reply;
    }
}
