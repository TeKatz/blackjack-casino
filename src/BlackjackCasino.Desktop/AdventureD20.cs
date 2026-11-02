using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BlackjackCasino.Desktop;

internal sealed class AdventureD20 : FrameworkElement
{
    private int value;
    private bool active;
    private string visualVariant = "dice-ivory";

    internal AdventureD20()
    {
        Cursor = CasinoCursors.Arrow;
        ForceCursor = true;
        MouseEnter += (_, _) => Cursor = active ? CasinoCursors.Hand : CasinoCursors.Arrow;
    }

    internal int Value
    {
        get => value;
        set
        {
            this.value = Math.Clamp(value, 0, 20);
            InvalidateVisual();
        }
    }

    internal bool Active
    {
        get => active;
        set
        {
            active = value;
            Cursor = active ? CasinoCursors.Hand : CasinoCursors.Arrow;
            InvalidateVisual();
        }
    }

    internal string VisualVariant
    {
        get => visualVariant;
        set
        {
            visualVariant = string.IsNullOrWhiteSpace(value) ? "dice-ivory" : value;
            InvalidateVisual();
        }
    }

    // AdventureView's original prototype assigned a continuously accumulating RotateTransform while rolling.
    // Keep decorative idle rotations, but intercept that legacy active-roll transform and replace it with a
    // bounded tabletop wobble. This deliberately prevents the die from orbiting around its top-left corner.
    internal new Transform RenderTransform
    {
        get => base.RenderTransform;
        set
        {
            if (active && value is RotateTransform)
            {
                BeginControlledRollMotion();
                return;
            }

            base.RenderTransform = value;
        }
    }

    private (Color Top, Color Bottom, Color Number, Color Edge) Palette => visualVariant switch
    {
        "dice-obsidian" => (Rgb("#35404A"), Rgb("#0C1119"), Rgb("#F4D483"), Rgb("#C4A05B")),
        "dice-jade" => (Rgb("#4A987D"), Rgb("#123E36"), Rgb("#F5EDD5"), Rgb("#97C7A4")),
        "dice-violet" => (Rgb("#9471B7"), Rgb("#35224D"), Rgb("#FFE5A5"), Rgb("#CBA7E7")),
        "dice-ice" => (Rgb("#B7DAEE"), Rgb("#4D7594"), Rgb("#132F49"), Rgb("#D5EDFA")),
        "dice-ruby" => (Rgb("#AE5668"), Rgb("#491F32"), Rgb("#FFF0D2"), Rgb("#E1A287")),
        "dice-aurora" => (Rgb("#348B88"), Rgb("#663D84"), Rgb("#E1FFF4"), Rgb("#85E6D0")),
        _ => (Rgb("#FFFCEE"), Rgb("#D7CBB2"), Rgb("#2D3730"), Rgb("#BDA36E")),
    };

    private void BeginControlledRollMotion()
    {
        RenderTransformOrigin = new Point(.5, .5);
        var scale = new ScaleTransform(1, 1);
        var rotation = new RotateTransform(0);
        var group = new TransformGroup();
        group.Children.Add(scale);
        group.Children.Add(rotation);
        base.RenderTransform = group;

        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var turn = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(560) };
        turn.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero), ease));
        turn.KeyFrames.Add(new EasingDoubleKeyFrame(-6.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80)), ease));
        turn.KeyFrames.Add(new EasingDoubleKeyFrame(9.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(190)), ease));
        turn.KeyFrames.Add(new EasingDoubleKeyFrame(-4.2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(320)), ease));
        turn.KeyFrames.Add(new EasingDoubleKeyFrame(2.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(440)), ease));
        turn.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(560)), ease));

        var pulse = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(560) };
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero), ease));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(.955, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(80)), ease));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1.045, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(205)), ease));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(.985, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(370)), ease));
        pulse.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(560)), ease));

        rotation.BeginAnimation(RotateTransform.AngleProperty, turn, HandoffBehavior.SnapshotAndReplace);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse, HandoffBehavior.SnapshotAndReplace);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse, HandoffBehavior.SnapshotAndReplace);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double size = Math.Min(ActualWidth, ActualHeight) - 10;
        if (size <= 0) return;
        double x = (ActualWidth - size) / 2;
        double y = (ActualHeight - size) / 2;
        Point P(double px, double py) => new(x + px * size, y + py * size);
        Point[] outer = [P(.50, .03), P(.83, .16), P(.97, .48), P(.82, .84), P(.50, .97), P(.18, .84), P(.03, .48), P(.17, .16)];

        var geometry = new StreamGeometry();
        using (StreamGeometryContext context = geometry.Open())
        {
            context.BeginFigure(outer[0], true, true);
            context.PolyLineTo(outer[1..], true, true);
        }
        geometry.Freeze();

        var palette = Palette;
        var fill = new LinearGradientBrush(palette.Top, palette.Bottom, 70);
        var edge = new Pen(new SolidColorBrush(active ? Rgb("#E8C982") : palette.Edge), active ? 3 : 1.8);
        dc.DrawGeometry(fill, edge, geometry);

        var facetPen = new Pen(new SolidColorBrush(Color.FromArgb(95, palette.Number.R, palette.Number.G, palette.Number.B)), 1);
        Point centre = P(.50, .52);
        foreach (Point point in outer) dc.DrawLine(facetPen, centre, point);
        dc.DrawLine(facetPen, outer[7], outer[2]);
        dc.DrawLine(facetPen, outer[6], outer[1]);
        dc.DrawLine(facetPen, outer[5], outer[2]);
        dc.DrawLine(facetPen, outer[3], outer[6]);

        string text = value == 0 ? "20" : value.ToString(CultureInfo.InvariantCulture);
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Georgia"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
            size * (text.Length == 1 ? .25 : .21), new SolidColorBrush(palette.Number), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, new Point(centre.X - formatted.Width / 2, centre.Y - formatted.Height / 2 - size * .01));

        if (active)
            dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(70, 232, 201, 130)), 5), geometry);
    }

    private static Color Rgb(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
