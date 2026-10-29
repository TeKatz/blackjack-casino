using System.Collections.Concurrent;

namespace BlackjackCasino.Desktop;

internal sealed class BackgroundMusic(Soundtrack track = Soundtrack.Blackjack) : IDisposable
{
    private static readonly ConcurrentDictionary<Soundtrack,Lazy<Task<AudioClip>>> Tracks = new();
    private readonly object owner = new();
    private bool enabled, disposed;
    private double volume;
    private AudioClip? clip;
    internal Task Ready { get; private set; } = Task.CompletedTask;
    private static Task<AudioClip> Load(Soundtrack track) => Tracks.GetOrAdd(track,
        key => new Lazy<Task<AudioClip>>(() => Task.Run(() => CasinoMusicSynthesis.Build(key)))).Value;
    internal static void Prewarm() { foreach (Soundtrack item in Enum.GetValues<Soundtrack>()) _ = Load(item); }
    public void Start(bool shouldPlay, double gain)
    {
        enabled = shouldPlay; volume = Math.Clamp(gain, 0, 0.32);
        Ready = Open();
    }
    private async Task Open()
    {
        AudioClip loaded = await Load(track);
        if (disposed) return;
        clip = loaded; CasinoAudio.Start();
        CasinoAudio.Mixer.Loop(loaded, owner, enabled ? volume * 0.45 : 0);
    }
    public void SetEnabled(bool value)
    {
        enabled = value;
        if (clip is null) { if (Ready.IsCompleted) Ready = Open(); }
        else CasinoAudio.Mixer.Volume(owner, enabled ? volume * 0.45 : 0);
    }
    public void SetVolume(double value)
    {
        volume = Math.Clamp(value, 0, 0.32);
        if (clip is not null) CasinoAudio.Mixer.Volume(owner, enabled ? volume * 0.45 : 0);
    }
    public void Dispose() { disposed = true; CasinoAudio.Mixer.Stop(owner); }
}
