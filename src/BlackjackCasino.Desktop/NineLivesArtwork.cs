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
            : CreateFallback(192, 192, symbol.ToString());

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
            return CreateFallback(pixelWidth, pixelWidth, assetName);
        }
    }

    private static ImageSource CreateFallback(double width, double height, string assetName)
    {
        if (string.Equals(assetName, nameof(NineLivesSymbol.Yarn), StringComparison.OrdinalIgnoreCase))
        {
            return CreateYarnFallback(width, height);
        }

        var background = new LinearGradientBrush(
            Color.FromRgb(20, 12, 34),
            Color.FromRgb(8, 7, 18),
            45);
        var accent = new SolidColorBrush(Color.FromRgb(201, 92, 255));
        var gold = new SolidColorBrush(Color.FromRgb(239, 201, 106));
        var framePen = new Pen(accent, Math.Max(2, width / 80));
        background.Freeze();
        accent.Freeze();
        gold.Freeze();
        framePen.Freeze();

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(background, framePen,
            new RectangleGeometry(new Rect(1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2)), width / 14, height / 14)));

        double cx = width / 2;
        double cy = height / 2;
        group.Children.Add(new GeometryDrawing(accent, null,
            new EllipseGeometry(new Point(cx - width * 0.11, cy), width * 0.13, height * 0.23)));
        group.Children.Add(new GeometryDrawing(gold, null,
            new EllipseGeometry(new Point(cx + width * 0.11, cy), width * 0.13, height * 0.23)));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(8, 7, 18)), null,
            new EllipseGeometry(new Point(cx - width * 0.08, cy), width * 0.035, height * 0.07)));
        group.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(8, 7, 18)), null,
            new EllipseGeometry(new Point(cx + width * 0.08, cy), width * 0.035, height * 0.07)));
        group.Freeze();

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }

    private static ImageSource CreateYarnFallback(double width, double height)
    {
        var background = new LinearGradientBrush(
            Color.FromRgb(29, 13, 45),
            Color.FromRgb(8, 7, 18),
            35);
        var yarn = new RadialGradientBrush
        {
            Center = new Point(0.42, 0.36),
            GradientOrigin = new Point(0.35, 0.30),
            RadiusX = 0.7,
            RadiusY = 0.7,
        };
        yarn.GradientStops.Add(new GradientStop(Color.FromRgb(238, 137, 255), 0));
        yarn.GradientStops.Add(new GradientStop(Color.FromRgb(181, 67, 232), 0.5));
        yarn.GradientStops.Add(new GradientStop(Color.FromRgb(95, 35, 145), 1));
        var goldPen = new Pen(new SolidColorBrush(Color.FromRgb(245, 206, 112)), Math.Max(2, width / 70));
        var lightPen = new Pen(new SolidColorBrush(Color.FromRgb(247, 196, 255)), Math.Max(2, width / 85));
        background.Freeze();
        yarn.Freeze();
        goldPen.Freeze();
        lightPen.Freeze();

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(background, goldPen,
            new RectangleGeometry(new Rect(1, 1, width - 2, height - 2), width / 14, height / 14)));

        double cx = width * 0.50;
        double cy = height * 0.51;
        double radius = width * 0.29;
        group.Children.Add(new GeometryDrawing(yarn, null,
            new EllipseGeometry(new Point(cx, cy), radius, radius)));

        foreach ((double y, double bend) in new[] { (-0.15, 0.10), (0.00, -0.12), (0.15, 0.08) })
        {
            var curve = new StreamGeometry();
            using StreamGeometryContext context = curve.Open();
            context.BeginFigure(new Point(width * 0.28, height * (0.5 + y)), false, false);
            context.BezierTo(
                new Point(width * 0.40, height * (0.31 + y + bend)),
                new Point(width * 0.65, height * (0.67 + y - bend)),
                new Point(width * 0.72, height * (0.5 + y)),
                true, false);
            curve.Freeze();
            group.Children.Add(new GeometryDrawing(null, lightPen, curve));
        }

        var tail = new StreamGeometry();
        using (StreamGeometryContext context = tail.Open())
        {
            context.BeginFigure(new Point(width * 0.67, height * 0.67), false, false);
            context.BezierTo(
                new Point(width * 0.82, height * 0.70),
                new Point(width * 0.74, height * 0.88),
                new Point(width * 0.90, height * 0.88),
                true, false);
        }
        tail.Freeze();
        group.Children.Add(new GeometryDrawing(null, goldPen, tail));
        group.Freeze();

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}

internal static partial class NineLivesArtData
{
}
