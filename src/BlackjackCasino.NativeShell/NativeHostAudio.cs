using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using BlackjackCasino.HostShared;
using BlackjackCasino.Core;

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
    private static readonly ConcurrentDictionary<TableCue, AudioClip> TableCues = new();
    private static readonly ConcurrentDictionary<MergeCue, AudioClip> MergeCues = new();
    private static readonly ConcurrentDictionary<MemoryCue, AudioClip> MemoryCues = new();
    private static readonly ConcurrentDictionary<WordSearchCue, AudioClip> WordCues = new();
    private readonly SlotAudio slotCues = new();
    private readonly NineLivesAudio nineCues = new();
    private readonly object owner = new();
    private Soundtrack? current;
    private int generation;
    private bool disposed;
    private bool enabled;
    private double volume;
    private string? licensedAlias;

    internal NativeHostAudio()
    {
        // Precompute the original mechanical and melodic cues before the
        // first spin; the initial WinMM playback must not stall for synthesis.
        SlotAudio.Prewarm();
        NineLivesAudio.Prewarm();
    }

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
        // A zero-volume start creates no player. If the user raises the
        // master volume later, reopen the soundtrack instead of only changing
        // the gain on a nonexistent voice.
        if (current == track && enabled == allow && (volume > 0 || gain <= 0))
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

    internal void PlayGameCue(string? mode, string action, bool allowed)
    {
        if (disposed || !allowed) return;
        switch (mode)
        {
            case "reels":
            case "neon-vault":
                slotCues.Play(action switch
                {
                    "spin" => SlotSound.Start,
                    "reel-stop" => SlotSound.Stop,
                    "feature-impact" => SlotSound.Win,
                    _ => SlotSound.Tick,
                }, true);
                break;
            case "nine-lives":
                nineCues.Play(action switch
                {
                    "spin" => NineLivesCue.Spin,
                    "reel-stop" => NineLivesCue.ReelStop,
                    "feature-impact" => NineLivesCue.Win,
                    _ => NineLivesCue.Pounce,
                }, true);
                break;
            case "midnight-merge":
                PlayOriginal(MergeCues.GetOrAdd(action switch
                {
                    "merge" or "combine" => MergeCue.Merge,
                    "order" or "order.claim" => MergeCue.Order,
                    "spawn" or "produce" => MergeCue.Spawn,
                    "move" or "place" => MergeCue.Drop,
                    _ => MergeCue.PickUp,
                }, MergeSoundSynthesis.Build));
                break;
            case "midnight-dice":
                PlayOriginal(TableCues.GetOrAdd(
                    action.Contains("roll", StringComparison.Ordinal) ? TableCue.DiceRoll : TableCue.Menu,
                    CasinoSoundSynthesis.Build));
                break;
            case "chill-lounge" when action.StartsWith("memory.", StringComparison.Ordinal):
                PlayOriginal(MemoryCues.GetOrAdd(action switch
                {
                    "memory.flip" => MemoryCue.Flip,
                    "memory.match" => MemoryCue.Match,
                    "memory.complete" => MemoryCue.Complete,
                    _ => MemoryCue.Flip,
                }, MemorySoundSynthesis.Build));
                break;
            case "chill-lounge" when action.StartsWith("word-search.", StringComparison.Ordinal):
                if (action is "word-search.select" or "word-search.submit" or "word-search.found")
                    PlayOriginal(WordCues.GetOrAdd(WordSearchCue.Found,
                        WordSearchSoundSynthesis.Build));
                break;
            case "chill-lounge":
            case "casino-empire":
                PlayOriginal(TableCues.GetOrAdd(
                    action.Contains("roll", StringComparison.Ordinal) ? TableCue.DiceRoll : TableCue.Menu,
                    CasinoSoundSynthesis.Build));
                break;
        }
    }

    // All winning and loss outcomes are read from the C# authoritative spin
    // result, never from untrusted frontend payout claims. These choices match
    // the established WPF sound palette and never change a player balance.
    internal void PlayReelsOutcome(SlotSpinResult result, bool allowed)
    {
        if (disposed || !allowed) return;
        SlotSound cue = result.Multiplier == 100 ? SlotSound.Jackpot
            : result.Net > 0 ? SlotSound.Win
            : result.Net == 0 ? SlotSound.Push : SlotSound.Loss;
        slotCues.Play(cue, true);
    }

    internal void PlayNeonOutcome(NeonVaultSpinResult result, bool allowed)
    {
        if (disposed || !allowed) return;
        SlotSound outcome = result.Jackpot != NeonVaultJackpot.None
            ? result.Jackpot is NeonVaultJackpot.Grand or NeonVaultJackpot.Major
                ? SlotSound.NeonMegaWin : SlotSound.Jackpot
            : result.Payout > 0 && result.TotalMultiplier >= 50m ? SlotSound.NeonMegaWin
            : result.Payout > 0 && result.TotalMultiplier >= 10m ? SlotSound.NeonBigWin
            : result.FreeSpinsAwarded > 0 ? SlotSound.FreeSpins
            : result.Payout > 0 ? SlotSound.Win : SlotSound.Loss;
        slotCues.Play(outcome, true);
        if (result.Payout > 0)
        {
            SlotSound flourish = result.Jackpot is NeonVaultJackpot.Grand or NeonVaultJackpot.Major
                || result.TotalMultiplier >= 50m ? SlotSound.NeonWinTriumph
                : result.Jackpot != NeonVaultJackpot.None || result.TotalMultiplier >= 10m
                    ? SlotSound.NeonWinCascade : SlotSound.NeonWinSparkle;
            slotCues.Play(flourish, true);
        }
    }

    internal void PlayNeonFeature(NeonVaultSpinResult result, bool allowed)
    {
        if (disposed || !allowed) return;
        SlotSound? cue = result.Blackout switch
        {
            NeonVaultBlackout.Total => SlotSound.NeonTotalBlackout,
            NeonVaultBlackout.Overload => SlotSound.NeonOverload,
            NeonVaultBlackout.Blackout => SlotSound.NeonLockdown,
            _ => result.SecurityOverrideReel.HasValue ? SlotSound.NeonOverride
                : result.ExpandedReels.Count > 0 ? SlotSound.Wild
                : result.FreeSpinsAwarded > 0 ? SlotSound.FreeSpins : null,
        };
        if (cue is SlotSound sound) slotCues.Play(sound, true);
    }

    internal void PlayNineOutcome(NineLivesSpinResult result, bool allowed)
    {
        if (disposed || !allowed) return;
        if (result.PounceWildCell.HasValue) nineCues.Play(NineLivesCue.Pounce, true);
        NineLivesCue outcome = result.FullGridFrenzy ? NineLivesCue.Frenzy
            : result.BonusSpinsAwarded > 0 ? NineLivesCue.Bonus
            : result.Payout >= result.Bet * 12m ? NineLivesCue.BigWin
            : result.Payout > 0 ? NineLivesCue.Win : NineLivesCue.Loss;
        nineCues.Play(outcome, true);
    }

    private void PlayOriginal(AudioClip clip) => CasinoAudio.Play(clip, owner, .65);

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
        current = null;
        enabled = false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Stop();
        slotCues.Dispose();
        nineCues.Dispose();
    }
}
