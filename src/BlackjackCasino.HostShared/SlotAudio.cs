namespace BlackjackCasino.Desktop;

internal enum SlotSound { Start, Tick, Stop, Win, Jackpot, Push, Loss, Anticipation, Wild, Blackout, FreeSpins,
    NeonLockdown, NeonOverload, NeonTotalBlackout, NeonOverride, NeonBigWin, NeonMegaWin,
    NeonWinSparkle, NeonWinCascade, NeonWinTriumph }

/// <summary>Original synthesized mechanical and musical cues, offline and asset-free.</summary>
internal sealed class SlotAudio : IDisposable
{
    private static readonly IReadOnlyDictionary<SlotSound,Lazy<AudioClip>> Cues = Enum.GetValues<SlotSound>()
        .ToDictionary(c => c, c => new Lazy<AudioClip>(() => AudioClip.FromWave(BuildCue(c))));
    internal static void Prewarm() => _ = Task.Run(() => { foreach (var cue in Cues.Values) _ = cue.Value; });
    private readonly object owner = new();
    public void Play(SlotSound sound, bool enabled)
    {
        if (enabled) CasinoAudio.Play(Cues[sound].Value, owner);
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
            SlotSound.Jackpot => 1.65,
            SlotSound.Win => 0.65,
            SlotSound.Anticipation => 0.70,
            SlotSound.Wild => 0.48,
            SlotSound.Blackout => 0.72,
            SlotSound.FreeSpins => 1.10,
            SlotSound.NeonLockdown => 0.95,
            SlotSound.NeonOverload => 1.25,
            SlotSound.NeonTotalBlackout => 1.65,
            SlotSound.NeonOverride => 0.82,
            SlotSound.NeonBigWin => 1.32,
            SlotSound.NeonMegaWin => 2.15,
            SlotSound.NeonWinSparkle => 0.83,
            SlotSound.NeonWinCascade => 1.43,
            SlotSound.NeonWinTriumph => 2.24,
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
            SlotSound.NeonBigWin => [392.00, 523.25, 659.25, 783.99, 1046.50, 1318.51],
            SlotSound.NeonMegaWin => [261.63, 392.00, 523.25, 659.25, 783.99, 1046.50, 1318.51, 1567.98, 2093.00],
            SlotSound.NeonWinSparkle => [987.77, 1318.51, 1567.98, 2093.00],
            SlotSound.NeonWinCascade => [392.00, 587.33, 783.99, 1174.66, 1567.98, 2093.00, 2349.32],
            SlotSound.NeonWinTriumph => [261.63, 392.00, 523.25, 783.99, 1046.50, 1318.51, 1567.98, 2093.00, 2637.02, 3135.96],
            SlotSound.NeonOverride => [196, 293.66, 440, 659.25],
            SlotSound.Win => [523.25, 659.25, 783.99, 1046.50],
            SlotSound.FreeSpins => [392.00, 523.25, 659.25, 783.99, 1046.50],
            SlotSound.Wild => [659.25, 880.00, 1046.50],
            SlotSound.Anticipation => [329.63, 440.00, 554.37, 659.25],
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
                SlotSound.Blackout => (Math.Sin(2 * Math.PI * 58 * t) * 0.72 + (noise.NextDouble() * 2 - 1) * 0.22) * Envelope(t, 0.004, 0.20),
                SlotSound.NeonLockdown => (Math.Sin(2 * Math.PI * (92 - 45 * t) * t) * 0.88
                    + (noise.NextDouble() * 2 - 1) * 0.31) * Envelope(t, 0.007, 0.27),
                SlotSound.NeonOverload => (Math.Sin(2 * Math.PI * (100 - 24 * t) * t) * 0.9
                    + Math.Sin(2 * Math.PI * 245 * t) * 0.22
                    + (noise.NextDouble() * 2 - 1) * 0.28) * Envelope(t, 0.004, 0.42),
                SlotSound.NeonTotalBlackout => (Math.Sin(2 * Math.PI * (66 - 14 * t) * t) * 1.1
                    + (noise.NextDouble() * 2 - 1) * 0.36 * Math.Sin(2 * Math.PI * 9 * t))
                    * Envelope(t, 0.006, 0.58),
                _ => 0,
            };
            for (int note = 0; note < notes.Length; note++)
            {
                double local = t - note * (sound is SlotSound.Jackpot or SlotSound.NeonMegaWin or SlotSound.NeonWinTriumph ? 0.19 : 0.14);
                if (local >= 0)
                {
                    sample += (Math.Sin(2 * Math.PI * notes[note] * local)
                        + 0.18 * Math.Sin(2 * Math.PI * notes[note] * 2 * local)) * Envelope(local, 0.004, 0.11) * 0.4;
                }
            }
            // Fade the last 180 ms of every synthesized cue. Long low-frequency
            // lockdown signals otherwise stop at a nonzero phase and click.
            double release = Math.Clamp((duration - t) / 0.18, 0, 1);
            writer.Write((short)(Math.Clamp(sample * 0.18 * release, -1, 1) * short.MaxValue));
        }
        return stream.ToArray();
    }

    private static double Envelope(double time, double attack, double decay) => Math.Min(1, time / attack) * Math.Exp(-time / decay);
}
