using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal sealed class EmpireOfflineReportWindow : Window
{
    internal EmpireOfflineReportWindow(EmpireAccrual report)
    {
        Title = "Casino Empire · Offline Report";
        Width = 540;
        MinHeight = 430;
        MaxHeight = 760;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Ink("#091714");
        Foreground = Ink("#F2E4C8");
        FontFamily = new FontFamily("Segoe UI");

        var root = new Border
        {
            Background = new LinearGradientBrush(Color.FromRgb(10, 29, 24), Color.FromRgb(28, 23, 31), 35),
            BorderBrush = Ink("#8B7048"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(28),
        };
        var stack = new StackPanel();
        root.Child = stack;

        stack.Children.Add(new TextBlock
        {
            Text = "WELCOME BACK, BOSS",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink("#D6B478"),
            CharacterSpacing = 120,
        });
        stack.Children.Add(new TextBlock
        {
            Text = "The house kept working.",
            FontFamily = new FontFamily("Georgia"),
            FontSize = 31,
            Margin = new Thickness(0, 8, 0, 4),
            Foreground = Ink("#F5E7CC"),
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Offline for " + Duration(report.Elapsed),
            FontSize = 13,
            Foreground = Ink("#9FB5A8"),
            Margin = new Thickness(0, 0, 0, 18),
        });

        var totals = new Grid { Margin = new Thickness(0, 0, 0, 18) };
        totals.ColumnDefinitions.Add(new ColumnDefinition());
        totals.ColumnDefinitions.Add(new ColumnDefinition());
        AddMetric(totals, 0, "EMPIRE CREDITS", "+" + EmpireView.Number(report.Credits) + " cr");
        AddMetric(totals, 1, "OWNER DIVIDEND", "+" + report.Dividend.ToString("C2", CultureInfo.GetCultureInfo("en-US")));
        stack.Children.Add(totals);

        stack.Children.Add(new TextBlock
        {
            Text = "ROOM EARNINGS",
            FontSize = 10,
            Foreground = Ink("#B89B69"),
            Margin = new Thickness(0, 0, 0, 7),
        });

        var roomList = new StackPanel();
        foreach (var pair in report.Rooms.OrderBy(pair => EmpireCatalog.Find(pair.Key)?.Floor ?? int.MaxValue)
                     .ThenBy(pair => EmpireCatalog.Find(pair.Key)?.Name ?? pair.Key))
        {
            if (pair.Value <= 0) continue;
            EmpireDepartment? room = EmpireCatalog.Find(pair.Key);
            var line = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var amount = new TextBlock
            {
                Text = "+" + EmpireView.Number(pair.Value) + " cr",
                Foreground = Ink("#B9DFC4"),
                FontWeight = FontWeights.SemiBold,
            };
            DockPanel.SetDock(amount, Dock.Right);
            line.Children.Add(amount);
            line.Children.Add(new TextBlock
            {
                Text = room is null ? pair.Key : (room.Floor == 1 ? room.Name : "Upper Floor · " + room.Name),
                Foreground = Ink("#D8E2DA"),
            });
            roomList.Children.Add(line);
        }
        stack.Children.Add(new Border
        {
            Background = Ink("#10241E"),
            BorderBrush = Ink("#33483D"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Child = roomList,
        });

        if (report.Capped)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "Offline production reached the 8-hour storage limit. Time beyond the cap was not paid twice.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Ink("#D9BD82"),
                Margin = new Thickness(0, 14, 0, 0),
                FontSize = 12,
            });
        }

        var close = new Button
        {
            Content = "COLLECT & CONTINUE",
            Height = 42,
            Margin = new Thickness(0, 22, 0, 0),
            Padding = new Thickness(18, 8, 18, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        close.Click += (_, _) => Close();
        stack.Children.Add(close);
        Content = root;
    }

    private static void AddMetric(Grid grid, int column, string label, string value)
    {
        var stack = new StackPanel { Margin = new Thickness(column == 0 ? 0 : 8, 0, column == 0 ? 8 : 0, 0) };
        stack.Children.Add(new TextBlock { Text = label, FontSize = 9, Foreground = Ink("#B89B69") });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 22,
            Foreground = Ink("#F0D39C"),
            Margin = new Thickness(0, 4, 0, 0),
        });
        var panel = new Border
        {
            Background = Ink("#132820"),
            BorderBrush = Ink("#3A4E40"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(13, 10, 13, 10),
            Child = stack,
        };
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private static string Duration(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1) return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m";
        return $"{Math.Max(1, (int)elapsed.TotalMinutes)}m";
    }

    private static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
