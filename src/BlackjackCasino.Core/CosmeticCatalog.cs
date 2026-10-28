namespace BlackjackCasino.Core;

public static class CosmeticCatalog
{
    public const string DefaultTitleId = "title-rookie";
    public const string DefaultFrameId = "frame-standard";

    private static readonly IReadOnlyList<CosmeticDefinition> Cosmetics =
    [
        new(DefaultTitleId, "Rookie", CosmeticKind.Title, 0, false, "The default title."),
        new(DefaultFrameId, "Classic", CosmeticKind.Frame, 0, false, "The clean default profile frame."),

        new("title-night-owl", "Night Owl", CosmeticKind.Title, 300, true, "For sessions that somehow became 2 AM."),
        new("title-sharp-eye", "Sharp Eye", CosmeticKind.Title, 500, true, "A calm title for careful players."),
        new("title-high-roller", "High Roller", CosmeticKind.Title, 900, true, "Purely visual. Your bets remain your business."),
        new("title-house-favorite", "House Favorite", CosmeticKind.Title, 1400, true, "The house insists this is not suspicious."),
        new("title-after-hours", "After Hours", CosmeticKind.Title, 2000, true, "For the late-night table regular."),
        new("frame-emerald", "Emerald Edge", CosmeticKind.Frame, 600, true, "A deep green profile border."),
        new("frame-midnight", "Midnight Edge", CosmeticKind.Frame, 900, true, "A restrained midnight profile border."),
        new("frame-burgundy", "Burgundy Edge", CosmeticKind.Frame, 900, true, "A dark red profile border."),
        new("frame-gold-trim", "Gold Trim", CosmeticKind.Frame, 1800, true, "A subtle gold-trimmed profile border."),

        new("title-prestige-1", "The Regular", CosmeticKind.Title, 0, false, "Prestige I reward."),
        new("frame-prestige-2", "Prestige Bronze", CosmeticKind.Frame, 0, false, "Prestige II reward."),
        new("title-prestige-3", "Card Shark", CosmeticKind.Title, 0, false, "Prestige III reward."),
        new("frame-prestige-5", "Prestige Silver", CosmeticKind.Frame, 0, false, "Prestige V reward."),
        new("frame-prestige-10", "Prestige Gold", CosmeticKind.Frame, 0, false, "Prestige X reward."),
        new("title-prestige-25", "Blackjack Veteran", CosmeticKind.Title, 0, false, "Prestige XXV reward."),

        new("title-season-midnight-5", "Night Shift", CosmeticKind.Title, 0, false, "Midnight Shift tier 5 reward."),
        new("frame-season-midnight-10", "Midnight Line", CosmeticKind.Frame, 0, false, "Midnight Shift tier 10 reward."),
        new("title-season-midnight-20", "After Midnight", CosmeticKind.Title, 0, false, "Midnight Shift tier 20 reward."),
        new("frame-season-midnight-35", "Neon Noir", CosmeticKind.Frame, 0, false, "Midnight Shift tier 35 reward."),
        new("title-season-midnight-50", "Midnight Legend", CosmeticKind.Title, 0, false, "Midnight Shift tier 50 reward."),
    ];

    public static IReadOnlyList<CosmeticDefinition> All => Cosmetics;

    public static IReadOnlyList<CosmeticDefinition> ShopItems => Cosmetics.Where(x => x.ShopAvailable).ToArray();

    public static CosmeticDefinition Get(string id) => Cosmetics.First(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    public static CosmeticDefinition? TryGet(string id) => Cosmetics.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    public static IReadOnlyList<CosmeticDefinition> PrestigeRewards(int prestige)
    {
        string? id = prestige switch
        {
            1 => "title-prestige-1",
            2 => "frame-prestige-2",
            3 => "title-prestige-3",
            5 => "frame-prestige-5",
            10 => "frame-prestige-10",
            25 => "title-prestige-25",
            _ => null,
        };

        return id is null ? [] : [Get(id)];
    }
}
