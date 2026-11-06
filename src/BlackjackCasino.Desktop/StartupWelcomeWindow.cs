using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shell;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class StartupWelcomeWindow : Window
{
    private StartupWelcomeWindow(PlayerProfileState profile)
    {
        Title = T("WindowTitle");
        Width = 940;
        Height = 620;
        MinWidth = 940;
        MinHeight = 620;
        MaxWidth = 940;
        MaxHeight = 620;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Ink("#071012");
        Foreground = Ink("#F2E9D9");
        FontFamily = new FontFamily("Segoe UI");
        ShowInTaskbar = false;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(0),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false,
        });
        Content = Build(profile);
        MenuAudio.Attach(this);
    }

    internal static void ShowFor(Window owner)
    {
        PlayerProfileState profile = PlayerProfileStore.LoadProfile();
        var window = new StartupWelcomeWindow(profile) { Owner = owner };
        window.ShowDialog();
    }

    private UIElement Build(PlayerProfileState profile)
    {
        var frame = new Border
        {
            Background = new LinearGradientBrush(Ink("#0B1A19").Color, Ink("#10131A").Color, 55),
            BorderBrush = Ink("#6B5B3F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
        };
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        frame.Child = root;

        var titleBar = new Border
        {
            Background = Ink("#091718"),
            BorderBrush = Ink("#304A46"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(9, 9, 0, 0),
            Padding = new Thickness(16, 0, 7, 0),
        };
        titleBar.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        var titleDock = new DockPanel();
        var close = SmallButton("×", 38);
        close.FontSize = 18;
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Right);
        titleDock.Children.Add(close);
        titleDock.Children.Add(new TextBlock
        {
            Text = T("TopBar"),
            VerticalAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Georgia"),
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = Ink("#E9D5A8"),
        });
        titleBar.Child = titleDock;
        root.Children.Add(titleBar);

        var body = new Grid { Margin = new Thickness(26, 22, 26, 18) };
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        body.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetRow(body, 1);
        root.Children.Add(body);

        var left = new StackPanel();
        left.Children.Add(Eyebrow(F("Version", AppVersion.Current)));
        left.Children.Add(new TextBlock
        {
            Text = F("Back", profile.DisplayName),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 31,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#F6E8CD"),
            Margin = new Thickness(0, 6, 0, 5),
            TextWrapping = TextWrapping.Wrap,
        });
        left.Children.Add(new TextBlock
        {
            Text = T("Intro"),
            FontSize = 12.5,
            Foreground = Ink("#A4B7AE"),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 19,
            Margin = new Thickness(0, 0, 0, 18),
        });

        var stats = new UniformGrid { Columns = 2 };
        decimal totalWagered = TotalWagered(profile);
        decimal totalNet = TotalNet(profile);
        stats.Children.Add(Stat(T("Player"), $"P{profile.Prestige} · LV {profile.Level}", T("PlayerDetail"), "#E0BD70"));
        stats.Children.Add(Stat(T("Playtime"), FormatDuration(TimeSpan.FromSeconds(profile.TotalPlaytimeSeconds)), T("PlaytimeDetail"), "#86C7AF"));
        stats.Children.Add(Stat(T("Wagered"), Money(totalWagered), T("WageredDetail"), "#9DB7C8"));
        stats.Children.Add(Stat(T("Net"), SignedMoney(totalNet), T(totalNet >= 0 ? "NetProfit" : "NetResult"), totalNet >= 0 ? "#8CCB8D" : "#D98A85"));
        left.Children.Add(stats);
        left.Children.Add(new TextBlock
        {
            Text = F("Cosmetics", profile.Coins, profile.OwnedCosmeticIds.Count),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Ink("#9CB0A7"),
            FontSize = 11,
            Margin = new Thickness(4, 12, 0, 0),
        });
        body.Children.Add(left);

        var notesPanel = new Border
        {
            Background = new LinearGradientBrush(Ink("#121E1B").Color, Ink("#101318").Color, 90),
            BorderBrush = Ink("#465044"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
        };
        Grid.SetColumn(notesPanel, 2);
        body.Children.Add(notesPanel);
        var notes = new StackPanel();
        notes.Children.Add(Eyebrow(T("WhatsNew")));
        notes.Children.Add(new TextBlock
        {
            Text = T("Changes"),
            FontFamily = new FontFamily("Georgia"),
            FontSize = 25,
            Foreground = Ink("#F0DFC0"),
            Margin = new Thickness(0, 5, 0, 12),
        });
        foreach (ReleaseNote note in ReleaseNotesCatalog.Current)
        {
            var item = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            item.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 0 });
            item.Children.Add(new TextBlock { Text = "✦", Foreground = Ink(note.Accent), FontSize = 13, Margin = new Thickness(1, 2, 0, 0) });
            var copy = new StackPanel();
            copy.Children.Add(new TextBlock
            {
                Text = note.Title,
                FontWeight = FontWeights.Bold,
                Foreground = Ink("#E9DFC9"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17,
                ToolTip = note.Title,
            });
            if (!string.IsNullOrWhiteSpace(note.Description))
                copy.Children.Add(new TextBlock { Text = note.Description, Foreground = Ink("#9FB1A8"), FontSize = 11, TextWrapping = TextWrapping.Wrap, LineHeight = 16, Margin = new Thickness(0, 3, 0, 0) });
            Grid.SetColumn(copy, 1);
            item.Children.Add(copy);
            notes.Children.Add(item);
        }
        var notesScroll = new ScrollViewer
        {
            Content = notes,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
        };
        // A ScrollViewer can otherwise measure a StackPanel wider than its viewport.
        // Bind its actual content width to the scrollable viewport (including the
        // space reserved for a vertical scrollbar) so long release titles and
        // descriptions wrap instead of disappearing behind the right border.
        notesScroll.ScrollChanged += (_, e) =>
        {
            if (e.ViewportWidthChange == 0 || !double.IsFinite(notesScroll.ViewportWidth))
                return;
            double width = Math.Max(1, notesScroll.ViewportWidth);
            if (double.IsNaN(notes.Width) || Math.Abs(notes.Width - width) > .5)
                notes.Width = width;
        };
        notesPanel.Child = notesScroll;

        var footer = new Border
        {
            BorderBrush = Ink("#34423B"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(26, 12, 26, 16),
        };
        var footerDock = new DockPanel();
        var enter = new Button
        {
            Content = T("Enter"),
            MinWidth = 180,
            Height = 42,
            Padding = new Thickness(18, 7, 18, 7),
            FontFamily = new FontFamily("Georgia"),
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#FFF0C8"),
            Background = new LinearGradientBrush(Ink("#4A391F").Color, Ink("#17231E").Color, 90),
            BorderBrush = Ink("#C6A55E"),
            BorderThickness = new Thickness(1.4),
            Cursor = CasinoCursors.Hand,
            Effect = new DropShadowEffect { Color = Ink("#D4A84F").Color, BlurRadius = 12, ShadowDepth = 0, Opacity = .32 },
        };
        enter.Click += (_, _) => Close();
        DockPanel.SetDock(enter, Dock.Right);
        footerDock.Children.Add(enter);
        footerDock.Children.Add(new TextBlock
        {
            Text = T("Ready"),
            Foreground = Ink("#7F978C"),
            FontSize = 10.5,
            VerticalAlignment = VerticalAlignment.Center,
        });
        footer.Child = footerDock;
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        return frame;
    }

    private static Border Stat(string caption, string value, string detail, string accent)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = caption, Foreground = Ink("#81978D"), FontSize = 9, FontWeight = FontWeights.Bold });
        stack.Children.Add(new TextBlock { Text = value, Foreground = Ink(accent), FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 1), TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = value });
        stack.Children.Add(new TextBlock { Text = detail, Foreground = Ink("#758A81"), FontSize = 9.5, TextWrapping = TextWrapping.Wrap });
        return new Border
        {
            Background = Ink("#0D1A18"),
            BorderBrush = Ink("#32473E"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 8, 8),
            Child = stack,
        };
    }

    private static TextBlock Eyebrow(string text) => new()
    {
        Text = text,
        Foreground = Ink("#D0AD66"),
        FontSize = 10,
        FontWeight = FontWeights.Bold,
    };

    private static Button SmallButton(string content, double width) => new()
    {
        Content = content,
        Width = width,
        Height = 30,
        Padding = new Thickness(0),
        Background = Brushes.Transparent,
        BorderBrush = Brushes.Transparent,
        Foreground = Ink("#B8C7BE"),
        Cursor = CasinoCursors.Hand,
    };

    private static string T(string key) => Localization.T("Shell.Welcome." + key);
    private static string F(string key, params object?[] args) => Localization.F("Shell.Welcome." + key, args);

    private static decimal TotalWagered(PlayerProfileState p) => p.LifetimeStatistics.TotalWagered + p.Slots.TotalWagered + p.NeonVault.TotalWagered + p.NineLives.TotalWagered + p.Manor.TotalWagered + p.LegacyPooledSlots.TotalWagered + p.Kniffel.TotalWagered;
    private static decimal TotalNet(PlayerProfileState p) => p.LifetimeStatistics.NetProfit + p.Slots.NetProfit + p.NeonVault.NetProfit + p.NineLives.NetProfit + p.Manor.NetProfit + p.LegacyPooledSlots.NetProfit + p.Kniffel.NetProfit;
    private static string Money(decimal value) => value.ToString("C0", CultureInfo.GetCultureInfo("en-US"));
    private static string SignedMoney(decimal value) => value >= 0 ? "+" + Money(value) : "-" + Money(Math.Abs(value));
    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1 ? $"{(int)duration.TotalHours}h {duration.Minutes}m" : $"{Math.Max(0, duration.Minutes)}m";
    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}

