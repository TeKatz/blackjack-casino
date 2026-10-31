using System.Windows;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void OpenMidnightMerge()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen) return;
        ProcessLatestCompletedRound();
        void PersistMerge(MergeGameState candidate)
        {
            var updated = preferences with { MidnightMerge = candidate, Empire = empire?.Snapshot ?? preferences.Empire, Bankroll = session.Bankroll };
            if (!testSession) PlayerStateStore.SaveOrThrow(updated);
            preferences = updated;
        }
        MergeBoard board;
        try
        {
            board = new MergeBoard(preferences.MidnightMerge, PersistMerge);
            PersistMerge(board.Snapshot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = Localization.T("Merge.SaveFailed");
            return;
        }

        if (progressionController is not null)
        {
            try
            {
                MergeProfileSyncResult initialSync = progressionController.ProcessMidnightMerge(board.Snapshot);
                if (initialSync.Changed) RefreshProfileButton();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                StatusText.Text = Localization.T("Merge.ProgressionSaveFailed");
            }
        }

        var shell = new Window { Title = Localization.T("Merge.Title"), Width = 1500, Height = 900 };
        var view = new MidnightMergeView(
            board,
            () => SoundEnabled,
            () => AnimationsEnabled,
            shell.Close,
            language =>
            {
                preferences = preferences with { Language = language };
                Localization.SetLanguage(language);
                SavePreferences();
            },
            state =>
            {
                if (progressionController is null) return MergeProfileSyncResult.None;
                MergeProfileSyncResult result = progressionController.ProcessMidnightMerge(state);
                if (result.Changed) RefreshProfileButton();
                return result;
            },
            () => progressionController?.Profile);
        shell.Content = view;
        shell.Closed += (_, _) => view.Dispose();
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        ShowEmbeddedGame(shell, "Midnight Merge · Blackjack Casino", muteHostMusic: false, closed: () => { });
    }
}
