namespace BlackjackCasino.Core.Empire;

public sealed record EmpireDepartment(string Id, string Name, string Description, decimal BaseCost,
    decimal BaseCreditsPerSecond, string Accent, int Floor = 1, EmpireTag Tags = EmpireTag.None, string Category = "Gaming", EmpireRequirement? Requirements = null)
{
    public EmpireRequirement UnlockRequirement => Requirements ?? EmpireFloorCatalog.Find(Floor)?.Requirement ?? new(Prestige: int.MaxValue);
}

public static class EmpireCatalog
{
    public static IReadOnlyList<EmpireDepartment> Departments { get; } = Array.AsReadOnly<EmpireDepartment>(
    [
        new("slots", "Slot Gallery", "Warm lights, brass cabinets and the first heartbeat of your casino.", 50m, .5m, "#D6B578", 1, EmpireTag.Gaming | EmpireTag.Slots | EmpireTag.Classic, "Gaming"),
        new("tables", "Card Salon", "Velvet tables and fresh decks. A quiet room with a growing reputation.", 350m, 3m, "#79BEA6", 1, EmpireTag.Gaming | EmpireTag.Tables | EmpireTag.Classic, "Gaming"),
        new("bar", "Midnight Bar", "Polished glass, soft jazz and a reason to stay for another round.", 2_500m, 15m, "#B99BD8", 1, EmpireTag.Hospitality, "Hospitality"),
        new("roulette", "Roulette Room", "The turn of a wheel brings a little more grandeur to the floor.", 15_000m, 70m, "#D78C83", 1, EmpireTag.Gaming | EmpireTag.Tables | EmpireTag.Classic, "Gaming"),
        new("vip", "VIP Lounge", "Private booths, golden details and the finest corner of your empire.", 100_000m, 300m, "#E0C58A", 1, EmpireTag.VIP, "VIP"),

        new("high-limit-slots", "High Limit Slots", "Private cabinets, tailored service and a floor built for very large nights.", 2_000_000m, 3_000m, "#61D7E8", 2, EmpireTag.Gaming | EmpireTag.Slots | EmpireTag.HighLimit | EmpireTag.VIP, "Gaming"),
        new("poker", "Poker Room", "Invitation-only tables where patience, reputation and long sessions pay.", 10_000_000m, 14_000m, "#79BEA6", 2, EmpireTag.Gaming | EmpireTag.Tables | EmpireTag.HighLimit, "Gaming"),
        new("dining", "Fine Dining", "A destination restaurant that keeps premium guests inside your estate.", 50_000_000m, 65_000m, "#D8B878", 2, EmpireTag.Hospitality, "Hospitality"),
        new("stage", "Grand Stage", "Live entertainment turns a gaming floor into a place guests plan a night around.", 250_000_000m, 300_000m, "#C48AE8", 2, EmpireTag.Entertainment, "Entertainment"),
        new("private-salon", "Private Gaming Salon", "The most exclusive room in the house: discreet, expensive and exceptionally productive.", 1_250_000_000m, 1_350_000m, "#E5C477", 2, EmpireTag.Gaming | EmpireTag.Tables | EmpireTag.HighLimit | EmpireTag.VIP, "Gaming"),
        new("hotel", "Luxury Hotel", "A destination stay linking every part of the resort.", 5_000_000_000m, 2_000_000m, "#83BFCB", 3, EmpireTag.Hospitality | EmpireTag.Resort, "Resort"),
        new("spa", "Spa & Wellness", "A peaceful retreat with premium hospitality.", 20_000_000_000m, 7_000_000m, "#A1D4BD", 3, EmpireTag.Hospitality | EmpireTag.Resort, "Resort"),
        new("sky-bar", "Sky Bar", "Drinks and music above the city lights.", 80_000_000_000m, 22_000_000m, "#CFA6D7", 3, EmpireTag.Entertainment | EmpireTag.Resort, "Resort"),
        new("convention-hall", "Convention Hall", "A permanent destination for casino gatherings.", 320_000_000_000m, 65_000_000m, "#9BADE1", 3, EmpireTag.Entertainment | EmpireTag.Resort, "Resort"),
        new("penthouse-club", "Penthouse Club", "A private address above your growing empire.", 1_280_000_000_000m, 180_000_000m, "#E3C388", 3, EmpireTag.VIP | EmpireTag.Resort, "Resort"),

    ]);

    public static IReadOnlyList<EmpireDepartment> Floor(int floor) => Departments.Where(room => room.Floor == floor).ToArray();
    public static EmpireDepartment? Find(string id) => Departments.FirstOrDefault(room => room.Id == id);
}

public static class EmpireRules
{
    public const int MaximumLevel = 200;
    public const int MaximumPrestige = 100;
    public const decimal MaximumCredits = 1_000_000_000_000_000_000_000_000_000m;
    public const decimal MaximumDividend = 320m;
    public const decimal PrestigeProductionBonus = .25m;
    public const int FirstPrestigeMainFloorLevels = 250;
    public const decimal FirstPrestigeRunCredits = 5_000_000m;
    public static readonly TimeSpan MaximumCatchUp = TimeSpan.FromHours(8);

    public static int RequiredPrestigeForFloor(int floor) => EmpireFloorCatalog.Find(floor)?.Requirement.Prestige ?? int.MaxValue;

    private static readonly IReadOnlyDictionary<string, decimal[]> Costs = EmpireCatalog.Departments.ToDictionary(room => room.Id, room =>
    {
        var values = new decimal[MaximumLevel + 1];
        decimal cost = room.BaseCost;
        for (int i = 0; i <= MaximumLevel; i++) { values[i] = decimal.Ceiling(cost); cost *= 1.18m; }
        return values;
    }, StringComparer.Ordinal);

    public static decimal UpgradeCost(EmpireDepartment room, int level)
    {
        if (Costs.TryGetValue(room.Id, out decimal[]? values) && EmpireCatalog.Find(room.Id) == room)
            return values[Math.Clamp(level, 0, MaximumLevel)];
        decimal cost = room.BaseCost;
        for (int i = 0; i < Math.Clamp(level, 0, MaximumLevel); i++) cost *= 1.18m;
        return decimal.Ceiling(cost);
    }

    public static decimal UpgradeCost(EmpireDepartment room, int level, int count)
    {
        int actual = Math.Clamp(count, 0, MaximumLevel - Math.Clamp(level, 0, MaximumLevel));
        decimal total = 0;
        for (int i = 0; i < actual; i++) total += UpgradeCost(room, level + i);
        return total;
    }

    public static decimal Production(EmpireDepartment room, int level)
    {
        level = Math.Clamp(level, 0, MaximumLevel);
        decimal multiplier = 1;
        for (int i = 0; i < level / 10; i++) multiplier *= 2;
        return room.BaseCreditsPerSecond * level * multiplier;
    }

    public static decimal PrestigeMultiplier(int prestige) => 1m + Math.Clamp(prestige, 0, MaximumPrestige) * PrestigeProductionBonus;

    public static int PrestigeMainFloorLevelRequirement(int currentPrestige)
        => Math.Min(500, FirstPrestigeMainFloorLevels + Math.Clamp(currentPrestige, 0, MaximumPrestige) * 10);

    public static decimal PrestigeRunCreditRequirement(int currentPrestige)
    {
        decimal step = Math.Clamp(currentPrestige, 0, MaximumPrestige) + 1m;
        return Math.Min(MaximumCredits, FirstPrestigeRunCredits * step * step);
    }
}
