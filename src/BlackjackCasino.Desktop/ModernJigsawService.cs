using System.Diagnostics;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Jigsaw;

namespace BlackjackCasino.Desktop;

/// <summary>Transactional adapter around the existing native puzzle, persistence, achievements and receipts.</summary>
internal sealed class ModernJigsawService
{
    private JigsawProgressState progress;
    private readonly Action<JigsawProgressState> persist;
    private readonly Func<JigsawStatistics> statistics;
    private readonly Func<JigsawCompletionRecord, ProgressionAward> reward;
    private readonly Func<string,string> motifLabel, categoryLabel;
    private readonly Func<JigsawAchievementDefinition,string> achievementName, achievementDescription, achievementTier;
    private JigsawGameState? active;
    private JigsawGameState? completed;
    private readonly Stopwatch clock = new();
    private long elapsedBase, lastSaved;
    private string screen = "library", notice = "", rewardNotice = "";

    internal ModernJigsawService(JigsawProgressState saved, Action<JigsawProgressState> persist,
        Func<JigsawStatistics> statistics, Func<JigsawCompletionRecord, ProgressionAward> reward,
        Func<string,string>? motifLabel = null, Func<string,string>? categoryLabel = null,
        Func<JigsawAchievementDefinition,string>? achievementName = null,
        Func<JigsawAchievementDefinition,string>? achievementDescription = null,
        Func<JigsawAchievementDefinition,string>? achievementTier = null)
    {
        saved.Normalize();
        progress = Copy(saved);
        this.persist = persist;
        this.statistics = statistics;
        this.reward = reward;
        this.motifLabel = motifLabel ?? (id => JigsawCatalog.Motif(id).Name);
        this.categoryLabel = categoryLabel ?? (category => category);
        this.achievementName = achievementName ?? (a => a.Name);
        this.achievementDescription = achievementDescription ?? (a => a.Description);
        this.achievementTier = achievementTier ?? (a => a.Tier.ToString());
    }

    internal JigsawGameState? CurrentGame => active;
    internal long CurrentSeconds => active is { TimerEnabled: true }
        ? elapsedBase + (long)Math.Floor(clock.Elapsed.TotalSeconds) : elapsedBase;

    internal object Snapshot()
    {
        JigsawStatistics stats = statistics();
        stats.Normalize();
        JigsawGameState? game = active ?? completed;
        object? board = game is null ? null : new
        {
            GameId = game.GameId.ToString("D"), game.MotifId, game.PieceCount,
            Columns = JigsawCatalog.Size(game.PieceCount).Columns,
            Rows = JigsawCatalog.Size(game.PieceCount).Rows,
            Width = JigsawGame.BoardWidth, Height = JigsawGame.BoardHeight,
            ElapsedSeconds = active is null ? game.ElapsedSeconds : CurrentSeconds,
            game.TimerEnabled, game.Completed, game.LockedCount, game.OnBoardCount,
            Pieces = game.Pieces.Select(piece => new
            { piece.Id, piece.X, piece.Y, piece.OnBoard, piece.Locked }).ToArray(),
        };
        object? resume = progress.ActiveGame is null ? null : new
        {
            progress.ActiveGame.MotifId, progress.ActiveGame.PieceCount,
            progress.ActiveGame.LockedCount, progress.ActiveGame.ElapsedSeconds,
        };
        return new
        {
            Screen = screen, progress.SelectedMotifId, progress.SelectedPieceCount,
            progress.TimerEnabled, Game = board, Resume = resume,
            Motifs = JigsawCatalog.Motifs.Select(m => new
            { m.Id, Name = motifLabel(m.Id), Category = categoryLabel(m.Category),
              m.Asset }).ToArray(),
            Sizes = JigsawCatalog.Sizes.Select(s => new { s.Count, s.Columns, s.Rows,
                RewardXp = JigsawProgression.PlayerExperience(s.Count) }).ToArray(),
            Stats = new { stats.CompletedGames, stats.PiecesCompleted, stats.LargestPuzzle,
                AchievementsUnlocked = stats.UnlockedAchievementIds.Count },
            Achievements = JigsawAchievements.All.Select(a => new
            {
                a.Id, Name = achievementName(a),
                Description = achievementDescription(a),
                Tier = achievementTier(a), a.Goal, Current = Math.Min(a.Goal, a.Current(stats)),
                Unlocked = stats.UnlockedAchievementIds.Contains(a.Id),
                RewardXp = a.ExperienceReward, RewardCoins = a.CoinReward,
            }).ToArray(),
            Notice = notice, RewardNotice = rewardNotice,
        };
    }

    internal (bool Success, string Message) Execute(ModernJigsawCommand command)
    {
        switch (command.Action)
        {
            case "jigsaw.motif":
                return ChangePreference(next => next.SelectedMotifId = command.MotifId!);
            case "jigsaw.size":
                return ChangePreference(next => next.SelectedPieceCount = command.PieceCount!.Value);
            case "jigsaw.timer":
            {
                bool enabled = command.Enabled!.Value;
                JigsawProgressState next = Copy(progress);
                next.TimerEnabled = enabled;
                if (active is not null)
                {
                    JigsawGameState game = Copy(active);
                    game.ElapsedSeconds = enabled ? CurrentSeconds : 0;
                    game.TimerEnabled = enabled;
                    next.ActiveGame = game;
                    persist(next);
                    progress = next;
                    active = game;
                    elapsedBase = game.ElapsedSeconds;
                    lastSaved = elapsedBase;
                    if (enabled) clock.Restart(); else clock.Reset();
                }
                else { persist(next); progress = next; }
                return (true, "Timer preference saved.");
            }
            case "jigsaw.start":
                if (progress.ActiveGame is not null && command.ConfirmReplace != true)
                    return (false, "Confirm replacement of your unfinished puzzle first.");
                JigsawGameState started = JigsawGame.Start(progress.SelectedMotifId, progress.SelectedPieceCount);
                started.TimerEnabled = progress.TimerEnabled;
                JigsawProgressState replacement = Copy(progress);
                replacement.ActiveGame = Copy(started);
                persist(replacement);
                progress = replacement;
                Open(started);
                return (true, "Puzzle ready.");
            case "jigsaw.resume":
                if (progress.ActiveGame is null) return (false, "No saved puzzle.");
                Open(Copy(progress.ActiveGame));
                return (true, "Puzzle resumed.");
            case "jigsaw.place":
            case "jigsaw.move":
            case "jigsaw.tray":
                return Manipulate(command);
            case "jigsaw.back":
                CheckpointClock(force: true);
                active = null;
                completed = null;
                clock.Reset();
                screen = "library";
                return (true, "Returned to puzzle selection.");
            case "jigsaw.achievements":
                screen = "achievements";
                return (true, "Achievements opened.");
            case "jigsaw.exit":
                Stop();
                active = null;
                completed = null;
                screen = "library";
                return (true, "Jigsaw closed.");
            default:
                return (false, "Unknown puzzle action.");
        }
    }

    private (bool, string) ChangePreference(Action<JigsawProgressState> change)
    {
        JigsawProgressState next = Copy(progress);
        change(next);
        persist(next);
        progress = next;
        return (true, "Puzzle preference saved.");
    }

    private void Open(JigsawGameState game)
    {
        active = game;
        completed = null;
        screen = "game";
        elapsedBase = game.ElapsedSeconds;
        lastSaved = elapsedBase;
        clock.Reset();
        if (game.TimerEnabled) clock.Start();
        notice = "";
        rewardNotice = "";
    }

    private (bool, string) Manipulate(ModernJigsawCommand command)
    {
        if (active is null || active.Completed) return (false, "Open a puzzle first.");
        int index = command.Index!.Value;
        if (index < 0 || index >= active.PieceCount) return (false, "Invalid puzzle piece.");
        JigsawGameState candidate = Copy(active);
        bool ok = command.Action switch
        {
            "jigsaw.place" => JigsawGame.Place(candidate, index, command.X!.Value, command.Y!.Value),
            "jigsaw.move" => candidate.Pieces[index].OnBoard && !candidate.Pieces[index].Locked
                && JigsawGame.Move(candidate, index, command.X!.Value - candidate.Pieces[index].X,
                    command.Y!.Value - candidate.Pieces[index].Y),
            "jigsaw.tray" => JigsawGame.ReturnToTray(candidate, index),
            _ => false,
        };
        if (!ok) return (false, "This piece cannot be moved.");
        if (command.Action != "jigsaw.tray") JigsawGame.Snap(candidate, index);
        candidate.ElapsedSeconds = CurrentSeconds;
        JigsawProgressState next = Copy(progress);
        if (candidate.Completed)
        {
            var completion = new JigsawCompletionRecord(candidate.GameId, candidate.MotifId,
                candidate.PieceCount, candidate.ElapsedSeconds,
                candidate.CompletedAt ?? DateTimeOffset.UtcNow);
            if (next.CompletedGames.All(c => c.GameId != completion.GameId))
                next.CompletedGames.Add(completion);
            next.ActiveGame = null;
            persist(next);
            progress = next;
            active = null;
            completed = candidate;
            clock.Stop();
            screen = "complete";
            notice = "Puzzle completed and saved.";
            try
            {
                ProgressionAward earned = reward(completion);
                rewardNotice = earned.PlayerExperience > 0
                    ? $"+{earned.PlayerExperience:N0} player XP · +{earned.SeasonExperience:N0} Season XP · +{earned.CoinsEarned:N0} coins"
                    : "Rewards were already recorded.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                rewardNotice = "Rewards saved for recovery when Lounge is opened again.";
            }
            return (true, notice);
        }
        next.ActiveGame = Copy(candidate);
        persist(next);
        progress = next;
        active = candidate;
        lastSaved = candidate.ElapsedSeconds;
        return (true, "Piece saved.");
    }

    internal void CheckpointClock(bool force = false)
    {
        if (active is not { TimerEnabled: true }) return;
        long seconds = CurrentSeconds;
        if (!force && seconds - lastSaved < 10 || seconds <= lastSaved) return;
        JigsawGameState candidate = Copy(active);
        candidate.ElapsedSeconds = seconds;
        JigsawProgressState next = Copy(progress);
        next.ActiveGame = Copy(candidate);
        persist(next);
        progress = next;
        active = candidate;
        lastSaved = seconds;
    }

    internal void Stop()
    {
        CheckpointClock(force: true);
        clock.Stop();
    }

    internal static JigsawGameState Copy(JigsawGameState source) => new()
    {
        GameId = source.GameId, MotifId = source.MotifId, PieceCount = source.PieceCount,
        Pieces = source.Pieces.Select(p => new JigsawPieceState
        { Id = p.Id, X = p.X, Y = p.Y, OnBoard = p.OnBoard, Locked = p.Locked }).ToList(),
        ElapsedSeconds = source.ElapsedSeconds, TimerEnabled = source.TimerEnabled,
        StartedAt = source.StartedAt, CompletedAt = source.CompletedAt,
    };

    internal static JigsawProgressState Copy(JigsawProgressState source) => new()
    {
        SchemaVersion = source.SchemaVersion, SelectedMotifId = source.SelectedMotifId,
        SelectedPieceCount = source.SelectedPieceCount, TimerEnabled = source.TimerEnabled,
        ActiveGame = source.ActiveGame is null ? null : Copy(source.ActiveGame),
        CompletedGames = source.CompletedGames.ToList(),
    };
}
