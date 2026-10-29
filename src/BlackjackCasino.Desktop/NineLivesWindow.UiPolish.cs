using System.Windows;
using System.Windows.Controls.Primitives;

namespace BlackjackCasino.Desktop;

internal partial class NineLivesWindow
{
    private void SettingsFlyoutButton_Click(object sender, RoutedEventArgs e)
    {
        PaytableFlyout.Visibility = Visibility.Collapsed;
        SoundToggle.IsChecked = soundsEnabled;
        AnimationsToggle.IsChecked = animationsEnabled;
        SettingsFlyout.Visibility = SettingsFlyout.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void CloseSettingsFlyout_Click(object sender, RoutedEventArgs e)
        => SettingsFlyout.Visibility = Visibility.Collapsed;

    private void SoundToggle_Click(object sender, RoutedEventArgs e)
    {
        soundsEnabled = SoundToggle.IsChecked == true;
        StatusText.Text = soundsEnabled ? "Sound enabled." : "Sound muted.";
    }

    private void AnimationsToggle_Click(object sender, RoutedEventArgs e)
    {
        animationsEnabled = AnimationsToggle.IsChecked == true;
        StatusText.Text = animationsEnabled ? "Animations enabled." : "Animations disabled.";
    }

    private void PaytableButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsFlyout.Visibility = Visibility.Collapsed;
        PaytableFlyout.Visibility = Visibility.Visible;
    }

    private void ClosePaytable_Click(object sender, RoutedEventArgs e)
        => PaytableFlyout.Visibility = Visibility.Collapsed;
}
