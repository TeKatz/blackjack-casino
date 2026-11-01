using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Compact vector vault-door mark for the persistent casino header.</summary>
internal sealed class VaultHeaderArtwork : FrameworkElement
{
    internal VaultHeaderArtwork()
    {
        Width = 32;
        Height = 32;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        Brush panel = new LinearGradientBrush(Color.FromRgb(20, 39, 37), Color.FromRgb(8, 22, 24), 45);
        Brush brass = new SolidColorBrush(Color.FromRgb(218, 177, 79));
        Brush brassLight = new SolidColorBrush(Color.FromRgb(248, 218, 137));
        Pen outline = new(brass, 1.2);
        Pen inner = new(new SolidColorBrush(Color.FromRgb(93, 116, 104)), 1);

        dc.DrawRoundedRectangle(panel, new Pen(new SolidColorBrush(Color.FromRgb(76, 99, 84)), 1), new Rect(1, 1, 30, 30), 7, 7);
        dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(13, 29, 29)), outline, new Point(16, 16), 10.5, 10.5);
        dc.DrawEllipse(null, inner, new Point(16, 16), 7.2, 7.2);

        for (int i = 0; i < 6; i++)
        {
            double angle = i * Math.PI / 3;
            Point start = new(16 + Math.Cos(angle) * 3.5, 16 + Math.Sin(angle) * 3.5);
            Point end = new(16 + Math.Cos(angle) * 7.1, 16 + Math.Sin(angle) * 7.1);
            dc.DrawLine(new Pen(brass, 1.2), start, end);
        }

        dc.DrawEllipse(brassLight, new Pen(new SolidColorBrush(Color.FromRgb(91, 66, 24)), .8), new Point(16, 16), 3.1, 3.1);
        dc.DrawLine(new Pen(brassLight, 1.4), new Point(16, 16), new Point(20.5, 12.5));
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(39, 54, 48)), outline, new Rect(5, 8, 3.5, 16), 1.2, 1.2);
    }
}
