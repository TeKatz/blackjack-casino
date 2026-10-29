using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace BlackjackCasino.Desktop;

internal sealed class NeonVaultArtwork : Grid
{
    internal NeonVaultArtwork()
    {
        ClipToBounds = true;
        Background = new LinearGradientBrush(
            Color.FromRgb(5, 16, 27),
            Color.FromRgb(13, 8, 31),
            22);

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

        // Large vault-door rings make the mode readable as "Neon Vault" even at card size,
        // without repeating the title inside the artwork itself.
        stage.Children.Add(new Ellipse
        {
            Width = 228,
            Height = 228,
            Stroke = Ink("#194FE5FF"),
            StrokeThickness = 2,
            Fill = new RadialGradientBrush(Color.FromArgb(44, 40, 188, 255), Colors.Transparent),
            Effect = new BlurEffect { Radius = 1.5, RenderingBias = RenderingBias.Performance },
        });
        Canvas.SetLeft(stage.Children[^1], 205);
        Canvas.SetTop(stage.Children[^1], -70);

        stage.Children.Add(new Ellipse
        {
            Width = 168,
            Height = 168,
            Stroke = Ink("#5668E8FF"),
            StrokeThickness = 2,
        });
        Canvas.SetLeft(stage.Children[^1], 235);
        Canvas.SetTop(stage.Children[^1], -40);

        stage.Children.Add(new Ellipse
        {
            Width = 103,
            Height = 103,
            Stroke = Ink("#7EBA78FF"),
            StrokeThickness = 1.5,
        });
        Canvas.SetLeft(stage.Children[^1], 267);
        Canvas.SetTop(stage.Children[^1], -7);

        for (int spoke = 0; spoke < 8; spoke++)
        {
            double angle = spoke * Math.PI / 4.0;
            Point centre = new(318.5, 44.5);
            Point end = new(
                centre.X + Math.Cos(angle) * 48,
                centre.Y + Math.Sin(angle) * 48);
            stage.Children.Add(new Line
            {
                X1 = centre.X,
                Y1 = centre.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Ink(spoke % 2 == 0 ? "#866AEAFF" : "#6253D6FF"),
                StrokeThickness = 1.2,
                IsHitTestVisible = false,
            });
        }

        // Main five-reel window. It is deliberately larger than the old miniature cabinet so
        // the symbols remain legible in the lobby card and the artwork reads as game key art.
        var cabinet = new Border
        {
            Width = 306,
            Height = 126,
            CornerRadius = new CornerRadius(15),
            BorderThickness = new Thickness(1.8),
            BorderBrush = Ink("#66E9FAFF"),
            Background = new LinearGradientBrush(InkColor("#EA07111F"), InkColor("#F10B071B"), 90),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(64, 221, 255),
                BlurRadius = 22,
                ShadowDepth = 0,
                Opacity = 0.52,
                RenderingBias = RenderingBias.Performance,
            },
        };
        Canvas.SetLeft(cabinet, 27);
        Canvas.SetTop(cabinet, 27);
        stage.Children.Add(cabinet);

        var reelGrid = new Grid { Margin = new Thickness(9, 9, 9, 9) };
        cabinet.Child = reelGrid;
        for (int reel = 0; reel < 5; reel++)
        {
            reelGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var reelPanel = new Border
            {
                Margin = new Thickness(2.5, 0, 2.5, 0),
                CornerRadius = new CornerRadius(7),
                BorderBrush = Ink(reel is 1 or 3 ? "#916F7CFF" : "#6545DDF8"),
                BorderThickness = new Thickness(1.2),
                Background = new LinearGradientBrush(InkColor("#F0141830"), InkColor("#F0060914"), 90),
            };

            var symbols = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            symbols.RowDefinitions.Add(new RowDefinition());
            symbols.RowDefinitions.Add(new RowDefinition());
            symbols.RowDefinitions.Add(new RowDefinition());

            string[] glyphs = reel switch
            {
                0 => ["◆", "✦", "●"],
                1 => ["W", "♛", "◇"],
                2 => ["▣", "◆", "W"],
                3 => ["◇", "W", "✦"],
                _ => ["●", "♛", "◆"],
            };

            for (int row = 0; row < glyphs.Length; row++)
            {
                string glyph = glyphs[row];
                var symbol = new TextBlock
                {
                    Text = glyph,
                    Foreground = glyph == "W" ? Ink("#FFE5A1") : Ink(row == 1 ? "#C9F7FF" : "#72DAF4"),
                    FontSize = glyph == "W" ? 21 : 17,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Effect = new DropShadowEffect
                    {
                        Color = glyph == "W" ? Color.FromRgb(255, 206, 104) : Color.FromRgb(82, 224, 255),
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.56,
                    },
                };
                Grid.SetRow(symbol, row);
                symbols.Children.Add(symbol);
            }

            reelPanel.Child = symbols;
            Grid.SetColumn(reelPanel, reel);
            reelGrid.Children.Add(reelPanel);
        }

        // A central scan line and corner accents sell the high-voltage presentation without
        // adding tiny status text that competes with the real card labels below the artwork.
        stage.Children.Add(new Rectangle
        {
            Width = 292,
            Height = 2,
            Fill = Ink("#B35BEAFF"),
            Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(80, 227, 255),
                BlurRadius = 11,
                ShadowDepth = 0,
                Opacity = 0.74,
            },
            IsHitTestVisible = false,
        });
        Canvas.SetLeft(stage.Children[^1], 34);
        Canvas.SetTop(stage.Children[^1], 89);

        stage.Children.Add(new Polygon
        {
            Points = new PointCollection([new Point(0, 0), new Point(66, 0), new Point(42, 22), new Point(0, 22)]),
            Fill = new LinearGradientBrush(InkColor("#4E3CE8FF"), Colors.Transparent, 0),
            IsHitTestVisible = false,
        });
        Canvas.SetLeft(stage.Children[^1], 0);
        Canvas.SetTop(stage.Children[^1], 0);

        stage.Children.Add(new Polygon
        {
            Points = new PointCollection([new Point(0, 22), new Point(24, 0), new Point(86, 0), new Point(86, 22)]),
            Fill = new LinearGradientBrush(Colors.Transparent, InkColor("#45A86BFF"), 0),
            IsHitTestVisible = false,
        });
        Canvas.SetLeft(stage.Children[^1], 274);
        Canvas.SetTop(stage.Children[^1], 163);
    }

    private static SolidColorBrush Ink(string hex) => new(InkColor(hex));
    private static Color InkColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
