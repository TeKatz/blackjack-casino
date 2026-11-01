namespace BlackjackCasino.Core.Collections;

public enum CollectionRarity { Common, Uncommon, Rare, Epic, Legendary }

public sealed record CollectionCard(string Id, string ThemeId, string Name, int Number, CollectionRarity Rarity);
public sealed record CollectionTheme(string Id, string Name, string Symbol, string Accent, bool UltraRare, IReadOnlyList<CollectionCard> Cards);
public sealed record CollectionPack(CollectionRarity Rarity, long Price, IReadOnlyList<int> Weights)
{
    // One of the five slots meets this floor; the other four still use the listed odds.
    public CollectionRarity GuaranteedRarity => Rarity;
    public double UltraRareChance => Rarity switch
    {
        CollectionRarity.Common => .0025,
        CollectionRarity.Uncommon => .005,
        CollectionRarity.Rare => .01,
        CollectionRarity.Epic => .02,
        _ => .03,
    };
}

public static class CollectionCatalog
{
    public const int CardsPerPack = 5;
    public const int DuplicatePackCost = 500;
    public const int Version = 1;

    public static IReadOnlyList<CollectionTheme> Themes { get; } = BuildThemes();
    public static IReadOnlyList<CollectionCard> Cards { get; } = Array.AsReadOnly(Themes.SelectMany(t => t.Cards).ToArray());
    private static readonly Dictionary<string, CollectionCard> CardsById = Cards.ToDictionary(c => c.Id, StringComparer.Ordinal);
    public static IReadOnlyList<CollectionPack> Packs { get; } = Array.AsReadOnly(new CollectionPack[]
    {
        new(CollectionRarity.Common, 100, Array.AsReadOnly(new[] { 620, 250, 90, 35, 5 })),
        new(CollectionRarity.Uncommon, 200, Array.AsReadOnly(new[] { 350, 420, 170, 50, 10 })),
        new(CollectionRarity.Rare, 400, Array.AsReadOnly(new[] { 200, 340, 340, 100, 20 })),
        new(CollectionRarity.Epic, 800, Array.AsReadOnly(new[] { 100, 250, 390, 210, 50 })),
        new(CollectionRarity.Legendary, 1600, Array.AsReadOnly(new[] { 50, 180, 350, 300, 120 })),
    });

    public static CollectionCard? Find(string id) => CardsById.GetValueOrDefault(id);
    public static CollectionTheme Theme(CollectionCard card) => Themes.First(t => t.Id == card.ThemeId);
    public static CollectionPack Pack(CollectionRarity rarity) => Packs.First(p => p.Rarity == rarity);
    public static int DuplicatePoints(CollectionRarity rarity) => rarity switch
    {
        CollectionRarity.Common => 5,
        CollectionRarity.Uncommon => 10,
        CollectionRarity.Rare => 25,
        CollectionRarity.Epic => 75,
        CollectionRarity.Legendary => 200,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity)),
    };

    public static CollectionRarity? SeasonPack(int tier) => tier switch
    {
        100 => CollectionRarity.Legendary,
        > 0 when tier % 25 == 0 => CollectionRarity.Epic,
        > 0 when tier % 10 == 0 => CollectionRarity.Rare,
        > 0 when tier % 5 == 0 => CollectionRarity.Common,
        _ => null,
    };

    private static IReadOnlyList<CollectionTheme> BuildThemes()
    {
        (string Id, string Name, string Symbol, string Accent, string[] Names)[] regular =
        [
            ("velvet-table", "Velvet Table", "♠", "#69CDAE", ["First Deal", "Green Felt", "Lucky Chip", "Cut Card", "Double Down", "Perfect Pair", "Split Decision", "Twenty-One", "Midnight Dealer", "The House Edge"]),
            ("neon-city", "Neon City", "◇", "#45D8F5", ["Rainy Alley", "Night Cab", "Streetlight", "Arcade Token", "Neon Sign", "Rooftop Bar", "Electric Avenue", "Skyline", "Last Train", "City of Lights"]),
            ("midnight-manor", "Midnight Manor", "☽", "#B394E8", ["Iron Gate", "Dusty Key", "Candlelight", "Portrait Hall", "Velvet Curtain", "Music Box", "Secret Stair", "Ghost Waltz", "The Host", "Midnight Heir"]),
            ("ocean-depths", "Ocean Depths", "≈", "#539BE4", ["Sea Glass", "Coral Branch", "Pearl Shell", "Driftwood", "Jellyfish", "Sea Turtle", "Manta Ray", "Sunken Temple", "Leviathan", "Heart of the Ocean"]),
            ("enchanted-forest", "Enchanted Forest", "♣", "#8AC878", ["Moss Stone", "Fern Frond", "Firefly", "Wild Mushroom", "Silver Fox", "Ancient Oak", "Moon Stag", "Hidden Grove", "Forest Guardian", "World Tree"]),
            ("desert-gold", "Desert Gold", "☀", "#E4B965", ["Sand Grain", "Clay Lantern", "Date Palm", "Desert Rose", "Caravan", "Oasis", "Golden Dune", "Lost Pyramid", "Sun Pharaoh", "Eternal Scarab"]),
            ("celestial-voyage", "Celestial Voyage", "✦", "#8C9EF4", ["Stardust", "Moon Rock", "Satellite", "Star Map", "Comet Tail", "Ringed Planet", "Solar Sail", "Nebula", "Cosmic Dragon", "First Light"]),
            ("frost-kingdom", "Frost Kingdom", "❄", "#9EDDE8", ["Snowflake", "Frozen Leaf", "Ice Crystal", "Winter Lantern", "Arctic Fox", "Snow Owl", "Glacier Gate", "Aurora", "Frost Queen", "Crown of Winter"]),
            ("clockwork", "Clockwork Wonders", "⚙", "#D5A177", ["Brass Gear", "Copper Wire", "Pocket Watch", "Steam Valve", "Automaton", "Clockwork Bird", "Airship", "Time Engine", "Master Inventor", "Perpetual Heart"]),
            ("sakura-garden", "Sakura Garden", "❀", "#E8A0BD", ["Fallen Petal", "Tea Cup", "Paper Fan", "Stone Lantern", "Koi Pond", "Red Bridge", "Crane Dance", "Moon Pavilion", "Sakura Spirit", "Thousand Blossoms"]),
            ("pirate-cove", "Pirate Cove", "⚓", "#6BB9BF", ["Old Rope", "Silver Coin", "Torn Map", "Compass", "Parrot", "Captain's Hat", "Black Sail", "Treasure Isle", "Ghost Captain", "Crown of the Sea"]),
            ("royal-court", "Royal Court", "♛", "#D29CDA", ["Wax Seal", "Silver Spoon", "Court Mask", "Silk Ribbon", "Royal Knight", "Rose Throne", "Crown Jewels", "Grand Ball", "The Sovereign", "Royal Flush"]),
        ];
        (string Id, string Name, string Symbol, string Accent, string[] Names)[] chase =
        [
            ("astral-sovereigns", "Astral Sovereigns", "✧", "#EAC7FF", ["Dawn Empress", "Eclipse King", "Star Weaver", "Infinity Throne"]),
            ("primordial-dragons", "Primordial Dragons", "◆", "#FF997D", ["Ember Origin", "Tidal Ancestor", "Storm Eternal", "Void Wyrm"]),
            ("mythic-relics", "Mythic Relics", "⚜", "#FFE39C", ["Chalice of Ages", "Blade of Dawn", "Oracle Mirror", "Fate's Compass"]),
            ("eternal-casino", "Eternal Casino", "♜", "#BDFFE4", ["The First Chip", "Fortune's Hand", "Timeless Dealer", "The Infinite Vault"]),
        ];
        var result = new List<CollectionTheme>();
        foreach (var theme in regular.Concat(chase))
        {
            bool ultra = result.Count >= regular.Length;
            CollectionCard[] cards = theme.Names.Select((name, i) => new CollectionCard($"{theme.Id}-{i + 1:00}", theme.Id, name, i + 1,
                ultra ? CollectionRarity.Legendary : i switch
                {
                    < 4 => CollectionRarity.Common,
                    < 7 => CollectionRarity.Uncommon,
                    7 => CollectionRarity.Rare,
                    8 => CollectionRarity.Epic,
                    _ => CollectionRarity.Legendary,
                })).ToArray();
            result.Add(new(theme.Id, theme.Name, theme.Symbol, theme.Accent, ultra, Array.AsReadOnly(cards)));
        }
        return result.AsReadOnly();
    }
}
