namespace BlackjackCasino.Core;

public static class CosmeticCatalog
{
    public const string DefaultTitleId = "title-rookie";
    public const string DefaultFrameId = "frame-standard";
    public const string DefaultPortraitId = "portrait-classic";

    private static readonly IReadOnlyList<CosmeticDefinition> Cosmetics =
    [
        new(DefaultTitleId, "Rookie", CosmeticKind.Title, 0, false, "The default title."),
        new(DefaultFrameId, "Classic", CosmeticKind.Frame, 0, false, "The clean default profile frame.", "classic"),
        new(DefaultPortraitId, "House Guest", CosmeticKind.Portrait, 0, false, "The default casino portrait.", "portrait-house-guest"),

        // Shop titles.
        new("title-night-owl", "Night Owl", CosmeticKind.Title, 300, true, "For sessions that somehow became 2 AM."),
        new("title-sharp-eye", "Sharp Eye", CosmeticKind.Title, 500, true, "A calm title for careful players."),
        new("title-high-roller", "High Roller", CosmeticKind.Title, 900, true, "Purely visual. Your bets remain your business."),
        new("title-house-favorite", "House Favorite", CosmeticKind.Title, 1400, true, "The house insists this is not suspicious."),
        new("title-after-hours", "After Hours", CosmeticKind.Title, 2000, true, "For the late-night table regular."),
        new("title-card-counter-ish", "Probably Counting", CosmeticKind.Title, 2400, true, "Legal disclaimer: probably just lucky."),
        new("title-neon-regular", "Neon Regular", CosmeticKind.Title, 3200, true, "A modern title for a very familiar face."),
        new("title-house-myth", "House Myth", CosmeticKind.Title, 5200, true, "For players whose save file has stories."),

        // Shop frames. Every purchasable frame has a distinct presentation variant used by the WPF profile UI.
        new("frame-emerald", "Emerald Edge", CosmeticKind.Frame, 600, true, "Deep green glass with a slow breathing glow.", "emerald-breathe"),
        new("frame-midnight", "Midnight Edge", CosmeticKind.Frame, 900, true, "A cool blue border with a restrained moonlight sweep.", "midnight-sweep"),
        new("frame-burgundy", "Burgundy Edge", CosmeticKind.Frame, 900, true, "Dark red velvet with a slow ember pulse.", "burgundy-ember"),
        new("frame-gold-trim", "Gold Trim", CosmeticKind.Frame, 1800, true, "Warm metallic trim with a subtle travelling shimmer.", "gold-shimmer"),
        new("frame-aurora-glass", "Aurora Glass", CosmeticKind.Frame, 2600, true, "Cool teal and violet light drifts around the profile edge.", "aurora-drift"),
        new("frame-crimson-veil", "Crimson Veil", CosmeticKind.Frame, 3000, true, "A restrained crimson halo that swells like a heartbeat.", "crimson-heartbeat"),
        new("frame-starlight", "Starlight", CosmeticKind.Frame, 3600, true, "Silver-blue light slowly circles the frame.", "starlight-orbit"),
        new("frame-royal-violet", "Royal Violet", CosmeticKind.Frame, 4200, true, "A violet-gold gradient with a slow regal shimmer.", "royal-violet"),
        new("frame-obsidian-pulse", "Obsidian Pulse", CosmeticKind.Frame, 5200, true, "Nearly black glass with a razor-thin cyan pulse.", "obsidian-pulse"),

        // Shop portraits. These are original vector-style profile images rendered by the desktop client.
        new("portrait-night-dealer", "Night Dealer", CosmeticKind.Portrait, 700, true, "A sharp-suited dealer beneath a single table light.", "portrait-night-dealer"),
        new("portrait-card-shark", "Card Shark", CosmeticKind.Portrait, 900, true, "A cool silhouette framed by two aces.", "portrait-card-shark"),
        new("portrait-moon-cat", "Moon Cat", CosmeticKind.Portrait, 1100, true, "A casino cat who absolutely knows the count.", "portrait-moon-cat"),
        new("portrait-neon-ace", "Neon Ace", CosmeticKind.Portrait, 1400, true, "An ace-shaped neon portrait in cyan and magenta.", "portrait-neon-ace"),
        new("portrait-lucky-raven", "Lucky Raven", CosmeticKind.Portrait, 1700, true, "A black raven perched above a gold chip.", "portrait-lucky-raven"),
        new("portrait-midnight-lady", "Midnight Lady", CosmeticKind.Portrait, 2300, true, "A moonlit portrait with a quiet Manor influence.", "portrait-midnight-lady"),
        new("portrait-golden-joker", "Golden Joker", CosmeticKind.Portrait, 2800, true, "A theatrical gold mask for players who enjoy variance.", "portrait-golden-joker"),
        new("portrait-velvet-king", "Velvet King", CosmeticKind.Portrait, 3400, true, "A burgundy royal portrait with old-casino attitude.", "portrait-velvet-king"),

        // Prestige rewards.
        new("title-prestige-1", "The Regular", CosmeticKind.Title, 0, false, "Prestige I reward."),
        new("frame-prestige-2", "Prestige Bronze", CosmeticKind.Frame, 0, false, "Prestige II reward.", "prestige-bronze"),
        new("title-prestige-3", "Card Shark", CosmeticKind.Title, 0, false, "Prestige III reward."),
        new("frame-prestige-5", "Prestige Silver", CosmeticKind.Frame, 0, false, "Prestige V reward.", "prestige-silver"),
        new("frame-prestige-10", "Prestige Gold", CosmeticKind.Frame, 0, false, "Prestige X reward.", "prestige-gold"),
        new("title-prestige-25", "Blackjack Veteran", CosmeticKind.Title, 0, false, "Prestige XXV reward."),

        // Season 1 — Midnight Shift. Kept permanently for existing saves.
        new("title-season-midnight-5", "Night Shift", CosmeticKind.Title, 0, false, "Midnight Shift tier 5 reward."),
        new("frame-season-midnight-10", "Midnight Line", CosmeticKind.Frame, 0, false, "Midnight Shift tier 10 reward.", "midnight-line"),
        new("title-season-midnight-20", "After Midnight", CosmeticKind.Title, 0, false, "Midnight Shift tier 20 reward."),
        new("frame-season-midnight-35", "Neon Noir", CosmeticKind.Frame, 0, false, "Midnight Shift tier 35 reward.", "neon-noir"),
        new("title-season-midnight-50", "Midnight Legend", CosmeticKind.Title, 0, false, "Midnight Shift tier 50 reward."),

        // Season 2 — Velvet Eclipse.
        new("portrait-eclipse-raven", "Eclipse Raven", CosmeticKind.Portrait, 0, false, "Velvet Eclipse tier 15 portrait.", "portrait-eclipse-raven"),
        new("frame-eclipse-thread", "Eclipse Thread", CosmeticKind.Frame, 0, false, "Velvet Eclipse tier 25 frame.", "eclipse-thread"),
        new("title-eclipse-afterglow", "Afterglow", CosmeticKind.Title, 0, false, "Velvet Eclipse tier 35 title."),
        new("portrait-eclipse-lady", "Lady in Eclipse", CosmeticKind.Portrait, 0, false, "Velvet Eclipse tier 45 portrait.", "portrait-eclipse-lady"),
        new("frame-eclipse-halo", "Eclipse Halo", CosmeticKind.Frame, 0, false, "Velvet Eclipse tier 55 frame.", "eclipse-halo"),
        new("title-eclipse-architect", "Night Architect", CosmeticKind.Title, 0, false, "Velvet Eclipse tier 65 title."),
        new("portrait-eclipse-moon", "Moonbound", CosmeticKind.Portrait, 0, false, "Velvet Eclipse tier 75 portrait.", "portrait-eclipse-moon"),
        new("frame-eclipse-crown", "Eclipse Crown", CosmeticKind.Frame, 0, false, "Velvet Eclipse tier 85 frame.", "eclipse-crown"),
        new("title-eclipse-velvet-hour", "The Velvet Hour", CosmeticKind.Title, 0, false, "Velvet Eclipse tier 95 title."),
        new("portrait-eclipse-sovereign", "Eclipse Sovereign", CosmeticKind.Portrait, 0, false, "Velvet Eclipse tier 100 portrait.", "portrait-eclipse-sovereign"),
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
