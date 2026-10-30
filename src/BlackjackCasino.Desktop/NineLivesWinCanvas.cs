using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class NineLivesWinCanvas : Canvas
{
    // Payline effects are allowed to exist only inside the authored 3x3 reel field.
    // Keep this as a hard safety boundary: a malformed/animated line can never escape
    // across the surrounding UI again.
    private static readonly Rect ReelBounds = new(366d, 185d, 803d, 623d);

    public NineLivesWinCanvas()
    {
        Clip = new RectangleGeometry(ReelBounds);
        ClipToBounds = true;
        IsHitTestVisible = false;
    }
}
