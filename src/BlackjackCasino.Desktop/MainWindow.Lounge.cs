using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Memory;
using BlackjackCasino.Core.Solitaire;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Core.WordSearch;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private BackgroundMusic? loungeBackgroundMusic;
    private string sudokuRewardNotice = "";

    private void OpenLounge()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen)
        {
            return;
        }
        ProcessLatestCompletedRound();
        SudokuLoungePolish.EnsureInstalled();
        LoungeVisualUpgrade.EnsureInstalled(() => preferences.Sudoku);
        RecordSudokuCompletions(preferences.Sudoku);

        preferences = preferences with { Solitaire = SolitaireProgressState.Normalize(preferences.Solitaire) };
        preferences.WordSearch.Normalize();
        RecordWordSearchCompletions(preferences.WordSearch);
        preferences.Memory.Normalize();
        RecordMemoryCompletions(preferences.Memory);
        preferences = preferences with { Adventure = AdventureProgressState.Normalize(preferences.Adventure) };

        bool loungeClosing = false;
        bool adventureMusicActive = false;
        void SetLoungeSoundtrack(bool adventureActive)
        {
            if (loungeClosing) return;
            adventureMusicActive = adventureActive;
            string area = adventureActive ? "adventure" : "chill-lounge";
            AreaPresentationSettings presentation = preferences.Presentation(area);

            loungeBackgroundMusic?.Dispose();
            loungeBackgroundMusic = new BackgroundMusic(adventureActive ? Soundtrack.Adventure : Soundtrack.ChillLounge);
            loungeBackgroundMusic.Start(presentation.MusicEnabled, presentation.MusicVolume);
        }

        void UpdateAdventurePresentation(AreaPresentationSettings presentation)
        {
            preferences = preferences.WithPresentation("adventure", presentation);
            if (!testSession) SavePreferences();

            if (!adventureMusicActive || loungeBackgroundMusic is null) return;
            AreaPresentationSettings current = preferences.Presentation("adventure");
            loungeBackgroundMusic.SetEnabled(current.MusicEnabled);
            loungeBackgroundMusic.SetVolume(current.MusicVolume);
        }

        AdventureCompletionMetaResult RecordAdventureCompletion(AdventureDefinition definition, AdventureRunState state)
        {
            if (progressionController is null)
            {
                string key = string.IsNullOrWhiteSpace(state.EndingId)
                    ? string.Empty
                    : AdventureProgressionService.EndingKey(definition.Id, state.EndingId);
                return new AdventureCompletionMetaResult(false, false, key, [], null);
            }

            return progressionController.RecordAdventureCompletion(definition, state);
        }

        Func<SolitaireStatistics> solitaireStats = () => progressionController?.Profile.Solitaire ?? new SolitaireStatistics();
        Func<WordSearchStatistics> wordSearchStats = () => progressionController?.Profile.WordSearch ?? new WordSearchStatistics();
        Func<MemoryStatistics> memoryStats = () => progressionController?.Profile.Memory ?? new MemoryStatistics();

        LoungeHostedGameRegistry.SetSessionModules([
            LoungeVisualUpgrade.UpgradeSolitaire(SolitaireLoungeModule.Create(
                preferences.Solitaire,
                state =>
                {
                    preferences = preferences with { Solitaire = SolitaireProgressState.Normalize(state) };
                    if (!testSession) SavePreferences();
                },
                solitaireStats,
                RecordSolitaireStart,
                RecordSolitaireAbandon,
                RecordSolitaireCompletion,
                () => progressionController?.Profile.EquippedCardSetId)),
            LoungeVisualUpgrade.UpgradeWordSearch(WordSearchLoungeModule.Create(
                preferences.WordSearch,
                state =>
                {
                    state.Normalize();
                    preferences = preferences with { WordSearch = state };
                    if (!testSession) SavePreferences();
                },
                wordSearchStats,
                RecordWordSearchCompletion),
                wordSearchStats,
                () => preferences.WordSearch),
            LoungeVisualUpgrade.UpgradeMemory(MemoryLoungeModuleVisual.Create(
                preferences.Memory,
                state =>
                {
                    state.Normalize();
                    preferences = preferences with { Memory = state };
                    if (!testSession) SavePreferences();
                },
                memoryStats,
                RecordMemoryCompletion,
                () => preferences.Presentation("chill-lounge").SoundEnabled,
                () => preferences.Presentation("chill-lounge").AnimationsEnabled),
                memoryStats),
            AdventureLibraryLoungeModule.Create(
                preferences.Adventure,
                state =>
                {
                    preferences = preferences with { Adventure = AdventureProgressState.Normalize(state) };
                    if (!testSession) SavePreferences();
                },
                RecordAdventureCompletion,
                () => progressionController?.Profile.EquippedDiceSetId,
                () => progressionController?.Profile.DisplayName,
                () => preferences.Presentation("adventure").AnimationsEnabled,
                () => preferences.Presentation("adventure"),
                UpdateAdventurePresentation,
                SetLoungeSoundtrack)
        ]);

        var window = new LoungeWindow(
            preferences.Sudoku,
            state =>
            {
                SudokuProgressState normalized = SudokuProgressState.Normalize(state);
                preferences = preferences with { Sudoku = normalized };
                if (!testSession)
                {
                    SavePreferences();
                }
                RecordSudokuCompletions(normalized);
            },
            preferences.SudokuShowTimer,
            showTimer =>
            {
                preferences = preferences with { SudokuShowTimer = showTimer };
                if (!testSession)
                {
                    SavePreferences();
                }
            },
            preferences.SudokuShowGuides,
            showGuides =>
            {
                preferences = preferences with { SudokuShowGuides = showGuides };
                if (!testSession)
                {
                    SavePreferences();
                }
            },
            rewardNotice: () => sudokuRewardNotice);

        TableModeText.Text = "Chill Lounge";
        MenuAudio.Attach(window, () => preferences.Presentation("chill-lounge").SoundEnabled);

        ShowEmbeddedGame(window, "Chill Lounge · The Midnight Club", muteHostMusic: true, closed: () =>
        {
            loungeClosing = true;
            LoungeHostedGameRegistry.ClearSessionModules();
            loungeBackgroundMusic?.Dispose();
            loungeBackgroundMusic = null;
            TableModeText.Text = "Casino lobby";
        });
        SetLoungeSoundtrack(adventureActive: false);
    }

    private void RecordSudokuCompletions(SudokuProgressState state)
    {
        if (progressionController is null)
        {
            return;
        }

        SudokuProgressState normalized = SudokuProgressState.Normalize(state);
        foreach (string puzzleId in normalized.CompletedPuzzleIds)
        {
            if (progressionController.Profile.Sudoku.RecordedPuzzleIds.Contains(puzzleId) ||
                !SudokuLibrary.TryGet(puzzleId, out SudokuPuzzle? puzzle) ||
                puzzle is null ||
                !normalized.Games.TryGetValue(puzzleId, out SudokuGameState? game))
            {
                continue;
            }

            SudokuGameState clean = SudokuGameState.Normalize(puzzle, game);
            if (!clean.Completed)
            {
                continue;
            }

            try
            {
                var award = progressionController.ProcessSudoku(new SudokuCompletionRecord(
                    puzzle.Id, puzzle.Difficulty, puzzle.Pool, clean.ElapsedSeconds, clean.HintsUsed));
                if (award.PlayerExperience > 0)
                    sudokuRewardNotice = $"+{award.PlayerExperience:N0} XP · +{award.SeasonExperience:N0} Season XP earned!";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                sudokuRewardNotice = "Puzzle saved. Reopen the Chill Lounge to retry saving your XP.";
            }
        }
    }

    private void RecordSolitaireStart(SolitaireStartRecord start)
    {
        if (progressionController is null) return;
        _ = progressionController.ProcessSolitaireStart(start);
    }

    private void RecordSolitaireAbandon(SolitaireAbandonRecord abandon)
    {
        if (progressionController is null) return;
        _ = progressionController.ProcessSolitaireAbandon(abandon);
    }

    private string RecordSolitaireCompletion(SolitaireCompletionRecord completion)
    {
        if (progressionController is null) return "Solved. Progression is unavailable in this session.";
        try
        {
            var award = progressionController.ProcessSolitaire(completion);
            return award.PlayerExperience > 0
                ? $"+{award.PlayerExperience:N0} XP · +{award.SeasonExperience:N0} Season XP earned!"
                : "Solved · reward already recorded.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "Solved and saved locally. Reopen this deal to retry saving XP. " + ex.Message;
        }
    }

    private void RecordWordSearchCompletions(WordSearchProgressState state)
    {
        if (progressionController is null) return;
        state.Normalize();
        foreach (WordSearchCompletionRecord completion in state.CompletedGames)
        {
            if (progressionController.Profile.WordSearch.RecordedPuzzleIds.Contains(completion.PuzzleId)) continue;
            try { _ = progressionController.ProcessWordSearch(completion); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return; }
        }
    }

    private ProgressionAward RecordWordSearchCompletion(WordSearchCompletionRecord completion)
    {
        if (progressionController is null) return ProgressionAward.None;
        try { return progressionController.ProcessWordSearch(completion); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return ProgressionAward.None; }
    }

    private void RecordMemoryCompletions(MemoryProgressState state)
    {
        if (progressionController is null) return;
        state.Normalize();
        foreach (MemoryCompletionRecord completion in state.CompletedGames)
        {
            if (progressionController.Profile.Memory.RecordedGameIds.Contains(completion.GameId)) continue;
            try { _ = progressionController.ProcessMemory(completion); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return; }
        }
    }

    private ProgressionAward RecordMemoryCompletion(MemoryCompletionRecord completion)
    {
        if (progressionController is null) return ProgressionAward.None;
        try { return progressionController.ProcessMemory(completion); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return ProgressionAward.None; }
    }
}
