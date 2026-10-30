using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal sealed class NineLivesLobbyArtwork : Grid
{
    internal NineLivesLobbyArtwork()
    {
        ClipToBounds = true;
        Background = new LinearGradientBrush(
            Color.FromRgb(10, 7, 18),
            Color.FromRgb(25, 9, 35),
            28);

        // Keep a little of the authored rooftop atmosphere, but never depend on it alone for
        // lobby readability. The foreground composition below remains useful even if the
        // background asset has to fall back to safe vector artwork.
        Children.Add(new Image
        {
            Source = NineLivesArtwork.Background,
            Stretch = Stretch.UniformToFill,
            Opacity = 0.28,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
        });

        Children.Add(new Border
        {
            Background = new LinearGradientBrush(
                Color.FromArgb(40, 8, 6, 16),
                Color.FromArgb(210, 8, 5, 16),
                90),
            IsHitTestVisible = false,
        });

        var stage = new Canvas
        {
            Width = 360,
            Height = 185,
            ClipToBounds = true,
        };
        Children.Add(new Viewbox
        {
            Stretch = Stretch.UniformToFill,
            Child = stage,
        });

        // Moon and rooftop silhouettes establish the setting immediately, independent from
        // whatever crop the full-size machine background happens to use.
        var moon = new Ellipse
        {
            Width = 76,
            Height = 76,
            Fill = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(255, 236, 174), 0),
                    new GradientStop(Color.FromRgb(232, 193, 106), 0.62),
                    new GradientStop(Color.FromArgb(0, 201, 92, 255), 1),
                },
            },
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(242, 205, 117),
                BlurRadius = 22,
                ShadowDepth = 0,
                Opacity = 0.52,
            },
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(moon, 252);
        Canvas.SetTop(moon, -18);
        stage.Children.Add(moon);

        stage.Children.Add(new Polygon
        {
            Points = new PointCollection([
                new Point(0, 163), new Point(36, 132), new Point(70, 153), new Point(111, 118),
                new Point(151, 151), new Point(205, 112), new Point(247, 148), new Point(292, 124),
                new Point(360, 159), new Point(360, 185), new Point(0, 185)
            ]),
            Fill = new SolidColorBrush(Color.FromArgb(220, 7, 6, 14)),
            IsHitTestVisible = false,
        });

        // Three large real slot symbols make the preview read as Nine Lives rather than an
        // abstract fallback graphic. The centre tile is deliberately dominant like a hero icon.
        AddSymbolCard(stage, NineLivesSymbol.BlackCat, 35, 51, 88, 94, "#D96B4DFF", 0.94);
        AddSymbolCard(stage, NineLivesSymbol.Bonus, 136, 35, 92, 110, "#EEF0CC72", 1.0);
        AddSymbolCard(stage, NineLivesSymbol.Yarn, 239, 55, 84, 90, "#C9C95CFF", 0.92);

        // A subtle five-line motif reinforces the actual 3x3/five-lines machine identity.
        for (int line = 0; line < 5; line++)
        {
            double y = 151 + (line * 4.4);
            stage.Children.Add(new Line
            {
                X1 = 62,
                Y1 = y,
                X2 = 300,
                Y2 = y - ((line % 2 == 0 ? 1 : -1) * 5),
                Stroke = new SolidColorBrush(Color.FromArgb((byte)(35 + line * 8), 201, 92, 255)),
                StrokeThickness = line == 2 ? 1.6 : 0.9,
                IsHitTestVisible = false,
            });
        }

        // Small cat-eye glints give the card a signature focal point without adding duplicate text.
        AddEye(stage, 13, 29, "#F3D06E");
        AddEye(stage, 28, 29, "#C95CFF");
    }

    private static void AddSymbolCard(
        Canvas stage,
        NineLivesSymbol symbol,
        double left,
        double top,
        double width,
        double height,
        string borderHex,
        double opacity)
    {
        var image = new Image
        {
            Source = NineLivesArtwork.Symbol(symbol),
            Stretch = Stretch.UniformToFill,
            Opacity = opacity,
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

        var card = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(13),
            BorderBrush = Ink(borderHex),
            BorderThickness = new Thickness(1.7),
            Background = Ink("#F20A0712"),
            ClipToBounds = true,
            Child = image,
            Effect = new DropShadowEffect
            {
                Color = symbol == NineLivesSymbol.Bonus
                    ? Color.FromRgb(242, 204, 114)
                    : Color.FromRgb(201, 92, 255),
                BlurRadius = symbol == NineLivesSymbol.Bonus ? 18 : 14,
                ShadowDepth = 0,
                Opacity = symbol == NineLivesSymbol.Bonus ? 0.62 : 0.42,
            },
        };
        Canvas.SetLeft(card, left);
        Canvas.SetTop(card, top);
        stage.Children.Add(card);
    }

    private static void AddEye(Canvas stage, double left, double top, string color)
    {
        var eye = new Ellipse
        {
            Width = 9,
            Height = 5,
            Fill = Ink(color),
            Effect = new DropShadowEffect
            {
                Color = InkColor(color),
                BlurRadius = 9,
                ShadowDepth = 0,
                Opacity = 0.8,
            },
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(eye, left);
        Canvas.SetTop(eye, top);
        stage.Children.Add(eye);
    }

    private static SolidColorBrush Ink(string hex) => new(InkColor(hex));
    private static Color InkColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
