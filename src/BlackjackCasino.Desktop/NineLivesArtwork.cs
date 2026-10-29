using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class NineLivesArtwork
{
    private static readonly Lazy<ImageSource> BackgroundLazy = new(() => Decode(NineLivesArtData.Background, 960));
    private static readonly IReadOnlyDictionary<NineLivesSymbol, ImageSource> Symbols = BuildSymbols();

    internal static ImageSource Background => BackgroundLazy.Value;
    internal static ImageSource Symbol(NineLivesSymbol symbol) => Symbols[symbol];

    internal static FrameworkElement LobbyArtwork()
    {
        var image = new Image
        {
            Source = Background,
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(10, 7, 18)),
            ClipToBounds = true,
            Child = image,
        };
    }

    private static IReadOnlyDictionary<NineLivesSymbol, ImageSource> BuildSymbols() =>
        new Dictionary<NineLivesSymbol, ImageSource>
        {
            [NineLivesSymbol.Yarn] = Decode(NineLivesArtData.Yarn, 192),
            [NineLivesSymbol.Fishbone] = Decode(NineLivesArtData.Fishbone, 192),
            [NineLivesSymbol.Bell] = Decode(NineLivesArtData.Bell, 192),
            [NineLivesSymbol.TabbyCat] = Decode(NineLivesArtData.TabbyCat, 192),
            [NineLivesSymbol.WhiteCat] = Decode(NineLivesArtData.WhiteCat, 192),
            [NineLivesSymbol.BlackCat] = Decode(NineLivesArtData.BlackCat, 192),
            [NineLivesSymbol.Wild] = Decode(NineLivesArtData.Wild, 192),
            [NineLivesSymbol.Bonus] = Decode(NineLivesArtData.Bonus, 192),
        };

    private static ImageSource Decode(string base64, int pixelWidth)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        using var stream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bitmap.DecodePixelWidth = pixelWidth;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}

internal static partial class NineLivesArtData
{
}
