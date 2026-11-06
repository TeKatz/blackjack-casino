using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private ModernLoungeView? modernLoungeView;
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
        modernLoungeView = new ModernLoungeView(CreateModernLoungeSnapshot,
            OnModernLoungeGameSelected, CloseModernLounge, OnModernLoungeFailed);
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
        CloseModernLounge();
        // The native gameplay screens are retained until each game's React bridge
        // has been tested; the new hub is not allowed to mutate game state.
        Dispatcher.BeginInvoke(new Action(() => OpenLoungeCore(id)), DispatcherPriority.Loaded);
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
