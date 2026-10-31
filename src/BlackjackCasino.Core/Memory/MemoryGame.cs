namespace BlackjackCasino.Core.Memory;

public static class MemoryGame
{
    public static MemoryGameState Start(
        MemoryDifficulty difficulty,
        string? deckId = null,
        int? seed = null,
        DateTimeOffset? startedAt = null)
    {
        string selectedDeck = MemoryCatalog.TryGetDeck(deckId, out _) ? deckId! : MemoryCatalog.DefaultDeckId;
        int selectedSeed = seed ?? Random.Shared.Next(1, int.MaxValue);
        return new MemoryGameState
        {
            GameId = Guid.NewGuid(),
            Difficulty = difficulty,
            DeckId = selectedDeck,
            Seed = selectedSeed,
            Cards = CreateCards(difficulty, selectedDeck, selectedSeed),
            StartedAt = startedAt ?? DateTimeOffset.UtcNow,
        };
    }

    public static List<string> CreateCards(MemoryDifficulty difficulty, string deckId, int seed)
    {
        MemoryDifficultySpec spec = MemoryCatalog.Spec(difficulty);
        MemoryDeckDefinition deck = MemoryCatalog.GetDeck(deckId);
        if (deck.Symbols.Count < spec.Pairs)
            throw new InvalidOperationException($"Deck '{deck.Id}' does not contain enough symbols for {difficulty}.");

        var random = new Random(seed);
        MemorySymbol[] available = deck.Symbols.ToArray();
        Shuffle(available, random);
        string[] chosen = available.Take(spec.Pairs).Select(x => x.Id).ToArray();
        var cards = chosen.Concat(chosen).ToArray();
        Shuffle(cards, random);
        return [.. cards];
    }

    public static MemoryFlipResult Flip(MemoryGameState game, int index, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (index < 0 || index >= game.Cards.Count) return new(MemoryFlipOutcome.InvalidIndex, index);
        if (game.MatchedIndices.Contains(index)) return new(MemoryFlipOutcome.AlreadyMatched, index);
        if (game.FaceUpIndices.Contains(index)) return new(MemoryFlipOutcome.AlreadyFaceUp, index);
        if (game.FaceUpIndices.Count >= 2) return new(MemoryFlipOutcome.WaitingForMismatch, index);

        game.FaceUpIndices.Add(index);
        game.CardsFlipped = checked(game.CardsFlipped + 1);
        if (game.FaceUpIndices.Count == 1)
            return new(MemoryFlipOutcome.FirstCard, index, Streak: game.CurrentStreak);

        int first = game.FaceUpIndices[0];
        int second = game.FaceUpIndices[1];
        game.Moves = checked(game.Moves + 1);
        if (string.Equals(game.Cards[first], game.Cards[second], StringComparison.Ordinal))
        {
            game.MatchedIndices.Add(first);
            game.MatchedIndices.Add(second);
            game.FaceUpIndices.Clear();
            game.CurrentStreak = checked(game.CurrentStreak + 1);
            game.BestStreak = Math.Max(game.BestStreak, game.CurrentStreak);
            if (game.Completed)
            {
                game.CompletedAt = now ?? DateTimeOffset.UtcNow;
                return new(MemoryFlipOutcome.Completed, second, first, game.CurrentStreak);
            }
            return new(MemoryFlipOutcome.Match, second, first, game.CurrentStreak);
        }

        game.WrongMatches = checked(game.WrongMatches + 1);
        game.CurrentStreak = 0;
        return new(MemoryFlipOutcome.Mismatch, second, first);
    }

    public static bool ResolveMismatch(MemoryGameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (game.FaceUpIndices.Count != 2) return false;
        int first = game.FaceUpIndices[0];
        int second = game.FaceUpIndices[1];
        if (first < 0 || second < 0 || first >= game.Cards.Count || second >= game.Cards.Count)
        {
            game.FaceUpIndices.Clear();
            return true;
        }
        if (string.Equals(game.Cards[first], game.Cards[second], StringComparison.Ordinal)) return false;
        game.FaceUpIndices.Clear();
        return true;
    }

    public static MemoryCompletionRecord Complete(MemoryGameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        game.Normalize();
        if (!game.Completed) throw new InvalidOperationException("Memory game is not complete.");
        game.CompletedAt ??= DateTimeOffset.UtcNow;
        return new(
            game.GameId,
            game.Difficulty,
            game.DeckId,
            game.PairCount,
            game.Moves,
            game.CardsFlipped,
            game.WrongMatches,
            game.BestStreak,
            game.ElapsedSeconds,
            game.CompletedAt.Value);
    }

    private static void Shuffle<T>(T[] values, Random random)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int other = random.Next(i + 1);
            (values[i], values[other]) = (values[other], values[i]);
        }
    }
}
