namespace BlackjackCasino.HostShared;

/// <summary>
/// Optional locally bundled, user-selected music. No network or decoder package needed.
/// </summary>
internal static class LicensedSoundtracks
{
    internal static string? FileName(Soundtrack soundtrack) => soundtrack switch
    {
        Soundtrack.Manor => "midnight-manor.mp3",
        Soundtrack.ChillLounge => "chill-lounge.mp3",
        Soundtrack.Lobby => "lobby.mp3",
        Soundtrack.DiceLounge => "midnight-dice.mp3",
        _ => null,
    };

    internal static string? ExistingPath(Soundtrack soundtrack)
    {
        string? fileName = FileName(soundtrack);
        if (fileName is null) return null;
        string path = Path.Combine(AppContext.BaseDirectory, "Music", "Licensed", fileName);
        return File.Exists(path) ? path : null;
    }
}
