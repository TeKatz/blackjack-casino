using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly BackgroundMusic backgroundMusic = new();

    private void PresentationLoaded(object sender, RoutedEventArgs e)
    {
        InitializeTableRulesFeature();
        InitializeProgressionFeature();

        suppressPreferenceEvents = true;
        MusicCheckBox.IsChecked = preferences.MusicEnabled;
        MusicVolumeSlider.Value = preferences.MusicVolume;
        MusicVolumeText.Text = $"{preferences.MusicVolume:P0}";
        suppressPreferenceEvents = false;

        backgroundMusic.Start(preferences.MusicEnabled, preferences.MusicVolume);
    }

    private void PresentationClosed(object? sender, EventArgs e)
    {
        ShutdownProgressionFeature();
        backgroundMusic.Dispose();
    }

    private void MusicSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || suppressPreferenceEvents)
        {
            return;
        }

        bool enabled = MusicCheckBox.IsChecked == true;
        preferences = preferences with { MusicEnabled = enabled };
        backgroundMusic.SetEnabled(enabled);
        SavePreferences();
    }

    private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        double volume = Math.Clamp(e.NewValue, 0d, 0.32d);
        if (MusicVolumeText is not null)
        {
            MusicVolumeText.Text = $"{volume:P0}";
        }

        if (!IsLoaded || suppressPreferenceEvents)
        {
            return;
        }

        preferences = preferences with { MusicVolume = volume };
        backgroundMusic.SetVolume(volume);
        SavePreferences();
    }
}
