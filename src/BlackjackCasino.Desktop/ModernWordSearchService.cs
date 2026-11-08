using System.Diagnostics;
using BlackjackCasino.Core;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Native-authoritative Word Search; existing serialized grids survive the move from WPF.
/// All puzzle selection, found-word validation, hint selection, saves and rewards stay in C#.
/// </summary>
internal sealed class ModernWordSearchService
{
    private WordSearchProgressState progress;
    private WordSearchGameState? game;
    private readonly Action<WordSearchProgressState> persist;
    private readonly Func<WordSearchStatistics> statistics;
    private readonly Func<WordSearchCompletionRecord, ProgressionAward> complete;
    private readonly Func<string> language;
    private readonly Action<bool>? foundSound;
    private readonly Stopwatch clock = new();
    private long elapsedBase, lastPersistedSeconds;
    private WordSearchPlacement? hint;
    private string notice = "", rewardNotice = "";
    private (string Word, bool IsNew, string Id)? lastFind;

    internal ModernWordSearchService(WordSearchProgressState initial,
        Action<WordSearchProgressState> persist,
        Func<WordSearchStatistics> statistics,
        Func<WordSearchCompletionRecord, ProgressionAward> complete,
        Func<string> language, Action<bool>? foundSound = null)
    {
        initial.Normalize();
        progress = CloneProgress(initial);
        this.persist = persist;
        this.statistics = statistics;
        this.complete = complete;
        this.language = language;
        this.foundSound = foundSound;
    }

    internal WordSearchGameState? CurrentGame => game;
    internal long CurrentSeconds => elapsedBase + (long)Math.Floor(clock.Elapsed.TotalSeconds);

    internal object Snapshot()
    {
        WordSearchStatistics stats = statistics();
        stats.Normalize();
        var difficulties = Enum.GetValues<WordSearchDifficulty>().Select(difficulty =>
        {
            (int size, int count) = WordSearchCatalog.Spec(difficulty);
            return new
            {
                Difficulty = difficulty.ToString(), Size = size, WordCount = count,
                Total = WordSearchCatalog.For(difficulty).Count,
                Completed = progress.CompletedGames.Count(x => x.Difficulty == difficulty),
                RewardXp = WordSearchProgression.PlayerExperience(difficulty),
            };
        }).ToArray();
        object? active = null;
        if (game is not null)
        {
            WordSearchGameState current = game;
            var placements = current.Puzzle.Placements;
            active = new
            {
                current.Puzzle.Id, Difficulty = current.Puzzle.Difficulty.ToString(),
                current.Puzzle.Theme, current.Puzzle.Size, current.Puzzle.Grid,
                Words = placements.Select(x => x.Word).ToArray(),
                Found = current.FoundWords.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                FoundPaths = placements.Where(x => current.FoundWords.Contains(x.Word)).Select(x => new
                {
                    x.Word, x.StartRow, x.StartColumn, x.EndRow, x.EndColumn,
                }).ToArray(),
                Hint = hint is null ? null : new { hint.Word, hint.StartRow, hint.StartColumn },
                current.HintsUsed, ElapsedSeconds = CurrentSeconds, current.Completed,
                RewardXp = WordSearchProgression.PlayerExperience(current.Puzzle.Difficulty),
            };
        }
        return new
        {
            Screen = game is null ? "library" : "game",
            DiscoveredWords = progress.DiscoveredWords
                .OrderByDescending(entry => entry.DiscoveredAt)
                .ThenBy(entry => entry.Word, StringComparer.Ordinal)
                .Select(entry => new
                {
                    entry.Word, entry.Theme, Difficulty = entry.Difficulty.ToString(),
                    entry.PuzzleId, entry.DiscoveredAt,
                }).ToArray(),
            LastFind = lastFind is { } fresh ? new
                { fresh.Word, New = fresh.IsNew, fresh.Id } : null,
            CatalogTotal = WordSearchCatalog.All.Count,
            ThemeTotal = WordSearchCatalog.All.Select(x => x.Theme).Distinct(StringComparer.Ordinal).Count(),
            CompletedTotal = progress.CompletedGames.Count,
            Difficulties = difficulties,
            Stats = new
            {
                stats.CompletedPuzzles, stats.WordsFound, stats.NoHintCompletions,
                stats.ExpertCompleted, stats.MasterCompleted,
            },
            Resume = progress.ActiveGame is { } saved ? new
            {
                saved.Puzzle.Id, Difficulty = saved.Puzzle.Difficulty.ToString(),
                saved.Puzzle.Theme, Found = saved.FoundWords.Count,
                Total = saved.Puzzle.Placements.Count, saved.ElapsedSeconds,
            } : null,
            Game = active, Notice = notice, RewardNotice = rewardNotice,
        };
    }

    internal (bool Success, string Message) Execute(ModernWordSearchCommand command)
    {
        switch (command.Action)
        {
            case "word-search.start":
                return Start(command.Difficulty!.Value);
            case "word-search.resume":
                if (game is not null) return (false, "Return to the Word Search library first.");
                if (progress.ActiveGame is not { } saved) return (false, "No puzzle to resume.");
                Open(CloneGame(saved));
                return (true, "Word Search resumed.");
            case "word-search.back":
                if (game is null) return (false, "There is no active Word Search view.");
                if (!game.Completed) SaveElapsed();
                clock.Stop(); game = null; hint = null; lastFind = null;
                return (true, "Returned to Word Search levels.");
            case "word-search.exit":
                return game is null ? (true, "Returned to Chill Lounge.") :
                    (false, "Return to Word Search levels first.");
        }

        if (game is null) return (false, "Select a Word Search puzzle first.");
        if (game.Completed) return (false, "This puzzle is already completed.");
        if (command.Action == "word-search.hint")
        {
            WordSearchGameState next = CloneGame(game);
            WordSearchPlacement? requested = WordSearchGame.Hint(next);
            if (requested is null) return (false, "All words have been found.");
            next.ElapsedSeconds = CurrentSeconds;
            PersistActive(next);
            hint = requested;
            notice = $"Hint: {requested.Word} begins at the glowing letter.";
            return (true, notice);
        }
        if (command.Action == "word-search.select")
        {
            WordSearchGameState next = CloneGame(game);
            var start = new WordSearchCell(command.StartRow!.Value, command.StartColumn!.Value);
            var end = new WordSearchCell(command.EndRow!.Value, command.EndColumn!.Value);
            if (!WordSearchGame.TryFind(next, start, end, out string? found))
                return (false, "No listed word on that line. Keep searching.");
            next.ElapsedSeconds = CurrentSeconds;
            if (next.Completed)
            {
                WordSearchCompletionRecord receipt = WordSearchGame.Complete(next);
                WordSearchProgressState updated = CloneProgress(progress);
                if (updated.CompletedGames.All(x => x.PuzzleId != receipt.PuzzleId))
                    updated.CompletedGames.Add(receipt);
                updated.ActiveGame = null;
                bool newDiscovery = updated.Discover(found!, next.Puzzle);
                persist(updated);
                progress = updated;
                game = next;
                lastFind = (found!, newDiscovery, Guid.NewGuid().ToString("D"));
                foundSound?.Invoke(newDiscovery);
                lastPersistedSeconds = next.ElapsedSeconds;
                clock.Stop();
                hint = null;
                notice = $"All {next.FoundWords.Count} words found!";
                ProgressionAward award = complete(receipt);
                rewardNotice = award.PlayerExperience > 0
                    ? $"+{award.PlayerExperience:N0} player XP · +{award.SeasonExperience:N0} Season XP"
                    : "Puzzle complete and saved.";
                return (true, notice);
            }
            PersistActive(next, found);
            hint = null;
            notice = $"Found {found}. Saved.";
            return (true, notice);
        }
        return (false, "Unsupported Word Search action.");
    }

    private (bool, string) Start(WordSearchDifficulty difficulty)
    {
        // Starting a replacement is a deliberate library action. A live grid must be
        // exited first, so the frontend can ask for confirmation before overwriting it.
        if (game is not null)
            return (false, "Return to the Word Search levels before starting a new puzzle.");
        WordSearchPuzzleDefinition definition = WordSearchGame.NextPuzzle(progress, difficulty);
        if (progress.ActiveGame is { } unfinished && definition.Id == unfinished.Puzzle.Id)
        {
            // A replacement must not generate the exact same deterministic grid.
            // Skip already awarded puzzles so completion XP remains grant-once.
            IReadOnlyList<WordSearchPuzzleDefinition> pool = WordSearchCatalog.For(difficulty);
            HashSet<string> solved = progress.CompletedGames.Select(x => x.PuzzleId)
                .ToHashSet(StringComparer.Ordinal);
            int position = -1;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].Id == unfinished.Puzzle.Id) { position = i; break; }
            for (int offset = 1; offset < pool.Count; offset++)
            {
                WordSearchPuzzleDefinition candidate = pool[(position + offset) % pool.Count];
                if (candidate.Id != unfinished.Puzzle.Id && !solved.Contains(candidate.Id))
                {
                    definition = candidate;
                    break;
                }
            }
        }
        WordSearchGameState created = WordSearchGame.Start(definition, language: language());
        WordSearchProgressState next = CloneProgress(progress);
        next.ActiveGame = CloneGame(created);
        persist(next);
        progress = next;
        Open(created);
        return (true, "New Word Search puzzle started.");
    }

    private void Open(WordSearchGameState next)
    {
        clock.Stop(); clock.Reset();
        game = next;
        elapsedBase = next.ElapsedSeconds;
        lastPersistedSeconds = elapsedBase;
        if (!next.Completed) clock.Start();
        notice = "Drag from the first letter to the last letter of a hidden word.";
        hint = null; rewardNotice = ""; lastFind = null;
    }

    private void PersistActive(WordSearchGameState next, string? newlyFound = null)
    {
        WordSearchProgressState updated = CloneProgress(progress);
        updated.ActiveGame = CloneGame(next);
        bool isNew = newlyFound is not null && updated.Discover(newlyFound, next.Puzzle);
        persist(updated);
        progress = updated;
        game = next;
        if (newlyFound is not null)
        {
            lastFind = (newlyFound, isNew, Guid.NewGuid().ToString("D"));
            foundSound?.Invoke(isNew);
        }
        lastPersistedSeconds = next.ElapsedSeconds;
    }

    internal void CheckpointClock()
    {
        if (game is null || game.Completed || CurrentSeconds - lastPersistedSeconds < 10) return;
        SaveElapsed();
    }

    private void SaveElapsed()
    {
        if (game is null || game.Completed) return;
        long seconds = CurrentSeconds;
        if (seconds <= lastPersistedSeconds) return;
        WordSearchGameState next = CloneGame(game);
        next.ElapsedSeconds = seconds;
        PersistActive(next);
    }

    internal void Stop()
    {
        SaveElapsed();
        clock.Stop();
    }

    internal static WordSearchGameState CloneGame(WordSearchGameState source) => new()
    {
        Puzzle = source.Puzzle,
        FoundWords = new HashSet<string>(source.FoundWords, StringComparer.Ordinal),
        HintsUsed = source.HintsUsed,
        ElapsedSeconds = source.ElapsedSeconds,
        StartedAt = source.StartedAt,
        CompletedAt = source.CompletedAt,
    };

    private static WordSearchProgressState CloneProgress(WordSearchProgressState source) => new()
    {
        SchemaVersion = source.SchemaVersion,
        ActiveGame = source.ActiveGame is null ? null : CloneGame(source.ActiveGame),
        CompletedGames = source.CompletedGames.ToList(),
        DiscoveredWords = source.DiscoveredWords.ToList(),
    };
}
