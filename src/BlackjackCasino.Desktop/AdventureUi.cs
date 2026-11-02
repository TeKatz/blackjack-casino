using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

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
    };

    internal static TextBlock SectionLabel(string text) => new()
    {
        Text = text,
        FontFamily = new FontFamily("Georgia"),
        FontSize = 11,
        FontWeight = FontWeights.SemiBold,
        Foreground = Ink("#92CDB4"),
        Margin = new Thickness(0, 8, 0, 7),
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

    internal static Button Button(string text, bool accent = false, double fontSize = 12.5, bool scaleOnHover = true)
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
            Cursor = CasinoCursors.Hand,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new ScaleTransform(1, 1),
        };

        button.MouseEnter += (_, _) =>
        {
            button.Background = hover;
            button.BorderBrush = hoverBorder;
            button.Effect = new DropShadowEffect
            {
                BlurRadius = accent ? 18 : 14,
                ShadowDepth = 1,
                Opacity = .32,
                Color = (Color)ColorConverter.ConvertFromString(accent ? "#9B814D" : "#2C7563"),
            };
            if (scaleOnHover && button.RenderTransform is ScaleTransform scale)
            {
                scale.ScaleX = 1.012;
                scale.ScaleY = 1.012;
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
            highlighted ? "#302D20" : "#0D1917",
            highlighted ? "#242019" : "#091311",
            highlighted ? "#B09152" : "#2E4A43",
            new Thickness(9, 9, 9, 10),
            radius: 9,
            shadow: false);
        tile.Margin = new Thickness(0, 0, 7, 7);
        tile.MinHeight = 68;

        var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        var labelRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        FrameworkElement icon = StatIcon(label, highlighted);
        icon.Margin = new Thickness(0, 0, 5, 0);
        labelRow.Children.Add(icon);
        labelRow.Children.Add(new TextBlock
        {
            Text = label,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 9.5,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink(highlighted ? "#E5C97F" : "#8DA79E"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        stack.Children.Add(labelRow);
        stack.Children.Add(new TextBlock
        {
            Text = value,
            FontFamily = new FontFamily("Georgia"),
            FontSize = 15.5,
            FontWeight = highlighted ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = Ink(highlighted ? "#FFF0C7" : "#EEE2CF"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 5, 0, 0),
        });
        tile.Child = stack;

        if (highlighted)
        {
            tile.Effect = new DropShadowEffect
            {
                BlurRadius = 13,
                ShadowDepth = 0,
                Opacity = .24,
                Color = (Color)ColorConverter.ConvertFromString("#C5A55B"),
            };
        }

        return tile;
    }

    private static FrameworkElement StatIcon(string label, bool highlighted)
    {
        string data = label.ToUpperInvariant() switch
        {
            "HP" => "M12,21 C8,18 3,14 3,9 C3,5.7 5.2,4 7.8,4 C9.5,4 11,5 12,6.4 C13,5 14.5,4 16.2,4 C18.8,4 21,5.7 21,9 C21,14 16,18 12,21 Z",
            "ARMOR" => "M12,2 L20,5 V10.5 C20,16 16.6,20 12,22 C7.4,20 4,16 4,10.5 V5 Z",
            "XP" => "M12,2 L14.9,8.4 L22,9.1 L16.7,13.8 L18.3,21 L12,17.4 L5.7,21 L7.3,13.8 L2,9.1 L9.1,8.4 Z",
            "STR" => "M17.8,2.5 L21.5,6.2 L13.8,13.9 L10.1,10.2 Z M9.2,12.1 L11.9,14.8 L9.2,17.5 L7.7,16 L4.2,19.5 L2.5,17.8 L6,14.3 L4.5,12.8 L7.2,10.1 Z",
            "DEX" => "M3,12 L18.5,4.5 L16.1,10 L22,12 L16.1,14 L18.5,19.5 Z",
            "CON" => "M12,2 L19.5,5 V11 C19.5,16.2 16.2,19.4 12,21.5 C7.8,19.4 4.5,16.2 4.5,11 V5 Z M12,5 V18.2",
            "INT" => "M3,5.5 C6.8,4.5 9.5,5.2 12,7.2 C14.5,5.2 17.2,4.5 21,5.5 V19 C17.2,18 14.5,18.7 12,20.7 C9.5,18.7 6.8,18 3,19 Z M12,7.2 V20.7",
            "WIS" => "M2,12 C5,7.4 8.1,5.5 12,5.5 C15.9,5.5 19,7.4 22,12 C19,16.6 15.9,18.5 12,18.5 C8.1,18.5 5,16.6 2,12 Z M12,9 A3,3 0 1 0 12,15 A3,3 0 1 0 12,9",
            "CHA" => "M3,18 L5.2,7.2 L10,12 L12,4 L14,12 L18.8,7.2 L21,18 Z M5,21 H19",
            _ => "M12,3 A9,9 0 1 0 12,21 A9,9 0 1 0 12,3",
        };

        var path = new Path
        {
            Data = Geometry.Parse(data),
            Stretch = Stretch.Uniform,
            Stroke = Ink(highlighted ? "#E3C574" : "#79A994"),
            Fill = label is "HP" or "ARMOR" or "XP" ? Ink(highlighted ? "#9D8145" : "#29473D") : Brushes.Transparent,
            StrokeThickness = 1.55,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
        };
        return new Viewbox
        {
            Width = 16,
            Height = 16,
            Child = path,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
