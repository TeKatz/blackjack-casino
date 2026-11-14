using System.Globalization;
using System.Resources;
using BlackjackCasino.HostShared;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Uses the exact English and German game text resources supplied with the
/// regular desktop build without referencing its WPF Localization singleton.
/// </summary>
internal static class NativeMergeText
{
    private static readonly ResourceManager Resources = new(
        "BlackjackCasino.NativeShell.Resources.MergeStrings", typeof(NativeMergeText).Assembly);

    internal static string Translate(string key, UiLanguagePreference language)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        CultureInfo culture = CultureInfo.GetCultureInfo(
            language == UiLanguagePreference.German ? "de" : "en");
        return Resources.GetString(key, culture) ?? key;
    }
}
