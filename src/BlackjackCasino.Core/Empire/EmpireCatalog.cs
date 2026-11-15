namespace BlackjackCasino.Core.Empire;

/// <summary>A finite, visible progression path. No infinite building levels or hidden idle portfolios.</summary>
public sealed record EmpireBuilding(string Id, string Name, string Description,
    decimal BaseCost, decimal BaseRate, decimal ClickPower,
    string? RequiresId = null, int RequiresLevel = 0);

public static class EmpireCatalog
{
    public const int MaximumLevel = 8;
    public const int MaximumPrestige = 100;
    public const decimal MaximumCredits = 1_000_000_000_000_000_000_000_000m;
    public static readonly TimeSpan MaximumOfflineTime = TimeSpan.FromHours(4);

    public static IReadOnlyList<EmpireBuilding> Buildings { get; } = Array.AsReadOnly<EmpireBuilding>(
    [
        new("cashier", "Casino Cashier", "Turn every click into a better deal.", 12m, 0m, 2m),
        new("slots", "Slot Gallery", "Your first self-running gaming machines.", 45m, .6m, 0m, "cashier", 1),
        new("tables", "Card Salon", "Higher stakes and stronger active earnings.", 240m, 2.4m, 3m, "slots", 2),
        new("bar", "Midnight Bar", "A welcoming bar that keeps business moving.", 950m, 9m, 0m, "tables", 3),
        new("roulette", "Roulette Gallery", "A lavish table with a productive floor.", 3_900m, 34m, 6m, "bar", 3),
        new("poker", "Poker Club", "Exclusive tables lift the entire gaming floor.", 15_000m, 125m, 10m, "roulette", 3),
        new("stage", "Grand Stage", "Entertainment builds a profitable reputation.", 62_000m, 430m, 0m, "poker", 3),
        new("vip", "VIP Lounge", "Premium guests raise the value of every click.", 250_000m, 1_400m, 25m, "stage", 4),
        new("high-limit", "High Limit Floor", "High stakes bring exceptional income.", 1_000_000m, 5_000m, 45m, "vip", 4),
        new("hotel", "Grand Hotel", "A destination resort supports the whole casino.", 4_200_000m, 18_000m, 0m, "high-limit", 4),
        new("vault", "Casino Vault", "Store and multiply the wealth of the empire.", 17_000_000m, 60_000m, 90m, "hotel", 5),
        new("penthouse", "Penthouse Club", "The crown jewel of your casino empire.", 72_000_000m, 210_000m, 160m, "vault", 5),
    ]);

    public static EmpireBuilding? Find(string? id) =>
        Buildings.FirstOrDefault(b => string.Equals(b.Id, id, StringComparison.Ordinal));

    public static decimal NextCost(EmpireBuilding building, int currentLevel)
    {
        if (currentLevel is < 0 or >= MaximumLevel) return 0;
        decimal cost = building.BaseCost;
        for (int i = 0; i < currentLevel; i++) cost *= 3.2m;
        return decimal.Ceiling(cost);
    }

    public static int VisualTier(int level) => level switch
    {
        >= 8 => 4,
        >= 5 => 3,
        >= 3 => 2,
        >= 1 => 1,
        _ => 0,
    };
}
