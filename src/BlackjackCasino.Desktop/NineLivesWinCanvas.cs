using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BlackjackCasino.Desktop;

internal sealed class NineLivesWinCanvas : Canvas
{
    protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
    {
        base.OnVisualChildrenChanged(visualAdded, visualRemoved);

        if (visualAdded is not Line line)
        {
            return;
        }

        double dx = line.X2 - line.X1;
        double dy = line.Y2 - line.Y1;
        double length = Math.Sqrt((dx * dx) + (dy * dy));
        if (length < 1)
        {
            return;
        }

        // Existing game logic draws center-to-center. Extend the visual sweep almost to the
        // outside edges of the first/last winning cells so the line reads as a complete payline.
        const double extension = 118d;
        double ux = dx / length;
        double uy = dy / length;
        line.X1 -= ux * extension;
        line.Y1 -= uy * extension;
        line.X2 += ux * extension;
        line.Y2 += uy * extension;

        line.StrokeThickness = line.StrokeThickness >= 10 ? 18 : 6;
        line.StrokeStartLineCap = PenLineCap.Round;
        line.StrokeEndLineCap = PenLineCap.Round;
        line.SnapsToDevicePixels = false;
    }
}
