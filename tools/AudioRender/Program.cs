using System.Text.Json;
using BlackjackCasino.Desktop;

string output=Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/audio-review");
Directory.CreateDirectory(output);
var report=new List<object>();
foreach(Soundtrack track in Enum.GetValues<Soundtrack>())
{
    AudioClip clip=CasinoMusicSynthesis.Build(track);
    string name=track.ToString().ToLowerInvariant()+"-loop.wav";
    File.WriteAllBytes(Path.Combine(output,name),clip.Wave());
    report.Add(new {File=name,Bpm=CasinoMusicSynthesis.Tempo(track),Seconds=clip.Frames/(double)AudioClip.Rate,
        Peak=clip.Samples.Max(s=>Math.Abs(s)),Rms=Math.Sqrt(clip.Samples.Sum(s=>(double)s*s)/clip.Samples.Length),
        Seam=Math.Max(Math.Abs(clip.Samples[0]-clip.Samples[^2]),Math.Abs(clip.Samples[1]-clip.Samples[^1]))});
}
foreach(TableCue cue in Enum.GetValues<TableCue>())
    File.WriteAllBytes(Path.Combine(output,cue.ToString().ToLowerInvariant()+".wav"),CasinoSoundSynthesis.Build(cue).Wave());
foreach(ManorCue cue in Enum.GetValues<ManorCue>())
    File.WriteAllBytes(Path.Combine(output,"manor-"+cue+".wav"),ManorSoundSynthesis.Cue(cue));
var mixer=new AudioMixer(); var owner=new object(); var samples=new List<float>();
for(int tick=0;tick<16;tick++)
{
    if(tick<8) mixer.Play(AudioClip.FromWave(ManorSoundSynthesis.Cue(tick%2==0 ? ManorCue.Reel : ManorCue.Stop2)),owner,0.65);
    if(tick==5) mixer.Play(AudioClip.FromWave(ManorSoundSynthesis.Cue(ManorCue.SmallWin)),owner,0.65);
    var pcm=new short[(int)(0.12*AudioClip.Rate)*2]; mixer.Render(pcm);
    samples.AddRange(pcm.Select(s=>s/32768f));
}
File.WriteAllBytes(Path.Combine(output,"manor-overlapping-effects.wav"),new AudioClip(samples.ToArray()).Wave());
File.WriteAllText(Path.Combine(output,"audio-report.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions {WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(report));
