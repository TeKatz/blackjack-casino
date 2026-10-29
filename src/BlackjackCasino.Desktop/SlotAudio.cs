namespace BlackjackCasino.Desktop;

internal enum SlotSound { Start, Tick, Stop, Win, Jackpot, Push, Loss }

/// <summary>Original synthesized mechanical and musical cues, offline and asset-free.</summary>
internal sealed class SlotAudio : IDisposable
{
    private static readonly IReadOnlyDictionary<SlotSound,AudioClip> Cues = Enum.GetValues<SlotSound>().ToDictionary(c => c, c => AudioClip.FromWave(BuildCue(c)));
    private readonly object owner = new();
    public void Play(SlotSound sound, bool enabled)
    {
        if (enabled) CasinoAudio.Play(Cues[sound], owner);
    }
    public void Stop() => CasinoAudio.Mixer.Stop(owner);
    public void Dispose() => Stop();

    private static byte[] BuildCue(SlotSound sound)
    {
        if (sound is SlotSound.Start or SlotSound.Loss)
            return CasinoSoundSynthesis.Build(sound == SlotSound.Start ? TableCue.ReelSpin : TableCue.ReelLoss).Wave();
        double duration = sound switch
        {
            SlotSound.Tick => 0.045,
            SlotSound.Stop => 0.16,
            SlotSound.Start => 0.30,
            SlotSound.Jackpot => 1.65,
            SlotSound.Win => 0.65,
            _ => 0.30,
        };
        const int rate = 44_100;
        int count = (int)(rate * duration);
        using var stream = new MemoryStream(44 + count * 2);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + count * 2);
        writer.Write("WAVEfmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(rate);
        writer.Write(rate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write("data"u8.ToArray());
        writer.Write(count * 2);
        var noise = new Random(7719 + (int)sound);
        double[] notes = sound switch
        {
            SlotSound.Jackpot => [523.25, 659.25, 783.99, 1046.50, 783.99, 1046.50, 1318.51, 1567.98],
            SlotSound.Win => [523.25, 659.25, 783.99, 1046.50],
            SlotSound.Push => [440, 554.37],
            SlotSound.Loss => [220, 196],
            _ => [],
        };
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)rate;
            double sample = sound switch
            {
                SlotSound.Tick => ((noise.NextDouble() * 2 - 1) * 0.22 + Math.Sin(2 * Math.PI * 1500 * t) * 0.26) * Envelope(t, 0.0007, 0.009),
                SlotSound.Stop => (Math.Sin(2 * Math.PI * 170 * t) * 0.6 + Math.Sin(2 * Math.PI * 1050 * t) * 0.23) * Envelope(t, 0.001, 0.036),
                SlotSound.Start => Math.Sin(2 * Math.PI * (180 * t + 800 * t * t)) * Envelope(t, 0.008, 0.09),
                _ => 0,
            };
            for (int note = 0; note < notes.Length; note++)
            {
                double local = t - note * (sound == SlotSound.Jackpot ? 0.16 : 0.10);
                if (local >= 0)
                {
                    sample += (Math.Sin(2 * Math.PI * notes[note] * local)
                        + 0.18 * Math.Sin(2 * Math.PI * notes[note] * 2 * local)) * Envelope(local, 0.004, 0.11) * 0.4;
                }
            }
            writer.Write((short)(Math.Clamp(sample * 0.18, -1, 1) * short.MaxValue));
        }
        return stream.ToArray();
    }

    private static double Envelope(double time, double attack, double decay) => Math.Min(1, time / attack) * Math.Exp(-time / decay);
}
