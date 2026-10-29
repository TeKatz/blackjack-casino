using BlackjackCasino.Core.Manor;

namespace BlackjackCasino.Desktop;

internal sealed class ManorAudio : IDisposable
{
    private static readonly Lazy<IReadOnlyDictionary<ManorCue,AudioClip>> Cues = new(() => Enum.GetValues<ManorCue>().ToDictionary(c => c, c => AudioClip.FromWave(ManorSoundSynthesis.Cue(c))));
    private static readonly Dictionary<int,AudioClip> Ambiences = [];
    private readonly object effects = new(), heart = new(), room = new();
    private readonly BackgroundMusic music = new(Soundtrack.Manor);
    private ManorArea? activeArea;
    private bool started, disposed;
    public bool SoundEnabled { get; set; }
    public bool MusicEnabled { get; set; }
    public double MusicVolume { get; set; }
    internal ManorAudio() { _ = Cues.Value; }
    public void Play(ManorCue cue)
    {
        if (!disposed && SoundEnabled) CasinoAudio.Play(Cues.Value[cue], effects, 0.65);
    }
    private static AudioClip Ambience(int layer)
    {
        if (!Ambiences.TryGetValue(layer, out AudioClip? clip))
        {
            clip = AudioClip.FromWave(ManorSoundSynthesis.Ambience(layer)); Ambiences.Add(layer,clip);
        }
        return clip;
    }
    public void Mood(int haunt, bool anticipation = false)
    {
        if (disposed) return;
        if (!started) { music.Start(MusicEnabled,MusicVolume); started=true; }
        music.SetEnabled(MusicEnabled);
        double gain = MusicEnabled ? Math.Clamp(MusicVolume,0,0.32) : 0;
        music.SetVolume(gain*(anticipation ? 0.45 : 0.75));
        if (MusicEnabled && (haunt>=3 || anticipation))
            CasinoAudio.Mixer.Loop(Ambience(haunt>=6 ? 2 : 1),heart,gain*(haunt>=6 ? 0.45 : 0.3));
        else CasinoAudio.Mixer.Volume(heart,0);
        CasinoAudio.Mixer.Volume(room,gain*0.35);
        if (!SoundEnabled) CasinoAudio.Mixer.Stop(effects);
    }
    public void Room(ManorArea area,int haunt)
    {
        Mood(haunt);
        if (disposed || !MusicEnabled) return;
        if (activeArea != area)
        {
            CasinoAudio.Mixer.Stop(room);
            // A room owner changes so its previous ambience can finish its short gain ramp.
            activeArea=area;
            CasinoAudio.Mixer.Loop(Ambience(3+(int)area),room,Math.Clamp(MusicVolume,0,0.32)*0.35);
        }
    }
    public void EndRoom() { CasinoAudio.Mixer.Stop(room); activeArea=null; }
    public void Dispose()
    {
        disposed=true; music.Dispose();
        CasinoAudio.Mixer.Stop(effects); CasinoAudio.Mixer.Stop(heart); CasinoAudio.Mixer.Stop(room);
    }
}
