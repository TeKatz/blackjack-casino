using System.Windows;
using System.Windows.Controls;

namespace BlackjackCasino.Desktop;

internal partial class ProgressionWindow
{
    private Action? embeddedClose;

    /// <summary>Call before moving Content into the casino's profile/progression host.</summary>
    internal void ConfigureEmbeddedPresentation(Action close)
    {
        ArgumentNullException.ThrowIfNull(close);
        embeddedClose = close;
        NativeTitleBar.Visibility = Visibility.Collapsed;
        EmbeddedTitleBar.Visibility = Visibility.Visible;
        ResizeMode = ResizeMode.NoResize;
    }

    // This belongs to the content, not Window.Loaded/OnContentRendered: a detached
    // ProgressionRoot is loaded by the main window without ever showing this Window.
    private void ProgressionRoot_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeCollections();
        RefreshAll();

        // Keep the menu's lazy-loading contract intact. Challenges/Shop must not construct the
        // hidden 100-tier Season Pass just because the progression shell was embedded.
        if (ProgressionTabs.SelectedItem is TabItem tab
            && string.Equals(tab.Tag as string, "Season Pass", StringComparison.Ordinal))
        {
            InitializeSeasonPassLibrary();
        }
        else if (!seasonPassTabHooked)
        {
            ProgressionTabs.SelectionChanged += SeasonPassTabs_SelectionChanged;
            seasonPassTabHooked = true;
        }
    }

    private void EmbeddedBackButton_Click(object sender, RoutedEventArgs e) => embeddedClose?.Invoke();
}
