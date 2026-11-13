using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Jigsaw;
using BlackjackCasino.Core.Memory;
using BlackjackCasino.Core.Minesweeper;
using BlackjackCasino.Core.Solitaire;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Core.TowerDefense;
using BlackjackCasino.Core.WordSearch;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

internal sealed record NativeLoungeReply(string Type, object Data);

/// <summary>
/// WPF-independent composition of all eight existing React Lounge game services.
/// Each service is the same native C# authority used by MainWindow, including
/// versioned request ID/revision receipts and original serialized board formats.
/// </summary>
internal sealed class NativeLoungeRuntime(NativePreviewRuntime owner) : IDisposable
{
    private ModernSudokuSession sudokuSession = new();
    private ModernSolitaireSession solitaireSession = new();
    private ModernWordSearchSession wordSearchSession = new();
    private ModernMemorySession memorySession = new();
    private ModernJigsawSession jigsawSession = new();
    private ModernAdventureSession adventureSession = new();
    private ModernTowerDefenseSession towerSession = new();
    private ModernMinesweeperSession mineSession = new();

    private ModernSudokuService? sudoku;
    private ModernSolitaireService? solitaire;
    private ModernWordSearchService? wordSearch;
    private ModernMemoryService? memory;
    private ModernJigsawService? jigsaw;
    private ModernAdventureService? adventure;
    private ModernTowerDefenseService? tower;
    private ModernMinesweeperService? mine;
    private bool disposed;

    private AppPreferences Prefs => owner.Preferences;
    private PlayerProfileState Profile => owner.Profile;
    private PlayerProgressionController Progress => owner.Progression;
    internal void SetBoardWindow(bool open)
    {
        if (adventure is not null) adventure.DetachedBoardOpen = open;
    }

    internal bool CanOpenBoard => adventure is { HasCaseBoard: true } &&
        adventure.Screen is "story" or "board";

    internal object Snapshot(bool boardOnly = false) => new
    {
        Version = ModernLoungeCatalog.SnapshotVersion,
        BoardOnly = boardOnly,
        Session = sudokuSession.Id, Revision = sudokuSession.Revision,
        Sudoku = sudoku?.Snapshot(),
        SolitaireSession = solitaireSession.Id, SolitaireRevision = solitaireSession.Revision,
        Solitaire = solitaire?.Snapshot(),
        WordSearchSession = wordSearchSession.Id, WordSearchRevision = wordSearchSession.Revision,
        WordSearch = wordSearch?.Snapshot(),
        MemorySession = memorySession.Id, MemoryRevision = memorySession.Revision,
        Memory = memory?.Snapshot(),
        JigsawSession = jigsawSession.Id, JigsawRevision = jigsawSession.Revision,
        Jigsaw = jigsaw?.Snapshot(),
        AdventureSession = adventureSession.Id, AdventureRevision = adventureSession.Revision,
        Adventure = adventure?.Snapshot(),
        TowerDefenseSession = towerSession.Id, TowerDefenseRevision = towerSession.Revision,
        TowerDefense = tower?.Snapshot(),
        MinesweeperSession = mineSession.Id, MinesweeperRevision = mineSession.Revision,
        Minesweeper = mine?.Snapshot(),
        Language = Prefs.Language == UiLanguagePreference.German ? "de" : "en",
        MusicEnabled = Prefs.Presentation("chill-lounge").MusicEnabled,
        SoundEnabled = Prefs.Presentation("chill-lounge").SoundEnabled,
        AnimationsEnabled = Prefs.Presentation("chill-lounge").AnimationsEnabled,
        Games = ModernLoungeCatalog.Create(id => id switch
        {
            "sudoku" => $"{Prefs.Sudoku.CompletedPuzzleIds.Count:N0} puzzles complete",
            "solitaire" => $"{Profile.Solitaire.Wins:N0} wins",
            "word-search" => $"{Prefs.WordSearch.CompletedGames.Count:N0} puzzles complete",
            "memory" => $"{Prefs.Memory.CompletedGames.Count:N0} games complete",
            "jigsaw" => $"{Prefs.Jigsaw.CompletedGames.Count:N0} puzzles complete",
            "tales-backroom" => $"{Prefs.Adventure.Runs.Count:N0} adventures recorded",
            "tower-defense" => $"Lv {Prefs.TowerDefense.Progress.Level}",
            "minesweeper" => $"{Profile.Minesweeper.Wins:N0} wins",
            _ => null,
        }),
    };

    internal void Open(string id)
    {
        if (disposed || owner.LoungeRequiresRestart)
            throw new InvalidOperationException("Lounge checkpoint recovery requires restart.");
        if (!ModernLoungeCatalog.Contains(id))
            throw new ArgumentOutOfRangeException(nameof(id));
        switch (id)
        {
            case "sudoku":
                sudoku ??= new ModernSudokuService(Prefs.Sudoku,
                    value =>
                    {
                        owner.UpdateLoungePreferences(p => p with { Sudoku = SudokuProgressState.Normalize(value) });
                        ReconcileSudoku();
                    },
                    () => Prefs.SudokuShowTimer,
                    value => owner.UpdateLoungePreferences(p => p with { SudokuShowTimer = value }),
                    () => Prefs.SudokuShowGuides,
                    value => owner.UpdateLoungePreferences(p => p with { SudokuShowGuides = value }),
                    () => string.Empty,
                    () => Profile.SudokuCareer,
                    new ModernSudokuVariants(Prefs.SudokuVariants,
                        value =>
                        {
                            owner.UpdateLoungePreferences(p => p with
                            { SudokuVariants = SudokuVariantProgressState.Normalize(value) });
                            ReconcileSudokuVariants();
                        }, () => Profile.SudokuVariants, () => string.Empty));
                break;
            case "solitaire":
                solitaire ??= new ModernSolitaireService(Prefs.Solitaire,
                    value => owner.UpdateLoungePreferences(p => p with
                    { Solitaire = SolitaireProgressState.Normalize(value) }),
                    () => Profile.Solitaire,
                    () => Profile.EquippedCardSetId,
                    record =>
                    {
                        if (Progress.ProcessSolitaireStart(record)) owner.SaveLoungeProfile();
                    },
                    record =>
                    {
                        if (Progress.ProcessSolitaireAbandon(record)) owner.SaveLoungeProfile();
                    },
                    record => RewardText(Progress.ProcessSolitaire(record), "Solitaire"));
                break;
            case "word-search":
                wordSearch ??= new ModernWordSearchService(Prefs.WordSearch,
                    value =>
                    {
                        value.Normalize();
                        owner.UpdateLoungePreferences(p => p with { WordSearch = value });
                    },
                    () => Profile.WordSearch,
                    record => Reward(Progress.ProcessWordSearch(record)),
                    () => Prefs.Language == UiLanguagePreference.German ? "de" : "en");
                break;
            case "memory":
                memory ??= new ModernMemoryService(Prefs.Memory,
                    value =>
                    {
                        value.Normalize();
                        owner.UpdateLoungePreferences(p => p with { Memory = value });
                    },
                    () => Profile.Memory,
                    record => Reward(Progress.ProcessMemory(record)));
                break;
            case "jigsaw":
                jigsaw ??= new ModernJigsawService(Prefs.Jigsaw,
                    value =>
                    {
                        value.Normalize();
                        owner.UpdateLoungePreferences(p => p with { Jigsaw = value });
                    },
                    () => Profile.Jigsaw,
                    record => Reward(Progress.ProcessJigsaw(record)));
                break;
            case "tales-backroom":
                adventure ??= new ModernAdventureService(Prefs.Adventure,
                    value => owner.UpdateLoungePreferences(p => p with
                    { Adventure = AdventureProgressState.Normalize(value) }),
                    (definition, state) =>
                    {
                        AdventureCompletionMetaResult reward =
                            Progress.RecordAdventureCompletion(definition, state);
                        owner.SaveLoungeProfile();
                        return reward;
                    },
                    () => Profile.DisplayName,
                    musicSettings: () => (Prefs.Presentation("adventure").MusicEnabled,
                        Prefs.Presentation("adventure").MusicVolume),
                    saveMusic: (enabled, volume) =>
                    {
                        AreaPresentationSettings presentation = Prefs.Presentation("adventure") with
                        { MusicEnabled = enabled, MusicVolume = Math.Clamp(volume, 0, .32) };
                        owner.UpdateLoungePreferences(p =>
                            p.WithPresentation("adventure", presentation.Sanitize())
                                .WithPresentation("chill-lounge", presentation.Sanitize()));
                    });
                break;
            case "tower-defense":
                tower ??= new ModernTowerDefenseService(Prefs.TowerDefense, value =>
                {
                    owner.UpdateLoungePreferences(p => p with { TowerDefense = value });
                    // The retained profile statistic ledger tracks completed runs
                    // without allowing React to submit outcomes or award amounts.
                    Profile.TowerDefense = value.Progress.Copy();
                    owner.SaveLoungeProfile();
                });
                break;
            case "minesweeper":
                mine ??= new ModernMinesweeperService(Prefs.Minesweeper, value =>
                {
                    owner.UpdateLoungePreferences(p => p with { Minesweeper = value.Copy() });
                    if (!value.Run.Finished) return;
                    SweepStatistics candidate = Profile.Minesweeper.Copy();
                    if (candidate.Record(value.Run))
                    {
                        Profile.Minesweeper = candidate;
                        owner.SaveLoungeProfile();
                    }
                });
                break;
        }
    }

    private ProgressionAward Reward(ProgressionAward award)
    {
        if (award != ProgressionAward.None) owner.SaveLoungeAward(award);
        return award;
    }

    private string RewardText(ProgressionAward award, string game)
    {
        Reward(award);
        return award == ProgressionAward.None ? game + " · reward already recorded."
            : $"+{award.PlayerExperience:N0} XP · +{award.BankrollEarned:C2} bankroll";
    }

    private void ReconcileSudoku()
    {
        SudokuProgressState state = SudokuProgressState.Normalize(Prefs.Sudoku);
        foreach (string id in state.CompletedPuzzleIds)
        {
            if (Profile.Sudoku.RecordedPuzzleIds.Contains(id)
                || !SudokuLibrary.TryGet(id, out SudokuPuzzle? puzzle)
                || puzzle is null || !state.Games.TryGetValue(id, out SudokuGameState? game))
                continue;
            SudokuGameState completed = SudokuGameState.Normalize(puzzle, game);
            if (completed.Completed)
                Reward(Progress.ProcessSudoku(new SudokuCompletionRecord(
                    puzzle.Id, puzzle.Difficulty, puzzle.Pool,
                    completed.ElapsedSeconds, completed.HintsUsed)));
        }
    }

    private void ReconcileSudokuVariants()
    {
        foreach (SudokuVariantCompletion receipt in Prefs.SudokuVariants.CompletedReceipts ?? [])
            if (!Profile.SudokuVariants.RecordedPuzzleIds.Contains(receipt.PuzzleId))
                Reward(Progress.ProcessSudokuVariant(receipt));
    }

    internal NativeLoungeReply? Execute(string type, string payload, bool boardOnly = false)
    {
        if (disposed || owner.LoungeRequiresRestart) return null;
        if (boardOnly && type != "lounge.adventure.command") return null;
        try
        {
            switch (type)
            {
                case "lounge.sudoku.command" when sudoku is not null:
                    if (ModernSudokuProtocol.Parse(payload) is { } sc)
                        return new("lounge.sudoku.result", sudokuSession.Execute(sc, sudoku.Execute));
                    break;
                case "lounge.solitaire.command" when solitaire is not null:
                    if (ModernSolitaireProtocol.Parse(payload) is { } so)
                        return new("lounge.solitaire.result", solitaireSession.Execute(so, solitaire.Execute));
                    break;
                case "lounge.word-search.command" when wordSearch is not null:
                    if (ModernWordSearchProtocol.Parse(payload) is { } ws)
                        return new("lounge.word-search.result", wordSearchSession.Execute(ws, wordSearch.Execute));
                    break;
                case "lounge.memory.command" when memory is not null:
                    if (ModernMemoryProtocol.Parse(payload) is { } me)
                        return new("lounge.memory.result", memorySession.Execute(me, memory.Execute));
                    break;
                case "lounge.jigsaw.command" when jigsaw is not null:
                    if (ModernJigsawProtocol.Parse(payload) is { } ji)
                        return new("lounge.jigsaw.result", jigsawSession.Execute(ji, jigsaw.Execute));
                    break;
                case "lounge.adventure.command" when adventure is not null:
                    if (ModernAdventureProtocol.Parse(payload) is { } ad)
                    {
                        if (boardOnly && ad.Action == "adventure.board.close") return null;
                        if (boardOnly && ad.Action is not ("adventure.board.move" or "adventure.board.view"))
                            return null;
                        return new("lounge.adventure.result", adventureSession.Execute(ad, adventure.Execute));
                    }
                    break;
                case "lounge.tower-defense.command" when tower is not null:
                    if (ModernTowerDefenseProtocol.Parse(payload) is { } td)
                        return new("lounge.tower-defense.result", towerSession.Execute(td, tower.Execute));
                    break;
                case "lounge.minesweeper.command" when mine is not null:
                    if (ModernMinesweeperProtocol.Parse(payload) is { } sw)
                        return new("lounge.minesweeper.result", mineSession.Execute(sw, mine.Execute));
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
            ArgumentException or InvalidDataException or InvalidOperationException or OverflowException)
        {
            // A partially written checkpoint can already be durable. Refuse
            // new commands until its recovery has replayed at process startup.
            owner.MarkLoungeRecoveryRequired();
            throw new InvalidOperationException(
                "Lounge command interrupted during persistence. Restart before continuing.", ex);
        }
        return null;
    }

    internal bool Tick()
    {
        if (disposed || owner.LoungeRequiresRestart) return false;
        try
        {
            sudoku?.CheckpointClock();
            solitaire?.CheckpointClock();
            wordSearch?.CheckpointClock();
            memory?.CheckpointClock();
            jigsaw?.CheckpointClock();
            mine?.Tick();
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            owner.MarkLoungeRecoveryRequired();
            return false;
        }
    }

    internal bool TickTower()
    {
        if (disposed || owner.LoungeRequiresRestart) return false;
        try { return tower?.Tick() ?? false; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            owner.MarkLoungeRecoveryRequired();
            return false;
        }
    }

    internal void Close()
    {
        if (disposed) return;
        disposed = true;
        sudoku?.Stop();
        solitaire?.Stop();
        wordSearch?.Stop();
        memory?.Stop();
        jigsaw?.Stop();
        tower?.Checkpoint();
        mine?.Checkpoint();
        owner.Persist();
    }

    public void Dispose() => Close();
}
