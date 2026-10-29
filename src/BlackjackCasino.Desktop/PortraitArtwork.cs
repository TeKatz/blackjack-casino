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
    };
    private static readonly Dictionary<string,BitmapSource> Cache = new(StringComparer.Ordinal);

    internal static BitmapSource? Get(string portraitId)
    {
        if (!Files.TryGetValue(portraitId, out string? file)) return null;
        if (Cache.TryGetValue(portraitId, out BitmapSource? cached)) return cached;

        // Retain the full original PNG in the assembly, decode once at a bounded size
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
}
