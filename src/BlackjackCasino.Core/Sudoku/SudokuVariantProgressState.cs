namespace BlackjackCasino.Core.Sudoku;

/// <summary>One saved puzzle per variation/size/difficulty plus recoverable completion receipts.</summary>
public sealed record SudokuVariantSavedGame
{
    public string PuzzleId { get; init; } = "";
    public string Mode { get; init; } = "";
    public SudokuDifficulty Difficulty { get; init; } = SudokuDifficulty.Easy;
    public int MiniSize { get; init; } = 9;
    public int Index { get; init; }
    public string Board { get; init; } = "";
    public int[] Notes { get; init; } = [];
    public long ElapsedSeconds { get; init; }
    public int HintsUsed { get; init; }
    public bool Completed { get; init; }
}

public sealed record SudokuVariantProgressState
{
    public Dictionary<string, SudokuVariantSavedGame> Games { get; init; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> NextSequences { get; init; } = new(StringComparer.Ordinal);
    public List<SudokuVariantCompletion> CompletedReceipts { get; init; } = [];

    public static SudokuVariantProgressState Normalize(SudokuVariantProgressState? state)
    {
        var games = new Dictionary<string, SudokuVariantSavedGame>(StringComparer.Ordinal);
        var sequences = new Dictionary<string, int>(StringComparer.Ordinal);
        var receipts = new List<SudokuVariantCompletion>();
        if (state is null) return new SudokuVariantProgressState();
        foreach ((string key, int next) in state.NextSequences ?? [])
            if (SudokuVariantLibrary.TryParseKey(key, out _, out _, out _) && next is >= 0 and <= 9999)
                sequences[key] = next;
        foreach ((string key, SudokuVariantSavedGame saved) in state.Games ?? [])
        {
            if (saved is null || !SudokuVariantLibrary.TryParseKey(key,
                out string mode, out SudokuDifficulty difficulty, out int size) ||
                saved.Mode != mode || saved.Difficulty != difficulty || saved.MiniSize != size ||
                saved.Index is < 0 or > 9999 || saved.Board is null)
                continue;
            // The trusted factory reconstructs clues, solution and region rules. Never accept
            // a forged board or a variant whose schema was upgraded incompatibly.
            SudokuVariantPuzzle puzzle = SudokuVariantLibrary.Create(mode, difficulty, size, saved.Index);
            if (saved.PuzzleId != puzzle.Id || saved.Board.Length != puzzle.Givens.Length)
                continue;
            char[] board = saved.Board.ToCharArray();
            bool valid = true;
            for (int i = 0; i < board.Length; i++)
            {
                if (puzzle.Givens[i] == '-')
                    valid &= board[i] == '-';
                else if (puzzle.Givens[i] != '0')
                    valid &= board[i] == puzzle.Givens[i];
                else
                    valid &= board[i] is >= '0' and <= '9' && board[i] <= '0' + puzzle.Digits;
            }
            if (!valid) continue;
            int[] notes = saved.Notes is { } raw && raw.Length == board.Length
                ? raw.Select(v => v & ((1 << (puzzle.Digits + 1)) - 2)).ToArray()
                : new int[board.Length];
            for (int i = 0; i < board.Length; i++)
                if (board[i] != '0') notes[i] = 0;
            games[key] = saved with
            {
                Notes = notes,
                ElapsedSeconds = Math.Max(0, saved.ElapsedSeconds),
                HintsUsed = Math.Max(0, saved.HintsUsed),
                Completed = saved.Board == puzzle.Solution,
            };
        }
        foreach (SudokuVariantCompletion record in state.CompletedReceipts ?? [])
        {
            if (record is null || record.PuzzleId is null ||
                !SudokuVariantLibrary.Supports(record.Mode, record.Difficulty, record.MiniSize) ||
                receipts.Any(r => r.PuzzleId == record.PuzzleId))
                continue;
            receipts.Add(record with { ElapsedSeconds = Math.Max(0, record.ElapsedSeconds),
                HintsUsed = Math.Max(0, record.HintsUsed) });
        }
        return new SudokuVariantProgressState { Games = games, NextSequences = sequences,
            CompletedReceipts = receipts };
    }
}
