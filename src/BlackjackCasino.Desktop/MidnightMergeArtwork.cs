using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

/// <summary>Original vector chip artwork, shared by the board, order cards and lobby tile.</summary>
internal sealed class MergeChipArtwork : FrameworkElement
{
    private readonly MergeItemDefinition definition;
    internal MergeChipArtwork(MergeItemDefinition definition, double size = 64)
    { this.definition = definition; Width = size; Height = size; IsHitTestVisible = false; }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double scale = ActualWidth / 64;
        dc.PushTransform(new ScaleTransform(scale, scale));
        var ink = MidnightMergeView.Ink(definition.Visual.Color);
        for (int layer = definition.Visual.StackCount - 1; layer >= 0; layer--)
        {
            double y = 29 + layer * 4;
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)), null, new Point(33, y + 5), 27, 23);
            dc.DrawEllipse(MidnightMergeView.Ink("#14262A"), new Pen(ink, 2), new Point(32, y), 25, 23);
            dc.DrawEllipse(ink, null, new Point(32, y - 1), 22, 20);
            dc.DrawEllipse(MidnightMergeView.Ink("#17282C"), new Pen(MidnightMergeView.Ink("#F4E6C7"), .8), new Point(32, y - 1), 16, 15);
            for (int n = 0; n < 8; n++)
            {
                double a = n * Math.PI / 4;
                dc.DrawLine(new Pen(MidnightMergeView.Ink("#F9EDD6"), 3),
                    new Point(32 + Math.Cos(a) * 18.5, y - 1 + Math.Sin(a) * 17),
                    new Point(32 + Math.Cos(a) * 23, y - 1 + Math.Sin(a) * 21));
            }
        }
        var text = new FormattedText(definition.Visual.Mark, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Georgia"), 16, ink, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(32 - text.Width / 2, 28 - text.Height / 2));
        dc.Pop();
    }
}

internal sealed class MergeLobbyArtwork : FrameworkElement
{
    private readonly long points;
    internal MergeLobbyArtwork(long points = 100) { this.points = points; Width = 300; Height = 112; IsHitTestVisible = false; }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var gold = MidnightMergeView.Ink(points >= 50 ? "#D5B471" : "#625B4A");
        dc.DrawRoundedRectangle(MidnightMergeView.Ink("#121D22"), new Pen(gold, 1), new Rect(0, 0, 300, 112), 8, 8);
        dc.DrawRectangle(MidnightMergeView.Ink(points >= 20 ? "#253B3D" : "#242724"), null, new Rect(18, 16, 264, 77));
        for (int i = 0; i < 4; i++)
        {
            double x = 32 + i * 72;
            dc.DrawRectangle(gold, null, new Rect(x, 20, 8, 68));
            dc.DrawRectangle(gold, null, new Rect(x - 4, 17, 16, 4));
            dc.DrawRectangle(gold, null, new Rect(x - 4, 87, 16, 4));
        }
        dc.DrawRoundedRectangle(MidnightMergeView.Ink(points >= 100 ? "#5F333F" : "#323638"), new Pen(gold, 1), new Rect(106, 43, 88, 47), 5, 5);
        dc.DrawLine(new Pen(gold, 1), new Point(150, 20), new Point(150, 35));
        dc.DrawEllipse(points >= 50 ? MidnightMergeView.Ink("#F2D698") : MidnightMergeView.Ink("#4A473D"), null, new Point(150, 35), 25, 5);
        if (points < 20)
            for (int i = 0; i < 7; i++) dc.DrawEllipse(MidnightMergeView.Ink("#777364"), null, new Point(38 + i * 37, 98), 9, 1);
        else dc.DrawLine(new Pen(gold, 1), new Point(15, 98), new Point(285, 98));
    }
}
