using System.Diagnostics;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Authoritative C# Solitaire game session for the React Chill Lounge.
/// The original save format, deterministic deals and all six rule sets are reused.
/// Browser moves commit a cloned game and persistent profile before replacing live state.
/// </summary>
internal sealed class ModernSolitaireService
{
    private SolitaireProgressState progress;
    private SolitaireGameState? game;
    private readonly Action<SolitaireProgressState> persist;
    private readonly Func<SolitaireStatistics> statistics;
    private readonly Action<SolitaireStartRecord> started;
    private readonly Action<SolitaireAbandonRecord> abandoned;
    private readonly Func<SolitaireCompletionRecord, string> completed;
    private readonly Stopwatch clock = new();
    private long elapsedBase, lastPersistedSeconds;
    private string notice = "", rewardNotice = "";

    internal ModernSolitaireService(SolitaireProgressState initial,
        Action<SolitaireProgressState> persist,
        Func<SolitaireStatistics> statistics,
        Action<SolitaireStartRecord> started,
        Action<SolitaireAbandonRecord> abandoned,
        Func<SolitaireCompletionRecord, string> completed)
    {
        progress = CloneProgress(SolitaireProgressState.Normalize(initial));
        this.persist = persist;
        this.statistics = statistics;
        this.started = started;
        this.abandoned = abandoned;
        this.completed = completed;
    }

    internal SolitaireGameState? CurrentGame => game;
    internal long CurrentSeconds => elapsedBase + (long)Math.Floor(clock.Elapsed.TotalSeconds);

    internal object Snapshot()
    {
        SolitaireStatistics stats = statistics();
        stats.Normalize();
        var options = new List<object>();
        foreach (SolitaireVariant variant in Enum.GetValues<SolitaireVariant>())
        {
            int[] suitOptions = variant == SolitaireVariant.Spider ? [1, 2, 4] : [1];
            foreach (int suits in suitOptions)
            {
                progress.TryGet(variant, out SolitaireGameState? saved, suits);
                options.Add(new
                {
                    Variant = variant.ToString(), SpiderSuits = suits,
                    CanResume = saved is not null,
                    Won = saved?.Won ?? false,
                    DealNumber = saved?.DealNumber ?? 0, Moves = saved?.Moves ?? 0,
                    ElapsedSeconds = saved?.ElapsedSeconds ?? 0L,
                });
            }
        }

        object? active = null;
        if (game is not null)
        {
            SolitaireGameState current = game;
            var piles = current.Piles.ToDictionary(pair => pair.Key,
                pair => pair.Value.Select((item, index) => new
                {
                    Rank = (int)item.Card.Rank, Suit = item.Card.Suit.ToString(),
                    item.FaceUp,
                    Selectable = SolitaireGame.IsSelectable(current, pair.Key, index),
                }).ToArray(), StringComparer.Ordinal);
            active = new
            {
                GameId = current.GameId.ToString("D"), Variant = current.Variant.ToString(),
                current.DealNumber, current.SpiderSuits, current.Moves,
                ElapsedSeconds = CurrentSeconds, current.CompletedRuns, current.Won,
                Piles = piles,
                RewardXp = SolitaireProgression.PlayerExperience(new SolitaireCompletionRecord(
                    current.GameId, current.Variant, current.SpiderSuits, current.DealNumber,
                    current.ElapsedSeconds, current.Moves)),
            };
        }
        return new
        {
            Screen = game is null ? "library" : "game",
            NextDealNumber = progress.NextDealNumber,
            Options = options,
            Stats = new
            {
                stats.GamesStarted, stats.Wins, stats.GamesAbandoned,
                stats.BestWinStreak, stats.CurrentWinStreak, stats.WinRate,
                Achievements = SolitaireAchievementCatalog.All.Select(a => new
                {
                    a.Id, a.Name, a.Description, Complete = a.Complete(stats),
                }).ToArray(),
            },
            Game = active, Notice = notice, RewardNotice = rewardNotice,
        };
    }

    internal (bool Success, string Message) Execute(ModernSolitaireCommand command)
    {
        switch (command.Action)
        {
            case "solitaire.new": return StartNew(command.Variant!.Value, command.SpiderSuits, command.DealNumber);
            case "solitaire.resume": return Resume(command.Variant!.Value, command.SpiderSuits);
            case "solitaire.back":
                if (game is null) return (false, "No Solitaire game is open.");
                SaveElapsed();
                clock.Stop();
                game = null;
                return (true, "Returned to the Solitaire cabinet.");
            case "solitaire.exit":
                if (game is not null) return (false, "Return to the Solitaire cabinet first.");
                return (true, "Returned to Chill Lounge.");
        }
        if (game is null) return (false, "Choose a Solitaire game first.");
        if (game.Won) return (false, "This deal has already been completed.");
        return command.Action switch
        {
            "solitaire.draw" => Mutate(candidate => SolitaireGame.TryDraw(candidate), "Stock dealt."),
            "solitaire.activate" => Mutate(candidate =>
                SolitaireGame.TryActivateCard(candidate, command.Source!, command.Index!.Value), "Card activated."),
            "solitaire.auto" => Mutate(candidate =>
                SolitaireGame.TryAutoFoundation(candidate, command.Source!, command.Index!.Value),
                "Card moved to its foundation."),
            "solitaire.move" => Mutate(candidate =>
                SolitaireGame.TryMove(candidate, command.Source!, command.Index!.Value, command.Target!),
                "Cards moved."),
            _ => (false, "Unsupported Solitaire action."),
        };
    }

    private (bool, string) StartNew(SolitaireVariant variant, int spiderSuits, int? numbered)
    {
        int dealNumber = Math.Max(1, numbered ?? progress.NextDealNumber);
        progress.TryGet(variant, out SolitaireGameState? existing, spiderSuits);
        SolitaireGameState created = SolitaireGame.Create(variant, dealNumber, spiderSuits);
        SolitaireProgressState next = CloneProgress(progress);
        next.Save(created);
        persist(next); // A failure must not displace the prior playable game.
        progress = next;
        if (existing is { Won: false })
            abandoned(new SolitaireAbandonRecord(existing.GameId, existing.Variant,
                existing.SpiderSuits, existing.DealNumber, existing.ElapsedSeconds, existing.Moves));
        started(new SolitaireStartRecord(created.GameId, variant, spiderSuits, dealNumber));
        Open(created);
        return (true, "New deterministic Solitaire deal saved.");
    }

    private (bool, string) Resume(SolitaireVariant variant, int spiderSuits)
    {
        if (!progress.TryGet(variant, out SolitaireGameState? saved, spiderSuits) || saved is null)
            return (false, "No saved game exists for this variant.");
        Open(CloneGame(saved));
        if (game?.Won == true) AwardWinIfNeeded(game);
        return (true, "Saved Solitaire deal restored.");
    }

    private void Open(SolitaireGameState next)
    {
        clock.Stop(); clock.Reset();
        game = next;
        elapsedBase = next.ElapsedSeconds;
        lastPersistedSeconds = elapsedBase;
        if (!next.Won) clock.Start();
        notice = next.Won ? "Deal complete." : "Your deal is ready.";
        rewardNotice = "";
    }

    private (bool, string) Mutate(Func<SolitaireGameState, bool> play, string description)
    {
        SolitaireGameState next = CloneGame(game!);
        if (!play(next)) return (false, "This move is not legal in the current game.");
        next.ElapsedSeconds = CurrentSeconds;
        SolitaireProgressState nextProgress = CloneProgress(progress);
        nextProgress.Save(next);
        persist(nextProgress);
        progress = nextProgress;
        game = next;
        lastPersistedSeconds = next.ElapsedSeconds;
        notice = next.Won ? "Deal completed!" : description;
        if (next.Won)
        {
            clock.Stop();
            AwardWinIfNeeded(next);
        }
        return (true, notice);
    }

    private void AwardWinIfNeeded(SolitaireGameState won)
    {
        rewardNotice = completed(new SolitaireCompletionRecord(
            won.GameId, won.Variant, won.SpiderSuits, won.DealNumber, won.ElapsedSeconds, won.Moves));
    }

    internal void CheckpointClock()
    {
        if (game is null || game.Won || CurrentSeconds - lastPersistedSeconds < 10) return;
        SaveElapsed();
    }

    private void SaveElapsed()
    {
        if (game is null) return;
        long seconds = CurrentSeconds;
        if (seconds <= lastPersistedSeconds) return;
        SolitaireGameState next = CloneGame(game);
        next.ElapsedSeconds = seconds;
        SolitaireProgressState nextProgress = CloneProgress(progress);
        nextProgress.Save(next);
        persist(nextProgress);
        progress = nextProgress;
        game = next;
        lastPersistedSeconds = seconds;
    }

    internal void Stop()
    {
        SaveElapsed();
        clock.Stop();
    }

    internal static SolitaireGameState CloneGame(SolitaireGameState source) => new()
    {
        GameId = source.GameId, Variant = source.Variant, DealNumber = source.DealNumber,
        SpiderSuits = source.SpiderSuits, ElapsedSeconds = source.ElapsedSeconds,
        Moves = source.Moves, CompletedRuns = source.CompletedRuns, Won = source.Won,
        Piles = source.Piles.ToDictionary(pair => pair.Key,
            pair => pair.Value.Select(item => new SolitaireCardState(item.Card, item.FaceUp)).ToList(),
            StringComparer.Ordinal),
    };

    private static SolitaireProgressState CloneProgress(SolitaireProgressState source) => new()
    {
        SchemaVersion = source.SchemaVersion, NextDealNumber = source.NextDealNumber,
        ActiveGames = source.ActiveGames.ToDictionary(pair => pair.Key,
            pair => CloneGame(pair.Value), StringComparer.Ordinal),
    };
}
