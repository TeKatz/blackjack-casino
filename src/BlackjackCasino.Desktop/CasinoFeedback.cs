using System.Media;

namespace BlackjackCasino.Desktop;

internal static class CasinoFeedback
{
    private static readonly Lazy<SoundPlayer> CardPlayer = CreatePlayer("card.wav");
    private static readonly Lazy<SoundPlayer> ChipPlayer = CreatePlayer("chip.wav");
    private static readonly Lazy<SoundPlayer> WinPlayer = CreatePlayer("win.wav");
    private static readonly Lazy<SoundPlayer> LossPlayer = CreatePlayer("loss.wav");
    private static readonly Lazy<SoundPlayer> PushPlayer = CreatePlayer("push.wav");

    public static void PlayChip(bool enabled) => Play(enabled, ChipPlayer);

    public static void PlayCard(bool enabled) => Play(enabled, CardPlayer);

    public static void PlayOutcome(bool enabled, decimal net)
    {
        Play(enabled, net switch
        {
            > 0m => WinPlayer,
            < 0m => LossPlayer,
            _ => PushPlayer,
        });
    }

    private static Lazy<SoundPlayer> CreatePlayer(string fileName) => new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "assets", "audio", fileName);
        var player = new SoundPlayer(path);
        player.Load();
        return player;
    });

    private static void Play(bool enabled, Lazy<SoundPlayer> player)
    {
        if (!enabled)
        {
            return;
        }

        try
        {
            player.Value.Play();
        }
        catch
        {
            // Audio feedback is cosmetic and must never interrupt play.
        }
    }
}
