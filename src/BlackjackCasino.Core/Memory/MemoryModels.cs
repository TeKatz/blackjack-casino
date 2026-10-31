namespace BlackjackCasino.Core.Memory;

public enum MemoryDifficulty
{
    Easy,
    Normal,
    Hard,
    Expert,
    Master,
}

public sealed record MemoryDifficultySpec(int Rows, int Columns, int Pairs)
{
    public int Cards => checked(Pairs * 2);
}

public sealed record MemorySymbol(string Id, string Glyph, string Name, string Accent);

public sealed record MemoryDeckDefinition(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<MemorySymbol> Symbols)
{
    public override string ToString() => Name;
}

public enum MemoryFlipOutcome
{
    FirstCard,
    Match,
    Mismatch,
    Completed,
    InvalidIndex,
    AlreadyMatched,
    AlreadyFaceUp,
    WaitingForMismatch,
}

public sealed record MemoryFlipResult(
    MemoryFlipOutcome Outcome,
    int Index = -1,
    int OtherIndex = -1,
    int Streak = 0)
{
    public bool Success => Outcome is MemoryFlipOutcome.FirstCard or MemoryFlipOutcome.Match
        or MemoryFlipOutcome.Mismatch or MemoryFlipOutcome.Completed;
}

public sealed class MemoryGameState
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public MemoryDifficulty Difficulty { get; set; } = MemoryDifficulty.Easy;
    public string DeckId { get; set; } = MemoryCatalog.DefaultDeckId;
    public int Seed { get; set; }
    public List<string> Cards { get; set; } = [];
    public HashSet<int> MatchedIndices { get; set; } = [];
    public List<int> FaceUpIndices { get; set; } = [];
    public int Moves { get; set; }
    public int CardsFlipped { get; set; }
    public int WrongMatches { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public long ElapsedSeconds { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public bool Completed => Cards.Count > 0 && MatchedIndices.Count == Cards.Count;
    public int PairsFound => MatchedIndices.Count / 2;
    public int PairCount => Cards.Count / 2;
    public decimal Accuracy => Moves <= 0 ? 1m : Math.Clamp((decimal)PairsFound / Moves, 0m, 1m);
    public bool Perfect => Completed && WrongMatches == 0;

    public void Normalize()
    {
        if (GameId == Guid.Empty) GameId = Guid.NewGuid();
        MemoryDifficultySpec spec = MemoryCatalog.Spec(Difficulty);
        if (!MemoryCatalog.TryGetDeck(DeckId, out MemoryDeckDefinition? deck) || deck is null)
            DeckId = MemoryCatalog.DefaultDeckId;

        if (Cards is null || Cards.Count != spec.Cards || Cards.Any(string.IsNullOrWhiteSpace))
            Cards = MemoryGame.CreateCards(Difficulty, DeckId, Seed == 0 ? 1 : Seed);

        MatchedIndices ??= [];
        MatchedIndices = MatchedIndices.Where(i => i >= 0 && i < Cards.Count).ToHashSet();
        FaceUpIndices ??= [];
        FaceUpIndices = FaceUpIndices.Where(i => i >= 0 && i < Cards.Count && !MatchedIndices.Contains(i)).Distinct().Take(2).ToList();
        if (FaceUpIndices.Count == 2 && !string.Equals(Cards[FaceUpIndices[0]], Cards[FaceUpIndices[1]], StringComparison.Ordinal))
            FaceUpIndices.Clear();

        Moves = Math.Max(0, Moves);
        CardsFlipped = Math.Max(0, CardsFlipped);
        WrongMatches = Math.Clamp(WrongMatches, 0, Moves);
        CurrentStreak = Math.Max(0, CurrentStreak);
        BestStreak = Math.Max(CurrentStreak, BestStreak);
        ElapsedSeconds = Math.Max(0, ElapsedSeconds);
        if (!Completed) CompletedAt = null;
    }
}

public sealed record MemoryCompletionRecord(
    Guid GameId,
    MemoryDifficulty Difficulty,
    string DeckId,
    int Pairs,
    int Moves,
    int CardsFlipped,
    int WrongMatches,
    int BestStreak,
    long ElapsedSeconds,
    DateTimeOffset CompletedAt)
{
    public bool Perfect => WrongMatches == 0;
    public decimal Accuracy => Moves <= 0 ? 1m : Math.Clamp((decimal)Pairs / Moves, 0m, 1m);
}

public sealed class MemoryProgressState
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public MemoryGameState? ActiveGame { get; set; }
    public List<MemoryCompletionRecord> CompletedGames { get; set; } = [];
    public string PreferredDeckId { get; set; } = MemoryCatalog.DefaultDeckId;

    public void Normalize()
    {
        if (SchemaVersion > CurrentSchemaVersion)
            throw new NotSupportedException($"Memory save schema {SchemaVersion} is newer than supported schema {CurrentSchemaVersion}.");
        SchemaVersion = CurrentSchemaVersion;
        if (!MemoryCatalog.TryGetDeck(PreferredDeckId, out _)) PreferredDeckId = MemoryCatalog.DefaultDeckId;
        CompletedGames ??= [];
        CompletedGames = CompletedGames
            .Where(x => x.GameId != Guid.Empty)
            .GroupBy(x => x.GameId)
            .Select(x => x.OrderByDescending(y => y.CompletedAt).First())
            .OrderBy(x => x.CompletedAt)
            .ToList();
        ActiveGame?.Normalize();
        if (ActiveGame?.Completed == true) ActiveGame = null;
    }
}

public sealed class MemoryStatistics
{
    public HashSet<Guid> RecordedGameIds { get; set; } = [];
    public long CompletedGames { get; set; }
    public long PairsFound { get; set; }
    public long CardsFlipped { get; set; }
    public long WrongMatches { get; set; }
    public long PerfectGames { get; set; }
    public long ExpertCompleted { get; set; }
    public long MasterCompleted { get; set; }
    public int BestStreak { get; set; }
    public decimal BestAccuracy { get; set; }
    public Dictionary<string, int> BestMovesByDifficulty { get; set; } = new(StringComparer.Ordinal);

    public void Normalize()
    {
        RecordedGameIds ??= [];
        RecordedGameIds.Remove(Guid.Empty);
        CompletedGames = Math.Max(0, CompletedGames);
        PairsFound = Math.Max(0, PairsFound);
        CardsFlipped = Math.Max(0, CardsFlipped);
        WrongMatches = Math.Max(0, WrongMatches);
        PerfectGames = Math.Max(0, PerfectGames);
        ExpertCompleted = Math.Max(0, ExpertCompleted);
        MasterCompleted = Math.Max(0, MasterCompleted);
        BestStreak = Math.Max(0, BestStreak);
        BestAccuracy = Math.Clamp(BestAccuracy, 0m, 1m);
        BestMovesByDifficulty ??= new(StringComparer.Ordinal);
        foreach (string key in BestMovesByDifficulty.Keys.ToArray())
            if (BestMovesByDifficulty[key] <= 0) BestMovesByDifficulty.Remove(key);
    }

    public bool Record(MemoryCompletionRecord completion)
    {
        Normalize();
        if (!RecordedGameIds.Add(completion.GameId)) return false;
        CompletedGames = checked(CompletedGames + 1);
        PairsFound = checked(PairsFound + Math.Max(0, completion.Pairs));
        CardsFlipped = checked(CardsFlipped + Math.Max(0, completion.CardsFlipped));
        WrongMatches = checked(WrongMatches + Math.Max(0, completion.WrongMatches));
        if (completion.Perfect) PerfectGames = checked(PerfectGames + 1);
        if (completion.Difficulty == MemoryDifficulty.Expert) ExpertCompleted = checked(ExpertCompleted + 1);
        if (completion.Difficulty == MemoryDifficulty.Master) MasterCompleted = checked(MasterCompleted + 1);
        BestStreak = Math.Max(BestStreak, completion.BestStreak);
        BestAccuracy = Math.Max(BestAccuracy, completion.Accuracy);
        string key = completion.Difficulty.ToString();
        if (completion.Moves > 0 && (!BestMovesByDifficulty.TryGetValue(key, out int best) || completion.Moves < best))
            BestMovesByDifficulty[key] = completion.Moves;
        return true;
    }
}
