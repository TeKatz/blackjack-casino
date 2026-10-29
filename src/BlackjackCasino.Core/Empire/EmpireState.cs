namespace BlackjackCasino.Core.Empire;

/// <summary>A separate idle economy; credits are never the shared casino bankroll.</summary>
public sealed record EmpireState
{
    public int SchemaVersion { get; init; } = 1;
    public bool Started { get; init; }
    public decimal Credits { get; init; } = 150m;
    public decimal LifetimeCredits { get; init; }
    public decimal Dividend { get; init; }
    /// <summary>Unpaid cash-rate seconds, retained until they make a full cent.</summary>
    public decimal DividendRemainder { get; init; }
    public decimal LifetimeCashClaimed { get; init; }
    public DateTimeOffset LastAccruedAt { get; init; }
    public Dictionary<string, int> Levels { get; init; } = new(StringComparer.Ordinal)
    {
        ["slots"] = 1,
    };

    public static EmpireState Normalize(EmpireState? state)
    {
        state ??= new EmpireState();
        var levels = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (EmpireDepartment room in EmpireCatalog.Departments)
            levels[room.Id] = Math.Clamp(state.Levels?.GetValueOrDefault(room.Id) ?? 0, 0, EmpireRules.MaximumLevel);
        // A working starter machine means no save can leave the player unable to progress.
        levels["slots"] = Math.Max(1, levels["slots"]);
        return state with
        {
            SchemaVersion = 1,
            Credits = Math.Clamp(state.Credits, 0, EmpireRules.MaximumCredits),
            LifetimeCredits = Math.Clamp(state.LifetimeCredits, 0, EmpireRules.MaximumCredits),
            Dividend = Math.Clamp(state.Dividend, 0, EmpireRules.MaximumDividend),
            DividendRemainder = Math.Clamp(state.DividendRemainder, 0, 36m),
            LifetimeCashClaimed = Math.Clamp(state.LifetimeCashClaimed, 0, EmpireRules.MaximumCredits),
            Levels = levels,
        };
    }
}

public sealed record EmpireDepartment(string Id, string Name, string Description, decimal BaseCost,
    decimal BaseCreditsPerSecond, string Accent);

public static class EmpireCatalog
{
    public static IReadOnlyList<EmpireDepartment> Departments { get; } = Array.AsReadOnly<EmpireDepartment>(
    [
        new("slots", "Slot Gallery", "Warm lights, brass cabinets and the first heartbeat of your casino.", 50m, .5m, "#D6B578"),
        new("tables", "Card Salon", "Velvet tables and fresh decks. A quiet room with a growing reputation.", 350m, 3m, "#79BEA6"),
        new("bar", "Midnight Bar", "Polished glass, soft jazz and a reason to stay for another round.", 2_500m, 15m, "#B99BD8"),
        new("roulette", "Roulette Room", "The turn of a wheel brings a little more grandeur to the floor.", 15_000m, 70m, "#D78C83"),
        new("vip", "VIP Lounge", "Private booths, golden details and the finest corner of your empire.", 100_000m, 300m, "#E0C58A"),
    ]);

    public static EmpireDepartment? Find(string id) => Departments.FirstOrDefault(room => room.Id == id);
}

public static class EmpireRules
{
    public const int MaximumLevel = 200;
    public const decimal MaximumCredits = 1_000_000_000_000_000_000_000_000m;
    public const decimal MaximumDividend = 320m;
    public static readonly TimeSpan MaximumCatchUp = TimeSpan.FromHours(8);

    public static decimal UpgradeCost(EmpireDepartment room, int level)
    {
        decimal cost = room.BaseCost;
        for (int i = 0; i < Math.Clamp(level, 0, MaximumLevel); i++) cost *= 1.18m;
        return decimal.Ceiling(cost);
    }

    public static decimal Production(EmpireDepartment room, int level)
    {
        level = Math.Clamp(level, 0, MaximumLevel);
        decimal multiplier = 1;
        for (int i = 0; i < level / 10; i++) multiplier *= 2;
        return room.BaseCreditsPerSecond * level * multiplier;
    }
}
