using System.Diagnostics;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Memory;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Keeps the original C# Memory rules, cards, saves, challenge receipts and lifetime stats.
/// The browser sees symbols only for genuinely face-up or matched cards.
/// </summary>
internal sealed class ModernMemoryService
{
    private MemoryProgressState progress;
    private MemoryGameState? game;
    private MemoryGameState? completedGame;
    private readonly Action<MemoryProgressState> persist;
    private readonly Func<MemoryStatistics> stats;
    private readonly Func<MemoryCompletionRecord, ProgressionAward> reward;
    private readonly Stopwatch clock = new();
    private readonly Stopwatch mismatch = new();
    private long elapsedBase;
    private long lastPersistedSeconds;
    private string notice = "", rewardNotice = "";

    internal ModernMemoryService(MemoryProgressState saved,
        Action<MemoryProgressState> persist,
        Func<MemoryStatistics> stats,
        Func<MemoryCompletionRecord, ProgressionAward> reward)
    {
        saved.Normalize();
        progress = CloneProgress(saved);
        this.persist = persist;
        this.stats = stats;
        this.reward = reward;
    }

    internal MemoryGameState? CurrentGame => game;
    internal string PreferredDeckId => progress.PreferredDeckId;
    internal long CurrentSeconds => elapsedBase + (long)Math.Floor(clock.Elapsed.TotalSeconds);

    internal object Snapshot()
    {
        MemoryStatistics lifetime = stats();
        lifetime.Normalize();
        var decks = MemoryCatalog.Decks.Select(deck => new
        {
            deck.Id, deck.Name, deck.Description,
            Preview = deck.Symbols.Take(4).Select(symbol => new
            {
                symbol.Glyph, symbol.Name, symbol.Accent,
            }).ToArray(),
        }).ToArray();
        var difficulties = Enum.GetValues<MemoryDifficulty>().Select(difficulty =>
        {
            MemoryDifficultySpec spec = MemoryCatalog.Spec(difficulty);
            lifetime.BestMovesByDifficulty.TryGetValue(difficulty.ToString(), out int best);
            return new
            {
                Difficulty = difficulty.ToString(), spec.Rows, spec.Columns,
                spec.Pairs, RewardXp = MemoryProgression.PlayerExperience(difficulty),
                BestMoves = best,
            };
        }).ToArray();
        object? current = null;
        MemoryGameState? visibleGame = game ?? completedGame;
        if (visibleGame is not null)
        {
            MemoryDeckDefinition deck = MemoryCatalog.GetDeck(visibleGame.DeckId);
            var symbolById = deck.Symbols.ToDictionary(symbol => symbol.Id, StringComparer.Ordinal);
            current = new
            {
                GameId = visibleGame.GameId.ToString("D"),
                Difficulty = visibleGame.Difficulty.ToString(),
                DeckId = visibleGame.DeckId, DeckName = deck.Name,
                Rows = MemoryCatalog.Spec(visibleGame.Difficulty).Rows,
                Columns = MemoryCatalog.Spec(visibleGame.Difficulty).Columns,
                Cards = visibleGame.Cards.Select((id, index) =>
                {
                    bool matched = visibleGame.MatchedIndices.Contains(index);
                    bool faceUp = matched || visibleGame.FaceUpIndices.Contains(index);
                    MemorySymbol? symbol = faceUp ? symbolById[id] : null;
                    return new
                    {
                        Index = index, Matched = matched, FaceUp = faceUp,
                        Glyph = symbol?.Glyph, Name = symbol?.Name,
                        Accent = symbol?.Accent,
                    };
                }).ToArray(),
                PairsFound = visibleGame.PairsFound, visibleGame.PairCount,
                visibleGame.Moves, visibleGame.CardsFlipped, visibleGame.WrongMatches,
                visibleGame.CurrentStreak, visibleGame.BestStreak,
                Accuracy = (double)visibleGame.Accuracy,
                ElapsedSeconds = game is null ? visibleGame.ElapsedSeconds : CurrentSeconds,
                visibleGame.Completed, visibleGame.Perfect,
                ResolvingMismatch = game is not null && mismatch.IsRunning,
                RewardXp = MemoryProgression.PlayerExperience(visibleGame.Difficulty),
            };
        }

        return new
        {
            Screen = completedGame is not null ? "complete" : game is not null ? "game" : "library",
            PreferredDeckId = progress.PreferredDeckId,
            Decks = decks,
            Difficulties = difficulties,
            Stats = new
            {
                lifetime.CompletedGames, lifetime.PairsFound, lifetime.CardsFlipped,
                lifetime.WrongMatches, lifetime.PerfectGames, lifetime.ExpertCompleted,
                lifetime.MasterCompleted, lifetime.BestStreak,
                BestAccuracy = (double)lifetime.BestAccuracy,
            },
            Resume = progress.ActiveGame is { } saved ? new
            {
                Difficulty = saved.Difficulty.ToString(), saved.DeckId,
                PairsFound = saved.PairsFound, saved.PairCount,
                saved.Moves, saved.ElapsedSeconds,
            } : null,
            Game = current, Notice = notice, RewardNotice = rewardNotice,
        };
    }

    internal (bool Success, string Message) Execute(ModernMemoryCommand command)
    {
        switch (command.Action)
        {
            case "memory.deck":
                if (game is not null || completedGame is not null || progress.ActiveGame is not null)
                    return (false, "Finish or resume your active Memory game before switching decks.");
                if (command.DeckId == progress.PreferredDeckId)
                    return (true, "Deck already selected.");
                MemoryProgressState selected = CloneProgress(progress);
                selected.PreferredDeckId = command.DeckId!;
                persist(selected);
                progress = selected;
                notice = "Card deck selected.";
                return (true, notice);
            case "memory.start":
                if (game is not null || completedGame is not null || progress.ActiveGame is not null)
                    return (false, "Resume or finish the current Memory table first.");
                return Start(command.Difficulty!.Value, progress.PreferredDeckId);
            case "memory.resume":
                if (game is not null || completedGame is not null)
                    return (false, "Return to the Memory cabinet first.");
                if (progress.ActiveGame is not { } saved)
                    return (false, "No Memory table to resume.");
                Open(CloneGame(saved));
                return (true, "Saved Memory table restored.");
            case "memory.again":
                if (completedGame is not { } won || game is not null || progress.ActiveGame is not null)
                    return (false, "Complete the current table before playing again.");
                return Start(won.Difficulty, won.DeckId);
            case "memory.back":
                if (game is null && completedGame is null)
                    return (false, "There is no Memory table to leave.");
                if (game is not null)
                {
                    if (mismatch.IsRunning) ResolveMismatch();
                    SaveElapsed();
                    clock.Stop();
                }
                game = null;
                completedGame = null;
                mismatch.Reset();
                notice = "";
                rewardNotice = "";
                return (true, "Returned to Memory cabinet.");
            case "memory.exit":
                return game is null && completedGame is null
                    ? (true, "Returned to Chill Lounge.")
                    : (false, "Return to Memory cabinet first.");
            case "memory.flip":
                return Flip(command.Index!.Value);
            default:
                return (false, "Unsupported Memory action.");
        }
    }

    private (bool, string) Start(MemoryDifficulty difficulty, string deckId)
    {
        MemoryGameState created = MemoryGame.Start(difficulty, deckId);
        MemoryProgressState next = CloneProgress(progress);
        next.ActiveGame = CloneGame(created);
        next.PreferredDeckId = deckId;
        persist(next);
        progress = next;
        Open(created);
        return (true, "Fresh Memory table ready.");
    }

    private void Open(MemoryGameState next)
    {
        game = next;
        completedGame = null;
        mismatch.Reset();
        elapsedBase = next.ElapsedSeconds;
        lastPersistedSeconds = elapsedBase;
        clock.Restart();
        notice = "Flip two cards and find their match.";
        rewardNotice = "";
    }

    private (bool, string) Flip(int index)
    {
        if (game is null || completedGame is not null) return (false, "Choose a Memory table first.");
        if (mismatch.IsRunning) return (false, "Remember these two cards before they turn over.");
        MemoryGameState candidate = CloneGame(game);
        MemoryFlipResult flip = MemoryGame.Flip(candidate, index);
        if (!flip.Success) return (false, "This card cannot be turned now.");
        candidate.ElapsedSeconds = CurrentSeconds;
        if (flip.Outcome == MemoryFlipOutcome.Completed)
        {
            MemoryCompletionRecord completed = MemoryGame.Complete(candidate);
            MemoryProgressState finished = CloneProgress(progress);
            if (finished.CompletedGames.All(c => c.GameId != completed.GameId))
                finished.CompletedGames.Add(completed);
            finished.ActiveGame = null;
            persist(finished);
            progress = finished;
            completedGame = candidate;
            game = null;
            clock.Stop();
            mismatch.Reset();
            lastPersistedSeconds = candidate.ElapsedSeconds;
            notice = candidate.Perfect ? "PERFECT MEMORY · All pairs found!" : "TABLE CLEARED · All pairs found!";
            ProgressionAward award = reward(completed);
            rewardNotice = award.PlayerExperience > 0
                ? $"+{award.PlayerExperience:N0} player XP · +{award.SeasonExperience:N0} season XP"
                : "Table complete and saved.";
            return (true, notice);
        }

        PersistActive(candidate);
        switch (flip.Outcome)
        {
            case MemoryFlipOutcome.FirstCard:
                notice = "One card open · choose its match.";
                break;
            case MemoryFlipOutcome.Match:
                notice = $"PAIR FOUND · ×{flip.Streak}";
                break;
            case MemoryFlipOutcome.Mismatch:
                mismatch.Restart();
                notice = "Not a pair · remember where they were.";
                break;
        }
        return (true, notice);
    }

    private void PersistActive(MemoryGameState candidate)
    {
        MemoryProgressState updated = CloneProgress(progress);
        updated.ActiveGame = CloneGame(candidate);
        persist(updated);
        progress = updated;
        game = candidate;
        lastPersistedSeconds = candidate.ElapsedSeconds;
    }

    private void ResolveMismatch()
    {
        if (game is null || !mismatch.IsRunning) return;
        MemoryGameState candidate = CloneGame(game);
        if (!MemoryGame.ResolveMismatch(candidate)) { mismatch.Reset(); return; }
        candidate.ElapsedSeconds = CurrentSeconds;
        PersistActive(candidate);
        mismatch.Reset();
        notice = "Cards turned face down · try another pair.";
    }

    /// <summary>Called by the native host every second, not by a browser timer or forged event.</summary>
    internal void CheckpointClock()
    {
        if (game is null) return;
        if (mismatch.IsRunning && mismatch.ElapsedMilliseconds >= 760)
            ResolveMismatch();
        if (CurrentSeconds - lastPersistedSeconds >= 10)
            SaveElapsed();
    }

    private void SaveElapsed()
    {
        if (game is null) return;
        long seconds = CurrentSeconds;
        if (seconds <= lastPersistedSeconds) return;
        MemoryGameState candidate = CloneGame(game);
        candidate.ElapsedSeconds = seconds;
        PersistActive(candidate);
    }

    internal void Stop()
    {
        if (mismatch.IsRunning) ResolveMismatch();
        SaveElapsed();
        clock.Stop();
    }

    internal static MemoryGameState CloneGame(MemoryGameState source) => new()
    {
        GameId = source.GameId, Difficulty = source.Difficulty, DeckId = source.DeckId,
        Seed = source.Seed, Cards = source.Cards.ToList(),
        MatchedIndices = new HashSet<int>(source.MatchedIndices),
        FaceUpIndices = source.FaceUpIndices.ToList(),
        Moves = source.Moves, CardsFlipped = source.CardsFlipped,
        WrongMatches = source.WrongMatches, CurrentStreak = source.CurrentStreak,
        BestStreak = source.BestStreak, ElapsedSeconds = source.ElapsedSeconds,
        StartedAt = source.StartedAt, CompletedAt = source.CompletedAt,
    };

    private static MemoryProgressState CloneProgress(MemoryProgressState source) => new()
    {
        SchemaVersion = source.SchemaVersion, PreferredDeckId = source.PreferredDeckId,
        ActiveGame = source.ActiveGame is null ? null : CloneGame(source.ActiveGame),
        CompletedGames = source.CompletedGames.ToList(),
    };
}
