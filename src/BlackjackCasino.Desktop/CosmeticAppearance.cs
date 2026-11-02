using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class CosmeticAppearance
{
    internal static Brush TitleBrush(CosmeticDefinition title)
    {
        if (title.VisualVariant == "title-adventure-names")
        {
            return new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#D7C17B"),
                (Color)ColorConverter.ConvertFromString("#8FD5B4"),
                0);
        }
        if (title.VisualVariant == "title-adventure-bell")
        {
            return new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString("#65D7CF"),
                (Color)ColorConverter.ConvertFromString("#F0D38A"),
                0);
        }

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(title.VisualVariant switch
        {
            "title-violet" => "#CEAAFF", "title-gold" => "#FFD877", "title-teal" => "#68E9CD",
            "title-rose" => "#FFAACD", "title-ice" => "#A7DEFF", "title-amber" => "#FFB968",
            "title-neon-cyan" => "#63F5F0", "title-neon-magenta" => "#FF6BC7",
            "title-dynasty-gold" => "#FFD36E", "title-dynasty-prism" => "#E7B6FF",
            _ => "#F3E6CF",
        }));
    }

    internal static Color StandardFrame(string variant) => (Color)ColorConverter.ConvertFromString(variant switch
    {
        "static-slate" => "#92A4B8", "static-copper" => "#CF966E",
        "static-sage" => "#9BC5A5", "static-pearl" => "#E1DFD3", _ => "#52695A",
    });
}
