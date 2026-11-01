using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed partial class EmpireView
{
    private void BuildCircuitOverview()
    {
        circuitOverview.Children.Add(Label("SYNERGY CIRCUITS  /  GROW YOUR DEPARTMENTS TOGETHER", 9, "#C3AE86", new Thickness(3, 0, 0, 8)));
        var circuits = new UniformGrid { Columns = 4 };
        foreach (EmpireSynergyDefinition definition in EmpireSynergyCatalog.All)
        {
            var content = new StackPanel { Margin = new Thickness(11, 8, 11, 8) };
            var title = Label(definition.Name + "  ↗", 13, "#E6D2A8"); content.Children.Add(title);
            var state = Label("", 11, "#ACCFCE"); content.Children.Add(state);
            var progress = new ProgressBar { Name = "EmpireOverviewCircuit_" + definition.Id.Replace('-', '_'), Minimum = 0, Maximum = 1, BorderThickness = new Thickness(0), BorderBrush = Brushes.Transparent, Height = 5,
                Foreground = Ink("#BFA36F"), Background = Ink("#12232D"), Margin = new Thickness(0, 8, 0, 7) }; content.Children.Add(progress);
            var goal = Label("", 10, "#8FAAB7"); content.Children.Add(goal);
            Button open = Command("", "EmpireCircuit_" + definition.Id.Replace('-', '_'), () => OpenSection("Synergies"));
            open.Content = content; open.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            open.Padding = new Thickness(0); open.Margin = new Thickness(0, 0, 8, 0);
            open.Background = new LinearGradientBrush(Color.FromRgb(23, 44, 54), Color.FromRgb(12, 28, 38), 100);
            circuits.Children.Add(open);
            commandUpdates.Add(() =>
            {
                EmpireSynergyStatus current = service.Synergies.Single(s => s.Definition.Id == definition.Id);
                state.Text = $"Tier {current.Tier} / {definition.Levels.Length}   ·   +{current.Bonus:P0} production";
                progress.Value = current.NextLevel is int next ? Math.Clamp((double)current.MinimumLevel / next, 0, 1) : 1;
                goal.Text = current.NextLevel is int level ? $"Lowest member: {current.MinimumLevel} / {level}" : "Circuit complete  ✦";
                open.ToolTip = definition.Description + "\n" + string.Join(" · ", EmpireCatalog.Departments.Where(r => r.Floor <= definition.MaximumFloor && (r.Tags & definition.Members) != 0).Select(r => r.Name + " " + service.Level(r.Id)));
            });
        }
        circuitOverview.Children.Add(circuits);
    }
}
