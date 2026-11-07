using System.Reflection;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class NeonVaultAudioCuesTests
{
    [Theory]
    [InlineData("NeonLockdown")]
    [InlineData("NeonOverload")]
    [InlineData("NeonTotalBlackout")]
    [InlineData("NeonOverride")]
    [InlineData("NeonBigWin")]
    [InlineData("NeonMegaWin")]
    public void HighRollerCuesAreOfflineDecodableAndBounded(string cueName)
    {
        SlotSound cue = Enum.Parse<SlotSound>(cueName);
        MethodInfo synthesis = typeof(SlotAudio).GetMethod("BuildCue",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        byte[] wave = (byte[])synthesis.Invoke(null, [cue])!;
        AudioClip decoded = AudioClip.FromWave(wave);
        Assert.True(decoded.Frames > AudioClip.Rate / 2);
        Assert.All(decoded.Samples, sample => Assert.True(float.IsFinite(sample)));
        Assert.InRange(decoded.Samples.Max(sample => Math.Abs(sample)), .002f, .50f);
        Assert.InRange(Math.Abs(decoded.Samples[^1]), 0f, .004f);
    }
}
