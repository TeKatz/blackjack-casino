using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class NineLivesArtwork
{
    private static readonly Lazy<ImageSource> BackgroundLazy = new(() => DecodeOrFallback(NineLivesArtData.Background, 960, "background"));
    private static readonly IReadOnlyDictionary<NineLivesSymbol, Lazy<ImageSource>> Symbols = BuildSymbols();

    internal static ImageSource Background => BackgroundLazy.Value;

    internal static ImageSource Symbol(NineLivesSymbol symbol)
        => Symbols.TryGetValue(symbol, out Lazy<ImageSource>? image)
            ? image.Value
            : CreateFallback(192, 192);

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

    private static IReadOnlyDictionary<NineLivesSymbol, Lazy<ImageSource>> BuildSymbols() =>
        new Dictionary<NineLivesSymbol, Lazy<ImageSource>>
        {
            [NineLivesSymbol.Yarn] = LazySymbol(NineLivesArtData.Yarn, nameof(NineLivesSymbol.Yarn)),
            [NineLivesSymbol.Fishbone] = LazySymbol(NineLivesArtData.Fishbone, nameof(NineLivesSymbol.Fishbone)),
            [NineLivesSymbol.Bell] = LazySymbol(NineLivesArtData.Bell, nameof(NineLivesSymbol.Bell)),
            [NineLivesSymbol.TabbyCat] = LazySymbol(NineLivesArtData.TabbyCat, nameof(NineLivesSymbol.TabbyCat)),
            [NineLivesSymbol.WhiteCat] = LazySymbol(NineLivesArtData.WhiteCat, nameof(NineLivesSymbol.WhiteCat)),
            [NineLivesSymbol.BlackCat] = LazySymbol(NineLivesArtData.BlackCat, nameof(NineLivesSymbol.BlackCat)),
            [NineLivesSymbol.Wild] = LazySymbol(NineLivesArtData.Wild, nameof(NineLivesSymbol.Wild)),
            [NineLivesSymbol.Bonus] = LazySymbol(NineLivesArtData.Bonus, nameof(NineLivesSymbol.Bonus)),
        };

    private static Lazy<ImageSource> LazySymbol(string base64, string name)
        => new(() => DecodeOrFallback(base64, 192, name));

    private static ImageSource DecodeOrFallback(string base64, int pixelWidth, string assetName)
    {
        try
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
        catch (Exception ex)
        {
            AppCrashLog.Write($"Nine Lives artwork '{assetName}' could not be decoded. Falling back to safe vector artwork.", ex);
            return CreateFallback(pixelWidth, pixelWidth);
        }
    }

    private static ImageSource CreateFallback(double width, double height)
    {
        var background = new SolidColorBrush(Color.FromRgb(16, 10, 28));
        var accent = new SolidColorBrush(Color.FromRgb(201, 92, 255));
        var pen = new Pen(accent, Math.Max(2, width / 80));
        background.Freeze();
        accent.Freeze();
        pen.Freeze();

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(background, pen,
            new RectangleGeometry(new Rect(1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2)), width / 14, height / 14)));
        group.Children.Add(new GeometryDrawing(accent, null,
            new EllipseGeometry(new Point(width / 2, height / 2), width / 7, height / 7)));
        group.Freeze();

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}

internal static partial class NineLivesArtData
{
}
