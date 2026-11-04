using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>One palette for the shop D6, Midnight Dice and the adventure D20.</summary>
internal sealed record DiceAppearance(Color Top, Color Bottom, Color Pips, Color Edge, Color? Glow = null, bool Engraved = false)
{
    internal static DiceAppearance For(string variant) => variant switch
    {
        "dice-obsidian" => Make("#35404A", "#0C1119", "#F4D483", "#C4A05B"),
        "dice-jade" => Make("#4A987D", "#123E36", "#F5EDD5", "#97C7A4"),
        "dice-violet" => Make("#9471B7", "#35224D", "#FFE5A5", "#CBA7E7"),
        "dice-ice" => Make("#B7DAEE", "#4D7594", "#132F49", "#D5EDFA"),
        "dice-ruby" => Make("#AC435E", "#401A30", "#FFF0D2", "#E3A696", "#F06F95"),
        "dice-aurora" => Make("#389E97", "#573776", "#E1FFF4", "#8BE5D3", "#86EDDA"),
        "dice-porcelain" => Make("#FFFDF4", "#D4DFE9", "#204E83", "#6C93B7"),
        "dice-forest" => Make("#527663", "#1A372D", "#F4E8CF", "#97AD8D"),
        "dice-copper" => Make("#E0B38B", "#87503B", "#302A29", "#F2CE9E", engraved: true),
        "dice-sapphire" => Make("#3C70A7", "#132642", "#E5F0FD", "#A3BBD7", engraved: true),
        "dice-astral-opal" => Make("#BCAEDB", "#407777", "#172D42", "#E1EBFF", "#B7E8FF", true),
        "dice-solar-crown" => Make("#37323B", "#0E1019", "#FFE0A0", "#DDBB75", "#F6CF7F", true),
        _ => Make("#FFFCEE", "#D7CBB2", "#2D3730", "#BDA36E"),
    };

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
