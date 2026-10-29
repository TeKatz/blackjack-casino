using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BlackjackCasino.Desktop;

internal sealed class NineLivesWinCanvas : Canvas
{
    // Authored outer bounds of the 3x3 reel cells. Paylines are drawn by game logic
    // center-to-center and this presentation layer extends them exactly to the grid edges.
    private const double ReelLeft = 366d;
    private const double ReelRight = 1169d;

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
        if (length < 1 || Math.Abs(dx) < 0.001)
        {
            return;
        }

        double ux = dx / length;
        double uy = dy / length;

        // Every valid Nine Lives payline runs left-to-right. Use its actual direction to
        // intersect the first/last cell edges. Unlike the old fixed 118 px extension this
        // also reaches the correct corners for both diagonal lines.
        double startDistance = Math.Abs((line.X1 - ReelLeft) / ux);
        double endDistance = Math.Abs((ReelRight - line.X2) / ux);
        line.X1 -= ux * startDistance;
        line.Y1 -= uy * startDistance;
        line.X2 += ux * endDistance;
        line.Y2 += uy * endDistance;

        line.StrokeThickness = line.StrokeThickness >= 10 ? 18 : 6;
        line.StrokeStartLineCap = PenLineCap.Round;
        line.StrokeEndLineCap = PenLineCap.Round;
        line.SnapsToDevicePixels = false;
    }
}
