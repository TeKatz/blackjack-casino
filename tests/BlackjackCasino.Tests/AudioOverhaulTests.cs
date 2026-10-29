using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class AudioOverhaulTests
{
    [Theory]
    [InlineData(0,72)] [InlineData(1,104)] [InlineData(2,62)]
    public void TracksAreFullStereoPhrasesWithContinuousLoopBoundaries(int trackId,int tempo)
    {
        Soundtrack track=(Soundtrack)trackId;
        AudioClip clip=CasinoMusicSynthesis.Build(track);
        Assert.Equal(tempo,CasinoMusicSynthesis.Tempo(track));
        Assert.InRange(clip.Frames,(int)Math.Round(CasinoMusicSynthesis.Beats(track)*60d/tempo*AudioClip.Rate)-1,(int)Math.Round(CasinoMusicSynthesis.Beats(track)*60d/tempo*AudioClip.Rate)+1);
        Assert.DoesNotContain(clip.Samples,s=>!float.IsFinite(s));
        double peak=clip.Samples.Max(s=>Math.Abs(s));
        double rms=Math.Sqrt(clip.Samples.Sum(s=>(double)s*s)/clip.Samples.Length);
        Assert.InRange(peak,0.1,0.65); Assert.InRange(rms,0.015,0.18);
        Assert.Contains(Enumerable.Range(0,clip.Frames),i=>Math.Abs(clip.Samples[i*2]-clip.Samples[i*2+1])>0.001);
        for(int channel=0;channel<2;channel++)
            Assert.InRange(Math.Abs(clip.Samples[channel]-clip.Samples[clip.Samples.Length-2+channel]),0,0.025);
        // Run through the seam on the actual mixer, including non-block-aligned loop lengths.
        var mixer=new AudioMixer(); mixer.Loop(clip,new object(),0.2);
        var block=new short[1024];
        int rendered=0;
        while(rendered<clip.Frames*2+600) { mixer.Render(block); rendered+=block.Length/2; }
        Assert.Equal(1,mixer.VoiceCount);
        Assert.Contains(block,s=>s!=0);
    }

    [Fact]
    public void RepeatedCuesOverlapInsteadOfRestartingAndOwnerStopsDoNotMuteMusic()
    {
        var mixer=new AudioMixer(); var owner=new object(); var music=new object();
        var cue=new AudioClip(Enumerable.Repeat(0.05f,1000).ToArray());
        mixer.Loop(new(Enumerable.Repeat(0.01f,1000).ToArray()),music,1);
        mixer.Play(cue,owner); var block=new short[200]; mixer.Render(block); short single=block[^1];
        mixer.Play(cue,owner); mixer.Render(block);
        Assert.Equal(3,mixer.VoiceCount); Assert.True(block[^1]>single*1.7);
        mixer.Stop(owner);
        for(int i=0;i<100;i++) mixer.Render(block);
        Assert.Equal(1,mixer.VoiceCount); Assert.True(block[^1]>0);
    }

    [Fact]
    public void LoopReplacementFadesOutOldTrackAndPolyphonyIsBounded()
    {
        var mixer=new AudioMixer(); var owner=new object();
        mixer.Loop(new(Enumerable.Repeat(0.05f,2000).ToArray()),owner,1);
        var block=new short[2048]; mixer.Render(block);
        mixer.Loop(new(Enumerable.Repeat(-0.05f,2000).ToArray()),owner,1);
        for(int i=0;i<12;i++) mixer.Render(block);
        Assert.Equal(1,mixer.VoiceCount); Assert.True(block[^1]<0);
        for(int i=0;i<100;i++) mixer.Play(new(new float[10000]),new object());
        Assert.Equal(49,mixer.VoiceCount);
    }

    [Fact]
    public void AllNewEffectsStayQuietAndFinishWithASoftRelease()
    {
        foreach(TableCue cue in Enum.GetValues<TableCue>())
        {
            AudioClip clip=CasinoSoundSynthesis.Build(cue);
            Assert.InRange(clip.Samples.Max(s=>Math.Abs(s)),0.001,0.06);
            Assert.InRange(Math.Abs(clip.Samples[^1]),0,0.001);
            AudioClip decoded=AudioClip.FromWave(clip.Wave());
            Assert.Equal(clip.Frames,decoded.Frames);
        }
        AudioClip manor=AudioClip.FromWave(ManorSoundSynthesis.Cue(ManorCue.DoorOpen));
        Assert.Equal((int)(0.9*AudioClip.Rate),manor.Frames);
    }
}
