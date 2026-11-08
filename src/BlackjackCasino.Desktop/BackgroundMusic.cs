using System.Collections.Concurrent;
using System.Windows.Media;
using System.Windows.Threading;

namespace BlackjackCasino.Desktop;

internal sealed class BackgroundMusic(Soundtrack track = Soundtrack.Blackjack) : IDisposable
{
    private static readonly ConcurrentDictionary<Soundtrack, Lazy<Task<AudioClip>>> Tracks = new();
    private readonly object owner = new();
    private bool enabled, disposed;
    private double volume;
    private AudioClip? clip;
    private MediaPlayer? licensedPlayer;
    private DispatcherTimer? fadeTimer;
    internal Task Ready { get; private set; } = Task.CompletedTask;

    private static Task<AudioClip> Load(Soundtrack soundtrack) => Tracks.GetOrAdd(soundtrack,
        key => new Lazy<Task<AudioClip>>(() => Task.Run(() => CasinoMusicSynthesis.Build(key)))).Value;

    internal static void Prewarm()
    {
        foreach (Soundtrack item in Enum.GetValues<Soundtrack>())
            if (LicensedSoundtracks.ExistingPath(item) is null) _ = Load(item);
    }

    public void Start(bool shouldPlay, double gain)
    {
        enabled = shouldPlay;
        volume = Math.Clamp(gain, 0, 0.32);
        if (licensedPlayer is not null) { UpdateLicensedVolume(); return; }

        string? path = LicensedSoundtracks.ExistingPath(track);
        if (path is not null && TryPlayLicensed(path))
        {
            Ready = Task.CompletedTask;
            return;
        }
        Ready = Open();
    }

    private bool TryPlayLicensed(string path)
    {
        try
        {
            var player = new MediaPlayer { Volume = 0 };
            licensedPlayer = player;
            fadeTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(30),
            };
            fadeTimer.Tick += (_, _) => RampLicensedVolume();
            player.MediaEnded += (_, _) =>
            {
                if (disposed || !ReferenceEquals(licensedPlayer, player)) return;
                player.Position = TimeSpan.Zero;
                player.Play();
            };
            player.MediaFailed += (_, _) => OnLicensedFailure(player);
            player.Open(new Uri(path, UriKind.Absolute));
            player.Play();
            UpdateLicensedVolume();
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or
            ArgumentException or NotSupportedException)
        {
            fadeTimer?.Stop();
            fadeTimer = null;
            licensedPlayer?.Close();
            licensedPlayer = null;
            return false;
        }
    }

    private void OnLicensedFailure(MediaPlayer player)
    {
        if (disposed || !ReferenceEquals(licensedPlayer, player)) return;
        fadeTimer?.Stop();
        fadeTimer = null;
        licensedPlayer = null;
        player.Close();
        // A missing or unsupported source must not silence the original soundtrack.
        Ready = Open();
    }

    private void UpdateLicensedVolume()
    {
        if (licensedPlayer is not null) fadeTimer?.Start();
    }

    private void RampLicensedVolume()
    {
        if (licensedPlayer is null || disposed) { fadeTimer?.Stop(); return; }
        double target = enabled ? volume * 0.45 : 0;
        double current = licensedPlayer.Volume;
        double next = current + (target - current) * 0.38;
        if (Math.Abs(target - next) < 0.001)
        {
            licensedPlayer.Volume = target;
            fadeTimer?.Stop();
        }
        else licensedPlayer.Volume = next;
    }

    private async Task Open()
    {
        AudioClip loaded = await Load(track);
        if (disposed) return;
        clip = loaded;
        CasinoAudio.Start();
        CasinoAudio.Mixer.Loop(loaded, owner, enabled ? volume * 0.45 : 0);
    }

    public void SetEnabled(bool value)
    {
        enabled = value;
        if (licensedPlayer is not null) { UpdateLicensedVolume(); return; }
        if (clip is null) { if (Ready.IsCompleted) Ready = Open(); }
        else CasinoAudio.Mixer.Volume(owner, enabled ? volume * 0.45 : 0);
    }

    public void SetVolume(double value)
    {
        volume = Math.Clamp(value, 0, 0.32);
        if (licensedPlayer is not null) { UpdateLicensedVolume(); return; }
        if (clip is not null) CasinoAudio.Mixer.Volume(owner, enabled ? volume * 0.45 : 0);
    }

    public void Dispose()
    {
        disposed = true;
        fadeTimer?.Stop();
        licensedPlayer?.Close();
        licensedPlayer = null;
        CasinoAudio.Mixer.Stop(owner);
    }
}
