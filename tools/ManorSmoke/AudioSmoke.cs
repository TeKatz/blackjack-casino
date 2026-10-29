using System.IO;
using BlackjackCasino.Desktop;

internal static class AudioSmoke
{
    internal static async Task Check(string output)
    {
        CasinoAudio.Start();
        using(var music=new BackgroundMusic(Soundtrack.Reels))
        {
            music.Start(false,0.1); await music.Ready;
            music.SetVolume(0.05); music.SetEnabled(false);
        }
        await Task.Delay(150);
        long before=CasinoAudio.RenderedFrames;
        // Deliberately stall the dispatcher; audio must continue on its own sample clock.
        Thread.Sleep(180);
        if(CasinoAudio.RenderedFrames<=before+512*3) throw new Exception("Audio output stalled with the WPF dispatcher: "+CasinoAudio.DeviceStatus);
        using(var manor=new ManorAudio {SoundEnabled=false,MusicEnabled=false})
        {
            manor.Mood(0); manor.Play(ManorCue.Reel); manor.Room(BlackjackCasino.Core.Manor.ManorArea.Library,4); manor.EndRoom();
        }
        File.WriteAllText(Path.Combine(output,"audio-runtime-result.txt"),"PASS: dedicated PCM render clock continues during dispatcher stall, async music preparation, volume/mute, Manor scene lifecycle. Device status: "+CasinoAudio.DeviceStatus+". Human speaker/headphone audition remains required for mix preference.");
    }
}
