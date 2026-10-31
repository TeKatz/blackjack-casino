namespace BlackjackCasino.Core.Solitaire;

public enum SolitaireVariant
{
    Klondike,
    FreeCell,
    Spider,
    Pyramid,
    Golf,
    TriPeaks,
}

public sealed class SolitaireCardState
{
    public Card Card { get; set; }
    public bool FaceUp { get; set; }

    public SolitaireCardState() { }

    public SolitaireCardState(Card card, bool faceUp)
    {
        Card = card;
        FaceUp = faceUp;
    }
}

public sealed class SolitaireGameState
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public SolitaireVariant Variant { get; set; }
    public int DealNumber { get; set; } = 1;
    public int SpiderSuits { get; set; } = 1;
    public long ElapsedSeconds { get; set; }
    public int Moves { get; set; }
    public int CompletedRuns { get; set; }
    public bool Won { get; set; }
    public Dictionary<string, List<SolitaireCardState>> Piles { get; set; } = new(StringComparer.Ordinal);

    public SolitaireGameState Normalize()
    {
        if (GameId == Guid.Empty) GameId = Guid.NewGuid();
        DealNumber = Math.Max(1, DealNumber);
        SpiderSuits = SpiderSuits is 1 or 2 or 4 ? SpiderSuits : 1;
        ElapsedSeconds = Math.Max(0L, ElapsedSeconds);
        Moves = Math.Max(0, Moves);
        CompletedRuns = Math.Clamp(CompletedRuns, 0, 8);
        Piles ??= new(StringComparer.Ordinal);
        foreach (string key in Piles.Keys.ToArray())
        {
            Piles[key] ??= [];
        }
        return this;
    }
}

public sealed class SolitaireProgressState
{
    public int SchemaVersion { get; set; } = 1;
    public int NextDealNumber { get; set; } = 1;
    public Dictionary<string, SolitaireGameState> ActiveGames { get; set; } = new(StringComparer.Ordinal);

    public static SolitaireProgressState Normalize(SolitaireProgressState? state)
    {
        state ??= new();
        state.SchemaVersion = Math.Max(1, state.SchemaVersion);
        state.NextDealNumber = Math.Max(1, state.NextDealNumber);
        state.ActiveGames ??= new(StringComparer.Ordinal);
        foreach (string key in state.ActiveGames.Keys.ToArray())
        {
            SolitaireGameState? game = state.ActiveGames[key];
            if (game is null)
            {
                state.ActiveGames.Remove(key);
                continue;
            }
            game.Normalize();
        }
        return state;
    }

    public static string GameKey(SolitaireVariant variant, int spiderSuits = 1) =>
        variant == SolitaireVariant.Spider ? $"spider-{(spiderSuits is 1 or 2 or 4 ? spiderSuits : 1)}" : variant.ToString().ToLowerInvariant();

    public bool TryGet(SolitaireVariant variant, out SolitaireGameState? game, int spiderSuits = 1) =>
        ActiveGames.TryGetValue(GameKey(variant, spiderSuits), out game);

    public void Save(SolitaireGameState game)
    {
        ArgumentNullException.ThrowIfNull(game);
        game.Normalize();
        ActiveGames[GameKey(game.Variant, game.SpiderSuits)] = game;
        NextDealNumber = Math.Max(NextDealNumber, game.DealNumber + 1);
    }
}

public sealed record SolitaireStartRecord(Guid GameId, SolitaireVariant Variant, int SpiderSuits, int DealNumber);
public sealed record SolitaireAbandonRecord(Guid GameId, SolitaireVariant Variant, int SpiderSuits, int DealNumber, long ElapsedSeconds, int Moves);
public sealed record SolitaireCompletionRecord(Guid GameId, SolitaireVariant Variant, int SpiderSuits, int DealNumber, long ElapsedSeconds, int Moves);

public sealed class SolitaireStatistics
{
    public long GamesStarted { get; set; }
    public long Wins { get; set; }
    public long GamesAbandoned { get; set; }
    public long KlondikeWins { get; set; }
    public long FreeCellWins { get; set; }
    public long SpiderWins { get; set; }
    public long SpiderOneSuitWins { get; set; }
    public long SpiderTwoSuitWins { get; set; }
    public long SpiderFourSuitWins { get; set; }
    public long PyramidWins { get; set; }
    public long GolfWins { get; set; }
    public long TriPeaksWins { get; set; }
    public long TotalMovesInWins { get; set; }
    public long TotalWinSeconds { get; set; }
    public long FastestWinSeconds { get; set; }
    public int FewestMoves { get; set; }
    public int CurrentWinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public HashSet<Guid> StartedGameIds { get; set; } = [];
    public HashSet<Guid> CompletedGameIds { get; set; } = [];
    public HashSet<Guid> AbandonedGameIds { get; set; } = [];

    public decimal WinRate => GamesStarted <= 0 ? 0m : Math.Round((decimal)Wins / GamesStarted * 100m, 1);

    public void Normalize()
    {
        StartedGameIds ??= [];
        CompletedGameIds ??= [];
        AbandonedGameIds ??= [];
        StartedGameIds.Remove(Guid.Empty);
        CompletedGameIds.Remove(Guid.Empty);
        AbandonedGameIds.Remove(Guid.Empty);
        GamesStarted = Math.Max(0, GamesStarted);
        Wins = Math.Max(0, Wins);
        GamesAbandoned = Math.Max(0, GamesAbandoned);
        CurrentWinStreak = Math.Max(0, CurrentWinStreak);
        BestWinStreak = Math.Max(BestWinStreak, CurrentWinStreak);
    }

    public bool RecordStart(SolitaireStartRecord start)
    {
        ArgumentNullException.ThrowIfNull(start);
        Normalize();
        if (start.GameId == Guid.Empty || !StartedGameIds.Add(start.GameId)) return false;
        GamesStarted++;
        return true;
    }

    public bool RecordAbandon(SolitaireAbandonRecord abandon)
    {
        ArgumentNullException.ThrowIfNull(abandon);
        Normalize();
        if (abandon.GameId == Guid.Empty || CompletedGameIds.Contains(abandon.GameId) || !AbandonedGameIds.Add(abandon.GameId)) return false;
        if (StartedGameIds.Add(abandon.GameId)) GamesStarted++;
        GamesAbandoned++;
        CurrentWinStreak = 0;
        return true;
    }

    public bool RecordWin(SolitaireCompletionRecord completion)
    {
        ArgumentNullException.ThrowIfNull(completion);
        Normalize();
        if (completion.GameId == Guid.Empty || !CompletedGameIds.Add(completion.GameId)) return false;
        if (StartedGameIds.Add(completion.GameId)) GamesStarted++;
        AbandonedGameIds.Remove(completion.GameId);
        Wins++;
        TotalMovesInWins += Math.Max(0, completion.Moves);
        TotalWinSeconds += Math.Max(0L, completion.ElapsedSeconds);
        if (completion.ElapsedSeconds > 0 && (FastestWinSeconds == 0 || completion.ElapsedSeconds < FastestWinSeconds))
            FastestWinSeconds = completion.ElapsedSeconds;
        if (completion.Moves > 0 && (FewestMoves == 0 || completion.Moves < FewestMoves))
            FewestMoves = completion.Moves;
        CurrentWinStreak++;
        BestWinStreak = Math.Max(BestWinStreak, CurrentWinStreak);

        switch (completion.Variant)
        {
            case SolitaireVariant.Klondike: KlondikeWins++; break;
            case SolitaireVariant.FreeCell: FreeCellWins++; break;
            case SolitaireVariant.Spider:
                SpiderWins++;
                if (completion.SpiderSuits == 4) SpiderFourSuitWins++;
                else if (completion.SpiderSuits == 2) SpiderTwoSuitWins++;
                else SpiderOneSuitWins++;
                break;
            case SolitaireVariant.Pyramid: PyramidWins++; break;
            case SolitaireVariant.Golf: GolfWins++; break;
            case SolitaireVariant.TriPeaks: TriPeaksWins++; break;
        }
        return true;
    }

    public long WinsFor(SolitaireVariant variant) => variant switch
    {
        SolitaireVariant.Klondike => KlondikeWins,
        SolitaireVariant.FreeCell => FreeCellWins,
        SolitaireVariant.Spider => SpiderWins,
        SolitaireVariant.Pyramid => PyramidWins,
        SolitaireVariant.Golf => GolfWins,
        SolitaireVariant.TriPeaks => TriPeaksWins,
        _ => 0,
    };
}

public sealed record SolitaireAchievementDefinition(string Id, string Name, string Description, Func<SolitaireStatistics, bool> Complete);

public static class SolitaireAchievementCatalog
{
    public static IReadOnlyList<SolitaireAchievementDefinition> All { get; } = Array.AsReadOnly(new[]
    {
        A("solitaire-first", "First Quiet Victory", "Win any Midnight Solitaire game.", s => s.Wins >= 1),
        A("solitaire-10", "Card Table Regular", "Win 10 Solitaire games.", s => s.Wins >= 10),
        A("solitaire-50", "After Hours", "Win 50 Solitaire games.", s => s.Wins >= 50),
        A("solitaire-250", "Midnight Patience", "Win 250 Solitaire games.", s => s.Wins >= 250),
        A("solitaire-klondike", "Classic Hand", "Win Klondike.", s => s.KlondikeWins >= 1),
        A("solitaire-freecell", "Open Information", "Win FreeCell.", s => s.FreeCellWins >= 1),
        A("solitaire-spider", "Eight Runs", "Win Spider.", s => s.SpiderWins >= 1),
        A("solitaire-spider-four", "Four-Suit Web", "Win four-suit Spider.", s => s.SpiderFourSuitWins >= 1),
        A("solitaire-pyramid", "Desert Night", "Win Pyramid.", s => s.PyramidWins >= 1),
        A("solitaire-golf", "Under Par", "Win Golf.", s => s.GolfWins >= 1),
        A("solitaire-tripeaks", "Three Peaks", "Win TriPeaks.", s => s.TriPeaksWins >= 1),
        A("solitaire-all", "The Full Cabinet", "Win every Solitaire variant at least once.", s =>
            s.KlondikeWins > 0 && s.FreeCellWins > 0 && s.SpiderWins > 0 && s.PyramidWins > 0 && s.GolfWins > 0 && s.TriPeaksWins > 0),
    });

    public static int Completed(SolitaireStatistics stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        return All.Count(a => a.Complete(stats));
    }

    private static SolitaireAchievementDefinition A(string id, string name, string description, Func<SolitaireStatistics, bool> complete) =>
        new(id, name, description, complete);
}
