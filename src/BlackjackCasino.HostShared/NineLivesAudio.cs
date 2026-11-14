namespace BlackjackCasino.Desktop;

/// <summary>Dedicated moonlit-cabinet cues. No sound from the generic SlotAudio library is used.</summary>
internal enum NineLivesCue { Spin, ReelStop, Loss, Win, BigWin, Pounce, Bonus, Frenzy }

internal sealed class NineLivesAudio : IDisposable
{
    private static readonly IReadOnlyDictionary<NineLivesCue, Lazy<AudioClip>> Cues =
        Enum.GetValues<NineLivesCue>()
            .ToDictionary(cue => cue, cue => new Lazy<AudioClip>(() => AudioClip.FromWave(BuildCue(cue))));
    internal static void Prewarm() => _ = Task.Run(() =>
    {
        foreach (Lazy<AudioClip> cue in Cues.Values) _ = cue.Value;
    });
    private readonly object owner = new();

    public void Play(NineLivesCue cue, bool enabled)
    {
        if (enabled) CasinoAudio.Play(Cues[cue].Value, owner);
    }

    public void Stop() => CasinoAudio.Mixer.Stop(owner);
    public void Dispose() => Stop();

    // Original night-sky / soft paw percussion and glass-bell arpeggio synthesis,
    // offline, clipped conservatively and faded to silence for gapless mixing.
    private static byte[] BuildCue(NineLivesCue cue)
    {
        const int rate = AudioClip.Rate;
        double duration = cue switch
        {
            NineLivesCue.Spin => .83,
            NineLivesCue.ReelStop => .22,
            NineLivesCue.Loss => .42,
            NineLivesCue.Win => 1.25,
            NineLivesCue.BigWin => 1.82,
            NineLivesCue.Pounce => .56,
            NineLivesCue.Bonus => 2.05,
            NineLivesCue.Frenzy => 2.65,
            _ => 1,
        };
        double[] melody = cue switch
        {
            NineLivesCue.Win => [587.33, 783.99, 880.00, 1174.66, 1567.98],
            NineLivesCue.BigWin => [440, 587.33, 880, 1174.66, 1318.51, 1760, 2349.32],
            NineLivesCue.Bonus => [392, 523.25, 783.99, 1046.50, 1567.98, 2093, 2637.02, 3135.96],
            NineLivesCue.Frenzy => [293.66, 440, 587.33, 880, 1174.66, 1760, 2349.32, 3135.96, 3520, 4698.63],
            NineLivesCue.Loss => [392, 329.63],
            NineLivesCue.Pounce => [659.25, 880],
            _ => [],
        };
        int count = (int)(rate * duration);
        using var stream = new MemoryStream(44 + count * 2);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8); writer.Write(36 + count * 2); writer.Write("WAVEfmt "u8);
        writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(count * 2);
        var noise = new Random(0x91E5 + (int)cue * 73);
        for (int i = 0; i < count; i++)
        {
            double time = i / (double)rate;
            double sample = cue switch
            {
                NineLivesCue.Spin =>
                    (.52 * Math.Sin(2 * Math.PI * (73 + 92 * time) * time)
                     + .24 * (noise.NextDouble() * 2 - 1)
                     + .18 * Math.Sin(2 * Math.PI * 34 * time))
                     * Math.Exp(-time * 2.9),
                NineLivesCue.ReelStop =>
                    (.65 * Math.Sin(2 * Math.PI * (170 - 86 * time) * time)
                     + .19 * (noise.NextDouble() * 2 - 1))
                     * Math.Exp(-time * 20),
                NineLivesCue.Pounce =>
                    (.38 * Math.Sin(2 * Math.PI * (420 + 520 * time) * time)
                     + .14 * (noise.NextDouble() * 2 - 1))
                     * Math.Exp(-time * 8),
                _ => 0,
            };
            for (int note = 0; note < melody.Length; note++)
            {
                double gap = cue is NineLivesCue.Frenzy or NineLivesCue.Bonus ? .19 : .15;
                double local = time - note * gap;
                if (local < 0) continue;
                double attack = Math.Min(1, local / .004);
                double envelope = attack * Math.Exp(-local * (cue == NineLivesCue.Frenzy ? 4.2 : 5.8));
                // Glass-like partials and a warm mallet fundamental, distinct from slot fanfares.
                double phase = 2 * Math.PI * melody[note] * local;
                sample += (.55 * Math.Sin(phase) + .23 * Math.Sin(phase * 2.01)
                    + .12 * Math.Sin(phase * 3.97)) * envelope;
            }
            double attackRamp = Math.Min(1, time / .003);
            double tail = Math.Clamp((duration - time) / .16, 0, 1);
            writer.Write((short)(Math.Clamp(sample * .18 * attackRamp * tail, -.45, .45) * short.MaxValue));
        }
        return stream.ToArray();
    }
}
