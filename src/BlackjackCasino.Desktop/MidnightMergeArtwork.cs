using System.Globalization;
using System.Windows;
using System.Windows.Media;
using BlackjackCasino.Core.MidnightMerge;

namespace BlackjackCasino.Desktop;

/* Lobby fallback artwork must not depend on the retired WPF game view. */
internal static class MergeArtworkBrushes
{
    internal static SolidColorBrush Ink(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));
}


/// <summary>Original vector merge-item artwork shared by the board, order cards and restoration scenes.</summary>
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
        if (definition.LineId == "bar-drinks")
        {
            DrawBarItem(dc);
            dc.Pop();
            return;
        }

        var ink = MergeArtworkBrushes.Ink(definition.Visual.Color);
        for (int layer = definition.Visual.StackCount - 1; layer >= 0; layer--)
        {
            double y = 29 + layer * 4;
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)), null, new Point(33, y + 5), 27, 23);
            dc.DrawEllipse(MergeArtworkBrushes.Ink("#14262A"), new Pen(ink, 2), new Point(32, y), 25, 23);
            dc.DrawEllipse(ink, null, new Point(32, y - 1), 22, 20);
            dc.DrawEllipse(MergeArtworkBrushes.Ink("#17282C"), new Pen(MergeArtworkBrushes.Ink("#F4E6C7"), .8), new Point(32, y - 1), 16, 15);
            for (int n = 0; n < 8; n++)
            {
                double a = n * Math.PI / 4;
                dc.DrawLine(new Pen(MergeArtworkBrushes.Ink("#F9EDD6"), 3),
                    new Point(32 + Math.Cos(a) * 18.5, y - 1 + Math.Sin(a) * 17),
                    new Point(32 + Math.Cos(a) * 23, y - 1 + Math.Sin(a) * 21));
            }
        }
        var text = new FormattedText(definition.Visual.Mark, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Georgia"), 16, ink, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(text, new Point(32 - text.Width / 2, 28 - text.Height / 2));
        dc.Pop();
    }

    private void DrawBarItem(DrawingContext dc)
    {
        Brush accent = MergeArtworkBrushes.Ink(definition.Visual.Color);
        Pen gold = new(MergeArtworkBrushes.Ink("#E8D7A8"), 1.4);
        Brush glass = new SolidColorBrush(Color.FromArgb(110, 214, 235, 232));
        int tier = definition.Tier;

        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(55, 0, 0, 0)), null, new Point(32, 54), 22, 5);
        if (tier <= 2)
        {
            dc.DrawRoundedRectangle(glass, gold, new Rect(17, 15, 30, 38), 5, 5);
            dc.DrawRectangle(accent, null, new Rect(19, tier == 1 ? 42 : 31, 26, tier == 1 ? 8 : 19));
            if (tier == 2)
            {
                dc.DrawRectangle(MergeArtworkBrushes.Ink("#EAF5F2"), new Pen(MergeArtworkBrushes.Ink("#B5D6D1"), .7), new Rect(22, 24, 8, 8));
                dc.DrawRectangle(MergeArtworkBrushes.Ink("#EAF5F2"), new Pen(MergeArtworkBrushes.Ink("#B5D6D1"), .7), new Rect(33, 27, 8, 8));
            }
        }
        else if (tier <= 5)
        {
            var bowl = new StreamGeometry();
            using (StreamGeometryContext ctx = bowl.Open())
            {
                ctx.BeginFigure(new Point(12, 16), true, true);
                ctx.LineTo(new Point(52, 16), true, false);
                ctx.LineTo(new Point(32, 38), true, false);
            }
            dc.DrawGeometry(accent, gold, bowl);
            dc.DrawLine(gold, new Point(32, 38), new Point(32, 51));
            dc.DrawLine(new Pen(MergeArtworkBrushes.Ink("#E8D7A8"), 2), new Point(20, 52), new Point(44, 52));
            if (tier >= 4) dc.DrawEllipse(MergeArtworkBrushes.Ink("#A5CE91"), null, new Point(44, 18), 5, 3);
            if (tier >= 5) dc.DrawEllipse(MergeArtworkBrushes.Ink("#E7C970"), null, new Point(37, 24), 3, 3);
        }
        else
        {
            dc.DrawRoundedRectangle(accent, gold, new Rect(20, 20, 24, 34), 5, 5);
            dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#24383A"), gold, new Rect(26, 8, 12, 17), 3, 3);
            dc.DrawRectangle(MergeArtworkBrushes.Ink("#F2E2C5"), null, new Rect(23, 34, 18, 10));
            if (tier >= 7) dc.DrawLine(new Pen(MergeArtworkBrushes.Ink("#D8B46C"), 2), new Point(23, 30), new Point(41, 30));
            if (tier >= 8) dc.DrawEllipse(MergeArtworkBrushes.Ink("#F5E6AA"), new Pen(MergeArtworkBrushes.Ink("#6E5631"), 1), new Point(32, 39), 5, 5);
        }

        var text = new FormattedText(definition.Visual.Mark, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Georgia"), definition.Visual.Mark.Length > 1 ? 9 : 12, MergeArtworkBrushes.Ink("#132327"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#F0DFC0"), null, new Rect(25, 45, 14, 12), 4, 4);
        dc.DrawText(text, new Point(32 - text.Width / 2, 51 - text.Height / 2));
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
        Brush gold = MergeArtworkBrushes.Ink(lit ? "#D9B86F" : cleaned ? "#847557" : "#565247");
        Brush wall = MergeArtworkBrushes.Ink(cleaned ? "#1D383B" : "#282C2B");
        Brush wallDark = MergeArtworkBrushes.Ink(cleaned ? "#14292D" : "#202321");
        Brush floor = MergeArtworkBrushes.Ink(cleaned ? "#192B2D" : "#242622");

        dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#0E171C"), new Pen(gold, 1.5), new Rect(1, 1, DesignWidth - 2, DesignHeight - 2), 12, 12);
        dc.DrawRectangle(wall, null, new Rect(18, 16, DesignWidth - 36, 144));
        dc.DrawRectangle(wallDark, null, new Rect(18, 16, DesignWidth - 36, 24));
        dc.DrawRectangle(floor, null, new Rect(18, 160, DesignWidth - 36, 57));

        for (int i = 0; i < 7; i++)
        {
            double panelX = 42 + i * 198;
            dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink(cleaned ? "#203F42" : "#30322E"), new Pen(MergeArtworkBrushes.Ink(cleaned ? "#38595A" : "#42433D"), 1),
                new Rect(panelX, 50, 158, 86), 5, 5);
            dc.DrawLine(new Pen(gold, 1), new Point(panelX + 79, 58), new Point(panelX + 79, 128));
            dc.DrawLine(new Pen(gold, .8), new Point(panelX + 20, 93), new Point(panelX + 138, 93));
        }
        for (int i = 0; i < 8; i++)
        {
            double x = 24 + i * 194;
            dc.DrawRectangle(MergeArtworkBrushes.Ink("#132327"), new Pen(gold, 1), new Rect(x, 34, 13, 130));
            dc.DrawRectangle(gold, null, new Rect(x - 4, 31, 21, 5));
            dc.DrawRectangle(gold, null, new Rect(x - 4, 160, 21, 5));
        }

        Brush desk = MergeArtworkBrushes.Ink(ready ? "#673845" : cleaned ? "#3B3635" : "#333431");
        dc.DrawRoundedRectangle(desk, new Pen(gold, 1.5), new Rect(545, 115, 310, 72), 8, 8);
        dc.DrawRectangle(MergeArtworkBrushes.Ink(ready ? "#522C38" : "#2C3030"), null, new Rect(565, 133, 270, 39));
        dc.DrawLine(new Pen(gold, 2), new Point(700, 116), new Point(700, 185));
        if (ready)
        {
            var label = new FormattedText("MIDNIGHT CLUB", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Georgia"), 18, MergeArtworkBrushes.Ink("#F0D99E"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(label, new Point(700 - label.Width / 2, 141 - label.Height / 2));
        }

        dc.DrawLine(new Pen(gold, 2), new Point(700, 18), new Point(700, 57));
        dc.DrawEllipse(MergeArtworkBrushes.Ink(lit ? "#F1D798" : "#4A473E"), new Pen(gold, 1), new Point(700, 61), 65, 9);
        for (int i = -2; i <= 2; i++)
        {
            double x = 700 + i * 24;
            dc.DrawLine(new Pen(gold, 1), new Point(x, 62), new Point(x, 79));
            dc.DrawEllipse(MergeArtworkBrushes.Ink(lit ? "#FFE4A1" : "#49483F"), null, new Point(x, 82), 7, 7);
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

        if (ready)
        {
            DrawSofa(dc, 270, 147, gold);
            DrawSofa(dc, 962, 147, gold);
            dc.DrawEllipse(MergeArtworkBrushes.Ink("#755143"), new Pen(gold, 1), new Point(410, 184), 55, 13);
            dc.DrawEllipse(MergeArtworkBrushes.Ink("#755143"), new Pen(gold, 1), new Point(990, 184), 55, 13);
            DrawPlant(dc, 126, 143, gold);
            DrawPlant(dc, 1274, 143, gold);
            dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#4A2D36"), new Pen(gold, 1), new Rect(470, 190, 460, 21), 9, 9);
        }
        else if (cleaned)
        {
            dc.DrawLine(new Pen(gold, 1), new Point(54, 204), new Point(1346, 204));
        }

        if (!cleaned)
        {
            for (int i = 0; i < 15; i++)
            {
                double x = 55 + i * 91;
                dc.DrawEllipse(MergeArtworkBrushes.Ink("#716D5F"), null, new Point(x, 205 - (i % 3) * 5), 15 + i % 4, 2.5);
            }
            dc.DrawLine(new Pen(MergeArtworkBrushes.Ink("#625F54"), 4), new Point(1110, 165), new Point(1160, 202));
            dc.DrawLine(new Pen(MergeArtworkBrushes.Ink("#625F54"), 3), new Point(1140, 165), new Point(1105, 202));
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(65, 0, 0, 0)), null, new Rect(18, 16, DesignWidth - 36, 201));
        }

        dc.Pop();
        dc.Pop();
    }

    private static void DrawSofa(DrawingContext dc, double x, double y, Brush gold)
    {
        Brush fabric = MergeArtworkBrushes.Ink("#57313A");
        dc.DrawRoundedRectangle(fabric, new Pen(gold, 1), new Rect(x, y, 166, 42), 11, 11);
        dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#673A45"), new Pen(gold, .8), new Rect(x + 14, y - 22, 138, 35), 10, 10);
        dc.DrawLine(new Pen(gold, .8), new Point(x + 83, y - 18), new Point(x + 83, y + 7));
    }

    private static void DrawPlant(DrawingContext dc, double x, double y, Brush gold)
    {
        dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#68493A"), new Pen(gold, .8), new Rect(x - 19, y + 31, 38, 32), 5, 5);
        for (int i = -2; i <= 2; i++)
        {
            double dx = i * 8;
            dc.DrawEllipse(MergeArtworkBrushes.Ink("#426451"), null, new Point(x + dx, y + 17 - Math.Abs(i) * 5), 12, 25);
        }
    }
}

/// <summary>Second restoration panorama: the Midnight Club bar from shuttered room to a fully stocked night bar.</summary>
internal sealed class MergeBarArtwork : FrameworkElement
{
    private const double DesignWidth = 1400;
    private const double DesignHeight = 235;
    private readonly long points;

    internal MergeBarArtwork(long points, double width = DesignWidth, double height = DesignHeight)
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

        bool cleaned = points >= 500;
        bool stocked = points >= 1200;
        bool open = points >= 2500;
        Brush gold = MergeArtworkBrushes.Ink(cleaned ? "#D8B46C" : "#6F6550");
        Brush wall = MergeArtworkBrushes.Ink(cleaned ? "#172D31" : "#292B2A");
        Brush wood = MergeArtworkBrushes.Ink(cleaned ? "#633C36" : "#3C3733");

        dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#0D171C"), new Pen(gold, 1.5), new Rect(1, 1, DesignWidth - 2, DesignHeight - 2), 12, 12);
        dc.DrawRectangle(wall, null, new Rect(18, 16, DesignWidth - 36, 145));
        dc.DrawRectangle(MergeArtworkBrushes.Ink(cleaned ? "#102326" : "#202321"), null, new Rect(18, 16, DesignWidth - 36, 25));
        dc.DrawRectangle(MergeArtworkBrushes.Ink(cleaned ? "#1A2829" : "#252622"), null, new Rect(18, 161, DesignWidth - 36, 56));

        for (int section = 0; section < 5; section++)
        {
            double x = 150 + section * 224;
            dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink(cleaned ? "#1D3A3D" : "#31322F"), new Pen(gold, 1), new Rect(x, 49, 180, 86), 5, 5);
            dc.DrawLine(new Pen(gold, 1), new Point(x + 12, 78), new Point(x + 168, 78));
            dc.DrawLine(new Pen(gold, 1), new Point(x + 12, 106), new Point(x + 168, 106));
            if (stocked)
            {
                for (int shelf = 0; shelf < 3; shelf++)
                    for (int bottle = 0; bottle < 6; bottle++)
                        DrawBottle(dc, x + 21 + bottle * 24, 70 + shelf * 28, bottle + shelf * 2);
            }
        }

        dc.DrawRoundedRectangle(wood, new Pen(gold, 1.5), new Rect(100, 128, 1200, 63), 9, 9);
        dc.DrawRectangle(MergeArtworkBrushes.Ink(cleaned ? "#4F302F" : "#34312F"), null, new Rect(125, 144, 1150, 29));
        dc.DrawLine(new Pen(gold, 2), new Point(100, 128), new Point(1300, 128));

        if (cleaned)
        {
            for (int i = 0; i < 6; i++)
            {
                double x = 210 + i * 196;
                dc.DrawLine(new Pen(gold, 1), new Point(x, 18), new Point(x, 48));
                dc.DrawEllipse(MergeArtworkBrushes.Ink(stocked ? "#FFE1A0" : "#75694D"), new Pen(gold, 1), new Point(x, 53), 15, 7);
            }
        }

        if (open)
        {
            for (int i = 0; i < 8; i++)
            {
                double x = 205 + i * 142;
                dc.DrawEllipse(MergeArtworkBrushes.Ink(i % 2 == 0 ? "#643B48" : "#35595B"), new Pen(gold, 1), new Point(x, 193), 25, 9);
                dc.DrawLine(new Pen(gold, 2), new Point(x, 201), new Point(x, 215));
            }
            var sign = new FormattedText("MIDNIGHT BAR", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Georgia"), 21, MergeArtworkBrushes.Ink("#F4D98D"), VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#20383A"), new Pen(gold, 1), new Rect(545, 44, 310, 50), 9, 9);
            dc.DrawText(sign, new Point(700 - sign.Width / 2, 69 - sign.Height / 2));
            DrawCocktail(dc, 80, 145, gold);
            DrawCocktail(dc, 1320, 145, gold);
        }
        else if (!cleaned)
        {
            for (int i = 0; i < 12; i++)
            {
                double x = 70 + i * 112;
                dc.DrawRoundedRectangle(MergeArtworkBrushes.Ink("#55524A"), null, new Rect(x, 183 - (i % 2) * 8, 55, 18), 3, 3);
            }
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(75, 0, 0, 0)), null, new Rect(18, 16, DesignWidth - 36, 201));
        }

        dc.Pop();
        dc.Pop();
    }

    private static void DrawBottle(DrawingContext dc, double x, double y, int variant)
    {
        string[] colors = ["#9B6B52", "#568273", "#7A608A", "#B58B45"];
        Brush fill = MergeArtworkBrushes.Ink(colors[variant % colors.Length]);
        dc.DrawRoundedRectangle(fill, new Pen(MergeArtworkBrushes.Ink("#D8C38D"), .6), new Rect(x, y - 15, 10, 17), 2, 2);
        dc.DrawRectangle(fill, null, new Rect(x + 3, y - 21, 4, 7));
    }

    private static void DrawCocktail(DrawingContext dc, double x, double y, Brush gold)
    {
        var bowl = new StreamGeometry();
        using (StreamGeometryContext ctx = bowl.Open())
        {
            ctx.BeginFigure(new Point(x - 22, y), true, true);
            ctx.LineTo(new Point(x + 22, y), true, false);
            ctx.LineTo(new Point(x, y + 26), true, false);
        }
        dc.DrawGeometry(MergeArtworkBrushes.Ink("#B96F7C"), new Pen(gold, 1), bowl);
        dc.DrawLine(new Pen(gold, 1), new Point(x, y + 26), new Point(x, y + 43));
        dc.DrawLine(new Pen(gold, 2), new Point(x - 15, y + 44), new Point(x + 15, y + 44));
    }
}
