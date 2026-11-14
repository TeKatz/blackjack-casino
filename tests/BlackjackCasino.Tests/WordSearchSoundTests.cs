using BlackjackCasino.HostShared;

namespace BlackjackCasino.Tests;

public sealed class WordSearchSoundTests
{
    [Theory]
    [InlineData("Found")]
    [InlineData("NewDiscovery")]
    public void AllSavedWordFeedbackCuesAreShortSoftAndClickFree(string name)
    {
        WordSearchCue cue = Enum.Parse<WordSearchCue>(name);
        AudioClip clip = WordSearchSoundSynthesis.Build(cue);
        Assert.InRange(clip.Frames, (int)(AudioClip.Rate*.18), (int)(AudioClip.Rate*.5));
        Assert.All(clip.Samples, sample=>Assert.True(float.IsFinite(sample)));
        Assert.InRange(clip.Samples.Max(x=>Math.Abs(x)), .004f, .16f);
        Assert.InRange(Math.Abs(clip.Samples[^1]),0f,.001f);
        AudioClip decoded=AudioClip.FromWave(clip.Wave());
        Assert.Equal(clip.Frames,decoded.Frames);
    }
}
