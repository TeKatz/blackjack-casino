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
    private static readonly int[] BulkChoices = [1, 5, 10, 25, 50, 100];

    private readonly EmpireService service;
    private readonly Func<string, int, bool> upgrade;
    private readonly Func<bool> claim;
    private readonly Func<EmpirePrestigeResult> prestige;
    private readonly Func<bool> animations;
    private readonly Grid root = new() { Margin = new Thickness(24, 17, 24, 14) };
    private readonly WrapPanel rooms = new() { Name = "EmpireRoomGrid", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly List<EmpireRoomCard> cards = [];
    private readonly EmpireBalanceCounter credits = new() { FontSize = 25, Foreground = Ink("#EFD5A0") };
    private readonly TextBlock production = Label("", 25, "#A8D9BB");
    private readonly TextBlock dividend = Label("", 23, "#EFD5A0");
    private readonly TextBlock cashRate = Label("", 10, "#9BB7A5");
    private readonly TextBlock roomTitle = Label("", 26, "#F2E4C8");
    private readonly TextBlock roomDescription = Label("", 13, "#A5BCAF");
    private readonly TextBlock roomStats = Label("", 15, "#D1DFD4");
    private readonly TextBlock roomBoosts = Label("", 11, "#B9CDBF");
    private readonly TextBlock milestone = Label("", 12, "#B5C8B8");
    private readonly TextBlock estateStatus = Label("", 10, "#9DB1A2");
    private readonly TextBlock lifetime = Label("", 21, "#E6CE9D");
    private readonly TextBlock cashClaimed = Label("", 21, "#E6CE9D");
    private readonly TextBlock levels = Label("", 21, "#B2D3B8");
    private readonly TextBlock openRooms = Label("", 21, "#B2D3B8");
    private readonly TextBlock notice = Label("Your casino is working. Select a room to open or upgrade it.", 12, "#B8CCBB");
    private readonly TextBlock buyLabel = Label("", 13, "#F3E7CC");
    private readonly TextBlock prestigeTitle = Label("", 18, "#E8CE99");
    private readonly TextBlock prestigeBonus = Label("", 12, "#B9D8C0");
    private readonly TextBlock prestigeRequirement = Label("", 11, "#9FB4A7");
    private readonly Button buy = new() { Name = "EmpireUpgradeButton", Margin = new Thickness(0, 10, 0, 14), MinHeight = 48 };
    private readonly Button collect = new() { Name = "EmpireClaimButton", Padding = new Thickness(13, 8, 13, 8), MinHeight = 35, FontSize = 12 };
    private readonly Button prestigeButton = new() { Name = "EmpirePrestigeButton", Padding = new Thickness(12, 8, 12, 8), MinHeight = 40, Margin = new Thickness(0, 10, 0, 0) };
    private readonly Dictionary<string, Button> filters = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Button> floors = [];
    private readonly Dictionary<int, Button> bulkButtons = [];
    private readonly Border milestoneFill = new() { Height = 5, Background = Ink("#C7AA6E"), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3) };
    private readonly Grid milestoneTrack = new() { Height = 5, Background = Ink("#2A3E34"), Margin = new Thickness(0, 8, 0, 10) };
    private readonly ScrollViewer roomScroll;
    private readonly DispatcherTimer animationTimer;
    private string selected = "slots";
    private string filter = "all";
    private int selectedFloor = 1;
    private int bulk = 1;
    private bool disposed;
    private bool wasAnimating;
    private double phase;

    internal EmpireView(
        EmpireService service,
        Func<string, int, bool> upgrade,
        Func<bool> claim,
        Func<EmpirePrestigeResult> prestige,
        Action back,
        Func<bool> animations,
        string backLabel)
    {
        this.service = service;
        this.upgrade = upgrade;
        this.claim = claim;
        this.prestige = prestige;
        this.animations = animations;
        Name = "CasinoEmpirePage";
        FontFamily = new FontFamily("Segoe UI");
        Foreground = Ink("#F2E4C8");
        Background = new LinearGradientBrush(Color.FromRgb(8, 20, 19), Color.FromRgb(26, 23, 29), 25);
        Resources[typeof(Button)] = Application.Current.FindResource("CasinoButton");

        foreach (GridLength height in new[] { GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = height });

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 13) };
        var exit = new Button { Name = "EmpireBackButton", Content = backLabel, VerticalAlignment = VerticalAlignment.Center };
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
        collect.Click += (_, _) => { this.claim(); Refresh(); };
        DockPanel.SetDock(collect, Dock.Right);
        dividendContent.Children.Add(collect);
        dividendContent.Children.Add(dividend);
        AddMetric(2, "OWNER DIVIDEND", dividendContent, cashRate);
        Grid.SetRow(balances, 1);
        root.Children.Add(balances);

        var body = new Grid();
        body.ColumnDefinitions.Add(new ColumnDefinition());
        body.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(315) });
        var estate = new Grid { Margin = new Thickness(0, 0, 16, 0) };
        estate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        estate.RowDefinitions.Add(new RowDefinition());

        var toolbar = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        toolbar.ColumnDefinitions.Add(new ColumnDefinition());
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var floorBar = new StackPanel { Orientation = Orientation.Horizontal };
        AddFloorButton(1, "MAIN FLOOR");
        AddFloorButton(2, "UPPER FLOOR · PRESTIGE I");
        floorBar.Children.Add(floors[1]);
        floorBar.Children.Add(floors[2]);
        toolbar.Children.Add(floorBar);

        var filterBar = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var item in new[] { (Id: "all", Text: "All rooms"), (Id: "open", Text: "Operating"), (Id: "locked", Text: "Locked") })
        {
            var choice = new Button { Name = "EmpireFilter_" + item.Id, Content = item.Text, FontSize = 11, MinHeight = 29, Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(4, 0, 0, 0) };
            choice.Click += (_, _) => { filter = item.Id; Refresh(); };
            filters[item.Id] = choice;
            filterBar.Children.Add(choice);
        }
        Grid.SetColumn(filterBar, 1);
        toolbar.Children.Add(filterBar);
        estate.Children.Add(toolbar);

        var estateContent = new StackPanel();
        estateStatus.Margin = new Thickness(2, 0, 0, 7);
        estateContent.Children.Add(estateStatus);
        for (int i = 0; i < EmpireCatalog.Departments.Count; i++)
        {
            var card = new EmpireRoomCard(EmpireCatalog.Departments[i], i, id =>
            {
                selected = id;
                selectedFloor = EmpireCatalog.Find(id)?.Floor ?? selectedFloor;
                Refresh();
            });
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
        roomDescription.TextWrapping = TextWrapping.Wrap;
        management.Children.Add(roomDescription);
        roomStats.Margin = new Thickness(0, 16, 0, 4);
        management.Children.Add(roomStats);
        roomBoosts.TextWrapping = TextWrapping.Wrap;
        management.Children.Add(roomBoosts);

        management.Children.Add(Label("BULK UPGRADE", 9, "#BEA674", new Thickness(0, 15, 0, 5)));
        var bulkBar = new WrapPanel();
        foreach (int count in BulkChoices)
        {
            int value = count;
            var button = new Button
            {
                Name = "EmpireBulk" + count + "Button",
                Content = "×" + count,
                MinWidth = count >= 100 ? 53 : 43,
                MinHeight = 28,
                Padding = new Thickness(7, 3, 7, 3),
                Margin = new Thickness(0, 0, 5, 5),
                FontSize = 11,
            };
            button.Click += (_, _) => { bulk = value; Refresh(); };
            bulkButtons[count] = button;
            bulkBar.Children.Add(button);
        }
        management.Children.Add(bulkBar);

        buyLabel.TextAlignment = TextAlignment.Center;
        buyLabel.TextWrapping = TextWrapping.Wrap;
        buyLabel.MaxWidth = 250;
        buy.Content = buyLabel;
        buy.Click += (_, _) =>
        {
            int before = service.Level(selected);
            if (this.upgrade(selected, bulk))
            {
                int gained = service.Level(selected) - before;
                SetNotice(before == 0 ? "Room opened. A new part of your casino is now earning." : $"Upgrade complete. +{gained} room level{(gained == 1 ? string.Empty : "s")}.");
            }
            Refresh();
        };
        management.Children.Add(buy);

        management.Children.Add(Label("THE NEXT MILESTONE", 9, "#BEA674"));
        milestoneTrack.Children.Add(milestoneFill);
        milestoneTrack.SizeChanged += (_, _) => RefreshMilestone();
        management.Children.Add(milestoneTrack);
        management.Children.Add(milestone);

        management.Children.Add(new Border { Background = Ink("#374C3E"), Height = 1, Margin = new Thickness(0, 18, 0, 15) });
        management.Children.Add(Label("EMPIRE PRESTIGE", 9, "#BEA674"));
        prestigeTitle.FontFamily = new FontFamily("Georgia");
        prestigeTitle.Margin = new Thickness(0, 7, 0, 4);
        management.Children.Add(prestigeTitle);
        prestigeBonus.TextWrapping = TextWrapping.Wrap;
        management.Children.Add(prestigeBonus);
        prestigeRequirement.Margin = new Thickness(0, 6, 0, 0);
        prestigeRequirement.TextWrapping = TextWrapping.Wrap;
        management.Children.Add(prestigeRequirement);
        prestigeButton.Content = "PRESTIGE EMPIRE";
        prestigeButton.Click += (_, _) => PrestigeClicked();
        management.Children.Add(prestigeButton);

        management.Children.Add(new Border { Background = Ink("#374C3E"), Height = 1, Margin = new Thickness(0, 18, 0, 15) });
        management.Children.Add(Label("HOW THE HOUSE GROWS", 9, "#BEA674"));
        management.Children.Add(Label("Every 10 room levels doubles that room's base production. Claimed achievements add permanent room boosts. Empire Prestige resets room levels and construction credits but permanently accelerates production.", 11, "#99B0A2"));

        var side = new Border { Background = new LinearGradientBrush(Color.FromRgb(24, 43, 34), Color.FromRgb(15, 30, 26), 90), BorderBrush = Ink("#4A5943"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Child = new ScrollViewer { Content = management, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        Grid.SetColumn(side, 1);
        body.Children.Add(side);
        Grid.SetRow(body, 2);
        root.Children.Add(body);

        notice.Name = "EmpireNotice";
        notice.TextWrapping = TextWrapping.Wrap;
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
            else if (wasAnimating)
            {
                foreach (EmpireRoomCard card in cards) card.StopAnimation();
            }
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

        void AddFloorButton(int floor, string text)
        {
            var button = new Button
            {
                Name = "EmpireFloor" + floor + "Button",
                Content = text,
                MinHeight = 29,
                Padding = new Thickness(11, 4, 11, 4),
                Margin = new Thickness(floor == 1 ? 0 : 5, 0, 0, 0),
                FontSize = 10,
            };
            button.Click += (_, _) =>
            {
                if (!service.FloorUnlocked(floor)) return;
                selectedFloor = floor;
                selected = EmpireCatalog.Floor(floor).First().Id;
                filter = "all";
                Refresh();
            };
            floors[floor] = button;
        }
    }

    // Compatibility overload for presentation helpers written against Empire v1.
    internal EmpireView(EmpireService service, Func<string, bool> upgrade, Func<bool> claim, Action back, Func<bool> animations)
        : this(service, (id, count) => count == 1 && upgrade(id), claim,
            () => new EmpirePrestigeResult(false, service.Prestige, service.PrestigeBonusPercent, service.PrestigeRequirementText()),
            back, animations, "←  Casino lobby")
    {
    }

    internal void Refresh()
    {
        if (disposed) return;
        if (!service.FloorUnlocked(selectedFloor)) selectedFloor = 1;
        EmpireDepartment? selectedRoom = EmpireCatalog.Find(selected);
        if (selectedRoom is null || selectedRoom.Floor != selectedFloor)
            selected = EmpireCatalog.Floor(selectedFloor).First().Id;

        bool animate = animations() && IsVisible;
        credits.SetAmount(service.Credits, animate);
        production.Text = Number(service.CreditsPerSecond) + " cr / sec";
        dividend.Text = service.ClaimableDividend.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        collect.Content = "Claim cash";
        collect.IsEnabled = service.ClaimableDividend >= .01m;
        cashRate.Text = $"${service.CashPerHour:0.00} / hour · $320 reserve capacity";

        foreach ((int floor, Button button) in floors)
        {
            button.IsEnabled = service.FloorUnlocked(floor);
            button.Content = floor == 1 ? "MAIN FLOOR" : service.FloorUnlocked(2) ? "UPPER FLOOR" : "UPPER FLOOR · PRESTIGE I";
            button.BorderBrush = Ink(floor == selectedFloor ? "#D8BB7E" : "#42594A");
        }
        foreach ((string id, Button button) in filters) button.BorderBrush = Ink(id == filter ? "#D8BB7E" : "#42594A");
        foreach ((int count, Button button) in bulkButtons) button.BorderBrush = Ink(count == bulk ? "#E4C47F" : "#42594A");

        EmpireDepartment room = EmpireCatalog.Find(selected)!;
        int level = service.Level(selected);
        decimal achievementBonus = service.AchievementBonusPercent(selected);
        decimal effectiveProduction = service.RoomCreditsPerSecond(selected);
        roomTitle.Text = room.Name;
        roomDescription.Text = room.Description;
        roomStats.Text = level == 0
            ? "Ready to open\nStarts at " + Number(room.BaseCreditsPerSecond * service.PrestigeMultiplier * (1m + achievementBonus)) + " cr / sec"
            : $"LEVEL {level} / {EmpireRules.MaximumLevel}\n+{Number(effectiveProduction)} cr / sec";
        roomBoosts.Text = $"Empire Prestige +{service.PrestigeBonusPercent:P0} · Achievements +{achievementBonus:P1}";

        EmpireUpgradeQuote quote = service.QuoteUpgrade(selected, bulk);
        if (level >= EmpireRules.MaximumLevel)
        {
            buyLabel.Text = "Fully upgraded";
            buy.IsEnabled = false;
        }
        else if (!quote.Unlocked)
        {
            buyLabel.Text = "Locked behind Empire Prestige";
            buy.IsEnabled = false;
        }
        else
        {
            buyLabel.Text = $"Upgrade ×{quote.Levels}  ·  LV {quote.StartLevel} → {quote.TargetLevel}\n{Number(quote.Cost)} cr";
            buy.IsEnabled = quote.Levels > 0 && service.Credits >= quote.Cost;
        }

        RefreshMilestone();
        int floorRooms = EmpireCatalog.Floor(selectedFloor).Count;
        estateStatus.Text = selectedFloor == 1
            ? $"MAIN FLOOR  /  {service.OpenRoomsOnFloor(1)} OF {floorRooms} ROOMS OPERATING"
            : $"UPPER FLOOR  /  {service.OpenRoomsOnFloor(2)} OF {floorRooms} ROOMS OPERATING  /  PRESTIGE {service.Prestige}";

        foreach (EmpireRoomCard card in cards)
        {
            int value = service.Level(card.Department.Id);
            bool onFloor = card.Department.Floor == selectedFloor;
            bool passesFilter = filter switch
            {
                "open" => value > 0,
                "locked" => value == 0,
                _ => true,
            };
            card.Visibility = onFloor && passesFilter ? Visibility.Visible : Visibility.Collapsed;
            card.Refresh(service, card.Department.Id == selected, animate);
        }

        EmpireState snapshot = service.Snapshot;
        lifetime.Text = Number(snapshot.LifetimeCredits) + " cr";
        cashClaimed.Text = snapshot.LifetimeCashClaimed.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        int availableRooms = EmpireCatalog.Departments.Count(room => service.FloorUnlocked(room.Floor));
        openRooms.Text = service.OpenRooms + " / " + availableRooms;
        levels.Text = service.TotalLevels.ToString(CultureInfo.InvariantCulture);

        prestigeTitle.Text = service.Prestige == 0 ? "Prestige not yet claimed" : $"Empire Prestige {service.Prestige}";
        prestigeBonus.Text = $"Permanent production bonus: +{service.PrestigeBonusPercent:P0}. "
            + (service.Floor2Unlocked ? "Upper Floor unlocked permanently." : "First Prestige permanently unlocks the Upper Floor.");
        prestigeRequirement.Text = service.PrestigeRequirementText();
        prestigeButton.IsEnabled = service.CanPrestige;
        prestigeButton.Content = service.CanPrestige ? $"PRESTIGE → {service.Prestige + 1}" : "PRESTIGE LOCKED";
        ResizeRooms();
    }

    internal void ShowOfflineReport(EmpireAccrual report)
    {
        if (disposed || report.Elapsed < TimeSpan.FromMinutes(2)) return;

        var overlayBody = new StackPanel { Margin = new Thickness(26) };
        overlayBody.Children.Add(Label("WELCOME BACK, BOSS", 10, "#D6B478"));
        overlayBody.Children.Add(new TextBlock
        {
            Text = "The house kept working.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 30,
            Foreground = Ink("#F5E7CC"),
            Margin = new Thickness(0, 7, 0, 4),
        });
        overlayBody.Children.Add(Label("Offline for " + OfflineDuration(report.Elapsed), 13, "#9FB5A8", new Thickness(0, 0, 0, 15)));

        var totals = new Grid { Margin = new Thickness(0, 0, 0, 15) };
        totals.ColumnDefinitions.Add(new ColumnDefinition());
        totals.ColumnDefinitions.Add(new ColumnDefinition());
        AddOfflineMetric(totals, 0, "EMPIRE CREDITS", "+" + Number(report.Credits) + " cr");
        AddOfflineMetric(totals, 1, "OWNER DIVIDEND", "+" + report.Dividend.ToString("C2", CultureInfo.GetCultureInfo("en-US")));
        overlayBody.Children.Add(totals);
        overlayBody.Children.Add(Label("ROOM EARNINGS", 9, "#B89B69", new Thickness(0, 0, 0, 6)));

        var roomList = new StackPanel();
        foreach (var pair in report.Rooms.OrderBy(pair => EmpireCatalog.Find(pair.Key)?.Floor ?? int.MaxValue)
                     .ThenBy(pair => EmpireCatalog.Find(pair.Key)?.Name ?? pair.Key))
        {
            if (pair.Value <= 0) continue;
            EmpireDepartment? room = EmpireCatalog.Find(pair.Key);
            var line = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
            var amount = Label("+" + Number(pair.Value) + " cr", 12, "#B9DFC4");
            amount.FontWeight = FontWeights.SemiBold;
            DockPanel.SetDock(amount, Dock.Right);
            line.Children.Add(amount);
            line.Children.Add(Label(room is null ? pair.Key : (room.Floor == 1 ? room.Name : "Upper Floor · " + room.Name), 12, "#D8E2DA"));
            roomList.Children.Add(line);
        }
        overlayBody.Children.Add(new Border
        {
            Background = Ink("#10241E"),
            BorderBrush = Ink("#33483D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(13, 8, 13, 8),
            Child = roomList,
        });
        if (report.Capped)
            overlayBody.Children.Add(Label("Offline production reached the 8-hour storage limit. Time beyond the cap was not paid twice.", 11, "#D9BD82", new Thickness(0, 12, 0, 0)));

        var close = new Button
        {
            Name = "EmpireOfflineContinueButton",
            Content = "COLLECT & CONTINUE",
            MinHeight = 42,
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(18, 8, 18, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        overlayBody.Children.Add(close);

        var card = new Border
        {
            MaxWidth = 560,
            MaxHeight = 650,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new LinearGradientBrush(Color.FromRgb(10, 29, 24), Color.FromRgb(28, 23, 31), 35),
            BorderBrush = Ink("#8B7048"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Child = new ScrollViewer { Content = overlayBody, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
        };
        var overlay = new Grid
        {
            Name = "EmpireOfflineReportOverlay",
            Background = new SolidColorBrush(Color.FromArgb(218, 3, 9, 8)),
        };
        overlay.Children.Add(card);
        Grid.SetRowSpan(overlay, 4);
        Panel.SetZIndex(overlay, 5000);
        close.Click += (_, _) => root.Children.Remove(overlay);
        root.Children.Add(overlay);
    }

    private void PrestigeClicked()
    {
        if (!service.CanPrestige) return;
        Window? owner = Window.GetWindow(this);
        MessageBoxResult answer = owner is null
            ? MessageBox.Show("Empire Prestige resets construction credits and all room levels. Lifetime statistics, claimed achievement boosts, dividends and permanent Prestige progress remain. Continue?", "Empire Prestige", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, "Empire Prestige resets construction credits and all room levels. Lifetime statistics, claimed achievement boosts, dividends and permanent Prestige progress remain. Continue?", "Empire Prestige", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        EmpirePrestigeResult result = prestige();
        SetNotice(result.Message);
        if (result.Success)
        {
            selectedFloor = 1;
            selected = "slots";
            filter = "all";
            bulk = 1;
        }
        Refresh();
    }

    private void RefreshMilestone()
    {
        int level = service.Level(selected);
        double progress = level == EmpireRules.MaximumLevel ? 1d : level % 10 / 10d;
        milestoneFill.Width = Math.Max(0, milestoneTrack.ActualWidth * progress);
        milestone.Text = level == EmpireRules.MaximumLevel
            ? "All room milestones complete."
            : $"Level {((level / 10) + 1) * 10}: base production ×2";
    }

    private void ResizeRooms()
    {
        double available = Math.Max(280, roomScroll.ViewportWidth > 0 ? roomScroll.ViewportWidth - 22 : ActualWidth - 370);
        int columns = Math.Max(1, (int)(available / 285));
        double width = Math.Clamp(available / columns - 13, 250, 335);
        foreach (EmpireRoomCard card in cards) card.Width = width;
    }

    internal void SetNotice(string text) => notice.Text = text;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        animationTimer.Stop();
        foreach (EmpireRoomCard card in cards) card.StopAnimation();
    }

    internal static string Number(decimal value)
    {
        decimal absolute = Math.Abs(value);
        (decimal Divisor, string Suffix)[] suffixes =
        [
            (1_000_000_000_000_000_000_000m, "Sx"),
            (1_000_000_000_000_000_000m, "Qi"),
            (1_000_000_000_000_000m, "Qa"),
            (1_000_000_000_000m, "T"),
            (1_000_000_000m, "B"),
            (1_000_000m, "M"),
            (1_000m, "K"),
        ];
        foreach ((decimal divisor, string suffix) in suffixes)
        {
            if (absolute >= divisor) return (value / divisor).ToString("0.##", CultureInfo.InvariantCulture) + suffix;
        }
        return value.ToString(absolute >= 100 ? "0" : "0.##", CultureInfo.InvariantCulture);
    }

    private static string OfflineDuration(TimeSpan elapsed)
        => elapsed.TotalHours >= 1 ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m" : $"{Math.Max(1, (int)elapsed.TotalMinutes)}m";

    private static void AddOfflineMetric(Grid grid, int column, string label, string value)
    {
        var stack = new StackPanel { Margin = new Thickness(column == 0 ? 0 : 7, 0, column == 0 ? 7 : 0, 0) };
        stack.Children.Add(Label(label, 9, "#B89B69"));
        stack.Children.Add(new TextBlock { Text = value, FontFamily = new FontFamily("Georgia"), FontSize = 21, Foreground = Ink("#F0D39C"), Margin = new Thickness(0, 3, 0, 0) });
        var panel = new Border { Background = Ink("#132820"), BorderBrush = Ink("#3A4E40"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 9, 12, 9), Child = stack };
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private static TextBlock Label(string text, double size, string color, Thickness? margin = null) => new()
    {
        Text = text,
        FontSize = size,
        Foreground = Ink(color),
        Margin = margin ?? new Thickness(0),
        TextWrapping = TextWrapping.Wrap,
    };

    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
