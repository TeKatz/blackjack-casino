using BlackjackCasino.Core;
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
        ["portrait-classic"] = "house-guest.jpg",
        ["portrait-eclipse-raven"] = "eclipse-raven.jpg",
        ["portrait-eclipse-lady"] = "eclipse-lady.jpg",
        ["portrait-eclipse-moon"] = "eclipse-moon.jpg",
        ["portrait-eclipse-sovereign"] = "eclipse-sovereign.jpg",
        ["portrait-neon-croupier"] = "neon-croupier.jpg",
        ["portrait-dynasty-heiress"] = "dynasty-heiress.jpg",
        ["portrait-neon-overseer"] = "neon-overseer.jpg",
        ["portrait-neon-emperor"] = "neon-emperor.jpg",
    };

    private static readonly Dictionary<string,BitmapSource> Cache = new(StringComparer.Ordinal);

    internal static BitmapSource? Get(string portraitId)
    {
        if (Cache.TryGetValue(portraitId, out BitmapSource? cached)) return cached;

        // Neutral geometric stand-in until the portrait owner approves an actual likeness.
        if (portraitId == CosmeticCatalog.SecretGuestPortraitId)
        {
            BitmapSource placeholder = CreateSecretGuestPlaceholder();
            Cache.Add(portraitId, placeholder);
            return placeholder;
        }

        if (Files.TryGetValue(portraitId, out string? file))
        {
            // Retain the full original artwork in the assembly, decode once at a bounded size
            // sufficient for profile portraits and shop previews on high-density displays.
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

        return null;
    }

    private static BitmapSource CreateSecretGuestPlaceholder()
    {
        const int size = 512;
        var visual = new DrawingVisual();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            var center = new Point(256, 256);
            var gold = new SolidColorBrush(Color.FromRgb(227, 195, 117));
            var pale = new SolidColorBrush(Color.FromRgb(249, 231, 171));
            var dark = new SolidColorBrush(Color.FromRgb(8, 29, 32));
            var background = new RadialGradientBrush(
                Color.FromRgb(27, 74, 66), Color.FromRgb(7, 19, 28));
            drawing.DrawRectangle(background, null, new Rect(0, 0, size, size));
            drawing.DrawEllipse(null, new Pen(gold, 9), center, 226, 226);
            drawing.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(96, 125, 107)), 3), center, 207, 207);
            drawing.DrawEllipse(null, new Pen(pale, 14), new Point(256, 220), 62, 68);
            drawing.DrawRoundedRectangle(gold, new Pen(pale, 5), new Rect(173, 220, 166, 154), 17, 17);
            drawing.DrawEllipse(dark, null, new Point(256, 277), 18, 18);
            drawing.DrawRoundedRectangle(dark, null, new Rect(247, 286, 18, 35), 5, 5);
            foreach (Point point in new[] { new Point(256, 65), new Point(256, 447),
                new Point(65, 256), new Point(447, 256) })
                drawing.DrawEllipse(pale, null, point, 5, 5);
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }
}
