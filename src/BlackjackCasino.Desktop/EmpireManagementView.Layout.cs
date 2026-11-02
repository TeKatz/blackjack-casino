using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireManagementView
{
    private static string SectionTitle(string section) => section switch
    {
        "Development" => "Development · Projects & Properties", "Operations" => "Operations · Managers & Automation",
        "Opportunities" => "Opportunities · Events & Contracts", "Research" => "Legacy Research", "Synergies" => "Synergy Circuits", "Projects" => "Capital Projects",
        "Managers" => "Executive Team", "Automation" => "Executive Automation", "Events" => "Casino Events",
        "Contracts" => "Contract Board", "Properties" => "Property Portfolio", _ => section,
    };

    private void BuildCombined(string firstTitle, Action first, string secondTitle, Action second, int firstColumns, int secondColumns, double ratio)
    {
        var overview = new Grid { Name = "EmpireCombinedOverview" };
        overview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ratio, GridUnitType.Star) });
        overview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddGroup(firstTitle, first, firstColumns, 0);
        AddGroup(secondTitle, second, secondColumns, 1);
        entries.Clear(); body.Children.Clear(); body.Children.Add(overview);

        void AddGroup(string title, Action build, int columns, int column)
        {
            body.Children.Clear(); entries.Clear();
            build();
            var group = new DockPanel { Name = "EmpireOverview_" + title, Margin = new Thickness(column == 0 ? 0 : 8, 0, 0, 0) };
            var heading = Text(title.ToUpperInvariant(), 16, "#E3C991", true, new Thickness(0, 0, 0, 8));
            DockPanel.SetDock(heading, Dock.Top); group.Children.Add(heading);
            foreach (UIElement element in body.Children.Cast<UIElement>().ToArray())
            {
                body.Children.Remove(element); DockPanel.SetDock(element, Dock.Top); group.Children.Add(element);
            }
            var cards = new UniformGrid { Name = "EmpireOverviewCards_" + title, Columns = columns,
                Rows = Math.Max(1, (entries.Count + columns - 1) / columns) };
            foreach (UIElement entry in entries) cards.Children.Add(entry);
            group.Children.Add(cards); Grid.SetColumn(group, column); overview.Children.Add(group);
        }
    }

    private static TextBox AutomationInput(string caption, string value, string name, Panel parent)
    {
        var row = new Grid { Margin = new Thickness(0, 2, 0, 7) };
        row.ColumnDefinitions.Add(new ColumnDefinition()); row.ColumnDefinitions.Add(new ColumnDefinition());
        TextBlock label = Text(caption, 11, "#ACC6B2"); label.VerticalAlignment = VerticalAlignment.Center; label.Margin = new Thickness(0, 0, 10, 0); row.Children.Add(label);
        var input = new TextBox { Name = name, Text = value, MinWidth = 135, Height = 30, Padding = new Thickness(7, 4, 7, 4),
            Background = Ink("#10291D"), Foreground = Ink("#F0E3CE"), BorderBrush = Ink("#50705A"), CaretBrush = Ink("#E5C47E") };
        System.Windows.Automation.AutomationProperties.SetName(input, caption);
        Grid.SetColumn(input, 1); row.Children.Add(input); parent.Children.Add(row); return input;
    }

    private void ArrangeEntries()
    {
        if (entries.Count == 0) return; // Research supplies its own constrained graph.
        foreach (UIElement element in body.Children) DockPanel.SetDock(element, Dock.Top);
        int pageSize = ActualHeight is > 0 and < 550 ? 2 : 4;
        int pages = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
        page = Math.Clamp(page, 0, pages - 1);
        if (pages > 1)
        {
            var controls = new DockPanel { Margin = new Thickness(0, 0, 10, 7) };
            Button previous = Button("← Previous", "EmpirePagePrevious", () => { page--; Build(); });
            Button next = Button("Next →", "EmpirePageNext", () => { page++; Build(); });
            previous.IsEnabled = page > 0; next.IsEnabled = page + 1 < pages;
            DockPanel.SetDock(next, Dock.Right); controls.Children.Add(next);
            DockPanel.SetDock(previous, Dock.Left); controls.Children.Add(previous);
            controls.Children.Add(Text($"Page {page + 1} / {pages} · {entries.Count} {selected.ToLowerInvariant()}", 11, "#CBB98E"));
            DockPanel.SetDock(controls, Dock.Top); body.Children.Add(controls);
        }
        int count = Math.Min(pageSize, entries.Count - page * pageSize);
        var grid = new UniformGrid { Name = "EmpireSectionCards", Columns = count == 1 ? 1 : 2, Rows = count > 2 ? 2 : 1 };
        foreach (UIElement element in entries.Skip(page * pageSize).Take(pageSize)) grid.Children.Add(element);
        body.Children.Add(grid);
    }
}
