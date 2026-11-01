using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BlackjackCasino.Desktop;

/// <summary>One scalable, offline emblem set for lobby destinations and header shortcuts.</summary>
internal static class ProgressionArtwork
{
    internal static FrameworkElement Create(string section, double size)
    {
        string color = section switch { "Season Pass" => "#F4C965", "Cosmetic Shop" => "#C1A0FF", "Challenges" => "#85E6B9", "Overview" => "#8BCBEE", _ => "#F3D18C" };
        var ink = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        var canvas = new Canvas { Width = 100, Height = 100, IsHitTestVisible = false };
        canvas.Children.Add(new Ellipse { Width = 96, Height = 96, Fill = new RadialGradientBrush(Color.FromArgb(65, ink.Color.R, ink.Color.G, ink.Color.B), Colors.Transparent) });
        void Path(string geometry, double thickness = 2.5, bool filled = false)
        {
            canvas.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(geometry), Stroke = ink, StrokeThickness = thickness, StrokeLineJoin = PenLineJoin.Round, Fill = filled ? new SolidColorBrush(Color.FromArgb(36, ink.Color.R, ink.Color.G, ink.Color.B)) : null });
        }
        switch (section)
        {
            case "Season Pass":
                Path("M30,16 L43,36 50,31 58,36 71,16 65,47 36,47 Z", 2.5, true);
                Path("M50,43 A20,20 0 1 1 49.9,43 M50,49 L54,58 64,60 57,67 59,77 50,72 41,77 43,67 36,60 46,58 Z", 2.5, true);
                Path("M37,81 L32,94 48,86 M63,81 L68,94 52,86", 2, true); break;
            case "Cosmetic Shop":
                Path("M18,35 L37,29 50,34 63,29 82,35 77,64 65,76 50,67 35,76 23,64 Z", 2.5, true);
                Path("M29,47 Q37,39 45,49 Q37,58 29,47 M55,49 Q63,39 71,47 Q63,58 55,49 M44,61 Q50,55 56,61");
                Path("M72,13 L75,22 84,25 75,28 72,37 69,28 60,25 69,22 Z M20,76 L23,83 30,86 23,89 20,96 17,89 10,86 17,83 Z", 1.5, true); break;
            case "Challenges":
                Path("M33,18 L67,18 65,47 Q64,65 50,70 Q36,65 35,47 Z", 2.5, true);
                Path("M34,25 L20,25 Q15,51 38,51 M66,25 L80,25 Q85,51 62,51 M50,70 L50,82 M36,87 L64,87 M39,82 L61,82");
                Path("M41,40 L48,47 60,32", 4); break;
            case "Overview":
                Path("M18,81 L18,20 M18,81 L84,81 M29,71 L29,57 40,57 40,71 M49,71 L49,43 60,43 60,71 M69,71 L69,28 80,28 80,71", 2, true);
                Path("M27,44 L47,31 59,35 80,14 M70,14 L80,14 80,24", 3); break;
            default:
                Path("M24,22 L77,22 77,85 24,85 Q17,85 17,78 L17,29 Q17,22 24,22 Z", 2.5, true);
                Path("M27,23 L27,84 M34,32 L68,32 68,73 34,73 Z M22,17 L71,17 M30,12 L75,12", 1.5);
                Path("M51,39 L55,49 65,53 55,57 51,67 47,57 37,53 47,49 Z", 2, true); break;
        }
        var visual = new Viewbox { Width = size, Height = size, Child = canvas, Stretch = Stretch.Uniform };
        System.Windows.Automation.AutomationProperties.SetName(visual, section + " emblem");
        return visual;
    }
}
