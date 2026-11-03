using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Author cyber-vault symbols as resolution-independent vectors, then rasterize
/// exactly once into frozen 384px textures. WPF previously rerendered complex
/// DrawingImages on twenty moving reel cells, reducing the frame rate.
/// </summary>
internal static class NeonVaultSymbolAssets
{
    private const int RasterSize = 384;

    private static readonly IReadOnlyDictionary<NeonVaultSymbol, ImageSource> Images =
        Enum.GetValues<NeonVaultSymbol>().ToDictionary(symbol => symbol, DrawSymbol);

    internal static ImageSource Get(NeonVaultSymbol symbol) => Images[symbol];

    private static ImageSource DrawSymbol(NeonVaultSymbol symbol)
    {
        var group = new DrawingGroup();
        using (DrawingContext dc = group.Open())
        {
            Color accent = symbol switch
            {
                NeonVaultSymbol.Pulse => C("#54ECFF"),
                NeonVaultSymbol.Prism => C("#AA73FF"),
                NeonVaultSymbol.Chip => C("#5AF2DA"),
                NeonVaultSymbol.Crown => C("#FFD77B"),
                NeonVaultSymbol.Key => C("#FFAB6B"),
                NeonVaultSymbol.Wild => C("#FF65E9"),
                NeonVaultSymbol.Scatter => C("#FFDE86"),
                _ => C("#D6A0FF"),
            };
            Background(dc, accent);
            switch (symbol)
            {
                case NeonVaultSymbol.Pulse: Pulse(dc); break;
                case NeonVaultSymbol.Prism: Prism(dc); break;
                case NeonVaultSymbol.Chip: Chip(dc); break;
                case NeonVaultSymbol.Crown: Crown(dc); break;
                case NeonVaultSymbol.Key: Key(dc); break;
                case NeonVaultSymbol.Wild: Wild(dc); break;
                case NeonVaultSymbol.Scatter: VaultWheel(dc); break;
                case NeonVaultSymbol.VaultKey: AccessKey(dc); break;
            }
            ForegroundDetails(dc, accent);
        }
        group.Freeze();

        // Render the richly layered icon once, rather than for each WPF frame.
        // 384x384 keeps downsampled reel art crisp on the 1080p interface while
        // eight shared textures use only about 4.5 MiB.
        var visual = new DrawingVisual();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.PushTransform(new ScaleTransform(RasterSize / 512.0, RasterSize / 512.0));
            drawing.DrawDrawing(group);
            drawing.Pop();
        }
        var bitmap = new RenderTargetBitmap(RasterSize, RasterSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static void Background(DrawingContext dc, Color accent)
    {
        dc.DrawRoundedRectangle(
            new LinearGradientBrush(C("#0B132E"), C("#10071F"), 100),
            P(C("#314D78"), 5),
            new Rect(12, 12, 488, 488), 42, 42);

        var aura = new RadialGradientBrush(Color.FromArgb(115, accent.R, accent.G, accent.B), Colors.Transparent)
        {
            RadiusX = 0.64, RadiusY = 0.64,
        };
        dc.DrawEllipse(aura, null, new Point(256, 251), 230, 230);

        for (int i = 0; i < 5; i++)
        {
            double v = 96 + i * 80;
            dc.DrawLine(P(Color.FromArgb(25, 89, 178, 229), 1),
                new Point(v, 32), new Point(v, 478));
            dc.DrawLine(P(Color.FromArgb(20, 136, 97, 210), 1),
                new Point(32, v), new Point(480, v));
        }

        // Machined metallic frame; all artwork stays within the safe bounds.
        dc.DrawRoundedRectangle(null, P(Color.FromArgb(170, accent.R, accent.G, accent.B), 7),
            new Rect(23, 23, 466, 466), 37, 37);
        dc.DrawRoundedRectangle(null, P(C("#B86EE4"), 2.5),
            new Rect(34, 34, 444, 444), 27, 27);
        dc.DrawRoundedRectangle(null, P(C("#6AB1EA"), 1.5),
            new Rect(43, 43, 426, 426), 24, 24);
        Line(dc, 64, 75, 132, 75, C("#A9F8FF"), 4);
        Line(dc, 380, 436, 450, 436, C("#E99CFF"), 4);
        Line(dc, 75, 72, 75, 138, accent, 4);
        Line(dc, 438, 380, 438, 439, accent, 4);
        for (int j = 0; j < 4; j++)
        {
            double x = 74 + j * 122;
            dc.DrawEllipse(new SolidColorBrush(accent), null, new Point(x, 474), 3.3, 3.3);
        }
    }

    private static void ForegroundDetails(DrawingContext dc, Color accent)
    {
        var glass = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new(Color.FromArgb(60, 245, 253, 255), 0),
                new(Color.FromArgb(9, 245, 253, 255), 0.33),
                new(Color.FromArgb(0, 245, 253, 255), 0.5),
                new(Color.FromArgb(10, accent.R, accent.G, accent.B), 1),
            },
        };
        dc.DrawGeometry(glass, null, Geometry.Parse("M 56,53 L 435,53 380,118 95,118 Z"));
        Line(dc, 85, 453, 426, 453, Color.FromArgb(100, accent.R, accent.G, accent.B), 2);
    }

    private static void Pulse(DrawingContext dc)
    {
        Color cyan = C("#59EFFF"), pink = C("#FF61FA"), blue = C("#4F7BFF");
        Fill(dc, "M256,72 L393,146 393,344 256,427 119,344 119,146 Z",
            Grad(C("#162449"), C("#091128")), cyan, 7);
        Fill(dc, "M256,103 L364,164 364,322 256,391 147,322 147,164 Z",
            Grad(C("#155798"), C("#281055")), pink, 5);
        Fill(dc, "M256,103 L364,164 256,230 147,164 Z",
            Grad(C("#42E3FE"), C("#16235D")), cyan, 3);
        Fill(dc, "M147,164 L256,230 256,391 147,322 Z",
            Grad(C("#3D35AB"), C("#071C51")), blue, 3);
        Fill(dc, "M364,164 L256,230 256,391 364,322 Z",
            Grad(C("#4D1B81"), C("#0A1642")), pink, 3);
        Stroke(dc, "M256,104 L256,391 M147,164 L364,322 M364,164 L147,322", cyan, 4);
        Stroke(dc, "M256,165 L274,236 341,250 274,269 256,337 237,269 170,250 237,236 Z", pink, 6);
        dc.DrawEllipse(new RadialGradientBrush(Colors.White, cyan), null, new Point(256,251), 27, 27);
        dc.DrawEllipse(Brushes.White, null, new Point(256,251), 9, 9);
    }

    private static void Prism(DrawingContext dc)
    {
        Color cyan=C("#65EFFF"), lilac=C("#D383FF"), magenta=C("#F45FFF");
        Fill(dc,"M256,83 L376,175 360,344 256,417 151,344 137,175 Z",Grad(C("#563AA1"),C("#071845")),lilac,7);
        Fill(dc,"M256,84 L377,175 256,236 136,175 Z",Grad(C("#52D6FF"),C("#9B5AF7")),cyan,4);
        Fill(dc,"M137,175 L256,236 256,416 151,344 Z",Grad(C("#1C60C5"),C("#17104C")),cyan,4);
        Fill(dc,"M377,175 L256,236 256,416 360,344 Z",Grad(C("#A533C8"),C("#24134D")),magenta,4);
        Stroke(dc,"M137,175 L377,175 M256,84 L256,417 M151,344 L360,344 M137,175 L256,416 L377,175",cyan,3);
        Stroke(dc,"M256,147 L297,225 356,256 297,280 256,360 214,280 156,256 214,225 Z",magenta,5);
        dc.DrawEllipse(new RadialGradientBrush(Colors.White,magenta),null,new Point(256,256),28,28);
        dc.DrawEllipse(Brushes.White,null,new Point(256,256),9,9);
    }

    private static void Chip(DrawingContext dc)
    {
        Color teal=C("#50F4D6"),cyan=C("#69EFFF");
        Fill(dc,"M164,140 L351,140 397,186 397,336 352,381 163,381 114,336 114,185 Z",
            Grad(C("#274A64"),C("#06152C")),teal,7);
        Fill(dc,"M165,160 L347,160 370,185 370,337 345,358 166,358 141,335 141,184 Z",
            Grad(C("#0D2C4C"),C("#092B38")),cyan,4);
        for(int i=0;i<6;i++)
        {
            int y=182+i*28;
            Line(dc,113,y,70,y,teal,7);
            Line(dc,399,y,442,y,teal,7);
        }
        for(int i=0;i<6;i++)
        {
            int x=183+i*29;
            Line(dc,x,140,x,99,cyan,6);
            Line(dc,x,381,x,418,cyan,6);
        }
        Fill(dc,"M195,190 L319,190 342,212 342,305 318,329 195,329 171,305 171,214 Z",
            Grad(C("#143D70"),C("#120D34")),teal,4);
        Stroke(dc,"M192,246 L236,246 236,213 L281,213 281,247 321,247 M192,288 L234,288 234,314 L281,314 281,278 324,278",cyan,7);
        dc.DrawEllipse(new RadialGradientBrush(Colors.White,teal),null,new Point(258,263),24,24);
    }

    private static void Crown(DrawingContext dc)
    {
        Color gold=C("#FFD171"),amber=C("#FF9C48");
        Fill(dc,"M105,334 L83,156 170,215 254,113 338,216 428,156 401,334 Z",
            Grad(C("#FFE8A2"),C("#70351C")),gold,7);
        Fill(dc,"M122,304 L110,205 177,247 254,162 331,247 396,202 383,305 Z",
            Grad(C("#B76B2D"),C("#2C1D36")),amber,3);
        Fill(dc,"M109,319 L405,319 405,363 109,363 Z",Grad(C("#FFF2AA"),C("#95501F")),gold,5);
        Stroke(dc,"M254,113 L254,312 M170,215 L192,302 M338,216 L314,302",gold,3);
        foreach((int x,int y,int radius) in new[]{(85,155,14),(254,112,20),(429,154,14)})
        {
            dc.DrawEllipse(new RadialGradientBrush(Colors.White,gold),null,new Point(x,y),radius,radius);
        }
        foreach(int x in new[]{163,207,254,301,348})
        {
            dc.DrawEllipse(Grad(C("#5AF4FF"),C("#582BBA")),P(gold,3),new Point(x,337),10,10);
        }
    }

    private static void Key(DrawingContext dc)
    {
        Color amber=C("#FFB061"),cyan=C("#64ECFF");
        dc.DrawEllipse(Grad(C("#8356D0"),C("#0A204E")),P(amber,9),new Point(192,195),93,93);
        dc.DrawEllipse(new RadialGradientBrush(C("#3ACFFF"),C("#071629")),P(cyan,4),new Point(192,195),56,56);
        Line(dc,236,254,375,390,amber,30);
        Line(dc,236,254,375,390,C("#FFD992"),16);
        Fill(dc,"M331,346 L390,346 390,381 418,382 418,419 366,420 Z",Grad(C("#FFDF9A"),C("#9A4B28")),amber,5);
        Fill(dc,"M326,349 L350,325 382,354 359,378 Z",Grad(C("#FFD881"),C("#6B3364")),amber,4);
        Stroke(dc,"M165,195 L180,180 205,180 218,195 205,210 180,210 Z",cyan,5);
        dc.DrawEllipse(Brushes.White,null,new Point(175,180),7,7);
    }

    private static void Wild(DrawingContext dc)
    {
        Color pink=C("#FF58DA"),cyan=C("#64EEFF"),gold=C("#FFCB82");
        Fill(dc,"M108,100 L403,100 421,137 421,381 402,409 109,409 89,378 89,138 Z",
            Grad(C("#2E1760"),C("#0B1D4E")),pink,8);
        Stroke(dc,"M117,145 L158,335 213,250 257,341 313,248 361,341 406,145",
            cyan,29);
        Stroke(dc,"M117,145 L158,335 213,250 257,341 313,248 361,341 406,145",
            pink,13);
        Stroke(dc,"M117,145 L158,335 213,250 257,341 313,248 361,341 406,145",
            Brushes.White.Color,3);
        Stroke(dc,"M124,120 L390,120 M125,384 L390,384",gold,4);
        dc.DrawEllipse(new RadialGradientBrush(Colors.White,pink),null,new Point(256,254),26,26);
    }

    private static void VaultWheel(DrawingContext dc)
    {
        Color gold=C("#FFD77F"),violet=C("#CF80FF"),cyan=C("#6BEFFF");
        dc.DrawEllipse(Grad(C("#61427A"),C("#0A1E43")),P(gold,13),new Point(256,251),171,171);
        dc.DrawEllipse(Grad(C("#0C346C"),C("#140C30")),P(violet,6),new Point(256,251),141,141);
        dc.DrawEllipse(null,P(cyan,6),new Point(256,251),118,118);
        for(int i=0;i<8;i++)
        {
            double a=Math.PI*i/4.0;
            double cs=Math.Cos(a),sn=Math.Sin(a);
            Line(dc,256+61*cs,251+61*sn,256+141*cs,251+141*sn,gold,13);
            dc.DrawEllipse(new SolidColorBrush(gold),null,new Point(256+138*cs,251+138*sn),8,8);
        }
        dc.DrawEllipse(Grad(C("#FEDB86"),C("#79427A")),P(cyan,8),new Point(256,251),71,71);
        dc.DrawEllipse(Grad(C("#5C31B0"),C("#0E2E57")),P(gold,5),new Point(256,251),42,42);
        Stroke(dc,"M256,222 L285,250 256,279 227,250 Z",cyan,5);
        dc.DrawEllipse(Brushes.White,null,new Point(256,251),10,10);
    }

    private static void AccessKey(DrawingContext dc)
    {
        Color violet=C("#D8A1FF"),cyan=C("#5AEFFF"),gold=C("#FFCE81");
        Fill(dc,"M129,115 L384,115 418,149 418,355 382,390 129,390 93,356 93,148 Z",
            Grad(C("#442C79"),C("#061E39")),violet,8);
        Fill(dc,"M133,151 L377,151 391,163 391,342 376,358 132,358 119,343 119,162 Z",
            Grad(C("#11365D"),C("#171035")),cyan,3);
        dc.DrawEllipse(Grad(C("#A464FF"),C("#062B4A")),P(gold,7),new Point(197,247),62,62);
        dc.DrawEllipse(Grad(C("#6BEFFF"),C("#111441")),P(violet,3),new Point(197,247),31,31);
        Line(dc,236,268,345,268,gold,18);
        Line(dc,327,268,327,303,gold,13);
        Line(dc,351,268,351,298,gold,13);
        Stroke(dc,"M165,248 L197,218 229,248 197,277 Z",cyan,4);
        Line(dc,150,180,349,180,violet,4);
        Line(dc,150,329,264,329,cyan,4);
        dc.DrawEllipse(Brushes.White,null,new Point(197,246),6,6);
    }

    private static Brush Grad(Color start, Color end) => new LinearGradientBrush(start, end, 80);
    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    private static Pen P(Color color, double width) =>
        new(new SolidColorBrush(color), width) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };

    private static void Fill(DrawingContext dc, string path, Brush fill, Color glow, double thickness)
    {
        Geometry geometry=Geometry.Parse(path);
        dc.DrawGeometry(null,P(Color.FromArgb(28,glow.R,glow.G,glow.B),thickness*5),geometry);
        dc.DrawGeometry(fill,P(glow,thickness),geometry);
    }

    private static void Stroke(DrawingContext dc,string path,Color color,double width)
    {
        Geometry geometry=Geometry.Parse(path);
        dc.DrawGeometry(null,P(Color.FromArgb(24,color.R,color.G,color.B),width*5),geometry);
        dc.DrawGeometry(null,P(color,width),geometry);
    }

    private static void Line(DrawingContext dc,double x1,double y1,double x2,double y2,Color color,double width)
    {
        dc.DrawLine(P(Color.FromArgb(25,color.R,color.G,color.B),width*4),new Point(x1,y1),new Point(x2,y2));
        dc.DrawLine(P(color,width),new Point(x1,y1),new Point(x2,y2));
    }
}
