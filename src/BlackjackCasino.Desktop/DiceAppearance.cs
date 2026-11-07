using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>One palette for the shop D6, Midnight Dice and the adventure D20.</summary>
internal sealed record DiceAppearance(Color Top, Color Bottom, Color Pips, Color Edge, Color? Glow = null, bool Engraved = false)
{
    internal static DiceAppearance For(string variant)
    {
        DicePaletteTokens palette = DicePaletteTokens.For(variant);
        return Make(palette.Top, palette.Bottom, palette.Pips,
            palette.Edge, palette.Glow, palette.Engraved);
    }

    internal static Pen Halo(Color color, double width, byte alpha) =>
        new(new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B)), width);

    internal Brush Reflection => new LinearGradientBrush(new GradientStopCollection
    {
        new(Color.FromArgb(65, 255, 255, 255), 0), new(Colors.Transparent, .35),
        new(Colors.Transparent, .7), new(Color.FromArgb(22, Edge.R, Edge.G, Edge.B), 1),
    }, new Point(0, 0), new Point(1, 1));

    private static DiceAppearance Make(string top, string bottom, string pips, string edge, string? glow = null, bool engraved = false) =>
        new(C(top), C(bottom), C(pips), C(edge), glow is null ? null : C(glow), engraved);
    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
