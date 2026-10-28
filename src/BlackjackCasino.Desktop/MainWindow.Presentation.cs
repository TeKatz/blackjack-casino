using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly BackgroundMusic backgroundMusic = new();

    private void PresentationLoaded(object sender, RoutedEventArgs e)
    {
        InitializeTableRulesFeature();
        InitializeProgressionFeature();
        ApplyCasinoDropdownStyles();
        RaiseBottomActionDeck();

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

    private void ApplyCasinoDropdownStyles()
    {
        if (Application.Current.TryFindResource(typeof(ComboBox)) is not Style comboStyle
            || Application.Current.TryFindResource(typeof(ComboBoxItem)) is not Style itemStyle)
        {
            return;
        }

        foreach (ComboBox combo in FindPresentationChildren<ComboBox>(this))
        {
            combo.Style = comboStyle;
            combo.ItemContainerStyle = itemStyle;
        }
    }

    private void RaiseBottomActionDeck()
    {
        StatusBorder.Margin = new Thickness(70, 8, 70, 24);
        InsurancePanel.Margin = new Thickness(0, 0, 0, 14);
        ActionPanel.Margin = new Thickness(16, 0, 16, 32);
    }

    private static IEnumerable<T> FindPresentationChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (T nested in FindPresentationChildren<T>(child))
            {
                yield return nested;
            }
        }
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
