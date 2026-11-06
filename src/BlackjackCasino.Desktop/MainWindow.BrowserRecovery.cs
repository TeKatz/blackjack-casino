using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private BrowserRecoveryView? browserRecoveryView;
    private bool BrowserRecoveryOpen => browserRecoveryView is not null;

    /// <summary>
    /// Offer recovery without creating LoungeWindow or any WPF game module.
    /// Legacy gameplay remains a deliberate option until native recovery is retired.
    /// </summary>
    private void ShowLoungeBrowserRecovery(bool assetsAvailable)
    {
        if (BrowserRecoveryOpen || embeddedGameShell is not null || ModernLoungeOpen)
            return;

        var view = new BrowserRecoveryView(assetsAvailable, ResolveLoungeBrowserRecovery);
        MountModernSurface(view);
        browserRecoveryView = view;
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        TableModeText.Text = "Chill Lounge · recovery";
        SyncHostMusic();
        modeRegistry.NotifyStateChanged();
        Dispatcher.BeginInvoke(new Action(view.Focus));
    }

    private void ResolveLoungeBrowserRecovery(BrowserRecoveryAction action)
    {
        if (!BrowserRecoveryOpen) return;
        CloseBrowserRecovery();

        switch (action)
        {
            case BrowserRecoveryAction.RetryModern:
                OpenLoungeCore(null);
                break;
            case BrowserRecoveryAction.OpenLegacyGames:
                OpenLoungeCore(null, forceLegacy: true);
                break;
            case BrowserRecoveryAction.ReturnToLobby:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

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
