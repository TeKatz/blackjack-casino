namespace BlackjackCasino.Desktop;

/// <summary>
/// Host soundtrack lifecycle and exclusive game-audio handoff, independent of WPF.
/// The caller selects the active area; this router owns the player and its cleanup.
/// </summary>
internal sealed class CasinoHostMusic : IDisposable
{
    private readonly BackgroundMusic backgroundMusic = new();

    internal void Start(AreaPresentationSettings settings)
    {
        backgroundMusic.Start(settings.MusicEnabled, settings.MusicVolume);
        BackgroundMusic.Prewarm();
        SlotAudio.Prewarm();
    }

    internal void Sync(AreaPresentationSettings settings, bool areaAllowsHostMusic)
    {
        backgroundMusic.SetEnabled(settings.MusicEnabled && areaAllowsHostMusic);
        backgroundMusic.SetVolume(settings.MusicVolume);
    }

    internal void Mute() => backgroundMusic.SetEnabled(false);

    public void Dispose() => backgroundMusic.Dispose();
}
