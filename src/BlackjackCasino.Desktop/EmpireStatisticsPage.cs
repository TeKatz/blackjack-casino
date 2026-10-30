using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Desktop;

internal static class EmpireStatisticsPage
{
    internal static void Attach(EmpireView view, EmpireService service)
    {
        view.Loaded += (_, _) => EnsureButton(view, service);
        if (view.IsLoaded) EnsureButton(view, service);
    }

    private static void EnsureButton(EmpireView view, EmpireService service)
    {
        if (view.Content is not Grid root || root.Children.OfType<DockPanel>().FirstOrDefault() is not DockPanel header)
            return;
        if (header.Children.OfType<Button>().Any(button => button.Name == "EmpireStatisticsButton"))
            return;

        var button = new Button
        {
            Name = "EmpireStatisticsButton",
            Content = "STATISTICS",
            Padding = new Thickness(12, 6, 12, 6),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = "Open detailed Casino Empire statistics",
        };
        DockPanel.SetDock(button, Dock.Right);
        button.Click += (_, _) => Show(root, service);
        header.Children.Insert(Math.Max(0, header.Children.Count - 1), button);
    }

    private static void Show(Grid root, EmpireService service)
    {
        Grid? existing = root.Children.OfType<Grid>().FirstOrDefault(grid => grid.Name == "EmpireStatisticsOverlay");
        if (existing is not null)
        {
            root.Children.Remove(existing);
        }

        EmpireState snapshot = service.Snapshot;
        var body = new StackPanel { Margin = new Thickness(26) };
        var top = new DockPanel();
        var close = new Button { Name = "EmpireStatisticsCloseButton", Content = "BACK TO EMPIRE", Padding = new Thickness(12, 6, 12, 6) };
        DockPanel.SetDock(close, Dock.Right);
        top.Children.Add(close);
        var heading = new StackPanel();
        heading.Children.Add(Text("CASINO EMPIRE / STATISTICS", 10, "#D6B478", bold: true));
        heading.Children.Add(Text("Estate Ledger", 31, "#F5E7CC", font: "Georgia", margin: new Thickness(0, 5, 0, 0)));
        heading.Children.Add(Text("Permanent performance, current run and room production.", 12, "#9FB5A8", margin: new Thickness(0, 4, 0, 0)));
        top.Children.Add(heading);
        body.Children.Add(top);

        var summary = new UniformGrid { Columns = 4, Margin = new Thickness(0, 18, 0, 10) };
        summary.Children.Add(Metric("LIFETIME CREDITS", EmpireView.Number(snapshot.LifetimeCredits) + " cr"));
        summary.Children.Add(Metric("CURRENT RUN", EmpireView.Number(snapshot.RunCredits) + " cr"));
        summary.Children.Add(Metric("OWNER CASH PAID", snapshot.LifetimeCashClaimed.ToString("C2", CultureInfo.GetCultureInfo("en-US"))));
        summary.Children.Add(Metric("EMPIRE PRESTIGE", service.Prestige.ToString(CultureInfo.InvariantCulture)));
        summary.Children.Add(Metric("CURRENT CREDITS", EmpireView.Number(service.Credits) + " cr"));
        summary.Children.Add(Metric("PRODUCTION", EmpireView.Number(service.CreditsPerSecond) + " cr/s"));
        summary.Children.Add(Metric("DIVIDEND RESERVE", service.ClaimableDividend.ToString("C2", CultureInfo.GetCultureInfo("en-US"))));
        summary.Children.Add(Metric("OWNER RATE", service.CashPerHour.ToString("C2", CultureInfo.GetCultureInfo("en-US")) + "/h"));
        body.Children.Add(summary);

        var progression = new Border
        {
            Background = Ink("#10241E"), BorderBrush = Ink("#35483B"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 12),
        };
        var progressionBody = new StackPanel();
        progressionBody.Children.Add(Text("ESTATE PROGRESSION", 9, "#BEA674", bold: true));
        progressionBody.Children.Add(Text(
            $"{service.OpenRooms:N0} rooms operating · {service.TotalLevels:N0} total levels · Main Floor {service.MainFloorLevels:N0} levels\n"
            + $"Permanent Prestige production +{service.PrestigeBonusPercent:P0} · {service.ActiveAchievementBoosts.Count:N0} active achievement boosts\n"
            + (service.Floor2Unlocked ? "Upper Floor permanently unlocked." : "Upper Floor locked until Empire Prestige I."),
            12, "#C5D6CB", margin: new Thickness(0, 7, 0, 0)));
        progression.Child = progressionBody;
        body.Children.Add(progression);

        foreach (int floor in new[] { 1, 2 })
        {
            bool unlocked = service.FloorUnlocked(floor);
            var section = new Border
            {
                Background = Ink("#0D201A"), BorderBrush = Ink(unlocked ? "#3B5545" : "#2D3832"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9),
                Padding = new Thickness(14), Margin = new Thickness(0, 0, 0, 12), Opacity = unlocked ? 1 : .62,
            };
            var sectionBody = new StackPanel();
            sectionBody.Children.Add(Text(floor == 1 ? "MAIN FLOOR" : unlocked ? "UPPER FLOOR" : "UPPER FLOOR · LOCKED", 10, "#D6B478", bold: true));
            decimal floorRate = EmpireCatalog.Floor(floor).Sum(room => service.RoomCreditsPerSecond(room.Id));
            int floorLevels = EmpireCatalog.Floor(floor).Sum(room => service.Level(room.Id));
            sectionBody.Children.Add(Text($"{service.OpenRoomsOnFloor(floor)} / {EmpireCatalog.Floor(floor).Count} rooms operating · {floorLevels:N0} levels · {EmpireView.Number(floorRate)} cr/s", 12, "#A8C5B3", margin: new Thickness(0, 4, 0, 7)));

            foreach (EmpireDepartment room in EmpireCatalog.Floor(floor))
            {
                int level = service.Level(room.Id);
                var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition());
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
                row.Children.Add(Text(room.Name, 12, level > 0 ? "#E7E1D2" : "#7F9388", bold: level > 0));
                var lvl = Text(level > 0 ? "LV " + level : "LOCKED", 11, "#9EB3A7"); Grid.SetColumn(lvl, 1); row.Children.Add(lvl);
                var rate = Text(level > 0 ? EmpireView.Number(service.RoomCreditsPerSecond(room.Id)) + " cr/s" : "—", 11, "#A8D9BB"); Grid.SetColumn(rate, 2); row.Children.Add(rate);
                var boost = Text(level > 0 ? "+" + service.AchievementBonusPercent(room.Id).ToString("P1", CultureInfo.InvariantCulture) + " achievements" : "—", 10, "#D6B478"); Grid.SetColumn(boost, 3); row.Children.Add(boost);
                sectionBody.Children.Add(row);
            }
            section.Child = sectionBody;
            body.Children.Add(section);
        }

        var scroll = new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var card = new Border
        {
            MaxWidth = 980, MaxHeight = 760, Margin = new Thickness(35), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Background = new LinearGradientBrush(Color.FromRgb(9, 27, 22), Color.FromRgb(25, 22, 29), 35), BorderBrush = Ink("#806A45"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12), Child = scroll,
        };
        var overlay = new Grid { Name = "EmpireStatisticsOverlay", Background = new SolidColorBrush(Color.FromArgb(235, 3, 9, 8)) };
        overlay.Children.Add(card);
        Grid.SetRowSpan(overlay, Math.Max(1, root.RowDefinitions.Count));
        Panel.SetZIndex(overlay, 6000);
        close.Click += (_, _) => root.Children.Remove(overlay);
        root.Children.Add(overlay);
    }

    private static Border Metric(string caption, string value)
    {
        var stack = new StackPanel();
        stack.Children.Add(Text(caption, 9, "#B89B69", bold: true));
        stack.Children.Add(Text(value, 18, "#EFD5A0", bold: true, font: "Georgia", margin: new Thickness(0, 4, 0, 0)));
        return new Border { Background = Ink("#132820"), BorderBrush = Ink("#354A3E"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(11), Margin = new Thickness(4), Child = stack };
    }

    private static TextBlock Text(string value, double size, string color, bool bold = false, string? font = null, Thickness? margin = null) => new()
    {
        Text = value, FontSize = size, Foreground = Ink(color), FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
        FontFamily = font is null ? new FontFamily("Segoe UI") : new FontFamily(font), Margin = margin ?? new Thickness(0), TextWrapping = TextWrapping.Wrap,
    };

    private static SolidColorBrush Ink(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
