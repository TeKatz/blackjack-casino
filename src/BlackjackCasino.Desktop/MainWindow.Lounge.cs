using BlackjackCasino.Core.Sudoku;

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
        RecordSudokuCompletions(preferences.Sudoku);

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

        if (lobby is not null)
        {
            lobby.Visibility = System.Windows.Visibility.Collapsed;
        }
        TableModeText.Text = "Chill Lounge";

        loungeBackgroundMusic?.Dispose();
        loungeBackgroundMusic = new BackgroundMusic(Soundtrack.ChillLounge);
        ShowEmbeddedGame(window, "Chill Lounge · The Midnight Club", muteHostMusic: true, closed: () =>
        {
            loungeBackgroundMusic?.Dispose();
            loungeBackgroundMusic = null;
            TableModeText.Text = "Casino lobby";
        });
        loungeBackgroundMusic.Start(preferences.MusicEnabled, preferences.MusicVolume);
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
}
