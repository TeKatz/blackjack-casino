using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Central UI localization service. English is the neutral fallback resource; German is the
/// first translated satellite resource. Resource catalogs are split by surface so future game
/// translations can grow without turning one resource file into a monolith.
/// Existing hard-coded English controls are migrated at runtime by matching their neutral
/// resource value once and remembering the resource key. New UI should call T/F directly.
/// </summary>
internal static class Localization
{
    private static readonly ResourceManager[] ResourceManagers =
    [
        new("BlackjackCasino.Desktop.Resources.Strings", typeof(Localization).Assembly),
        new("BlackjackCasino.Desktop.Resources.LobbyStrings", typeof(Localization).Assembly),
    ];

    private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo[] SupportedCultures = [EnglishCulture, GermanCulture];
    private static readonly IReadOnlyDictionary<string, string> EnglishValueToKey = BuildEnglishValueIndex();
    private static UiLanguagePreference language = UiLanguagePreference.English;
    private static bool classHandlerInstalled;

    private static readonly DependencyProperty LocalizationKeyProperty = DependencyProperty.RegisterAttached(
        "LocalizationKey", typeof(string), typeof(Localization), new PropertyMetadata(null));
    private static readonly DependencyProperty WatchInstalledProperty = DependencyProperty.RegisterAttached(
        "WatchInstalled", typeof(bool), typeof(Localization), new PropertyMetadata(false));
    private static readonly DependencyProperty ApplyingProperty = DependencyProperty.RegisterAttached(
        "Applying", typeof(bool), typeof(Localization), new PropertyMetadata(false));

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
                if (sender is FrameworkElement element)
                {
                    InstallChangeWatch(element);
                    TranslateElement(element);
                }
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

    private static void InstallChangeWatch(FrameworkElement element)
    {
        if ((bool)element.GetValue(WatchInstalledProperty)) return;
        element.SetValue(WatchInstalledProperty, true);

        DependencyPropertyDescriptor? descriptor = element switch
        {
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
            {
                string? key = KeyForEnglishSource(tooltip);
                if (key is not null) element.ToolTip = T(key);
            }
        }
        finally
        {
            element.SetValue(ApplyingProperty, false);
        }
    }

    private static string TranslateRemembered(DependencyObject owner, string source)
    {
        string? key = (string?)owner.GetValue(LocalizationKeyProperty);
        if (key is not null && !IsValueForKey(key, source))
        {
            key = KeyForEnglishSource(source);
            owner.SetValue(LocalizationKeyProperty, key);
        }

        if (key is null)
        {
            key = KeyForEnglishSource(source);
            if (key is null) return source;
            owner.SetValue(LocalizationKeyProperty, key);
        }
        return T(key);
    }

    private static bool IsValueForKey(string key, string value)
    {
        foreach (ResourceManager manager in ResourceManagers)
        {
            foreach (CultureInfo culture in SupportedCultures)
            {
                if (string.Equals(manager.GetString(key, culture), value, StringComparison.Ordinal)) return true;
            }
        }
        return false;
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
}
