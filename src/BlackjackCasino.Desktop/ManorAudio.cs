using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class ManorAudio : IDisposable
{
    private readonly Dictionary<ManorCue,MediaPlayer> cues=[];
    private readonly List<MediaPlayer> layers=[];
    private readonly string folder=Path.Combine(Path.GetTempPath(),"BlackjackCasino","manor-audio-v1");
    private bool disposed;
    public bool SoundEnabled { get; set; }
    public bool MusicEnabled { get; set; }
    public double MusicVolume { get; set; }

    public void Play(ManorCue cue)
    {
        if(disposed || !SoundEnabled) return;
        try
        {
            if(!cues.TryGetValue(cue,out MediaPlayer? player))
            {
                string path=Ensure(cue+".wav",()=>ManorSoundSynthesis.Cue(cue));
                player=new MediaPlayer { Volume=0.65 };
                player.Open(new Uri(path));
                cues.Add(cue,player);
            }
            player.Position=TimeSpan.Zero; player.Play();
        }
        catch { /* Audio is optional; the financial state is already committed. */ }
    }

    public void Mood(int haunt,bool anticipation=false)
    {
        if(disposed) return;
        try
        {
            if(layers.Count==0 && MusicEnabled)
            {
                for(int i=0;i<3;i++)
                {
                    int layer=i;
                    var player=new MediaPlayer { Volume=0 };
                    player.Open(new Uri(Ensure("ambience-"+i+".wav",()=>ManorSoundSynthesis.Ambience(layer))));
                    player.MediaEnded+=(_,_)=> { if(!disposed) { player.Position=TimeSpan.Zero; player.Play(); } };
                    player.Play(); layers.Add(player);
                }
            }
            double gain=MusicEnabled ? Math.Clamp(MusicVolume,0,0.32) : 0;
            if(layers.Count==3)
            {
                layers[0].Volume=gain*(anticipation ? 0.25 : 0.7);
                layers[1].Volume=gain*(anticipation ? 0.65 : haunt>=3 ? 0.35 : 0);
                layers[2].Volume=gain*(haunt>=6 ? 0.45 : 0);
            }
            if(!SoundEnabled) foreach(MediaPlayer player in cues.Values) player.Stop();
        }
        catch { }
    }

    private string Ensure(string name,Func<byte[]> make)
    {
        Directory.CreateDirectory(folder);
        string path=Path.Combine(folder,name);
        if(!File.Exists(path)) File.WriteAllBytes(path,make());
        return path;
    }
    public void Dispose()
    {
        disposed=true;
        foreach(MediaPlayer player in cues.Values.Concat(layers)) { player.Stop(); player.Close(); }
        cues.Clear(); layers.Clear();
    }
}
