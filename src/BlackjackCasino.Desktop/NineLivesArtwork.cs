using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class NineLivesArtwork
{
    private const double Size = 256d;
    private static readonly Lazy<ImageSource> BackgroundLazy = new(() => DecodeBackground());
    private static readonly IReadOnlyDictionary<NineLivesSymbol, ImageSource> Symbols = BuildSymbols();

    internal static ImageSource Background => BackgroundLazy.Value;

    internal static ImageSource Symbol(NineLivesSymbol symbol)
        => Symbols.TryGetValue(symbol, out ImageSource? image) ? image : CreateYarn();

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
            Background = Solid("#0A0712"),
            ClipToBounds = true,
            Child = image,
        };
    }

    private static IReadOnlyDictionary<NineLivesSymbol, ImageSource> BuildSymbols() =>
        new Dictionary<NineLivesSymbol, ImageSource>
        {
            [NineLivesSymbol.Yarn] = CreateYarn(),
            [NineLivesSymbol.Fishbone] = CreateFishbone(),
            [NineLivesSymbol.Bell] = CreateBell(),
            [NineLivesSymbol.TabbyCat] = CreateCat("#A96931", "#E0A955", "#62D38D", tabby: true),
            [NineLivesSymbol.WhiteCat] = CreateCat("#EAE7E2", "#FBF8F4", "#76C8FF", tabby: false),
            [NineLivesSymbol.BlackCat] = CreateCat("#17131D", "#28202F", "#FFBE3F", tabby: false),
            [NineLivesSymbol.Wild] = CreateWild(),
            [NineLivesSymbol.Bonus] = CreateBonus(),
        };

    private static ImageSource DecodeBackground()
    {
        try
        {
            byte[] bytes = Convert.FromBase64String(NineLivesArtData.Background);
            using var stream = new MemoryStream(bytes, writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.DecodePixelWidth = 1536;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            AppCrashLog.Write("Nine Lives background could not be decoded. Falling back to vector backdrop.", ex);
            return CreateBackdropFallback();
        }
    }

    private static ImageSource CreateYarn()
    {
        DrawingGroup g = BaseTile("#180D24", "#57236E");
        Brush yarn = Gradient("#D86BFF", "#77269A");
        Add(g, new EllipseGeometry(new Point(128, 130), 72, 72), yarn, Pen("#F2A1FF", 3));
        Stroke(g, "M70,112 C105,78 154,91 190,122", "#F0B2FF", 5);
        Stroke(g, "M63,140 C101,111 151,121 195,153", "#A943C9", 5);
        Stroke(g, "M86,73 C116,104 145,151 163,190", "#E78BFF", 5);
        Stroke(g, "M191,158 C213,169 222,184 232,203", "#B64ADB", 5);
        AddText(g, "YARN", 17, 216, "#D8B5E8", 13, "Segoe UI Semibold");
        return Finish(g);
    }

    private static ImageSource CreateFishbone()
    {
        DrawingGroup g = BaseTile("#091521", "#174664");
        Pen bone = Pen("#D7E5ED", 8);
        Add(g, new LineGeometry(new Point(60, 130), new Point(198, 130)), null, bone);
        for (int i = 0; i < 6; i++)
        {
            double x = 82 + (i * 19);
            Add(g, new LineGeometry(new Point(x, 130), new Point(x - 13, 96)), null, Pen("#C9D9E3", 5));
            Add(g, new LineGeometry(new Point(x, 130), new Point(x - 13, 164)), null, Pen("#C9D9E3", 5));
        }
        Add(g, Geometry.Parse("M196,130 L228,103 L224,157 Z"), Gradient("#E5EFF4", "#879DAA"), Pen("#F5FBFF", 2));
        Add(g, new EllipseGeometry(new Point(216, 126), 3, 3), Solid("#19232A"));
        Add(g, Geometry.Parse("M61,130 L36,109 L39,151 Z"), Gradient("#CADDE8", "#7F97A5"));
        AddText(g, "FISH", 19, 216, "#AFC5D2", 13, "Segoe UI Semibold");
        return Finish(g);
    }

    private static ImageSource CreateBell()
    {
        DrawingGroup g = BaseTile("#1B0D16", "#6F332B");
        Add(g, new EllipseGeometry(new Point(128, 71), 20, 18), null, Pen("#F7CE67", 9));
        Add(g, Geometry.Parse("M82,159 C87,98 101,80 128,80 C155,80 169,98 174,159 L190,178 L66,178 Z"),
            Gradient("#FFE27A", "#B56B13"), Pen("#FFF0A8", 3));
        Add(g, new EllipseGeometry(new Point(128, 184), 13, 13), Gradient("#F9D163", "#8E5110"));
        Add(g, new EllipseGeometry(new Point(128, 140), 17, 15), Solid("#4A210F"));
        Add(g, new EllipseGeometry(new Point(128, 140), 7, 6), Solid("#F8C95B"));
        AddText(g, "BELL", 19, 216, "#E8C47D", 13, "Segoe UI Semibold");
        return Finish(g);
    }

    private static ImageSource CreateCat(string furDark, string furLight, string eyeColor, bool tabby)
    {
        DrawingGroup g = BaseTile("#110C18", tabby ? "#593419" : furDark == "#EAE7E2" ? "#34405A" : "#3C1B50");
        Brush dark = Solid(furDark);
        Brush light = Solid(furLight);
        Brush eyes = Solid(eyeColor);

        Add(g, Geometry.Parse("M62,111 L73,39 L111,82 C122,76 134,76 145,82 L184,39 L194,112 C204,128 205,155 192,178 C178,202 153,214 128,214 C103,214 78,202 64,178 C51,155 52,128 62,111 Z"),
            light, Pen(tabby ? "#F0B96C" : "#7C5B88", 3));
        Add(g, Geometry.Parse("M74,91 L78,55 L104,86 Z"), dark);
        Add(g, Geometry.Parse("M182,91 L178,55 L152,86 Z"), dark);
        Add(g, new EllipseGeometry(new Point(100, 134), 24, 17), Solid("#17131B"));
        Add(g, new EllipseGeometry(new Point(156, 134), 24, 17), Solid("#17131B"));
        Add(g, new EllipseGeometry(new Point(100, 134), 11, 13), eyes);
        Add(g, new EllipseGeometry(new Point(156, 134), 11, 13), eyes);
        Add(g, new EllipseGeometry(new Point(100, 134), 3, 11), Solid("#110D14"));
        Add(g, new EllipseGeometry(new Point(156, 134), 3, 11), Solid("#110D14"));
        Add(g, Geometry.Parse("M119,161 L137,161 L128,171 Z"), Solid("#E69BA6"));
        Stroke(g, "M128,171 C125,180 118,183 111,183", "#8E6474", 2.5);
        Stroke(g, "M128,171 C131,180 138,183 145,183", "#8E6474", 2.5);
        Stroke(g, "M112,174 L57,163", "#CDBACD", 2);
        Stroke(g, "M111,181 L54,184", "#CDBACD", 2);
        Stroke(g, "M144,174 L199,163", "#CDBACD", 2);
        Stroke(g, "M145,181 L202,184", "#CDBACD", 2);

        if (tabby)
        {
            Stroke(g, "M102,87 L113,111", "#633518", 7);
            Stroke(g, "M128,82 L128,111", "#633518", 7);
            Stroke(g, "M154,87 L143,111", "#633518", 7);
            Stroke(g, "M78,151 L99,158", "#633518", 6);
            Stroke(g, "M178,151 L157,158", "#633518", 6);
        }

        return Finish(g);
    }

    private static ImageSource CreateWild()
    {
        DrawingGroup g = BaseTile("#120A1B", "#6B287C");
        Add(g, Geometry.Parse("M43,72 C75,45 105,50 126,80 C147,50 177,45 213,72 C196,129 167,153 128,153 C89,153 60,129 43,72 Z"),
            Gradient("#261331", "#09070D"), Pen("#CC63FF", 3));
        Add(g, Geometry.Parse("M66,99 C82,82 103,84 115,101 C103,117 82,120 66,99 Z"), Solid("#FFC94A"));
        Add(g, Geometry.Parse("M190,99 C174,82 153,84 141,101 C153,117 174,120 190,99 Z"), Solid("#FFC94A"));
        Add(g, new EllipseGeometry(new Point(93, 101), 3, 14), Solid("#150E18"));
        Add(g, new EllipseGeometry(new Point(163, 101), 3, 14), Solid("#150E18"));
        Stroke(g, "M70,178 L119,146", "#D968FF", 7);
        Stroke(g, "M102,194 L143,158", "#F0B2FF", 6);
        Stroke(g, "M138,199 L173,163", "#A53DCA", 7);
        AddText(g, "WILD", 0, 207, "#F0CE72", 25, "Georgia", bold: true);
        return Finish(g);
    }

    private static ImageSource CreateBonus()
    {
        DrawingGroup g = BaseTile("#160A22", "#72349A");
        Add(g, new EllipseGeometry(new Point(128, 122), 82, 82), Gradient("#35154D", "#0D0814"), Pen("#E1A84F", 5));
        Add(g, new EllipseGeometry(new Point(128, 122), 68, 68), null, Pen("#B54CDE", 3));
        AddText(g, "9", 0, 53, "#F4CF69", 112, "Georgia", bold: true);
        Add(g, Geometry.Parse("M79,62 L91,35 L107,65 Z"), Solid("#D68B48"));
        Add(g, Geometry.Parse("M149,65 L165,35 L178,62 Z"), Solid("#D68B48"));
        AddText(g, "NINE LIVES", 0, 208, "#D8B1EA", 15, "Segoe UI Semibold");
        return Finish(g);
    }

    private static DrawingGroup BaseTile(string center, string edge)
    {
        var g = new DrawingGroup();
        var bg = new RadialGradientBrush
        {
            Center = new Point(0.5, 0.42),
            GradientOrigin = new Point(0.5, 0.42),
            RadiusX = 0.75,
            RadiusY = 0.75,
            GradientStops = new GradientStopCollection
            {
                new((Color)ColorConverter.ConvertFromString(center), 0),
                new((Color)ColorConverter.ConvertFromString(edge), 0.68),
                new((Color)ColorConverter.ConvertFromString("#07050C"), 1),
            },
        };
        bg.Freeze();
        Add(g, new RectangleGeometry(new Rect(0, 0, Size, Size), 18, 18), bg, Pen("#B27935", 3));
        Add(g, new RectangleGeometry(new Rect(8, 8, Size - 16, Size - 16), 14, 14), null, Pen("#533457", 1.5));
        return g;
    }

    private static ImageSource CreateBackdropFallback()
    {
        var g = new DrawingGroup();
        Add(g, new RectangleGeometry(new Rect(0, 0, 1536, 1024)), Gradient("#160922", "#05030A"));
        Add(g, new EllipseGeometry(new Point(1180, 210), 145, 145), Solid("#DCCBEB"));
        Stroke(g, "M0,840 C280,720 430,770 690,700 C920,640 1170,720 1536,610", "#40204E", 28);
        return Finish(g);
    }

    private static void Add(DrawingGroup group, Geometry geometry, Brush? fill, Pen? pen = null)
    {
        if (geometry.CanFreeze) geometry.Freeze();
        group.Children.Add(new GeometryDrawing(fill, pen, geometry));
    }

    private static void Stroke(DrawingGroup group, string path, string color, double thickness)
        => Add(group, Geometry.Parse(path), null, Pen(color, thickness));

    private static void AddText(DrawingGroup group, string text, double x, double y, string color, double fontSize,
        string fontFamily, bool bold = false)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(fontFamily), FontStyles.Normal, bold ? FontWeights.Bold : FontWeights.SemiBold, FontStretches.Normal),
            fontSize,
            Solid(color),
            1.0);
        double drawX = x == 0 ? (Size - formatted.Width) / 2d : x;
        Geometry geometry = formatted.BuildGeometry(new Point(drawX, y));
        Add(group, geometry, Solid(color));
    }

    private static SolidColorBrush Solid(string value)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
        brush.Freeze();
        return brush;
    }

    private static LinearGradientBrush Gradient(string from, string to)
    {
        var brush = new LinearGradientBrush(
            (Color)ColorConverter.ConvertFromString(from),
            (Color)ColorConverter.ConvertFromString(to),
            new Point(0, 0), new Point(1, 1));
        brush.Freeze();
        return brush;
    }

    private static Pen Pen(string color, double thickness)
    {
        var pen = new Pen(Solid(color), thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        pen.Freeze();
        return pen;
    }

    private static ImageSource Finish(DrawingGroup group)
    {
        group.Freeze();
        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }
}
