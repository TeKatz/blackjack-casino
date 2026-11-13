using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private CasinoExitOverlay? exitOverlay;
    private Window? exitDialog;

    private void InitializeCasinoExitOverlay()
    {
        if (exitDialog is not null || !CasinoExitOverlay.IsAvailable) return;

        exitOverlay = new CasinoExitOverlay(
            // Dispose WebView2 only after its message callback has returned.
            () => Dispatcher.BeginInvoke(new Action(() => FinishCasinoExitConfirmation(true))),
            () => Dispatcher.BeginInvoke(new Action(() => FinishCasinoExitConfirmation(false))));

        // An owned top-level surface avoids WebView2 HWND z-order conflicts
        // with the lobby and game browser surfaces. The content stays React.
        exitDialog = new Window
        {
            Owner = this,
            Title = "The Midnight Club",
            Width = 660,
            Height = 460,
            MinWidth = 660,
            MinHeight = 460,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false,
            Background = Brushes.Black,
            Content = exitOverlay,
        };
        exitDialog.Closed += (_, _) =>
        {
            exitOverlay?.Dispose();
            exitOverlay = null;
            exitDialog = null;
            exitConfirmationOpen = false;
        };
    }

    private void FinishCasinoExitConfirmation(bool accepted)
    {
        if (!exitConfirmationOpen) return;
        if (accepted) exitConfirmationAccepted = true;
        exitDialog?.Close();
        exitConfirmationOpen = false;
        if (accepted) Close();
    }

    private void RequestCasinoExitConfirmation()
    {
        InitializeCasinoExitOverlay();
        if (exitDialog is null || exitOverlay is null)
        {
            // Missing frontend must never strand the host close state.
            exitConfirmationOpen = false;
            return;
        }

        exitOverlay.Show(preferences.Language == UiLanguagePreference.German);
        // An actual modal window blocks input to its owner while the user
        // decides, without competing for embedded WebView2 hit-testing.
        exitDialog.ShowDialog();
    }
}
