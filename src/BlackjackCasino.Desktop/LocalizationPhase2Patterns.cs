using System.Globalization;
using System.Text.RegularExpressions;

namespace BlackjackCasino.Desktop;

/// <summary>
/// German translations for runtime-composed gameplay copy. Keep numeric values,
/// payouts, character names, saved puzzle words and machine IDs unchanged.
/// Explicit resource keys should still be preferred for newly authored UI.
/// </summary>
internal static partial class LocalizationPhase2Patterns
{
    internal static string Translate(string source, CultureInfo culture)
    {
        if (!string.Equals(culture.Name, "de-DE", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(source))
            return source;

        Match m;
        if (source.StartsWith("Turn ", StringComparison.Ordinal) && (m = Turn().Match(source)).Success)
            return $"Runde {m.Groups[1].Value} von 13";
        if (source.StartsWith("Roll ", StringComparison.Ordinal) && (m = Roll().Match(source)).Success)
            return $"Wurf {m.Groups[1].Value} / 3";
        if (source.StartsWith("Record ", StringComparison.Ordinal) && (m = RecordScore().Match(source)).Success)
            return $"{m.Groups[1].Value} Punkte eintragen";
        if (source.StartsWith("Entry ", StringComparison.Ordinal) && (m = DiceEntry().Match(source)).Success)
            return $"Eintritt {m.Groups[1].Value} · Guthaben {m.Groups[2].Value}";
        if (source.StartsWith("Upper: ", StringComparison.Ordinal) && (m = UpperScore().Match(source)).Success)
            return $"Oben: {m.Groups[1].Value} Punkte · Bonus: {m.Groups[2].Value} / 35 (bei 63)";
        if (source.StartsWith("Lower: ", StringComparison.Ordinal) && (m = LowerScore().Match(source)).Success)
            return $"Unten: {m.Groups[1].Value} Punkte · Bis zum oberen Bonus: {m.Groups[2].Value}";
        if (source.StartsWith("Extra five-of-a-kind: ", StringComparison.Ordinal) && (m = ExtraFive().Match(source)).Success)
            return $"Zusätzlicher Fünferpasch: +{m.Groups[1].Value} · Gesamt: {m.Groups[2].Value}";
        if (source.StartsWith("Game complete! ", StringComparison.Ordinal) && (m = DiceComplete().Match(source)).Success)
            return $"Partie beendet! {m.Groups[1].Value} Punkte. {m.Groups[2].Value}";
        if (source.StartsWith("Found ", StringComparison.Ordinal) && (m = FoundWord().Match(source)).Success)
            return $"{m.Groups[1].Value} gefunden. Gespeichert.";
        if (source.StartsWith("Hint: ", StringComparison.Ordinal) && (m = WordHint().Match(source)).Success)
            return $"Hinweis: {m.Groups[1].Value} beginnt beim leuchtenden Buchstaben.";
        if (source.Contains(" words found · ", StringComparison.Ordinal) && (m = WordsFound().Match(source)).Success)
            return $"{m.Groups[1].Value} / {m.Groups[2].Value} Wörter gefunden · {m.Groups[3].Value.Trim()} · {m.Groups[4].Value} Hinweise";
        if (source.Contains(" pairs · ", StringComparison.Ordinal) && (m = MemoryProgress().Match(source)).Success)
            return $"{m.Groups[1].Value} / {m.Groups[2].Value} Paare · {m.Groups[3].Value} Züge · {m.Groups[4].Value} Genauigkeit · {m.Groups[5].Value}";
        if (source.StartsWith("NINE LIVES · ", StringComparison.Ordinal) && (m = NineLivesSpins().Match(source)).Success)
            return $"NINE LIVES · {m.Groups[1].Value} FREISPIELE ÜBRIG";
        if (source.StartsWith("NINE LIVES AWAKENED", StringComparison.Ordinal) && (m = NineLivesAwakened().Match(source)).Success)
            return $"NINE LIVES ERWACHT · {m.Groups[1].Value} FREISPIELE";
        if (source.StartsWith("FULL GRID FRENZY · ", StringComparison.Ordinal) && (m = FullGrid().Match(source)).Success)
            return $"VOLLES-RASTER-FIEBER · {m.Groups[1].Value}× · GEWINN {m.Groups[2].Value}";
        if (source.StartsWith("WIN ", StringComparison.Ordinal) && (m = NineLivesWin().Match(source)).Success)
            return $"GEWINN {m.Groups[1].Value}  ·  {m.Groups[2].Value} LINIE{(m.Groups[2].Value == "1" ? "" : "N")}  ·  {m.Groups[3].Value}×";
        if (source.StartsWith("HAUNTED NIGHT · ", StringComparison.Ordinal) && (m = ManorSpins().Match(source)).Success)
            return $"SPUKNACHT · {m.Groups[1].Value} FREISPIELE";
        if (source.StartsWith("FREE SPINS LEFT: ", StringComparison.Ordinal) && (m = ManorRemaining().Match(source)).Success)
            return $"FREISPIELE ÜBRIG: {m.Groups[1].Value} · KEINE ERNEUTE FEATURE-AUSLÖSUNG";
        if (source.EndsWith(" FREE SPINS", StringComparison.Ordinal) && (m = NeonSpins().Match(source)).Success)
            return $"{m.Groups[1].Value} FREISPIELE";
        if (source.StartsWith("Active managers: ", StringComparison.Ordinal) && (m = Managers().Match(source)).Success)
            return $"Aktive Manager: {m.Groups[1].Value}/{m.Groups[2].Value}";
        if (source.StartsWith("Tier ", StringComparison.Ordinal) && (m = EmpireTier().Match(source)).Success)
            return $"Stufe {m.Groups[1].Value}/{m.Groups[2].Value} · +{m.Groups[3].Value} Produktion";
        if (source.StartsWith("Prestige ", StringComparison.Ordinal) && (m = EmpirePrestige().Match(source)).Success)
            return $"Prestige {m.Groups[1].Value} · {m.Groups[2].Value} Raumstufen · {m.Groups[3].Value} Credits · {m.Groups[4].Value} Projekte";

        return source;
    }

    [GeneratedRegex(@"^Turn (\d+) of 13$")] private static partial Regex Turn();
    [GeneratedRegex(@"^Roll (\d+) / 3$")] private static partial Regex Roll();
    [GeneratedRegex(@"^Record (\d+) points$")] private static partial Regex RecordScore();
    [GeneratedRegex(@"^Entry (.+?) · Bankroll (.+)$")] private static partial Regex DiceEntry();
    [GeneratedRegex(@"^Upper: (\d+) points · Bonus: (\d+) / 35 \(at 63\)$")] private static partial Regex UpperScore();
    [GeneratedRegex(@"^Lower: (\d+) points · To upper bonus: (\d+)$")] private static partial Regex LowerScore();
    [GeneratedRegex(@"^Extra five-of-a-kind: \+(\d+) · Total: (\d+)$")] private static partial Regex ExtraFive();
    [GeneratedRegex(@"^Game complete! (\d+) points\. (.*)$")] private static partial Regex DiceComplete();
    [GeneratedRegex(@"^Found (.+)\. Saved\.$")] private static partial Regex FoundWord();
    [GeneratedRegex(@"^Hint: (.+) begins at the glowing letter\.$")] private static partial Regex WordHint();
    [GeneratedRegex(@"^(\d+) / (\d+) words found · ([^·]+) · (\d+) hints?$")] private static partial Regex WordsFound();
    [GeneratedRegex(@"^(\d+) / (\d+) pairs · (\d+) moves · (.+) accuracy · (.+)$")] private static partial Regex MemoryProgress();
    [GeneratedRegex(@"^NINE LIVES · (\d+) SPINS LEFT$")] private static partial Regex NineLivesSpins();
    [GeneratedRegex(@"^NINE LIVES AWAKENED · (\d+) BONUS SPINS$")] private static partial Regex NineLivesAwakened();
    [GeneratedRegex(@"^FULL GRID FRENZY · (.+)× · WIN (.+)$")] private static partial Regex FullGrid();
    [GeneratedRegex(@"^WIN (.+?)  ·  (\d+) LINES?  ·  (.+)×$")] private static partial Regex NineLivesWin();
    [GeneratedRegex(@"^HAUNTED NIGHT · (\d+) FREE SPINS$")] private static partial Regex ManorSpins();
    [GeneratedRegex(@"^FREE SPINS LEFT: (\d+) · FEATURES DO NOT RETRIGGER$")] private static partial Regex ManorRemaining();
    [GeneratedRegex(@"^(\d+) FREE SPINS$")] private static partial Regex NeonSpins();
    [GeneratedRegex(@"^Active managers: (\d+)/(\d+)$")] private static partial Regex Managers();
    [GeneratedRegex(@"^Tier (\d+)/(\d+) · \+(.+) production$")] private static partial Regex EmpireTier();
    [GeneratedRegex(@"^Prestige (\d+) · (.+) room levels · (.+) cr · (\d+) projects$")] private static partial Regex EmpirePrestige();
}
