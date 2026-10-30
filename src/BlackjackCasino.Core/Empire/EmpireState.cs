namespace BlackjackCasino.Core.Empire;

public sealed record EmpireAutomationState
{
    public bool Enabled { get; init; }
    public decimal Reserve { get; init; } = 1_000_000m;
    public decimal SpendPercent { get; init; } = .25m;
    public int TargetLevel { get; init; } = 100;
    public bool CheapestMilestone { get; init; } = true;
    public string[] Priorities { get; init; } = ["slots", "tables"];
}
public sealed record EmpireEventState(string Id, DateTimeOffset StartedAt, DateTimeOffset EndsAt, long ActivityBaseline, bool Encore = false);
public sealed record EmpireContractState(string Id, decimal Baseline);
public sealed record EmpireContractBuff(string ContractId, decimal RemainingSeconds);

/// <summary>Independent property economy. Credits, projects, rooms and management belong to this estate.</summary>
public sealed record EmpirePropertyState
{
    public decimal Credits { get; init; } = 150m;
    public decimal LifetimeCredits { get; init; }
    public decimal RunCredits { get; init; }
    public int Prestige { get; init; }
    public Dictionary<string, int> Levels { get; init; } = new(StringComparer.Ordinal) { ["slots"] = 1 };
    public HashSet<string> Projects { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Managers { get; init; } = new(StringComparer.Ordinal);
    public EmpireAutomationState Automation { get; init; } = new();
    public DateTimeOffset NextAutomationAt { get; init; }
    public EmpireEventState? Event { get; init; }
    public EmpireContractState? Contract { get; init; }
    public EmpireContractBuff? ContractBuff { get; init; }
    public Dictionary<EmpireTag, decimal> ProducedByTag { get; init; } = [];
    public long LevelsPurchased { get; init; }
    public long EventsCompleted { get; init; }
    public long EventEncores { get; init; }
    public long ContractsCompleted { get; init; }
    public long AutomatedLevels { get; init; }
}

/// <summary>Schema 3 keeps legacy fields as a projection of the selected property.
/// Properties are authoritative after migration; the owner dividend and research are shared.</summary>
public sealed record EmpireState
{
    public const int CurrentSchemaVersion = 3;
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public bool Started { get; init; }
    public decimal Credits { get; init; } = 150m;
    public decimal LifetimeCredits { get; init; }
    public decimal RunCredits { get; init; }
    public decimal Dividend { get; init; }
    public decimal DividendRemainder { get; init; }
    public decimal LifetimeCashClaimed { get; init; }
    public DateTimeOffset LastAccruedAt { get; init; }
    public int Prestige { get; init; }
    public Dictionary<string, int> Levels { get; init; } = new(StringComparer.Ordinal) { ["slots"] = 1 };
    public string ActivePropertyId { get; init; } = EmpirePropertyCatalog.DefaultId;
    public Dictionary<string, EmpirePropertyState> Properties { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Research { get; init; } = new(StringComparer.Ordinal);

    public static EmpireState Normalize(EmpireState? state)
    {
        state ??= new EmpireState();
        var properties = new Dictionary<string, EmpirePropertyState>(StringComparer.Ordinal);
        if (state.SchemaVersion >= 3 && state.Properties is { Count: > 0 })
            foreach (var (id, estate) in state.Properties)
                if (EmpirePropertyCatalog.Find(id) is not null && estate is not null) properties[id] = NormalizeProperty(estate);
        if (!properties.ContainsKey(EmpirePropertyCatalog.DefaultId))
            properties[EmpirePropertyCatalog.DefaultId] = NormalizeProperty(new()
            {
                Credits = state.Credits, LifetimeCredits = state.LifetimeCredits, RunCredits = state.RunCredits,
                Prestige = state.Prestige, Levels = state.Levels,
            });
        int points = properties.Values.Sum(p => p.Prestige);
        var research = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (EmpireResearchDefinition node in EmpireResearchCatalog.All)
        {
            int rank = Math.Clamp(state.Research?.GetValueOrDefault(node.Id) ?? 0, 0, node.MaxRank);
            if (node.Prerequisite is not null && research.GetValueOrDefault(node.Prerequisite) < node.PrerequisiteRank) rank = 0;
            rank = Math.Min(rank, points / node.CostPerRank);
            if (rank > 0) research[node.Id] = rank;
            points -= rank * node.CostPerRank;
        }
        foreach (string id in properties.Keys.ToArray())
        {
            EmpirePropertyState estate = properties[id];
            int slots = estate.Projects.Contains("executive-offices") ? Math.Min(3, 1 + (estate.Prestige >= 5 ? 1 : 0) + research.GetValueOrDefault("directors")) : 0;
            var managers = estate.Managers.Where(pair => EmpireManagerCatalog.Find(pair.Key) is { } manager && estate.Prestige >= manager.RequiredPrestige)
                .Take(slots).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            properties[id] = estate with { Managers = managers };
        }
        string active = state.ActivePropertyId is not null && properties.ContainsKey(state.ActivePropertyId) ? state.ActivePropertyId : EmpirePropertyCatalog.DefaultId;
        EmpirePropertyState current = properties[active];
        decimal capacity = Math.Min(640m, EmpireRules.MaximumDividend + research.GetValueOrDefault("guests") * 40m + properties.Values.Count(p => p.Projects.Contains("casino-vault")) * 160m);
        return state with
        {
            SchemaVersion = CurrentSchemaVersion, ActivePropertyId = active, Properties = properties, Research = research,
            Credits = current.Credits, RunCredits = current.RunCredits, Prestige = current.Prestige,
            LifetimeCredits = Math.Clamp(Math.Max(state.LifetimeCredits, properties.Values.Sum(p => p.LifetimeCredits)), 0, EmpireRules.MaximumCredits),
            Dividend = Math.Clamp(state.Dividend, 0, capacity), DividendRemainder = Math.Clamp(state.DividendRemainder, 0, 36m),
            LifetimeCashClaimed = Math.Clamp(state.LifetimeCashClaimed, 0, EmpireRules.MaximumCredits),
            Levels = new(current.Levels, StringComparer.Ordinal),
        };
    }

    private static EmpirePropertyState NormalizeProperty(EmpirePropertyState estate)
    {
        int prestige = Math.Clamp(estate.Prestige, 0, EmpireRules.MaximumPrestige);
        var projects = new HashSet<string>((estate.Projects ?? []).Where(id => EmpireProjectCatalog.Find(id) is not null), StringComparer.Ordinal);
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (EmpireDepartment room in EmpireCatalog.Departments)
        {
            int level = Math.Clamp(estate.Levels?.GetValueOrDefault(room.Id) ?? 0, 0, EmpireRules.MaximumLevel);
            EmpireFloorDefinition? floor = EmpireFloorCatalog.Find(room.Floor);
            if (floor is null || prestige < floor.Requirement.Prestige || floor.Requirement.ProjectId is string projectId && !projects.Contains(projectId)) level = 0;
            levels[room.Id] = level;
        }
        levels["slots"] = Math.Max(1, levels["slots"]);
        var managers = (estate.Managers ?? []).Where(pair => EmpireManagerCatalog.Find(pair.Key) is not null && EmpireCatalog.Find(pair.Value) is not null)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        EmpireAutomationState auto = estate.Automation ?? new();
        var automation = auto with
        {
            Reserve = Math.Clamp(auto.Reserve, 0, EmpireRules.MaximumCredits), SpendPercent = Math.Clamp(auto.SpendPercent, .01m, .50m),
            TargetLevel = Math.Clamp(auto.TargetLevel, 1, EmpireRules.MaximumLevel),
            Priorities = (auto.Priorities ?? []).Where(id => EmpireCatalog.Find(id) is not null).Distinct(StringComparer.Ordinal).Take(EmpireCatalog.Departments.Count).ToArray(),
        };
        EmpireEventState? activeEvent = estate.Event;
        if (activeEvent is not null && (EmpireEventCatalog.Find(activeEvent.Id) is not { } definition || activeEvent.StartedAt == default
            || activeEvent.EndsAt <= activeEvent.StartedAt || activeEvent.EndsAt - activeEvent.StartedAt > definition.Duration + TimeSpan.FromMinutes(10))) activeEvent = null;
        if (activeEvent is not null) activeEvent = activeEvent with { ActivityBaseline = Math.Max(0, activeEvent.ActivityBaseline) };
        var tags = (estate.ProducedByTag ?? []).Where(pair => pair.Key != EmpireTag.None && Enum.IsDefined(pair.Key))
            .ToDictionary(x => x.Key, x => Math.Clamp(x.Value, 0, EmpireRules.MaximumCredits));
        EmpireContractState? contract = estate.Contract is { } c && EmpireContractCatalog.Find(c.Id) is not null ? c with { Baseline = Math.Clamp(c.Baseline, 0, EmpireRules.MaximumCredits) } : null;
        EmpireContractBuff? buff = estate.ContractBuff is { } b && EmpireContractCatalog.Find(b.ContractId) is not null && b.RemainingSeconds > 0
            ? b with { RemainingSeconds = Math.Min(7200, b.RemainingSeconds) } : null;
        return estate with
        {
            Prestige = prestige, Levels = levels, Projects = projects, Managers = managers, Automation = automation, Event = activeEvent,
            Contract = contract, ContractBuff = buff, ProducedByTag = tags,
            Credits = Math.Clamp(estate.Credits, 0, EmpireRules.MaximumCredits), LifetimeCredits = Math.Clamp(estate.LifetimeCredits, 0, EmpireRules.MaximumCredits),
            RunCredits = Math.Clamp(estate.RunCredits, 0, EmpireRules.MaximumCredits),
            LevelsPurchased = Math.Max(0, estate.LevelsPurchased), EventsCompleted = Math.Max(0, estate.EventsCompleted),
            EventEncores = Math.Max(0, estate.EventEncores), ContractsCompleted = Math.Max(0, estate.ContractsCompleted), AutomatedLevels = Math.Max(0, estate.AutomatedLevels),
        };
    }
}
