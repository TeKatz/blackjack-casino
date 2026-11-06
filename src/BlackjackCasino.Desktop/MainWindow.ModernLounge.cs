using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Core.Solitaire;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernLoungeView? modernLoungeView;
    private ModernSudokuSession? modernSudokuSession;
    private ModernSudokuService? modernSudoku;
    private ModernSolitaireSession? modernSolitaireSession;
    private ModernSolitaireService? modernSolitaire;
    private readonly List<(UIElement Element, bool Enabled)> modernLoungeCoveredElements = [];
    private bool ModernLoungeOpen => modernLoungeView is not null;

    private object CreateModernLoungeSnapshot()
    {
        var games = LoungeHostedGameRegistry.Snapshot.Select(module => new
        {
            module.Definition.Id,
            Status = module.Definition.Status?.Invoke(),
        }).ToList();
        games.Insert(0, new
        {
            Id = "sudoku",
            Status = (string?)null,
        });
        return new
        {
            Version = 1,
            Session = modernSudokuSession?.Id ?? string.Empty,
            Revision = modernSudokuSession?.Revision ?? 0L,
            Sudoku = modernSudoku?.Snapshot(),
            SolitaireSession = modernSolitaireSession?.Id ?? string.Empty,
            SolitaireRevision = modernSolitaireSession?.Revision ?? 0L,
            Solitaire = modernSolitaire?.Snapshot(),
            Language = Localization.Language == UiLanguagePreference.German ? "de" : "en",
            MusicEnabled = preferences.Presentation("chill-lounge").MusicEnabled,
            SoundEnabled = preferences.Presentation("chill-lounge").SoundEnabled,
            AnimationsEnabled = preferences.Presentation("chill-lounge").AnimationsEnabled,
            Games = games,
        };
    }

    private void OpenModernLounge()
    {
        if (ModernLoungeOpen || embeddedGameShell is not null) return;
        EnsureEmbeddedGameHost();
        modernSudokuSession = new ModernSudokuSession();
        modernSolitaireSession = new ModernSolitaireSession();
        modernLoungeView = new ModernLoungeView(CreateModernLoungeSnapshot,
            OnModernLoungeGameSelected, ApplyModernSudokuCommand, CheckpointModernSudoku,
            ApplyModernSolitaireCommand, CheckpointModernSolitaire,
            CloseModernLounge, OnModernLoungeFailed);
        foreach (UIElement element in CasinoRoot.Children.OfType<UIElement>()
            .Where(element => element != embeddedGameOverlay))
        {
            modernLoungeCoveredElements.Add((element, element.IsEnabled));
            element.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Content = modernLoungeView;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        KeyboardNavigation.SetTabNavigation(modernLoungeView, KeyboardNavigationMode.Cycle);
        loungeBackgroundMusic?.Dispose();
        loungeBackgroundMusic = new BackgroundMusic(Soundtrack.ChillLounge);
        AreaPresentationSettings settings = preferences.Presentation("chill-lounge");
        loungeBackgroundMusic.Start(settings.MusicEnabled, settings.MusicVolume);
        TableModeText.Text = "Chill Lounge";
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }

    private void OnModernLoungeGameSelected(string id)
    {
        if (!ModernLoungeOpen) return;
        if (id == "sudoku")
        {
            modernSudoku ??= new ModernSudokuService(preferences.Sudoku,
                SaveModernSudokuProgress, () => preferences.SudokuShowTimer,
                value => SaveModernSudokuTimer(value),
                () => preferences.SudokuShowGuides,
                value => SaveModernSudokuGuides(value), () => sudokuRewardNotice);
            modernLoungeView?.Refresh();
            return;
        }
        if (id == "solitaire")
        {
            modernSolitaire ??= new ModernSolitaireService(preferences.Solitaire,
                SaveModernSolitaireProgress,
                () => progressionController?.Profile.Solitaire ?? new SolitaireStatistics(),
                RecordSolitaireStart, RecordSolitaireAbandon, RecordSolitaireCompletion);
            modernLoungeView?.Refresh();
            return;
        }
        CloseModernLounge();
        // Other native gameplay screens are retained until each game's React bridge
        // has been tested; the new hub is not allowed to mutate game state.
        Dispatcher.BeginInvoke(new Action(() => OpenLoungeCore(id)), DispatcherPriority.Loaded);
    }

    private ModernSudokuReply ApplyModernSudokuCommand(ModernSudokuCommand command)
    {
        if (modernSudokuSession is null || modernSudoku is null)
            return new ModernSudokuReply(command.RequestId, false, "Sudoku is not open.", 0);
        return modernSudokuSession.Execute(command, intent =>
        {
            try
            {
                (bool success, string message) = modernSudoku.Execute(intent);
                if (success && intent.Action == "sudoku.exit") modernSudoku = null;
                return (success, message);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or ArgumentException or InvalidOperationException or OverflowException)
            {
                return (false, ex is IOException or UnauthorizedAccessException
                    ? "Sudoku could not be saved. Your last committed board is unchanged."
                    : ex.Message);
            }
        });
    }

    private ModernSolitaireReply ApplyModernSolitaireCommand(ModernSolitaireCommand command)
    {
        if (modernSolitaireSession is null || modernSolitaire is null)
            return new ModernSolitaireReply(command.RequestId, false, "Solitaire is not open.", 0);
        return modernSolitaireSession.Execute(command, intent =>
        {
            try
            {
                (bool success, string message) = modernSolitaire.Execute(intent);
                if (success && intent.Action == "solitaire.exit") modernSolitaire = null;
                return (success, message);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                or InvalidOperationException or ArgumentException or OverflowException)
            {
                return (false, ex is IOException or UnauthorizedAccessException
                    ? "Solitaire could not be saved. The last committed deal is preserved."
                    : ex.Message);
            }
        });
    }

    private void SaveModernSolitaireProgress(SolitaireProgressState state)
    {
        AppPreferences previous = preferences;
        preferences = preferences with { Solitaire = SolitaireProgressState.Normalize(state) };
        try { if (!testSession) SavePreferences(); }
        catch { preferences = previous; throw; }
    }

    private void CheckpointModernSolitaire()
    {
        try { modernSolitaire?.CheckpointClock(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "Solitaire timer checkpoint could not be saved: " + ex.Message;
        }
    }

    private void SaveModernSudokuProgress(SudokuProgressState state)
    {
        AppPreferences previous = preferences;
        preferences = preferences with { Sudoku = state };
        try { if (!testSession) SavePreferences(); }
        catch { preferences = previous; throw; }
        RecordSudokuCompletions(state);
    }

    private void SaveModernSudokuTimer(bool value)
    {
        AppPreferences old = preferences;
        preferences = preferences with { SudokuShowTimer = value };
        try { if (!testSession) SavePreferences(); }
        catch { preferences = old; throw; }
    }

    private void SaveModernSudokuGuides(bool value)
    {
        AppPreferences old = preferences;
        preferences = preferences with { SudokuShowGuides = value };
        try { if (!testSession) SavePreferences(); }
        catch { preferences = old; throw; }
    }

    private void CheckpointModernSudoku()
    {
        try { modernSudoku?.CheckpointClock(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "Sudoku timer checkpoint could not be saved: " + ex.Message;
        }
    }

    private void OnModernLoungeFailed()
    {
        if (modernLoungeView?.Failed != true || !IsLoaded) return;
        CloseModernLounge();
        StatusText.Text = "Chill Lounge browser unavailable. Opening the original saved games.";
        Dispatcher.BeginInvoke(new Action(() => OpenLoungeCore(null, forceLegacy: true)),
            DispatcherPriority.Loaded);
    }

    private void CloseModernLounge()
    {
        if (modernLoungeView is null) return;
        try { modernSudoku?.Stop(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "Sudoku time checkpoint could not be saved: " + ex.Message;
        }
        modernSudoku = null;
        modernSudokuSession = null;
        try { modernSolitaire?.Stop(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = "Solitaire time checkpoint could not be saved: " + ex.Message;
        }
        modernSolitaire = null;
        modernSolitaireSession = null;
        modernLoungeView.Dispose();
        modernLoungeView = null;
        if (embeddedGameHost is not null) embeddedGameHost.Content = null;
        if (embeddedGameOverlay is not null) embeddedGameOverlay.Visibility = Visibility.Collapsed;
        foreach ((UIElement element, bool enabled) in modernLoungeCoveredElements)
            element.SetCurrentValue(IsEnabledProperty, enabled);
        modernLoungeCoveredElements.Clear();
        LoungeHostedGameRegistry.ClearSessionModules();
        loungeBackgroundMusic?.Dispose();
        loungeBackgroundMusic = null;
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }
}
