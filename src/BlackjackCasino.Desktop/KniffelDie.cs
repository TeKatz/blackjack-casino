using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Resolution-independent ivory die. Game state never comes from this drawing.</summary>
internal sealed class KniffelDie : FrameworkElement
{
    private int value;
    private bool held;
    private string visualVariant = "dice-ivory";
    internal string VisualVariant { get => visualVariant; set { visualVariant = value; InvalidateVisual(); } }
    private (Color Top, Color Bottom, Color Pips, Color Edge) Palette => visualVariant switch
    {
        "dice-obsidian" => (Rgb("#35404A"), Rgb("#0C1119"), Rgb("#F4D483"), Rgb("#C4A05B")),
        "dice-jade" => (Rgb("#4A987D"), Rgb("#123E36"), Rgb("#F5EDD5"), Rgb("#97C7A4")),
        "dice-violet" => (Rgb("#9471B7"), Rgb("#35224D"), Rgb("#FFE5A5"), Rgb("#CBA7E7")),
        "dice-ice" => (Rgb("#B7DAEE"), Rgb("#4D7594"), Rgb("#132F49"), Rgb("#D5EDFA")),
        "dice-ruby" => (Rgb("#AE5668"), Rgb("#491F32"), Rgb("#FFF0D2"), Rgb("#E1A287")),
        "dice-aurora" => (Rgb("#348B88"), Rgb("#663D84"), Rgb("#E1FFF4"), Rgb("#85E6D0")),
        _ => (Rgb("#FFFCEE"), Rgb("#D7CBB2"), Rgb("#2D3730"), Rgb("#BDA36E")),
    };
    private static Color Rgb(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    internal int Value { get => value; set { this.value = value; InvalidateVisual(); } }
    internal bool Held { get => held; set { held = value; InvalidateVisual(); } }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double size = Math.Min(ActualWidth, ActualHeight) - 8;
        if (size <= 0) return;
        var face = new Rect((ActualWidth - size) / 2, (ActualHeight - size) / 2, size, size);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)), null,
            new Rect(face.X + 2, face.Y + 5, size, size), 15, 15);
        var palette = Palette;
        var fill = new LinearGradientBrush(palette.Top, palette.Bottom, 75);
        dc.DrawRoundedRectangle(fill, new Pen(new SolidColorBrush(held ? Color.FromRgb(109, 226, 172) : palette.Edge), held ? 3 : 1.5), face, 14, 14);
        dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), 1),
            new Rect(face.X + 4, face.Y + 4, size - 8, size - 8), 11, 11);
        if (value == 0)
        {
            var text = new FormattedText("?", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Georgia"), size * .45, new SolidColorBrush(palette.Pips), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(text, new Point(face.X + (size - text.Width) / 2, face.Y + (size - text.Height) / 2));
            return;
        }
        if (value % 2 == 1) Pip(.5, .5);
        if (value >= 2) { Pip(.26, .26); Pip(.74, .74); }
        if (value >= 4) { Pip(.74, .26); Pip(.26, .74); }
        if (value == 6) { Pip(.26, .5); Pip(.74, .5); }
        void Pip(double x, double y)
        {
            var centre = new Point(face.X + size * x, face.Y + size * y);
            dc.DrawEllipse(new SolidColorBrush(palette.Pips), null, centre, size * .072, size * .072);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), null,
                new Point(centre.X, centre.Y - 1), size * .045, size * .035);
        }
    }
}
