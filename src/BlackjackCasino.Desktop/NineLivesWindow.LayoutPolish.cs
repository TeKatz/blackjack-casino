using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal partial class NineLivesWindow
{
    private bool polishedLayoutApplied;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (polishedLayoutApplied)
        {
            return;
        }

        polishedLayoutApplied = true;

        // The authored reel coordinates end at roughly Y=808. Keep the visual stage behind the
        // complete third row rather than letting the old artwork frame stop early.
        var reelStage = new Border
        {
            Width = 830,
            Height = 628,
            CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromArgb(226, 10, 7, 18)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(91, 50, 112)),
            BorderThickness = new Thickness(2),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(reelStage, 353);
        Canvas.SetTop(reelStage, 183);
        ReelCanvas.Children.Insert(0, reelStage);

        // The previous status badge overlapped the lower symbols. It now sits in the narrow
        // breathing room between the reel stage and the permanent control deck.
        StatusBadge.Height = 38;
        Canvas.SetTop(StatusBadge, 812);

        AutoButton.Content = "AUTO ×20";
        SpinButton.Content = machine.BonusActive ? "FREE SPIN" : "SPIN";
    }
}
