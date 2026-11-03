using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BlackjackCasino.Desktop;

/// <summary>Offline portrait resources shared by shop previews and equipped pictures.</summary>
internal static class PortraitArtwork
{
    private static readonly IReadOnlyDictionary<string,string> Files = new Dictionary<string,string>(StringComparer.Ordinal)
    {
        ["portrait-night-dealer"] = "night-dealer.png",
        ["portrait-card-shark"] = "card-shark.png",
        ["portrait-moon-cat"] = "moon-cat.png",
        ["portrait-neon-ace"] = "neon-ace.png",
        ["portrait-lucky-raven"] = "lucky-raven.png",
        ["portrait-midnight-lady"] = "midnight-lady.png",
        ["portrait-golden-joker"] = "golden-joker.png",
        ["portrait-velvet-king"] = "velvet-king.png",
        ["portrait-neon-croupier"] = "neon-croupier.jpg",
    };

    private static readonly IReadOnlyDictionary<string, DynastyPortraitPalette> DynastyPortraits =
        new Dictionary<string, DynastyPortraitPalette>(StringComparer.Ordinal)
        {
            ["portrait-dynasty-heiress"] = new("#160D22", "#D342A1", "#F1BC55", "#F5DCD0", true, false),
            ["portrait-neon-overseer"] = new("#080F19", "#38E6DF", "#8F55E8", "#D7EDF0", false, true),
            ["portrait-neon-emperor"] = new("#120A18", "#40ECE5", "#FF4DB3", "#F6C95F", true, true),
        };

    private static readonly Dictionary<string,BitmapSource> Cache = new(StringComparer.Ordinal);

    internal static BitmapSource? Get(string portraitId)
    {
        if (Cache.TryGetValue(portraitId, out BitmapSource? cached)) return cached;

        if (Files.TryGetValue(portraitId, out string? file))
        {
            // Retain the full original artwork in the assembly, decode once at a bounded size
            // sufficient for the largest 112-DIP portrait even on a 4x-density display.
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = 512;
            image.UriSource = new Uri($"pack://application:,,,/BlackjackCasino.Desktop;component/Assets/Portraits/{file}", UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            Cache.Add(portraitId, image);
            return image;
        }

        if (!DynastyPortraits.TryGetValue(portraitId, out DynastyPortraitPalette? palette)) return null;
        BitmapSource generated = RenderDynastyPortrait(palette);
        Cache.Add(portraitId, generated);
        return generated;
    }

    private static BitmapSource RenderDynastyPortrait(DynastyPortraitPalette palette)
    {
        const int size = 512;
        Color background = C(palette.Background);
        Color primary = C(palette.Primary);
        Color secondary = C(palette.Secondary);
        Color skin = C(palette.Skin);
        Color blackGlass = Color.FromRgb(8, 11, 18);
        Color gold = Color.FromRgb(244, 198, 82);

        var visual = new DrawingVisual();
        using (DrawingContext dc = visual.RenderOpen())
        {
            var backdrop = new LinearGradientBrush(background, Color.FromRgb(21, 10, 32), new Point(0, 0), new Point(1, 1));
            dc.DrawRectangle(backdrop, null, new Rect(0, 0, size, size));

            // Architectural neon rails keep all four portraits visually tied to the same pass.
            var primaryPen = new Pen(new SolidColorBrush(primary), 10);
            var secondaryPen = new Pen(new SolidColorBrush(secondary), 7);
            primaryPen.Freeze(); secondaryPen.Freeze();
            dc.DrawLine(primaryPen, new Point(34, 92), new Point(196, 20));
            dc.DrawLine(primaryPen, new Point(316, 20), new Point(478, 92));
            dc.DrawLine(secondaryPen, new Point(22, 390), new Point(166, 486));
            dc.DrawLine(secondaryPen, new Point(346, 486), new Point(490, 390));

            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(105, primary.R, primary.G, primary.B)), null, new Point(256, 228), 196, 196);
            dc.DrawEllipse(Brushes.Transparent, new Pen(new SolidColorBrush(Color.FromArgb(220, secondary.R, secondary.G, secondary.B)), 5), new Point(256, 228), 171, 171);

            // Tailored dark silhouette, intentionally consistent across the collection.
            StreamGeometry shoulders = Polygon(
                new Point(98, 512), new Point(126, 390), new Point(194, 336),
                new Point(256, 366), new Point(318, 336), new Point(386, 390), new Point(414, 512));
            dc.DrawGeometry(new SolidColorBrush(blackGlass), new Pen(new SolidColorBrush(primary), 4), shoulders);

            StreamGeometry lapelLeft = Polygon(new Point(194, 336), new Point(252, 390), new Point(224, 492), new Point(151, 379));
            StreamGeometry lapelRight = Polygon(new Point(318, 336), new Point(260, 390), new Point(288, 492), new Point(361, 379));
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(19, 25, 38)), new Pen(new SolidColorBrush(secondary), 3), lapelLeft);
            dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(19, 25, 38)), new Pen(new SolidColorBrush(primary), 3), lapelRight);

            // Face and stylised hair/headpiece.
            dc.DrawEllipse(new SolidColorBrush(skin), null, new Point(256, 238), 82, 101);
            StreamGeometry hair = Polygon(
                new Point(177, 225), new Point(190, 156), new Point(232, 126), new Point(291, 132),
                new Point(330, 170), new Point(335, 228), new Point(302, 188), new Point(261, 170), new Point(216, 188));
            dc.DrawGeometry(new SolidColorBrush(palette.Armored ? Color.FromRgb(26, 30, 45) : Color.FromRgb(18, 17, 29)),
                new Pen(new SolidColorBrush(palette.Armored ? primary : secondary), 4), hair);

            // Minimal facial lighting rather than literal features keeps the portrait readable at 38 DIP.
            dc.DrawLine(new Pen(new SolidColorBrush(primary), 6), new Point(216, 239), new Point(240, 239));
            dc.DrawLine(new Pen(new SolidColorBrush(secondary), 6), new Point(272, 239), new Point(296, 239));
            dc.DrawLine(new Pen(new SolidColorBrush(Color.FromArgb(160, secondary.R, secondary.G, secondary.B)), 4), new Point(240, 292), new Point(274, 292));

            if (palette.Crowned)
            {
                StreamGeometry crown = Polygon(
                    new Point(190, 151), new Point(205, 93), new Point(244, 126),
                    new Point(258, 65), new Point(280, 126), new Point(318, 93), new Point(329, 153));
                dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(75, gold.R, gold.G, gold.B)), new Pen(new SolidColorBrush(gold), 7), crown);
                dc.DrawEllipse(new SolidColorBrush(primary), null, new Point(258, 96), 9, 9);
            }

            if (palette.Armored)
            {
                dc.DrawLine(new Pen(new SolidColorBrush(gold), 5), new Point(157, 411), new Point(213, 454));
                dc.DrawLine(new Pen(new SolidColorBrush(gold), 5), new Point(355, 411), new Point(299, 454));
            }

            // Shared Dynasty seal.
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(220, 10, 14, 22)), new Pen(new SolidColorBrush(gold), 4), new Point(424, 86), 33, 33);
            StreamGeometry diamond = Polygon(new Point(424, 61), new Point(446, 86), new Point(424, 111), new Point(402, 86));
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(150, primary.R, primary.G, primary.B)), new Pen(new SolidColorBrush(secondary), 3), diamond);
        }

        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static StreamGeometry Polygon(params Point[] points)
    {
        var geometry = new StreamGeometry();
        using StreamGeometryContext context = geometry.Open();
        context.BeginFigure(points[0], true, true);
        context.PolyLineTo(points.Skip(1).ToArray(), true, false);
        geometry.Freeze();
        return geometry;
    }

    private static Color C(string value) => (Color)ColorConverter.ConvertFromString(value);

    private sealed record DynastyPortraitPalette(
        string Background,
        string Primary,
        string Secondary,
        string Skin,
        bool Crowned,
        bool Armored);
}