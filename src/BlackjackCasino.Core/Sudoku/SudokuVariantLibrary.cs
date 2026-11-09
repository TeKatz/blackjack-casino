namespace BlackjackCasino.Core.Sudoku;

/// <summary>
/// Offline variant library: canonical solved templates, deterministic clue layouts and
/// constraint-preserving transforms. No external puzzle data or web service is required.
/// </summary>
public static class SudokuVariantLibrary
{
    private const string DiagonalSolution =
        "379264518865791342241358769532849176418627935796135284983576421157482693624913857";
    private const string KillerCages =
        "15,29,44,48,68,79,39.40.41.42,49.57.58.59,16.25.26.35,67.76.77.78,23.30.31.32,0.9,5.6.7.14,54.55,56.65.66,24.33.34.43,74.75,60.61.62.69,38.45.46.47,3.11.12.20,10.18.19.28,50.51.52.53,63.64.72.73,70.71.80,4.13,21.22,8.17,1.2,27.36.37";

    private static readonly string[] Modes = ["x", "killer", "jigsaw", "samurai", "mini"];

    public static bool Supports(string mode, SudokuDifficulty difficulty, int size) =>
        Modes.Contains(mode, StringComparer.Ordinal) &&
        difficulty is SudokuDifficulty.Easy or SudokuDifficulty.Medium or SudokuDifficulty.Hard &&
        (mode == "mini" ? size is 4 or 6 : size == 9);

    public static bool TryParseKey(string value, out string mode, out SudokuDifficulty difficulty, out int size)
    {
        mode = ""; difficulty = SudokuDifficulty.Easy; size = 9;
        string[] parts = value.Split(':');
        if (parts.Length is < 2 or > 3 || !Enum.TryParse(parts[1], false, out difficulty))
            return false;
        mode = parts[0];
        if (parts.Length == 3 && (!int.TryParse(parts[2], out size) || mode != "mini"))
            return false;
        return Supports(mode, difficulty, size);
    }

    public static SudokuVariantPuzzle Create(string mode, SudokuDifficulty difficulty, int size, int index)
    {
        if (!Supports(mode, difficulty, size) || index < 0 || index > 9999)
            throw new ArgumentOutOfRangeException(nameof(index), "Unsupported Sudoku puzzle variant.");
        return mode switch
        {
            "x" => BuildNine(mode, difficulty, index),
            "jigsaw" => BuildNine(mode, difficulty, index),
            "killer" => BuildNine(mode, difficulty, index),
            "mini" => BuildMini(difficulty, size, index),
            "samurai" => BuildSamurai(difficulty, index),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }

    private static SudokuVariantPuzzle BuildNine(string mode, SudokuDifficulty difficulty, int index)
    {
        int[] rawRegions = Enumerable.Range(0, 81).Select(i => (i / 27) * 3 + (i % 9 / 3)).ToArray();
        string solution;
        if (mode == "x")
            solution = DiagonalSolution;
        else
        {
            // Swap the centre 3-column stack for a canonical complete Latin Sudoku.
            int[] columns = mode == "jigsaw"
                ? [0, 1, 2, 5, 4, 3, 6, 7, 8]
                : [0, 1, 2, 3, 4, 5, 6, 7, 8];
            solution = string.Concat(Enumerable.Range(0, 81)
                .Select(i => (char)('1' + ((i / 9 * 3 + i / 27 + columns[i % 9]) % 9))));
        }

        if (mode == "jigsaw")
        {
            for (int band = 0; band < 3; band++)
                foreach (int left in new[] { 2, 5 })
                {
                    int a = (band * 3 + 1) * 9 + left;
                    int b = band * 3 * 9 + left + 1;
                    if (solution[a] != solution[b])
                        throw new InvalidDataException("Jigsaw region swap would violate solution.");
                    (rawRegions[a], rawRegions[b]) = (rawRegions[b], rawRegions[a]);
                }
        }

        int transform = index % 8, shift = index / 8 % 9;
        solution = Remap(solution, 9, transform,
            digit => mode == "killer" && shift % 2 == 1 ? 10 - digit
                : mode != "killer" ? (digit - 1 + shift) % 9 + 1 : digit);
        int[] regions = Remap(rawRegions, 9, transform);
        var groups = new List<int[]>();
        AddRowsColumns(groups, 9, 9);
        for (int region = 0; region < 9; region++)
        {
            int[] members = Enumerable.Range(0, 81).Where(i => regions[i] == region).ToArray();
            groups.Add(members);
        }
        if (mode == "x")
        {
            groups.Add(Enumerable.Range(0, 9).Select(i => i * 10).ToArray());
            groups.Add(Enumerable.Range(0, 9).Select(i => i * 9 + 8 - i).ToArray());
        }

        var cages = new List<SudokuVariantCage>();
        if (mode == "killer")
            foreach (string raw in KillerCages.Split(','))
            {
                int[] cells = raw.Split('.').Select(int.Parse).Select(i => MapIndex(i, 9, transform)).ToArray();
                cages.Add(new SudokuVariantCage(cells.Sum(i => solution[i] - '0'), cells));
            }

        char[] givens = solution.ToCharArray();
        if (mode == "killer")
        {
            Array.Fill(givens, '0');
            // Easier Killer puzzles supply a few ordinary clues in addition to the authentic cages.
            int clues = difficulty == SudokuDifficulty.Easy ? 11
                : difficulty == SudokuDifficulty.Medium ? 4 : 0;
            var random = Shuffle(Enumerable.Range(0, 81), 733 + index * 101);
            foreach (int cell in random.Take(clues)) givens[cell] = solution[cell];
        }
        else
        {
            int missing = difficulty switch
            {
                SudokuDifficulty.Easy => 29,
                SudokuDifficulty.Medium => 39,
                _ => 48,
            };
            foreach (int cell in Shuffle(Enumerable.Range(0, 81), 401 + index * 107).Take(missing))
                givens[cell] = '0';
        }
        string id = $"variant-{mode}-{difficulty.ToString().ToLowerInvariant()}-{index:D4}";
        var puzzle = new SudokuVariantPuzzle(id, mode, difficulty, 9, 9, 9,
            new string(givens), solution, groups.ToArray(), regions, cages);
        // Guaranteed unique even with the actual extra constraints, not merely a target answer.
        if (mode != "killer")
            EnsureUnique(puzzle, givens, 9, mode == "x" ? SudokuSolver.CountSolutions : null);
        return new SudokuVariantPuzzle(id, mode, difficulty, 9, 9, 9,
            new string(givens), solution, groups.ToArray(), regions, cages);
    }

    private static SudokuVariantPuzzle BuildMini(SudokuDifficulty difficulty, int size, int index)
    {
        int boxHeight = 2, boxWidth = size / 2;
        string raw = string.Concat(Enumerable.Range(0, size * size)
            .Select(i => (char)('1' + ((i / size * boxWidth + i / size / boxHeight + i % size) % size))));
        int transform = index % 8;
        string solution = Remap(raw, size, transform, digit => (digit - 1 + index / 8 % size) % size + 1);
        int[] regions = Enumerable.Range(0, size * size).Select(i =>
            (i / size / boxHeight) * (size / boxWidth) + i % size / boxWidth).ToArray();
        regions = Remap(regions, size, transform);
        var groups = new List<int[]>();
        AddRowsColumns(groups, size, size);
        for (int region = 0; region < size; region++)
            groups.Add(Enumerable.Range(0, size * size).Where(i => regions[i] == region).ToArray());
        char[] givens = solution.ToCharArray();
        int missing = size == 4
            ? (difficulty == SudokuDifficulty.Easy ? 5 : difficulty == SudokuDifficulty.Medium ? 8 : 11)
            : (difficulty == SudokuDifficulty.Easy ? 12 : difficulty == SudokuDifficulty.Medium ? 18 : 24);
        foreach (int cell in Shuffle(Enumerable.Range(0, size * size), 809 + index * 71).Take(missing))
            givens[cell] = '0';
        string id = $"variant-mini{size}-{difficulty.ToString().ToLowerInvariant()}-{index:D4}";
        var puzzle = new SudokuVariantPuzzle(id, "mini", difficulty, size, size, size,
            new string(givens), solution, groups.ToArray(), regions);
        EnsureUnique(puzzle, givens, size, null);
        return new SudokuVariantPuzzle(id, "mini", difficulty, size, size, size,
            new string(givens), solution, groups.ToArray(), regions);
    }

    private static SudokuVariantPuzzle BuildSamurai(SudokuDifficulty difficulty, int index)
    {
        const int width = 21;
        int[] regions = Enumerable.Repeat(-1, width * width).ToArray();
        char[] solution = Enumerable.Repeat('-', width * width).ToArray();
        char[] givens = Enumerable.Repeat('-', width * width).ToArray();
        var groups = new List<int[]>();
        (int R, int C)[] offsets = [(6, 6), (0, 0), (0, 12), (12, 0), (12, 12)];
        // The centre fixes the overlapping 3x3. Each outer grid is independently
        // unique by its classic seed, transformed by a bijection of all nine digits.
        IReadOnlyList<SudokuPuzzle> seeds = SudokuLibrary.ForDifficulty(difficulty);
        for (int grid = 0; grid < offsets.Length; grid++)
        {
            SudokuPuzzle seed = seeds[(index * 17 + grid * 13) % seeds.Count];
            if (!SudokuSolver.TrySolve(seed.Givens, out string solved))
                throw new InvalidDataException("Samurai component cannot be solved.");
            int row = offsets[grid].R, column = offsets[grid].C;
            char[] mapping = new char[10];
            if (grid == 0)
                for (int digit = 1; digit <= 9; digit++) mapping[digit] = (char)('0' + digit);
            else
                for (int local = 0; local < 81; local++)
                {
                    int global = (row + local / 9) * width + column + local % 9;
                    if (solution[global] == '-') continue;
                    int digit = solved[local] - '0';
                    if (mapping[digit] != '\0' && mapping[digit] != solution[global])
                        throw new InvalidDataException("Samurai overlapping digits disagree.");
                    mapping[digit] = solution[global];
                }
            if (mapping.Skip(1).Any(x => x == '\0') ||
                mapping.Skip(1).Distinct().Count() != 9)
                throw new InvalidDataException("Samurai overlap is not a full 3x3 permutation.");
            for (int local = 0; local < 81; local++)
            {
                int global = (row + local / 9) * width + column + local % 9;
                char digit = mapping[solved[local] - '0'];
                if (solution[global] != '-' && solution[global] != digit)
                    throw new InvalidDataException("Samurai overlap does not agree.");
                solution[global] = digit;
                if (seed.Givens[local] != '0')
                    givens[global] = digit;
                else if (givens[global] == '-')
                    givens[global] = '0';
                int region = grid * 9 + (local / 27) * 3 + (local % 9 / 3);
                if (regions[global] == -1) regions[global] = region;
            }
            for (int r = 0; r < 9; r++)
            {
                groups.Add(Enumerable.Range(0, 9).Select(c => (row + r) * width + column + c).ToArray());
                groups.Add(Enumerable.Range(0, 9).Select(c => (row + c) * width + column + r).ToArray());
            }
            for (int br = 0; br < 9; br += 3)
                for (int bc = 0; bc < 9; bc += 3)
                    groups.Add(Enumerable.Range(0, 9)
                        .Select(i => (row + br + i / 3) * width + column + bc + i % 3).ToArray());
        }
        return new SudokuVariantPuzzle($"variant-samurai-{difficulty.ToString().ToLowerInvariant()}-{index:D4}",
            "samurai", difficulty, 9, width, 9,
            new string(givens), new string(solution), groups.ToArray(), regions);
    }

    private static void AddRowsColumns(List<int[]> groups, int width, int digits)
    {
        for (int i = 0; i < digits; i++)
        {
            groups.Add(Enumerable.Range(0, digits).Select(j => i * width + j).ToArray());
            groups.Add(Enumerable.Range(0, digits).Select(j => j * width + i).ToArray());
        }
    }

    private static int MapIndex(int index, int size, int transform)
    {
        int r = index / size, c = index % size;
        if ((transform & 4) != 0) c = size - 1 - c;
        for (int i = 0; i < (transform & 3); i++)
            (r, c) = (c, size - 1 - r);
        return r * size + c;
    }

    private static string Remap(string input, int size, int transform, Func<int, int> digit)
    {
        char[] result = new char[size * size];
        for (int index = 0; index < result.Length; index++)
            result[MapIndex(index, size, transform)] = (char)('0' + digit(input[index] - '0'));
        return new string(result);
    }

    private static int[] Remap(int[] input, int size, int transform)
    {
        int[] result = new int[input.Length];
        for (int index = 0; index < input.Length; index++)
            result[MapIndex(index, size, transform)] = input[index];
        return result;
    }

    private static int[] Shuffle(IEnumerable<int> cells, int seed)
    {
        int[] result = cells.ToArray();
        uint state = unchecked((uint)seed) | 1u;
        for (int i = result.Length - 1; i > 0; i--)
        {
            state = unchecked(state * 1664525u + 1013904223u);
            int j = (int)(state % (uint)(i + 1));
            (result[i], result[j]) = (result[j], result[i]);
        }
        return result;
    }

    private static void EnsureUnique(SudokuVariantPuzzle puzzle, char[] givens, int size,
        Func<string, int, int>? classicCounter)
    {
        // Add known-correct clues until the actual variation has exactly one solution.
        // Typically zero to a handful of additional cells are needed.
        if (SudokuVariantSolver.CountSolutions(puzzle, new string(givens)) == 1) return;
        for (int i = 0; i < givens.Length; i++)
        {
            if (givens[i] != '0') continue;
            givens[i] = puzzle.Solution[i];
            if (SudokuVariantSolver.CountSolutions(puzzle, new string(givens)) == 1) return;
        }
        throw new InvalidDataException("Could not make the variant puzzle unique.");
    }
}
