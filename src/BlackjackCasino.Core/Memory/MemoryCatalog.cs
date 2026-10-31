namespace BlackjackCasino.Core.Memory;

public static class MemoryCatalog
{
    public const string DefaultDeckId = "casino-icons";

    private static readonly IReadOnlyDictionary<MemoryDifficulty, MemoryDifficultySpec> Specs =
        new Dictionary<MemoryDifficulty, MemoryDifficultySpec>
        {
            [MemoryDifficulty.Easy] = new(4, 4, 8),
            [MemoryDifficulty.Normal] = new(4, 5, 10),
            [MemoryDifficulty.Hard] = new(6, 6, 18),
            [MemoryDifficulty.Expert] = new(6, 8, 24),
            [MemoryDifficulty.Master] = new(8, 8, 32),
        };

    public static IReadOnlyList<MemoryDeckDefinition> Decks { get; } = Array.AsReadOnly(new[]
    {
        CasinoIcons(),
        CardSuits(),
        Animals(),
        FruitsAndTreats(),
    });

    static MemoryCatalog()
    {
        if (Decks.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != Decks.Count
            || Decks.Any(x => x.Symbols.Count < Specs[MemoryDifficulty.Master].Pairs
                || x.Symbols.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != x.Symbols.Count))
            throw new InvalidOperationException("Memory deck catalog contains duplicate or incomplete content.");
    }

    public static MemoryDifficultySpec Spec(MemoryDifficulty difficulty) =>
        Specs.TryGetValue(difficulty, out MemoryDifficultySpec? spec)
            ? spec
            : throw new ArgumentOutOfRangeException(nameof(difficulty));

    public static MemoryDeckDefinition GetDeck(string id) =>
        TryGetDeck(id, out MemoryDeckDefinition? deck) && deck is not null
            ? deck
            : throw new ArgumentOutOfRangeException(nameof(id));

    public static bool TryGetDeck(string? id, out MemoryDeckDefinition? deck)
    {
        deck = Decks.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal));
        return deck is not null;
    }

    public static MemorySymbol Symbol(string deckId, string symbolId)
    {
        MemoryDeckDefinition deck = GetDeck(deckId);
        return deck.Symbols.FirstOrDefault(x => string.Equals(x.Id, symbolId, StringComparison.Ordinal))
            ?? throw new ArgumentOutOfRangeException(nameof(symbolId));
    }

    private static MemoryDeckDefinition CasinoIcons() => Deck(
        "casino-icons",
        "Casino Icons",
        "A premium Midnight Club set: table classics, lucky emblems and high-roller details.",
        [
            ("chip", "Casino Chip", "#D7B665"),
            ("dice", "Lucky Dice", "#E7DDC7"),
            ("roulette", "Roulette Wheel", "#C96A68"),
            ("seven", "Lucky Seven", "#E0BD62"),
            ("bell", "Jackpot Bell", "#E3B75D"),
            ("vault", "Gold Vault", "#CDA85F"),
            ("crown", "High Roller Crown", "#E2C26D"),
            ("cocktail", "Midnight Cocktail", "#C98AB6"),
            ("ace", "Ace High", "#E9E1CF"),
            ("horseshoe", "Lucky Horseshoe", "#D7B665"),
            ("star", "Jackpot Star", "#E5C66A"),
            ("diamond", "House Diamond", "#79B9C7"),
            ("slot", "Slot Reels", "#D78670"),
            ("cards", "Card Fan", "#EFE5CF"),
            ("cash", "Cash Stack", "#7CB394"),
            ("key", "Vault Key", "#D8B66A"),
            ("bar", "BAR", "#D7A55E"),
            ("vip", "VIP Pass", "#B79AD7"),
            ("gold", "Gold Bar", "#E0B85D"),
            ("token", "House Token", "#89AF9E"),
            ("wheel", "Prize Wheel", "#8CB2C9"),
            ("dealer", "Dealer Button", "#E9DFC9"),
            ("table", "Green Table", "#72A487"),
            ("moon", "Midnight Moon", "#93A8D0"),
            ("club", "Club Emblem", "#D0B66F"),
            ("spin", "Spin Marker", "#74B5B0"),
            ("king", "King's Mark", "#D8B465"),
            ("queen", "Queen's Mark", "#CF94B0"),
            ("jack", "Jack's Mark", "#8CA7C8"),
            ("twentyone", "Blackjack 21", "#E5DCC7"),
            ("clover", "Lucky Clover", "#76AA83"),
            ("midnight", "Midnight Club", "#A891D0"),
        ]);

    private static MemoryDeckDefinition CardSuits()
    {
        string[] ranks = ["A", "K", "Q", "J", "10", "9", "8", "7"];
        (string Suit, string Name, string Accent)[] suits =
        [
            ("♠", "Spades", "#27312F"),
            ("♥", "Hearts", "#B84F55"),
            ("♦", "Diamonds", "#C65756"),
            ("♣", "Clubs", "#27312F"),
        ];
        var entries = new List<(string Glyph, string Name, string Accent)>(32);
        foreach ((string suit, string suitName, string accent) in suits)
        {
            foreach (string rank in ranks)
            {
                string rankName = rank switch
                {
                    "A" => "Ace",
                    "K" => "King",
                    "Q" => "Queen",
                    "J" => "Jack",
                    _ => rank,
                };
                entries.Add(($"{rank}{suit}", $"{rankName} of {suitName}", accent));
            }
        }
        return Deck(
            "card-suits",
            "Card Suits",
            "A refined playing-card set with ivory faces, classic suits and Art Deco detailing.",
            entries);
    }

    private static MemoryDeckDefinition Animals() => Deck(
        "animals",
        "Animals",
        "Cozy illustrated animal medallions with a calm woodland palette.",
        [
            ("🐈", "Cat", "#C98E73"), ("🐕", "Dog", "#B58A64"), ("🐇", "Rabbit", "#D1B7A4"), ("🦊", "Fox", "#D17A4C"),
            ("🐻", "Bear", "#9B765B"), ("🐼", "Panda", "#D7D4CA"), ("🐨", "Koala", "#9DA7A3"), ("🐯", "Tiger", "#D4934F"),
            ("🦁", "Lion", "#C99C58"), ("🐮", "Cow", "#D7D0BF"), ("🐷", "Pig", "#D9A6A9"), ("🐸", "Frog", "#75A66F"),
            ("🐵", "Monkey", "#B48765"), ("🐔", "Chicken", "#D8C9A8"), ("🐧", "Penguin", "#9AAAB0"), ("🐦", "Songbird", "#77A6BA"),
            ("🦉", "Owl", "#9A8060"), ("🦆", "Duck", "#8AAA79"), ("🦋", "Butterfly", "#A98AC4"), ("🐝", "Bee", "#D6B457"),
            ("🐞", "Ladybird", "#C85C57"), ("🐢", "Turtle", "#6E9A7B"), ("🐍", "Snake", "#71936D"), ("🦎", "Lizard", "#719C85"),
            ("🐙", "Octopus", "#B57E9C"), ("🐬", "Dolphin", "#76A9C0"), ("🐳", "Whale", "#6E91AD"), ("🦈", "Shark", "#82959E"),
            ("🐠", "Tropical Fish", "#D79468"), ("🦀", "Crab", "#C96C5F"), ("🐌", "Snail", "#AE9270"), ("🐿", "Squirrel", "#A97B55"),
        ]);

    private static MemoryDeckDefinition FruitsAndTreats() => Deck(
        "fruits",
        "Fruits & Treats",
        "Bright fruit and café favourites presented like treats from a late-night dessert cart.",
        [
            ("🍎", "Apple", "#C85D57"), ("🍐", "Pear", "#9BB564"), ("🍊", "Orange", "#D9934D"), ("🍋", "Lemon", "#D8C458"),
            ("🍌", "Banana", "#D9C55E"), ("🍉", "Watermelon", "#C86868"), ("🍇", "Grapes", "#9B7DB4"), ("🍓", "Strawberry", "#D45E68"),
            ("🫐", "Blueberries", "#7189B5"), ("🍈", "Melon", "#9CB378"), ("🍒", "Cherries", "#B74E59"), ("🍑", "Peach", "#D99A7E"),
            ("🥭", "Mango", "#D9A651"), ("🍍", "Pineapple", "#C8AA55"), ("🥝", "Kiwi", "#88A665"), ("🍅", "Tomato", "#C95C52"),
            ("🥥", "Coconut", "#B69B79"), ("🥐", "Croissant", "#D6A768"), ("🥨", "Pretzel", "#B68455"), ("🧀", "Cheese", "#D7B85C"),
            ("🍞", "Toast", "#C59A6B"), ("🧁", "Cupcake", "#C88DA8"), ("🍰", "Cake", "#D4A0A2"), ("🍪", "Cookie", "#B98A5D"),
            ("🍫", "Chocolate", "#8E6B59"), ("🍬", "Candy", "#C985A9"), ("☕", "Coffee", "#9B7458"), ("🫖", "Tea", "#8CA88E"),
            ("🥤", "Milkshake", "#B997AC"), ("🍯", "Honey", "#D3A84F"), ("🥜", "Peanuts", "#B9936A"), ("🍿", "Popcorn", "#D9C37A"),
        ]);

    private static MemoryDeckDefinition Deck(
        string id,
        string name,
        string description,
        IReadOnlyList<(string Glyph, string Name, string Accent)> entries)
    {
        MemorySymbol[] symbols = entries.Select((entry, index) => new MemorySymbol(
            $"{id}-{index + 1:00}", entry.Glyph, entry.Name, entry.Accent)).ToArray();
        return new(id, name, description, Array.AsReadOnly(symbols));
    }
}
