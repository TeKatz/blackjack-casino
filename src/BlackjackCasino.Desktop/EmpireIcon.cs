using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Original, resolution-independent club insignia on a common 24-unit grid.</summary>
internal sealed class EmpireIcon : FrameworkElement
{
    internal EmpireIcon(string kind, double size = 20)
    {
        Kind = kind; Width = Height = size; IsHitTestVisible = false;
        System.Windows.Automation.AutomationProperties.SetName(this, kind);
    }
    internal string Kind { get; }
    private Geometry? glyph;
    private static readonly Pen Gold = MakePen("#C9AD73");
    private static readonly Pen Sage = MakePen("#91BEA4");
    private static Pen MakePen(string color)
    {
        var pen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), 1.4)
            { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        pen.Freeze(); return pen;
    }
    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth / 24, ActualHeight / 24));
        string path = Kind switch
        {
            "Production" => "M3,19 L3,13 7,13 7,19 M10,19 L10,9 14,9 14,19 M17,19 L17,4 21,4 21,19 M2,22 L22,22",
            "Budget" => "M3,8 L21,8 21,20 3,20 Z M3,8 L5,4 19,4 21,8 M8,12 L16,12 M8,16 L13,16",
            "Dividend" => "M3,5 L21,5 21,19 3,19 Z M7,8 L5,10 M17,16 L19,14 M12,8 C6,8 6,16 12,16 C18,16 18,8 12,8 Z",
            "Managers" or "Operations" => "M8,7 C8,2 16,2 16,7 C16,12 8,12 8,7 M4,21 L5,15 9,12 12,16 15,12 19,15 20,21 Z M12,16 L12,21",
            "Automation" => "M5,8 C8,1 19,3 20,10 M16,7 L20,10 22,6 M19,16 C16,23 5,21 4,14 M8,17 L4,14 2,18 M9,12 L11,14 15,10",
            "Synergies" => "M8,5 L12,2 16,5 16,10 12,13 8,10 Z M2,14 L6,11 10,14 10,19 6,22 2,19 Z M14,14 L18,11 22,14 22,19 18,22 14,19 Z",
            "Milestones" or "Growth" => "M4,21 L4,3 M4,4 L19,4 15,8 19,12 4,12 M2,22 L9,22",
            "Research" or "Development" => "M9,3 L15,3 M10,3 L10,10 4,19 Q3,22 6,22 L18,22 Q21,22 20,19 L14,10 14,3 M7,16 L17,16 M10,19 L11,19 M14,18 L15,18",
            "Projects" => "M3,21 L3,10 12,3 21,10 21,21 Z M8,21 L8,14 16,14 16,21 M12,3 L12,10 M8,7 L16,7",
            "Properties" or "Estate" => "M2,21 L22,21 M4,21 L4,10 20,10 20,21 M2,10 L12,2 22,10 M8,13 L8,18 M12,13 L12,18 M16,13 L16,18",
            "Contracts" => "M5,2 L16,2 20,6 20,22 5,22 Z M16,2 L16,7 20,7 M8,11 L16,11 M8,15 L16,15 M8,19 L12,19",
            "Events" or "Opportunities" => "M4,6 L20,6 20,21 4,21 Z M4,10 L20,10 M8,3 L8,8 M16,3 L16,8 M8,14 L10,14 M14,14 L16,14 M8,18 L10,18",
            "Prestige" => "M3,6 L7,10 12,3 17,10 21,6 19,18 5,18 Z M5,21 L19,21",
            "Performance" => "M3,18 C-1,1 25,1 21,18 M12,14 L17,7 M7,20 L17,20 M5,12 L7,12 M11,5 L11,7",
            "Tables" => "M3,8 Q12,1 21,8 L19,17 Q12,21 5,17 Z M7,9 L7,13 M12,7 L12,11 M17,9 L17,13",
            "Slots" => "M4,3 L18,3 18,21 4,21 Z M6,8 L16,8 16,15 6,15 Z M9,8 L9,15 M13,8 L13,15 M18,7 L22,7 22,14 18,14",
            "Hospitality" => "M4,9 L18,9 17,18 Q11,23 5,18 Z M18,10 C24,8 24,18 18,17 M8,2 L8,6 M13,2 L13,6",
            _ => "M12,2 L22,12 12,22 2,12 Z M8,12 L12,8 16,12 12,16 Z",
        };
        if (glyph is null) { glyph = Geometry.Parse(path); glyph.Freeze(); }
        dc.DrawGeometry(null, Gold, glyph);
        dc.DrawLine(Sage, new Point(10, 23), new Point(14, 23));
        dc.Pop();
    }
}
