using System.Media;

namespace BlackjackCasino.Desktop;

internal static class CasinoFeedback
{
    public static void PlayChip(bool enabled) => Play(enabled, SystemSounds.Beep);

    public static void PlayCard(bool enabled) => Play(enabled, SystemSounds.Asterisk);

    public static void PlayOutcome(bool enabled, decimal net)
    {
        SystemSound sound = net switch
        {
            > 0m => SystemSounds.Exclamation,
            < 0m => SystemSounds.Hand,
            _ => SystemSounds.Question,
        };
        Play(enabled, sound);
    }

    private static void Play(bool enabled, SystemSound sound)
    {
        if (!enabled)
        {
            return;
        }

        try
        {
            sound.Play();
        }
        catch
        {
            // Sound feedback is cosmetic and must never interrupt play.
        }
    }
}
