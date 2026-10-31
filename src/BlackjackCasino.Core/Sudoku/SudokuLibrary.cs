using System.Diagnostics.CodeAnalysis;

namespace BlackjackCasino.Core.Sudoku;

public static class SudokuLibrary
{
    private static readonly SudokuPuzzle[] SeedNormalPuzzles =
    [
        P("normal-relaxed-001", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Normal, "256000703000000010000200084000360020000800000498072000060007300102043508080005900"),
        P("normal-relaxed-002", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Normal, "817000904000000050000070032080009400705204103030001600362907000000030000000840070"),
        P("normal-relaxed-003", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Normal, "000000040000614502005000083007000000604000050820593000010040900490705806080030200"),
        P("normal-relaxed-004", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Normal, "030500900040900200010024800260300000704000009090768000000871600900000054000400703"),
        P("normal-easy-001", SudokuDifficulty.Easy, SudokuPuzzlePool.Normal, "000800300073064085048025061700010040054603208000000000081509602400030010000000000"),
        P("normal-easy-002", SudokuDifficulty.Easy, SudokuPuzzlePool.Normal, "050090000201305690703802150900008001034720580000000000800001006029570310000000000"),
        P("normal-easy-003", SudokuDifficulty.Easy, SudokuPuzzlePool.Normal, "000000000403058096070400100804901503000060080607803904108095032000000000040100600"),
        P("normal-easy-004", SudokuDifficulty.Easy, SudokuPuzzlePool.Normal, "070030004000000000802501360030040009000000000107605480058310640600007000014860790"),
        P("normal-medium-001", SudokuDifficulty.Medium, SudokuPuzzlePool.Normal, "006000902008020560000604080407100000103000700000036018600201009900360000000009240"),
        P("normal-medium-002", SudokuDifficulty.Medium, SudokuPuzzlePool.Normal, "100079006027001000000650001080062000064700080701000060000090032003000059890506000"),
        P("normal-medium-003", SudokuDifficulty.Medium, SudokuPuzzlePool.Normal, "302750000000000000000940063003009010720003500109070300200006001050007630010400082"),
        P("normal-medium-004", SudokuDifficulty.Medium, SudokuPuzzlePool.Normal, "007006004090040607004010930720408000000000000910000403200305060008600300009084010"),
        P("normal-hard-001", SudokuDifficulty.Hard, SudokuPuzzlePool.Normal, "401003000580000000029080000000000023000007106000500870100000007007320408302870500"),
        P("normal-hard-002", SudokuDifficulty.Hard, SudokuPuzzlePool.Normal, "700280034403900087001070000102000300640000008089000000000807090000034000000150700"),
        P("normal-hard-003", SudokuDifficulty.Hard, SudokuPuzzlePool.Normal, "086090000920700000504000000000040810000000072000002305070209540012405900009000080"),
        P("normal-hard-004", SudokuDifficulty.Hard, SudokuPuzzlePool.Normal, "015000906000000450720000013450038000000104000903720000540001000190000070000090005"),
        P("normal-expert-001", SudokuDifficulty.Expert, SudokuPuzzlePool.Normal, "010300000000080004800009020030040001400600800007002090005007008040060000900000500"),
        P("normal-expert-002", SudokuDifficulty.Expert, SudokuPuzzlePool.Normal, "002300010090070003800005600000010020500008009003900000040000008009006400000700030"),
        P("normal-expert-003", SudokuDifficulty.Expert, SudokuPuzzlePool.Normal, "300700000001000050040002100009080000700300600020001009600000500080040002002100030"),
        P("normal-expert-004", SudokuDifficulty.Expert, SudokuPuzzlePool.Normal, "040000006003070400100500000500100800020009000007040020009003070070400001800000600"),
        P("normal-master-001", SudokuDifficulty.Master, SudokuPuzzlePool.Normal, "007000008200000700080000050009007006010400020000050300006003009900080200040100070"),
        P("normal-master-002", SudokuDifficulty.Master, SudokuPuzzlePool.Normal, "040000050900080300006007001000006002000300060000020400020700030005001007800090600"),
        P("normal-master-003", SudokuDifficulty.Master, SudokuPuzzlePool.Normal, "600500040030001900000080002004000001700000080010000700090002300500600010003070004"),
        P("normal-master-004", SudokuDifficulty.Master, SudokuPuzzlePool.Normal, "007080004030000100500006090020600400100009060008070005000020003000005020000400500"),
    ];

    private static readonly SudokuPuzzle[] SeedDailyPuzzles =
    [
        P("daily-relaxed-001", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Daily, "060010009050030160040090007090000832206009000180000004900706000000024006000080253"),
        P("daily-relaxed-002", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Daily, "300040001700020005086013092439000058000800270000000300178052000000600000000309800"),
        P("daily-relaxed-003", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Daily, "807002000020000736061000400000060397000074800002508000040020500080010200090030081"),
        P("daily-relaxed-004", SudokuDifficulty.Relaxed, SudokuPuzzlePool.Daily, "865000090000009106030000082010063000657080000000401009040090030090020010201050070"),
        P("daily-easy-001", SudokuDifficulty.Easy, SudokuPuzzlePool.Daily, "060020018007800900003600074040050000009700045002400096008100600001900027020040089"),
        P("daily-easy-002", SudokuDifficulty.Easy, SudokuPuzzlePool.Daily, "075906340000000000100050002017560409000002070082470106041709260800010005000000000"),
        P("daily-easy-003", SudokuDifficulty.Easy, SudokuPuzzlePool.Daily, "005920070070860001090003006030650002020007009008390050060480003080730005004000080"),
        P("daily-easy-004", SudokuDifficulty.Easy, SudokuPuzzlePool.Daily, "609031405070800090000000000706503049000060800208407063020900070307048506000000000"),
        P("daily-medium-001", SudokuDifficulty.Medium, SudokuPuzzlePool.Daily, "860500000205900600009000051000008540080005063601000008700403000093000405000701030"),
        P("daily-medium-002", SudokuDifficulty.Medium, SudokuPuzzlePool.Daily, "307040000000702803401000070008009760009000038000160900000200180800096000200850006"),
        P("daily-medium-003", SudokuDifficulty.Medium, SudokuPuzzlePool.Daily, "903060000061020009020000560008740000000580070270000604059000300300600790000300046"),
        P("daily-medium-004", SudokuDifficulty.Medium, SudokuPuzzlePool.Daily, "000540061057000093000000000086005009790003500005080600070800400300054900800720010"),
        P("daily-hard-001", SudokuDifficulty.Hard, SudokuPuzzlePool.Daily, "056030790000008300037056040000704000600089000070510000000000605300000280004000073"),
        P("daily-hard-002", SudokuDifficulty.Hard, SudokuPuzzlePool.Daily, "900680000020405000000072000007000201000000095100000630059100820012590700000060010"),
        P("daily-hard-003", SudokuDifficulty.Hard, SudokuPuzzlePool.Daily, "470560002010000050500470063026000000130007000804600000000005091000000470000020506"),
        P("daily-hard-004", SudokuDifficulty.Hard, SudokuPuzzlePool.Daily, "700801000090000001000907020000000810000502073000710906052039000430108000807000000"),
        P("daily-expert-001", SudokuDifficulty.Expert, SudokuPuzzlePool.Daily, "600900050030040001009000700010000003800050060005002000006001005040020000700600800"),
        P("daily-expert-002", SudokuDifficulty.Expert, SudokuPuzzlePool.Daily, "020001000000700600300080007009400000700030040010002005008090030070300100000005006"),
        P("daily-expert-003", SudokuDifficulty.Expert, SudokuPuzzlePool.Daily, "002000009010090600000400070004700002060008100900020050009003800000200005300060000"),
        P("daily-expert-004", SudokuDifficulty.Expert, SudokuPuzzlePool.Daily, "000100090600040100080000007002000003040070000900005040500009600070030002008400070"),
        P("daily-master-001", SudokuDifficulty.Master, SudokuPuzzlePool.Daily, "100000004080007050003400200300060001008200400090001030000005070000090008000300600"),
        P("daily-master-002", SudokuDifficulty.Master, SudokuPuzzlePool.Daily, "070000090100600500002080003000040007000100400000003010040006030900500600008020001"),
        P("daily-master-003", SudokuDifficulty.Master, SudokuPuzzlePool.Daily, "010009300008040006200800040005070003000900080090001400007050000040002000600300000"),
        P("daily-master-004", SudokuDifficulty.Master, SudokuPuzzlePool.Daily, "070000080006100300900070004500040000030002000001600000700090003080005040004800200"),
    ];

    private static readonly SudokuPuzzle[] MassPuzzles = LoadMassPuzzles();
    private static readonly SudokuPuzzle[] NormalPuzzles = SeedNormalPuzzles.Concat(MassPuzzles.Where(puzzle => puzzle.Pool == SudokuPuzzlePool.Normal)).ToArray();
    private static readonly SudokuPuzzle[] DailyPuzzles = SeedDailyPuzzles.Concat(MassPuzzles.Where(puzzle => puzzle.Pool == SudokuPuzzlePool.Daily)).ToArray();
    private static readonly SudokuPuzzle[] AllPuzzles = NormalPuzzles.Concat(DailyPuzzles).ToArray();
    private static readonly Dictionary<string, SudokuPuzzle> ById = AllPuzzles.ToDictionary(puzzle => puzzle.Id, StringComparer.Ordinal);

    public static IReadOnlyList<SudokuPuzzle> Normal => NormalPuzzles;
    public static IReadOnlyList<SudokuPuzzle> Daily => DailyPuzzles;
    public static IReadOnlyList<SudokuPuzzle> All => AllPuzzles;

    public static bool TryGet(string id, [NotNullWhen(true)] out SudokuPuzzle? puzzle) => ById.TryGetValue(id, out puzzle);

    public static SudokuPuzzle Get(string id) =>
        TryGet(id, out SudokuPuzzle? puzzle)
            ? puzzle
            : throw new KeyNotFoundException($"Unknown Sudoku puzzle '{id}'.");

    public static IReadOnlyList<SudokuPuzzle> ForDifficulty(SudokuDifficulty difficulty) =>
        NormalPuzzles.Where(puzzle => puzzle.Difficulty == difficulty).ToArray();

    private static SudokuPuzzle[] LoadMassPuzzles()
    {
        const string resourceName = "BlackjackCasino.Core.Sudoku.mass-catalog.txt";
        using Stream stream = typeof(SudokuLibrary).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Missing embedded Sudoku catalog resource: {resourceName}");
        using var reader = new StreamReader(stream);
        var counters = new Dictionary<(SudokuPuzzlePool Pool, SudokuDifficulty Difficulty), int>();
        foreach (SudokuPuzzlePool pool in Enum.GetValues<SudokuPuzzlePool>())
        {
            foreach (SudokuDifficulty difficulty in Enum.GetValues<SudokuDifficulty>())
            {
                counters[(pool, difficulty)] = 4;
            }
        }

        var puzzles = new List<SudokuPuzzle>(12954);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            string[] parts = line.Split('|');
            if (parts.Length != 3 ||
                !Enum.TryParse(parts[0], ignoreCase: true, out SudokuPuzzlePool pool) ||
                !Enum.TryParse(parts[1], ignoreCase: true, out SudokuDifficulty difficulty))
            {
                throw new InvalidDataException($"Invalid Sudoku catalog line: {line}");
            }

            int index = ++counters[(pool, difficulty)];
            string prefix = pool == SudokuPuzzlePool.Normal ? "normal" : "daily";
            string id = $"{prefix}-{difficulty.ToString().ToLowerInvariant()}-{index:000}";
            puzzles.Add(P(id, difficulty, pool, parts[2]));
        }

        if (puzzles.Count != 12954)
        {
            throw new InvalidDataException($"Expected 12,954 generated Sudoku puzzles, found {puzzles.Count}.");
        }
        foreach (SudokuDifficulty difficulty in Enum.GetValues<SudokuDifficulty>())
        {
            if (counters[(SudokuPuzzlePool.Normal, difficulty)] != 1500 ||
                counters[(SudokuPuzzlePool.Daily, difficulty)] != 667)
            {
                throw new InvalidDataException($"Sudoku catalog has an invalid count for {difficulty}.");
            }
        }
        return puzzles.ToArray();
    }

    private static SudokuPuzzle P(string id, SudokuDifficulty difficulty, SudokuPuzzlePool pool, string givens) =>
        new(id, difficulty, pool, givens);
}

public static class SudokuDailySchedule
{
    public static DateOnly Epoch { get; } = new(2026, 10, 30);

    public static SudokuPuzzle ForDate(DateOnly date)
    {
        IReadOnlyList<SudokuPuzzle> puzzles = SudokuLibrary.Daily;
        if (puzzles.Count == 0)
        {
            throw new InvalidOperationException("The Daily Sudoku library is empty.");
        }

        int stride = CoprimeStride(puzzles.Count);
        long dayOffset = date.DayNumber - Epoch.DayNumber;
        int offset = Mod(dayOffset, puzzles.Count);
        int index = (offset * stride + Math.Min(7, puzzles.Count - 1)) % puzzles.Count;
        return puzzles[index];
    }

    private static int CoprimeStride(int count)
    {
        if (count <= 2) return 1;
        for (int candidate = Math.Max(2, count / 2 + 1); candidate < count; candidate++)
        {
            if (GreatestCommonDivisor(candidate, count) == 1) return candidate;
        }
        return 1;
    }

    private static int GreatestCommonDivisor(int left, int right)
    {
        while (right != 0) (left, right) = (right, left % right);
        return Math.Abs(left);
    }

    private static int Mod(long value, int modulo)
    {
        long result = value % modulo;
        return (int)(result < 0 ? result + modulo : result);
    }
}
