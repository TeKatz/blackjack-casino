using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Shared visual primitives for Chill Lounge games. New lounge modes should prefer these
/// helpers before introducing game-local copies of the same buttons, cards or palette values.
/// </summary>
internal static class LoungeUi
{
    public static SolidColorBrush Ink(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    public static Button Button(string text) => new()
    {
        Content = text,
        Foreground = Ink("#EEE4D4"),
        Background = Ink("#17282A"),
        BorderBrush = Ink("#4A6661"),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(15, 10, 15, 10),
        FontWeight = FontWeights.SemiBold,
        Cursor = Cursors.Hand,
        HorizontalContentAlignment = HorizontalAlignment.Center,
    };

    public static Button SmallActionButton(string text) => new()
    {
        Content = text,
        Foreground = Ink("#EEE4D4"),
        Background = Ink("#17282A"),
        BorderBrush = Ink("#4A6661"),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(10, 6, 10, 6),
        FontSize = 9,
        FontWeight = FontWeights.SemiBold,
        Cursor = Cursors.Hand,
        MinWidth = 82,
        HorizontalContentAlignment = HorizontalAlignment.Center,
    };

    public static Border Card(UIElement child, Thickness? padding = null) => new()
    {
        Child = child,
        Background = Ink("#101F21"),
        BorderBrush = Ink("#42645E"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(14),
        Padding = padding ?? new Thickness(22),
    };

    public static TextBlock Eyebrow(string text) => new()
    {
        Text = text,
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Foreground = Ink("#8EC9B0"),
    };
}
