using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    private readonly Grid workspace = new();
    private readonly Grid overview = new() { Name = "EmpireOverview" };
    private readonly Dictionary<string, Button> routes = new(StringComparer.Ordinal);
    private readonly TextBlock legacySummary = Label("", 11, "#CBB791");
    private readonly ColumnDefinition inspectorColumn = new() { Width = new GridLength(270) };
    private readonly TextBlock emptyRooms = Label("No rooms match this filter. Choose All rooms to see this floor.", 12, "#AFC5B6");
    private readonly StackPanel inspectorContent = new() { Margin = new Thickness(14) };
    private readonly TextBlock inspectorEmpty = Label("Select a room\n\nExplore the estate, then select a department to see its upgrades here and understand its performance below.", 15, "#ACC4B3", new Thickness(20));

    private void BuildCommandCenter(Action back, string backLabel)
    {
        Background = new RadialGradientBrush(Color.FromRgb(24, 49, 36), Color.FromRgb(6, 18, 14))
        { Center = new Point(.25, .1), GradientOrigin = new Point(.25, .1), RadiusX = 1.2, RadiusY = 1.2 };
        root.Margin = new Thickness(20, 10, 20, 10);
        foreach (GridLength height in new[] { GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = height });
        // The statistics presenter attaches to this direct-child header.
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        Button exit = Command(backLabel, "EmpireBackButton", back);
        DockPanel.SetDock(exit, Dock.Right); header.Children.Add(exit);
        legacySummary.VerticalAlignment = VerticalAlignment.Center; legacySummary.Margin = new Thickness(12, 0, 18, 0);
        DockPanel.SetDock(legacySummary, Dock.Right); header.Children.Add(legacySummary);
        header.Children.Add(new TextBlock { Text = "Casino Empire", FontFamily = new FontFamily("Georgia"), FontSize = 30, Foreground = Ink("#F7E9CE") });
        root.Children.Add(header);

        var balances = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 9) };
        Metric("01  /  CONSTRUCTION BUDGET", "Budget", credits, budgetPreview, 0);
        Metric("02  /  PORTFOLIO PRODUCTION", "Production", production, productionPreview, 1);
        var payout = new DockPanel(); collect.VerticalAlignment = VerticalAlignment.Center;
        collect.Click += (_, _) => { ClearPreview(); claim(); Refresh(); };
        DockPanel.SetDock(collect, Dock.Right); payout.Children.Add(collect); payout.Children.Add(dividend);
        Metric("03  /  YOUR OWNER DIVIDEND", "Dividend", payout, cashRate, 2);
        Grid.SetRow(balances, 1); root.Children.Add(balances);
        production.ToolTip = "Production across all owned properties. Hover an action to preview its portfolio impact.";
        credits.ToolTip = "Construction credits in the selected estate.";

        var body = new Grid(); body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); body.RowDefinitions.Add(new RowDefinition());
        var navigation = new UniformGrid { Columns = 4, Margin = new Thickness(0, 0, 0, 9) };
        foreach (var (section, caption, name) in new[] {
            ("Overview", "Estate", "EmpireOverviewButton"), ("Development", "Development", "EmpireDevelopmentButton"),
            ("Managers", "Operations", "EmpireOperationsButton"), ("Events", "Opportunities", "EmpireOpportunitiesButton") })
        {
            Button tab = Command(caption, name, () => OpenSection(section));
            tab.Content = IconCaption(caption, caption); tab.Margin = new Thickness(0, 0, 6, 0);
            AutomationProperties.SetName(tab, caption); routes[section] = tab; navigation.Children.Add(tab);
        }
        body.Children.Add(navigation);
        overview.ColumnDefinitions.Add(new ColumnDefinition()); overview.ColumnDefinitions.Add(inspectorColumn);
        var estate = new Grid { Margin = new Thickness(0, 0, 14, 0) };
        estate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        estate.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        estate.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.15, GridUnitType.Star) });
        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        propertyPicker.Width = 190; propertyPicker.MinHeight = 28; propertyPicker.Margin = new Thickness(0, 0, 10, 0);
        AutomationProperties.SetName(propertyPicker, "Active estate"); toolbar.Children.Add(propertyPicker);
        propertyPicker.SelectionChanged += (_, _) =>
        {
            if (selectingProperty || propertyPicker.SelectedValue is not string id || id == service.ActivePropertyId) return;
            ClearPreview(); manage(s => s.SelectProperty(id, DateTimeOffset.UtcNow)); selectedFloor = 1; selected = ""; Refresh();
        };
        var floorBar = new WrapPanel();
        foreach (EmpireFloorDefinition floor in EmpireFloorCatalog.All.OrderBy(f => f.SortOrder))
        {
            int id = floor.Id;
            Button button = Command(floor.DisplayName, "EmpireFloor" + id + "Button", () =>
            {
                if (!service.FloorUnlocked(id)) return;
                ClearPreview(); selectedFloor = id; selected = ""; filter = "all"; Refresh();
            });
            button.FontSize = 10; button.Padding = new Thickness(7, 5, 7, 5); button.Margin = new Thickness(0, 0, 4, 0);
            floors[id] = button; floorBar.Children.Add(button);
        }
        Button circuits = Command("Circuits", "EmpireTab_Synergies", () => OpenSection("Synergies"));
        circuits.Content = IconCaption("Synergies", "Circuits", 15); floorBar.Children.Add(circuits);
        toolbar.Children.Add(floorBar); estate.Children.Add(toolbar);
        var roomArea = new StackPanel();
        var roomHeading = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };
        var filterBar = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (id, caption) in new[] { ("all", "All"), ("open", "Operating"), ("locked", "Unopened") })
        {
            Button button = Command(caption, "EmpireFilter_" + id, () => { ClearPreview(); filter = id; selected = ""; Refresh(); });
            button.FontSize = 10; button.Padding = new Thickness(7, 2, 7, 2); button.MinHeight = 23;
            button.Margin = new Thickness(4, 0, 0, 0); filters[id] = button; filterBar.Children.Add(button);
        }
        DockPanel.SetDock(filterBar, Dock.Right); roomHeading.Children.Add(filterBar);
        estateStatus.VerticalAlignment = VerticalAlignment.Center; roomHeading.Children.Add(estateStatus); roomArea.Children.Add(roomHeading);
        for (int i = 0; i < EmpireCatalog.Departments.Count; i++)
        {
            var card = new EmpireRoomCard(EmpireCatalog.Departments[i], i, id =>
            {
                ClearPreview(); selected = selected == id ? "" : id; Refresh(); AnimateFeedback(intelligence, .55);
            });
            cards.Add(card); rooms.Children.Add(card);
        }
        roomArea.Children.Add(rooms); roomArea.Children.Add(emptyRooms);
        roomScroll.Content = roomArea; roomScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        roomScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        roomScroll.SizeChanged += (_, _) => ResizeRooms();
        roomScroll.ScrollChanged += (_, e) => { if (e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0) ResizeRooms(); };
        Grid.SetRow(roomScroll, 1); estate.Children.Add(roomScroll);
        Border information = BuildIntelligencePanel(); Grid.SetRow(information, 2); estate.Children.Add(information);
        overview.Children.Add(estate);
        Border inspector = BuildRoomInspector(); Grid.SetColumn(inspector, 1); overview.Children.Add(inspector);
        workspace.Children.Add(overview); Grid.SetRow(workspace, 1); body.Children.Add(workspace);
        Grid.SetRow(body, 2); root.Children.Add(body);
        notice.Name = "EmpireNotice"; notice.FontSize = 11;
        var status = new Border { Child = notice, Background = Ink("#132B20"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 8, 0, 0) };
        Grid.SetRow(status, 3); root.Children.Add(status); Content = root;
        SizeChanged += (_, _) =>
        {
            inspectorColumn.Width = new GridLength(ActualWidth < 1150 ? 245 : 280);
            roomTitle.FontSize = ActualHeight < 800 ? 21 : 25;
            ResizeRooms();
        };
        KeyboardNavigation.SetTabNavigation(workspace, KeyboardNavigationMode.Cycle);
        OpenSection("Overview");

        void Metric(string caption, string icon, UIElement value, UIElement detail, int index)
        {
            var content = new StackPanel { Margin = new Thickness(13, 8, 13, 8) };
            content.Children.Add(IconCaption(icon, caption, 15));
            if (value is FrameworkElement element) element.Margin = new Thickness(0, 3, 0, 1);
            content.Children.Add(value); content.Children.Add(detail);
            Border panel = Surface(content); panel.Margin = new Thickness(index == 0 ? 0 : 10, 0, 0, 0); balances.Children.Add(panel);
        }
    }

    private Border BuildRoomInspector()
    {
        var content = inspectorContent;
        content.Children.Add(Label("SELECTED ROOM / ACTIONS", 9, "#C4AE83"));
        roomTitle.FontFamily = new FontFamily("Georgia"); roomTitle.Margin = new Thickness(0, 8, 0, 6); content.Children.Add(roomTitle);
        roomStats.Margin = new Thickness(0, 6, 0, 8); content.Children.Add(roomStats);
        content.Children.Add(roomPreview);
        content.Children.Add(Label("INVEST IN THIS ROOM", 9, "#C4AE83", new Thickness(0, 9, 0, 6)));
        var amounts = new UniformGrid { Columns = 6 };
        foreach (int count in BulkChoices)
        {
            Button button = Command("×" + count, "EmpireBulk" + count + "Button", () => { ClearPreview(); bulk = count; Refresh(); });
            button.FontSize = 10; button.Padding = new Thickness(2, 5, 2, 5); button.Margin = new Thickness(0, 0, 3, 0);
            button.MinWidth = 0; bulkButtons[count] = button; amounts.Children.Add(button);
            BindPreview(button, () => service.PreviewUpgrade(selected, count), () => "Upgrade ×" + count);
        }
        content.Children.Add(amounts); buyLabel.TextAlignment = TextAlignment.Center; buy.Content = buyLabel;
        buy.Margin = new Thickness(0, 7, 0, 8); buy.Background = Ink("#305449"); buy.BorderBrush = Ink("#BFA26E");
        buy.Click += (_, _) =>
        {
            ClearPreview(); int before = service.Level(selected);
            if (upgrade(selected, bulk))
            {
                bool crossed = service.Level(selected) / 10 > before / 10;
                SetNotice(crossed ? $"Milestone reached · Level {service.Level(selected)} · production increased."
                    : before == 0 ? "Room opened. Your new department is earning." : $"Upgrade saved · +{service.Level(selected) - before} room levels.");
                Refresh(); AnimateFeedback(production, .45); AnimateFeedback(buy, .6); AnimateFeedback(intelligence, .65);
            }
            else Refresh();
        };
        BindPreview(buy, () => service.PreviewUpgrade(selected, bulk), () => "Upgrade ×" + bulk);
        content.Children.Add(buy); content.Children.Add(Label("NEXT PRODUCTION MILESTONE", 9, "#C7B18A"));
        milestoneTrack.Children.Add(milestoneFill); milestoneTrack.SizeChanged += (_, _) => RefreshMilestone();
        content.Children.Add(milestoneTrack); content.Children.Add(milestone);
        var grid = new Grid(); grid.Children.Add(inspectorEmpty);
        grid.Children.Add(new ScrollViewer { Name = "EmpireInspectorScroll", Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        Border panel = Surface(grid); panel.BorderBrush = Ink("#5E6244"); return panel;
    }

    private static string GroupRoute(string section) => section switch
    {
        "Development" or "Projects" or "Properties" or "Research" => "Development",
        "Operations" or "Automation" or "Managers" => "Managers",
        "Opportunities" or "Contracts" or "Events" => "Events", _ => "Overview",
    };

    private void OpenSection(string section)
    {
        ClearPreview();
        if (managementView is not null) workspace.Children.Remove(managementView);
        managementView = null;
        overview.Visibility = section == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        if (section != "Overview")
        {
            managementView = new EmpireManagementView(service, change => manage(change), () => OpenSection("Overview"), section, true,
                (roomId, action, caption) => ShowPreview(service.Preview(roomId, action), caption), ClearPreview);
            workspace.Children.Add(managementView);
        }
        foreach (var (key, button) in routes)
        {
            bool active = key == GroupRoute(section);
            button.Background = Ink(active ? "#294C38" : "#12291F");
            button.BorderBrush = Ink(active ? "#BAA16D" : "#344F40");
            button.Foreground = Ink(active ? "#F2E0BA" : "#AFC6B8");
            AutomationProperties.SetHelpText(button, active ? "Current section" : "Open " + key);
        }
        AnimateFeedback(workspace, .55);
        if (IsLoaded) (managementView as UIElement ?? routes["Overview"]).MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        RefreshCommandCenter();
    }

    internal void StopWorkspaceAnimation()
    {
        foreach (UIElement element in new UIElement[] { workspace, intelligence, production, productionPreview, budgetPreview, roomPreview, buy })
            element.BeginAnimation(OpacityProperty, null);
        credits.SetAmount(service.Credits, false); production.SetAmount(service.TotalCreditsPerSecond, false);
        StopIntelligenceAnimations();
    }

    private void RefreshCommandCenter()
    {
        if (!animations()) StopWorkspaceAnimation();
        legacySummary.Text = $"PRESTIGE {service.Prestige}   /   {service.LegacyPointsAvailable} LP   /   REP {service.Reputation:N0}";
        prestigeTitle.ToolTip = prestigeRequirement.Text + "\n" + prestigeBonus.Text;
        prestigeButton.ToolTip = prestigeRequirement.Text + "\nPrestige resets this property's room levels and credits. Research, projects, managers, contracts and other properties remain.";
        emptyRooms.Visibility = cards.Any(c => c.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        inspectorContent.Visibility = selected.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        inspectorEmpty.Visibility = selected.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshIntelligence();
        RefreshPreview();
    }

    private Button Command(string text, string name, Action action)
    {
        var button = new Button { Name = name, Content = text, FontSize = 11, Padding = new Thickness(9, 6, 9, 6), MinHeight = 28,
            Background = Ink("#19382A"), BorderBrush = Ink("#3F604C"), Foreground = Ink("#D8E4DA"), Margin = new Thickness(0, 2, 0, 2) };
        button.Click += (_, _) => action();
        button.MouseEnter += (_, _) => AnimateFeedback(button, .72);
        return button;
    }

    internal static StackPanel IconCaption(string kind, string text, double size = 19)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(new EmpireIcon(kind, size) { Margin = new Thickness(0, 0, 8, 0) });
        var caption = Label(text, size <= 15 ? 9 : 11, "#C6CDB7"); caption.VerticalAlignment = VerticalAlignment.Center; content.Children.Add(caption);
        return content;
    }

    private void AnimateFeedback(UIElement element, double from)
    {
        element.BeginAnimation(OpacityProperty, null);
        if (IsLoaded && animations()) element.BeginAnimation(OpacityProperty,
            new DoubleAnimation(from, 1, TimeSpan.FromMilliseconds(160)) { FillBehavior = FillBehavior.Stop });
    }

    private static Border Surface(UIElement child) => new()
    {
        Background = new LinearGradientBrush(Color.FromRgb(22, 43, 32), Color.FromRgb(11, 28, 21), 110),
        BorderBrush = Ink("#354C3B"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Child = child,
        Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 5, Opacity = .23 },
    };
}
