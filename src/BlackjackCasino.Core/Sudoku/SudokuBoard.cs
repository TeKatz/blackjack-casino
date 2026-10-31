namespace BlackjackCasino.Core.Sudoku;

public sealed record SudokuBoardFrame(string Board, int[] Notes);

public sealed class SudokuBoard
{
    private readonly SudokuPuzzle puzzle;
    private readonly string solution;
    private readonly int[] values = new int[81];
    private readonly int[] notes = new int[81];

    public SudokuBoard(SudokuPuzzle puzzle, SudokuGameState? state = null)
    {
        this.puzzle = puzzle ?? throw new ArgumentNullException(nameof(puzzle));
        if (!SudokuSolver.TrySolve(puzzle.Givens, out solution))
        {
            throw new ArgumentException($"Puzzle '{puzzle.Id}' is not solvable.", nameof(puzzle));
        }

        SudokuGameState clean = SudokuGameState.Normalize(puzzle, state);
        Load(clean.Board, clean.Notes);
        HintsUsed = clean.HintsUsed;
    }

    public SudokuPuzzle Puzzle => puzzle;
    public int HintsUsed { get; private set; }
    public bool IsComplete => CurrentGrid == solution;
    public string CurrentGrid => string.Create(81, values, static (span, cells) =>
    {
        for (int index = 0; index < cells.Length; index++)
        {
            span[index] = (char)('0' + cells[index]);
        }
    });

    public int ValueAt(int index)
    {
        ValidateIndex(index);
        return values[index];
    }

    public int NotesAt(int index)
    {
        ValidateIndex(index);
        return notes[index];
    }

    public bool IsGiven(int index)
    {
        ValidateIndex(index);
        return puzzle.Givens[index] != '0';
    }

    public bool IsCorrectAt(int index)
    {
        ValidateIndex(index);
        return values[index] != 0 && values[index] == solution[index] - '0';
    }

    public bool IsIncorrectAt(int index)
    {
        ValidateIndex(index);
        return values[index] != 0 && values[index] != solution[index] - '0';
    }

    public bool IsDigitComplete(int digit)
    {
        if (digit is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }

        char expected = (char)('0' + digit);
        for (int index = 0; index < 81; index++)
        {
            if (solution[index] == expected)
            {
                if (values[index] != digit)
                {
                    return false;
                }
            }
            else if (values[index] == digit)
            {
                return false;
            }
        }
        return true;
    }

    public bool SetValue(int index, int value)
    {
        ValidateIndex(index);
        if (value is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        if (IsGiven(index))
        {
            return false;
        }
        if (values[index] == value)
        {
            return false;
        }

        values[index] = value;
        notes[index] = 0;
        if (value != 0)
        {
            ClearPeerNote(index, value);
        }
        return true;
    }

    public bool ToggleNote(int index, int digit)
    {
        ValidateIndex(index);
        if (digit is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(digit));
        }
        if (IsGiven(index) || values[index] != 0)
        {
            return false;
        }

        int bit = 1 << digit;
        notes[index] ^= bit;
        return true;
    }

    public int ApplyHint(int preferredIndex = -1)
    {
        int index = preferredIndex is >= 0 and < 81 && CanReveal(preferredIndex)
            ? preferredIndex
            : Enumerable.Range(0, 81).FirstOrDefault(candidate =>
                !IsGiven(candidate) && values[candidate] != 0 && IsIncorrectAt(candidate), -1);
        if (index < 0)
        {
            index = Enumerable.Range(0, 81).FirstOrDefault(candidate => !IsGiven(candidate) && values[candidate] == 0, -1);
        }
        if (index < 0)
        {
            return -1;
        }

        int digit = solution[index] - '0';
        values[index] = digit;
        notes[index] = 0;
        ClearPeerNote(index, digit);
        HintsUsed++;
        return index;
    }

    public bool HasConflictAt(int index)
    {
        ValidateIndex(index);
        int value = values[index];
        if (value == 0)
        {
            return false;
        }

        int row = index / 9;
        int column = index % 9;
        for (int other = 0; other < 81; other++)
        {
            if (other == index || values[other] != value)
            {
                continue;
            }

            int otherRow = other / 9;
            int otherColumn = other % 9;
            if (otherRow == row || otherColumn == column ||
                (otherRow / 3 == row / 3 && otherColumn / 3 == column / 3))
            {
                return true;
            }
        }
        return false;
    }

    public SudokuBoardFrame CaptureFrame() => new(CurrentGrid, (int[])notes.Clone());

    public void RestoreFrame(SudokuBoardFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        SudokuSolver.ValidateGrid(frame.Board, allowEmpty: true);
        if (frame.Notes is not { Length: 81 })
        {
            throw new ArgumentException("A Sudoku board frame needs 81 note masks.", nameof(frame));
        }

        char[] candidate = frame.Board.Replace('.', '0').ToCharArray();
        for (int index = 0; index < 81; index++)
        {
            if (puzzle.Givens[index] != '0' && candidate[index] != puzzle.Givens[index])
            {
                throw new ArgumentException("A board frame cannot change puzzle givens.", nameof(frame));
            }
        }
        Load(new string(candidate), frame.Notes);
    }

    public SudokuGameState Snapshot(long elapsedSeconds) => new()
    {
        PuzzleId = puzzle.Id,
        Board = CurrentGrid,
        Notes = (int[])notes.Clone(),
        ElapsedSeconds = Math.Max(0L, elapsedSeconds),
        HintsUsed = HintsUsed,
        Completed = IsComplete,
    };

    private bool CanReveal(int index) => !IsGiven(index) && !IsCorrectAt(index);

    private void Load(string board, int[] noteMasks)
    {
        for (int index = 0; index < 81; index++)
        {
            values[index] = board[index] - '0';
            notes[index] = values[index] == 0 ? noteMasks[index] & 0x3FE : 0;
        }
    }

    private void ClearPeerNote(int index, int digit)
    {
        int bit = 1 << digit;
        int row = index / 9;
        int column = index % 9;
        for (int other = 0; other < 81; other++)
        {
            int otherRow = other / 9;
            int otherColumn = other % 9;
            if (otherRow == row || otherColumn == column ||
                (otherRow / 3 == row / 3 && otherColumn / 3 == column / 3))
            {
                notes[other] &= ~bit;
            }
        }
    }

    private static void ValidateIndex(int index)
    {
        if (index is < 0 or >= 81)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
