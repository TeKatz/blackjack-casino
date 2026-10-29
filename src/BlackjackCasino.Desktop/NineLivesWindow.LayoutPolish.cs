using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        // Let the authored rooftop/moon artwork participate in the composition again.
        // The previous 0.09 opacity made the generated art almost invisible behind the UI.
        BackgroundArt.Opacity = 0.22;

        // Raster symbol art is the preferred presentation. Uniform rendering protects the
        // square source composition; vector drawings remain a crash-safe fallback only.
        for (int i = 0; i < symbolImages.Length; i++)
        {
            symbolImages[i].Stretch = Stretch.Uniform;
            RenderOptions.SetBitmapScalingMode(symbolImages[i], BitmapScalingMode.HighQuality);
            cells[i].Background = FrozenBrush("#F10A0712");
        }

        // A real icon reads as a settings control instead of looking like another text action.
        SettingsButton.Content = "⚙";
        SettingsButton.FontFamily = new FontFamily("Segoe UI Symbol");
        SettingsButton.FontSize = 23;

        SoundToggle.IsChecked = soundsEnabled;
        AnimationsToggle.IsChecked = animationsEnabled;
        AutoButton.Content = "AUTO ×20";
        SpinButton.Content = machine.BonusActive ? "FREE SPIN" : "SPIN";
    }
}
