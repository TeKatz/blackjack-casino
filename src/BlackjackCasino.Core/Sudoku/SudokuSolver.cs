using System.Numerics;

namespace BlackjackCasino.Core.Sudoku;

public static class SudokuSolver
{
    private const int AllDigitsMask = 0x3FE;

    public static void ValidateGrid(string grid, bool allowEmpty)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.Length != 81)
        {
            throw new ArgumentException("A Sudoku grid must contain exactly 81 cells.", nameof(grid));
        }

        foreach (char cell in grid)
        {
            if (cell is >= '1' and <= '9')
            {
                continue;
            }
            if (allowEmpty && cell is '0' or '.')
            {
                continue;
            }
            throw new ArgumentException("Sudoku cells must contain digits 1-9" + (allowEmpty ? " or an empty marker." : "."), nameof(grid));
        }
    }

    public static int CountSolutions(string givens, int limit = 2)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        ValidateGrid(givens, allowEmpty: true);
        int[] cells = Parse(givens);
        int[] rows = new int[9];
        int[] columns = new int[9];
        int[] boxes = new int[9];
        if (!InitializeMasks(cells, rows, columns, boxes))
        {
            return 0;
        }

        int count = 0;
        Search(cells, rows, columns, boxes, limit, ref count, stopAtFirst: false);
        return count;
    }

    public static bool TrySolve(string givens, out string solution)
    {
        ValidateGrid(givens, allowEmpty: true);
        int[] cells = Parse(givens);
        int[] rows = new int[9];
        int[] columns = new int[9];
        int[] boxes = new int[9];
        if (!InitializeMasks(cells, rows, columns, boxes))
        {
            solution = string.Empty;
            return false;
        }

        int count = 0;
        bool solved = Search(cells, rows, columns, boxes, 1, ref count, stopAtFirst: true);
        solution = solved ? ToGrid(cells) : string.Empty;
        return solved;
    }

    public static bool IsValidCompletedGrid(string grid)
    {
        try
        {
            ValidateGrid(grid, allowEmpty: false);
        }
        catch (ArgumentException)
        {
            return false;
        }

        int[] cells = Parse(grid);
        int[] rows = new int[9];
        int[] columns = new int[9];
        int[] boxes = new int[9];
        return InitializeMasks(cells, rows, columns, boxes);
    }

    private static bool Search(
        int[] cells,
        int[] rows,
        int[] columns,
        int[] boxes,
        int limit,
        ref int count,
        bool stopAtFirst)
    {
        int bestIndex = -1;
        int bestMask = 0;
        int bestCount = 10;

        for (int index = 0; index < cells.Length; index++)
        {
            if (cells[index] != 0)
            {
                continue;
            }

            int row = index / 9;
            int column = index % 9;
            int box = Box(row, column);
            int candidates = AllDigitsMask & ~(rows[row] | columns[column] | boxes[box]);
            int candidateCount = BitOperations.PopCount((uint)candidates);
            if (candidateCount == 0)
            {
                return false;
            }
            if (candidateCount < bestCount)
            {
                bestIndex = index;
                bestMask = candidates;
                bestCount = candidateCount;
                if (candidateCount == 1)
                {
                    break;
                }
            }
        }

        if (bestIndex < 0)
        {
            count++;
            return true;
        }

        int bestRow = bestIndex / 9;
        int bestColumn = bestIndex % 9;
        int bestBox = Box(bestRow, bestColumn);
        bool found = false;
        for (int digit = 1; digit <= 9; digit++)
        {
            int bit = 1 << digit;
            if ((bestMask & bit) == 0)
            {
                continue;
            }

            cells[bestIndex] = digit;
            rows[bestRow] |= bit;
            columns[bestColumn] |= bit;
            boxes[bestBox] |= bit;

            bool branchFound = Search(cells, rows, columns, boxes, limit, ref count, stopAtFirst);
            found |= branchFound;
            if ((stopAtFirst && branchFound) || count >= limit)
            {
                if (stopAtFirst && branchFound)
                {
                    return true;
                }
                cells[bestIndex] = 0;
                rows[bestRow] &= ~bit;
                columns[bestColumn] &= ~bit;
                boxes[bestBox] &= ~bit;
                return found;
            }

            cells[bestIndex] = 0;
            rows[bestRow] &= ~bit;
            columns[bestColumn] &= ~bit;
            boxes[bestBox] &= ~bit;
        }

        return found;
    }

    private static bool InitializeMasks(int[] cells, int[] rows, int[] columns, int[] boxes)
    {
        for (int index = 0; index < cells.Length; index++)
        {
            int digit = cells[index];
            if (digit == 0)
            {
                continue;
            }

            int row = index / 9;
            int column = index % 9;
            int box = Box(row, column);
            int bit = 1 << digit;
            if ((rows[row] & bit) != 0 || (columns[column] & bit) != 0 || (boxes[box] & bit) != 0)
            {
                return false;
            }
            rows[row] |= bit;
            columns[column] |= bit;
            boxes[box] |= bit;
        }
        return true;
    }

    private static int[] Parse(string grid)
    {
        var cells = new int[81];
        for (int index = 0; index < grid.Length; index++)
        {
            char value = grid[index];
            cells[index] = value is >= '1' and <= '9' ? value - '0' : 0;
        }
        return cells;
    }

    private static string ToGrid(int[] cells)
    {
        char[] result = new char[81];
        for (int index = 0; index < cells.Length; index++)
        {
            result[index] = (char)('0' + cells[index]);
        }
        return new string(result);
    }

    private static int Box(int row, int column) => (row / 3 * 3) + (column / 3);
}
