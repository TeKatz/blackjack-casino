using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class CasinoTableBackdrop : FrameworkElement
{
    internal static readonly DependencyProperty FeltProperty = DependencyProperty.Register(
        nameof(Felt), typeof(Brush), typeof(CasinoTableBackdrop),
        new FrameworkPropertyMetadata(Brushes.DarkGreen, FrameworkPropertyMetadataOptions.AffectsRender));

    internal static readonly DependencyProperty PhaseProperty = DependencyProperty.Register(
        nameof(Phase), typeof(double), typeof(CasinoTableBackdrop),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    internal static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
        nameof(Theme), typeof(TableThemePreference), typeof(CasinoTableBackdrop),
        new FrameworkPropertyMetadata(TableThemePreference.ClassicGreen, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Felt { get => (Brush)GetValue(FeltProperty); set => SetValue(FeltProperty, value); }
    public double Phase { get => (double)GetValue(PhaseProperty); set => SetValue(PhaseProperty, value); }
    public TableThemePreference Theme { get => (TableThemePreference)GetValue(ThemeProperty); set => SetValue(ThemeProperty, value); }

    private sealed record TrimColors(
        string Base, string RailA, string RailB, string Rim, string Inlay,
        string Stitch, string FeltEdge, string FaintLine, string WagerArc,
        string WagerMarker, string Letters, string Shoe, Color Glow);

    private TrimColors ColorsForTheme() => Theme switch
    {
        TableThemePreference.MidnightBlue => new(
            "#06101B", "#334359", "#121F33", "#8EBDD4", "#13283C",
            "#7D9FB6", "#355A75", "#09DAEFFF", "#66BCE5FF",
            "#4AA0C2DE", "#ADCEE1", "#2E4B65", Color.FromArgb(49, 116, 190, 223)),
        TableThemePreference.Burgundy => new(
            "#160A11", "#5A333E", "#26111D", "#CCA188", "#301822",
            "#BF8D81", "#794353", "#09F7D4C6", "#6AE5B39D",
            "#4AE3AA9C", "#D8B1A6", "#59313D", Color.FromArgb(47, 214, 125, 131)),
        _ => new(
            "#080F10", "#3F2A20", "#171414", "#96724B", "#101E1A",
            "#B19862", "#364E40", "#06F4EFDB", "#45D7BB7C",
            "#38D9BD85", "#BCAD7E", "#283D38", Color.FromArgb(44, 180, 211, 183)),
    };

    protected override void OnRender(DrawingContext d)
    {
        if (ActualWidth < 1 || ActualHeight < 1) return;
        double w = ActualWidth, h = ActualHeight;
        TrimColors t = ColorsForTheme();

        d.DrawRectangle(Ink(t.Base), null, new Rect(0, 0, w, h));
        d.DrawRoundedRectangle(new LinearGradientBrush(Parse(t.RailA), Parse(t.RailB), 90),
            new Pen(Ink(t.Rim), 1), new Rect(12, 12, w - 24, h - 24), 52, 52);
        for (int i = 0; i < 5; i++)
            d.DrawRoundedRectangle(null, new Pen(Ink(i % 2 == 0 ? t.WagerArc : "#28000000"), 1),
                new Rect(17 + i * 2, 17 + i * 2, w - 34 - i * 4, h - 34 - i * 4), 47 - i, 47 - i);
        d.DrawRoundedRectangle(Ink(t.Inlay), new Pen(Ink(t.Stitch), 1.4), new Rect(30, 30, w - 60, h - 60), 36, 36);
        d.DrawRoundedRectangle(Felt, new Pen(Ink(t.FeltEdge), 1), new Rect(39, 39, w - 78, h - 78), 28, 28);
        var glow = new RadialGradientBrush(t.Glow, System.Windows.Media.Colors.Transparent)
        {
            Center = new Point(.5, .37), GradientOrigin = new Point(.5, .37), RadiusX = .65, RadiusY = .65,
        };
        d.DrawRoundedRectangle(glow, null, new Rect(40, 40, w - 80, h - 80), 28, 28);
        d.PushClip(new RectangleGeometry(new Rect(42, 42, w - 84, h - 84), 26, 26));
        for (int y = 44; y < h - 40; y += 9)
            d.DrawLine(new Pen(Ink(t.FaintLine), .4), new Point(42, y), new Point(w - 42, y));
        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            g.BeginFigure(new Point(w * .14, h * .66), false, false);
            g.BezierTo(new(w * .32, h * .39), new(w * .68, h * .39), new(w * .86, h * .66), true, false);
        }
        d.DrawGeometry(null, new Pen(Ink(t.WagerArc), 1.2)
        {
            DashStyle = new DashStyle(new[] { 7d, 5d }, 0),
        }, arc);
        foreach (double x in new[] { .23, .5, .77 })
            d.DrawEllipse(null, new Pen(Ink(t.WagerMarker), 1), new Point(w * x, h * .68), 39, 23);
        DrawText(d, "THE MIDNIGHT CLUB", 13, new Point(w * .5, h - 29), t.Letters);

        double shoe = w - 170;
        for (int i = 0; i < 4; i++)
            d.DrawRoundedRectangle(Ink("#D5CDB7"), new Pen(Ink("#786B58"), .7),
                new Rect(shoe + i * 2, 68 - i * 2, 46, 65), 4, 4);
        d.DrawRoundedRectangle(Ink(t.Shoe), new Pen(Ink(t.Stitch), 1),
            new Rect(shoe + 7, 63, 40, 58), 3, 3);
        DrawText(d, "♠", 23, new Point(shoe + 27, 77), t.Letters);
        for (int i = 0; i < 18; i++)
        {
            double x = 80 + (i * 137 + Math.Sin(Phase * .35 + i) * 12) % Math.Max(1, w - 160);
            double y = 70 + (i * 97 + Phase * 3) % Math.Max(1, h - 140);
            d.DrawEllipse(Ink(t.WagerMarker), null, new Point(x, y), 1, 1);
        }
        d.Pop();
    }

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static Brush Ink(string hex) => new SolidColorBrush(Parse(hex));
    private static void DrawText(DrawingContext d, string label, double size, Point centre, string hex)
    {
        var formatted = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Georgia"), size, Ink(hex), 1);
        d.DrawText(formatted, new Point(centre.X - formatted.Width / 2, centre.Y));
    }
}
