using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private readonly BackgroundMusic backgroundMusic = new();
    private DropShadowEffect? splitOpportunityGlow;

    private void PresentationLoaded(object sender, RoutedEventArgs e)
    {
        InitializeTableRulesFeature();
        InitializeProgressionFeature();
        ApplyCasinoDropdownStyles();
        RaiseBottomActionDeck();
        InitializeSplitOpportunityCue();
        InitializeTableWagerPresentation();
        InitializeLobby();
        var tableAtmosphere=new System.Windows.Threading.DispatcherTimer {Interval=TimeSpan.FromMilliseconds(80)};
        tableAtmosphere.Tick+=(_,_)=> {if(AnimationsEnabled && lobby?.Visibility!=Visibility.Visible && embeddedGameShell is null) TableArt.Phase+=.08;};
        tableAtmosphere.Start(); Closed+=(_,_)=>tableAtmosphere.Stop();

        suppressPreferenceEvents = true;
        MusicCheckBox.IsChecked = preferences.MusicEnabled;
        MusicVolumeSlider.Value = preferences.MusicVolume;
        MusicVolumeText.Text = $"{preferences.MusicVolume:P0}";
        suppressPreferenceEvents = false;

        backgroundMusic.Start(preferences.MusicEnabled, preferences.MusicVolume);
        BackgroundMusic.Prewarm();
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

    private void InitializeSplitOpportunityCue()
    {
        SplitButton.IsEnabledChanged += (_, _) => UpdateSplitOpportunityCue();
        UpdateSplitOpportunityCue();
    }

    private void UpdateSplitOpportunityCue()
    {
        if (!SplitButton.IsEnabled)
        {
            if (splitOpportunityGlow is not null)
            {
                splitOpportunityGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, null);
                splitOpportunityGlow.BeginAnimation(DropShadowEffect.OpacityProperty, null);
            }

            splitOpportunityGlow = null;
            SplitButton.ClearValue(BackgroundProperty);
            SplitButton.ClearValue(BorderBrushProperty);
            SplitButton.ClearValue(ForegroundProperty);
            SplitButton.ClearValue(EffectProperty);
            return;
        }

        SplitButton.Background = new SolidColorBrush(Color.FromRgb(24, 68, 45));
        SplitButton.BorderBrush = new SolidColorBrush(Color.FromRgb(213, 173, 86));
        SplitButton.Foreground = new SolidColorBrush(Color.FromRgb(255, 226, 158));

        splitOpportunityGlow = new DropShadowEffect
        {
            Color = Color.FromRgb(215, 169, 68),
            BlurRadius = 8,
            ShadowDepth = 0,
            Opacity = 0.22,
        };
        SplitButton.Effect = splitOpportunityGlow;

        if (!AnimationsEnabled)
        {
            return;
        }

        var blur = new DoubleAnimation
        {
            From = 7,
            To = 12,
            Duration = TimeSpan.FromMilliseconds(850),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };
        var opacity = new DoubleAnimation
        {
            From = 0.16,
            To = 0.34,
            Duration = TimeSpan.FromMilliseconds(850),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
        };

        splitOpportunityGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blur);
        splitOpportunityGlow.BeginAnimation(DropShadowEffect.OpacityProperty, opacity);
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
