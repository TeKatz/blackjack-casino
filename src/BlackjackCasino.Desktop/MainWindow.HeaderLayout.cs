using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private void InitializeHeaderLayout()
    {
        // Keep the existing named controls, bindings and click handlers. Only the rail is fitted.
        var rail = new Grid { Name = "CasinoHeaderRail", VerticalAlignment = VerticalAlignment.Center };
        for (int i = 0; i < 4; i++) rail.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        foreach (FrameworkElement element in CasinoHeader.Children.OfType<FrameworkElement>().Where(e => Grid.GetColumn(e) > 0).ToArray())
        {
            int column = Grid.GetColumn(element) - 1;
            CasinoHeader.Children.Remove(element);
            Grid.SetColumn(element, column); rail.Children.Add(element);
        }
        CasinoHeader.ColumnDefinitions.Clear();
        CasinoHeader.ColumnDefinitions.Add(new ColumnDefinition { MinWidth = 170 });
        CasinoHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var fit = new Viewbox { Name = "CasinoHeaderRailFit", Child = rail, Stretch = Stretch.Uniform,
            StretchDirection = StretchDirection.DownOnly, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        void Fit() => fit.MaxWidth = Math.Max(1, CasinoHeader.ActualWidth - 180);
        CasinoHeader.SizeChanged += (_, _) => Fit();
        Grid.SetColumn(fit, 1); CasinoHeader.Children.Add(fit); Fit();
        // Reserve stable number widths; live ticks must not shift or remeasure the neighboring actions.
        EmpireHeaderBalance.Width = 140;
        EmpireHeaderBalance.TextTrimming = TextTrimming.CharacterEllipsis;
    }
}
