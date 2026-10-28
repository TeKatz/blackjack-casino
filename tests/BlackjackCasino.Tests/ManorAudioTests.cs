using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ManorAudioTests
{
    [Fact]
    public void EveryOriginalCueIsValidSoftPcmWithBoundedTransients()
    {
        var hashes=new HashSet<string>();
        foreach(ManorCue cue in Enum.GetValues<ManorCue>())
        {
            byte[] bytes=ManorSoundSynthesis.Cue(cue);
            Assert.Equal("RIFF",System.Text.Encoding.ASCII.GetString(bytes,0,4));
            Assert.Equal("WAVE",System.Text.Encoding.ASCII.GetString(bytes,8,4));
            Assert.Equal(ManorSoundSynthesis.Rate,BitConverter.ToInt32(bytes,24));
            Assert.Equal(bytes.Length-44,BitConverter.ToInt32(bytes,40));
            double peak=0,squares=0; int previous=0,maxStep=0;
            for(int i=44;i<bytes.Length;i+=2)
            {
                short value=BitConverter.ToInt16(bytes,i); peak=Math.Max(peak,Math.Abs((double)value)/short.MaxValue);
                squares+=Math.Pow(value/(double)short.MaxValue,2); maxStep=Math.Max(maxStep,Math.Abs(value-previous)); previous=value;
            }
            Assert.InRange(peak,0.001,0.12);
            Assert.InRange(Math.Sqrt(squares/((bytes.Length-44)/2)),0.0001,0.035);
            Assert.True(maxStep<7000);
            hashes.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        }
        Assert.True(hashes.Count>=20);
    }
}
