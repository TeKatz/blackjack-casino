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

        TabItem? selectedTab = ProgressionTabs.SelectedItem as TabItem;
        bool seasonSelected = IsSeasonPassTab(selectedTab);
        if (seasonSelected && selectedTab is not null)
        {
            // RefreshAll -> RefreshSelectedTab still contains the legacy Season renderer. Calling it
            // here used to build the entire reward track once, then InitializeSeasonPassLibrary built
            // the same 100/120-tier track a second time immediately afterwards.
            renderedTabs.Clear();
            if (menuReady && ProgressionRoot.IsLoaded)
            {
                controller.AccruePlaytime();
                RefreshProfileHeader();
                renderedTabs.Add(selectedTab);
            }

            InitializeSeasonPassLibrary();
            InitializeSeasonBankrollPresentation();
            return;
        }

        RefreshAll();

        // The library owns Season Pass rendering. Mark that tab as handled for the generic legacy
        // renderer before the user selects it; SeasonPassTabs_SelectionChanged then materializes it
        // exactly once. Other progression tabs retain the existing lazy-rendering behavior.
        TabItem? seasonTab = ProgressionTabs.Items
            .OfType<TabItem>()
            .FirstOrDefault(IsSeasonPassTab);
        if (seasonTab is not null)
        {
            renderedTabs.Add(seasonTab);
        }

        if (!seasonPassTabHooked)
        {
            ProgressionTabs.SelectionChanged += SeasonPassTabs_SelectionChanged;
            seasonPassTabHooked = true;
        }

        InitializeSeasonBankrollPresentation();
    }

    private static bool IsSeasonPassTab(TabItem? tab) =>
        tab is not null && string.Equals(tab.Tag as string, "Season Pass", StringComparison.Ordinal);

    private void EmbeddedBackButton_Click(object sender, RoutedEventArgs e) => embeddedClose?.Invoke();
}
