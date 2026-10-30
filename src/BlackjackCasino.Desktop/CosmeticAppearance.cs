using System.Windows.Media;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class CosmeticAppearance
{
    internal static Brush TitleBrush(CosmeticDefinition title) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(title.VisualVariant switch
    {
        "title-violet" => "#CEAAFF", "title-gold" => "#FFD877", "title-teal" => "#68E9CD",
        "title-rose" => "#FFAACD", "title-ice" => "#A7DEFF", "title-amber" => "#FFB968",
        _ => "#F3E6CF",
    }));
    internal static Color StandardFrame(string variant) => (Color)ColorConverter.ConvertFromString(variant switch
    {
        "static-slate" => "#92A4B8", "static-copper" => "#CF966E",
        "static-sage" => "#9BC5A5", "static-pearl" => "#E1DFD3", _ => "#52695A",
    });
}
