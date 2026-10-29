using System.Windows;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

/// <summary>Textured lobby cover; individual rooms use the same cached art collection.</summary>
internal sealed class EmpireFloorArtwork : FrameworkElement
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        Rect bounds = new(0, 0, ActualWidth, ActualHeight);
        dc.DrawRectangle(new ImageBrush(EmpireRoomTextures.Get(5)) { Stretch = Stretch.UniformToFill }, null, bounds);
        dc.DrawRectangle(new LinearGradientBrush(Color.FromArgb(0, 6, 20, 16), Color.FromArgb(190, 6, 20, 16), 90), null, bounds);
    }
}
