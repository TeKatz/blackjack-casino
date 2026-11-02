using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

internal static class CasinoUxEnhancements
{
    private static readonly DependencyProperty EnhancedProperty = DependencyProperty.RegisterAttached(
        "Enhanced", typeof(bool), typeof(CasinoUxEnhancements), new PropertyMetadata(false));

    private static bool installed;

    internal static void Install()
    {
        if (installed) return;
        installed = true;
        EventManager.RegisterClassHandler(typeof(Button), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnButtonLoaded), true);
        EventManager.RegisterClassHandler(typeof(CasinoLobbyView), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnLobbyLoaded), true);
    }

    private static void OnButtonLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || (bool)button.GetValue(EnhancedProperty)) return;
        if (button.Name is not ("PrestigeButton" or "EmpirePrestigeButton")) return;

        button.SetValue(EnhancedProperty, true);
        bool empire = button.Name == "EmpirePrestigeButton";
        button.MinHeight = empire ? 48 : 44;
        button.Padding = new Thickness(empire ? 15 : 18, 9, empire ? 15 : 18, 9);
        button.FontFamily = new FontFamily("Georgia");
        button.FontWeight = FontWeights.Bold;
        button.FontSize = empire ? 12.5 : 12;
        button.Foreground = Ink("#FFF0C4");
        button.BorderThickness = new Thickness(1.5);
        button.BorderBrush = new LinearGradientBrush(Ink("#F4D98A").Color, Ink("#9A6727").Color, 25);
        button.Background = PremiumBackground(false);
        button.Effect = PremiumGlow(.34);
        button.Cursor = CasinoCursors.Hand;

        button.MouseEnter += (_, _) =>
        {
            if (!button.IsEnabled) return;
            button.Background = PremiumBackground(true);
            button.BorderBrush = new LinearGradientBrush(Ink("#FFF0AF").Color, Ink("#C9943D").Color, 25);
            button.Effect = PremiumGlow(.58);
        };
        button.MouseLeave += (_, _) =>
        {
            button.Background = PremiumBackground(false);
            button.BorderBrush = new LinearGradientBrush(Ink("#F4D98A").Color, Ink("#9A6727").Color, 25);
            button.Effect = PremiumGlow(.34);
        };
    }

    private static void OnLobbyLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not CasinoLobbyView lobby || (bool)lobby.GetValue(EnhancedProperty)) return;
        lobby.SetValue(EnhancedProperty, true);
        if (lobby.Content is not Grid root || root.RowDefinitions.Count == 0) return;

        if (root.Children.OfType<StackPanel>().FirstOrDefault() is StackPanel heading)
        {
            Thickness margin = heading.Margin;
            heading.Margin = new Thickness(margin.Left, margin.Top, Math.Max(175, margin.Right), margin.Bottom);
        }

        var exit = new Button
        {
            Name = "LobbyExitGameButton",
            Content = "EXIT GAME",
            Width = 126,
            Height = 38,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 10, 0),
            Padding = new Thickness(12, 6, 12, 6),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#EFD8CE"),
            Background = new LinearGradientBrush(Ink("#301D1F").Color, Ink("#17171A").Color, 90),
            BorderBrush = Ink("#755052"),
            BorderThickness = new Thickness(1),
            Cursor = CasinoCursors.Hand,
            ToolTip = "Close Blackjack Casino",
        };
        exit.MouseEnter += (_, _) =>
        {
            exit.Background = new LinearGradientBrush(Ink("#4A2629").Color, Ink("#22191B").Color, 90);
            exit.BorderBrush = Ink("#B8756E");
        };
        exit.MouseLeave += (_, _) =>
        {
            exit.Background = new LinearGradientBrush(Ink("#301D1F").Color, Ink("#17171A").Color, 90);
            exit.BorderBrush = Ink("#755052");
        };
        exit.Click += (_, _) =>
        {
            Window? owner = Window.GetWindow(lobby) ?? Application.Current.MainWindow;
            if (!PremiumConfirmationWindow.ConfirmExit(owner)) return;
            if (owner is BlackjackCasino.Desktop.MainWindow main) main.Close();
            else Application.Current.Shutdown();
        };
        Grid.SetRow(exit, 0);
        Panel.SetZIndex(exit, 50);
        root.Children.Add(exit);
    }

    private static LinearGradientBrush PremiumBackground(bool hover)
    {
        Color first = hover ? Ink("#584019").Color : Ink("#392A17").Color;
        Color second = hover ? Ink("#1D211F").Color : Ink("#121A18").Color;
        var brush = new LinearGradientBrush(first, second, 90);
        brush.GradientStops.Insert(1, new GradientStop(hover ? Ink("#76572A").Color : Ink("#4E391E").Color, .46));
        return brush;
    }

    private static DropShadowEffect PremiumGlow(double opacity) => new()
    {
        Color = Ink("#E8B957").Color,
        BlurRadius = 15,
        ShadowDepth = 0,
        Opacity = opacity,
    };

    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
