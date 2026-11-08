using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class DailyLoginClaimAudioTests
{
    [Fact]
    public void OriginalDailyClaimChimeIsFiniteSoftAndDecodable()
    {
        AudioClip clip = DailyLoginClaimAudio.Build();
        Assert.InRange(clip.Frames, AudioClip.Rate, AudioClip.Rate * 2);
        Assert.All(clip.Samples, sample => Assert.True(float.IsFinite(sample)));
        float peak = clip.Samples.Max(sample => Math.Abs(sample));
        Assert.InRange(peak, .01f, .30f);
        Assert.InRange(Math.Abs(clip.Samples[^1]), 0f, .001f);
        AudioClip decoded = AudioClip.FromWave(clip.Wave());
        Assert.Equal(clip.Frames, decoded.Frames);
    }
}
