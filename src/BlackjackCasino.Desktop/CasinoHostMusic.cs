namespace BlackjackCasino.Desktop;

/// <summary>Host music handoff: the lobby has its own music, without overlap with game music.</summary>
internal sealed class CasinoHostMusic : IDisposable
{
    private BackgroundMusic backgroundMusic = new();
    private Soundtrack activeTrack = Soundtrack.Blackjack;
    private bool started;

    internal void Start(AreaPresentationSettings settings, bool lobbyVisible = false)
    {
        if (started) { Sync(settings, true, lobbyVisible); return; }
        activeTrack = lobbyVisible ? Soundtrack.Lobby : Soundtrack.Blackjack;
        backgroundMusic = new BackgroundMusic(activeTrack);
        backgroundMusic.Start(settings.MusicEnabled, settings.MusicVolume);
        started = true;
        BackgroundMusic.Prewarm();
        SlotAudio.Prewarm();
    }

    internal void Sync(AreaPresentationSettings settings, bool areaAllowsHostMusic, bool lobbyVisible = false)
    {
        if (!started) return;
        Soundtrack desired = lobbyVisible ? Soundtrack.Lobby : Soundtrack.Blackjack;
        if (areaAllowsHostMusic && activeTrack != desired)
        {
            backgroundMusic.Dispose();
            activeTrack = desired;
            backgroundMusic = new BackgroundMusic(desired);
            backgroundMusic.Start(settings.MusicEnabled, settings.MusicVolume);
        }
        else
        {
            backgroundMusic.SetEnabled(settings.MusicEnabled && areaAllowsHostMusic);
            backgroundMusic.SetVolume(settings.MusicVolume);
        }
    }

    internal void Mute() => backgroundMusic.SetEnabled(false);

    internal void Resume(AreaPresentationSettings settings, bool updateVolume = false)
    {
        backgroundMusic.SetEnabled(settings.MusicEnabled);
        if (updateVolume) backgroundMusic.SetVolume(settings.MusicVolume);
    }

    public void Dispose() => backgroundMusic.Dispose();
}
