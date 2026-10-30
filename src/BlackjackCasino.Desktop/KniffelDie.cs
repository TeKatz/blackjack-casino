using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Resolution-independent ivory die. Game state never comes from this drawing.</summary>
internal sealed class KniffelDie : FrameworkElement
{
    private int value;
    private bool held;
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
        var fill = new LinearGradientBrush(Color.FromRgb(255, 252, 238), Color.FromRgb(215, 203, 178), 75);
        dc.DrawRoundedRectangle(fill, new Pen(new SolidColorBrush(held ? Color.FromRgb(109, 226, 172) : Color.FromRgb(189, 163, 110)), held ? 3 : 1.5), face, 14, 14);
        dc.DrawRoundedRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), 1),
            new Rect(face.X + 4, face.Y + 4, size - 8, size - 8), 11, 11);
        if (value == 0)
        {
            var text = new FormattedText("?", System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Georgia"), size * .45, Brushes.SlateGray, VisualTreeHelper.GetDpi(this).PixelsPerDip);
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
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(45, 55, 48)), null, centre, size * .072, size * .072);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)), null,
                new Point(centre.X, centre.Y - 1), size * .045, size * .035);
        }
    }
}
