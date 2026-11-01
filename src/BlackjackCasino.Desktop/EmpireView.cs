using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>Responsive estate dashboard. The simulation is independent of its visual clocks.</summary>
internal sealed partial class EmpireView : UserControl, IDisposable
{
    private static readonly int[] BulkChoices = [1, 5, 10, 25, 50, 100];

    private readonly EmpireService service;
    private readonly Func<string, int, bool> upgrade;
    private readonly Func<bool> claim;
    private readonly Func<EmpirePrestigeResult> prestige;
    private readonly Func<bool> animations;
    private readonly Func<Func<EmpireService, EmpireActionResult>, EmpireActionResult> manage;
    private EmpireManagementView? managementView;
    private readonly ComboBox propertyPicker = new() { Name = "EmpirePropertySelector", DisplayMemberPath = "Name", SelectedValuePath = "Id", Width = 230, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 7) };
    private bool selectingProperty;
    private readonly TextBlock offlineDetail = Label("", 10, "#91A99C");
    private readonly Grid root = new() { Margin = new Thickness(24, 17, 24, 14) };
    private readonly WrapPanel rooms = new() { Name = "EmpireRoomGrid", HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly List<EmpireRoomCard> cards = [];
    private readonly EmpireBalanceCounter credits = new() { FontSize = 25, Foreground = Ink("#EFD5A0") };
    private readonly EmpireBalanceCounter production = new() { FontSize = 25, Foreground = Ink("#A8D9BB"), UnitText = " cr / sec" };
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
    private readonly ScrollViewer roomScroll = new();
    private readonly DispatcherTimer animationTimer;
    private string selected = "";
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
        string backLabel,
        Func<Func<EmpireService, EmpireActionResult>, EmpireActionResult>? managementAction = null)
    {
        this.service = service;
        this.upgrade = upgrade;
        this.claim = claim;
        this.prestige = prestige;
        this.animations = animations;
        manage = managementAction ?? (change => change(service));
        Name = "CasinoEmpirePage";
        FontFamily = new FontFamily("Segoe UI");
        Foreground = Ink("#F2E4C8");
        Background = new LinearGradientBrush(Color.FromRgb(8, 20, 19), Color.FromRgb(26, 23, 29), 25);
        Resources[typeof(Button)] = Application.Current.FindResource("CasinoButton");

        BuildCommandCenter(back, backLabel);

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
        Unloaded += (_, _) => { animationTimer.Stop(); ClearPreview(); StopWorkspaceAnimation(); };
        Refresh();

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
            selected = "";

        bool animate = animations() && IsVisible;
        credits.SetAmount(service.Credits, animate);
        production.SetAmount(service.TotalCreditsPerSecond, animate);
        dividend.Text = service.ClaimableDividend.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
        collect.Content = "Claim cash";
        collect.IsEnabled = service.ClaimableDividend >= .01m;
        cashRate.Text = $"${service.CashPerHour:0.00} / hour · ${service.DividendCapacity:0} shared reserve";
        offlineDetail.Text = $"All owned properties earn · {service.OfflineCapacity.TotalHours:0}h offline storage";
        selectingProperty = true;
        if (propertyPicker.Items.Count != service.OwnedProperties.Count) propertyPicker.ItemsSource = service.OwnedProperties;
        propertyPicker.SelectedValue = service.ActivePropertyId;
        selectingProperty = false;

        foreach ((int floor, Button button) in floors)
        {
            button.IsEnabled = service.FloorUnlocked(floor);
            EmpireFloorDefinition definition = EmpireFloorCatalog.Find(floor)!;
            button.Content = definition.DisplayName.ToUpperInvariant() + (button.IsEnabled ? "" : " · LOCKED");
            button.ToolTip = button.IsEnabled ? definition.DisplayName : definition.Requirement.Description;
            button.BorderBrush = Ink(floor == selectedFloor ? "#D8BB7E" : "#42594A");
        }
        foreach ((string id, Button button) in filters) button.BorderBrush = Ink(id == filter ? "#D8BB7E" : "#42594A");
        foreach ((int count, Button button) in bulkButtons) button.BorderBrush = Ink(count == bulk ? "#E4C47F" : "#42594A");

        if (selected.Length > 0)
        {
            EmpireDepartment room = EmpireCatalog.Find(selected)!;
            int level = service.Level(selected);
            decimal achievementBonus = service.AchievementBonusPercent(selected);
            decimal effectiveProduction = service.RoomCreditsPerSecond(selected);
            roomTitle.Text = room.Name;
            roomDescription.Text = room.Description;
            roomStats.Text = level == 0
                ? "Ready to open\nStarts at " + Number(room.BaseCreditsPerSecond * service.PrestigeMultiplier * (1m + achievementBonus)) + " cr / sec"
                : $"LEVEL {level} / {EmpireRules.MaximumLevel}\n+{Number(effectiveProduction)} cr / sec";
            roomBoosts.Text = $"{room.Category} · {room.Tags}\nEmpire Prestige +{service.PrestigeBonusPercent:P0} · Achievements +{achievementBonus:P1}\nConstruction discount {service.ConstructionDiscount(selected):P0}";

            EmpireUpgradeQuote quote = service.QuoteUpgrade(selected, bulk);
            if (level >= EmpireRules.MaximumLevel)
            {
                buyLabel.Text = "Fully upgraded";
                buy.IsEnabled = false;
            }
            else if (!quote.Unlocked)
            {
                buyLabel.Text = "Locked · " + room.UnlockRequirement.Description;
                buy.IsEnabled = false;
            }
            else
            {
                buyLabel.Text = $"Upgrade ×{quote.Levels}  ·  LV {quote.StartLevel} → {quote.TargetLevel}\n{Number(quote.Cost)} cr";
                buy.IsEnabled = quote.Levels > 0 && service.Credits >= quote.Cost;
            }

            RefreshMilestone();
        }
        int floorRooms = EmpireCatalog.Floor(selectedFloor).Count;
        estateStatus.Text = $"{service.ActiveProperty.Name.ToUpperInvariant()} / {EmpireFloorCatalog.Find(selectedFloor)!.DisplayName.ToUpperInvariant()} / {service.OpenRoomsOnFloor(selectedFloor)} OF {floorRooms} ROOMS OPERATING";

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
            + string.Join(" · ", EmpireFloorCatalog.All.Where(floor => floor.Requirement.Prestige > 0).Select(floor => floor.DisplayName + (service.FloorUnlocked(floor.Id) ? " unlocked" : " locked")));
        prestigeRequirement.Text = service.PrestigeRequirementText();
        prestigeButton.IsEnabled = service.CanPrestige;
        prestigeActionCaption.Text = service.CanPrestige ? $"PRESTIGE → {service.Prestige + 1}" : "PRESTIGE LOCKED";
        managementView?.Refresh();
        RefreshCommandCenter();
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
        if (report.Properties.Count > 1)
            foreach (var (id, earned) in report.Properties)
                overlayBody.Children.Add(Label(EmpirePropertyCatalog.Find(id)!.Name + ": +" + Number(earned) + " cr", 12, "#B9DFC4"));
        overlayBody.Children.Add(Label("SELECTED PROPERTY · ROOM EARNINGS", 9, "#B89B69", new Thickness(0, 0, 0, 6)));

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
            line.Children.Add(Label(room is null ? pair.Key : (room.Floor == 1 ? room.Name : EmpireFloorCatalog.Find(room.Floor)!.DisplayName + " · " + room.Name), 12, "#D8E2DA"));
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
            overlayBody.Children.Add(Label("Offline production reached the " + service.OfflineCapacity.TotalHours.ToString("0", CultureInfo.InvariantCulture) + "-hour storage limit. Excess absence has been consumed.", 11, "#D9BD82", new Thickness(0, 12, 0, 0)));

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

    internal bool TryCloseManagement()
    {
        if (managementView is null) return false;
        OpenSection("Overview");
        return true;
    }

    private void PrestigeClicked()
    {
        if (!service.CanPrestige) return;
        Window? owner = Window.GetWindow(this);
        MessageBoxResult answer = owner is null
            ? MessageBox.Show("Empire Prestige resets construction credits and all room levels. Research, Legacy Points, capital projects, contracts, managers, other properties and lifetime statistics remain. The active event ends. Continue?", "Empire Prestige", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            : MessageBox.Show(owner, "Empire Prestige resets construction credits and all room levels. Research, Legacy Points, capital projects, contracts, managers, other properties and lifetime statistics remain. The active event ends. Continue?", "Empire Prestige", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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
        double available = Math.Max(240, (roomScroll.ViewportWidth > 0 ? roomScroll.ViewportWidth : roomScroll.ActualWidth - 20) - 4);
        int columns = available < 620 ? 3 : 5;
        double width = Math.Min(335, available / columns - 10);
        double height = Math.Clamp(roomScroll.ActualHeight - 44, 150, 310);
        foreach (EmpireRoomCard card in cards) card.SetSize(width, height);

    }

    internal void SetNotice(string text) => notice.Text = text;

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        animationTimer.Stop();
        ClearPreview(); StopWorkspaceAnimation();
        foreach (EmpireRoomCard card in cards) card.StopAnimation();
    }

    internal static string Number(decimal value)
    {
        decimal absolute = Math.Abs(value);
        (decimal Divisor, string Suffix)[] suffixes =
        [
            (1_000_000_000_000_000_000_000_000_000m, "Oc"),
            (1_000_000_000_000_000_000_000_000m, "Sp"),
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
