namespace BlackjackCasino.Core.Empire;

public sealed partial class EmpireService
{
    public int ManagerSlots => ManagerSlotsFor(Estate);
    public IReadOnlyDictionary<string, string> ManagerAssignments => new Dictionary<string, string>(Estate.Managers, StringComparer.Ordinal);
    private int ManagerSlotsFor(EmpirePropertyState estate) => estate.Projects.Contains("executive-offices")
        ? Math.Min(3, 1 + (estate.Prestige >= 5 ? 1 : 0) + ResearchRank("directors")) : 0;
    public EmpireActionResult AssignManager(string managerId, string? roomId, DateTimeOffset now)
    {
        Accrue(now);
        EmpireManagerDefinition? manager = EmpireManagerCatalog.Find(managerId);
        if (manager is null || !Started) return new(false, "Unknown manager or estate not started.");
        var assignments = new Dictionary<string, string>(Estate.Managers, StringComparer.Ordinal);
        if (roomId is null)
        {
            if (!assignments.Remove(managerId)) return new(false, "This manager is not active.");
        }
        else
        {
            if (Prestige < manager.RequiredPrestige || ManagerSlots == 0 || !assignments.ContainsKey(managerId) && assignments.Count >= ManagerSlots)
                return new(false, $"Requires Executive Offices, Prestige {manager.RequiredPrestige} and a free manager slot.");
            if (EmpireCatalog.Find(roomId) is null || Level(roomId) <= 0) return new(false, "Assign managers to an operating department.");
            assignments[managerId] = roomId;
        }
        Put(ActivePropertyId, Estate with { Managers = assignments });
        return new(true, roomId is null ? $"{manager.Name} is now inactive." : $"{manager.Name} assigned to {EmpireCatalog.Find(roomId)!.Name}.");
    }
    public bool AutomationUnlocked => AutomationUnlockedFor(Estate);
    private bool AutomationUnlockedFor(EmpirePropertyState estate) => estate.Projects.Contains("executive-offices") && ResearchRank("automation") > 0;
    public EmpireAutomationState Automation => Estate.Automation with { Priorities = Estate.Automation.Priorities.ToArray() };
    public EmpireActionResult ConfigureAutomation(EmpireAutomationState config, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(config);
        Accrue(now);
        if (!Started || !AutomationUnlocked) return new(false, "Executive Offices and Executive Automation research are required.");
        if (config.Reserve < 0 || config.Reserve > EmpireRules.MaximumCredits || config.SpendPercent is < .01m or > .50m || config.TargetLevel is < 1 or > EmpireRules.MaximumLevel
            || config.Priorities is null || config.Priorities.Any(id => EmpireCatalog.Find(id) is null)) return new(false, "Use a nonnegative reserve, a 1–50% budget and a level target from 1 to 200.");
        Put(ActivePropertyId, Estate with { Automation = config with { Priorities = config.Priorities.Distinct(StringComparer.Ordinal).ToArray() }, NextAutomationAt = (now > state.LastAccruedAt ? now : state.LastAccruedAt).AddSeconds(30) });
        return new(true, config.Enabled ? "Automation enabled. A bounded upgrade budget is evaluated every 30 production seconds." : "Automation paused.");
    }
    private EmpirePropertyState Automate(EmpirePropertyState estate)
    {
        decimal budget = Math.Min(Math.Max(0, estate.Credits - estate.Automation.Reserve), estate.Credits * estate.Automation.SpendPercent);
        for (int step = 0; step < 50 && budget > 0; step++)
        {
            var candidates = new List<EmpireUpgradeQuote>();
            IEnumerable<EmpireDepartment> ordered = estate.Automation.CheapestMilestone ? EmpireCatalog.Departments
                : estate.Automation.Priorities.Select(id => EmpireCatalog.Find(id)!);
            foreach (EmpireDepartment room in ordered)
            {
                int level = estate.Levels.GetValueOrDefault(room.Id);
                if (level >= estate.Automation.TargetLevel) continue;
                int count = estate.Automation.CheapestMilestone ? Math.Min(10 - level % 10, estate.Automation.TargetLevel - level) : 1;
                EmpireUpgradeQuote quote = QuoteFor(estate, room.Id, count);
                if (quote.Unlocked && quote.Levels > 0 && quote.Cost <= budget) candidates.Add(quote);
            }
            EmpireUpgradeQuote? best = estate.Automation.CheapestMilestone ? candidates.OrderBy(q => q.Cost).ThenBy(q => q.RoomId, StringComparer.Ordinal).FirstOrDefault() : candidates.FirstOrDefault();
            if (best is null) break;
            estate = BuyLevels(estate, best) with { AutomatedLevels = Math.Min(long.MaxValue - best.Levels, estate.AutomatedLevels) + best.Levels };
            budget -= best.Cost;
        }
        return estate;
    }
    public EmpireEventState? ActiveEvent => Estate.Event;
    public int EventActivityProgress => Estate.Event is { } ev && EmpireEventCatalog.Find(ev.Id) is { } definition
        ? (int)Math.Min(definition.ActivityGoal, Math.Max(0, activity.Count(definition.Activity) - ev.ActivityBaseline)) : 0;
    public bool CanStartEvent(string id) => Started && Estate.Event is null && EmpireEventCatalog.Find(id) is { } ev && Credits >= ev.Cost && RequirementsMet(Estate, ev.Requirement);
    public EmpireActionResult StartEvent(string id, DateTimeOffset now)
    {
        Accrue(now);
        if (!CanStartEvent(id)) return new(false, "One event per property. Meet its requirements and reserve the entry budget first.");
        EmpireEventDefinition definition = EmpireEventCatalog.Find(id)!;
        DateTimeOffset start = now < state.LastAccruedAt ? state.LastAccruedAt : now;
        Put(ActivePropertyId, Estate with { Credits = Credits - definition.Cost, Event = new(id, start, start + definition.Duration, activity.Count(definition.Activity)) });
        return new(true, $"{definition.Name} started. The optional game goal adds a ten-minute encore; no game rules change.");
    }
    public void ObserveActivity(EmpireActivitySnapshot current, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);
        Accrue(now); // New completions must not retroactively boost the previous interval.
        activity = current;
        foreach (string id in state.Properties.Keys.ToArray())
        {
            EmpirePropertyState estate = state.Properties[id];
            if (estate.Event is not { Encore: false } ev || now < ev.StartedAt || now >= ev.EndsAt) continue;
            EmpireEventDefinition definition = EmpireEventCatalog.Find(ev.Id)!;
            if (activity.Count(definition.Activity) - ev.ActivityBaseline < definition.ActivityGoal) continue;
            Put(id, estate with { Event = ev with { Encore = true, EndsAt = ev.EndsAt.AddMinutes(10) }, EventEncores = estate.EventEncores == long.MaxValue ? long.MaxValue : estate.EventEncores + 1 });
        }
    }
    private static EmpirePropertyState ExpireEffects(EmpirePropertyState estate, DateTimeOffset at) => estate with
    {
        Event = estate.Event is { } ev && ev.EndsAt <= at ? null : estate.Event,
        EventsCompleted = estate.EventsCompleted < long.MaxValue && estate.Event is { } completed && completed.EndsAt <= at ? estate.EventsCompleted + 1 : estate.EventsCompleted,
        ContractBuff = estate.ContractBuff is { RemainingSeconds: > 0 } buff ? buff : null,
    };
    public EmpireContractState? ActiveContract => Estate.Contract;
    public EmpireContractBuff? ContractBuff => Estate.ContractBuff;
    private static decimal ContractMetric(EmpirePropertyState estate, EmpireContractMetric metric) => metric switch
    {
        EmpireContractMetric.HospitalityCredits => estate.ProducedByTag.GetValueOrDefault(EmpireTag.Hospitality),
        EmpireContractMetric.VipCredits => estate.ProducedByTag.GetValueOrDefault(EmpireTag.VIP),
        _ => estate.LevelsPurchased,
    };
    public decimal ContractProgress => Estate.Contract is { } c && EmpireContractCatalog.Find(c.Id) is { } definition
        ? Math.Clamp(ContractMetric(Estate, definition.Metric) - c.Baseline, 0, definition.Goal) : 0;
    public bool CanClaimContract => Estate.Contract is { } c && EmpireContractCatalog.Find(c.Id) is { } definition && ContractProgress >= definition.Goal;
    public EmpireActionResult AcceptContract(string id, DateTimeOffset now)
    {
        Accrue(now);
        if (!Started || Estate.Contract is not null || EmpireContractCatalog.Find(id) is not { } definition) return new(false, "Finish the current contract before accepting a new brief.");
        Put(ActivePropertyId, Estate with { Contract = new(id, ContractMetric(Estate, definition.Metric)) });
        return new(true, $"{definition.Name} accepted. It has no deadline.");
    }
    public EmpireActionResult ClaimContract(DateTimeOffset now)
    {
        Accrue(now);
        if (!CanClaimContract) return new(false, "The contract's production or construction goal is not complete.");
        string id = Estate.Contract!.Id;
        Put(ActivePropertyId, Estate with { Contract = null, ContractsCompleted = Estate.ContractsCompleted == long.MaxValue ? long.MaxValue : Estate.ContractsCompleted + 1, ContractBuff = new(id, 7200) });
        return new(true, "Contract complete. A two-hour production buff replaces the previous contract buff. Choose your next brief.");
    }
    public bool CanBuyProperty(string id) => Started && !state.Properties.ContainsKey(id) && EmpirePropertyCatalog.Find(id) is { } property
        && Credits >= property.Cost && RequirementsMet(Estate, property.Requirement);
    public EmpireActionResult BuyProperty(string id, DateTimeOffset now)
    {
        Accrue(now);
        if (!CanBuyProperty(id)) return new(false, "Meet this property's expansion requirements and purchase cost in your current estate.");
        EmpirePropertyDefinition property = EmpirePropertyCatalog.Find(id)!;
        Put(ActivePropertyId, Estate with { Credits = Credits - property.Cost });
        var estates = new Dictionary<string, EmpirePropertyState>(state.Properties, StringComparer.Ordinal) { [id] = new() };
        state = state with { Properties = estates };
        return new(true, $"{property.Name} purchased. It has an independent budget and keeps earning while you manage other properties.");
    }
    public EmpireActionResult SelectProperty(string id, DateTimeOffset now)
    {
        Accrue(now);
        if (!state.Properties.TryGetValue(id, out EmpirePropertyState? estate)) return new(false, "Purchase this property first.");
        state = state with { ActivePropertyId = id, Credits = estate.Credits, RunCredits = estate.RunCredits, Prestige = estate.Prestige, Levels = estate.Levels };
        return new(true, $"Managing {EmpirePropertyCatalog.Find(id)!.Name}.");
    }
}
