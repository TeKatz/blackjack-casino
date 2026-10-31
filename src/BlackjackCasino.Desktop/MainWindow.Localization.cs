using System.Windows;
using System.Windows.Controls;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private bool languageUiInitialized;
    private bool suppressLanguageSelection;
    private TextBlock? languageLabel;
    private ComboBox? languageComboBox;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(MainWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is MainWindow window) window.InitializeLocalizationUi();
            }));
    }

    private void InitializeLocalizationUi()
    {
        if (languageUiInitialized) return;
        languageUiInitialized = true;

        Localization.SetLanguage(preferences.Language, refreshOpenWindows: false);

        if (SoundCheckBox.Parent is Panel presentationPanel)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 9) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(156) });

            languageLabel = new TextBlock
            {
                Text = Localization.T("Settings.Language"),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush("#E1ECE4"),
                FontWeight = FontWeights.SemiBold,
            };
            row.Children.Add(languageLabel);

            languageComboBox = new ComboBox
            {
                Name = "LanguageComboBox",
                SelectedIndex = preferences.Language == UiLanguagePreference.German ? 1 : 0,
            };
            languageComboBox.Items.Add(new ComboBoxItem { Content = "English", Tag = UiLanguagePreference.English });
            languageComboBox.Items.Add(new ComboBoxItem { Content = "Deutsch", Tag = UiLanguagePreference.German });
            languageComboBox.SelectionChanged += LanguageComboBox_SelectionChanged;
            Grid.SetColumn(languageComboBox, 1);
            row.Children.Add(languageComboBox);

            int soundIndex = presentationPanel.Children.IndexOf(SoundCheckBox);
            presentationPanel.Children.Insert(Math.Max(0, soundIndex), row);
        }

        Localization.LanguageChanged += Localization_LanguageChanged;
        Closed += (_, _) => Localization.LanguageChanged -= Localization_LanguageChanged;

        RefreshLocalizedUi();
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (suppressLanguageSelection || !languageUiInitialized || languageComboBox?.SelectedItem is not ComboBoxItem { Tag: UiLanguagePreference requested })
            return;

        if (preferences.Language == requested)
            return;

        preferences = preferences with { Language = requested };
        Localization.SetLanguage(requested);
        RefreshLocalizedUi();
        SavePreferences();
    }

    private void Localization_LanguageChanged(object? sender, EventArgs e) => RefreshLocalizedUi();

    private void RefreshLocalizedUi()
    {
        if (!languageUiInitialized) return;

        if (languageLabel is not null)
            languageLabel.Text = Localization.T("Settings.Language");

        if (languageComboBox is not null)
        {
            suppressLanguageSelection = true;
            languageComboBox.SelectedIndex = Localization.Language == UiLanguagePreference.German ? 1 : 0;
            suppressLanguageSelection = false;
        }

        RefreshGameUi();
        Localization.ApplyTree(this);
    }
}
