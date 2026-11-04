using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Small, frozen vector ornaments; each deck keeps a distinct back at any DPI.</summary>
internal static class CardBackArtwork
{
    private static readonly Dictionary<string, DrawingBrush> Cache = new(StringComparer.Ordinal);

    internal static DrawingBrush Get(string variant)
    {
        if (Cache.TryGetValue(variant, out DrawingBrush? cached)) return cached;
        PlayingCardTheme theme = PlayingCardTheme.Resolve(variant);
        var drawing = new DrawingGroup { ClipGeometry = new RectangleGeometry(new Rect(0, 0, 60, 90)) };
        using (DrawingContext dc = drawing.Open())
        {
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, 60, 90));
            var pen = new Pen(new SolidColorBrush(theme.BackAccent), .65);
            var soft = new Pen(new SolidColorBrush(Color.FromArgb(100, theme.BackAccent.R, theme.BackAccent.G, theme.BackAccent.B)), .5);
            switch (theme.Pattern)
            {
                case "weave":
                    for (int n = 6; n < 90; n += 6) dc.DrawLine(soft, new Point(3, n), new Point(57, n));
                    for (int n = 6; n < 60; n += 6) dc.DrawLine(soft, new Point(n, 3), new Point(n, 87));
                    break;
                case "lattice":
                case "diamonds":
                    for (int n = -90; n < 90; n += theme.Pattern == "diamonds" ? 18 : 12)
                    {
                        dc.DrawLine(soft, new Point(0, n), new Point(60, n + 60));
                        dc.DrawLine(soft, new Point(0, n + 60), new Point(60, n));
                    }
                    break;
                case "fans":
                    foreach (double y in new[] { 17d, 45d, 73d })
                        foreach (double x in new[] { 15d, 45d })
                        {
                            dc.DrawEllipse(null, soft, new Point(x, y), 12, 12);
                            for (int i = -2; i <= 2; i++)
                                dc.DrawLine(soft, new Point(x, y + 12), new Point(x + i * 5, y - 8));
                        }
                    break;
                case "stars":
                    foreach (Point p in new[] { new Point(13, 14), new Point(45, 24), new Point(16, 64), new Point(43, 77), new Point(40, 55), new Point(24, 33) })
                    {
                        dc.DrawLine(pen, new Point(p.X - 2, p.Y), new Point(p.X + 2, p.Y));
                        dc.DrawLine(pen, new Point(p.X, p.Y - 3), new Point(p.X, p.Y + 3));
                    }
                    dc.DrawLine(soft, new Point(13, 14), new Point(24, 33));
                    dc.DrawLine(soft, new Point(24, 33), new Point(45, 24));
                    dc.DrawLine(soft, new Point(16, 64), new Point(40, 55));
                    dc.DrawLine(soft, new Point(40, 55), new Point(43, 77));
                    break;
                case "eclipse":
                    dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(25, 255, 218, 143)), 3), new Point(30, 45), 16, 16);
                    dc.DrawEllipse(null, pen, new Point(30, 45), 18, 18);
                    for (int i = 0; i < 16; i++)
                    {
                        double angle = Math.PI * i / 8;
                        dc.DrawLine(soft, new Point(30 + Math.Cos(angle) * 20, 45 + Math.Sin(angle) * 20), new Point(30 + Math.Cos(angle) * 24, 45 + Math.Sin(angle) * 24));
                    }
                    break;
            }
        }
        var brush = new DrawingBrush(drawing) { Stretch = Stretch.Fill, TileMode = TileMode.None };
        brush.Freeze(); Cache.Add(variant, brush); return brush;
    }
}
