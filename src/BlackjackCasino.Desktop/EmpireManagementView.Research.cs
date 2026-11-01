using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireManagementView
{
    private string researchBranch = "Operations";
    private string researchSelection = "construction";

    private void BuildResearchTree()
    {
        var heading = new DockPanel { Margin = new Thickness(0, 0, 0, 9) };
        var points = Text("", 12, "#DFCA99", true);
        DockPanel.SetDock(points, Dock.Right); heading.Children.Add(points);
        updates.Add(() => points.Text = $"{service.LegacyPointsAvailable} LP available · permanent across all estates");
        var branches = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (string branch in EmpireResearchCatalog.All.Select(n => n.Branch).Distinct())
        {
            string route = branch;
            Button tab = Button(branch, "EmpireResearchBranch_" + branch, () =>
            {
                researchBranch = route;
                researchSelection = EmpireResearchCatalog.All.First(n => n.Branch == route).Id;
                Build();
            });
            tab.BorderBrush = Ink(branch == researchBranch ? "#D7B97D" : "#405866"); branches.Children.Add(tab);
        }
        heading.Children.Add(branches); DockPanel.SetDock(heading, Dock.Top); body.Children.Add(heading);
        var legend = Text("Select a node to inspect it. Lines show the exact prerequisite; research spends Legacy Points earned through prestige.", 11, "#9DB5C4");
        DockPanel.SetDock(legend, Dock.Top); body.Children.Add(legend);

        var layout = new Grid { Name = "EmpireResearchTree", Margin = new Thickness(0, 8, 0, 0) };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(270) });
        var canvas = new Canvas { Width = researchBranch == "Operations" ? 900 : 680, Height = 340, Background = Brushes.Transparent };
        var positions = researchBranch switch
        {
            "Operations" => new Dictionary<string, Point> { ["construction"] = new(12, 110), ["night-shift"] = new(240, 15), ["compound"] = new(240, 210), ["automation"] = new(470, 210), ["directors"] = new(700, 210) },
            "Gaming" => new Dictionary<string, Point> { ["tables"] = new(30, 30), ["mechanical"] = new(30, 210), ["high-roller"] = new(330, 30) },
            _ => new Dictionary<string, Point> { ["hospitality"] = new(30, 110), ["destination"] = new(330, 15), ["guests"] = new(330, 210) },
        };
        EmpireResearchDefinition[] nodes = EmpireResearchCatalog.All.Where(n => n.Branch == researchBranch).ToArray();
        // Draw connections before nodes so the hit targets stay unobstructed.
        foreach (EmpireResearchDefinition node in nodes.Where(n => n.Prerequisite is not null))
        {
            Point from = positions[node.Prerequisite!]; Point to = positions[node.Id];
            double startX = from.X + 190, startY = from.Y + 53, endY = to.Y + 53, elbow = startX + (to.X - startX) / 2;
            var line = new Polyline { Name = "EmpireResearchLink_" + node.Id.Replace('-', '_'), StrokeThickness = 3,
                Points = new PointCollection { new(startX, startY), new(elbow, startY), new(elbow, endY), new(to.X, endY) }, IsHitTestVisible = false };
            canvas.Children.Add(line);
            var arrow = new Polygon { Points = new PointCollection { new(to.X, endY), new(to.X - 8, endY - 5), new(to.X - 8, endY + 5) }, IsHitTestVisible = false };
            canvas.Children.Add(arrow);
            updates.Add(() =>
            {
                Brush color = Ink(service.ResearchRank(node.Prerequisite!) >= node.PrerequisiteRank ? "#D6B97A" : "#536675");
                line.Stroke = color; arrow.Fill = color;
            });
            var requirement = Text("R" + node.PrerequisiteRank, 9, "#C8B180");
            Canvas.SetLeft(requirement, elbow - 8); Canvas.SetTop(requirement, endY - 23); canvas.Children.Add(requirement);
        }
        foreach (EmpireResearchDefinition node in nodes)
        {
            var stack = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
            var rank = Text("", 10, "#BFD8D7"); stack.Children.Add(rank);
            stack.Children.Add(Text(node.Name, 14, "#F2E2BF", true));
            var state = Text("", 10, "#B4C9CD"); stack.Children.Add(state);
            Button target = Button("", "EmpireResearchNode_" + node.Id.Replace('-', '_'), () => { researchSelection = node.Id; Build(); });
            target.Content = stack; target.Width = 190; target.Height = 108; target.Padding = new Thickness(0);
            target.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            target.Background = new LinearGradientBrush(Color.FromRgb(33, 58, 70), Color.FromRgb(16, 32, 44), 100);
            target.ToolTip = node.Description; AutomationProperties.SetName(target, node.Name);
            Canvas.SetLeft(target, positions[node.Id].X); Canvas.SetTop(target, positions[node.Id].Y); canvas.Children.Add(target);
            updates.Add(() =>
            {
                int current = service.ResearchRank(node.Id);
                rank.Text = $"RANK {current} / {node.MaxRank}    ·    {node.CostPerRank} LP";
                bool prerequisite = node.Prerequisite is null || service.ResearchRank(node.Prerequisite) >= node.PrerequisiteRank;
                state.Text = current == node.MaxRank ? "✦ MASTERED" : !prerequisite ? "◆ PREREQUISITE REQUIRED" : service.CanResearch(node.Id) ? "● READY TO RESEARCH" : "◇ SAVE LEGACY POINTS";
                target.BorderBrush = Ink(node.Id == researchSelection ? "#F3D28C" : current > 0 ? "#73B6A7" : "#506575");
                target.BorderThickness = new Thickness(node.Id == researchSelection ? 2 : 1);
                AutomationProperties.SetHelpText(target, rank.Text + ". " + state.Text + ". " + node.Description);
            });
        }
        layout.Children.Add(new Border { Background = Ink("#0B1B26"), BorderBrush = Ink("#344B59"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Margin = new Thickness(0, 0, 12, 0), Child = new Viewbox { Stretch = Stretch.Uniform, StretchDirection = StretchDirection.DownOnly, Child = canvas } });
        EmpireResearchDefinition selectedNode = EmpireResearchCatalog.Find(researchSelection)!;
        var inspector = new StackPanel { Margin = new Thickness(15) };
        inspector.Children.Add(Text(researchBranch.ToUpperInvariant() + " / LEGACY NODE", 9, "#D1B77D"));
        inspector.Children.Add(Text(selectedNode.Name, 23, "#F1E0BD", true, new Thickness(0, 8, 0, 10)));
        inspector.Children.Add(Text(selectedNode.Description, 13, "#C1D3D6"));
        var requirementText = Text("", 12, "#C7B68F", margin: new Thickness(0, 16, 0, 12)); inspector.Children.Add(requirementText);
        Button buy = Button("", "EmpireResearch_" + selectedNode.Id.Replace('-', '_'), () => Run(s => s.Research(selectedNode.Id, DateTimeOffset.UtcNow)));
        inspector.Children.Add(buy);
        updates.Add(() =>
        {
            int rank = service.ResearchRank(selectedNode.Id);
            requirementText.Text = $"Rank {rank} / {selectedNode.MaxRank}\nCost per rank: {selectedNode.CostPerRank} LP\nAvailable: {service.LegacyPointsAvailable} LP\n\n"
                + (selectedNode.Prerequisite is null ? "Entry node · no prerequisite." : $"Requires {EmpireResearchCatalog.Find(selectedNode.Prerequisite)!.Name}\nRank {service.ResearchRank(selectedNode.Prerequisite)} / {selectedNode.PrerequisiteRank} required.");
            buy.Content = rank >= selectedNode.MaxRank ? "Mastered permanently" : service.CanResearch(selectedNode.Id) ? "Research next rank  ✦" : "Requirements not met";
            buy.IsEnabled = service.CanResearch(selectedNode.Id);
        });
        inspector.Children.Add(Text("Legacy Points come from property prestige. Every rank survives a reset and benefits every property.", 11, "#8FAAB9", margin: new Thickness(0, 14, 0, 0)));
        Border detail = Card(new ScrollViewer { Content = inspector, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
        detail.Padding = new Thickness(0); Grid.SetColumn(detail, 1); layout.Children.Add(detail); body.Children.Add(layout);
    }
}
