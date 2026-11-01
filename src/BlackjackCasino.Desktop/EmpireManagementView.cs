using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Effects;
using System.Windows.Media;
using System.Windows.Input;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>In-place management pages. Inputs remain stable during the host's 500 ms tick.</summary>
internal sealed partial class EmpireManagementView : UserControl
{
    private readonly EmpireService service;
    private readonly Func<Func<EmpireService, EmpireActionResult>, EmpireActionResult> commit;
    private readonly TextBlock summary = Text("", 12, "#AFCBBB");
    private readonly TextBlock notice = Text("Changes save immediately. All bonuses apply only inside Empire.", 12, "#B9D3C2");
    private readonly DockPanel body = new();
    private readonly List<UIElement> entries = [];
    private int page;
    private int arrangedPageSize = 4;
    private readonly List<Action> updates = [];
    private readonly Dictionary<string, Button> tabs = new(StringComparer.Ordinal);
    private string selected = "Research";
    private readonly Action<string, Func<EmpireService, DateTimeOffset, EmpireActionResult>, string>? showPreview;
    private readonly Action? clearPreview;
    private readonly TextBlock pageTitle = Text("", 23, "#F3E7D0", true);
    private Action? refreshActionPreview;
    private Button? actionPreviewOwner;

    internal EmpireManagementView(EmpireService service, Func<Func<EmpireService, EmpireActionResult>, EmpireActionResult> commit, Action close, string initialSection = "Research", bool embedded = false,
        Action<string, Func<EmpireService, DateTimeOffset, EmpireActionResult>, string>? showPreview = null, Action? clearPreview = null)
    {
        this.service = service; this.commit = commit; selected = initialSection;
        this.showPreview = showPreview; this.clearPreview = clearPreview;
        Unloaded += (_, _) => ClearActionPreview();
        Name = "EmpireManagementPage";
        Foreground = Ink("#F3E7D0");
        var root = new Grid { Margin = new Thickness(embedded ? 0 : 18) };
        foreach (GridLength height in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = height });
        var header = new DockPanel();
        Button back = Button("← Estate overview", "EmpireManagementClose", close);
        DockPanel.SetDock(back, Dock.Right); header.Children.Add(back);
        header.Children.Add(pageTitle); root.Children.Add(header);
        summary.Margin = new Thickness(0, 4, 0, 8); Grid.SetRow(summary, 1); root.Children.Add(summary);
        var navigation = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (string section in embedded ? GroupSections(initialSection) : new[] { "Research", "Synergies", "Projects", "Managers", "Automation", "Events", "Contracts", "Properties" })
        {
            string route = section;
            Button tab = Button(section, "EmpireTab_" + section, () => { selected = route; page = 0; Build(); });
            tab.Content = EmpireView.IconCaption(section, section, 16);
            tabs[section] = tab; navigation.Children.Add(tab);
        }
        Grid.SetRow(navigation, 2); root.Children.Add(navigation);
        Grid.SetRow(body, 3); root.Children.Add(body);
        var status = new Border { Background = Ink("#173328"), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 8, 0, 0), CornerRadius = new CornerRadius(7), Child = notice };
        Grid.SetRow(status, 4); root.Children.Add(status);
        Content = root;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(this, KeyboardNavigationMode.Cycle);
        Loaded += (_, _) => back.Focus();
        PreviewKeyDown += (_, args) => { if (args.Key == Key.Escape) { close(); args.Handled = true; } };
        SizeChanged += (_, _) =>
        {
            int nextSize = ActualHeight is > 0 and < 550 ? 2 : 4;
            if (nextSize == arrangedPageSize || selected is "Automation" or "Research") return;
            arrangedPageSize = nextSize; page = 0; Build();
        };
        Build();
    }
    internal void Refresh()
    {
        summary.Text = $"{service.ActiveProperty.Name} · Prestige {service.Prestige} · Reputation {service.Reputation:N0} · Legacy Points {service.LegacyPointsAvailable}/{service.LegacyPointsEarned} · {EmpireView.Number(service.Credits)} cr";
        foreach (Action update in updates) update();
        if (actionPreviewOwner is { IsEnabled: true }) refreshActionPreview?.Invoke();
        else if (actionPreviewOwner is not null) ClearActionPreview();
    }
    private static string[] GroupSections(string section) => section switch
    {
        "Research" or "Projects" or "Properties" => ["Research", "Projects", "Properties"],
        "Managers" or "Automation" => ["Managers", "Automation"],
        "Events" or "Contracts" => ["Events", "Contracts"], _ => ["Synergies"],
    };

    private void ClearActionPreview()
    {
        actionPreviewOwner = null; refreshActionPreview = null; clearPreview?.Invoke();
    }

    private void PreviewAction(Button owner, Func<string> room, Func<Func<EmpireService, DateTimeOffset, EmpireActionResult>> action, string caption)
    {
        void Enter()
        {
            if (!owner.IsEnabled) return;
            actionPreviewOwner = owner;
            refreshActionPreview = () => showPreview?.Invoke(room(), action(), caption);
            refreshActionPreview();
        }
        owner.MouseEnter += (_, _) => Enter(); owner.GotKeyboardFocus += (_, _) => Enter();
        owner.MouseLeave += (_, _) => { if (actionPreviewOwner == owner) ClearActionPreview(); };
        owner.LostKeyboardFocus += (_, _) => { if (actionPreviewOwner == owner) ClearActionPreview(); };
    }

    private void Run(Func<EmpireService, EmpireActionResult> action)
    {
        EmpireActionResult result = commit(action); notice.Text = result.Message;
        if (result.Success) Build(); else Refresh();
    }
    private void Build()
    {
        ClearActionPreview(); pageTitle.Text = SectionTitle(selected);
        updates.Clear(); entries.Clear(); body.Children.Clear();
        foreach ((string section, Button button) in tabs) button.BorderBrush = Ink(section == selected ? "#DAB67B" : "#41624F");
        switch (selected)
        {
            case "Research": Research(); break;
            case "Synergies": Synergies(); break;
            case "Projects": Projects(); break;
            case "Managers": Managers(); break;
            case "Automation": Automation(); break;
            case "Events": Events(); break;
            case "Contracts": Contracts(); break;
            case "Properties": Properties(); break;
        }
        ArrangeEntries();
        Refresh();
    }
    private void Research()
    {
        BuildResearchTree();
    }
    private void Synergies()
    {
        Intro("An estate that works together", "Synergies use department tags and level milestones. They need no adjacency. A complete tier applies its displayed bonus; tiers replace each other. All strategic production additions share a capped additive bonus bucket.");
        foreach (EmpireSynergyDefinition definition in EmpireSynergyCatalog.All)
        {
            var status = Text("", 12, "#A8D9BB");
            var members = Text("", 11, "#9EBBAA");
            var progress = new ProgressBar { Name = "EmpireSynergyProgress_" + definition.Id.Replace('-', '_'), Minimum = 0, Maximum = 1, BorderThickness = new Thickness(0), BorderBrush = Brushes.Transparent, Height = 7,
                Foreground = Ink("#CEB47A"), Background = Ink("#0C2419"), Margin = new Thickness(0, 8, 0, 8) };
            var stack = new StackPanel(); stack.Children.Add(status); stack.Children.Add(progress); stack.Children.Add(members);
            Row(definition.Name, definition.Description, stack);
            updates.Add(() =>
            {
                EmpireSynergyStatus current = service.Synergies.Single(s => s.Definition.Id == definition.Id);
                status.Text = $"Tier {current.Tier}/{definition.Levels.Length} · +{current.Bonus:P1} production" + (current.NextLevel is int next ? $" · next: every member at level {next}" : " · CIRCUIT COMPLETE ✦");
                progress.Value = current.NextLevel is int target ? Math.Clamp((double)current.MinimumLevel / target, 0, 1) : 1;
                progress.ToolTip = current.NextLevel is int goal ? $"Lowest member: level {current.MinimumLevel} / {goal}" : "All tiers complete";
                members.Text = string.Join("   /   ", EmpireCatalog.Departments.Where(room => room.Floor <= definition.MaximumFloor && (room.Tags & definition.Members) != 0).Select(room => $"{room.Name} {service.Level(room.Id)}"));
            });
        }
    }
    private void Projects()
    {
        Intro("Capital projects", "One-time investments in this property. Projects remain after prestige. Department level requirements apply when building; permanent floor and system unlocks stay open afterwards.");
        foreach (EmpireProjectDefinition project in EmpireProjectCatalog.All)
        {
            var state = Text("", 11, "#AFCCBA");
            Button build = Button("Build", "EmpireProject_" + project.Id.Replace('-', '_'), () => Run(s => s.BuildProject(project.Id, DateTimeOffset.UtcNow)));
            Row(project.Name, project.Description + "\n" + project.Requirement.Description, state, build);
            updates.Add(() => { bool owned = service.Projects.Contains(project.Id); state.Text = owned ? "COMPLETED · PERMANENT" : EmpireView.Number(project.Cost) + " cr"; build.Content = owned ? "Completed" : "Build"; build.IsEnabled = service.CanBuildProject(project.Id); });
        }
    }
    private void Managers()
    {
        Intro("Your management team", "Fixed specialists, no random hiring. Executive Offices unlock one active slot; Prestige V adds a second, Board of Directors research a third. Assignments persist through prestige, but a reset room must reopen to produce.");
        var capacity = Text("", 14, "#D7BE88", true); body.Children.Add(capacity);
        updates.Add(() => capacity.Text = $"Active managers: {service.ManagerAssignments.Count}/{service.ManagerSlots}");
        EmpireDepartment[] operating = EmpireCatalog.Departments.Where(room => service.Level(room.Id) > 0).ToArray();
        foreach (EmpireManagerDefinition manager in EmpireManagerCatalog.All)
        {
            var controls = new WrapPanel();
            var room = new ComboBox { Name = "EmpireManagerRoom_" + manager.Id, ItemsSource = operating, DisplayMemberPath = "Name", SelectedValuePath = "Id", Width = 230, Margin = new Thickness(0, 5, 8, 0) };
            room.SelectedValue = service.ManagerAssignments.GetValueOrDefault(manager.Id) ?? operating.FirstOrDefault()?.Id;
            controls.Children.Add(room);
            Button assign = Button("Assign", "EmpireManagerAssign_" + manager.Id, () =>
            {
                if (room.SelectedValue is string id) Run(s => s.AssignManager(manager.Id, id, DateTimeOffset.UtcNow));
            });
            Button remove = Button("Make inactive", "EmpireManagerRemove_" + manager.Id, () => Run(s => s.AssignManager(manager.Id, null, DateTimeOffset.UtcNow)));
            PreviewAction(assign, () => room.SelectedValue as string ?? "slots", () =>
                (s, at) => s.AssignManager(manager.Id, room.SelectedValue as string, at), "Assign " + manager.Name);
            PreviewAction(remove, () => service.ManagerAssignments.GetValueOrDefault(manager.Id) ?? "slots", () =>
                (s, at) => s.AssignManager(manager.Id, null, at), "Release " + manager.Name);
            room.SelectionChanged += (_, _) => ClearActionPreview();
            controls.Children.Add(assign); controls.Children.Add(remove);
            var state = Text("", 11, "#A6CCB4");
            var content = new StackPanel(); content.Children.Add(state); content.Children.Add(controls);
            Row(manager.Name + " · " + manager.Role, manager.Description + $" Requires Prestige {manager.RequiredPrestige}.", content);
            updates.Add(() =>
            {
                string? assignment = service.ManagerAssignments.GetValueOrDefault(manager.Id);
                state.Text = assignment is null ? "INACTIVE" : "ACTIVE · " + EmpireCatalog.Find(assignment)!.Name;
                assign.IsEnabled = service.Prestige >= manager.RequiredPrestige && service.ManagerSlots > 0 && (assignment is not null || service.ManagerAssignments.Count < service.ManagerSlots) && room.Items.Count > 0;
                remove.IsEnabled = assignment is not null;
            });
        }
    }
    private void Automation()
    {
        Intro("Executive automation", "Requires Executive Offices and Executive Automation research. Each 30 production seconds, the system spends up to your chosen percentage of the current property budget while preserving the reserve. It follows the same schedule offline. At most 50 upgrade batches run per decision.");
        EmpireAutomationState config = service.Automation;
        var form = new Grid { Margin = new Thickness(8) };
        form.ColumnDefinitions.Add(new ColumnDefinition()); form.ColumnDefinitions.Add(new ColumnDefinition());
        var settings = new StackPanel { Margin = new Thickness(8, 0, 24, 0) };
        var prioritiesPanel = new StackPanel { Margin = new Thickness(8, 0, 8, 0) };
        var enabled = new CheckBox { Name = "EmpireAutomationEnabled", Content = "Enable automatic upgrades", IsChecked = config.Enabled, Margin = new Thickness(0, 4, 0, 8), Foreground = Foreground };
        settings.Children.Add(enabled);
        TextBox reserve = AutomationInput("Reserve (cr)", config.Reserve.ToString("0.##", CultureInfo.CurrentCulture), "EmpireAutomationReserve", settings);
        TextBox percent = AutomationInput("Budget / decision (%) · 1–50", (config.SpendPercent * 100).ToString("0.##", CultureInfo.CurrentCulture), "EmpireAutomationPercent", settings);
        TextBox target = AutomationInput("Level target · 1–200", config.TargetLevel.ToString(CultureInfo.InvariantCulture), "EmpireAutomationTarget", settings);
        var mode = new ComboBox { Name = "EmpireAutomationMode", Width = 300, HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = new[] { "Cheapest next milestone", "Priority order" }, SelectedIndex = config.CheapestMilestone ? 0 : 1, Margin = new Thickness(0, 4, 0, 8) };
        settings.Children.Add(mode);
        prioritiesPanel.Children.Add(Text("Priority queue", 19, "#E3D0AB", true));
        prioritiesPanel.ToolTip = "Used by Priority order mode. Select a department to move it or remove it.";
        var priorities = new ObservableCollection<EmpireDepartment>(config.Priorities.Select(id => EmpireCatalog.Find(id)!));
        var list = new ListBox { Name = "EmpireAutomationPriority", ItemsSource = priorities, DisplayMemberPath = "Name", MinHeight = 90, Height = 100, HorizontalAlignment = HorizontalAlignment.Stretch, Background = Ink("#10291D"), Foreground = Foreground, BorderBrush = Ink("#45634D"), Margin = new Thickness(0, 6, 0, 7) };
        prioritiesPanel.Children.Add(list);
        var add = new ComboBox { Width = 230, HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = EmpireCatalog.Departments, DisplayMemberPath = "Name", SelectedIndex = 0 };
        prioritiesPanel.Children.Add(add);
        var tools = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        tools.Children.Add(Button("Add", "EmpireAutomationAdd", () => { if (add.SelectedItem is EmpireDepartment room && !priorities.Contains(room)) priorities.Add(room); }));
        tools.Children.Add(Button("Remove", "EmpireAutomationRemove", () => { if (list.SelectedItem is EmpireDepartment room) priorities.Remove(room); }));
        tools.Children.Add(Button("Move up", "EmpireAutomationUp", () => { int i = list.SelectedIndex; if (i > 0) { priorities.Move(i, i - 1); list.SelectedIndex = i - 1; } }));
        tools.Children.Add(Button("Move down", "EmpireAutomationDown", () => { int i = list.SelectedIndex; if (i >= 0 && i < priorities.Count - 1) { priorities.Move(i, i + 1); list.SelectedIndex = i + 1; } }));
        prioritiesPanel.Children.Add(tools);
        EmpireAutomationState? ReadConfiguration()
        {
            if (!Decimal(reserve.Text, out decimal minimum) || !Decimal(percent.Text, out decimal budget) || !int.TryParse(target.Text, out int maximum)) return null;
            return new EmpireAutomationState { Enabled = enabled.IsChecked == true, Reserve = minimum, SpendPercent = budget / 100, TargetLevel = maximum, CheapestMilestone = mode.SelectedIndex == 0, Priorities = priorities.Select(room => room.Id).ToArray() };
        }
        Button save = Button("Save automation", "EmpireAutomationSave", () =>
        {
            EmpireAutomationState? next = ReadConfiguration();
            if (next is null) { notice.Text = "Enter valid numbers for reserve, budget and level target."; return; }
            Run(s => s.ConfigureAutomation(next, DateTimeOffset.UtcNow));
        });
        PreviewAction(save, () => priorities.FirstOrDefault()?.Id ?? "slots", () => (s, at) =>
            ReadConfiguration() is { } next ? s.ConfigureAutomation(next, at) : new(false, "Invalid configuration"), "Automation policy");
        foreach (TextBox input in new[] { reserve, percent, target }) input.TextChanged += (_, _) => ClearActionPreview();
        enabled.Checked += (_, _) => ClearActionPreview(); enabled.Unchecked += (_, _) => ClearActionPreview();
        mode.SelectionChanged += (_, _) => ClearActionPreview();
        priorities.CollectionChanged += (_, _) => ClearActionPreview();
        save.Margin = new Thickness(0, 8, 0, 0); settings.Children.Add(save);
        var availability = Text("", 11, "#CDB581", margin: new Thickness(0, 8, 0, 0)); prioritiesPanel.Children.Add(availability);
        updates.Add(() =>
        {
            save.IsEnabled = service.AutomationUnlocked;
            availability.Text = service.AutomationUnlocked ? "Automation ready · decisions every 30 production seconds." : "Locked · build Executive Offices and research Executive Automation.";
        });
        form.Children.Add(new ScrollViewer { Content = settings, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        var queueScroll = new ScrollViewer { Content = prioritiesPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(queueScroll, 1); form.Children.Add(queueScroll); entries.Add(Card(form));
    }
    private void Events()
    {
        Intro("Casino events", "One active event per property. The optional game goal earns a single ten-minute encore. Ordinary events run for 30 minutes. Ending an event never penalizes your estate. All effects are Empire production bonuses.");
        var current = Text("", 14, "#A6DCC0", true); body.Children.Add(current);
        updates.Add(() => current.Text = service.ActiveEvent is { } ev
            ? $"{EmpireEventCatalog.Find(ev.Id)!.Name} · {Math.Max(0, (ev.EndsAt - DateTimeOffset.UtcNow).TotalMinutes):0} min remaining · goal {service.EventActivityProgress}/{EmpireEventCatalog.Find(ev.Id)!.ActivityGoal}" + (ev.Encore ? " · ENCORE EARNED" : "")
            : "No event running. Your departments continue to earn normally.");
        foreach (EmpireEventDefinition definition in EmpireEventCatalog.All)
        {
            Button start = Button("Start event", "EmpireEvent_" + definition.Id, () => Run(s => s.StartEvent(definition.Id, DateTimeOffset.UtcNow)));
            var cost = Text(EmpireView.Number(definition.Cost) + " cr · " + definition.Requirement.Description, 11, "#B7CEBC");
            Row(definition.Name, definition.Description, cost, start);
            updates.Add(() => start.IsEnabled = service.CanStartEvent(definition.Id));
        }
    }
    private void Contracts()
    {
        Intro("The contract board", "Choose one persistent brief. Only production and room levels earned after acceptance count. There are no daily resets, deadlines or login streaks. Rewards replace the previous contract buff and last for two production hours.");
        var current = Text("", 14, "#A8DCC0", true);
        Button claim = Button("Claim completed contract", "EmpireContractClaim", () => Run(s => s.ClaimContract(DateTimeOffset.UtcNow)));
        body.Children.Add(current); body.Children.Add(claim);
        updates.Add(() =>
        {
            current.Text = service.ActiveContract is { } contract ? $"{EmpireContractCatalog.Find(contract.Id)!.Name} · {EmpireView.Number(service.ContractProgress)} / {EmpireView.Number(EmpireContractCatalog.Find(contract.Id)!.Goal)}"
                : "No contract accepted. Choose your next brief.";
            if (service.ContractBuff is { } buff) current.Text += $"\nReward active: {EmpireContractCatalog.Find(buff.ContractId)!.RewardDescription} · {buff.RemainingSeconds / 60:0} production minutes left";
            claim.IsEnabled = service.CanClaimContract;
        });
        foreach (EmpireContractDefinition contract in EmpireContractCatalog.All)
        {
            Button accept = Button("Accept brief", "EmpireContract_" + contract.Id.Replace('-', '_'), () => Run(s => s.AcceptContract(contract.Id, DateTimeOffset.UtcNow)));
            Row(contract.Name, contract.Description, Text("Reward: " + contract.RewardDescription, 12, "#D7BE88"), accept);
            updates.Add(() => accept.IsEnabled = service.ActiveContract is null);
        }
    }
    private void Properties()
    {
        Intro("A growing casino empire", "Properties have independent construction budgets, rooms, prestige, projects, managers, events and contracts. All owned properties earn together. Legacy research and the bounded owner reserve are shared; production never improves another game's odds.");
        foreach (EmpirePropertyDefinition property in EmpirePropertyCatalog.All)
        {
            var state = Text("", 12, "#B1D2BE");
            Button action = Button("", "EmpireProperty_" + property.Id.Replace('-', '_'), () => Run(s => s.OwnedProperties.Any(p => p.Id == property.Id)
                ? s.SelectProperty(property.Id, DateTimeOffset.UtcNow) : s.BuyProperty(property.Id, DateTimeOffset.UtcNow)));
            Row(property.Name, property.Description + (property.Specialty == EmpireTag.None ? "" : $"\nSpecialty: {property.Specialty} +{property.ProductionBonus:P0}.") + "\n" + property.Requirement.Description, state, action);
            updates.Add(() =>
            {
                bool owned = service.OwnedProperties.Any(p => p.Id == property.Id);
                if (owned)
                {
                    EmpirePropertyState estate = service.PropertySnapshot(property.Id);
                    state.Text = $"Prestige {estate.Prestige} · {estate.Levels.Values.Sum():N0} room levels · {EmpireView.Number(estate.Credits)} cr · {estate.Projects.Count} projects";
                    action.Content = property.Id == service.ActivePropertyId ? "Managing" : "Manage estate";
                    action.IsEnabled = property.Id != service.ActivePropertyId;
                }
                else { state.Text = EmpireView.Number(property.Cost) + " cr from this property's budget"; action.Content = "Purchase property"; action.IsEnabled = service.CanBuyProperty(property.Id); }
            });
        }
    }
    private void Intro(string title, string detail)
    {
        var intro = Text(detail, 11, "#9EBAAA", margin: new Thickness(0, 0, 0, 10));
        intro.MaxHeight = 48; intro.TextTrimming = TextTrimming.CharacterEllipsis; intro.ToolTip = detail;
        body.Children.Add(intro);
    }
    private void Row(string title, string detail, UIElement status, Button? action = null)
    {
        var content = new DockPanel();
        var info = new StackPanel { Margin = new Thickness(0, 0, 0, 0) }; info.Children.Add(Text(title, 16, "#EAD5AF", true));
        info.Children.Add(Text(detail, 12, "#ACC4B5", margin: new Thickness(0, 4, 0, 7))); info.Children.Add(status); content.Children.Add(info);
        if (action is not null) { action.MinWidth = 135; action.HorizontalAlignment = HorizontalAlignment.Left; DockPanel.SetDock(action, Dock.Bottom); content.Children.Insert(0, action); }
        entries.Add(Card(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled }));
    }
    private static Border Card(UIElement child) => new() { Background = new LinearGradientBrush(Color.FromRgb(24, 47, 33), Color.FromRgb(12, 29, 21), 110), BorderBrush = Ink("#405B47"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(14), Margin = new Thickness(0, 0, 10, 10), Effect = new DropShadowEffect { BlurRadius = 12, ShadowDepth = 4, Opacity = .2 }, Child = child };
    private static bool Decimal(string value, out decimal number) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number);
    private static Button Button(string text, string name, Action action)
    {
        var button = new Button { Name = name, Content = text, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 7, 6), FontSize = 11, Background = Ink("#19382A"), BorderBrush = Ink("#3F604C"), Foreground = Ink("#D8E4DF") };
        button.Click += (_, _) => action(); return button;
    }
    private static TextBlock Text(string text, double size = 12, string color = "#B7CDBD", bool bold = false, Thickness? margin = null) => new()
    { Text = text, FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = margin ?? new Thickness(0, 0, 0, 3) };
    private static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
