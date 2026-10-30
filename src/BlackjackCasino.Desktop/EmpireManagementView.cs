using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

/// <summary>In-place management pages. Inputs remain stable during the host's 500 ms tick.</summary>
internal sealed class EmpireManagementView : UserControl
{
    private readonly EmpireService service;
    private readonly Func<Func<EmpireService, EmpireActionResult>, EmpireActionResult> commit;
    private readonly TextBlock summary = Text("", 12, "#AFCBBB");
    private readonly TextBlock notice = Text("Changes save immediately. All bonuses apply only inside Empire.", 12, "#B9D3C2");
    private readonly StackPanel body = new();
    private readonly List<Action> updates = [];
    private readonly Dictionary<string, Button> tabs = new(StringComparer.Ordinal);
    private string selected = "Research";

    internal EmpireManagementView(EmpireService service, Func<Func<EmpireService, EmpireActionResult>, EmpireActionResult> commit, Action close)
    {
        this.service = service; this.commit = commit;
        Name = "EmpireManagementPage";
        Foreground = Ink("#F3E7D0");
        var root = new Grid { Margin = new Thickness(18), MaxWidth = 1220 };
        foreach (GridLength height in new[] { GridLength.Auto, GridLength.Auto, GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = height });
        var header = new DockPanel();
        Button back = Button("BACK TO ESTATE", "EmpireManagementClose", close);
        DockPanel.SetDock(back, Dock.Right); header.Children.Add(back);
        header.Children.Add(Text("Empire management", 29, "#F3E7D0", true)); root.Children.Add(header);
        summary.Margin = new Thickness(0, 6, 0, 12); Grid.SetRow(summary, 1); root.Children.Add(summary);
        var navigation = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (string section in new[] { "Research", "Synergies", "Projects", "Managers", "Automation", "Events", "Contracts", "Properties" })
        {
            string route = section;
            Button tab = Button(section, "EmpireTab_" + section, () => { selected = route; Build(); });
            tabs[section] = tab; navigation.Children.Add(tab);
        }
        Grid.SetRow(navigation, 2); root.Children.Add(navigation);
        var scroll = new ScrollViewer { Name = "EmpireManagementScroll", Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(scroll, 3); root.Children.Add(scroll);
        var status = new Border { Background = Ink("#173328"), Padding = new Thickness(12), Margin = new Thickness(0, 12, 0, 0), CornerRadius = new CornerRadius(7), Child = notice };
        Grid.SetRow(status, 4); root.Children.Add(status);
        Content = root;
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        KeyboardNavigation.SetControlTabNavigation(this, KeyboardNavigationMode.Cycle);
        Loaded += (_, _) => back.Focus();
        PreviewKeyDown += (_, args) => { if (args.Key == Key.Escape) { close(); args.Handled = true; } };
        Build();
    }
    internal void Refresh()
    {
        summary.Text = $"{service.ActiveProperty.Name} · Prestige {service.Prestige} · Reputation {service.Reputation:N0} · Legacy Points {service.LegacyPointsAvailable}/{service.LegacyPointsEarned} · {EmpireView.Number(service.Credits)} cr";
        foreach (Action update in updates) update();
    }
    private void Run(Func<EmpireService, EmpireActionResult> action)
    {
        EmpireActionResult result = commit(action); notice.Text = result.Message;
        if (result.Success) Build(); else Refresh();
    }
    private void Build()
    {
        updates.Clear(); body.Children.Clear();
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
        Refresh();
    }
    private void Research()
    {
        Intro("A permanent legacy", "Each property prestige earns one Legacy Point. Existing prestige is credited automatically. Research persists across every reset and affects every property. Branch prerequisites and limited points make your build a choice.");
        foreach (var branch in EmpireResearchCatalog.All.GroupBy(node => node.Branch))
        {
            body.Children.Add(Text(branch.Key.ToUpperInvariant(), 12, "#D9B477", true));
            foreach (EmpireResearchDefinition node in branch)
            {
                var status = Text("", 11, "#A2CBB1");
                Button buy = Button("Research", "EmpireResearch_" + node.Id.Replace('-', '_'), () => Run(s => s.Research(node.Id, DateTimeOffset.UtcNow)));
                Row(node.Name, node.Description + (node.Prerequisite is null ? "" : $"\nRequires {EmpireResearchCatalog.Find(node.Prerequisite)!.Name} rank {node.PrerequisiteRank}."), status, buy);
                updates.Add(() => { status.Text = $"Rank {service.ResearchRank(node.Id)}/{node.MaxRank} · {node.CostPerRank} Legacy Point(s) per rank"; buy.IsEnabled = service.CanResearch(node.Id); });
            }
        }
    }
    private void Synergies()
    {
        Intro("An estate that works together", "Synergies use department tags and level milestones. They need no adjacency. A complete tier applies its displayed bonus; tiers replace each other. All strategic production additions share a capped additive bonus bucket.");
        foreach (EmpireSynergyDefinition definition in EmpireSynergyCatalog.All)
        {
            var status = Text("", 12, "#A8D9BB");
            var members = Text("", 11, "#9EBBAA");
            var stack = new StackPanel(); stack.Children.Add(status); stack.Children.Add(members);
            Row(definition.Name, definition.Description, stack);
            updates.Add(() =>
            {
                EmpireSynergyStatus current = service.Synergies.Single(s => s.Definition.Id == definition.Id);
                status.Text = $"Tier {current.Tier}/{definition.Levels.Length} · +{current.Bonus:P1} production" + (current.NextLevel is int next ? $" · next: every member at level {next}" : " · CIRCUIT COMPLETE ✦");
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
        var panel = new StackPanel { Margin = new Thickness(16) };
        var enabled = new CheckBox { Name = "EmpireAutomationEnabled", Content = "Enable automatic upgrades", IsChecked = config.Enabled, Margin = new Thickness(0, 4, 0, 12), Foreground = Foreground };
        panel.Children.Add(enabled);
        TextBox reserve = Input("Minimum reserve (cr)", config.Reserve.ToString(CultureInfo.InvariantCulture), "EmpireAutomationReserve", panel);
        TextBox percent = Input("Maximum budget per decision (%) · 1–50", (config.SpendPercent * 100).ToString(CultureInfo.InvariantCulture), "EmpireAutomationPercent", panel);
        TextBox target = Input("Stop each department at level · 1–200", config.TargetLevel.ToString(CultureInfo.InvariantCulture), "EmpireAutomationTarget", panel);
        var mode = new ComboBox { Name = "EmpireAutomationMode", Width = 300, HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = new[] { "Cheapest next milestone", "Priority order" }, SelectedIndex = config.CheapestMilestone ? 0 : 1, Margin = new Thickness(0, 8, 0, 12) };
        panel.Children.Add(mode);
        panel.Children.Add(Text("Priority order (used in Priority order mode)", 12, "#B7CFBE"));
        var priorities = new ObservableCollection<EmpireDepartment>(config.Priorities.Select(id => EmpireCatalog.Find(id)!));
        var list = new ListBox { Name = "EmpireAutomationPriority", ItemsSource = priorities, DisplayMemberPath = "Name", Width = 340, Height = 145, HorizontalAlignment = HorizontalAlignment.Left, Background = Ink("#10251D"), Foreground = Foreground, BorderBrush = Ink("#456653"), Margin = new Thickness(0, 6, 0, 7) };
        panel.Children.Add(list);
        var add = new ComboBox { Width = 230, ItemsSource = EmpireCatalog.Departments, DisplayMemberPath = "Name", SelectedIndex = 0 };
        var tools = new WrapPanel(); tools.Children.Add(add);
        tools.Children.Add(Button("Add", "EmpireAutomationAdd", () => { if (add.SelectedItem is EmpireDepartment room && !priorities.Contains(room)) priorities.Add(room); }));
        tools.Children.Add(Button("Remove", "EmpireAutomationRemove", () => { if (list.SelectedItem is EmpireDepartment room) priorities.Remove(room); }));
        tools.Children.Add(Button("Move up", "EmpireAutomationUp", () => { int i = list.SelectedIndex; if (i > 0) { priorities.Move(i, i - 1); list.SelectedIndex = i - 1; } }));
        tools.Children.Add(Button("Move down", "EmpireAutomationDown", () => { int i = list.SelectedIndex; if (i >= 0 && i < priorities.Count - 1) { priorities.Move(i, i + 1); list.SelectedIndex = i + 1; } }));
        panel.Children.Add(tools);
        Button save = Button("Save automation", "EmpireAutomationSave", () =>
        {
            if (!Decimal(reserve.Text, out decimal minimum) || !Decimal(percent.Text, out decimal budget) || !int.TryParse(target.Text, out int maximum))
            { notice.Text = "Enter valid numbers for reserve, budget and level target."; return; }
            var next = new EmpireAutomationState { Enabled = enabled.IsChecked == true, Reserve = minimum, SpendPercent = budget / 100, TargetLevel = maximum, CheapestMilestone = mode.SelectedIndex == 0, Priorities = priorities.Select(room => room.Id).ToArray() };
            Run(s => s.ConfigureAutomation(next, DateTimeOffset.UtcNow));
        });
        save.Margin = new Thickness(0, 14, 0, 0); panel.Children.Add(save);
        updates.Add(() => save.IsEnabled = service.AutomationUnlocked);
        body.Children.Add(Card(panel));
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
        body.Children.Add(Text(title, 24, "#F0E2C5", true));
        body.Children.Add(Text(detail, 12, "#A4BEAE", margin: new Thickness(0, 5, 0, 14)));
    }
    private void Row(string title, string detail, UIElement status, Button? action = null)
    {
        var content = new Grid(); content.ColumnDefinitions.Add(new ColumnDefinition()); content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var info = new StackPanel { Margin = new Thickness(0, 0, 14, 0) }; info.Children.Add(Text(title, 16, "#EAD5AF", true));
        info.Children.Add(Text(detail, 12, "#ACC4B5", margin: new Thickness(0, 4, 0, 7))); info.Children.Add(status); content.Children.Add(info);
        if (action is not null) { Grid.SetColumn(action, 1); action.MinWidth = 135; action.VerticalAlignment = VerticalAlignment.Center; content.Children.Add(action); }
        body.Children.Add(Card(content));
    }
    private static Border Card(UIElement child) => new() { Background = Ink("#10271F"), BorderBrush = Ink("#3C5946"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), Padding = new Thickness(14), Margin = new Thickness(0, 0, 6, 10), Child = child };
    private static TextBox Input(string caption, string value, string name, Panel parent)
    {
        parent.Children.Add(Text(caption, 11, "#ADCBBA"));
        var input = new TextBox { Name = name, Text = value, Width = 300, Height = 32, Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 5, 0, 10),
            Background = Ink("#0B2018"), Foreground = Ink("#F0E3CE"), BorderBrush = Ink("#507660"), CaretBrush = Ink("#E5C47E") };
        parent.Children.Add(input); return input;
    }
    private static bool Decimal(string value, out decimal number) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number);
    private static Button Button(string text, string name, Action action)
    {
        var button = new Button { Name = name, Content = text, Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(0, 0, 7, 6), FontSize = 11 };
        button.Click += (_, _) => action(); return button;
    }
    private static TextBlock Text(string text, double size = 12, string color = "#B7CDBD", bool bold = false, Thickness? margin = null) => new()
    { Text = text, FontSize = size, Foreground = Ink(color), TextWrapping = TextWrapping.Wrap, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, Margin = margin ?? new Thickness(0, 0, 0, 3) };
    private static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
