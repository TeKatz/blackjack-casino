namespace BlackjackCasino.Core.WordSearch;

public sealed record WordSearchPuzzleDefinition(
    string Id,
    WordSearchDifficulty Difficulty,
    string Theme,
    int Size,
    int WordCount,
    int Seed,
    IReadOnlyList<string> WordBank);

public static class WordSearchCatalog
{
    private sealed record ThemeWords(string Theme, string[] Words);

    private static readonly ThemeWords[] Themes =
    [
        new("Midnight Club", ["CASINO", "VELVET", "LOUNGE", "TABLE", "CHIPS", "DEALER", "CARDS", "JAZZ", "GOLD", "NIGHT", "LIGHTS", "BAR", "STAGE", "SUITE", "LUCK", "MUSIC", "GLASS", "DICE"]),
        new("Quiet Morning", ["COFFEE", "TEA", "SUNRISE", "WINDOW", "CALM", "PAPER", "MUG", "TOAST", "LIGHT", "BOOK", "CHAIR", "BREEZE", "PORCH", "HONEY", "CLOCK", "PLANT", "SILENCE", "CLOUD"]),
        new("City Lights", ["CITY", "TOWER", "NEON", "STREET", "TRAIN", "BRIDGE", "TAXI", "PLAZA", "HOTEL", "SIGN", "ROOFTOP", "METRO", "PARK", "NIGHT", "BLOCK", "LIGHT", "CAFE", "RIVER"]),
        new("Book Nook", ["NOVEL", "PAGES", "STORY", "POEM", "AUTHOR", "SHELF", "PLOT", "CHAPTER", "INK", "PAPER", "LIBRARY", "COVER", "PROSE", "TALE", "READER", "VERSE", "BOOK", "SPINE"]),
        new("Jazz Night", ["JAZZ", "PIANO", "BASS", "DRUMS", "TRUMPET", "SAX", "RHYTHM", "SWING", "SOLO", "CLUB", "STAGE", "NOTE", "MELODY", "BAND", "BLUES", "BRASS", "TEMPO", "SONG"]),
        new("Ocean Air", ["OCEAN", "WAVES", "SHELL", "CORAL", "TIDE", "BEACH", "SAND", "ISLAND", "REEF", "BOAT", "SAIL", "BREEZE", "FOAM", "COVE", "HARBOR", "GULL", "DUNE", "COAST"]),
        new("Forest Walk", ["FOREST", "MOSS", "FERN", "TRAIL", "PINE", "OAK", "LEAVES", "CREEK", "STONE", "DEER", "BIRDS", "SHADE", "ROOT", "GROVE", "BARK", "PATH", "RAIN", "MEADOW"]),
        new("Night Sky", ["STARS", "MOON", "COMET", "ORBIT", "SPACE", "NOVA", "PLANET", "GALAXY", "COSMOS", "METEOR", "ECLIPSE", "VENUS", "MARS", "SATURN", "LUNAR", "SOLAR", "SKY", "DUSK"]),
        new("Autumn", ["AUTUMN", "AMBER", "LEAVES", "ACORN", "MAPLE", "CIDER", "SCARF", "BREEZE", "HARVEST", "PUMPKIN", "CANDLE", "COPPER", "RAIN", "APPLE", "WOOL", "GOLD", "MIST", "PATH"]),
        new("Cinema", ["CINEMA", "MOVIE", "SCREEN", "ACTOR", "SCENE", "CAMERA", "TICKET", "FILM", "DRAMA", "COMEDY", "DIRECTOR", "LIGHTS", "SOUND", "FRAME", "STUDIO", "CREDITS", "ROLE", "SCRIPT"]),
        new("Road Trip", ["TRAVEL", "ROUTE", "ROAD", "MAP", "MOTEL", "RADIO", "BRIDGE", "TUNNEL", "VALLEY", "MILES", "FUEL", "PICNIC", "CAMERA", "COAST", "SIGN", "DRIVE", "SUNSET", "BAG"]),
        new("Kitchen", ["KITCHEN", "PLATE", "SPOON", "KNIFE", "BREAD", "PASTA", "HERBS", "OVEN", "PAN", "BOWL", "SUGAR", "CREAM", "LEMON", "HONEY", "SAUCE", "APPLE", "GARLIC", "TOAST"]),
    ];


    // Expansion pool: 12 additional bilingual themes, with two seeded layouts each.
    // Keep the original 24 definitions per difficulty intact (ID, seed, bank and grid)
    // so old saves and completion receipts remain byte-for-byte compatible.
    private static readonly ThemeWords[] ExpansionThemes =
    [
        new("Mountain Trails", ["MOUNTAIN", "SUMMIT", "RIDGE", "CLIFF", "TRAIL", "VALLEY", "PEAK", "STONE", "HIKER", "ROPE", "CAMP", "BOOTS", "MAP", "FROST", "EAGLE", "CLOUD", "PINE", "ASCENT", "SLOPE", "CREST", "PATH", "RIVER", "CABIN", "MIST", "LAKE", "WIND"]),
        new("Garden Paths", ["GARDEN", "PETAL", "BLOOM", "TULIP", "ROSE", "LILY", "DAISY", "SEED", "SOIL", "HEDGE", "POND", "HERB", "BASIL", "MINT", "BEES", "BUTTERFLY", "SPRING", "GLOVES", "WATER", "SHOVEL", "VINES", "ROOTS", "FLOWER", "BUD", "ORCHID", "THYME"]),
        new("Winter Cabin", ["WINTER", "SNOW", "FROST", "ICICLE", "CABIN", "HEARTH", "FIRE", "COCOA", "BLANKET", "SCARF", "MITTEN", "SLED", "PINE", "FLAKE", "GLOW", "NIGHT", "SOCKS", "WOOD", "STOVE", "TEA", "EMBER", "COZY", "COLD", "WINDOW", "CANDLE", "FROZEN"]),
        new("Desert Journey", ["DESERT", "DUNES", "OASIS", "CAMEL", "SAND", "SUN", "NOMAD", "CACTUS", "MIRAGE", "STONE", "LIZARD", "DUST", "TRAIL", "HEAT", "SHADE", "PALM", "WIND", "SCARF", "MAP", "GOLD", "NIGHT", "ROCK", "HORIZON", "TENT", "WATER", "CANYON"]),
        new("Wildlife", ["OTTER", "FOX", "WOLF", "BEAR", "DEER", "MOOSE", "LYNX", "BISON", "RAVEN", "EAGLE", "OWL", "FALCON", "WHALE", "SHARK", "SEAL", "TIGER", "ZEBRA", "KOALA", "PANDA", "LEMUR", "GECKO", "FROG", "NEWT", "BEE", "SWAN", "CRANE"]),
        new("Space Lab", ["ROCKET", "ASTRO", "ORBIT", "LUNAR", "COMET", "PROBE", "GALAXY", "SATURN", "VENUS", "MARS", "ENGINE", "MODULE", "DOCK", "RADAR", "SIGNAL", "SOLAR", "STARS", "PLASMA", "VACUUM", "ALIEN", "CRATER", "METEOR", "HELMET", "ROVER", "TELESCOPE", "THRUST"]),
        new("Ancient Ruins", ["RUINS", "TEMPLE", "COLUMN", "MARBLE", "SCROLL", "MOSAIC", "TOMB", "ARCH", "STONE", "CRYPT", "ALTAR", "RELIC", "STATUE", "TORCH", "GATE", "WALL", "DUST", "CROWN", "GLYPH", "GOLD", "VAULT", "MYTH", "KING", "QUEEN", "PALACE", "BRONZE"]),
        new("Night Market", ["MARKET", "STALL", "SPICE", "LANTERN", "STREET", "FRUIT", "MANGO", "DATES", "TEA", "BREAD", "BASKET", "COINS", "MUSIC", "FABRIC", "SILK", "CRAFT", "GLOW", "NIGHT", "SWEET", "LIGHT", "CROWD", "MERCHANT", "AROMA", "COLOR", "GIFT", "DANCE"]),
        new("Lakeside", ["LAKE", "RIVER", "STREAM", "SHORE", "CANOE", "PADDLE", "REEDS", "WILLOW", "HERON", "PIER", "DOCK", "FISH", "WATER", "WAVES", "ISLAND", "MIST", "BRIDGE", "BOAT", "LILY", "OTTER", "SAND", "MOSS", "POND", "DELTA", "COVE", "SPLASH"]),
        new("Workshop", ["WORKSHOP", "HAMMER", "NAILS", "SCREW", "WOOD", "METAL", "GLUE", "SAW", "DRILL", "GEARS", "BRUSH", "PAINT", "TOOLS", "BENCH", "CLAMP", "RULER", "LATHE", "FRAME", "WRENCH", "CRAFT", "SAND", "CHISEL", "SHELF", "BOLT", "COPPER", "HANDLE"]),
        new("Summer Festival", ["FESTIVAL", "DANCE", "MUSIC", "DRUM", "SONG", "LIGHTS", "RIBBON", "STAGE", "CROWD", "TICKET", "SUMMER", "FIREWORK", "PARADE", "CARNIVAL", "FLAGS", "FOOD", "SWEET", "BALLOON", "NIGHT", "LAUGH", "GAMES", "WHEEL", "PARTY", "GLOW", "JOY", "BAND"]),
        new("Mythic Tales", ["DRAGON", "PHOENIX", "GRIFFIN", "UNICORN", "GIANT", "FAIRY", "ELF", "DWARF", "QUEST", "MAGIC", "SPELL", "RUNE", "CASTLE", "FOREST", "SWORD", "SHIELD", "WIZARD", "KNIGHT", "CROWN", "GOBLIN", "POTION", "LEGEND", "TROLL", "ORACLE", "TEMPLE", "HERO"]),
    ];

    // The localized list changes only the authored puzzle vocabulary, never the stable
    // puzzle ID, difficulty, seed, completion receipt or serialized in-progress grid.
    // Existing active games therefore remain playable after a language switch.
    private static readonly IReadOnlyDictionary<string, ThemeWords> GermanThemes =
        new Dictionary<string, ThemeWords>(StringComparer.Ordinal)
        {
            ["Midnight Club"] = new("Mitternachtsclub", ["CASINO", "SAMT", "LOUNGE", "TISCH", "JETON", "GEBER", "KARTE", "JAZZ", "GOLD", "NACHT", "LICHT", "BAR", "BUEHNE", "SUITE", "GLUECK", "MUSIK", "GLAS", "WUERFEL"]),
            ["Quiet Morning"] = new("Stiller Morgen", ["KAFFEE", "TEE", "SONNE", "FENSTER", "RUHE", "PAPIER", "TASSE", "TOAST", "LICHT", "BUCH", "STUHL", "WIND", "BALKON", "HONIG", "UHR", "PFLANZE", "STILLE", "WOLKE"]),
            ["City Lights"] = new("Lichter der Stadt", ["STADT", "TURM", "NEON", "STRASSE", "ZUG", "BRUECKE", "TAXI", "PLATZ", "HOTEL", "SCHILD", "DACH", "METRO", "PARK", "NACHT", "BLOCK", "LICHT", "CAFE", "FLUSS"]),
            ["Book Nook"] = new("Leseecke", ["ROMAN", "SEITEN", "STORY", "GEDICHT", "AUTOR", "REGAL", "PLOT", "KAPITEL", "TINTE", "PAPIER", "BUCH", "EINBAND", "PROSA", "SAGE", "LESER", "VERS", "WORT", "LESEN"]),
            ["Jazz Night"] = new("Jazznacht", ["JAZZ", "PIANO", "BASS", "TROMMEL", "TROMPETE", "SAXOFON", "RHYTHMUS", "SWING", "SOLO", "CLUB", "BUEHNE", "NOTE", "MELODIE", "BAND", "BLUES", "BLECH", "TEMPO", "LIED"]),
            ["Ocean Air"] = new("Meeresluft", ["MEER", "WELLEN", "MUSCHEL", "KORALLE", "FLUT", "STRAND", "SAND", "INSEL", "RIFF", "BOOT", "SEGEL", "BRISE", "SCHAUM", "BUCHT", "HAFEN", "MOEWE", "DUENE", "KUESTE"]),
            ["Forest Walk"] = new("Waldspaziergang", ["WALD", "MOOS", "FARN", "PFAD", "KIEFER", "EICHE", "LAUB", "BACH", "STEIN", "HIRSCH", "VOEGEL", "SCHATTEN", "WURZEL", "HAIN", "RINDE", "WEG", "REGEN", "WIESE"]),
            ["Night Sky"] = new("Nachthimmel", ["STERNE", "MOND", "KOMET", "BAHN", "ALL", "NOVA", "PLANET", "GALAXIE", "KOSMOS", "METEOR", "FINSTERNIS", "VENUS", "MARS", "SATURN", "LUNAR", "SONNE", "HIMMEL", "DAEMMER"]),
            ["Autumn"] = new("Herbst", ["HERBST", "BERNSTEIN", "BLAETTER", "EICHEL", "AHORN", "MOST", "SCHAL", "BRISE", "ERNTE", "KUERBIS", "KERZE", "KUPFER", "REGEN", "APFEL", "WOLLE", "GOLD", "NEBEL", "PFAD"]),
            ["Cinema"] = new("Kino", ["KINO", "FILM", "LEINWAND", "MIME", "SZENE", "KAMERA", "TICKET", "DRAMA", "KOMOEDIE", "REGIE", "LICHT", "TON", "BILD", "STUDIO", "ABSPANN", "ROLLE", "SKRIPT", "POPCORN"]),
            ["Road Trip"] = new("Autoreise", ["REISE", "ROUTE", "STRASSE", "KARTE", "MOTEL", "RADIO", "BRUECKE", "TUNNEL", "TAL", "MEILEN", "BENZIN", "PICKNICK", "KAMERA", "KUESTE", "SCHILD", "FAHRT", "SONNE", "TASCHE"]),
            ["Kitchen"] = new("Kueche", ["KUECHE", "TELLER", "LOEFFEL", "MESSER", "BROT", "NUDEL", "KRAUT", "OFEN", "PFANNE", "SCHALE", "ZUCKER", "SAHNE", "ZITRONE", "HONIG", "SOSSE", "APFEL", "KNOBLAUCH", "TOAST"]),
            ["Mountain Trails"] = new("Bergpfade", ["BERG", "GIPFEL", "GRAT", "FELS", "PFAD", "TAL", "KAMM", "STEIN", "WANDERER", "SEIL", "ZELT", "STIEFEL", "KARTE", "FROST", "ADLER", "WOLKE", "KIEFER", "ANSTIEG", "HANG", "GEBIRGE", "WEG", "BACH", "HUETTE", "NEBEL", "SEE", "WIND"]),
            ["Garden Paths"] = new("Gartenwege", ["GARTEN", "BLUETE", "KNOSPE", "TULPE", "ROSE", "LILIE", "ASTER", "SAMEN", "ERDE", "HECKE", "TEICH", "KRAUT", "BASILIKUM", "MINZE", "BIENE", "FALTER", "FRUEHLING", "HANDSCHUH", "WASSER", "SPATEN", "RANKE", "WURZEL", "BLUME", "BLATT", "ORCHIDEE", "THYMIAN"]),
            ["Winter Cabin"] = new("Winterhuette", ["WINTER", "SCHNEE", "FROST", "EISZAPFEN", "HUETTE", "KAMIN", "FEUER", "KAKAO", "DECKE", "SCHAL", "FAEUSTLING", "SCHLITTEN", "KIEFER", "FLOCKE", "SCHEIN", "NACHT", "SOCKEN", "HOLZ", "OFEN", "TEE", "GLUT", "WARM", "KALT", "FENSTER", "KERZE", "EIS"]),
            ["Desert Journey"] = new("Wuestenreise", ["WUESTE", "DUENE", "OASE", "KAMEL", "SAND", "SONNE", "NOMADE", "KAKTUS", "FATA", "STEIN", "ECHSE", "STAUB", "PFAD", "HITZE", "SCHATTEN", "PALME", "WIND", "TUCH", "KARTE", "GOLD", "NACHT", "FELS", "FERNE", "ZELT", "WASSER", "SCHLUCHT"]),
            ["Wildlife"] = new("Wildtiere", ["OTTER", "FUCHS", "WOLF", "BAER", "HIRSCH", "ELCH", "LUCHS", "BISON", "RABE", "ADLER", "EULE", "FALKE", "WAL", "HAI", "ROBBE", "TIGER", "ZEBRA", "KOALA", "PANDA", "LEMUR", "GECKO", "FROSCH", "MOLCH", "BIENE", "SCHWAN", "KRANICH"]),
            ["Space Lab"] = new("Weltraumlabor", ["RAKETE", "ASTRO", "ORBIT", "MOND", "KOMET", "SONDE", "GALAXIE", "SATURN", "VENUS", "MARS", "MOTOR", "MODUL", "DOCK", "RADAR", "SIGNAL", "SONNE", "STERNE", "PLASMA", "VAKUUM", "ALIEN", "KRATER", "METEOR", "HELM", "ROVER", "TELESKOP", "SCHUB"]),
            ["Ancient Ruins"] = new("Alte Ruinen", ["RUINE", "TEMPEL", "SAEULE", "MARMOR", "ROLLE", "MOSAIK", "GRAB", "BOGEN", "STEIN", "GRUFT", "ALTAR", "RELIKT", "STATUE", "FACKEL", "TOR", "MAUER", "STAUB", "KRONE", "RUNE", "GOLD", "KAMMER", "MYTHOS", "KOENIG", "KOENIGIN", "PALAST", "BRONZE"]),
            ["Night Market"] = new("Nachtmarkt", ["MARKT", "STAND", "GEWUERZ", "LATERNE", "STRASSE", "OBST", "MANGO", "DATTEL", "TEE", "BROT", "KORB", "MUENZEN", "MUSIK", "STOFF", "SEIDE", "HANDWERK", "GLANZ", "NACHT", "SUESS", "LICHT", "MENGE", "HAENDLER", "DUFT", "FARBE", "GABE", "TANZ"]),
            ["Lakeside"] = new("Am See", ["SEE", "FLUSS", "BACH", "UFER", "KANU", "PADDEL", "SCHILF", "WEIDE", "REIHER", "STEG", "HAFEN", "FISCH", "WASSER", "WELLE", "INSEL", "NEBEL", "BRUECKE", "BOOT", "LILIE", "OTTER", "SAND", "MOOS", "TEICH", "DELTA", "BUCHT", "SPRITZER"]),
            ["Workshop"] = new("Werkstatt", ["WERKSTATT", "HAMMER", "NAGEL", "SCHRAUBE", "HOLZ", "METALL", "LEIM", "SAEGE", "BOHRER", "ZAHNRAD", "PINSEL", "FARBE", "WERKZEUG", "BANK", "ZWINGE", "LINEAL", "DREHEN", "RAHMEN", "SCHLUESSEL", "HANDWERK", "SAND", "MEISSEL", "REGAL", "BOLZEN", "KUPFER", "GRIFF"]),
            ["Summer Festival"] = new("Sommerfest", ["FEST", "TANZ", "MUSIK", "TROMMEL", "LIED", "LICHT", "BAND", "BUEHNE", "MENGE", "KARTE", "SOMMER", "FEUERWERK", "UMZUG", "RUMMEL", "FAHNEN", "ESSEN", "SUESS", "BALLON", "NACHT", "LACHEN", "SPIELE", "RAD", "PARTY", "GLANZ", "FREUDE", "MELODIE"]),
            ["Mythic Tales"] = new("Sagenwelt", ["DRACHE", "PHOENIX", "GREIF", "EINHORN", "RIESE", "FEE", "ELF", "ZWERG", "QUESTE", "MAGIE", "ZAUBER", "RUNE", "BURG", "WALD", "SCHWERT", "SCHILD", "MAGIER", "RITTER", "KRONE", "GOBLIN", "TRANK", "SAGE", "TROLL", "ORAKEL", "TEMPEL", "HELD"]),
        };

    public static WordSearchPuzzleDefinition ForLanguage(WordSearchPuzzleDefinition definition, string? language)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return language?.StartsWith("de", StringComparison.OrdinalIgnoreCase) == true
               && GermanThemes.TryGetValue(definition.Theme, out ThemeWords? german)
            ? definition with { Theme = german.Theme, WordBank = german.Words }
            : definition;
    }

    private static readonly IReadOnlyList<WordSearchPuzzleDefinition> puzzles = Build();
    public static IReadOnlyList<WordSearchPuzzleDefinition> All => puzzles;

    public static IReadOnlyList<WordSearchPuzzleDefinition> For(WordSearchDifficulty difficulty) =>
        puzzles.Where(x => x.Difficulty == difficulty).ToArray();

    public static WordSearchPuzzleDefinition Find(string id) =>
        puzzles.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.Ordinal))
        ?? throw new KeyNotFoundException($"Unknown Word Search puzzle '{id}'.");

    public static (int Size, int WordCount) Spec(WordSearchDifficulty difficulty) => difficulty switch
    {
        WordSearchDifficulty.Relaxed => (8, 6),
        WordSearchDifficulty.Easy => (9, 7),
        WordSearchDifficulty.Medium => (10, 8),
        WordSearchDifficulty.Hard => (11, 10),
        WordSearchDifficulty.Expert => (12, 12),
        WordSearchDifficulty.Master => (13, 14),
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    private static IReadOnlyList<WordSearchPuzzleDefinition> Build()
    {
        var result = new List<WordSearchPuzzleDefinition>(288);
        foreach (WordSearchDifficulty difficulty in Enum.GetValues<WordSearchDifficulty>())
        {
            (int size, int count) = Spec(difficulty);
            for (int index = 0; index < 48; index++)
            {
                bool expansion = index >= 24;
                int localIndex = expansion ? index - 24 : index;
                ThemeWords theme = expansion
                    ? ExpansionThemes[localIndex % ExpansionThemes.Length]
                    : Themes[localIndex % Themes.Length];
                int variant = localIndex / 12;
                // Never modify a seed used by the original 144 puzzle definitions.
                int seed = expansion
                    ? 80_000 + (int)difficulty * 2_000 + localIndex * 61 + variant * 193
                    : 10_000 + (int)difficulty * 1_000 + index * 37 + variant * 101;
                string id = $"word-search-{difficulty.ToString().ToLowerInvariant()}-{index + 1:000}";
                result.Add(new(id, difficulty, theme.Theme, size, count, seed, theme.Words));
            }
        }
        return result.AsReadOnly();
    }
}

public static class WordSearchGenerator
{
    private readonly record struct Direction(int Row, int Column);

    public static WordSearchPuzzle Generate(WordSearchPuzzleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var random = new StableRandom((uint)definition.Seed);
        char[,] grid = new char[definition.Size, definition.Size];
        var placements = new List<WordSearchPlacement>();
        Direction[] directions = Directions(definition.Difficulty);

        string[] words = definition.WordBank
            .Select(NormalizeWord)
            .Where(word => word.Length >= 3 && word.Length <= definition.Size)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Shuffle(words, random);
        words = words.Take(definition.WordCount).OrderByDescending(x => x.Length).ToArray();
        if (words.Length < definition.WordCount)
            throw new InvalidOperationException($"Theme '{definition.Theme}' does not contain enough words for {definition.Id}.");

        foreach (string word in words)
        {
            if (!Place(word, grid, definition.Size, directions, random, out WordSearchPlacement? placement) || placement is null)
                throw new InvalidOperationException($"Could not place '{word}' in deterministic puzzle {definition.Id}.");
            placements.Add(placement);
        }

        string filler = definition.Difficulty >= WordSearchDifficulty.Hard
            ? "EEEEEEEEAAAAAAARRRRRIIIIOOOOTTTTNNNSSSLLLCCCDDDPMHGBFUVWYJKQXZ"
            : "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int row = 0; row < definition.Size; row++)
        for (int column = 0; column < definition.Size; column++)
            if (grid[row, column] == '\0') grid[row, column] = filler[random.Next(filler.Length)];

        var flat = new char[definition.Size * definition.Size];
        for (int row = 0; row < definition.Size; row++)
        for (int column = 0; column < definition.Size; column++)
            flat[row * definition.Size + column] = grid[row, column];

        return new(definition.Id, definition.Difficulty, definition.Theme, definition.Size,
            new string(flat), placements.OrderBy(x => x.Word, StringComparer.Ordinal).ToArray());
    }

    private static bool Place(string word, char[,] grid, int size, Direction[] directions, StableRandom random, out WordSearchPlacement? placement)
    {
        for (int attempt = 0; attempt < 2_000; attempt++)
        {
            Direction direction = directions[random.Next(directions.Length)];
            int startRow = random.Next(size);
            int startColumn = random.Next(size);
            int endRow = startRow + direction.Row * (word.Length - 1);
            int endColumn = startColumn + direction.Column * (word.Length - 1);
            if (endRow < 0 || endRow >= size || endColumn < 0 || endColumn >= size) continue;

            bool fits = true;
            int overlaps = 0;
            for (int i = 0; i < word.Length; i++)
            {
                char current = grid[startRow + direction.Row * i, startColumn + direction.Column * i];
                if (current != '\0' && current != word[i]) { fits = false; break; }
                if (current == word[i]) overlaps++;
            }
            if (!fits) continue;
            if (attempt < 150 && overlaps == 0 && word.Length >= 6) continue;

            for (int i = 0; i < word.Length; i++)
                grid[startRow + direction.Row * i, startColumn + direction.Column * i] = word[i];
            placement = new(word, startRow, startColumn, endRow, endColumn);
            return true;
        }
        placement = null;
        return false;
    }

    private static Direction[] Directions(WordSearchDifficulty difficulty) => difficulty switch
    {
        WordSearchDifficulty.Relaxed => [new(0, 1), new(1, 0)],
        WordSearchDifficulty.Easy => [new(0, 1), new(1, 0), new(1, 1), new(1, -1)],
        _ => [new(0, 1), new(0, -1), new(1, 0), new(-1, 0), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)],
    };

    private static string NormalizeWord(string value) => new(value
        .ToUpperInvariant()
        .Where(c => c is >= 'A' and <= 'Z')
        .ToArray());

    private static void Shuffle<T>(T[] values, StableRandom random)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private sealed class StableRandom(uint seed)
    {
        private uint state = seed == 0 ? 0xA341316Cu : seed;
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return (int)(x % (uint)maxExclusive);
        }
    }
}
