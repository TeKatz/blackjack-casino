using System.Globalization;
using System.Text.RegularExpressions;

namespace BlackjackCasino.Desktop;

/// <summary>
/// Transitional translations for runtime-composed English UI text. Static copy belongs in .resx;
/// these patterns keep values containing counts, money and progress localized until their owning
/// screens are converted to explicit Localization.F calls.
/// </summary>
internal static partial class LocalizationPatterns
{
    internal static string Translate(string source, CultureInfo culture)
    {
        if (!string.Equals(culture.Name, "de-DE", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(source))
            return source;

        Match match;

        if ((match = FullTableReadyRegex().Match(source)).Success)
            return $"Voller Tisch mit {match.Groups[1].Value} KI-Platz{(match.Groups[1].Value == "1" ? string.Empty : "en")} bereit. Setze deinen Einsatz, um zu beginnen.";
        if ((match = PlayingHandRegex().Match(source)).Success)
            return $"Hand {match.Groups[1].Value} von {match.Groups[2].Value} wird gespielt.";
        if ((match = YouWinRegex().Match(source)).Success)
            return $"Du gewinnst {match.Groups[1].Value}";
        if ((match = YouLoseRegex().Match(source)).Success)
            return $"Du verlierst {match.Groups[1].Value}";
        if ((match = ShoeRegex().Match(source)).Success)
            return $"#{match.Groups[1].Value} · {match.Groups[2].Value} Karten";
        if ((match = FullTableModeRegex().Match(source)).Success)
            return $"Voller Tisch · {match.Groups[1].Value} KI-Platz{(match.Groups[1].Value == "1" ? string.Empty : "e")}";
        if ((match = BetWaitingRegex().Match(source)).Success)
            return $"Einsatz {match.Groups[1].Value}\nWartet auf Karten";
        if ((match = ShowingRegex().Match(source)).Success)
            return $"Zeigt {match.Groups[1].Value}";
        if ((match = HandLabelRegex().Match(source)).Success)
            return $"HAND {match.Groups[1].Value}";
        if ((match = SoftRegex().Match(source)).Success)
            return $"Weiche {match.Groups[1].Value}";
        if ((match = BustRegex().Match(source)).Success)
            return $"{match.Groups[1].Value} · Überkauft";

        if ((match = LoungeStatusRegex().Match(source)).Success)
            return $"{match.Groups[1].Value} Siege · {match.Groups[2].Value} aktiv · {match.Groups[3].Value}/{match.Groups[4].Value} Erfolge";
        if ((match = SudokuProgressRegex().Match(source)).Success)
            return $"FREIES SPIEL {match.Groups[1].Value}/{match.Groups[2].Value} · DAILY {match.Groups[3].Value} abgeschlossen";
        if ((match = DailyDifficultyRegex().Match(source)).Success)
            return $"HEUTIGES DAILY · {TranslateDifficulty(match.Groups[1].Value)}";
        if ((match = InProgressRegex().Match(source)).Success)
            return $"IN ARBEIT · {match.Groups[1].Value}";
        if ((match = SudokuDigitRegex().Match(source)).Success)
            return $"Alle {match.Groups[1].Value}en sind korrekt gesetzt.";
        if ((match = ManyHintsRegex().Match(source)).Success)
            return $"{match.Groups[1].Value} Hinweise haben unterwegs geholfen.";

        if ((match = CareerRegex().Match(source)).Success)
            return $"KARRIERE   {match.Groups[1].Value} Siege / {match.Groups[2].Value} gestartet   ·   {match.Groups[3].Value}% Siegquote   ·   beste Serie {match.Groups[4].Value}   ·   Erfolge {match.Groups[5].Value}/{match.Groups[6].Value}";
        if ((match = SolitaireStateRegex().Match(source)).Success)
        {
            string state = match.Groups[1].Value == "SOLVED" ? "GELÖST" : "IN ARBEIT";
            return $"{state} · Partie #{match.Groups[2].Value} · {match.Groups[3].Value} Züge";
        }
        if ((match = DealInfoRegex().Match(source)).Success)
            return $"PARTIE #{match.Groups[1].Value}   ·   {match.Groups[2].Value} Züge   ·   {match.Groups[3].Value} vergangen";
        if ((match = SelectedRunRegex().Match(source)).Success)
        {
            string run = string.IsNullOrEmpty(match.Groups[2].Value) ? string.Empty : $" · {match.Groups[2].Value} KARTEN";
            return $"AUSGEWÄHLT  ·  {match.Groups[1].Value}{run}  ·  Ziel anklicken oder die Auswahl erneut anklicken, um abzubrechen";
        }
        if ((match = CardsRemainRegex().Match(source)).Success)
            return $"{(match.Groups[1].Value == "Deal row" ? "Reihe geben" : "Ziehen")} · {match.Groups[2].Value} Karten übrig";
        if ((match = FoundationRegex().Match(source)).Success)
            return $"{match.Groups[1].Value}-Fundament · Ass bis König";
        if ((match = SpiderSuitRegex().Match(source)).Success)
            return $"{match.Groups[1].Value} Farbe{(match.Groups[1].Value == "1" ? string.Empty : "n")}";
        if ((match = CompletedRunsRegex().Match(source)).Success)
            return $"VOLLSTÄNDIGE REIHEN  {match.Groups[1].Value}/8";

        if ((match = VaultProgressRegex().Match(source)).Success)
            return $"{match.Groups[1].Value} voll · {match.Groups[2].Value} Bonus bei vollem Vault";
        if ((match = VaultNextTierRegex().Match(source)).Success)
            return $"Nächste Stufe: {TranslateVaultTier(match.Groups[1].Value)} bei {match.Groups[2].Value} Gesamteinsatz.";
        if ((match = VaultUnlockedRegex().Match(source)).Success)
            return $"{TranslateVaultTier(match.Groups[1].Value)} ist freigeschaltet und gilt für den nächsten frischen Vault.";
        if ((match = VaultStatsRegex().Match(source)).Success)
            return $"Geöffnete Vaults {match.Groups[1].Value}  ·  Beiträge {match.Groups[2].Value}\nAusgezahlt {match.Groups[3].Value}  ·  Voll-Boni {match.Groups[4].Value}  ·  Rekord {match.Groups[5].Value}";
        if ((match = BreakOpenRegex().Match(source)).Success)
            return $"AUFBRECHEN · +{match.Groups[1].Value} BONUS";
        if ((match = AlmostFullRegex().Match(source)).Success)
            return $"FAST VOLL · NOCH {match.Groups[1].Value}";
        if ((match = VaultLockedRegex().Match(source)).Success)
            return $"VAULT GESPERRT · NOCH {match.Groups[1].Value}";
        if ((match = ReserveReleasedRegex().Match(source)).Success)
            return $"Reserve freigegeben {match.Groups[1].Value}";
        if ((match = FullVaultBonusRegex().Match(source)).Success)
            return $"BONUS FÜR VOLLEN VAULT +{match.Groups[1].Value} ({match.Groups[2].Value})";
        if ((match = VaultOpenErrorRegex().Match(source)).Success)
            return $"Vault konnte nicht geöffnet werden: {match.Groups[1].Value}";

        return source;
    }

    private static string TranslateDifficulty(string value) => value.ToUpperInvariant() switch
    {
        "RELAXED" => "ENTSPANNT",
        "EASY" => "LEICHT",
        "MEDIUM" => "MITTEL",
        "HARD" => "SCHWER",
        "EXPERT" => "EXPERTE",
        "MASTER" => "MEISTER",
        _ => value,
    };

    private static string TranslateVaultTier(string value) => value switch
    {
        "Bronze Vault" => "Bronze-Vault",
        "Silver Vault" => "Silber-Vault",
        "Gold Vault" => "Gold-Vault",
        "Platinum Vault" => "Platin-Vault",
        "Diamond Vault" => "Diamant-Vault",
        _ => value,
    };

    [GeneratedRegex(@"^Full table ready with (\d+) AI seats?\. Place a bet to begin\.$")]
    private static partial Regex FullTableReadyRegex();
    [GeneratedRegex(@"^Playing hand (\d+) of (\d+)\.$")]
    private static partial Regex PlayingHandRegex();
    [GeneratedRegex(@"^You win (.+)$")]
    private static partial Regex YouWinRegex();
    [GeneratedRegex(@"^You lose (.+)$")]
    private static partial Regex YouLoseRegex();
    [GeneratedRegex(@"^#(.+?) · (.+?) cards$")]
    private static partial Regex ShoeRegex();
    [GeneratedRegex(@"^Full table · (\d+) AI seats?$")]
    private static partial Regex FullTableModeRegex();
    [GeneratedRegex(@"^Bet (.+)\nWaiting for deal$")]
    private static partial Regex BetWaitingRegex();
    [GeneratedRegex(@"^Showing (\d+)$")]
    private static partial Regex ShowingRegex();
    [GeneratedRegex(@"^HAND (\d+)$")]
    private static partial Regex HandLabelRegex();
    [GeneratedRegex(@"^Soft (\d+)$")]
    private static partial Regex SoftRegex();
    [GeneratedRegex(@"^(\d+) · Bust$")]
    private static partial Regex BustRegex();

    [GeneratedRegex(@"^(.+) wins · (.+) active · (.+)/(.+) achievements$")]
    private static partial Regex LoungeStatusRegex();
    [GeneratedRegex(@"^FREE PLAY (.+)/(.+) · DAILY (.+) completed$")]
    private static partial Regex SudokuProgressRegex();
    [GeneratedRegex(@"^TODAY'S DAILY · (.+)$")]
    private static partial Regex DailyDifficultyRegex();
    [GeneratedRegex(@"^IN PROGRESS · (.+)$")]
    private static partial Regex InProgressRegex();
    [GeneratedRegex(@"^All (\d)s are correctly placed\.$")]
    private static partial Regex SudokuDigitRegex();
    [GeneratedRegex(@"^(\d+) hints helped along the way\.$")]
    private static partial Regex ManyHintsRegex();

    [GeneratedRegex(@"^CAREER\s+(.+) wins / (.+) started\s+·\s+(.+)% win rate\s+·\s+best streak (.+)\s+·\s+achievements (.+)/(.+)$")]
    private static partial Regex CareerRegex();
    [GeneratedRegex(@"^(SOLVED|IN PROGRESS) · deal #(.+) · (.+) moves$")]
    private static partial Regex SolitaireStateRegex();
    [GeneratedRegex(@"^DEAL #(.+)\s+·\s+(.+) moves\s+·\s+(.+) elapsed$")]
    private static partial Regex DealInfoRegex();
    [GeneratedRegex(@"^SELECTED\s+·\s+(.+?)(?:\s+·\s+(\d+) CARD RUN)?\s+·\s+click a destination or click the selection again to cancel$")]
    private static partial Regex SelectedRunRegex();
    [GeneratedRegex(@"^(Deal row|Draw) · (.+) cards remain$")]
    private static partial Regex CardsRemainRegex();
    [GeneratedRegex(@"^(.+) foundation · Ace to King$")]
    private static partial Regex FoundationRegex();
    [GeneratedRegex(@"^(\d+) Suit$")]
    private static partial Regex SpiderSuitRegex();
    [GeneratedRegex(@"^COMPLETED RUNS\s+(.+)/8$")]
    private static partial Regex CompletedRunsRegex();

    [GeneratedRegex(@"^(.+) full · (.+) full-vault bonus$")]
    private static partial Regex VaultProgressRegex();
    [GeneratedRegex(@"^Next tier: (.+ Vault) at (.+) lifetime wager\.$")]
    private static partial Regex VaultNextTierRegex();
    [GeneratedRegex(@"^(.+ Vault) is unlocked and will apply to the next fresh Vault\.$")]
    private static partial Regex VaultUnlockedRegex();
    [GeneratedRegex(@"^Vaults opened (.+) · Contributions (.+)\nPaid back (.+) · Full bonuses (.+) · Best (.+)$")]
    private static partial Regex VaultStatsRegex();
    [GeneratedRegex(@"^BREAK OPEN · \+(.+) BONUS$")]
    private static partial Regex BreakOpenRegex();
    [GeneratedRegex(@"^ALMOST FULL · (.+) TO GO$")]
    private static partial Regex AlmostFullRegex();
    [GeneratedRegex(@"^VAULT LOCKED · (.+) TO GO$")]
    private static partial Regex VaultLockedRegex();
    [GeneratedRegex(@"^Reserve released (.+)$")]
    private static partial Regex ReserveReleasedRegex();
    [GeneratedRegex(@"^FULL VAULT BONUS \+(.+) \((.+)\)$")]
    private static partial Regex FullVaultBonusRegex();
    [GeneratedRegex(@"^Vault could not be opened: (.+)$")]
    private static partial Regex VaultOpenErrorRegex();
}
