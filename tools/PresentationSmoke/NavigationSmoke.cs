using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BlackjackCasino.Desktop;

internal static class NavigationSmoke
{
    internal static void OpenCovered(MainWindow window, string id)
    {
        var overlay = (Grid?)typeof(MainWindow).GetField("embeddedGameOverlay", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window);
        var lobby = (CasinoLobbyView?)typeof(MainWindow).GetField("lobby", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window);
        if (lobby is null || id == "blackjack") { window.Modes.Open(id); return; }
        void Check(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue) return;
            if (overlay is not { IsVisible: true } || !overlay.Children.OfType<ContentControl>().Any(host => host.Visibility == Visibility.Visible && host.Content is not null))
                throw new InvalidOperationException("Blackjack table exposed before destination was ready: " + id);
        }
        lobby.IsVisibleChanged += Check;
        try { window.Modes.Open(id); }
        finally { lobby.IsVisibleChanged -= Check; }
    }
}
