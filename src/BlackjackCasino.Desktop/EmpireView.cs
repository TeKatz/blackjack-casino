using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>A hosted page, with no Window or native handle of its own.</summary>
internal sealed class EmpireView : UserControl, IDisposable
{
    private readonly EmpireService service;
    private readonly Func<string, bool> upgrade;
    private readonly Func<bool> claim;
    private readonly Func<bool> animations;
    private readonly EmpireFloorArtwork artwork = new();
    private readonly TextBlock credits = Label("", 26, "#F0D49B");
    private readonly TextBlock production = Label("", 26, "#9ED7BD");
    private readonly TextBlock dividend = Label("", 26, "#F0D49B");
    private readonly TextBlock roomTitle = Label("", 27, "#F4E8D4");
    private readonly TextBlock roomDescription = Label("", 14, "#ABBCB6");
    private readonly TextBlock roomStats = Label("", 15, "#D9E3DA");
    private readonly TextBlock milestone = Label("", 13, "#B5C8BB");
    private readonly TextBlock notice = Label("Your first slot cabinet is already earning. Select a room to invest your credits.", 13, "#B7C8BD");
    private readonly Button buy = new() { Name = "EmpireUpgradeButton", Margin = new Thickness(0, 18, 0, 14) };
    private readonly Button collect = new() { Name = "EmpireClaimButton", Margin = new Thickness(0, 12, 0, 0) };
    private readonly DispatcherTimer animationTimer;
    private string selected = "slots";

    internal EmpireView(EmpireService service, Func<string, bool> upgrade, Func<bool> claim, Action back, Func<bool> animations)
    {
        this.service = service;
        this.upgrade = upgrade;
        this.claim = claim;
        this.animations = animations;
        Name = "CasinoEmpirePage";
        FontFamily = new FontFamily("Segoe UI");
        Foreground = Ink("#F4E8D4");
        Background = new LinearGradientBrush(Color.FromRgb(10, 22, 23), Color.FromRgb(27, 23, 34), 30);
        // Explicit page scope prevents any host/table theme from changing these controls.
        Resources[typeof(Button)] = Application.Current.FindResource("CasinoButton");
        var root = new Grid { Margin = new Thickness(28, 20, 28, 18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 18) };
        var exit = new Button { Name = "EmpireBackButton", Content = "←  Casino lobby", VerticalAlignment = VerticalAlignment.Center };
        exit.Click += (_, _) => back();
        DockPanel.SetDock(exit, Dock.Right);
        header.Children.Add(exit);
        var heading = new StackPanel();
        heading.Children.Add(Label("THE MIDNIGHT CLUB  /  OWNER'S COLLECTION", 10, "#C6AD7A"));
        heading.Children.Add(new TextBlock { Text = "Casino Empire", FontSize = 34, FontFamily = new FontFamily("Georgia"), Foreground = Ink("#F4E8D4"), Margin = new Thickness(0, 5, 0, 2) });
        heading.Children.Add(Label("A small beginning. A name above the door.", 13, "#A5B8AE"));
        header.Children.Add(heading);
        root.Children.Add(header);

        var balances = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        for (int i = 0; i < 3; i++) balances.ColumnDefinitions.Add(new ColumnDefinition());
        AddBalance(0, "EMPIRE CREDITS", credits, "Only used to build your casino.");
        AddBalance(1, "AUTOMATIC PRODUCTION", production, "Keeps earning in other games and offline.");
        AddBalance(2, "OWNER DIVIDEND", dividend, "Separate cash for the other casino games.");
        Grid.SetRow(balances, 1);
        root.Children.Add(balances);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        var floor = new Grid { Margin = new Thickness(0, 0, 18, 0) };
        floor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        floor.RowDefinitions.Add(new RowDefinition());
        floor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        floor.Children.Add(Label("YOUR CASINO FLOOR     /     SELECT A ROOM", 10, "#C6AD7A"));
        var plan = new Canvas { Width = 1000, Height = 470 };
        artwork.Width = 1000;
        artwork.Height = 470;
        artwork.IsHitTestVisible = false;
        plan.Children.Add(artwork);
        for (int i = 0; i < EmpireCatalog.Departments.Count; i++)
        {
            EmpireDepartment room = EmpireCatalog.Departments[i];
            Rect rect = EmpireFloorArtwork.RoomBounds[i];
            var selector = new Button
            {
                Name = "EmpireRoom_" + room.Id, Width = rect.Width, Height = rect.Height,
                Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(2),
                Style = RoomSelectorStyle(), ToolTip = "Select " + room.Name,
            };
            AutomationProperties.SetName(selector, "Select " + room.Name);
            Canvas.SetLeft(selector, rect.X);
            Canvas.SetTop(selector, rect.Y);
            selector.Click += (_, _) => { selected = room.Id; Refresh(); };
            plan.Children.Add(selector);
        }
        var scene = new Viewbox { Child = plan, Stretch = Stretch.Uniform, Margin = new Thickness(0, 12, 0, 10), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(scene, 1);
        floor.Children.Add(scene);
        var floorNote = new StackPanel();
        floorNote.Children.Add(Label("THE HOUSE WORKS FOR YOU", 11, "#C6AD7A"));
        floorNote.Children.Add(Label("Every room earns automatically. Reach each 10th level to double that room's production and refresh its furnishings.", 13, "#A5B8AE"));
        floorNote.Children.Add(Label("Offline catch-up: up to 8 hours. Progress saves automatically.", 12, "#819C91"));
        Grid.SetRow(floorNote, 2);
        floor.Children.Add(floorNote);
        body.Children.Add(floor);

        var management = new StackPanel { Margin = new Thickness(20) };
        management.Children.Add(Label("ROOM MANAGEMENT", 10, "#C6AD7A"));
        roomTitle.FontFamily = new FontFamily("Georgia");
        roomTitle.Margin = new Thickness(0, 10, 0, 8);
        management.Children.Add(roomTitle);
        management.Children.Add(roomDescription);
        roomStats.Margin = new Thickness(0, 20, 0, 0);
        management.Children.Add(roomStats);
        buy.Click += (_, _) =>
        {
            if (this.upgrade(selected))
                SetNotice(service.Level(selected) == 1 ? "A new room is open. Its income is now automatic." : "Room upgraded. Your new production rate is already running.");
            Refresh();
        };
        management.Children.Add(buy);
        management.Children.Add(milestone);
        management.Children.Add(new Border { Height = 1, Background = Ink("#3E4942"), Margin = new Thickness(0, 22, 0, 20) });
        management.Children.Add(Label("YOUR OWNER DIVIDEND", 11, "#C6AD7A"));
        management.Children.Add(Label("The reserve earns independently of credits. Claiming pays your shared casino cash without spending your construction budget.", 13, "#A5B8AE"));
        collect.Click += (_, _) => { this.claim(); Refresh(); };
        management.Children.Add(collect);
        management.Children.Add(Label("Reserve capacity: $320.00. Rate grows with room levels, up to $40.00 / hour.", 11, "#819C91"));
        var side = new Border
        {
            Background = Ink("#142523"), BorderBrush = Ink("#435448"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Child = new ScrollViewer { Content = management, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled },
        };
        Grid.SetColumn(side, 1);
        body.Children.Add(side);
        Grid.SetRow(body, 2);
        root.Children.Add(body);

        notice.Name = "EmpireNotice";
        var status = new Border { Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 16, 0, 0), Background = Ink("#172C28"), CornerRadius = new CornerRadius(7), Child = notice };
        Grid.SetRow(status, 3);
        root.Children.Add(status);
        Content = root;
        Refresh();
        animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        animationTimer.Tick += (_, _) =>
        {
            if (IsVisible && this.animations()) artwork.Phase += .12;
        };
        Loaded += (_, _) => animationTimer.Start();
        Unloaded += (_, _) => animationTimer.Stop();

        void AddBalance(int column, string title, TextBlock value, string detail)
        {
            var stack = new StackPanel { Margin = new Thickness(16, 12, 16, 12) };
            stack.Children.Add(Label(title, 10, "#B5AA8D"));
            value.Margin = new Thickness(0, 4, 0, 3);
            stack.Children.Add(value);
            stack.Children.Add(Label(detail, 11, "#8DA69A"));
            var card = new Border { Background = Ink("#142724"), BorderBrush = Ink("#33473F"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Margin = new Thickness(column == 0 ? 0 : 10, 0, 0, 0), Child = stack };
            Grid.SetColumn(card, column);
            balances.Children.Add(card);
        }
    }

    internal void Refresh()
    {
        credits.Text = Number(service.Credits) + " cr";
        production.Text = Number(service.CreditsPerSecond) + " cr / sec";
        dividend.Text = service.ClaimableDividend.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        EmpireDepartment room = EmpireCatalog.Find(selected)!;
        int level = service.Level(selected);
        roomTitle.Text = room.Name;
        roomDescription.Text = room.Description;
        roomStats.Text = level == 0 ? "Not yet open\nPotential: " + Number(room.BaseCreditsPerSecond) + " cr / sec"
            : $"Level {level} / {EmpireRules.MaximumLevel}\nProducing {Number(EmpireRules.Production(room, level))} cr / sec";
        decimal cost = EmpireRules.UpgradeCost(room, level);
        buy.Content = new TextBlock
        {
            Text = level == EmpireRules.MaximumLevel ? "Fully upgraded" : (level == 0 ? "Open room" : "Upgrade to level " + (level + 1)) + "  ·  " + Number(cost) + " cr",
            TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, MaxWidth = 240,
        };
        buy.IsEnabled = level < EmpireRules.MaximumLevel && service.Credits >= cost;
        milestone.Text = level >= EmpireRules.MaximumLevel ? "All room milestones reached."
            : $"Next milestone: level {(level / 10 + 1) * 10}\nDoubles this room's production permanently.";
        collect.Content = "Claim " + service.ClaimableDividend.ToString("C2", CultureInfo.GetCultureInfo("en-US")) + " cash";
        collect.IsEnabled = service.ClaimableDividend >= .01m;
        collect.ToolTip = $"Current dividend rate: ${service.CashPerHour:0.00} / hour. No credits are exchanged.";
        artwork.State = service.Snapshot;
        artwork.SelectedRoom = selected;
        artwork.InvalidateVisual();
    }

    internal void SetNotice(string text) => notice.Text = text;
    public void Dispose() => animationTimer.Stop();

    internal static string Number(decimal value)
    {
        (decimal Scale, string Suffix)[] units = [(1_000_000_000_000_000_000_000_000m, "Sp"), (1_000_000_000_000_000_000_000m, "Sx"), (1_000_000_000_000_000_000m, "Qi"), (1_000_000_000_000_000m, "Qa"), (1_000_000_000_000m, "T"), (1_000_000_000m, "B"), (1_000_000m, "M"), (1_000m, "K")];
        foreach (var unit in units)
            if (value >= unit.Scale) return (value / unit.Scale).ToString("0.##", CultureInfo.InvariantCulture) + unit.Suffix;
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static TextBlock Label(string text, double size, string color) => new()
    {
        Text = text, FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap,
    };
    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private static Style RoomSelectorStyle()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "Outline";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        border.SetValue(Border.BorderThicknessProperty, new Thickness(2));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.BorderBrushProperty, Brushes.Transparent);
        var template = new ControlTemplate(typeof(Button)) { VisualTree = border };
        var hover = new Trigger { Property = IsMouseOverProperty, Value = true };
        hover.Setters.Add(new Setter(Border.BorderBrushProperty, Ink("#D9BE87"), "Outline"));
        template.Triggers.Add(hover);
        var focus = new Trigger { Property = IsKeyboardFocusedProperty, Value = true };
        focus.Setters.Add(new Setter(Border.BorderBrushProperty, Ink("#F8E3B2"), "Outline"));
        template.Triggers.Add(focus);
        var style = new Style(typeof(Button));
        style.Setters.Add(new Setter(TemplateProperty, template));
        style.Setters.Add(new Setter(CursorProperty, System.Windows.Input.Cursors.Hand));
        return style;
    }
}
