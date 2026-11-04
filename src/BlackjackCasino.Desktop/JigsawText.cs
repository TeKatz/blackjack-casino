namespace BlackjackCasino.Desktop;

internal static class JigsawText
{
    internal static bool German => Localization.Language == UiLanguagePreference.German;
    internal static string T(string english, string german) => German ? german : english;

    internal static string Tier(BlackjackCasino.Core.Jigsaw.JigsawAchievementTier tier) => tier switch
    {
        BlackjackCasino.Core.Jigsaw.JigsawAchievementTier.Bronze => "BRONZE",
        BlackjackCasino.Core.Jigsaw.JigsawAchievementTier.Silver => T("SILVER", "SILBER"),
        BlackjackCasino.Core.Jigsaw.JigsawAchievementTier.Gold => "GOLD",
        BlackjackCasino.Core.Jigsaw.JigsawAchievementTier.Platinum => T("PLATINUM", "PLATIN"),
        _ => tier.ToString().ToUpperInvariant(),
    };

    internal static string Motif(string id) => id switch
    {
        "enchanted-forest" => T("Enchanted Forest", "Verzauberter Wald"),
        "sakura-garden" => T("Sakura Garden", "Sakuragarten"),
        "ocean-depths" => T("Ocean Depths", "Meerestiefen"),
        "frost-kingdom" => T("Frost Kingdom", "Frostkönigreich"),
        "celestial-voyage" => T("Celestial Voyage", "Himmelsreise"),
        "pirate-cove" => T("Pirate Cove", "Piratenbucht"),
        "midnight-manor" => "Midnight Manor",
        "neon-city" => T("Neon City", "Neonstadt"),
        "eternal-casino" => T("Eternal Casino", "Ewiges Casino"),
        "clockwork" => T("Clockwork", "Uhrwerk"),
        "desert-gold" => T("Desert Gold", "Wüstengold"),
        "primordial-dragons" => T("Primordial Dragons", "Urdrachen"),
        _ => id,
    };

    internal static string Category(string category) => category switch
    {
        "Nature" => T("Nature", "Natur"),
        "Fantasy" => "Fantasy",
        "The Midnight Club" => "The Midnight Club",
        "Art & Design" => T("Art & Design", "Kunst & Design"),
        _ => category,
    };
}
