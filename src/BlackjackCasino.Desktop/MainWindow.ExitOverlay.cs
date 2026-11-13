using System.Windows;
using System.Windows.Controls;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private CasinoExitOverlay? exitOverlay;

    private void InitializeCasinoExitOverlay()
    {
        if (exitOverlay is not null || !CasinoExitOverlay.IsAvailable) return;
        exitOverlay = new CasinoExitOverlay(
            () =>
            {
                exitConfirmationOpen = false;
                exitConfirmationAccepted = true;
                Close();
            },
            () => exitConfirmationOpen = false);
        Grid.SetRow(exitOverlay, 0);
        Grid.SetRowSpan(exitOverlay, 2);
        Grid.SetColumnSpan(exitOverlay, 2);
        Panel.SetZIndex(exitOverlay, 3000);
        CasinoRoot.Children.Add(exitOverlay);
        Closed += (_, _) => exitOverlay?.Dispose();
    }

    private void RequestCasinoExitConfirmation()
    {
        InitializeCasinoExitOverlay();
        if (exitOverlay is null)
        {
            // The player stays in control if the packaged React page is absent;
            // never silently exit and never reintroduce a stock WPF MessageBox.
            exitConfirmationOpen = false;
            return;
        }
        exitOverlay.Show(preferences.Language == UiLanguagePreference.German);
    }
}
