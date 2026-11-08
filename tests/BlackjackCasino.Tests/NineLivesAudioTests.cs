using System.Reflection;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class NineLivesAudioTests
{
    [Theory]
    [InlineData("Spin")]
    [InlineData("ReelStop")]
    [InlineData("Loss")]
    [InlineData("Win")]
    [InlineData("BigWin")]
    [InlineData("Pounce")]
    [InlineData("Bonus")]
    [InlineData("Frenzy")]
    public void DedicatedCuesAreValidClippedAndFadeCleanly(string name)
    {
        NineLivesCue cue = Enum.Parse<NineLivesCue>(name);
        MethodInfo builder = typeof(NineLivesAudio).GetMethod("BuildCue",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        byte[] wav = (byte[])builder.Invoke(null, [cue])!;
        AudioClip audio = AudioClip.FromWave(wav);
        Assert.True(audio.Frames > 0);
        Assert.All(audio.Samples, sample => Assert.True(float.IsFinite(sample)));
        Assert.InRange(audio.Samples.Max(sample => Math.Abs(sample)), .001f, .46f);
        Assert.InRange(Math.Abs(audio.Samples[^1]), 0f, .004f);
    }
}
