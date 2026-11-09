namespace BlackjackCasino.Core.Sudoku;

/// <summary>Rule-aware uniqueness checker; supports irregular regions, diagonals and cage sums.</summary>
public static class SudokuVariantSolver
{
    public static int CountSolutions(SudokuVariantPuzzle puzzle, string givens, int limit = 2)
    {
        if (givens.Length != puzzle.Solution.Length || limit < 1)
            throw new ArgumentException("Invalid puzzle grid.", nameof(givens));
        int length = givens.Length, digits = puzzle.Digits;
        int[] values = new int[length];
        for (int i = 0; i < length; i++)
        {
            if (!puzzle.Active(i)) continue;
            int digit = givens[i] - '0';
            if (digit < 0 || digit > digits) return 0;
            values[i] = digit;
        }
        var peerSets = Enumerable.Range(0, length).Select(_ => new HashSet<int>()).ToArray();
        foreach (int[] group in puzzle.Groups)
            foreach (int cell in group)
                peerSets[cell].UnionWith(group.Where(other => other != cell));
        foreach (SudokuVariantCage cage in puzzle.Cages)
            foreach (int cell in cage.Cells)
                peerSets[cell].UnionWith(cage.Cells.Where(other => other != cell));
        int[][] peers = peerSets.Select(s => s.ToArray()).ToArray();
        int[] cageIndex = Enumerable.Repeat(-1, length).ToArray();
        for (int c = 0; c < puzzle.Cages.Count; c++)
            foreach (int cell in puzzle.Cages[c].Cells)
                cageIndex[cell] = c;

        int solutions = 0;
        void Search()
        {
            if (solutions >= limit) return;
            int target = -1, bestMask = 0, bestCount = int.MaxValue;
            for (int index = 0; index < length; index++)
            {
                if (!puzzle.Active(index) || values[index] != 0) continue;
                int mask = (1 << (digits + 1)) - 2;
                foreach (int peer in peers[index])
                    if (values[peer] != 0)
                        mask &= ~(1 << values[peer]);
                int cage = cageIndex[index];
                if (cage >= 0)
                {
                    SudokuVariantCage group = puzzle.Cages[cage];
                    int sum = group.Cells.Sum(i => values[i]);
                    int remain = group.Cells.Count(i => values[i] == 0);
                    for (int d = 1; d <= digits; d++)
                    {
                        if ((mask & (1 << d)) == 0) continue;
                        if (sum + d > group.Sum || (remain == 1 && sum + d != group.Sum))
                            mask &= ~(1 << d);
                    }
                }
                int choices = System.Numerics.BitOperations.PopCount((uint)mask);
                if (choices == 0) return;
                if (choices < bestCount)
                {
                    target = index;
                    bestCount = choices;
                    bestMask = mask;
                    if (choices == 1) break;
                }
            }
            if (target == -1)
            {
                if (puzzle.Cages.All(c => c.Cells.Sum(i => values[i]) == c.Sum)) solutions++;
                return;
            }
            int activeCage = cageIndex[target];
            for (int digit = 1; digit <= digits; digit++)
            {
                if ((bestMask & (1 << digit)) == 0) continue;
                values[target] = digit;
                bool possible = true;
                if (activeCage >= 0)
                {
                    SudokuVariantCage cage = puzzle.Cages[activeCage];
                    int sum = cage.Cells.Sum(i => values[i]), remain = cage.Cells.Count(i => values[i] == 0);
                    int[] available = Enumerable.Range(1, digits)
                        .Where(d => !cage.Cells.Any(i => values[i] == d)).ToArray();
                    if (sum > cage.Sum || available.Length < remain) possible = false;
                    else if (remain > 0)
                    {
                        Array.Sort(available);
                        int min = available.Take(remain).Sum();
                        int max = available.TakeLast(remain).Sum();
                        if (sum + min > cage.Sum || sum + max < cage.Sum) possible = false;
                    }
                    else if (sum != cage.Sum) possible = false;
                }
                if (possible) Search();
                values[target] = 0;
                if (solutions >= limit) return;
            }
        }
        // Reject inconsistent initial clues before searching.
        for (int index = 0; index < length; index++)
            if (values[index] != 0 && peers[index].Any(peer => values[peer] == values[index]))
                return 0;
        Search();
        return solutions;
    }
}
