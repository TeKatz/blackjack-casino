using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoLobbyView : UserControl
{
    private readonly CasinoModeRegistry modes;
    private readonly UniformGrid cards = new() { Columns = 4, Width = 1440 };
    private static readonly string[] Order = ["blackjack", "kniffel", "chill-lounge", "casino-empire", "midnight-reels", "neon-vault", "nine-lives", "midnight-manor"];

    internal CasinoLobbyView(CasinoModeRegistry modes, Action<string> progression)
    {
        this.modes = modes;
        Background = new LinearGradientBrush(Color.FromRgb(10,17,22), Color.FromRgb(23,32,28), 80);
        var root = new DockPanel { Margin = new Thickness(30, 14, 30, 10) };
        var footer = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
        foreach (string section in new[] { "Season Pass", "Cosmetic Shop", "Challenges", "Overview" })
        {
            string selected = section;
            var button = new Button { Content = section, Margin = new Thickness(5), Padding = new Thickness(20, 8, 20, 8) };
            button.Click += (_, _) => progression(selected);
            footer.Children.Add(button);
        }
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        var heading = new StackPanel { Margin = new Thickness(10, 0, 10, 10) };
        heading.Children.Add(new TextBlock { Text = "THE MIDNIGHT CLUB", Foreground = Ink("#D6B478"), FontSize = 11 });
        heading.Children.Add(new TextBlock { Text = "A different kind of night.", FontFamily = new FontFamily("Georgia"), FontSize = 30, Foreground = Ink("#F7EBD5"), Margin = new Thickness(0, 4, 0, 4) });
        heading.Children.Add(new TextBlock { Text = "Choose your table. Find your rhythm. Make yourself at home.", Foreground = Ink("#B8C7BE"), FontSize = 13 });
        DockPanel.SetDock(heading, Dock.Top);
        root.Children.Add(heading);
        root.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = cards, VerticalAlignment = VerticalAlignment.Center });
        Content = root;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Refresh();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        modes.Changed -= Refresh;
        modes.Changed += Refresh;
        Localization.LanguageChanged -= Localization_LanguageChanged;
        Localization.LanguageChanged += Localization_LanguageChanged;
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        modes.Changed -= Refresh;
        Localization.LanguageChanged -= Localization_LanguageChanged;
    }

    private void Localization_LanguageChanged(object? sender, EventArgs e) => Refresh();

    private void Refresh()
    {
        cards.Children.Clear();
        cards.Rows = Math.Max(1, (modes.Modes.Count + 3) / 4);
        cards.Height = cards.Rows * 266;
        foreach (CasinoMode mode in modes.Modes.OrderBy(m => Array.IndexOf(Order, m.Id) is int index && index >= 0 ? index : int.MaxValue))
        {
            Brush accent = Ink(mode.Accent);
            var panel = new Grid { Height = 246 };
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(94) });
            panel.RowDefinitions.Add(new RowDefinition());
            var art = new Grid { ClipToBounds = true, Background = new LinearGradientBrush(((SolidColorBrush)accent).Color, Color.FromRgb(9,17,22), 65) };
            if (mode.Artwork is not null) art.Children.Add(mode.Artwork());
            else art.Children.Add(new TextBlock { Text = mode.Emblem, FontSize = 48, Foreground = Ink("#F4DFC0"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            art.Children.Add(new Border { Background = Ink("#990B1718"), VerticalAlignment = VerticalAlignment.Bottom,
                Child = new TextBlock { Text = Localized(mode.Category).ToUpperInvariant(), FontSize = 10, Foreground = Ink("#F4DFC0"), Margin = new Thickness(14, 3, 14, 3) } });
            panel.Children.Add(art);
            var text = new StackPanel { Margin = new Thickness(16, 9, 16, 9) };
            text.Children.Add(new TextBlock { Text = Localized(mode.Subtitle).ToUpperInvariant(), FontSize = 10, Foreground = accent });
            text.Children.Add(new TextBlock { Text = mode.Title, FontFamily = new FontFamily("Georgia"), FontSize = 25, Foreground = Ink("#F3E6D1"), Margin = new Thickness(0, 3, 0, 6) });
            text.Children.Add(new TextBlock { Text = Localized(mode.Description), FontSize = 12, Foreground = Ink("#ADBCB6"), TextWrapping = TextWrapping.Wrap, Height = 50 });
            text.Children.Add(new TextBlock { Text = modes.CanOpen(mode.Id) ? Localization.T("Lobby.TakeSeat") : Localization.T("Lobby.CurrentlyOpen"), FontSize = 11, FontWeight = FontWeights.Bold, Foreground = Ink("#E7CCA0"), Margin = new Thickness(0, 7, 0, 0) });
            Grid.SetRow(text, 1);
            panel.Children.Add(text);
            var primary = new Button { Name = "Mode_" + mode.Id.Replace('-', '_'), Tag = mode.Id, Padding = new Thickness(0), Margin = new Thickness(0), Content = panel,
                Background = Ink("#111F20"), BorderBrush = accent, HorizontalContentAlignment = HorizontalAlignment.Stretch, IsEnabled = modes.CanOpen(mode.Id) };
            primary.Click += (_, _) => modes.Open(mode.Id);
            var wrapper = new Grid { Margin = new Thickness(8) };
            wrapper.Children.Add(primary);
            if (modes.HasSecondary(mode.Id))
            {
                var secondary = new Button { Name = "ModeSecondary_" + mode.Id.Replace('-', '_'), Tag = mode.Id + "-secondary", Content = Localization.T("Lobby.NewWindow"),
                    ToolTip = modes.SecondaryLabel(mode.Id), FontSize = 10, Padding = new Thickness(9, 5, 9, 5), Margin = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, IsEnabled = modes.CanOpenSecondary(mode.Id) };
                secondary.Click += (_, _) => modes.OpenSecondary(mode.Id);
                wrapper.Children.Add(secondary);
            }
            cards.Children.Add(wrapper);
        }
    }

    private static string Localized(string source) =>
        Localization.KeyForEnglishSource(source) is string key ? Localization.T(key) : source;

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
