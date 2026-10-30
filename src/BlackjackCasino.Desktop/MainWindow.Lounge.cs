using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private BackgroundMusic? loungeBackgroundMusic;

    private void OpenLounge()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen)
        {
            return;
        }
        ProcessLatestCompletedRound();
        SudokuLoungePolish.EnsureInstalled();

        var window = new LoungeWindow(
            preferences.Sudoku,
            state =>
            {
                preferences = preferences with { Sudoku = SudokuProgressState.Normalize(state) };
                if (!testSession)
                {
                    SavePreferences();
                }
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
            });

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
}
