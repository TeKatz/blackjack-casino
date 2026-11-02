using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    private const string PreviewColor = "#92CFE0";
    private readonly Grid intelligence = new() { Name = "EmpireRoomIntelligence" };
    private readonly TextBlock intelligenceTitle = Label("ESTATE OVERVIEW", 15, "#E2CFAB");
    private readonly StackPanel estateOverview = new() { Margin = new Thickness(18, 8, 18, 12) };
    private readonly TextBlock prestigeActionCaption = Label("", 15, "#E1CA9E");
    private readonly TextBlock estateOverviewText = Label("", 24, "#D6E2CF");
    private readonly TextBlock estateHint = Label("", 18, "#ACCBB5", new Thickness(0, 10, 0, 0));
    private readonly Grid intelligenceColumns = new();
    private readonly TextBlock growthInfo = Label("", 16, "#BED0BF");
    private readonly TextBlock growthPreview = Label("", 11, PreviewColor);
    private readonly TextBlock operationsInfo = Label("", 16, "#BED0BF");
    private readonly TextBlock automationInfo = Label("", 14, "#A9C1AF", new Thickness(0, 5, 0, 5));
    private readonly TextBlock performanceInfo = Label("", 16, "#BED0BF");
    private readonly TextBlock performanceFinal = Label("", 20, "#CBE2CE", new Thickness(0, 5, 0, 0));
    private readonly TextBlock performancePreview = Label("", 11, PreviewColor);
    private readonly StackPanel connections = new();
    private readonly List<Action<EmpireActionPreview?>> connectionUpdates = [];
    private readonly List<ProgressBar> intelligenceProgress = [];
    private readonly ComboBox managerChoice = new() { Name = "EmpireRoomManagerChoice", DisplayMemberPath = "Name", SelectedValuePath = "Id", MinHeight = 26 };
    private Button? assignRoomManager;
    private Button? toggleRoomAutomation;
    private string intelligenceKey = "";
    private EmpireActionPreview? currentPreview;
    private readonly TextBlock budgetPreview = Label("Hover an action to preview its cost", 10, "#91A99C");
    private readonly TextBlock productionPreview = Label("All owned properties · live production", 10, "#91A99C");
    private readonly TextBlock roomPreview = Label("", 11, PreviewColor);
    private Button? previewOwner;
    private Func<EmpireActionPreview>? previewFactory;
    private string previewCaption = "";

    private Border BuildIntelligencePanel()
    {
        roomPreview.MinHeight = 30;
        intelligence.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        intelligence.RowDefinitions.Add(new RowDefinition());
        var heading = new DockPanel { Margin = new Thickness(16, 9, 12, 4) };
        Button reset = Command("Estate overview", "EmpireClearRoomSelection", () => { ClearPreview(); selected = ""; Refresh(); });
        reset.FontSize = 10; reset.MinHeight = 24; reset.Padding = new Thickness(6, 2, 6, 2);
        DockPanel.SetDock(reset, Dock.Right); heading.Children.Add(reset); heading.Children.Add(intelligenceTitle); intelligence.Children.Add(heading);
        var overviewLayout = new Grid(); overviewLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) }); overviewLayout.ColumnDefinitions.Add(new ColumnDefinition());
        var estateFacts = new StackPanel { Margin = new Thickness(0, 0, 18, 0) }; estateFacts.Children.Add(estateOverviewText); estateFacts.Children.Add(estateHint);
        estateFacts.Children.Add(Label("Select a room above to explore growth, operations, connections and performance.", 11, "#8CA996", new Thickness(0, 12, 0, 0)));
        overviewLayout.Children.Add(estateFacts);
        var nextChapter = new StackPanel(); nextChapter.Children.Add(IconCaption("Prestige", "THE NEXT CHAPTER", 16));
        prestigeTitle.FontFamily = new System.Windows.Media.FontFamily("Georgia"); prestigeTitle.Margin = new Thickness(0, 8, 0, 5); nextChapter.Children.Add(prestigeTitle);
        prestigeRequirement.FontSize = 15;
        prestigeTitle.FontSize = 30; nextChapter.Children.Add(prestigeRequirement);
        var prestigeContent = new StackPanel { Orientation = Orientation.Horizontal }; prestigeContent.Children.Add(new EmpireIcon("Prestige", 18) { Margin = new Thickness(0, 0, 8, 0) }); prestigeContent.Children.Add(prestigeActionCaption);
        prestigeButton.Content = prestigeContent; prestigeButton.Click += (_, _) => { ClearPreview(); PrestigeClicked(); };
        ToolTipService.SetShowOnDisabled(prestigeButton, true); nextChapter.Children.Add(prestigeButton);
        Grid.SetColumn(nextChapter, 1); overviewLayout.Children.Add(nextChapter); estateOverview.Children.Add(overviewLayout);
        var overviewFit = FitIntelligence(estateOverview);
        Grid.SetRow(overviewFit, 1); intelligence.Children.Add(overviewFit);
        var growth = new StackPanel(); growth.Children.Add(growthInfo); growth.Children.Add(growthPreview);
        var operations = new StackPanel(); operations.Children.Add(operationsInfo); operations.Children.Add(automationInfo);
        var specialistControls = new StackPanel();
        managerChoice.ItemsSource = EmpireManagerCatalog.All; managerChoice.SelectedIndex = 0;
        managerChoice.SelectionChanged += (_, _) => ClearPreview(); specialistControls.Children.Add(managerChoice);
        assignRoomManager = Command("Assign specialist", "EmpireRoomManagerAssign", () =>
        {
            if (managerChoice.SelectedValue is string id) RunRoomAction(s => s.AssignManager(id, selected, DateTimeOffset.UtcNow));
        });
        BindPreview(assignRoomManager, () => service.Preview(selected, (s, at) => s.AssignManager((string)managerChoice.SelectedValue, selected, at)), () => "Assign specialist");
        specialistControls.Children.Add(assignRoomManager);
        operations.Children.Add(new Expander { Header = "Change specialist", Content = specialistControls, Foreground = Ink("#B7CCBB"), FontSize = 10, Margin = new Thickness(0, 3, 0, 3) });
        toggleRoomAutomation = Command("", "EmpireRoomAutomationToggle", () => RunRoomAction(s => s.ConfigureAutomation(s.Automation with { Enabled = !s.Automation.Enabled }, DateTimeOffset.UtcNow)));
        BindPreview(toggleRoomAutomation, () => service.Preview(selected, (s, at) => s.ConfigureAutomation(s.Automation with { Enabled = !s.Automation.Enabled }, at)), () => "Automation policy");
        operations.Children.Add(toggleRoomAutomation);
        var opsLinks = new WrapPanel(); opsLinks.Children.Add(Command("Team →", "EmpireRoomTeam", () => OpenSection("Managers")));
        opsLinks.Children.Add(Command("Research →", "EmpireRoomResearch", () => OpenSection("Research"))); operations.Children.Add(opsLinks);
        var performance = new StackPanel(); performance.Children.Add(performanceInfo); performance.Children.Add(performanceFinal); performance.Children.Add(performancePreview);
        AddColumn("Growth", growth, 0, .9); AddColumn("Operations", operations, 1, 1.1);
        AddColumn("Synergies", connections, 2, 1.25); AddColumn("Performance", performance, 3, 1.05);
        Grid.SetRow(intelligenceColumns, 1); intelligence.Children.Add(intelligenceColumns);
        Border panel = Surface(intelligence); panel.Margin = new Thickness(0, 10, 0, 0); return panel;

        void AddColumn(string caption, UIElement content, int column, double width)
        {
            intelligenceColumns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) });
            var stack = new DockPanel { Margin = new Thickness(12, 6, 12, 12) };
            var title = IconCaption(caption, caption.ToUpperInvariant(), 20);
            if (title.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock label) label.FontSize = 15;
            title.Margin = new Thickness(0, 0, 0, 9);
            DockPanel.SetDock(title, Dock.Top); stack.Children.Add(title);
            stack.Children.Add(FitIntelligence((FrameworkElement)content));
            var border = new Border { Child = stack, BorderThickness = new Thickness(column == 0 ? 0 : 1, 0, 0, 0), BorderBrush = Ink("#304C3B") };
            Grid.SetColumn(border, column); intelligenceColumns.Children.Add(border);
        }
    }

    private static FrameworkElement FitIntelligence(FrameworkElement content)
    {
        var fit = new Viewbox { Child = content, Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly,
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Top };
        // Set a natural wrapping width; only excess height causes fitting on a small window.
        fit.SizeChanged += (_, _) =>
        {
            double width = Math.Max(140, fit.ActualWidth);
            if (Math.Abs(content.Width - width) > .5 || double.IsNaN(content.Width)) content.Width = width;
        };
        return fit;
    }

    private void RefreshIntelligence()
    {
        bool hasRoom = EmpireCatalog.Find(selected) is not null;
        estateOverview.Visibility = hasRoom ? Visibility.Collapsed : Visibility.Visible;
        intelligenceColumns.Visibility = hasRoom ? Visibility.Visible : Visibility.Collapsed;
        intelligenceTitle.Text = hasRoom ? "ROOM INTELLIGENCE / " + EmpireCatalog.Find(selected)!.Name.ToUpperInvariant() : "ESTATE OVERVIEW / " + service.ActiveProperty.Name.ToUpperInvariant();
        if (!hasRoom)
        {
            estateOverviewText.Text = $"{service.OpenRooms} rooms operating · {service.TotalLevels:N0} total levels\nNext prestige: {service.MainFloorLevels:N0} / {service.PrestigeMainFloorLevelRequirement:N0} Main Floor levels\n{service.PrestigeRequirementText()}";
            EmpireSynergyStatus? next = service.Synergies.Where(s => s.NextLevel is not null).OrderByDescending(s => s.MinimumLevel).FirstOrDefault();
            string hint = next is null ? "All synergy tiers complete." : CircuitLimiter(next);
            int available = EmpireResearchCatalog.All.Count(n => service.CanResearch(n.Id));
            estateHint.Text = hint + (available > 0 ? $"\n{available} research upgrades available in Development." : "\nDevelopment holds research, projects and the property portfolio.");
            return;
        }
        EmpireDepartment room = EmpireCatalog.Find(selected)!;
        int level = service.Level(selected), nextLevel = Math.Min(EmpireRules.MaximumLevel, (level / 10 + 1) * 10);
        EmpireUpgradeQuote nextQuote = service.QuoteUpgrade(selected, 1);
        growthInfo.Text = $"LEVEL {level} / {EmpireRules.MaximumLevel}\n" + (level == EmpireRules.MaximumLevel ? "All milestones complete." : $"Next ×2: LV {nextLevel}\n{nextLevel - level} levels remaining")
            + "\n\n" + (!nextQuote.Unlocked ? room.UnlockRequirement.Description : service.Credits < nextQuote.Cost ? "Next step: build the budget." : level == EmpireRules.MaximumLevel ? "Develop other departments." : "Next upgrade is affordable.")
            + $"\nConstruction discount: {service.ConstructionDiscount(selected):P0}";
        EmpireManagerDefinition[] managers = EmpireManagerCatalog.All.Where(m => service.ManagerAssignments.TryGetValue(m.Id, out string? assignment)
            && (assignment == selected || m.Effect == EmpireManagerEffect.TagProduction && (m.Tags & room.Tags) != 0
                || m.Effect == EmpireManagerEffect.Synergy && (m.Tags & room.Tags) != 0)).ToArray();
        operationsInfo.Text = managers.Length == 0 ? "No active specialist for this room." : string.Join("\n", managers.Select(m => m.Name + "\n" + m.Description));
        if (service.ManagerSlots == 0) operationsInfo.Text += "\nTeam locked · build Executive Offices.";
        EmpireAutomationState auto = service.Automation;
        bool included = auto.CheapestMilestone || auto.Priorities.Contains(selected, StringComparer.Ordinal);
        if (toggleRoomAutomation is not null) toggleRoomAutomation.Visibility = service.AutomationUnlocked ? Visibility.Visible : Visibility.Collapsed;
        automationInfo.Text = service.AutomationUnlocked
            ? $"AUTO / {(auto.Enabled ? "RUNNING" : "PAUSED")} · {(level >= auto.TargetLevel ? "Target reached" : !nextQuote.Unlocked ? "Room locked" : included ? "Room queued" : "Not in priority queue")}\n{auto.SpendPercent:P0} budget / 30 sec · reserve {Number(auto.Reserve)} cr\nTarget LV {auto.TargetLevel} · {(auto.CheapestMilestone ? "cheapest milestone" : "priority order")}\nBuys upgrades; no direct output multiplier."
            : $"AUTO / LOCKED\n{(service.Projects.Contains("executive-offices") ? "✓" : "✕")} Executive Offices\n{(service.ResearchRank("automation") > 0 ? "✓" : "✕")} Executive Automation research";
        if (assignRoomManager is not null)
        {
            string? managerId = managerChoice.SelectedValue as string;
            EmpireManagerDefinition? manager = managerId is null ? null : EmpireManagerCatalog.Find(managerId);
            assignRoomManager.IsEnabled = level > 0 && manager is not null && service.Prestige >= manager.RequiredPrestige && service.ManagerSlots > 0
                && (service.ManagerAssignments.ContainsKey(manager.Id) || service.ManagerAssignments.Count < service.ManagerSlots);
            assignRoomManager.ToolTip = manager?.Description + "\nRequires an operating room, Executive Offices, a manager slot and the required prestige.";
        }
        if (toggleRoomAutomation is not null)
        {
            toggleRoomAutomation.IsEnabled = service.AutomationUnlocked;
            toggleRoomAutomation.Content = auto.Enabled ? "Pause automation" : "Resume automation";
        }
        EmpireProductionBreakdown value = service.ProductionBreakdown(selected);
        performanceInfo.Text = $"Base + milestones  {Number(value.Base)}\nPrestige  ×{value.Prestige:0.##}\nAchievements  ×{value.Achievements:0.###}\n\nResearch / property  +{value.Strategy:P1}\nManagers  +{value.Managers:P1}\nSynergies  +{value.Synergies:P1}\nProjects  +{value.Projects:P1}\nEvents / contracts  +{value.Events + value.Contracts:P1}\nStrategic total  +{value.StrategicBonus:P1} (cap 500%)";
        performanceInfo.ToolTip = "(Base including level milestones) × Prestige × Achievements × (1 + capped strategic total). Final room output is capped at 100 Qi cr/sec. Automation buys upgrades and is not an additive production bonus.";
        performanceFinal.Text = "FINAL / " + Number(value.Final) + " cr/sec";
        string key = service.ActivePropertyId + "/" + selected;
        if (intelligenceKey != key)
        {
            intelligenceKey = key; BuildRoomConnections(room);
        }
        foreach (Action<EmpireActionPreview?> update in connectionUpdates) update(currentPreview);
    }

    private void BuildRoomConnections(EmpireDepartment room)
    {
        StopIntelligenceAnimations(); connectionUpdates.Clear(); intelligenceProgress.Clear(); connections.Children.Clear();
        foreach (EmpireSynergyDefinition definition in EmpireSynergyCatalog.All.Where(d => (room.Tags & d.Beneficiaries) != 0
            || room.Floor <= d.MaximumFloor && (room.Tags & d.Members) != 0))
        {
            var title = Label(definition.Name.ToUpperInvariant(), 14, "#D1BD94", new Thickness(0, 0, 0, 4)); connections.Children.Add(title);
            var status = Label("", 13, "#AECAB6"); connections.Children.Add(status);
            var progress = new ProgressBar { Minimum = 0, Maximum = 1, Height = 6, Foreground = Ink("#9EBB8C"), Background = Ink("#263E2F"),
                Margin = new Thickness(0, 5, 0, 6), BorderThickness = new Thickness(0) }; intelligenceProgress.Add(progress); connections.Children.Add(progress);
            EmpireDepartment[] members = EmpireCatalog.Departments.Where(r => r.Floor <= definition.MaximumFloor && (r.Tags & definition.Members) != 0).ToArray();
            var memberDetails = new StackPanel();
            var lines = members.Select(_ => Label("", 13, "#A9C1AE")).ToArray(); foreach (TextBlock line in lines) memberDetails.Children.Add(line);
            var limiter = Label("", 12, "#D4C292", new Thickness(0, 5, 0, 8)); memberDetails.Children.Add(limiter);
            bool futureFloor = members.Any(m => !service.FloorUnlocked(m.Floor));
            if (futureFloor)
            {
                connections.Children.Add(new Expander { Header = "Future circuit · floor unlock required", Content = memberDetails, Foreground = Ink("#9FB8A5"), FontSize = 10, Margin = new Thickness(0, 0, 0, 8) });
            }
            else connections.Children.Add(memberDetails);
            connectionUpdates.Add(preview =>
            {
                EmpireSynergyStatus actual = service.Synergies.Single(s => s.Definition.Id == definition.Id);
                EmpireSynergyStatus after = preview?.SynergiesAfter.Single(s => s.Definition.Id == definition.Id) ?? actual;
                int target = actual.NextLevel ?? definition.Levels[^1];
                status.Text = $"Tier {actual.Tier} · +{actual.Bonus:P1}" + (after.Tier != actual.Tier ? $" → tier {after.Tier} · +{after.Bonus:P1} PREVIEW" : "");
                status.Foreground = Ink(after.Tier != actual.Tier ? PreviewColor : "#AECAB6");
                double next = Math.Clamp((double)after.MinimumLevel / target, 0, 1);
                if (Math.Abs(progress.Value - next) > .001)
                {
                    double previous = progress.Value; progress.BeginAnimation(RangeBase.ValueProperty, null); progress.Value = next;
                    if (animations() && IsLoaded) progress.BeginAnimation(RangeBase.ValueProperty, new DoubleAnimation(previous, next, TimeSpan.FromMilliseconds(150)) { FillBehavior = FillBehavior.Stop });
                }
                for (int i = 0; i < members.Length; i++)
                {
                    EmpireDepartment member = members[i]; int lv = service.Level(member.Id);
                    int future = preview is not null && member.Id == selected ? preview.LevelAfter : lv;
                    bool limiting = lv < target && lv == actual.MinimumLevel;
                    lines[i].Text = $"{member.Name}: {lv} / {target}" + (future != lv ? $" → {future} PREVIEW" : lv >= target ? " ✓" : limiting ? " · LIMIT" : "");
                    lines[i].Foreground = Ink(future != lv ? PreviewColor : limiting && member.Id == selected ? "#E1C283" : "#A9C1AE");
                }
                limiter.Text = actual.NextLevel is null ? "All tiers active." : CircuitLimiter(actual);
                if (preview is not null && after.Tier > actual.Tier) limiter.Text += "\nNext tier activates on purchase.";
            });
        }
        if (connections.Children.Count == 0) connections.Children.Add(Label("No circuits affect this room yet.", 11, "#9EB5A4"));
    }

    private string CircuitLimiter(EmpireSynergyStatus status)
    {
        if (status.NextLevel is not int target) return "Circuit complete.";
        string names = string.Join(", ", EmpireCatalog.Departments.Where(r => r.Floor <= status.Definition.MaximumFloor
            && (r.Tags & status.Definition.Members) != 0 && service.Level(r.Id) == status.MinimumLevel).Select(r => r.Name));
        return $"{names} limits {status.Definition.Name}.\n{target - status.MinimumLevel} levels to the next tier for each limiting room.";
    }

    private void BindPreview(Button owner, Func<EmpireActionPreview> factory, Func<string> caption)
    {
        void Enter()
        {
            if (!owner.IsEnabled || selected.Length == 0) return;
            previewOwner = owner; previewFactory = factory; previewCaption = caption(); RefreshPreview();
        }
        owner.MouseEnter += (_, _) => Enter(); owner.GotKeyboardFocus += (_, _) => Enter();
        owner.MouseLeave += (_, _) => { if (previewOwner == owner) ClearPreview(); };
        owner.LostKeyboardFocus += (_, _) => { if (previewOwner == owner) ClearPreview(); };
        owner.IsEnabledChanged += (_, _) => { if (!owner.IsEnabled && previewOwner == owner) ClearPreview(); };
    }

    private void RefreshPreview()
    {
        if (previewFactory is not null)
        {
            if (previewOwner is not { IsEnabled: true }) { ClearPreview(); return; }
            ShowPreview(previewFactory(), previewCaption);
        }
    }

    private void ShowPreview(EmpireActionPreview preview, string caption)
    {
        if (!preview.Success) { ClearPreview(); return; }
        bool entering = currentPreview is null;
        currentPreview = preview;
        budgetPreview.Foreground = productionPreview.Foreground = Ink(PreviewColor);
        budgetPreview.Text = preview.BudgetBefore == preview.BudgetAfter ? "PREVIEW / no construction cost" : $"PREVIEW / −{Number(preview.BudgetBefore - preview.BudgetAfter)} cr";
        decimal delta = preview.PortfolioAfter - preview.PortfolioBefore;
        productionPreview.Text = $"PREVIEW / {(delta >= 0 ? "+" : "−")}{Number(Math.Abs(delta))} cr/sec";
        roomPreview.Text = $"PREVIEW / {caption}\nLV {preview.LevelBefore} → {preview.LevelAfter} · {Number(preview.RoomBefore.Final)} → {Number(preview.RoomAfter.Final)} cr/sec";
        growthPreview.Text = preview.LevelAfter != preview.LevelBefore ? $"PREVIEW / LV {preview.LevelBefore} → {preview.LevelAfter}" : "";
        int threshold = (preview.LevelBefore / 10 + 1) * 10;
        if (preview.LevelAfter >= threshold) growthPreview.Text += $"\n×2 milestone at LV {threshold}";
        performancePreview.Text = $"PREVIEW / {Number(preview.RoomBefore.Final)} → {Number(preview.RoomAfter.Final)} cr/sec";
        if (preview.AutomationAfter.Enabled != service.Automation.Enabled)
        {
            productionPreview.Text += " · output unchanged now";
            roomPreview.Text += "\n" + (preview.AutomationAfter.Enabled ? "Future upgrades every 30 sec; reserve and policy apply." : "Future automatic purchases will pause.");
        }
        foreach (Action<EmpireActionPreview?> update in connectionUpdates) update(preview);
        if (entering) { AnimateFeedback(productionPreview, .2); AnimateFeedback(budgetPreview, .2); AnimateFeedback(roomPreview, .2); }
        AutomationProperties.SetHelpText(production, productionPreview.Text);
    }

    private void ClearPreview()
    {
        currentPreview = null; previewOwner = null; previewFactory = null;
        budgetPreview.Text = "Hover an action to preview its cost"; productionPreview.Text = "All owned properties · live production";
        budgetPreview.Foreground = productionPreview.Foreground = Ink("#91A99C");
        roomPreview.Text = growthPreview.Text = performancePreview.Text = "";
        AutomationProperties.SetHelpText(production, "Live portfolio production");
        foreach (Action<EmpireActionPreview?> update in connectionUpdates) update(null);
    }

    private void RunRoomAction(Func<EmpireService, EmpireActionResult> action)
    {
        ClearPreview(); EmpireActionResult result = manage(action); SetNotice(result.Message); Refresh();
        if (result.Success) { AnimateFeedback(intelligence, .55); AnimateFeedback(production, .55); }
    }

    private void StopIntelligenceAnimations()
    {
        foreach (ProgressBar progress in intelligenceProgress) progress.BeginAnimation(RangeBase.ValueProperty, null);
    }
}
