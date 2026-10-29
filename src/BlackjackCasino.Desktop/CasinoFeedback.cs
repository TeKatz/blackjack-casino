namespace BlackjackCasino.Desktop;

internal static class CasinoFeedback
{
    private static readonly object Owner = new();
    private static readonly IReadOnlyDictionary<TableCue,AudioClip> Cues = Enum.GetValues<TableCue>().ToDictionary(c => c, CasinoSoundSynthesis.Build);
    public static void PlayChip(bool enabled) => Play(enabled, TableCue.Chip);
    public static void PlayCard(bool enabled) => Play(enabled, TableCue.Deal);
    public static void PlayOutcome(bool enabled, decimal net) => Play(enabled, net > 0 ? TableCue.Win : net < 0 ? TableCue.Loss : TableCue.Push);
    internal static void Menu(bool enabled) => Play(enabled, TableCue.Menu);
    internal static void Stop() => CasinoAudio.Mixer.Stop(Owner);
    private static void Play(bool enabled, TableCue cue) { if (enabled) CasinoAudio.Play(Cues[cue], Owner); }
}
