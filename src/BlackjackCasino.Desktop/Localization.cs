using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Central UI localization service. English is the neutral fallback resource and German is the
/// first translated satellite resource. Existing hard-coded controls are migrated safely at
/// runtime while new UI can use T/F directly.
/// </summary>
internal static class Localization
{
    private static readonly ResourceManager[] ResourceManagers =
    [
        new("BlackjackCasino.Desktop.Resources.Strings", typeof(Localization).Assembly),
        new("BlackjackCasino.Desktop.Resources.LobbyStrings", typeof(Localization).Assembly),
        new("BlackjackCasino.Desktop.Resources.VaultShellStrings", typeof(Localization).Assembly),
        new("BlackjackCasino.Desktop.Resources.GameStrings", typeof(Localization).Assembly),
    ];

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly IReadOnlyDictionary<string, string> EnglishValueToKey = BuildEnglishValueIndex();
    private static UiLanguagePreference language = UiLanguagePreference.English;
    private static bool classHandlerInstalled;

    private static readonly DependencyProperty LocalizationKeyProperty = Attached("LocalizationKey", typeof(string));
    private static readonly DependencyProperty SourceTextProperty = Attached("SourceText", typeof(string));
    private static readonly DependencyProperty LastRenderedTextProperty = Attached("LastRenderedText", typeof(string));
    private static readonly DependencyProperty ToolTipKeyProperty = Attached("ToolTipKey", typeof(string));
    private static readonly DependencyProperty ToolTipSourceProperty = Attached("ToolTipSource", typeof(string));
    private static readonly DependencyProperty LastRenderedToolTipProperty = Attached("LastRenderedToolTip", typeof(string));
    private static readonly DependencyProperty WatchInstalledProperty = Attached("WatchInstalled", typeof(bool), false);
    private static readonly DependencyProperty ApplyingProperty = Attached("Applying", typeof(bool), false);
    private static readonly DependencyProperty SubtreeRefreshQueuedProperty = Attached("SubtreeRefreshQueued", typeof(bool), false);

    internal static event EventHandler? LanguageChanged;

    internal static UiLanguagePreference Language => language;
    internal static CultureInfo Culture => language == UiLanguagePreference.German ? GermanCulture : EnglishCulture;

    internal static void InstallAutomaticTranslation()
    {
        if (classHandlerInstalled) return;
        classHandlerInstalled = true;
        EventManager.RegisterClassHandler(
            typeof(FrameworkElement),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) =>
            {
                if (sender is not FrameworkElement element) return;
                InstallChangeWatch(element);
                TranslateElement(element);
                if (element is Window or UserControl) QueueSubtreeRefresh(element);
            }));
    }

    internal static void SetLanguage(UiLanguagePreference requested, bool refreshOpenWindows = true)
    {
        language = requested is UiLanguagePreference.German ? UiLanguagePreference.German : UiLanguagePreference.English;
        CultureInfo culture = Culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (refreshOpenWindows && Application.Current is not null)
        {
            foreach (Window window in Application.Current.Windows) ApplyTree(window);
        }

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    internal static string T(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        foreach (ResourceManager manager in ResourceManagers)
        {
            string? localized = manager.GetString(key, Culture);
            if (localized is not null) return localized;
        }
        foreach (ResourceManager manager in ResourceManagers)
        {
            string? fallback = manager.GetString(key, EnglishCulture);
            if (fallback is not null) return fallback;
        }
        return $"⟦{key}⟧";
    }

    internal static string F(string key, params object?[] args) => string.Format(Culture, T(key), args);

    internal static string TranslateSource(string englishSource)
    {
        string? key = KeyForEnglishSource(englishSource);
        return key is not null ? T(key) : TranslatePattern(englishSource);
    }

    internal static void ApplyTree(DependencyObject root)
    {
        if (root is FrameworkElement element)
        {
            InstallChangeWatch(element);
            TranslateElement(element);
        }

        int visualChildren = root is Visual or Visual3D ? VisualTreeHelper.GetChildrenCount(root) : 0;
        for (int i = 0; i < visualChildren; i++) ApplyTree(VisualTreeHelper.GetChild(root, i));

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependency && dependency is not Visual && dependency is not Visual3D)
                ApplyTree(dependency);
        }
    }

    internal static string? KeyForEnglishSource(string source) =>
        EnglishValueToKey.TryGetValue(source, out string? key) ? key : null;

    private static string TranslatePattern(string source)
    {
        string translated = LocalizationPatterns.Translate(source, Culture);
        return string.Equals(translated, source, StringComparison.Ordinal)
            ? LocalizationSupplementalPatterns.Translate(source, Culture)
            : translated;
    }

    private static void QueueSubtreeRefresh(FrameworkElement element)
    {
        if ((bool)element.GetValue(SubtreeRefreshQueuedProperty)) return;
        element.SetValue(SubtreeRefreshQueuedProperty, true);
        _ = element.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            element.SetValue(SubtreeRefreshQueuedProperty, false);
            if (element.IsLoaded) ApplyTree(element);
        }));
    }

    private static void InstallChangeWatch(FrameworkElement element)
    {
        if ((bool)element.GetValue(WatchInstalledProperty)) return;
        element.SetValue(WatchInstalledProperty, true);

        DependencyPropertyDescriptor? descriptor = element switch
        {
            Window => DependencyPropertyDescriptor.FromProperty(Window.TitleProperty, typeof(Window)),
            TextBlock => DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock)),
            HeaderedContentControl => DependencyPropertyDescriptor.FromProperty(HeaderedContentControl.HeaderProperty, typeof(HeaderedContentControl)),
            ContentControl => DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl)),
            _ => null,
        };
        descriptor?.AddValueChanged(element, (_, _) => TranslateElement(element));
    }

    private static void TranslateElement(FrameworkElement element)
    {
        if ((bool)element.GetValue(ApplyingProperty)) return;
        element.SetValue(ApplyingProperty, true);
        try
        {
            switch (element)
            {
                case Window window when !string.IsNullOrWhiteSpace(window.Title):
                    window.Title = TranslateRemembered(element, window.Title);
                    break;
                case TextBlock text:
                    text.Text = TranslateRemembered(element, text.Text);
                    break;
                case HeaderedContentControl headered when headered.Header is string header:
                    headered.Header = TranslateRemembered(element, header);
                    break;
                case ContentControl content when content.Content is string source:
                    content.Content = TranslateRemembered(element, source);
                    break;
            }

            if (element.ToolTip is string tooltip)
                element.ToolTip = TranslateRememberedToolTip(element, tooltip);
        }
        finally
        {
            element.SetValue(ApplyingProperty, false);
        }
    }

    private static string TranslateRemembered(DependencyObject owner, string current)
    {
        string? source = owner.GetValue(SourceTextProperty) as string;
        string? lastRendered = owner.GetValue(LastRenderedTextProperty) as string;
        string? key = owner.GetValue(LocalizationKeyProperty) as string;

        if (source is null || !string.Equals(current, lastRendered, StringComparison.Ordinal))
        {
            source = current;
            key = KeyForEnglishSource(source);
            owner.SetValue(SourceTextProperty, source);
            owner.SetValue(LocalizationKeyProperty, key);
        }

        string rendered = key is not null ? T(key) : TranslatePattern(source);
        owner.SetValue(LastRenderedTextProperty, rendered);
        return rendered;
    }

    private static string TranslateRememberedToolTip(DependencyObject owner, string current)
    {
        string? source = owner.GetValue(ToolTipSourceProperty) as string;
        string? lastRendered = owner.GetValue(LastRenderedToolTipProperty) as string;
        string? key = owner.GetValue(ToolTipKeyProperty) as string;
        if (source is null || !string.Equals(current, lastRendered, StringComparison.Ordinal))
        {
            source = current;
            key = KeyForEnglishSource(source);
            owner.SetValue(ToolTipSourceProperty, source);
            owner.SetValue(ToolTipKeyProperty, key);
        }

        string rendered = key is not null ? T(key) : TranslatePattern(source);
        owner.SetValue(LastRenderedToolTipProperty, rendered);
        return rendered;
    }

    private static IReadOnlyDictionary<string, string> BuildEnglishValueIndex()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);

        foreach (ResourceManager manager in ResourceManagers)
        {
            ResourceSet? set = manager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            if (set is null) continue;

            foreach (DictionaryEntry entry in set)
            {
                if (entry.Key is not string key || entry.Value is not string value || string.IsNullOrWhiteSpace(value)) continue;
                if (!keys.Add(key))
                    throw new InvalidOperationException($"Duplicate localization key across resource catalogs: {key}");
                result.TryAdd(value, key);
            }
        }
        return result;
    }

    private static DependencyProperty Attached(string name, Type type, object? defaultValue = null) =>
        DependencyProperty.RegisterAttached(name, type, typeof(Localization), new PropertyMetadata(defaultValue));
}
