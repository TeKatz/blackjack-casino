using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

/// <summary>Original vector chip artwork, shared by the board, order cards and restoration scene.</summary>
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

/// <summary>
/// Wide lobby panorama. Restoration milestones deliberately alter large scene layers rather than a tiny status tile,
/// so renovation is the visual reward for the merge loop.
/// </summary>
internal sealed class MergeLobbyArtwork : FrameworkElement
{
    private const double DesignWidth = 1400;
    private const double DesignHeight = 235;
    private readonly long points;

    internal MergeLobbyArtwork(long points = 100, double width = DesignWidth, double height = DesignHeight)
    {
        this.points = Math.Max(0, points);
        Width = width;
        Height = height;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        double scale = Math.Min(ActualWidth / DesignWidth, ActualHeight / DesignHeight);
        double offsetX = (ActualWidth - DesignWidth * scale) / 2;
        double offsetY = (ActualHeight - DesignHeight * scale) / 2;
        dc.PushTransform(new TranslateTransform(offsetX, offsetY));
        dc.PushTransform(new ScaleTransform(scale, scale));

        bool cleaned = points >= 20;
        bool lit = points >= 50;
        bool ready = points >= 100;
        Brush gold = MidnightMergeView.Ink(lit ? "#D9B86F" : cleaned ? "#847557" : "#565247");
        Brush wall = MidnightMergeView.Ink(cleaned ? "#1D383B" : "#282C2B");
        Brush wallDark = MidnightMergeView.Ink(cleaned ? "#14292D" : "#202321");
        Brush floor = MidnightMergeView.Ink(cleaned ? "#192B2D" : "#242622");

        dc.DrawRoundedRectangle(MidnightMergeView.Ink("#0E171C"), new Pen(gold, 1.5), new Rect(1, 1, DesignWidth - 2, DesignHeight - 2), 12, 12);
        dc.DrawRectangle(wall, null, new Rect(18, 16, DesignWidth - 36, 144));
        dc.DrawRectangle(wallDark, null, new Rect(18, 16, DesignWidth - 36, 24));
        dc.DrawRectangle(floor, null, new Rect(18, 160, DesignWidth - 36, 57));

        // Repeating art-deco wall panels and columns make the panorama read as a real room.
        for (int i = 0; i < 7; i++)
        {
            double panelX = 42 + i * 198;
            dc.DrawRoundedRectangle(MidnightMergeView.Ink(cleaned ? "#203F42" : "#30322E"), new Pen(MidnightMergeView.Ink(cleaned ? "#38595A" : "#42433D"), 1),
                new Rect(panelX, 50, 158, 86), 5, 5);
            dc.DrawLine(new Pen(gold, 1), new Point(panelX + 79, 58), new Point(panelX + 79, 128));
            dc.DrawLine(new Pen(gold, .8), new Point(panelX + 20, 93), new Point(panelX + 138, 93));
        }
        for (int i = 0; i < 8; i++)
        {
            double x = 24 + i * 194;
            dc.DrawRectangle(MidnightMergeView.Ink("#132327"), new Pen(gold, 1), new Rect(x, 34, 13, 130));
            dc.DrawRectangle(gold, null, new Rect(x - 4, 31, 21, 5));
            dc.DrawRectangle(gold, null, new Rect(x - 4, 160, 21, 5));
        }

        // Central reception desk becomes the focal point at the final restoration stage.
        Brush desk = MidnightMergeView.Ink(ready ? "#673845" : cleaned ? "#3B3635" : "#333431");
        dc.DrawRoundedRectangle(desk, new Pen(gold, 1.5), new Rect(545, 115, 310, 72), 8, 8);
        dc.DrawRectangle(MidnightMergeView.Ink(ready ? "#522C38" : "#2C3030"), null, new Rect(565, 133, 270, 39));
        dc.DrawLine(new Pen(gold, 2), new Point(700, 116), new Point(700, 185));
        if (ready)
        {
            var label = new FormattedText("MIDNIGHT CLUB", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Georgia"), 18, MidnightMergeView.Ink("#F0D99E"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(label, new Point(700 - label.Width / 2, 141 - label.Height / 2));
        }

        // Chandelier / restored lighting.
        dc.DrawLine(new Pen(gold, 2), new Point(700, 18), new Point(700, 57));
        dc.DrawEllipse(MidnightMergeView.Ink(lit ? "#F1D798" : "#4A473E"), new Pen(gold, 1), new Point(700, 61), 65, 9);
        for (int i = -2; i <= 2; i++)
        {
            double x = 700 + i * 24;
            dc.DrawLine(new Pen(gold, 1), new Point(x, 62), new Point(x, 79));
            dc.DrawEllipse(MidnightMergeView.Ink(lit ? "#FFE4A1" : "#49483F"), null, new Point(x, 82), 7, 7);
        }
        if (lit)
        {
            var glow = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(65, 255, 220, 144), 0),
                    new GradientStop(Color.FromArgb(0, 255, 220, 144), 1),
                }
            };
            dc.DrawEllipse(glow, null, new Point(700, 96), 235, 105);
        }

        // Lounge furniture arrives with the final stage.
        if (ready)
        {
            DrawSofa(dc, 270, 147, gold);
            DrawSofa(dc, 962, 147, gold);
            dc.DrawEllipse(MidnightMergeView.Ink("#755143"), new Pen(gold, 1), new Point(410, 184), 55, 13);
            dc.DrawEllipse(MidnightMergeView.Ink("#755143"), new Pen(gold, 1), new Point(990, 184), 55, 13);
            DrawPlant(dc, 126, 143, gold);
            DrawPlant(dc, 1274, 143, gold);
            dc.DrawRoundedRectangle(MidnightMergeView.Ink("#4A2D36"), new Pen(gold, 1), new Rect(470, 190, 460, 21), 9, 9);
        }
        else if (cleaned)
        {
            dc.DrawLine(new Pen(gold, 1), new Point(54, 204), new Point(1346, 204));
        }

        // Dust, debris and dimness disappear as the very first restoration milestone is reached.
        if (!cleaned)
        {
            for (int i = 0; i < 15; i++)
            {
                double x = 55 + i * 91;
                dc.DrawEllipse(MidnightMergeView.Ink("#716D5F"), null, new Point(x, 205 - (i % 3) * 5), 15 + i % 4, 2.5);
            }
            dc.DrawLine(new Pen(MidnightMergeView.Ink("#625F54"), 4), new Point(1110, 165), new Point(1160, 202));
            dc.DrawLine(new Pen(MidnightMergeView.Ink("#625F54"), 3), new Point(1140, 165), new Point(1105, 202));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)), null, new Rect(18, 16, DesignWidth - 36, 201));
        }

        dc.Pop();
        dc.Pop();
    }

    private static void DrawSofa(DrawingContext dc, double x, double y, Brush gold)
    {
        Brush fabric = MidnightMergeView.Ink("#57313A");
        dc.DrawRoundedRectangle(fabric, new Pen(gold, 1), new Rect(x, y, 166, 42), 11, 11);
        dc.DrawRoundedRectangle(MidnightMergeView.Ink("#673A45"), new Pen(gold, .8), new Rect(x + 14, y - 22, 138, 35), 10, 10);
        dc.DrawLine(new Pen(gold, .8), new Point(x + 83, y - 18), new Point(x + 83, y + 7));
    }

    private static void DrawPlant(DrawingContext dc, double x, double y, Brush gold)
    {
        dc.DrawRoundedRectangle(MidnightMergeView.Ink("#68493A"), new Pen(gold, .8), new Rect(x - 19, y + 31, 38, 32), 5, 5);
        for (int i = -2; i <= 2; i++)
        {
            double dx = i * 8;
            dc.DrawEllipse(MidnightMergeView.Ink("#426451"), null, new Point(x + dx, y + 17 - Math.Abs(i) * 5), 12, 25);
        }
    }
}
