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

    private static readonly string[] AccentPalette =
    [
        "#D8B46C", "#7FB59C", "#73ADCE", "#B396D6", "#D98080", "#91B66E", "#D89A63", "#72A9A5",
    ];

    public static IReadOnlyList<MemoryDeckDefinition> Decks { get; } = Array.AsReadOnly(new[]
    {
        Deck("casino-icons", "Casino Icons", "Chips, tables, lucky marks and late-night casino symbols.",
        ["♠","♥","♦","♣","★","☆","7","77","777","BAR","VIP","$","¢","◆","◇","♛","♚","⚄","⚂","⚀","21","A","K","Q","J","10","9","8","6","5","3","2"]),
        Deck("card-suits", "Card Suits", "A full lounge deck built from ranks and the four classic suits.",
        ["A♠","K♠","Q♠","J♠","10♠","9♠","8♠","7♠","A♥","K♥","Q♥","J♥","10♥","9♥","8♥","7♥","A♦","K♦","Q♦","J♦","10♦","9♦","8♦","7♦","A♣","K♣","Q♣","J♣","10♣","9♣","8♣","7♣"]),
        Deck("midnight-neon", "Midnight Neon", "Simple luminous signs that stay readable even on the largest board.",
        ["MOON","STAR","NITE","OPEN","CLUB","JAZZ","BLUE","PINK","GLOW","SIGN","CITY","LATE","LUX","BAR","LIVE","VIP","24H","NOVA","WAVE","BEAT","ECHO","DUSK","AURA","VIBE","NEON","NIGHT","MID","SKY","RAY","ARC","ZEN","LOOP"]),
        Deck("animals", "Animals", "A friendly collection of animals for relaxed rounds.",
        ["🐈","🐕","🐇","🦊","🐻","🐼","🐨","🐯","🦁","🐮","🐷","🐸","🐵","🐔","🐧","🐦","🦉","🦆","🦋","🐝","🐞","🐢","🐍","🦎","🐙","🐬","🐳","🦈","🐠","🦀","🐌","🐿"]),
        Deck("fruits", "Fruits & Treats", "Bright fruit and café favourites from a quiet lounge table.",
        ["🍎","🍐","🍊","🍋","🍌","🍉","🍇","🍓","🫐","🍈","🍒","🍑","🥭","🍍","🥝","🍅","🥥","🥐","🥨","🧀","🍞","🧁","🍰","🍪","🍫","🍬","☕","🫖","🥤","🍯","🥜","🍿"]),
        Deck("space", "Night Sky", "Planets, stars and navigation marks for the Midnight Club observatory crowd.",
        ["☀","☾","★","✦","✧","☄","⊙","◉","◎","◌","◍","◐","◑","◒","◓","N","S","E","W","NE","NW","SE","SW","∞","∆","Ω","λ","π","Σ","Φ","ψ","°"]),
        Deck("ocean", "Ocean", "Calm sea life, shells and navigation symbols.",
        ["🐚","🐙","🦑","🦀","🦞","🦐","🐠","🐟","🐡","🦈","🐬","🐳","🐋","🦭","🐢","🪼","⚓","⛵","🚤","🌊","💧","🫧","☀","☁","N","S","E","W","◈","◇","○","≈"]),
        Deck("gemstones", "Gemstones", "Geometric jewels and polished lounge ornaments.",
        ["◆","◇","◈","◊","⬟","⬢","⬡","▲","△","▼","▽","◀","◁","▶","▷","■","□","▣","▤","●","○","◉","◎","✦","✧","★","☆","✤","✥","✱","✲","✺"]),
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

    private static MemoryDeckDefinition Deck(string id, string name, string description, IReadOnlyList<string> glyphs)
    {
        MemorySymbol[] symbols = glyphs.Select((glyph, index) => new MemorySymbol(
            $"{id}-{index + 1:00}", glyph, $"{name} {index + 1}", AccentPalette[index % AccentPalette.Length])).ToArray();
        return new(id, name, description, Array.AsReadOnly(symbols));
    }
}
