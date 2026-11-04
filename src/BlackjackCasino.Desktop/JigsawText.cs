namespace BlackjackCasino.Desktop;
internal static class JigsawText
{
    internal static bool German => Localization.Language == UiLanguagePreference.German;
    internal static string T(string english, string german) => German ? german : english;
}
