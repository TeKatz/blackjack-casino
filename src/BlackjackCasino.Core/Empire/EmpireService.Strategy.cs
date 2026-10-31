namespace BlackjackCasino.Core.Empire;

public sealed record EmpireSynergyStatus(EmpireSynergyDefinition Definition, int Tier, int MinimumLevel, decimal Bonus, int? NextLevel);

public sealed partial class EmpireService
{
    public int ResearchRank(string id) => state.Research.GetValueOrDefault(id);
    public int LegacyPointsEarned => TotalPrestige;
    public int LegacyPointsSpent => EmpireResearchCatalog.All.Sum(node => ResearchRank(node.Id) * node.CostPerRank);
    public int LegacyPointsAvailable => Math.Max(0, LegacyPointsEarned - LegacyPointsSpent);
    public int Reputation => ReputationFor(Estate);
    public IReadOnlySet<string> Projects => new HashSet<string>(Estate.Projects, StringComparer.Ordinal);
    public IReadOnlyList<EmpireSynergyStatus> Synergies => EmpireSynergyCatalog.All.Select(def => SynergyFor(Estate, def)).ToArray();
    public decimal ConstructionDiscount(string roomId) => ConstructionDiscountFor(Estate, roomId);

    private static int ReputationFor(EmpirePropertyState estate) => (int)Math.Min(int.MaxValue,
        estate.Levels.Values.Count(level => level > 0) * 100L + estate.Levels.Values.Sum(level => level / 10) * 30L
        + estate.Projects.Count * 250L + estate.Prestige * 750L
        + Math.Min(100, estate.EventsCompleted) * 10L + Math.Min(100, estate.ContractsCompleted) * 10L
        + EmpireSynergyCatalog.All.Sum(def => SynergyTier(estate, def).Tier) * 150L);

    private bool RequirementsMet(EmpirePropertyState estate, EmpireRequirement requirement) => estate.Prestige >= requirement.Prestige
        && ReputationFor(estate) >= requirement.Reputation
        && estate.Levels.Values.Sum() >= requirement.TotalLevels
        && (requirement.ProjectId is null || estate.Projects.Contains(requirement.ProjectId))
        && (requirement.RoomTags == EmpireTag.None || EmpireCatalog.Departments
            .Where(room => room.Floor <= requirement.MaximumFloor && (room.Tags & requirement.RoomTags) != 0)
            .All(room => estate.Levels.GetValueOrDefault(room.Id) >= requirement.MinimumRoomLevel));
    public bool Meets(EmpireRequirement requirement) => RequirementsMet(Estate, requirement);
    public bool CanResearch(string id) => EmpireResearchCatalog.Find(id) is { } node && Started
        && ResearchRank(id) < node.MaxRank && LegacyPointsAvailable >= node.CostPerRank
        && (node.Prerequisite is null || ResearchRank(node.Prerequisite) >= node.PrerequisiteRank);
    public EmpireActionResult Research(string id, DateTimeOffset now)
    {
        Accrue(now);
        if (!CanResearch(id)) return new(false, "Research requires Legacy Points, an available rank and its prerequisite.");
        state = state with { Research = new(state.Research, StringComparer.Ordinal) { [id] = ResearchRank(id) + 1 } };
        return new(true, $"{EmpireResearchCatalog.Find(id)!.Name}: rank {ResearchRank(id)} unlocked permanently.");
    }
    public bool CanBuildProject(string id) => EmpireProjectCatalog.Find(id) is { } project && Started
        && !Estate.Projects.Contains(id) && Credits >= project.Cost && RequirementsMet(Estate, project.Requirement);
    public EmpireActionResult BuildProject(string id, DateTimeOffset now)
    {
        Accrue(now);
        if (!CanBuildProject(id)) return new(false, "This project is owned, unaffordable or its estate requirements are not met.");
        EmpireProjectDefinition project = EmpireProjectCatalog.Find(id)!;
        Put(ActivePropertyId, Estate with { Credits = Credits - project.Cost, Projects = new(Estate.Projects, StringComparer.Ordinal) { id } });
        return new(true, $"{project.Name} completed permanently for {ActiveProperty.Name}.");
    }
    private static (int Tier, int Minimum) SynergyTier(EmpirePropertyState estate, EmpireSynergyDefinition definition)
    {
        EmpireDepartment[] members = EmpireCatalog.Departments.Where(room => room.Floor <= definition.MaximumFloor && (room.Tags & definition.Members) != 0).ToArray();
        int minimum = members.Length == 0 ? 0 : members.Min(room => estate.Levels.GetValueOrDefault(room.Id));
        return (definition.Levels.Count(level => minimum >= level), minimum);
    }
    private EmpireSynergyStatus SynergyFor(EmpirePropertyState estate, EmpireSynergyDefinition definition)
    {
        var (tier, minimum) = SynergyTier(estate, definition);
        decimal bonus = tier > 0 ? definition.Bonuses[tier - 1] : 0;
        if (definition.Id == "high-roller" && estate.Projects.Contains("high-roller-elevator")) bonus *= 1.25m;
        if (estate.Managers.ContainsKey("mara") && (definition.Beneficiaries & EmpireManagerCatalog.Find("mara")!.Tags) != 0) bonus *= 1.25m;
        return new(definition, tier, minimum, bonus, tier < definition.Levels.Length ? definition.Levels[tier] : null);
    }
    private decimal ConstructionDiscountFor(EmpirePropertyState estate, string roomId)
    {
        decimal discount = ResearchRank("construction") * .02m;
        foreach (var (id, room) in estate.Managers)
            if (room == roomId && EmpireManagerCatalog.Find(id) is { Effect: EmpireManagerEffect.Construction } manager) discount += manager.Bonus;
        if (estate.ContractBuff is { ContractId: "expansion-drive", RemainingSeconds: > 0 }) discount += .05m;
        return Math.Min(.25m, discount);
    }
    private decimal RoomRate(EmpirePropertyState estate, string propertyId, EmpireDepartment? room, DateTimeOffset at)
    {
        if (room is null || !FloorUnlockedFor(estate, room.Floor)) return 0;
        int level = estate.Levels.GetValueOrDefault(room.Id);
        if (level <= 0) return 0;
        decimal strategy = 0;
        if (room.Tags.HasFlag(EmpireTag.Tables)) strategy += ResearchRank("tables") * .08m;
        if (room.Tags.HasFlag(EmpireTag.Slots)) strategy += ResearchRank("mechanical") * .08m;
        if ((room.Tags & (EmpireTag.Hospitality | EmpireTag.Entertainment)) != 0) strategy += ResearchRank("hospitality") * .08m;
        if ((room.Tags & (EmpireTag.VIP | EmpireTag.HighLimit)) != 0) strategy += ResearchRank("high-roller") * .05m * (level / 50);
        strategy += ResearchRank("compound") * .08m * Math.Min(3, level / 50);
        if (room.Tags.HasFlag(EmpireTag.Gaming) && EmpireCatalog.Departments.Any(other => other.Tags.HasFlag(EmpireTag.Entertainment) && estate.Levels.GetValueOrDefault(other.Id) >= 50))
            strategy += ResearchRank("destination") * .05m;
        EmpirePropertyDefinition property = EmpirePropertyCatalog.Find(propertyId)!;
        if ((room.Tags & property.Specialty) != 0) strategy += property.ProductionBonus;
        decimal synergies = EmpireSynergyCatalog.All.Where(def => (def.Beneficiaries & room.Tags) != 0).Sum(def => SynergyFor(estate, def).Bonus);
        decimal projects = EmpireProjectCatalog.All.Where(project => estate.Projects.Contains(project.Id)
            && (project.TargetTags == EmpireTag.None || (project.TargetTags & room.Tags) != 0)).Sum(project => project.ProductionBonus);
        decimal managers = 0;
        foreach (var (id, assignment) in estate.Managers)
        {
            EmpireManagerDefinition manager = EmpireManagerCatalog.Find(id)!;
            if (manager.Effect == EmpireManagerEffect.AssignedProduction && assignment == room.Id
                || manager.Effect == EmpireManagerEffect.TagProduction && (room.Tags & manager.Tags) != 0) managers += manager.Bonus;
        }
        decimal eventBonus = 0;
        if (estate.Event is { } active && at >= active.StartedAt && at < active.EndsAt && EmpireEventCatalog.Find(active.Id) is { } ev)
        {
            eventBonus = ev.Boosts.Where(boost => boost.RequireAll
                ? room.Tags.HasFlag(boost.Tags) : (room.Tags & boost.Tags) != 0).Select(boost => boost.Bonus).DefaultIfEmpty().Max();
            if (estate.Projects.Contains("convention-center")) eventBonus *= 1.25m;
        }
        decimal contractBonus = estate.ContractBuff is { RemainingSeconds: > 0 } buff
            && (buff.ContractId == "tourist-weekend" && room.Tags.HasFlag(EmpireTag.Hospitality)
                || buff.ContractId == "high-roller-invitation" && room.Tags.HasFlag(EmpireTag.VIP)) ? .15m : 0;
        // All strategic additions share one bounded additive bucket. The established
        // prestige and claimed-achievement factors remain intact and independently visible.
        decimal bucket = Math.Min(5m, strategy + synergies + projects + managers + eventBonus + contractBonus);
        return Math.Min(100_000_000_000_000_000_000m, EmpireRules.Production(room, level)
            * EmpireRules.PrestigeMultiplier(estate.Prestige) * (1 + AchievementBonusPercent(room.Id)) * (1 + bucket));
    }
}
