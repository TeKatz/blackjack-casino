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
        var result = new List<WordSearchPuzzleDefinition>(144);
        foreach (WordSearchDifficulty difficulty in Enum.GetValues<WordSearchDifficulty>())
        {
            (int size, int count) = Spec(difficulty);
            for (int index = 0; index < 24; index++)
            {
                ThemeWords theme = Themes[index % Themes.Length];
                int variant = index / Themes.Length;
                int seed = 10_000 + (int)difficulty * 1_000 + index * 37 + variant * 101;
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
