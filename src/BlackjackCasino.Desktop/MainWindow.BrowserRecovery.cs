using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private BrowserRecoveryView? browserRecoveryView;
    private bool BrowserRecoveryOpen => browserRecoveryView is not null;

    /// <summary>
    /// Browser-independent error surface. The only way to start a native game is
    /// the explicit legacy button, offered only where a real fallback exists.
    /// A retry constructs a fresh WebView; it never replays a wager or command.
    /// </summary>
    private void ShowBrowserRecovery(string title, bool assetsAvailable, Action retry,
        Action? openLegacyGames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(retry);
        if (BrowserRecoveryOpen || embeddedGameShell is not null
            || modernGameSurface?.IsOccupied == true)
            return;

        var view = new BrowserRecoveryView(title, assetsAvailable,
            legacyGamesAvailable: openLegacyGames is not null, selected: action =>
            {
                if (!BrowserRecoveryOpen) return;
                CloseBrowserRecovery();
                switch (action)
                {
                    case BrowserRecoveryAction.RetryModern:
                        retry();
                        break;
                    case BrowserRecoveryAction.OpenLegacyGames when openLegacyGames is not null:
                        openLegacyGames();
                        break;
                    case BrowserRecoveryAction.ReturnToLobby:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                }
            });
        MountModernSurface(view);
        browserRecoveryView = view;
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        TableModeText.Text = title + " · recovery";
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
        Dispatcher.BeginInvoke(new Action(() => { view.Focus(); }));
    }

    private void ShowLoungeBrowserRecovery(bool assetsAvailable)
        => ShowBrowserRecovery("Chill Lounge", assetsAvailable,
            () => OpenLoungeCore(null),
            () => OpenLoungeCore(null, forceLegacy: true));

    private void CloseBrowserRecovery()
    {
        BrowserRecoveryView? view = browserRecoveryView;
        if (view is null) return;

        UnmountModernSurface(view);
        browserRecoveryView = null;
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
    }
}
