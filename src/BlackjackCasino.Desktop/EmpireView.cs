using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>Responsive estate dashboard. The simulation is independent of its visual clocks.</summary>
internal sealed class EmpireView : UserControl, IDisposable
{
    private readonly EmpireService service;
    private readonly Func<bool> animations;
    private readonly WrapPanel rooms = new() { Name = "EmpireRoomGrid", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly List<EmpireRoomCard> cards = [];
    private readonly EmpireBalanceCounter credits = new() { FontSize = 25, Foreground = Ink("#EFD5A0") };
    private readonly TextBlock production = Label("", 25, "#A8D9BB");
    private readonly TextBlock dividend = Label("", 23, "#EFD5A0");
    private readonly TextBlock cashRate = Label("", 10, "#9BB7A5");
    private readonly TextBlock roomTitle = Label("", 26, "#F2E4C8");
    private readonly TextBlock roomDescription = Label("", 13, "#A5BCAF");
    private readonly TextBlock roomStats = Label("", 15, "#D1DFD4");
    private readonly TextBlock milestone = Label("", 12, "#B5C8B8");
    private readonly TextBlock estateStatus = Label("", 10, "#9DB1A2");
    private readonly TextBlock lifetime = Label("", 21, "#E6CE9D");
    private readonly TextBlock cashClaimed = Label("", 21, "#E6CE9D");
    private readonly TextBlock levels = Label("", 21, "#B2D3B8");
    private readonly TextBlock openRooms = Label("", 21, "#B2D3B8");
    private readonly TextBlock notice = Label("Your casino is working. Select a room to open or upgrade it.", 12, "#B8CCBB");
    private readonly TextBlock buyLabel = Label("", 13, "#F3E7CC");
    private readonly Button buy = new() { Name = "EmpireUpgradeButton", Margin = new Thickness(0, 14, 0, 14), MinHeight = 48 };
    private readonly Button collect = new() { Name = "EmpireClaimButton", Padding = new Thickness(13, 8, 13, 8), MinHeight = 35, FontSize = 12 };
    private readonly Dictionary<string, Button> filters = new(StringComparer.Ordinal);
    private readonly Border milestoneFill = new() { Height = 5, Background = Ink("#C7AA6E"), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3) };
    private readonly Grid milestoneTrack = new() { Height = 5, Background = Ink("#2A3E34"), Margin = new Thickness(0, 8, 0, 10) };
    private readonly ScrollViewer roomScroll;
    private readonly DispatcherTimer animationTimer;
    private string selected = "slots";
    private string filter = "all";
    private bool disposed;
    private bool wasAnimating;
    private double phase;

    internal EmpireView(EmpireService service, Func<string, bool> upgrade, Func<bool> claim, Action back, Func<bool> animations)
    {
        this.service = service;
        this.animations = animations;
        Name = "CasinoEmpirePage";
        FontFamily = new FontFamily("Segoe UI");
        Foreground = Ink("#F2E4C8");
        Background = new LinearGradientBrush(Color.FromRgb(8, 20, 19), Color.FromRgb(26, 23, 29), 25);
        Resources[typeof(Button)] = Application.Current.FindResource("CasinoButton");
        var root = new Grid { Margin = new Thickness(24, 17, 24, 14) };
        foreach (GridLength height in new[] { GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = height });

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 13) };
        var exit = new Button { Name = "EmpireBackButton", Content = "←  Casino lobby", VerticalAlignment = VerticalAlignment.Center };
        exit.Click += (_, _) => back();
        DockPanel.SetDock(exit, Dock.Right);
        header.Children.Add(exit);
        var title = new StackPanel();
        title.Children.Add(Label("THE MIDNIGHT CLUB  /  YOUR PRIVATE ESTATE", 9, "#BEA674"));
        title.Children.Add(new TextBlock { Text = "Casino Empire", FontFamily = new FontFamily("Georgia"), FontSize = 32, Foreground = Ink("#F4E7CA"), Margin = new Thickness(0, 3, 0, 0) });
        header.Children.Add(title);
        root.Children.Add(header);

        var balances = new Grid { Margin = new Thickness(0, 0, 0, 15) };
        for (int i = 0; i < 3; i++) balances.ColumnDefinitions.Add(new ColumnDefinition());
        AddMetric(0, "EMPIRE CREDITS", credits, Label("Your independent construction budget", 10, "#91A99C"));
        AddMetric(1, "AUTOMATIC PRODUCTION", production, Label("Earning in other games · 8h offline catch-up", 10, "#91A99C"));
        var dividendContent = new DockPanel();
        collect.VerticalAlignment = VerticalAlignment.Center;
        collect.Click += (_, _) => { claim(); Refresh(); };
        DockPanel.SetDock(collect, Dock.Right);
        dividendContent.Children.Add(collect);
        dividendContent.Children.Add(dividend);
        AddMetric(2, "OWNER DIVIDEND", dividendContent, cashRate);
        Grid.SetRow(balances, 1);
        root.Children.Add(balances);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(288) });
        var estate = new Grid { Margin = new Thickness(0, 0, 16, 0) };
        estate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        estate.RowDefinitions.Add(new RowDefinition());
        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var item in new[] { (Id: "all", Text: "All rooms"), (Id: "open", Text: "Operating"), (Id: "locked", Text: "Locked") })
        {
            var choice = new Button { Name = "EmpireFilter_" + item.Id, Content = item.Text, FontSize = 11, MinHeight = 29, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(4, 0, 0, 0) };
            choice.Click += (_, _) => { filter = item.Id; Refresh(); };
            filters[item.Id] = choice;
            buttons.Children.Add(choice);
        }
        DockPanel.SetDock(buttons, Dock.Right);
        toolbar.Children.Add(buttons);
        estateStatus.VerticalAlignment = VerticalAlignment.Center;
        toolbar.Children.Add(estateStatus);
        estate.Children.Add(toolbar);
        var estateContent = new StackPanel();
        for (int i = 0; i < EmpireCatalog.Departments.Count; i++)
        {
            var card = new EmpireRoomCard(EmpireCatalog.Departments[i], i, id => { selected = id; Refresh(); });
            cards.Add(card);
            rooms.Children.Add(card);
        }
        estateContent.Children.Add(rooms);
        var ledger = new Border { Background = Ink("#10251F"), BorderBrush = Ink("#35483B"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Margin = new Thickness(2, 3, 12, 0), Padding = new Thickness(14, 13, 14, 14) };
        var ledgerBody = new StackPanel();
        ledgerBody.Children.Add(Label("ESTATE LEDGER  /  LIFETIME", 9, "#BEA674"));
        var ledgerMetrics = new Grid { Margin = new Thickness(0, 9, 0, 0) };
        for (int i = 0; i < 4; i++) ledgerMetrics.ColumnDefinitions.Add(new ColumnDefinition());
        LedgerValue(0, lifetime, "Credits produced");
        LedgerValue(1, cashClaimed, "Cash paid to you");
        LedgerValue(2, openRooms, "Rooms operating");
        LedgerValue(3, levels, "Total room levels");
        ledgerBody.Children.Add(ledgerMetrics);
        ledger.Child = ledgerBody;
        estateContent.Children.Add(ledger);
        roomScroll = new ScrollViewer { Content = estateContent, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, CanContentScroll = false };
        roomScroll.SizeChanged += (_, _) => ResizeRooms();
        Grid.SetRow(roomScroll, 1);
        estate.Children.Add(roomScroll);
        body.Children.Add(estate);

        var management = new StackPanel { Margin = new Thickness(18) };
        management.Children.Add(Label("ROOM MANAGEMENT", 9, "#BEA674"));
        roomTitle.FontFamily = new FontFamily("Georgia");
        roomTitle.Margin = new Thickness(0, 9, 0, 8);
        management.Children.Add(roomTitle);
        management.Children.Add(roomDescription);
        roomStats.Margin = new Thickness(0, 18, 0, 0);
        management.Children.Add(roomStats);
        buyLabel.TextAlignment = TextAlignment.Center;
        buyLabel.MaxWidth = 220;
        buy.Content = buyLabel;
        buy.Click += (_, _) =>
        {
            if (upgrade(selected)) SetNotice(service.Level(selected) == 1 ? "Room opened. A new part of your casino is now earning." : "Upgrade complete. Your room is now producing more credits.");
            Refresh();
        };
        management.Children.Add(buy);
        management.Children.Add(Label("THE NEXT MILESTONE", 9, "#BEA674"));
        milestoneTrack.Children.Add(milestoneFill);
        milestoneTrack.SizeChanged += (_, _) => RefreshMilestone();
        management.Children.Add(milestoneTrack);
        management.Children.Add(milestone);
        management.Children.Add(new Border { Background = Ink("#374C3E"), Height = 1, Margin = new Thickness(0, 18, 0, 15) });
        management.Children.Add(Label("EVERY ROOM HAS ITS OWN RHYTHM", 9, "#BEA674"));
        management.Children.Add(Label("Every 10 levels doubles this room's production. Golden details mark your upgraded estate. Everything earns automatically.", 12, "#99B0A2"));
        var side = new Border { Background = new LinearGradientBrush(Color.FromRgb(24, 43, 34), Color.FromRgb(15, 30, 26), 90), BorderBrush = Ink("#4A5943"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = new ScrollViewer { Content = management, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        Grid.SetColumn(side, 1);
        body.Children.Add(side);
        Grid.SetRow(body, 2);
        root.Children.Add(body);

        notice.Name = "EmpireNotice";
        var status = new Border { Padding = new Thickness(13, 9, 13, 9), Margin = new Thickness(0, 12, 0, 0), Background = Ink("#193127"), CornerRadius = new CornerRadius(7), Child = notice };
        Grid.SetRow(status, 3);
        root.Children.Add(status);
        Content = root;
        animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
        animationTimer.Tick += (_, _) =>
        {
            bool active = this.animations() && IsVisible && !disposed;
            if (active)
            {
                phase += .08;
                foreach (EmpireRoomCard card in cards.Where(card => card.IsVisible && card.Artwork.Open)) card.Artwork.Phase = phase;
            }
            else if (wasAnimating) foreach (EmpireRoomCard card in cards) card.StopAnimation();
            wasAnimating = active;
        };
        Loaded += (_, _) => { if (!disposed) animationTimer.Start(); ResizeRooms(); };
        Unloaded += (_, _) => animationTimer.Stop();
        Refresh();

        void AddMetric(int column, string label, UIElement value, UIElement detail)
        {
            var stack = new StackPanel { Margin = new Thickness(14, 10, 14, 10) };
            stack.Children.Add(Label(label, 9, "#BEA674"));
            if (value is FrameworkElement element) element.Margin = new Thickness(0, 3, 0, 2);
            stack.Children.Add(value);
            stack.Children.Add(detail);
            var panel = new Border { Background = Ink("#142A22"), BorderBrush = Ink("#3B4F3E"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Margin = new Thickness(column == 0 ? 0 : 10, 0, 0, 0), Child = stack };
            Grid.SetColumn(panel, column);
            balances.Children.Add(panel);
        }
        void LedgerValue(int column, TextBlock value, string label)
        {
            var metric = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            metric.Children.Add(value);
            metric.Children.Add(Label(label, 10, "#8FA89A"));
            Grid.SetColumn(metric, column);
            ledgerMetrics.Children.Add(metric);
        }
    }

    internal void Refresh()
    {
        if (disposed) return;
        bool animate = animations() && IsVisible;
        credits.SetAmount(service.Credits, animate);
        production.Text = Number(service.CreditsPerSecond) + " cr / sec";
        dividend.Text = service.ClaimableDividend.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        collect.Content = "Claim cash";
        collect.IsEnabled = service.ClaimableDividend >= .01m;
        cashRate.Text = $"${service.CashPerHour:0.00} / hour · $320 reserve capacity";
        EmpireDepartment room = EmpireCatalog.Find(selected)!;
        int level = service.Level(selected);
        roomTitle.Text = room.Name;
        roomDescription.Text = room.Description;
        roomStats.Text = level == 0 ? "Ready to open\nStarts at " + Number(room.BaseCreditsPerSecond) + " cr / sec" : $"LEVEL {level} / {EmpireRules.MaximumLevel}\n+{Number(EmpireRules.Production(room, level))} cr / sec";
        decimal cost = EmpireRules.UpgradeCost(room, level);
        buyLabel.Text = level == EmpireRules.MaximumLevel ? "Fully upgraded" : (level == 0 ? "Open room" : "Upgrade to level " + (level + 1)) + "  ·  " + Number(cost) + " cr";
        buy.IsEnabled = level < EmpireRules.MaximumLevel && service.Credits >= cost;
        RefreshMilestone();
        estateStatus.Text = $"YOUR CASINO  /  {service.OpenRooms} OF {EmpireCatalog.Departments.Count} ROOMS OPERATING";
        foreach (var item in filters) item.Value.BorderBrush = Ink(item.Key == filter ? "#D8BB7E" : "#42594A");
        foreach (EmpireRoomCard card in cards)
        {
            int value = service.Level(card.Department.Id);
            card.Visibility = filter == "open" && value == 0 || filter == "locked" && value > 0 ? Visibility.Collapsed : Visibility.Visible;
            card.Refresh(service, card.Department.Id == selected, animate);
        }
        EmpireState snapshot = service.Snapshot;
        lifetime.Text = Number(snapshot.LifetimeCredits) + " cr";
        cashClaimed.Text = snapshot.LifetimeCashClaimed.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        openRooms.Text = service.OpenRooms + " / " + EmpireCatalog.Departments.Count;
        levels.Text = service.TotalLevels.ToString(CultureInfo.InvariantCulture);
        ResizeRooms();
    }

    private void RefreshMilestone()
    {
        int level = service.Level(selected);
        milestoneFill.Width = milestoneTrack.ActualWidth * (level == EmpireRules.MaximumLevel ? 1 : level % 10 / 10d);
        milestone.Text = level == EmpireRules.MaximumLevel ? "Every room milestone reached." : $"Level {(level / 10 + 1) * 10}: ×2 room production\n{10 - level % 10} levels to the next milestone";
    }

    private void ResizeRooms()
    {
        double available = Math.Max(210, roomScroll.ActualWidth - 20);
        // Keep the compact grid stable when filtering down to one operating room.
        int columns = Math.Max(1, (int)(available / 222));
        double width = Math.Max(200, available / columns - 12);
        foreach (EmpireRoomCard card in cards) card.Width = width;
    }

    internal void SetNotice(string text) => notice.Text = text;
    internal void StopVisualAnimations()
    {
        wasAnimating = false;
        phase = 0;
        credits.SetAmount(service.Credits, false);
        foreach (EmpireRoomCard card in cards) card.StopAnimation();
    }
    public void Dispose()
    {
        disposed = true;
        animationTimer.Stop();
        StopVisualAnimations();
    }

    internal static string Number(decimal value)
    {
        (decimal Scale, string Suffix)[] units = [(1_000_000_000_000_000_000_000_000m, "Sp"), (1_000_000_000_000_000_000_000m, "Sx"), (1_000_000_000_000_000_000m, "Qi"), (1_000_000_000_000_000m, "Qa"), (1_000_000_000_000m, "T"), (1_000_000_000m, "B"), (1_000_000m, "M"), (1_000m, "K")];
        foreach (var unit in units) if (value >= unit.Scale) return (value / unit.Scale).ToString("0.##", CultureInfo.InvariantCulture) + unit.Suffix;
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
    private static TextBlock Label(string text, double size, string color) => new() { Text = text, FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap };
    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
