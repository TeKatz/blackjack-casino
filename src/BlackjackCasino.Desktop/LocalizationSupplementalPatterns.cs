using System.Globalization;
using System.Text.RegularExpressions;

namespace BlackjackCasino.Desktop;

internal static partial class LocalizationSupplementalPatterns
{
    internal static string Translate(string source, CultureInfo culture)
    {
        if (!string.Equals(culture.Name, "de-DE", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(source))
            return source;

        string? exact = source switch
        {
            "Table preset" => "Tischvorgabe",
            "Relaxed default table: six decks, S17, 3:2 blackjack and forgiving actions." => "Entspannter Standardtisch: sechs Decks, S17, Blackjack 3:2 und großzügige Aktionsregeln.",
            "Advanced table setup..." => "Erweiterte Tischeinstellungen...",
            "Casual Solo stays the default: no setup is required to just sit down and play." => "Casual Solo bleibt der Standard: Du musst nichts einrichten, um dich einfach hinzusetzen und zu spielen.",
            _ => null,
        };
        if (exact is not null) return exact;

        Match match;
        if ((match = PlayerHandBetRegex().Match(source)).Success)
        {
            string label = match.Groups[1].Value == "YOUR HAND"
                ? "DEINE HAND"
                : $"HAND {match.Groups[2].Value}";
            return $"{label}  ·  {match.Groups[3].Value}";
        }
        if ((match = SettledRegex().Match(source)).Success)
            return $"ABGERECHNET {match.Groups[1].Value}";
        if ((match = VaultHeaderTierRegex().Match(source)).Success)
            return $"VAULT · {TranslateTierWord(match.Groups[1].Value)}";
        if ((match = VaultToolTipRegex().Match(source)).Success)
            return $"Casino Vault · {match.Groups[1].Value} voll · {match.Groups[2].Value} der berechtigten Einsätze · {match.Groups[3].Value} Bonus bei 100 %";
        if ((match = RewardXpRegex().Match(source)).Success)
            return $"+{match.Groups[1].Value} XP · +{match.Groups[2].Value} Season-XP verdient!";

        return source;
    }

    private static string TranslateTierWord(string tier) => tier switch
    {
        "BRONZE" => "BRONZE",
        "SILVER" => "SILBER",
        "GOLD" => "GOLD",
        "PLATINUM" => "PLATIN",
        "DIAMOND" => "DIAMANT",
        _ => tier,
    };

    [GeneratedRegex(@"^(YOUR HAND|HAND (\d+))\s+·\s+(.+)$")]
    private static partial Regex PlayerHandBetRegex();
    [GeneratedRegex(@"^SETTLED\s+(.+)$")]
    private static partial Regex SettledRegex();
    [GeneratedRegex(@"^VAULT · (BRONZE|SILVER|GOLD|PLATINUM|DIAMOND)$")]
    private static partial Regex VaultHeaderTierRegex();
    [GeneratedRegex(@"^Casino Vault · (.+) full · (.+) of eligible wagers · (.+) bonus at 100%$")]
    private static partial Regex VaultToolTipRegex();
    [GeneratedRegex(@"^\+(.+) XP · \+(.+) Season XP earned!$")]
    private static partial Regex RewardXpRegex();
}
