using System.Windows;
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

        // Symbols are authored as square vector illustrations. Uniform rendering keeps the
        // complete artwork visible instead of cropping faces/ears to the old wide JPEG frame.
        for (int i = 0; i < symbolImages.Length; i++)
        {
            symbolImages[i].Stretch = Stretch.Uniform;
            RenderOptions.SetBitmapScalingMode(symbolImages[i], BitmapScalingMode.HighQuality);
            cells[i].Background = FrozenBrush("#F10A0712");
        }

        SoundToggle.IsChecked = soundsEnabled;
        AnimationsToggle.IsChecked = animationsEnabled;
        AutoButton.Content = "AUTO ×20";
        SpinButton.Content = machine.BonusActive ? "FREE SPIN" : "SPIN";
    }
}
