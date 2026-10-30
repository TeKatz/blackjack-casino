using System.Windows;
using System.Windows.Input;
using BlackjackCasino.Core.Kniffel;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private KniffelView? kniffelView;
    private readonly List<(UIElement Element, bool Enabled)> kniffelCoveredElements = [];
    private bool KniffelPageOpen => kniffelView is not null;

    private void OpenKniffel()
    {
        if (session.RoundInProgress || MetaPageOpen || embeddedGameShell is not null || EmpirePageOpen || KniffelPageOpen) return;
        ProcessLatestCompletedRound();
        EnsureEmbeddedGameHost();
        kniffelView = new KniffelView(preferences.Kniffel, state =>
        {
            preferences = preferences with { Kniffel = state };
            if (!testSession) SavePreferences();
        }, CloseKniffel, () => AnimationsEnabled);
        if (lobby is not null) lobby.Visibility = Visibility.Collapsed;
        foreach (UIElement covered in CasinoRoot.Children.OfType<UIElement>().Where(child => child != embeddedGameOverlay))
        {
            kniffelCoveredElements.Add((covered, covered.IsEnabled));
            covered.SetCurrentValue(IsEnabledProperty, false);
        }
        embeddedGameHost!.Content = kniffelView;
        embeddedGameOverlay!.Visibility = Visibility.Visible;
        KeyboardNavigation.SetTabNavigation(kniffelView, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(kniffelView, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetDirectionalNavigation(kniffelView, KeyboardNavigationMode.Contained);
    }

    private void CloseKniffel()
    {
        if (kniffelView is null) return;
        preferences = preferences with { Kniffel = kniffelView.Game.Snapshot };
        kniffelView.Dispose();
        embeddedGameHost!.Content = null;
        kniffelView = null;
        embeddedGameOverlay!.Visibility = Visibility.Collapsed;
        foreach (var (element, enabled) in kniffelCoveredElements) element.SetCurrentValue(IsEnabledProperty, enabled);
        kniffelCoveredElements.Clear();
        if (lobby is not null) lobby.Visibility = Visibility.Visible;
        TableModeText.Text = "Casino lobby";
        if (!testSession) SavePreferences();
        lobby?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
}
