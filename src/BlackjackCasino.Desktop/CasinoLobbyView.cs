using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoLobbyView : UserControl
{
    private readonly CasinoModeRegistry modes;
    private readonly UniformGrid cards = new() { Columns = 4, Width = 1280 };
    private static readonly string[] Order = ["blackjack", "kniffel", "chill-lounge", "casino-empire", "midnight-reels", "neon-vault", "nine-lives", "midnight-manor"];

    internal CasinoLobbyView(CasinoModeRegistry modes, Action<string> progression)
    {
        this.modes = modes;
        Background = new LinearGradientBrush(Color.FromRgb(10, 17, 22), Color.FromRgb(23, 32, 28), 80);

        var root = new Grid { Margin = new Thickness(30, 14, 30, 14) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());

        var heading = new StackPanel { Margin = new Thickness(10, 0, 10, 12) };
        heading.Children.Add(new TextBlock { Text = "THE MIDNIGHT CLUB", Foreground = Ink("#D6B478"), FontSize = 11 });
        heading.Children.Add(new TextBlock
        {
            Text = "A different kind of night.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 30,
            Foreground = Ink("#F7EBD5"),
            Margin = new Thickness(0, 4, 0, 4),
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Choose your table. Find your rhythm. Make yourself at home.",
            Foreground = Ink("#B8C7BE"),
            FontSize = 13,
        });
        root.Children.Add(heading);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(264) });
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var gamesViewport = new Viewbox
        {
            Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.Both,
            Child = cards,
            VerticalAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        body.Children.Add(gamesViewport);

        Border hub = BuildProgressionHub(progression);
        Grid.SetColumn(hub, 2);
        body.Children.Add(hub);

        Content = root;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Refresh();
    }

    private static Border BuildProgressionHub(Action<string> progression)
    {
        var shell = new Border
        {
            Name = "LobbyOverviewHub",
            Background = new LinearGradientBrush(Ink("#10231F").Color, Ink("#0C1918").Color, 90),
            BorderBrush = Ink("#38564D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(15, 16, 15, 15),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 8, 0, 8),
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "YOUR MIDNIGHT CLUB",
            Foreground = Ink("#D0B36F"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "At a glance.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 24,
            Foreground = Ink("#F3E6D1"),
            Margin = new Thickness(0, 4, 0, 4),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Rewards, style, goals and your casino career — all in one corner.",
            FontSize = 11,
            Foreground = Ink("#91A69D"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12),
        });

        stack.Children.Add(ProgressionTile(
            "LobbyProgression_SeasonPass",
            "Season Pass",
            "SEASON REWARDS",
            "Follow your current season track and claim unlocked rewards.",
            "✦",
            "#D2B363",
            progression));
        stack.Children.Add(ProgressionTile(
            "LobbyProgression_CosmeticShop",
            "Cosmetic Shop",
            "PERSONAL STYLE",
            "Browse card sets, portraits and other ways to make the club yours.",
            "◆",
            "#A78AC8",
            progression));
        stack.Children.Add(ProgressionTile(
            "LobbyProgression_Album",
            "Collector's Album",
            "CARD COLLECTION",
            "Open packs, complete themed collections and inspect discovered editions.",
            "▣",
            "#D7B568",
            progression));
        stack.Children.Add(ProgressionTile(
            "LobbyProgression_Challenges",
            "Challenges",
            "GOALS & REWARDS",
            "See active challenges, completed goals and rewards ready to claim.",
            "✓",
            "#78B69A",
            progression));
        stack.Children.Add(ProgressionTile(
            "LobbyProgression_Overview",
            "Overview",
            "CAREER OVERVIEW",
            "Review your progress, collection and the wider Midnight Club journey.",
            "◎",
            "#7FA9C2",
            progression));

        shell.Child = stack;
        return shell;
    }

    private static Button ProgressionTile(
        string name,
        string section,
        string eyebrow,
        string description,
        string emblem,
        string accentHex,
        Action<string> progression)
    {
        Brush accent = Ink(accentHex);
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(35) });
        content.ColumnDefinitions.Add(new ColumnDefinition());

        var emblemFrame = new Border
        {
            Width = 29,
            Height = 29,
            CornerRadius = new CornerRadius(8),
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            Background = Ink("#152B26"),
            VerticalAlignment = VerticalAlignment.Top,
            Child = new TextBlock
            {
                Text = emblem,
                FontSize = 15,
                Foreground = accent,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
        content.Children.Add(emblemFrame);

        var copy = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        copy.Children.Add(new TextBlock
        {
            Text = eyebrow,
            FontSize = 8,
            FontWeight = FontWeights.SemiBold,
            Foreground = accent,
        });
        copy.Children.Add(new TextBlock
        {
            Text = section,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 17,
            Foreground = Ink("#F1E4CF"),
            Margin = new Thickness(0, 2, 0, 3),
        });
        copy.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 9.5,
            Foreground = Ink("#94A79F"),
            TextWrapping = TextWrapping.Wrap,
        });
        copy.Children.Add(new TextBlock
        {
            Text = "OPEN  →",
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#DCC794"),
            Margin = new Thickness(0, 7, 0, 0),
        });
        Grid.SetColumn(copy, 1);
        content.Children.Add(copy);

        var button = new Button
        {
            Name = name,
            Content = content,
            Tag = section,
            Background = Ink("#112420"),
            BorderBrush = Ink("#34544A"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 10, 10, 10),
            Margin = new Thickness(0, 0, 0, 9),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Cursor = CasinoCursors.Hand,
        };
        button.Click += (_, _) => progression(section);
        return button;
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
            string Label(string source) => mode.UseResourceKeys ? Localization.T(source) : Localized(source);
            Brush accent = Ink(mode.Accent);
            var panel = new Grid { Height = 246 };
            panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(94) });
            panel.RowDefinitions.Add(new RowDefinition());
            var art = new Grid
            {
                ClipToBounds = true,
                Background = new LinearGradientBrush(((SolidColorBrush)accent).Color, Color.FromRgb(9, 17, 22), 65),
            };
            if (mode.Artwork is not null) art.Children.Add(mode.Artwork());
            else art.Children.Add(new TextBlock
            {
                Text = mode.Emblem,
                FontSize = 48,
                Foreground = Ink("#F4DFC0"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
            art.Children.Add(new Border
            {
                Background = Ink("#990B1718"),
                VerticalAlignment = VerticalAlignment.Bottom,
                Child = new TextBlock
                {
                    Text = Label(mode.Category).ToUpperInvariant(),
                    FontSize = 10,
                    Foreground = Ink("#F4DFC0"),
                    Margin = new Thickness(14, 3, 14, 3),
                },
            });
            panel.Children.Add(art);

            var text = new StackPanel { Margin = new Thickness(16, 9, 16, 9) };
            text.Children.Add(new TextBlock { Text = Label(mode.Subtitle).ToUpperInvariant(), FontSize = 10, Foreground = accent });
            text.Children.Add(new TextBlock
            {
                Text = Label(mode.Title),
                FontFamily = new FontFamily("Georgia"),
                FontSize = 25,
                Foreground = Ink("#F3E6D1"),
                Margin = new Thickness(0, 3, 0, 6),
            });
            text.Children.Add(new TextBlock
            {
                Text = Label(mode.Description),
                FontSize = 12,
                Foreground = Ink("#ADBCB6"),
                TextWrapping = TextWrapping.Wrap,
                Height = 50,
            });
            text.Children.Add(new TextBlock
            {
                Text = modes.CanOpen(mode.Id) ? Localization.T("Lobby.TakeSeat") : Localization.T("Lobby.CurrentlyOpen"),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Ink("#E7CCA0"),
                Margin = new Thickness(0, 7, 0, 0),
            });
            Grid.SetRow(text, 1);
            panel.Children.Add(text);

            var primary = new Button
            {
                Name = "Mode_" + mode.Id.Replace('-', '_'),
                Tag = mode.Id,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Content = panel,
                Background = Ink("#111F20"),
                BorderBrush = accent,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = modes.CanOpen(mode.Id),
            };
            primary.Click += (_, _) => modes.Open(mode.Id);
            var wrapper = new Grid { Margin = new Thickness(8) };
            wrapper.Children.Add(primary);
            if (modes.HasSecondary(mode.Id))
            {
                var secondary = new Button
                {
                    Name = "ModeSecondary_" + mode.Id.Replace('-', '_'),
                    Tag = mode.Id + "-secondary",
                    Content = Localization.T("Lobby.NewWindow"),
                    ToolTip = modes.SecondaryLabel(mode.Id),
                    FontSize = 10,
                    Padding = new Thickness(9, 5, 9, 5),
                    Margin = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    IsEnabled = modes.CanOpenSecondary(mode.Id),
                };
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
