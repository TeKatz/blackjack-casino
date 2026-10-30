using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void OpenLounge()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen)
        {
            return;
        }
        ProcessLatestCompletedRound();

        var window = new LoungeWindow(preferences.Sudoku, state =>
        {
            preferences = preferences with { Sudoku = SudokuProgressState.Normalize(state) };
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
        ShowEmbeddedGame(window, "Chill Lounge · The Midnight Club", muteHostMusic: false, closed: () =>
        {
            TableModeText.Text = "Casino lobby";
        });
    }
}
