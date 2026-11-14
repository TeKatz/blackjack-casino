using System.Globalization;
using System.Resources;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// Resource-only Album translation provider; independent of WPF's dynamic
/// visual-tree localization engine.
/// </summary>
internal static class NativeLoreLocalization
{
    private static readonly ResourceManager Catalog = new(
        "BlackjackCasino.NativeShell.Resources.AlbumLoreStrings",
        typeof(NativeLoreLocalization).Assembly);
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    internal static string Translate(string key) =>
        Catalog.GetString(key, German)
        ?? Catalog.GetString(key, English)
        ?? key;
}
