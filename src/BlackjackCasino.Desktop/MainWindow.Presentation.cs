using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private AreaPresentationSettings CurrentPresentation => preferences.Presentation(
        KniffelPageOpen ? "kniffel" : EmpirePageOpen ? "casino-empire" :
        lobby?.IsVisible == true ? "lobby" : "blackjack");

    private AreaPresentationSettings HostMusicSettings => preferences.Presentation(
        keepHostMusicForEmbeddedGame ? "midnight-merge" :
        EmpirePageOpen ? "casino-empire" : lobby?.IsVisible == true ? "lobby" : "blackjack");

    private readonly CasinoHostMusic hostMusic = new();

    private void SyncHostMusic()
    {
        hostMusic.Sync(HostMusicSettings, !KniffelPageOpen &&
            !ModernLoungeOpen && !ModernReelsOpen && !ModernNineLivesOpen && !ModernNeonVaultOpen && (embeddedGameShell is null || keepHostMusicForEmbeddedGame));
    }

    private void PresentationLoaded(object sender, RoutedEventArgs e)
    {
        InitializeProgressionFeature();
        InitializeCardSetStyling();
        InitializeVaultFeature();
        ApplyCasinoDropdownStyles();
        InitializeLobby();
        hostMusic.Start(HostMusicSettings);
    }

    private void PresentationClosed(object? sender, EventArgs e)
    {
        ShutdownVaultFeature();
        ShutdownProgressionFeature();
        hostMusic.Dispose();
    }

    private void ApplyCasinoDropdownStyles()
    {
        if (Application.Current.TryFindResource(typeof(ComboBox)) is not Style comboStyle
            || Application.Current.TryFindResource(typeof(ComboBoxItem)) is not Style itemStyle) return;
        foreach (ComboBox combo in FindPresentationChildren<ComboBox>(this))
        {
            combo.Style = comboStyle;
            combo.ItemContainerStyle = itemStyle;
        }
    }

    private static IEnumerable<T> FindPresentationChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed) yield return typed;
            foreach (T nested in FindPresentationChildren<T>(child)) yield return nested;
        }
    }
}
