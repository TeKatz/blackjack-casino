using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shell;

namespace BlackjackCasino.Desktop;

internal enum PremiumDecisionKind
{
    PlayerPrestige,
    EmpirePrestige,
    ExitGame,
}

internal sealed class PremiumConfirmationWindow : Window
{
    private bool accepted;

    private PremiumConfirmationWindow(PremiumDecisionKind kind, string message)
    {
        Title = kind == PremiumDecisionKind.ExitGame ? "Exit Game" : "Prestige";
        Width = 650;
        Height = kind == PremiumDecisionKind.ExitGame ? 400 : 535;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Ink("#080D0E");
        Foreground = Ink("#F2E7D5");
        FontFamily = new FontFamily("Segoe UI");
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
        Content = Build(kind, message);
        MenuAudio.Attach(this);
    }

    internal static MessageBoxResult ConfirmPrestige(Window? owner, string caption, string message)
    {
        PremiumDecisionKind kind = caption.Contains("Empire", StringComparison.OrdinalIgnoreCase)
            ? PremiumDecisionKind.EmpirePrestige
            : PremiumDecisionKind.PlayerPrestige;
        var window = new PremiumConfirmationWindow(kind, message);
        if (owner is { IsVisible: true }) window.Owner = owner;
        window.ShowDialog();
        return window.accepted ? MessageBoxResult.Yes : MessageBoxResult.No;
    }

    internal static bool ConfirmExit(Window? owner)
    {
        var window = new PremiumConfirmationWindow(PremiumDecisionKind.ExitGame, "Your current progress is saved locally before the club closes for the night.");
        if (owner is { IsVisible: true }) window.Owner = owner;
        window.ShowDialog();
        return window.accepted;
    }

    private UIElement Build(PremiumDecisionKind kind, string message)
    {
        string eyebrow = kind switch
        {
            PremiumDecisionKind.PlayerPrestige => "PLAYER PRESTIGE",
            PremiumDecisionKind.EmpirePrestige => "CASINO EMPIRE · PRESTIGE",
            _ => "THE MIDNIGHT CLUB",
        };
        string headline = kind switch
        {
            PremiumDecisionKind.PlayerPrestige => "Leave the level behind. Keep the legacy.",
            PremiumDecisionKind.EmpirePrestige => "Rebuild the floor. Raise the empire.",
            _ => "Call it a night?",
        };
        string confirm = kind switch
        {
            PremiumDecisionKind.PlayerPrestige => "ENTER PRESTIGE  ✦",
            PremiumDecisionKind.EmpirePrestige => "PRESTIGE EMPIRE  ✦",
            _ => "EXIT GAME",
        };
        string cancel = kind == PremiumDecisionKind.ExitGame ? "STAY IN CLUB" : "NOT YET";

        var outer = new Border
        {
            Background = new LinearGradientBrush(Ink("#111C1A").Color, Ink("#111117").Color, 65),
            BorderBrush = new LinearGradientBrush(Ink("#E2C16F").Color, Ink("#6C5128").Color, 25),
            BorderThickness = new Thickness(1.4),
            CornerRadius = new CornerRadius(10),
            Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 28, ShadowDepth = 8, Opacity = .6 },
        };
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        root.RowDefinitions.Add(new RowDefinition());
        outer.Child = root;

        var titleBar = new Border
        {
            Background = Ink("#0B1717"),
            BorderBrush = Ink("#59492F"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(9, 9, 0, 0),
            Padding = new Thickness(15, 0, 7, 0),
        };
        titleBar.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        var top = new DockPanel();
        var close = QuietButton("×", 38);
        close.FontSize = 18;
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Right);
        top.Children.Add(close);
        top.Children.Add(new TextBlock
        {
            Text = eyebrow,
            FontFamily = new FontFamily("Georgia"),
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#DFC57D"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        });
        titleBar.Child = top;
        root.Children.Add(titleBar);

        var body = new StackPanel { Margin = new Thickness(30, 24, 30, 27) };
        Grid.SetRow(body, 1);
        root.Children.Add(body);
        body.Children.Add(new Border
        {
            Width = 62,
            Height = 62,
            CornerRadius = new CornerRadius(31),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new RadialGradientBrush(Ink("#6A5125").Color, Ink("#1A1C19").Color),
            BorderBrush = Ink("#E0BF69"),
            BorderThickness = new Thickness(1.5),
            Effect = new DropShadowEffect { Color = Ink("#DFAE4F").Color, BlurRadius = 17, ShadowDepth = 0, Opacity = .45 },
            Child = new TextBlock
            {
                Text = kind == PremiumDecisionKind.ExitGame ? "♠" : "✦",
                FontSize = 26,
                Foreground = Ink("#FFE8A7"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        });
        body.Children.Add(new TextBlock
        {
            Text = headline,
            FontFamily = new FontFamily("Georgia"),
            FontSize = kind == PremiumDecisionKind.ExitGame ? 30 : 28,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#F5E7CA"),
            Margin = new Thickness(0, 14, 0, 7),
            TextWrapping = TextWrapping.Wrap,
        });
        body.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = Ink("#AABBB2"),
            FontSize = 12.5,
            LineHeight = 19,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 15),
        });

        if (kind != PremiumDecisionKind.ExitGame)
        {
            var terms = new Grid { Margin = new Thickness(0, 0, 0, 18) };
            terms.ColumnDefinitions.Add(new ColumnDefinition());
            terms.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            terms.ColumnDefinitions.Add(new ColumnDefinition());
            terms.Children.Add(TermPanel("RESET", kind == PremiumDecisionKind.PlayerPrestige
                ? "Current level and level XP"
                : "Construction credits, room levels and the active event", "#D8887F"));
            Border keep = TermPanel("KEPT", kind == PremiumDecisionKind.PlayerPrestige
                ? "Coins, cosmetics, statistics, bankroll, Album and Season Pass"
                : "Research, Legacy Points, projects, contracts, managers, other properties and lifetime statistics", "#83C7A0");
            Grid.SetColumn(keep, 2);
            terms.Children.Add(keep);
            body.Children.Add(terms);

            var reward = new Border
            {
                Background = Ink("#241F13"),
                BorderBrush = Ink("#7E6531"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(13, 9, 13, 9),
                Margin = new Thickness(0, 0, 0, 18),
            };
            reward.Child = new TextBlock
            {
                Text = kind == PremiumDecisionKind.PlayerPrestige
                    ? "PRESTIGE REWARD  ·  1 LEGENDARY PACK + 2 EPIC PACKS"
                    : "PRESTIGE LEGACY  ·  PERMANENT EMPIRE PRODUCTION BONUS",
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Foreground = Ink("#E7C970"),
                TextWrapping = TextWrapping.Wrap,
            };
            body.Children.Add(reward);
        }

        var actions = new Grid();
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        Button no = QuietButton(cancel, double.NaN);
        no.Height = 42;
        no.BorderBrush = Ink("#49594F");
        no.BorderThickness = new Thickness(1);
        no.Background = Ink("#111C19");
        no.Click += (_, _) => Close();
        actions.Children.Add(no);

        Button yes = QuietButton(confirm, double.NaN);
        yes.Height = 42;
        yes.FontFamily = new FontFamily("Georgia");
        yes.FontWeight = FontWeights.Bold;
        yes.Foreground = kind == PremiumDecisionKind.ExitGame ? Ink("#F0D7D1") : Ink("#FFF0C0");
        yes.Background = kind == PremiumDecisionKind.ExitGame
            ? new LinearGradientBrush(Ink("#4A2528").Color, Ink("#21181A").Color, 90)
            : new LinearGradientBrush(Ink("#59421B").Color, Ink("#17231E").Color, 90);
        yes.BorderBrush = kind == PremiumDecisionKind.ExitGame ? Ink("#A86663") : Ink("#D1AE5D");
        yes.BorderThickness = new Thickness(1.4);
        yes.Effect = new DropShadowEffect { Color = kind == PremiumDecisionKind.ExitGame ? Ink("#A75555").Color : Ink("#D7A844").Color, BlurRadius = 12, ShadowDepth = 0, Opacity = .3 };
        yes.Click += (_, _) => { accepted = true; Close(); };
        Grid.SetColumn(yes, 2);
        actions.Children.Add(yes);
        body.Children.Add(actions);
        return outer;
    }

    private static Border TermPanel(string title, string copy, string accent) => new()
    {
        Background = Ink("#0E1817"),
        BorderBrush = Ink("#34463F"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(7),
        Padding = new Thickness(12, 9, 12, 9),
        Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, Foreground = Ink(accent), FontWeight = FontWeights.Bold, FontSize = 9.5 },
                new TextBlock { Text = copy, Foreground = Ink("#A8B7B0"), FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), LineHeight = 15 },
            },
        },
    };

    private static Button QuietButton(string content, double width) => new()
    {
        Content = content,
        Width = width,
        Padding = new Thickness(12, 6, 12, 6),
        Background = Brushes.Transparent,
        BorderBrush = Brushes.Transparent,
        Foreground = Ink("#B4C3BB"),
        Cursor = CasinoCursors.Hand,
    };

    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
