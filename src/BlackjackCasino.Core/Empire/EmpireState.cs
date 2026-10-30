namespace BlackjackCasino.Core.Empire;

/// <summary>A separate idle economy; credits are never the shared casino bankroll.</summary>
public sealed record EmpireState
{
    public int SchemaVersion { get; init; } = 2;
    public bool Started { get; init; }
    public decimal Credits { get; init; } = 150m;
    public decimal LifetimeCredits { get; init; }
    /// <summary>Credits produced since the most recent Empire prestige.</summary>
    public decimal RunCredits { get; init; }
    public decimal Dividend { get; init; }
    /// <summary>Unpaid cash-rate seconds, retained until they make a full cent.</summary>
    public decimal DividendRemainder { get; init; }
    public decimal LifetimeCashClaimed { get; init; }
    public DateTimeOffset LastAccruedAt { get; init; }
    /// <summary>Empire-only prestige. This is intentionally independent of player-profile prestige.</summary>
    public int Prestige { get; init; }
    public Dictionary<string, int> Levels { get; init; } = new(StringComparer.Ordinal)
    {
        ["slots"] = 1,
    };

    public static EmpireState Normalize(EmpireState? state)
    {
        state ??= new EmpireState();
        int prestige = Math.Clamp(state.Prestige, 0, EmpireRules.MaximumPrestige);
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (EmpireDepartment room in EmpireCatalog.Departments)
        {
            int value = Math.Clamp(state.Levels?.GetValueOrDefault(room.Id) ?? 0, 0, EmpireRules.MaximumLevel);
            if (room.Floor > 1 && prestige < EmpireRules.RequiredPrestigeForFloor(room.Floor)) value = 0;
            levels[room.Id] = value;
        }
        // A working starter machine means no save can leave the player unable to progress.
        levels["slots"] = Math.Max(1, levels["slots"]);
        return state with
        {
            SchemaVersion = 2,
            Prestige = prestige,
            Credits = Math.Clamp(state.Credits, 0, EmpireRules.MaximumCredits),
            LifetimeCredits = Math.Clamp(state.LifetimeCredits, 0, EmpireRules.MaximumCredits),
            RunCredits = Math.Clamp(state.RunCredits, 0, EmpireRules.MaximumCredits),
            Dividend = Math.Clamp(state.Dividend, 0, EmpireRules.MaximumDividend),
            DividendRemainder = Math.Clamp(state.DividendRemainder, 0, 36m),
            LifetimeCashClaimed = Math.Clamp(state.LifetimeCashClaimed, 0, EmpireRules.MaximumCredits),
            Levels = levels,
        };
    }
}

public sealed record EmpireDepartment(string Id, string Name, string Description, decimal BaseCost,
    decimal BaseCreditsPerSecond, string Accent, int Floor = 1);

public static class EmpireCatalog
{
    public static IReadOnlyList<EmpireDepartment> Departments { get; } = Array.AsReadOnly<EmpireDepartment>(
    [
        new("slots", "Slot Gallery", "Warm lights, brass cabinets and the first heartbeat of your casino.", 50m, .5m, "#D6B578"),
        new("tables", "Card Salon", "Velvet tables and fresh decks. A quiet room with a growing reputation.", 350m, 3m, "#79BEA6"),
        new("bar", "Midnight Bar", "Polished glass, soft jazz and a reason to stay for another round.", 2_500m, 15m, "#B99BD8"),
        new("roulette", "Roulette Room", "The turn of a wheel brings a little more grandeur to the floor.", 15_000m, 70m, "#D78C83"),
        new("vip", "VIP Lounge", "Private booths, golden details and the finest corner of your empire.", 100_000m, 300m, "#E0C58A"),

        new("high-limit-slots", "High Limit Slots", "Private cabinets, tailored service and a floor built for very large nights.", 2_000_000m, 3_000m, "#61D7E8", 2),
        new("poker", "Poker Room", "Invitation-only tables where patience, reputation and long sessions pay.", 10_000_000m, 14_000m, "#79BEA6", 2),
        new("dining", "Fine Dining", "A destination restaurant that keeps premium guests inside your estate.", 50_000_000m, 65_000m, "#D8B878", 2),
        new("stage", "Grand Stage", "Live entertainment turns a gaming floor into a place guests plan a night around.", 250_000_000m, 300_000m, "#C48AE8", 2),
        new("private-salon", "Private Gaming Salon", "The most exclusive room in the house: discreet, expensive and exceptionally productive.", 1_250_000_000m, 1_350_000m, "#E5C477", 2),
    ]);

    public static IReadOnlyList<EmpireDepartment> Floor(int floor) => Departments.Where(room => room.Floor == floor).ToArray();
    public static EmpireDepartment? Find(string id) => Departments.FirstOrDefault(room => room.Id == id);
}

public static class EmpireRules
{
    public const int MaximumLevel = 200;
    public const int MaximumPrestige = 100;
    public const decimal MaximumCredits = 1_000_000_000_000_000_000_000_000m;
    public const decimal MaximumDividend = 320m;
    public const decimal PrestigeProductionBonus = .25m;
    public const int FirstPrestigeMainFloorLevels = 250;
    public const decimal FirstPrestigeRunCredits = 5_000_000m;
    public static readonly TimeSpan MaximumCatchUp = TimeSpan.FromHours(8);

    public static int RequiredPrestigeForFloor(int floor) => floor <= 1 ? 0 : floor - 1;

    public static decimal UpgradeCost(EmpireDepartment room, int level)
    {
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
        => Math.Min(EmpireCatalog.Floor(1).Count * MaximumLevel, FirstPrestigeMainFloorLevels + Math.Clamp(currentPrestige, 0, MaximumPrestige) * 25);

    public static decimal PrestigeRunCreditRequirement(int currentPrestige)
    {
        decimal step = Math.Clamp(currentPrestige, 0, MaximumPrestige) + 1m;
        return Math.Min(MaximumCredits, FirstPrestigeRunCredits * step * step);
    }
}
