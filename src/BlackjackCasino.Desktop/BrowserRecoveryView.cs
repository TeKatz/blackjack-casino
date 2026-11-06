using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Minimal non-browser recovery screen; independent of all legacy WPF game views.
/// The main host decides what to do with an explicitly selected recovery action.
/// </summary>
internal sealed class BrowserRecoveryView : UserControl
{
    private readonly BrowserRecoverySession session;

    internal Button RetryButton { get; }
    internal Button LegacyButton { get; }
    internal Button LobbyButton { get; }

    internal BrowserRecoveryView(bool modernAssetsAvailable, Action<BrowserRecoveryAction> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        session = new BrowserRecoverySession(modernAssetsAvailable);
        bool german = Localization.Language == UiLanguagePreference.German;
        string title = german ? "CHILL LOUNGE — WIEDERHERSTELLUNG" : "CHILL LOUNGE — RECOVERY";
        string detail = modernAssetsAvailable
            ? german
                ? "Die Browser-Oberfläche konnte nicht gestartet werden. Deine gespeicherten Spiele bleiben erhalten."
                : "The browser interface could not start. Your saved games remain available."
            : german
                ? "Die lokalen Dateien der Browser-Oberfläche fehlen. Du kannst die ursprünglichen Spiele öffnen oder zur Lobby zurückkehren."
                : "The local browser assets are missing. You can open the original games or return to the lobby.";

        Background = new SolidColorBrush(Color.FromRgb(7, 23, 20));
        var card = new StackPanel
        {
            MaxWidth = 690,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(24),
        };
        card.Children.Add(new TextBlock
        {
            Text = title,
            Foreground = new SolidColorBrush(Color.FromRgb(235, 202, 134)),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 20),
        });
        card.Children.Add(new TextBlock
        {
            Text = detail,
            Foreground = Brushes.WhiteSmoke,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 25),
        });

        RetryButton = MakeButton("RecoveryRetryButton",
            german ? "Erneut versuchen" : "Retry modern Lounge",
            BrowserRecoveryAction.RetryModern, selected);
        RetryButton.IsEnabled = modernAssetsAvailable;
        LegacyButton = MakeButton("RecoveryLegacyButton",
            german ? "Ursprüngliche Spiele öffnen" : "Open original games",
            BrowserRecoveryAction.OpenLegacyGames, selected);
        LobbyButton = MakeButton("RecoveryLobbyButton",
            german ? "Zur Casino-Lobby" : "Return to casino lobby",
            BrowserRecoveryAction.ReturnToLobby, selected);

        var actions = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        actions.Children.Add(RetryButton);
        actions.Children.Add(LegacyButton);
        actions.Children.Add(LobbyButton);
        card.Children.Add(actions);

        Content = new Grid
        {
            Children = { card },
        };
        Focusable = true;
    }

    private Button MakeButton(string name, string label, BrowserRecoveryAction action,
        Action<BrowserRecoveryAction> selected)
    {
        var button = new Button
        {
            Name = name,
            Content = label,
            MinWidth = 255,
            MinHeight = 44,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 4),
        };
        AutomationProperties.SetName(button, label);
        button.Click += (_, _) =>
        {
            if (!session.TryChoose(action)) return;
            RetryButton.IsEnabled = false;
            LegacyButton.IsEnabled = false;
            LobbyButton.IsEnabled = false;
            selected(action);
        };
        return button;
    }
}
