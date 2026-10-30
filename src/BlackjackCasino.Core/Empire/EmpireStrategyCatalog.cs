namespace BlackjackCasino.Core.Empire;

[Flags]
public enum EmpireTag { None = 0, Gaming = 1, Tables = 2, Slots = 4, Hospitality = 8, Entertainment = 16, VIP = 32, HighLimit = 64, Classic = 128, Resort = 256 }

public sealed record EmpireRequirement(int Prestige = 0, int Reputation = 0, string? ProjectId = null, EmpireTag RoomTags = EmpireTag.None, int MinimumRoomLevel = 0, int MaximumFloor = 2, int TotalLevels = 0)
{
    public string Description => string.Join(" · ", new[]
    {
        Prestige > 0 ? $"Empire Prestige {Prestige}" : null,
        Reputation > 0 ? $"Reputation {Reputation:N0}" : null,
        ProjectId is not null ? "Project: " + ProjectId.Replace('-', ' ') : null,
        TotalLevels > 0 ? $"{TotalLevels} estate levels" : null,
        RoomTags != EmpireTag.None ? $"{RoomTags} departments at level {MinimumRoomLevel}" : null,
    }.Where(x => x is not null));
}
public sealed record EmpireFloorDefinition(int Id, string DisplayName, EmpireRequirement Requirement, int SortOrder, string Accent);
public static class EmpireFloorCatalog
{
    public static IReadOnlyList<EmpireFloorDefinition> All { get; } = Array.AsReadOnly<EmpireFloorDefinition>(
    [
        new(1, "Main Floor", new(), 0, "#B99B67"),
        new(2, "Upper Floor", new(Prestige: 1), 1, "#6CB9AB"),
        new(3, "Resort Floor", new(Prestige: 3, Reputation: 5000, ProjectId: "resort-wing"), 2, "#85BFD2"),
    ]);
    public static EmpireFloorDefinition? Find(int id) => All.FirstOrDefault(f => f.Id == id);
}
public sealed record EmpirePropertyDefinition(string Id, string Name, string Description, EmpireTag Specialty, decimal ProductionBonus, decimal Cost, EmpireRequirement Requirement);
public static class EmpirePropertyCatalog
{
    public const string DefaultId = "midnight-club";
    public static IReadOnlyList<EmpirePropertyDefinition> All { get; } = Array.AsReadOnly<EmpirePropertyDefinition>(
    [
        new(DefaultId, "The Midnight Club", "A balanced estate. The home of your casino career.", EmpireTag.None, 0, 0, new()),
        new("neon-bay", "Neon Bay Resort", "A coastal skyline built around modern slots.", EmpireTag.Slots, .25m, 250_000_000_000m, new(Prestige: 5, Reputation: 10000, ProjectId: "skyline-property")),
        new("crown-palace", "Crown Palace", "Private tables, discreet service and a royal guest list.", EmpireTag.Tables | EmpireTag.VIP, .25m, 1_000_000_000_000m, new(Prestige: 10, Reputation: 14000, ProjectId: "skyline-property")),
        new("aurora", "Aurora International", "A destination for hospitality, entertainment and grand occasions.", EmpireTag.Hospitality | EmpireTag.Entertainment, .25m, 5_000_000_000_000m, new(Prestige: 15, Reputation: 18000, ProjectId: "skyline-property")),
    ]);
    public static EmpirePropertyDefinition? Find(string id) => All.FirstOrDefault(p => p.Id == id);
}
public sealed record EmpireSynergyDefinition(string Id, string Name, string Description, EmpireTag Members, EmpireTag Beneficiaries, int[] Levels, decimal[] Bonuses, int MaximumFloor = 2);
public static class EmpireSynergyCatalog
{
    public static IReadOnlyList<EmpireSynergyDefinition> All { get; } = Array.AsReadOnly<EmpireSynergyDefinition>(
    [
        new("high-roller", "High Roller Circuit", "Build the VIP circuit together.", EmpireTag.VIP, EmpireTag.VIP, [25,50,100], [.10m,.20m,.35m]),
        new("night-out", "Night Out", "A complete evening: drinks, dinner and a live show.", EmpireTag.Hospitality | EmpireTag.Entertainment, EmpireTag.Hospitality | EmpireTag.Entertainment, [25,50,100], [.08m,.16m,.28m]),
        new("classic-casino", "Classic Casino", "Keep the original gaming floor at the heart of the estate.", EmpireTag.Classic, EmpireTag.Classic, [25,50,100], [.10m,.22m,.40m]),
        new("destination", "Destination Resort", "A balanced resort rewards every department.", EmpireTag.Resort, EmpireTag.Gaming | EmpireTag.Resort, [25,50,100], [.08m,.15m,.25m], 3),
    ]);
}
public sealed record EmpireResearchDefinition(string Id, string Name, string Branch, string Description, int MaxRank, int CostPerRank, string? Prerequisite = null, int PrerequisiteRank = 1);
public static class EmpireResearchCatalog
{
    public static IReadOnlyList<EmpireResearchDefinition> All { get; } = Array.AsReadOnly<EmpireResearchDefinition>(
    [
        new("construction", "Efficient Construction", "Operations", "Upgrade costs −2% per rank (up to −6%).", 3, 1),
        new("night-shift", "Night Shift", "Operations", "Offline storage +2 hours per rank (up to 12 hours).", 2, 2, "construction"),
        new("compound", "Compound Growth", "Operations", "+8% per rank at each room level 50 / 100 / 150 milestone.", 3, 2, "construction", 2),
        new("automation", "Executive Automation", "Operations", "Unlock automatic upgrades; requires Executive Offices to operate.", 1, 3, "compound"),
        new("directors", "Board of Directors", "Operations", "One additional active manager slot.", 1, 3, "automation"),
        new("tables", "Table Authority", "Gaming", "Table departments +8% production per rank.", 3, 1),
        new("mechanical", "Mechanical Empire", "Gaming", "Slot departments +8% production per rank.", 3, 1),
        new("high-roller", "High Roller Network", "Gaming", "VIP / High Limit production +5% per rank for each 50 room levels.", 3, 2, "tables", 2),
        new("hospitality", "Stay Another Night", "Hospitality", "Hospitality and entertainment production +8% per rank.", 3, 1),
        new("destination", "Destination Resort", "Hospitality", "Entertainment at level 50 gives gaming +5% per rank.", 3, 2, "hospitality", 2),
        new("guests", "Premium Guests", "Hospitality", "Owner rate +5% and reserve +40 cash per rank, globally bounded.", 2, 2, "hospitality"),
    ]);
    public static EmpireResearchDefinition? Find(string id) => All.FirstOrDefault(n => n.Id == id);
}
public sealed record EmpireProjectDefinition(string Id, string Name, string Description, decimal Cost, EmpireRequirement Requirement, EmpireTag TargetTags = EmpireTag.None, decimal ProductionBonus = 0);
public static class EmpireProjectCatalog
{
    public static IReadOnlyList<EmpireProjectDefinition> All { get; } = Array.AsReadOnly<EmpireProjectDefinition>(
    [
        new("grand-entrance", "Grand Entrance", "Permanent +5% estate production. Requires 250 total room levels.", 25_000_000m, new(TotalLevels: 250), ProductionBonus: .05m),
        new("casino-vault", "Casino Vault", "Adds $160 to the shared owner reserve; caps at $640 across the empire.", 100_000_000m, new(Prestige: 1)),
        new("luxury-atrium", "Luxury Atrium", "Hospitality +20%; Bar and Dining must both reach level 50.", 250_000_000m, new(RoomTags: EmpireTag.Hospitality, MinimumRoomLevel: 50), EmpireTag.Hospitality, .20m),
        new("high-roller-elevator", "High Roller Elevator", "VIP / High Limit +10% and stronger High Roller Circuit.", 500_000_000m, new(Prestige: 2), EmpireTag.VIP | EmpireTag.HighLimit, .10m),
        new("executive-offices", "Executive Offices", "Unlock one active manager; research can unlock automation.", 1_000_000_000m, new(Prestige: 3, Reputation: 2500)),
        new("convention-center", "Convention Center", "Event production boosts are 25% stronger. Grand Stage level 100.", 5_000_000_000m, new(RoomTags: EmpireTag.Entertainment, MinimumRoomLevel: 100)),
        new("penthouse-suites", "Penthouse Suites", "VIP +15% and owner rate +5%. VIP Lounge level 100.", 10_000_000_000m, new(RoomTags: EmpireTag.VIP, MinimumRoomLevel: 100, MaximumFloor: 1), EmpireTag.VIP, .15m),
        new("resort-wing", "Resort Wing", "Permanently opens the five-room Resort Floor. Complete the Grand Entrance first.", 25_000_000_000m, new(Prestige: 3, Reputation: 5000, ProjectId: "grand-entrance")),
        new("skyline-property", "Skyline Property Office", "Authorizes the purchase of additional independent properties.", 100_000_000_000m, new(Prestige: 5, Reputation: 10000, ProjectId: "resort-wing")),
    ]);
    public static EmpireProjectDefinition? Find(string id) => All.FirstOrDefault(p => p.Id == id);
}
public enum EmpireManagerEffect { AssignedProduction, TagProduction, Synergy, Construction }
public sealed record EmpireManagerDefinition(string Id, string Name, string Role, string Description, EmpireManagerEffect Effect, EmpireTag Tags, decimal Bonus, int RequiredPrestige);
public static class EmpireManagerCatalog
{
    public static IReadOnlyList<EmpireManagerDefinition> All { get; } = Array.AsReadOnly<EmpireManagerDefinition>(
    [
        new("elena", "Elena Ward", "Floor Manager", "Assigned department +15% production.", EmpireManagerEffect.AssignedProduction, EmpireTag.None, .15m, 3),
        new("victor", "Victor Shaw", "High Roller Host", "VIP / High Limit departments +12% production.", EmpireManagerEffect.TagProduction, EmpireTag.VIP | EmpireTag.HighLimit, .12m, 3),
        new("mara", "Mara Bell", "Entertainment Director", "Night Out / Resort synergies are 25% stronger.", EmpireManagerEffect.Synergy, EmpireTag.Hospitality | EmpireTag.Entertainment | EmpireTag.Resort, .25m, 3),
        new("daniel", "Daniel Price", "Operations Director", "Assigned department upgrade costs −8%.", EmpireManagerEffect.Construction, EmpireTag.None, .08m, 3),
    ]);
    public static EmpireManagerDefinition? Find(string id) => All.FirstOrDefault(m => m.Id == id);
}
public enum EmpireActivity { Blackjack, Dice, NeonVault, Manor, Sudoku }
public sealed record EmpireActivitySnapshot(long Blackjack = 0, long Dice = 0, long NeonVault = 0, long Manor = 0, long Sudoku = 0)
{
    public long Count(EmpireActivity activity) => Math.Max(0, activity switch { EmpireActivity.Blackjack => Blackjack, EmpireActivity.Dice => Dice, EmpireActivity.NeonVault => NeonVault, EmpireActivity.Manor => Manor, _ => Sudoku });
    public static EmpireActivitySnapshot FromProfile(PlayerProfileState profile) => new(profile.LifetimeStatistics.RoundsPlayed,
        profile.Kniffel.CompletedGames, profile.NeonVault.Spins, profile.Manor.PaidSpins + profile.Manor.FreeSpins, profile.Sudoku.CompletedPuzzles);
}
public sealed record EmpireEventBoost(EmpireTag Tags, decimal Bonus, bool RequireAll = false);
public sealed record EmpireEventDefinition(string Id, string Name, string Description, decimal Cost, TimeSpan Duration, EmpireEventBoost[] Boosts, EmpireActivity Activity, int ActivityGoal, EmpireRequirement Requirement);
public static class EmpireEventCatalog
{
    public static IReadOnlyList<EmpireEventDefinition> All { get; } = Array.AsReadOnly<EmpireEventDefinition>(
    [
        new("blackjack", "Blackjack Championship", "Table departments host a half-hour tournament. Play 10 Blackjack rounds for an encore.", 250_000m, TimeSpan.FromMinutes(30), [new(EmpireTag.Tables,.40m)], EmpireActivity.Blackjack, 10, new()),
        new("dice", "Midnight Dice League", "A community dice night for tables and entertainment. Finish a dice game for an encore.", 200_000m, TimeSpan.FromMinutes(30), [new(EmpireTag.Tables | EmpireTag.Entertainment | EmpireTag.Hospitality,.30m)], EmpireActivity.Dice, 1, new()),
        new("neon", "Neon Nights", "High Limit Slots +75%, other VIP rooms +25%. Play 25 Neon Vault spins for an encore.", 5_000_000m, TimeSpan.FromMinutes(30), [new(EmpireTag.Slots | EmpireTag.HighLimit,.75m,true),new(EmpireTag.VIP,.25m)], EmpireActivity.NeonVault, 25, new(Prestige: 1)),
        new("haunted", "Haunted Halloween", "A theatrical evening for hospitality and entertainment. Play 20 Manor spins for an encore.", 2_000_000m, TimeSpan.FromMinutes(30), [new(EmpireTag.Hospitality | EmpireTag.Entertainment,.45m)], EmpireActivity.Manor, 20, new(Prestige: 1)),
        new("quiet", "Quiet Sunday", "A relaxed evening for hospitality. Complete a Sudoku for an encore.", 150_000m, TimeSpan.FromMinutes(30), [new(EmpireTag.Hospitality,.30m)], EmpireActivity.Sudoku, 1, new()),
    ]);
    public static EmpireEventDefinition? Find(string id) => All.FirstOrDefault(e => e.Id == id);
}
public enum EmpireContractMetric { HospitalityCredits, VipCredits, LevelsPurchased }
public sealed record EmpireContractDefinition(string Id, string Name, string Description, EmpireContractMetric Metric, decimal Goal, string RewardDescription);
public static class EmpireContractCatalog
{
    public static IReadOnlyList<EmpireContractDefinition> All { get; } = Array.AsReadOnly<EmpireContractDefinition>(
    [
        new("tourist-weekend", "Tourist Weekend", "Produce 5M Hospitality credits at your own pace.", EmpireContractMetric.HospitalityCredits, 5_000_000m, "Hospitality +15% for 2 production hours"),
        new("high-roller-invitation", "High Roller Invitation", "Produce 20M VIP credits; the brief remains until completed.", EmpireContractMetric.VipCredits, 20_000_000m, "VIP +15% for 2 production hours"),
        new("expansion-drive", "Expansion Drive", "Purchase 50 room levels after accepting this brief.", EmpireContractMetric.LevelsPurchased, 50, "Construction −5% for 2 production hours"),
    ]);
    public static EmpireContractDefinition? Find(string id) => All.FirstOrDefault(c => c.Id == id);
}
