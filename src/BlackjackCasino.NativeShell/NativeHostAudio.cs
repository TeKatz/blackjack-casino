using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.NativeShell;

/// <summary>
/// WPF-free music and interaction cues. Uses the same original PCM synthesis
/// and waveOut mixer as the desktop application; optional licensed MP3s play
/// through Windows MCI and are never accessed outside the installed asset tree.
/// No WebView2-origin trust or financial logic is changed by audio.
/// </summary>
internal sealed class NativeHostAudio : IDisposable
{
    private static readonly ConcurrentDictionary<Soundtrack, Lazy<Task<AudioClip>>> Tracks = new();
    private static readonly AudioClip Cue = CreateCue();
    private readonly object owner = new();
    private Soundtrack? current;
    private int generation;
    private bool disposed;
    private bool enabled;
    private double volume;
    private string? licensedAlias;

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "mciSendStringW")]
    private static extern int MciSendString(string command, IntPtr result, int count, IntPtr callback);

    internal static Soundtrack TrackFor(string? mode) => mode switch
    {
        null => Soundtrack.Lobby,
        "blackjack" or "roulette" => Soundtrack.Blackjack,
        "reels" or "neon-vault" or "nine-lives" => Soundtrack.Reels,
        "midnight-manor" => Soundtrack.Manor,
        "midnight-dice" => Soundtrack.DiceLounge,
        "chill-lounge" => Soundtrack.ChillLounge,
        "midnight-merge" => Soundtrack.ChillLoungeActivity,
        "casino-empire" => Soundtrack.Lobby,
        _ => Soundtrack.Lobby,
    };

    internal void Update(string? mode, AppPreferences preferences)
    {
        if (disposed) return;
        string area = mode switch
        {
            null => "lobby",
            "reels" => "midnight-reels",
            "midnight-dice" => "kniffel",
            _ => mode,
        };
        AreaPresentationSettings settings = preferences.Presentation(area);
        bool allow = preferences.MusicEnabled && settings.MusicEnabled;
        double gain = Math.Clamp(preferences.MusicVolume, 0, .32) * .45;
        Soundtrack track = TrackFor(mode);
        if (current == track && enabled == allow)
        {
            volume = gain;
            if (licensedAlias is not null)
                _ = MciSendString("setaudio " + licensedAlias + " volume to " +
                    (int)Math.Round(gain * 1000), IntPtr.Zero, 0, IntPtr.Zero);
            else if (allow) CasinoAudio.Mixer.Volume(owner, gain);
            return;
        }
        Stop();
        current = track;
        enabled = allow;
        volume = gain;
        if (!allow || gain <= 0) return;
        if (TryPlayLicensed(track, gain)) return;
        int ticket = generation;
        _ = StartSynthesizedAsync(track, ticket);
    }

    private bool TryPlayLicensed(Soundtrack track, double gain)
    {
        string? file = track switch
        {
            Soundtrack.Manor => "midnight-manor.mp3",
            Soundtrack.Lobby => "lobby.mp3",
            Soundtrack.DiceLounge => "midnight-dice.mp3",
            Soundtrack.ChillLounge or Soundtrack.ChillLoungeActivity => "chill-lounge.mp3",
            _ => null,
        };
        if (file is null) return false;
        string path = Path.Combine(AppContext.BaseDirectory, "Music", "Licensed", file);
        if (!File.Exists(path) || path.Contains('"')) return false;
        string alias = "blackjack_native_music";
        if (MciSendString("open \"" + path + "\" type mpegvideo alias " + alias,
                IntPtr.Zero, 0, IntPtr.Zero) != 0) return false;
        licensedAlias = alias;
        _ = MciSendString("setaudio " + alias + " volume to " +
            (int)Math.Round(gain * 1000), IntPtr.Zero, 0, IntPtr.Zero);
        if (MciSendString("play " + alias + " repeat", IntPtr.Zero, 0, IntPtr.Zero) == 0)
            return true;
        _ = MciSendString("close " + alias, IntPtr.Zero, 0, IntPtr.Zero);
        licensedAlias = null;
        return false;
    }

    private async Task StartSynthesizedAsync(Soundtrack track, int ticket)
    {
        try
        {
            AudioClip loaded = await Tracks.GetOrAdd(track,
                key => new Lazy<Task<AudioClip>>(
                    () => Task.Run(() => CasinoMusicSynthesis.Build(key)))).Value;
            if (disposed || !enabled || current != track || generation != ticket) return;
            CasinoAudio.Start();
            CasinoAudio.Mixer.Loop(loaded, owner, volume);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException
            or NotSupportedException or InvalidOperationException)
        {
            // Missing audio assets are nonfatal; the casino must remain usable.
        }
    }

    internal void PlayCue(bool allowed)
    {
        if (disposed || !allowed) return;
        CasinoAudio.Play(Cue, owner, .15);
    }

    private static AudioClip CreateCue()
    {
        const int length = AudioClip.Rate / 8;
        var samples = new float[length * 2];
        for (int i = 0; i < length; i++)
        {
            double t = i / (double)AudioClip.Rate;
            double fall = Math.Pow(Math.Max(0, 1 - t * 8), 2);
            double wave = (Math.Sin(2 * Math.PI * (420 + t * 900) * t) * .65
                + Math.Sin(2 * Math.PI * 650 * t) * .23) * fall;
            samples[i * 2] = samples[i * 2 + 1] = (float)wave;
        }
        return new AudioClip(samples);
    }

    internal void Suspend() => Stop();

    private void Stop()
    {
        generation++;
        if (licensedAlias is { } alias)
        {
            _ = MciSendString("close " + alias, IntPtr.Zero, 0, IntPtr.Zero);
            licensedAlias = null;
        }
        CasinoAudio.Mixer.Stop(owner);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
    }
}
