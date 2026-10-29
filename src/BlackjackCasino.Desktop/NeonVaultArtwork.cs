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
            Color.FromRgb(10, 9, 29),
            Color.FromRgb(9, 31, 38),
            35);

        Children.Add(new Ellipse
        {
            Width = 280,
            Height = 280,
            Fill = new RadialGradientBrush(Color.FromArgb(82, 52, 222, 255), Colors.Transparent),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(-115, -110, 0, 0),
            IsHitTestVisible = false,
        });
        Children.Add(new Ellipse
        {
            Width = 230,
            Height = 230,
            Fill = new RadialGradientBrush(Color.FromArgb(60, 181, 71, 255), Colors.Transparent),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, -95, -110),
            IsHitTestVisible = false,
        });

        var cabinet = new Border
        {
            Width = 276,
            Height = 126,
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1.5),
            BorderBrush = Ink("#68E8FF"),
            Background = Ink("#CC090D1D"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Effect = new DropShadowEffect { Color = Color.FromRgb(62, 219, 255), BlurRadius = 18, ShadowDepth = 0, Opacity = 0.42 },
        };
        var reelGrid = new Grid { Margin = new Thickness(10, 12, 10, 12) };
        for (int reel = 0; reel < 5; reel++)
        {
            reelGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var reelPanel = new Border
            {
                Margin = new Thickness(2, 0, 2, 0),
                CornerRadius = new CornerRadius(5),
                BorderBrush = Ink(reel is 1 or 3 ? "#946DFF" : "#255D73"),
                BorderThickness = new Thickness(1),
                Background = new LinearGradientBrush(InkColor("#11172B"), InkColor("#070A16"), 90),
            };
            var symbols = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            string[] glyphs = reel switch
            {
                0 => ["◆", "✦", "●"],
                1 => ["W", "♛", "◇"],
                2 => ["▣", "◆", "W"],
                3 => ["◇", "W", "✦"],
                _ => ["●", "♛", "◆"],
            };
            foreach (string glyph in glyphs)
            {
                symbols.Children.Add(new TextBlock
                {
                    Text = glyph,
                    Foreground = glyph == "W" ? Ink("#FFE5A1") : Ink("#9DEFFF"),
                    FontSize = 17,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 1),
                });
            }
            reelPanel.Child = symbols;
            Grid.SetColumn(reelPanel, reel);
            reelGrid.Children.Add(reelPanel);
        }
        cabinet.Child = reelGrid;
        Children.Add(cabinet);

        Children.Add(new TextBlock
        {
            Text = "NEON VAULT",
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = Ink("#E7FBFF"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 13, 0, 0),
            Effect = new DropShadowEffect { Color = Color.FromRgb(71, 221, 255), BlurRadius = 9, ShadowDepth = 0, Opacity = 0.75 },
        });
        Children.Add(new TextBlock
        {
            Text = "SYSTEM // VAULT ONLINE",
            FontSize = 8,
            Foreground = Ink("#69DFF2"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 13),
        });

        Children.Add(new Rectangle
        {
            Height = 1,
            Fill = Ink("#66A6F4FF"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(34, 0, 34, 0),
            IsHitTestVisible = false,
        });
    }

    private static SolidColorBrush Ink(string hex) => new(InkColor(hex));
    private static Color InkColor(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
