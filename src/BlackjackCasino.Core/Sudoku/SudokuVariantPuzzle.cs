using System.Globalization;

namespace BlackjackCasino.Core.Sudoku;

public sealed record SudokuVariantCage(int Sum, int[] Cells);

/// <summary>Native, trusted puzzle definition. The answer never crosses the browser bridge.</summary>
public sealed class SudokuVariantPuzzle
{
    public string Id { get; }
    public string Mode { get; }
    public SudokuDifficulty Difficulty { get; }
    public int MiniSize { get; }
    public int Width { get; }
    public int Digits { get; }
    public string Givens { get; }
    public string Solution { get; }
    public int[][] Groups { get; }
    public int[] Regions { get; }
    public IReadOnlyList<SudokuVariantCage> Cages { get; }

    public SudokuVariantPuzzle(string id, string mode, SudokuDifficulty difficulty, int miniSize,
        int width, int digits, string givens, string solution, int[][] groups, int[] regions,
        IReadOnlyList<SudokuVariantCage>? cages = null)
    {
        Id = id;
        Mode = mode;
        Difficulty = difficulty;
        MiniSize = miniSize;
        Width = width;
        Digits = digits;
        Givens = givens;
        Solution = solution;
        Groups = groups;
        Regions = regions;
        Cages = cages ?? [];
        if (givens.Length != width * width || solution.Length != givens.Length || regions.Length != givens.Length)
            throw new InvalidDataException("Variant Sudoku dimensions do not match.");
        var included = new HashSet<int>(Groups.SelectMany(g => g));
        for (int cell = 0; cell < givens.Length; cell++)
        {
            bool inactive = solution[cell] == '-';
            if (inactive != (givens[cell] == '-') || (inactive && included.Contains(cell)))
                throw new InvalidDataException("Variant Sudoku has an inconsistent inactive cell.");
            if (!inactive && (solution[cell] < '1' || solution[cell] > '0' + digits ||
                (givens[cell] != '0' && givens[cell] != solution[cell]) || !included.Contains(cell)))
                throw new InvalidDataException("Variant Sudoku has an invalid solution or clue.");
        }
        foreach (int[] group in groups)
        {
            if (group.Length != digits || group.Distinct().Count() != digits ||
                group.Select(i => solution[i]).Distinct().Count() != digits)
                throw new InvalidDataException("Variant Sudoku constraint is not satisfied.");
        }
        foreach (SudokuVariantCage cage in Cages)
        {
            if (cage.Cells.Length == 0 || cage.Cells.Distinct().Count() != cage.Cells.Length ||
                cage.Cells.Select(i => solution[i]).Distinct().Count() != cage.Cells.Length ||
                cage.Cells.Sum(i => solution[i] - '0') != cage.Sum)
                throw new InvalidDataException("Invalid Killer Sudoku cage.");
        }
    }

    public bool Active(int index) => index >= 0 && index < Solution.Length && Solution[index] != '-';
    public bool IsGiven(int index) => Active(index) && Givens[index] != '0';
    public string PuzzleKey => Key(Mode, Difficulty, MiniSize);
    public static string Key(string mode, SudokuDifficulty difficulty, int miniSize) =>
        mode + ":" + difficulty + (mode == "mini" ? ":" + miniSize.ToString(CultureInfo.InvariantCulture) : "");

    public bool HasConflict(string board, int index)
    {
        if (!Active(index) || board[index] == '0') return false;
        foreach (int[] group in Groups)
            if (group.Contains(index) && group.Any(other => other != index && board[other] == board[index]))
                return true;
        foreach (SudokuVariantCage cage in Cages)
            if (cage.Cells.Contains(index))
            {
                int placedSum = cage.Cells.Sum(i => board[i] is >= '1' and <= '9' ? board[i] - '0' : 0);
                if (placedSum > cage.Sum || cage.Cells.All(i => board[i] != '0') && placedSum != cage.Sum)
                    return true;
                if (cage.Cells.Count(i => board[i] == board[index]) > 1) return true;
            }
        return false;
    }
}
