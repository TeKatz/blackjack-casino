namespace BlackjackCasino.Core;

public static class CosmeticCatalog
{
    public const string DefaultTitleId = "title-rookie";
    public const string DefaultFrameId = "frame-standard";
    public const string DefaultPortraitId = "portrait-classic";
    public const string DefaultCardSetId = "cardset-classic";

    public const string DefaultDiceSetId = "diceset-ivory";

    private static readonly IReadOnlyList<CosmeticDefinition> Cosmetics =
    [
        new(DefaultTitleId, "Rookie", CosmeticKind.Title, 0, false, "The default title."),
        new(DefaultFrameId, "Classic", CosmeticKind.Frame, 0, false, "The clean default profile frame.", "classic"),
        new(DefaultPortraitId, "House Guest", CosmeticKind.Portrait, 0, false, "The default casino portrait.", "portrait-house-guest"),
        new(DefaultCardSetId, "Classic Cards", CosmeticKind.CardSet, 0, false, "The familiar clean casino deck.", "card-classic"),

        new(DefaultDiceSetId, "House Ivory", CosmeticKind.DiceSet, 0, false, "The original Midnight Dice. Appearance only.", "dice-ivory"),

        // Premium colour titles and quiet, unanimated standard frames.
        new("title-velvet-sovereign", "Velvet Sovereign", CosmeticKind.Title, 6500, true, "A royal violet signature.", "title-violet"),
        new("title-golden-hour", "Golden Hour", CosmeticKind.Title, 7200, true, "A warm gold signature.", "title-gold"),
        new("title-aurora-dreamer", "Aurora Dreamer", CosmeticKind.Title, 8000, true, "An aurora teal signature.", "title-teal"),
        new("title-rose-noir", "Rose Noir", CosmeticKind.Title, 8500, true, "A rose pink signature.", "title-rose"),
        new("title-moonlit-legend", "Moonlit Legend", CosmeticKind.Title, 10000, true, "An ice blue signature.", "title-ice"),
        new("title-ember-crown", "Ember Crown", CosmeticKind.Title, 12000, true, "A glowing amber signature.", "title-amber"),
        new("frame-slate", "Slate Outline", CosmeticKind.Frame, 350, true, "A simple matte slate outline.", "static-slate"),
        new("frame-copper", "Copper Outline", CosmeticKind.Frame, 450, true, "A quiet brushed copper outline.", "static-copper"),
        new("frame-sage", "Sage Outline", CosmeticKind.Frame, 500, true, "A soft sage border without animation.", "static-sage"),
        new("frame-pearl", "Pearl Outline", CosmeticKind.Frame, 650, true, "A clean pearl-white outline.", "static-pearl"),
        new("diceset-obsidian", "Obsidian Gold", CosmeticKind.DiceSet, 2400, true, "Black glass and gold pips. Appearance only.", "dice-obsidian"),
        new("diceset-jade", "Jade Palace", CosmeticKind.DiceSet, 3200, true, "Polished jade and ivory pips. Appearance only.", "dice-jade"),
        new("diceset-violet", "Royal Amethyst", CosmeticKind.DiceSet, 4200, true, "Violet crystal and pale gold pips. Appearance only.", "dice-violet"),
        new("diceset-ice", "Moonstone", CosmeticKind.DiceSet, 5200, true, "Moonlit blue and silver pips. Appearance only.", "dice-ice"),
        new("diceset-ruby", "Ruby Velvet", CosmeticKind.DiceSet, 6200, true, "Deep ruby and cream pips. Appearance only.", "dice-ruby"),
        new("diceset-aurora", "Aurora Prism", CosmeticKind.DiceSet, 8000, true, "Teal and violet with luminous pips. Appearance only.", "dice-aurora"),

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
        new("portrait-card-shark", "Card Shark", CosmeticKind.Portrait, 900, true, "A silver-framed shark in a tailored petrol suit.", "portrait-card-shark"),
        new("portrait-moon-cat", "Moon Cat", CosmeticKind.Portrait, 1100, true, "A casino cat who absolutely knows the count.", "portrait-moon-cat"),
        new("portrait-neon-ace", "Neon Ace", CosmeticKind.Portrait, 1400, true, "A neon card virtuoso in cyan, magenta and black chrome.", "portrait-neon-ace"),
        new("portrait-lucky-raven", "Lucky Raven", CosmeticKind.Portrait, 1700, true, "An emerald-eyed raven adorned with gold and a lucky clover.", "portrait-lucky-raven"),
        new("portrait-midnight-lady", "Midnight Lady", CosmeticKind.Portrait, 2300, true, "A moonlit portrait with a quiet Manor influence.", "portrait-midnight-lady"),
        new("portrait-golden-joker", "Golden Joker", CosmeticKind.Portrait, 2800, true, "A theatrical gold mask for players who enjoy variance.", "portrait-golden-joker"),
        new("portrait-velvet-king", "Velvet King", CosmeticKind.Portrait, 3400, true, "An imperial velvet king crowned in gold, rubies and amethyst.", "portrait-velvet-king"),

        // Playing-card cosmetics. Classic remains free; the first premium collection requires Player Prestige I.
        new("cardset-art-deco", "Art Deco", CosmeticKind.CardSet, 1800, true, "Ivory faces, black geometry and restrained brass trim.", "card-art-deco"),
        new("cardset-crimson-velvet", "Crimson Velvet", CosmeticKind.CardSet, 2600, true, "Warm cream cards with deep velvet-red accents and silver edging.", "card-crimson-velvet"),
        new("cardset-emerald-house", "Emerald House", CosmeticKind.CardSet, 3400, true, "House green, warm gold and a classic private-table finish.", "card-emerald-house"),
        new("cardset-midnight-blue", "Midnight Blue", CosmeticKind.CardSet, 4600, true, "Cool moonlit faces with navy backs and pale silver details.", "card-midnight-blue"),
        new("cardset-midnight-sovereign", "Midnight Sovereign", CosmeticKind.CardSet, 12500, true, "Prestige collection: obsidian faces, antique gold trim and a royal Midnight Club back.", "card-midnight-sovereign", 1),

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
