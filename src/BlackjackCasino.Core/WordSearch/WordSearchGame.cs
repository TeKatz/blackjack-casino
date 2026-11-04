namespace BlackjackCasino.Core.WordSearch;

public static class WordSearchGame
{
    public static WordSearchGameState Start(WordSearchPuzzleDefinition definition, DateTimeOffset? now = null, string? language = null)
    {
        return new WordSearchGameState
        {
            Puzzle = WordSearchGenerator.Generate(WordSearchCatalog.ForLanguage(definition, language)),
            StartedAt = now ?? DateTimeOffset.UtcNow,
        };
    }

    public static IReadOnlyList<WordSearchCell> SelectionCells(WordSearchPuzzle puzzle, WordSearchCell start, WordSearchCell end)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        if (!Inside(puzzle, start) || !Inside(puzzle, end)) return [];
        int rowDistance = end.Row - start.Row;
        int columnDistance = end.Column - start.Column;
        if (rowDistance != 0 && columnDistance != 0 && Math.Abs(rowDistance) != Math.Abs(columnDistance)) return [];
        if (rowDistance == 0 && columnDistance == 0) return [start];

        int rowStep = Math.Sign(rowDistance);
        int columnStep = Math.Sign(columnDistance);
        int length = Math.Max(Math.Abs(rowDistance), Math.Abs(columnDistance)) + 1;
        return Enumerable.Range(0, length)
            .Select(i => new WordSearchCell(start.Row + rowStep * i, start.Column + columnStep * i))
            .ToArray();
    }

    public static bool TryFind(
        WordSearchGameState state,
        WordSearchCell start,
        WordSearchCell end,
        out string? foundWord)
    {
        ArgumentNullException.ThrowIfNull(state);
        IReadOnlyList<WordSearchCell> selection = SelectionCells(state.Puzzle, start, end);
        if (selection.Count < 2) { foundWord = null; return false; }

        foreach (WordSearchPlacement placement in state.Puzzle.Placements)
        {
            if (state.FoundWords.Contains(placement.Word)) continue;
            IReadOnlyList<WordSearchCell> target = placement.Cells();
            bool forward = selection.SequenceEqual(target);
            bool backward = !forward && selection.SequenceEqual(target.Reverse());
            if (!forward && !backward) continue;
            state.FoundWords.Add(placement.Word);
            if (state.Completed && state.CompletedAt is null) state.CompletedAt = DateTimeOffset.UtcNow;
            foundWord = placement.Word;
            return true;
        }

        foundWord = null;
        return false;
    }

    public static WordSearchPlacement? Hint(WordSearchGameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        WordSearchPlacement? result = state.Puzzle.Placements
            .FirstOrDefault(x => !state.FoundWords.Contains(x.Word));
        if (result is not null) state.HintsUsed = checked(state.HintsUsed + 1);
        return result;
    }

    public static WordSearchCompletionRecord Complete(WordSearchGameState state, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!state.Completed) throw new InvalidOperationException("The Word Search puzzle is not complete.");
        DateTimeOffset completed = state.CompletedAt ?? now ?? DateTimeOffset.UtcNow;
        state.CompletedAt = completed;
        long elapsed = Math.Max(0, state.ElapsedSeconds);
        state.ElapsedSeconds = elapsed;
        return new(state.Puzzle.Id, state.Puzzle.Difficulty, state.FoundWords.Count, state.HintsUsed, elapsed, completed);
    }

    public static WordSearchPuzzleDefinition NextPuzzle(WordSearchProgressState progress, WordSearchDifficulty difficulty)
    {
        ArgumentNullException.ThrowIfNull(progress);
        progress.Normalize();
        HashSet<string> completed = progress.CompletedGames.Select(x => x.PuzzleId).ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<WordSearchPuzzleDefinition> pool = WordSearchCatalog.For(difficulty);
        return pool.FirstOrDefault(x => !completed.Contains(x.Id)) ?? pool[0];
    }

    private static bool Inside(WordSearchPuzzle puzzle, WordSearchCell cell) =>
        cell.Row >= 0 && cell.Row < puzzle.Size && cell.Column >= 0 && cell.Column < puzzle.Size;
}
