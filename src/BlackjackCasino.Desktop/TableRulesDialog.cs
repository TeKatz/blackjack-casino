using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class TableRulesDialog : Window
{
    private readonly ComboBox decks = new();
    private readonly ComboBox dealer17 = new();
    private readonly ComboBox holeCard = new();
    private readonly CheckBox peek = new() { Content = "Dealer peeks for blackjack" };
    private readonly CheckBox insurance = new() { Content = "Insurance available" };
    private readonly CheckBox surrender = new() { Content = "Late surrender" };
    private readonly ComboBox blackjackPayout = new();
    private readonly ComboBox doubleRule = new();
    private readonly CheckBox das = new() { Content = "Double after split (DAS)" };
    private readonly CheckBox resplitAces = new() { Content = "Resplit aces" };
    private readonly CheckBox hitSplitAces = new() { Content = "Hit split aces" };
    private readonly ComboBox maxSplitHands = new();
    private readonly Slider penetration = new() { Minimum = 0.50, Maximum = 0.90, TickFrequency = 0.05, IsSnapToTickEnabled = true };
    private readonly TextBlock penetrationText = new();
    private readonly TextBox minimumBet = new();
    private readonly TextBox maximumBet = new();
    private readonly TextBlock validationText = new();
    private TextBlock? maximizeGlyph;

    public TableRulesDialog(GameRules initial)
    {
        ArgumentNullException.ThrowIfNull(initial);

        Title = "Advanced table setup";
        Width = 590;
        Height = 760;
        MinWidth = 540;
        MinHeight = 650;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.CanResize;
        Background = new SolidColorBrush(Color.FromRgb(5, 13, 9));
        Foreground = new SolidColorBrush(Color.FromRgb(239, 242, 235));
        FontFamily = new FontFamily("Segoe UI");

        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 0,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false,
        });

        StateChanged += (_, _) => RefreshMaximizeGlyph();
        Content = BuildWindowShell(BuildContent(initial));
        RefreshMaximizeGlyph();
    }

    public GameRules? ResultRules { get; private set; }

    private UIElement BuildWindowShell(UIElement content)
    {
        var root = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(7, 23, 15)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(49, 90, 67)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });
        grid.RowDefinitions.Add(new RowDefinition());

        var titleBar = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(8, 26, 17)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(40, 69, 53)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(9, 9, 0, 0),
        };
        titleBar.MouseLeftButtonDown += TitleBar_MouseLeftButtonDown;

        var titleGrid = new Grid { Margin = new Thickness(14, 0, 6, 0) };
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition());
        titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleText.Children.Add(new TextBlock
        {
            Text = "♠",
            Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 124)),
            FontSize = 15,
            Margin = new Thickness(0, 0, 9, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        titleText.Children.Add(new TextBlock
        {
            Text = "BLACKJACK CASINO",
            Foreground = new SolidColorBrush(Color.FromRgb(245, 241, 231)),
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        titleText.Children.Add(new TextBlock
        {
            Text = "  ·  TABLE SETUP",
            Foreground = new SolidColorBrush(Color.FromRgb(120, 146, 130)),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        });
        titleGrid.Children.Add(titleText);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        controls.Children.Add(WindowControl("—", () => WindowState = WindowState.Minimized));
        Border maximize = WindowControl("□", ToggleMaximize);
        maximizeGlyph = maximize.Child as TextBlock;
        controls.Children.Add(maximize);
        controls.Children.Add(WindowControl("×", Close, closeButton: true));
        Grid.SetColumn(controls, 1);
        titleGrid.Children.Add(controls);
        titleBar.Child = titleGrid;
        grid.Children.Add(titleBar);

        Grid.SetRow(content, 1);
        grid.Children.Add(content);
        root.Child = grid;
        return root;
    }

    private Border WindowControl(string glyph, Action action, bool closeButton = false)
    {
        var text = new TextBlock
        {
            Text = glyph,
            Foreground = new SolidColorBrush(Color.FromRgb(175, 193, 181)),
            FontSize = glyph == "×" ? 18 : 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var border = new Border
        {
            Width = 42,
            Height = 32,
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = text,
        };
        border.MouseEnter += (_, _) =>
        {
            border.Background = new SolidColorBrush(closeButton
                ? Color.FromRgb(111, 44, 44)
                : Color.FromRgb(23, 51, 38));
            text.Foreground = Brushes.White;
        };
        border.MouseLeave += (_, _) =>
        {
            border.Background = Brushes.Transparent;
            text.Foreground = new SolidColorBrush(Color.FromRgb(175, 193, 181));
        };
        border.MouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            action();
        };
        return border;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        RefreshMaximizeGlyph();
    }

    private void RefreshMaximizeGlyph()
    {
        if (maximizeGlyph is not null)
        {
            maximizeGlyph.Text = WindowState == WindowState.Maximized ? "❐" : "□";
        }
    }

    private UIElement BuildContent(GameRules initial)
    {
        foreach (int value in new[] { 1, 2, 4, 6, 8 }) decks.Items.Add(value);
        decks.SelectedItem = initial.DeckCount;

        dealer17.Items.Add("Stand on soft 17 (S17)");
        dealer17.Items.Add("Hit soft 17 (H17)");
        dealer17.SelectedIndex = initial.DealerHitsSoft17 ? 1 : 0;

        holeCard.Items.Add("US hole card");
        holeCard.Items.Add("European no-hole-card (ENHC)");
        holeCard.SelectedIndex = initial.DealerTakesHoleCard ? 0 : 1;
        holeCard.SelectionChanged += (_, _) => UpdatePeekAvailability();

        blackjackPayout.Items.Add("3:2");
        blackjackPayout.Items.Add("6:5");
        blackjackPayout.SelectedIndex = initial.BlackjackPayout == 1.2m ? 1 : 0;

        doubleRule.Items.Add("Any first two cards");
        doubleRule.Items.Add("Totals 9–11 only");
        doubleRule.Items.Add("Totals 10–11 only");
        doubleRule.SelectedIndex = (int)initial.DoubleRule;

        for (int i = 2; i <= 8; i++) maxSplitHands.Items.Add(i);
        maxSplitHands.SelectedItem = Math.Clamp(initial.MaxSplitHands, 2, 8);

        peek.IsChecked = initial.DealerPeeksForBlackjack;
        insurance.IsChecked = initial.AllowInsurance;
        surrender.IsChecked = initial.AllowLateSurrender;
        das.IsChecked = initial.DoubleAfterSplit;
        resplitAces.IsChecked = initial.ResplitAces;
        hitSplitAces.IsChecked = initial.HitSplitAces;
        penetration.Value = initial.Penetration;
        penetration.ValueChanged += (_, _) => penetrationText.Text = $"{penetration.Value:P0}";
        penetrationText.Text = $"{penetration.Value:P0}";
        minimumBet.Text = initial.MinimumBet.ToString("0.##", CultureInfo.InvariantCulture);
        maximumBet.Text = initial.MaximumBet.ToString("0.##", CultureInfo.InvariantCulture);
        UpdatePeekAvailability();

        var stack = new StackPanel { Margin = new Thickness(24) };
        stack.Children.Add(new TextBlock
        {
            Text = "Advanced table rules",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "These settings are optional. Casual Solo remains available as a one-click preset.",
            Margin = new Thickness(0, 5, 0, 18),
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(158, 177, 164)),
        });

        stack.Children.Add(Row("Decks", decks));
        stack.Children.Add(Row("Dealer 17", dealer17));
        stack.Children.Add(Row("Dealer cards", holeCard));
        stack.Children.Add(peek);
        stack.Children.Add(insurance);
        stack.Children.Add(surrender);
        stack.Children.Add(Row("Blackjack pays", blackjackPayout));
        stack.Children.Add(Row("Double", doubleRule));
        stack.Children.Add(das);
        stack.Children.Add(resplitAces);
        stack.Children.Add(hitSplitAces);
        stack.Children.Add(Row("Maximum split hands", maxSplitHands));

        var penetrationRow = new Grid { Margin = new Thickness(0, 10, 0, 0) };
        penetrationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        penetrationRow.ColumnDefinitions.Add(new ColumnDefinition());
        penetrationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) });
        penetrationRow.Children.Add(Label("Shoe penetration"));
        Grid.SetColumn(penetration, 1);
        penetrationRow.Children.Add(penetration);
        Grid.SetColumn(penetrationText, 2);
        penetrationText.VerticalAlignment = VerticalAlignment.Center;
        penetrationText.HorizontalAlignment = HorizontalAlignment.Right;
        penetrationRow.Children.Add(penetrationText);
        stack.Children.Add(penetrationRow);

        stack.Children.Add(Row("Minimum bet", minimumBet));
        stack.Children.Add(Row("Maximum bet", maximumBet));

        validationText.Margin = new Thickness(0, 12, 0, 0);
        validationText.Foreground = new SolidColorBrush(Color.FromRgb(255, 176, 136));
        validationText.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(validationText);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0),
        };
        var cancel = new Button { Content = "Cancel", MinWidth = 100, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(4) };
        cancel.Click += (_, _) => DialogResult = false;
        var apply = new Button { Content = "Use custom rules", MinWidth = 150, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(4), FontWeight = FontWeights.Bold };
        apply.Click += Apply_Click;
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        stack.Children.Add(buttons);

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack,
        };
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(minimumBet.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal minBet)
            || !decimal.TryParse(maximumBet.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal maxBet))
        {
            validationText.Text = "Minimum and maximum bet must be valid numbers (for example 5 or 2500).";
            return;
        }

        var rules = new GameRules
        {
            DeckCount = decks.SelectedItem is int deckCount ? deckCount : 6,
            DealerHitsSoft17 = dealer17.SelectedIndex == 1,
            DealerTakesHoleCard = holeCard.SelectedIndex == 0,
            DealerPeeksForBlackjack = holeCard.SelectedIndex == 0 && peek.IsChecked == true,
            AllowInsurance = insurance.IsChecked == true,
            AllowLateSurrender = surrender.IsChecked == true,
            BlackjackPayout = blackjackPayout.SelectedIndex == 1 ? 1.2m : 1.5m,
            DoubleRule = (DoubleRule)Math.Clamp(doubleRule.SelectedIndex, 0, 2),
            DoubleAfterSplit = das.IsChecked == true,
            ResplitAces = resplitAces.IsChecked == true,
            HitSplitAces = hitSplitAces.IsChecked == true,
            MaxSplitHands = maxSplitHands.SelectedItem is int splitHands ? splitHands : 4,
            Penetration = penetration.Value,
            MinimumBet = minBet,
            MaximumBet = maxBet,
        };

        try
        {
            rules.Validate();
        }
        catch (Exception ex)
        {
            validationText.Text = ex.Message;
            return;
        }

        ResultRules = rules;
        DialogResult = true;
    }

    private void UpdatePeekAvailability()
    {
        bool hasHoleCard = holeCard.SelectedIndex == 0;
        peek.IsEnabled = hasHoleCard;
        if (!hasHoleCard)
        {
            peek.IsChecked = false;
        }
    }

    private static Grid Row(string name, Control control)
    {
        var grid = new Grid { Margin = new Thickness(0, 9, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.Children.Add(Label(name));
        Grid.SetColumn(control, 1);
        control.MinHeight = 30;
        grid.Children.Add(control);
        return grid;
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Center,
        Foreground = new SolidColorBrush(Color.FromRgb(204, 219, 210)),
    };
}
