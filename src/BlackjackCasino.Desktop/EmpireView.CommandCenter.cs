using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    private readonly Grid workspace = new();
    private readonly Grid overview = new() { Name = "EmpireOverview" };
    private readonly Dictionary<string, Button> routes = new(StringComparer.Ordinal);
    private readonly List<Action> commandUpdates = [];
    private readonly TextBlock legacySummary = Label("", 11, "#CBB791");
    private readonly UniformGrid commandGrid = new() { Name = "EmpireCommandGrid", Columns = 4, Rows = 1 };
    private readonly ColumnDefinition inspectorColumn = new() { Width = new GridLength(290) };
    private readonly TextBlock emptyRooms = Label("No rooms match this filter. Choose All rooms to see this floor.", 12, "#AFC5C6");
    private readonly List<TextBlock> commandDetails = [];

    private void BuildCommandCenter(Action back, string backLabel)
    {
        Background = new RadialGradientBrush(Color.FromRgb(26, 44, 55), Color.FromRgb(9, 15, 23))
        { Center = new Point(.25, .1), GradientOrigin = new Point(.25, .1), RadiusX = 1.2, RadiusY = 1.2 };
        root.Margin = new Thickness(22, 14, 22, 12);
        foreach (GridLength height in new[] { GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = height });

        // Keep the header a direct child: the shared statistics presenter attaches here.
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
        Button exit = Command(backLabel, "EmpireBackButton", back);
        exit.VerticalAlignment = VerticalAlignment.Center;
        DockPanel.SetDock(exit, Dock.Right); header.Children.Add(exit);
        legacySummary.VerticalAlignment = VerticalAlignment.Center;
        legacySummary.Margin = new Thickness(12, 0, 18, 0);
        DockPanel.SetDock(legacySummary, Dock.Right); header.Children.Add(legacySummary);
        var title = new StackPanel();
        title.Children.Add(Label("PRIVATE ESTATES  /  THE EMPIRE COLLECTION", 9, "#C4A978"));
        title.Children.Add(new TextBlock { Text = "Casino Empire", FontFamily = new FontFamily("Georgia"), FontSize = 30, Foreground = Ink("#F7E9CE") });
        header.Children.Add(title); root.Children.Add(header);

        var balances = new UniformGrid { Columns = 3, Margin = new Thickness(0, 0, 0, 12) };
        Metric("01  /  CONSTRUCTION BUDGET", credits, Label("Selected estate · invest in rooms and projects", 10, "#93ABB6"), 0);
        Metric("02  /  PORTFOLIO PRODUCTION", production, offlineDetail, 1);
        var payout = new DockPanel(); collect.VerticalAlignment = VerticalAlignment.Center;
        collect.Click += (_, _) => { claim(); Refresh(); }; DockPanel.SetDock(collect, Dock.Right);
        payout.Children.Add(collect); payout.Children.Add(dividend);
        Metric("03  /  YOUR OWNER DIVIDEND", payout, cashRate, 2);
        Grid.SetRow(balances, 1); root.Children.Add(balances);

        var body = new Grid(); body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); body.RowDefinitions.Add(new RowDefinition());
        var navigation = new UniformGrid { Columns = 9, Margin = new Thickness(0, 0, 0, 12) };
        foreach (var item in new[] { ("Overview", "Estate", "◈"), ("Research", "Research", "✦"), ("Projects", "Projects", "▥"),
            ("Managers", "Team", "♟"), ("Automation", "Automation", "⟳"), ("Synergies", "Synergies", "◇"),
            ("Events", "Events", "♜"), ("Contracts", "Contracts", "▤"), ("Properties", "Properties", "⌂") })
        {
            string section = item.Item1;
            Button tab = Command(item.Item3 + "  " + item.Item2, section == "Overview" ? "EmpireOverviewButton" : "EmpireTab_" + section, () => OpenSection(section));
            tab.Padding = new Thickness(6, 9, 6, 9); tab.FontSize = 11; tab.Margin = new Thickness(0, 0, 5, 0);
            AutomationProperties.SetName(tab, item.Item2);
            navigation.Children.Add(tab); routes[section] = tab;
        }
        body.Children.Add(navigation);
        overview.ColumnDefinitions.Add(new ColumnDefinition()); overview.ColumnDefinitions.Add(inspectorColumn);
        var estate = new Grid { Margin = new Thickness(0, 0, 14, 0) };
        estate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        estate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        estate.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        estate.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 9) };
        propertyPicker.Width = 205; propertyPicker.MinHeight = 32; propertyPicker.Margin = new Thickness(0, 0, 12, 0);
        AutomationProperties.SetName(propertyPicker, "Active estate"); toolbar.Children.Add(propertyPicker);
        propertyPicker.SelectionChanged += (_, _) =>
        {
            if (selectingProperty || propertyPicker.SelectedValue is not string id || id == service.ActivePropertyId) return;
            manage(s => s.SelectProperty(id, DateTimeOffset.UtcNow)); selectedFloor = 1; selected = "slots"; Refresh();
        };
        var floorBar = new WrapPanel();
        foreach (EmpireFloorDefinition floor in EmpireFloorCatalog.All.OrderBy(f => f.SortOrder))
        {
            int id = floor.Id;
            Button button = Command(floor.DisplayName, "EmpireFloor" + id + "Button", () =>
            {
                if (!service.FloorUnlocked(id)) return;
                selectedFloor = id; selected = EmpireCatalog.Floor(id).First().Id; filter = "all"; Refresh();
            });
            button.FontSize = 10; button.Padding = new Thickness(9, 6, 9, 6); button.Margin = new Thickness(0, 0, 4, 0);
            floors[id] = button; floorBar.Children.Add(button);
        }
        toolbar.Children.Add(floorBar); estate.Children.Add(toolbar);

        var roomArea = new StackPanel();
        var roomHeading = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };
        var filterBar = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var item in new[] { ("all", "All"), ("open", "Operating"), ("locked", "Unopened") })
        {
            string id = item.Item1;
            Button button = Command(item.Item2, "EmpireFilter_" + id, () => { filter = id; Refresh(); });
            button.FontSize = 10; button.Padding = new Thickness(7, 3, 7, 3); button.MinHeight = 24;
            button.Margin = new Thickness(4, 0, 0, 0); filters[id] = button; filterBar.Children.Add(button);
        }
        DockPanel.SetDock(filterBar, Dock.Right); roomHeading.Children.Add(filterBar);
        estateStatus.VerticalAlignment = VerticalAlignment.Center; roomHeading.Children.Add(estateStatus); roomArea.Children.Add(roomHeading);
        for (int i = 0; i < EmpireCatalog.Departments.Count; i++)
        {
            var card = new EmpireRoomCard(EmpireCatalog.Departments[i], i, id => { selected = id; Refresh(); });
            cards.Add(card); rooms.Children.Add(card);
        }
        roomArea.Children.Add(rooms); roomArea.Children.Add(emptyRooms);
        roomScroll.Content = roomArea; roomScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        roomScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        roomScroll.SizeChanged += (_, _) => ResizeRooms();
        Grid.SetRow(roomScroll, 1); estate.Children.Add(roomScroll);

        BuildCommandCards();
        // The cards use the space that used to be empty below the room gallery.
        Grid.SetRow(commandGrid, 2); estate.Children.Add(commandGrid);
        var ledger = new UniformGrid { Columns = 4, Margin = new Thickness(0, 12, 0, 0) };
        Ledger(lifetime, "LIFETIME CREDITS"); Ledger(cashClaimed, "CASH PAID"); Ledger(openRooms, "ROOMS OPERATING"); Ledger(levels, "TOTAL LEVELS");
        Grid.SetRow(ledger, 3); estate.Children.Add(ledger); overview.Children.Add(estate);
        Border inspector = BuildRoomInspector(); Grid.SetColumn(inspector, 1); overview.Children.Add(inspector);
        workspace.Children.Add(overview); Grid.SetRow(workspace, 1); body.Children.Add(workspace);
        Grid.SetRow(body, 2); root.Children.Add(body);
        notice.Name = "EmpireNotice"; notice.FontSize = 11;
        var status = new Border { Child = notice, Background = Ink("#152B36"), CornerRadius = new CornerRadius(7), Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 10, 0, 0) };
        Grid.SetRow(status, 3); root.Children.Add(status); Content = root;
        SizeChanged += (_, _) =>
        {
            inspectorColumn.Width = new GridLength(ActualWidth < 1150 ? 260 : 290);
            commandGrid.Columns = 4; commandGrid.Rows = 1;
            foreach (TextBlock detail in commandDetails) detail.Visibility = ActualHeight < 800 ? Visibility.Collapsed : Visibility.Visible;
            ResizeRooms();
        };
        KeyboardNavigation.SetTabNavigation(workspace, KeyboardNavigationMode.Cycle);
        OpenSection("Overview");

        void Metric(string caption, UIElement value, UIElement detail, int index)
        {
            var content = new StackPanel { Margin = new Thickness(16, 10, 16, 10) };
            content.Children.Add(Label(caption, 9, "#C7AE80"));
            if (value is FrameworkElement element) element.Margin = new Thickness(0, 3, 0, 3);
            content.Children.Add(value); content.Children.Add(detail);
            Border panel = Surface(content); panel.Margin = new Thickness(index == 0 ? 0 : 10, 0, 0, 0); balances.Children.Add(panel);
        }
        void Ledger(TextBlock value, string caption)
        {
            value.FontSize = 16; var panel = new StackPanel { Margin = new Thickness(6, 0, 8, 0) };
            panel.Children.Add(Label(caption, 8, "#8CA4B2")); panel.Children.Add(value); ledger.Children.Add(panel);
        }
    }

    private Border BuildRoomInspector()
    {
        var content = new StackPanel { Margin = new Thickness(17) };
        content.Children.Add(Label("SELECTED DEPARTMENT", 9, "#D3B77F"));
        roomTitle.FontFamily = new FontFamily("Georgia"); roomTitle.FontSize = 25; roomTitle.Margin = new Thickness(0, 8, 0, 6);
        content.Children.Add(roomTitle); roomDescription.FontSize = 11; content.Children.Add(roomDescription);
        roomStats.Margin = new Thickness(0, 12, 0, 5); content.Children.Add(roomStats);
        roomBoosts.FontSize = 10; content.Children.Add(roomBoosts);
        content.Children.Add(Label("INVEST IN THIS ROOM", 9, "#D3B77F", new Thickness(0, 13, 0, 6)));
        var amounts = new UniformGrid { Columns = 6 };
        foreach (int count in BulkChoices)
        {
            int value = count;
            Button button = Command("×" + count, "EmpireBulk" + count + "Button", () => { bulk = value; Refresh(); });
            button.FontSize = 10; button.Padding = new Thickness(2, 5, 2, 5); button.Margin = new Thickness(0, 0, 3, 0);
            button.MinWidth = 0; bulkButtons[count] = button; amounts.Children.Add(button);
        }
        content.Children.Add(amounts); buyLabel.TextAlignment = TextAlignment.Center; buy.Content = buyLabel;
        buy.Margin = new Thickness(0, 10, 0, 12); buy.Background = Ink("#305449"); buy.BorderBrush = Ink("#BFA26E");
        buy.Click += (_, _) =>
        {
            int before = service.Level(selected);
            if (upgrade(selected, bulk)) SetNotice(before == 0 ? "Room opened. Your new department is earning." : $"Upgrade saved · +{service.Level(selected) - before} room levels.");
            Refresh();
        };
        content.Children.Add(buy); content.Children.Add(Label("NEXT PRODUCTION MILESTONE", 9, "#C7B18A"));
        milestoneTrack.Children.Add(milestoneFill); milestoneTrack.SizeChanged += (_, _) => RefreshMilestone();
        content.Children.Add(milestoneTrack); content.Children.Add(milestone);
        content.Children.Add(Command("View this room’s synergies  →", "EmpireRoomSynergies", () => OpenSection("Synergies")));
        content.Children.Add(new Border { Height = 1, Background = Ink("#3C4E58"), Margin = new Thickness(0, 15, 0, 13) });
        content.Children.Add(Label("THE NEXT CHAPTER", 9, "#D3B77F"));
        prestigeTitle.FontFamily = new FontFamily("Georgia"); prestigeTitle.Margin = new Thickness(0, 6, 0, 5); content.Children.Add(prestigeTitle);
        prestigeBonus.FontSize = 11; content.Children.Add(prestigeBonus);
        prestigeRequirement.Margin = new Thickness(0, 7, 0, 0); content.Children.Add(prestigeRequirement);
        prestigeButton.Click += (_, _) => PrestigeClicked(); content.Children.Add(prestigeButton);
        prestigeButton.ToolTip = "Prestige resets this property's credits and room levels. Research, projects, managers, contracts and other properties remain. Each prestige earns a Legacy Point.";
        Border panel = Surface(new ScrollViewer { Name = "EmpireInspectorScroll", Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        panel.BorderBrush = Ink("#685F4B"); return panel;
    }

    private void BuildCommandCards()
    {
        var research = CommandCard("✦", "LEGACY LAB", "Research", "EmpireQuickResearch", "Research", out TextBlock researchState, out TextBlock researchDetail);
        research.Children.Add(Command("Open research tree  →", "EmpireResearchShortcut", () => OpenSection("Research")));
        research.Children.Add(Command("Synergy circuits  →", "EmpireSynergyShortcut", () => OpenSection("Synergies")));
        commandUpdates.Add(() =>
        {
            researchState.Text = $"{service.LegacyPointsAvailable} Legacy Points";
            researchDetail.Text = $"{service.LegacyPointsSpent} points invested · {service.Synergies.Count(s => s.Tier > 0)} active circuits\nPermanent upgrades across your portfolio.";
        });

        var projects = CommandCard("▥", "DEVELOPMENT", "Projects", "EmpireQuickProjects", "Projects", out TextBlock projectState, out TextBlock projectDetail);
        Button build = Command("", "EmpireQuickBuild", () =>
        {
            EmpireProjectDefinition? next = NextProject();
            if (next is not null && service.CanBuildProject(next.Id)) RunCommand(s => s.BuildProject(next.Id, DateTimeOffset.UtcNow));
            else OpenSection("Projects");
        }); projects.Children.Add(build);
        projects.Children.Add(Command("Property portfolio  →", "EmpirePropertyShortcut", () => OpenSection("Properties")));
        commandUpdates.Add(() =>
        {
            EmpireProjectDefinition? next = NextProject(); projectState.Text = next?.Name ?? "Estate complete";
            projectDetail.Text = next is null ? "All permanent projects completed." : $"{Number(next.Cost)} cr · {next.Description}";
            build.Content = next is not null && service.CanBuildProject(next.Id) ? "Build project  ↗" : "Plan construction  →";
            build.ToolTip = next?.Requirement.Description;
        });

        var team = CommandCard("♟", "EXECUTIVE OFFICE", "Managers", "EmpireQuickManagers", "Team & automation", out TextBlock teamState, out TextBlock teamDetail);
        team.Children.Add(Command("Assign specialists  →", "EmpireManagerShortcut", () => OpenSection("Managers")));
        team.Children.Add(Command("Configure automation  →", "EmpireAutomationShortcut", () => OpenSection("Automation")));
        commandUpdates.Add(() =>
        {
            teamState.Text = $"{service.ManagerAssignments.Count} / {service.ManagerSlots} specialists";
            teamDetail.Text = !service.AutomationUnlocked ? "Automation locked · requires Executive Offices + research." : service.Automation.Enabled
                ? $"Automation ON · target level {service.Automation.TargetLevel}\nReserve {Number(service.Automation.Reserve)} cr" : "Automation ready · currently paused.";
        });

        var activity = CommandCard("♜", "LIVE OPPORTUNITIES", "Events", "EmpireQuickEvents", "Events & contracts", out TextBlock activityState, out TextBlock activityDetail);
        Button events = Command("Browse casino events  →", "EmpireEventShortcut", () => OpenSection("Events")); activity.Children.Add(events);
        Button contracts = Command("", "EmpireContractShortcut", () =>
        {
            if (service.CanClaimContract) RunCommand(s => s.ClaimContract(DateTimeOffset.UtcNow)); else OpenSection("Contracts");
        }); activity.Children.Add(contracts);
        commandUpdates.Add(() =>
        {
            activityState.Text = service.ActiveEvent is { } ev ? EmpireEventCatalog.Find(ev.Id)!.Name + $" · {Math.Max(0, (ev.EndsAt - DateTimeOffset.UtcNow).TotalMinutes):0}m" : "Your next occasion";
            activityDetail.Text = service.ActiveContract is { } contract
                ? EmpireContractCatalog.Find(contract.Id)!.Name + $"\n{Number(service.ContractProgress)} / {Number(EmpireContractCatalog.Find(contract.Id)!.Goal)}"
                : "Start an event or choose a contract.\nNo deadlines on contract progress.";
            contracts.Content = service.CanClaimContract ? "Claim contract reward  ✦" : "Open contract board  →";
            contracts.BorderBrush = Ink(service.CanClaimContract ? "#D6BD80" : "#41624F");
        });
    }

    private EmpireProjectDefinition? NextProject() => EmpireProjectCatalog.All.Where(p => !service.Projects.Contains(p.Id))
        .OrderByDescending(p => service.CanBuildProject(p.Id)).ThenBy(p => p.Cost).FirstOrDefault();

    private StackPanel CommandCard(string icon, string caption, string route, string name, string accessibleName, out TextBlock state, out TextBlock detail)
    {
        var content = new StackPanel { Margin = new Thickness(13) };
        Button heading = Command(icon + "  " + caption + "  ↗", name, () => OpenSection(route));
        heading.FontSize = 10; heading.Padding = new Thickness(0, 2, 0, 8); heading.HorizontalContentAlignment = HorizontalAlignment.Left;
        heading.Background = Brushes.Transparent; heading.BorderThickness = new Thickness(0); heading.Foreground = Ink("#DDC08A");
        AutomationProperties.SetName(heading, accessibleName); content.Children.Add(heading);
        state = Label("", 18, "#F2E6CD"); state.FontFamily = new FontFamily("Georgia"); state.Margin = new Thickness(0, 7, 0, 7); content.Children.Add(state);
        detail = Label("", 11, "#9FB7C1"); commandDetails.Add(detail); detail.MinHeight = 50; detail.Margin = new Thickness(0, 0, 0, 12); content.Children.Add(detail);
        Border panel = Surface(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        panel.Margin = new Thickness(0, 4, 9, 0); commandGrid.Children.Add(panel); return content;
    }

    private void RunCommand(Func<EmpireService, EmpireActionResult> change)
    {
        EmpireActionResult result = manage(change); SetNotice(result.Message); Refresh();
    }

    private void OpenSection(string section)
    {
        if (managementView is not null) workspace.Children.Remove(managementView);
        managementView = null;
        overview.Visibility = section == "Overview" ? Visibility.Visible : Visibility.Collapsed;
        if (section != "Overview")
        {
            managementView = new EmpireManagementView(service, change => manage(change), () => OpenSection("Overview"), section, true);
            workspace.Children.Add(managementView);
        }
        foreach (var (key, button) in routes)
        {
            button.Background = Ink(key == section ? "#304A55" : "#14242E");
            button.BorderBrush = Ink(key == section ? "#D1B57C" : "#344955");
            button.Foreground = Ink(key == section ? "#FFE8B8" : "#ADC2C9");
            AutomationProperties.SetHelpText(button, key == section ? "Current section" : "Open " + key);
        }
        if (IsLoaded && animations()) workspace.BeginAnimation(OpacityProperty, new DoubleAnimation(.45, 1, TimeSpan.FromMilliseconds(180)) { FillBehavior = FillBehavior.Stop });
        if (IsLoaded) (managementView as UIElement ?? routes["Overview"]).MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        RefreshCommandCenter();
    }

    private void RefreshCommandCenter()
    {
        if (!animations()) workspace.BeginAnimation(OpacityProperty, null);
        legacySummary.Text = $"PRESTIGE {service.Prestige}   /   {service.LegacyPointsAvailable} LP   /   REP {service.Reputation:N0}";
        foreach (Action update in commandUpdates) update();
        emptyRooms.Visibility = cards.Any(c => c.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
    }

    private Button Command(string text, string name, Action action)
    {
        var button = new Button { Name = name, Content = text, FontSize = 11, Padding = new Thickness(9, 6, 9, 6), MinHeight = 28,
            Background = Ink("#192F3B"), BorderBrush = Ink("#41606A"), Foreground = Ink("#D8E4DF"), Margin = new Thickness(0, 2, 0, 2) };
        button.Click += (_, _) => action(); return button;
    }

    private static Border Surface(UIElement child) => new()
    {
        Background = new LinearGradientBrush(Color.FromRgb(27, 44, 56), Color.FromRgb(15, 27, 36), 110),
        BorderBrush = Ink("#3A505D"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Child = child,
        Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 5, Opacity = .23 },
    };
}
