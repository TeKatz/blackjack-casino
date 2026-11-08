using System.Windows;

namespace BlackjackCasino.Desktop;

public partial class MainWindow
{
    private bool languageUiInitialized;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(typeof(MainWindow), LoadedEvent,
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
        Localization.LanguageChanged += Localization_LanguageChanged;
        Closed += (_, _) => Localization.LanguageChanged -= Localization_LanguageChanged;
        RefreshLocalizedUi();
    }

    private void Localization_LanguageChanged(object? sender, EventArgs e) => RefreshLocalizedUi();

    private void RefreshLocalizedUi()
    {
        if (!languageUiInitialized) return;
        // The language switch is available in React; only native fallback
        // windows still need a localized WPF visual tree.
        Localization.ApplyTree(this);
        modernBlackjackView?.Refresh();
    }
}
