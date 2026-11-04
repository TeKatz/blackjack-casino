using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

/// <summary>Language-specific Season Pass presentation with stable progression IDs.</summary>
internal static class SeasonTextLocalization
{
    private static string L(string key, string original)
    {
        if (Localization.Language != UiLanguagePreference.German) return original;
        string text = Localization.T(key);
        return text.StartsWith("⟦", StringComparison.Ordinal) ? original : text;
    }
    internal static string Name(SeasonDefinition season) => L($"Season.{season.Id}.Name", season.Name);
    internal static string Description(SeasonDefinition season) => L($"Season.{season.Id}.Description", season.Description);
    internal static string Ui(string key, string original) => L("Season.Ui." + key, original);
}
