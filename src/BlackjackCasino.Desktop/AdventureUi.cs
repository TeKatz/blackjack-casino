using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

internal static class AdventureUi
{
    internal static Brush Ink(string hex) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

    internal static Border Panel(
        string top,
        string bottom,
        string border,
        Thickness padding,
        double radius = 14,
        bool shadow = true)
    {
        var panel = new Border
        {
            Background = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(top),
                (Color)ColorConverter.ConvertFromString(bottom),
                90),
            BorderBrush = Ink(border),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(radius),
            Padding = padding,
        };

        if (shadow)
        {
            panel.Effect = new DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = .32,
                Color = Colors.Black,
            };
        }

        return panel;
    }

    internal static TextBlock Eyebrow(string text, double size = 11) => new()
    {
        Text = text,
        FontFamily = new FontFamily("Georgia"),
        FontSize = size,
        FontWeight = FontWeights.SemiBold,
        Foreground = Ink("#86C8AE"),
        CharacterSpacing = 70,
    };

    internal static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontFamily = new FontFamily("Georgia"),
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        Foreground = Ink("#92CDB4"),
        Margin = new Thickness(0, 8, 0, 7),
        CharacterSpacing = 55,
    };

    internal static FrameworkElement OrnamentRule(string glyph = "◆")
    {
        var grid = new Grid { Margin = new Thickness(0, 10, 0, 14), Opacity = .72 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var left = new Border { Height = 1, Background = Ink("#6F6245"), VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(left);
        var mark = new TextBlock
        {
            Text = glyph,
            FontSize = 10,
            Foreground = Ink("#C8AD70"),
            Margin = new Thickness(10, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(mark, 1);
        grid.Children.Add(mark);
        var right = new Border { Height = 1, Background = Ink("#6F6245"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);
        return grid;
    }

    internal static Button Button(string text, bool accent = false, double fontSize = 12.5)
    {
        Brush normal = Ink(accent ? "#263425" : "#13231F");
        Brush hover = Ink(accent ? "#34462E" : "#1B312A");
        Brush normalBorder = Ink(accent ? "#9B814D" : "#49675F");
        Brush hoverBorder = Ink(accent ? "#D6B96E" : "#77A392");

        var button = new Button
        {
            Content = text,
            FontFamily = new FontFamily("Georgia"),
            FontSize = fontSize,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink(accent ? "#F1DCA4" : "#EEE2CF"),
            Background = normal,
            BorderBrush = normalBorder,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(17, 10, 17, 10),
            Cursor = Cursors.Hand,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new ScaleTransform(1, 1),
        };

        button.MouseEnter += (_, _) =>
        {
            button.Background = hover;
            button.BorderBrush = hoverBorder;
            button.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 2,
                Opacity = .34,
                Color = (Color)ColorConverter.ConvertFromString(accent ? "#9B814D" : "#2C7563"),
            };
            if (button.RenderTransform is ScaleTransform scale)
            {
                scale.ScaleX = 1.018;
                scale.ScaleY = 1.018;
            }
        };

        button.MouseLeave += (_, _) =>
        {
            button.Background = normal;
            button.BorderBrush = normalBorder;
            button.Effect = null;
            if (button.RenderTransform is ScaleTransform scale)
            {
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
        };

        return button;
    }

    internal static Border StatTile(string label, string value, bool highlighted = false)
    {
        var tile = Panel(
            highlighted ? "#2B3022" : "#0D1917",
            highlighted ? "#20251B" : "#091311",
            highlighted ? "#A4884E" : "#2E4A43",
            new Thickness(9),
            radius: 9,
            shadow: false);
        tile.Margin = new Thickness(0, 0, 7, 7);

        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink(highlighted ? "#E3C883" : "#819D94"),
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15,
            Foreground = Ink("#EEE2CF"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 0),
        });
        tile.Child = stack;
        return tile;
    }
}
