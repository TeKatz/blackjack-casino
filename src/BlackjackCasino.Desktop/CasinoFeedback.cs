using System.Media;

namespace BlackjackCasino.Desktop;

internal static class CasinoFeedback
{
    private sealed class SoundCue : IDisposable
    {
        private readonly MemoryStream stream;

        public SoundCue(byte[] wav)
        {
            stream = new MemoryStream(wav, writable: false);
            Player = new SoundPlayer(stream);
            Player.Load();
        }

        public SoundPlayer Player { get; }

        public void Dispose()
        {
            Player.Dispose();
            stream.Dispose();
        }
    }

    private static readonly Lazy<SoundCue> CardCue = new(() => new SoundCue(BuildCardCue()));
    private static readonly Lazy<SoundCue> ChipCue = new(() => new SoundCue(BuildChipCue()));
    private static readonly Lazy<SoundCue> WinCue = new(() => new SoundCue(BuildWinCue()));
    private static readonly Lazy<SoundCue> LossCue = new(() => new SoundCue(BuildLossCue()));
    private static readonly Lazy<SoundCue> PushCue = new(() => new SoundCue(BuildPushCue()));

    public static void PlayChip(bool enabled) => Play(enabled, ChipCue);

    public static void PlayCard(bool enabled) => Play(enabled, CardCue);

    public static void PlayOutcome(bool enabled, decimal net) => Play(enabled, net switch
    {
        > 0m => WinCue,
        < 0m => LossCue,
        _ => PushCue,
    });

    private static void Play(bool enabled, Lazy<SoundCue> cue)
    {
        if (!enabled)
        {
            return;
        }

        try
        {
            cue.Value.Player.Play();
        }
        catch
        {
            // Audio feedback is cosmetic and must never interrupt play.
        }
    }

    private static byte[] BuildCardCue()
    {
        const double duration = 0.11;
        var random = new Random(21421);
        double lowPassedNoise = 0;

        return BuildWave(duration, t =>
        {
            double noise = (random.NextDouble() * 2d) - 1d;
            lowPassedNoise = (0.12d * noise) + (0.88d * lowPassedNoise);
            double felt = 0.34d * lowPassedNoise * Envelope(t, 0.002d, 0.045d);
            double snap = 0.08d * Math.Sin(2d * Math.PI * 980d * t) * Envelope(t, 0.001d, 0.018d);
            return felt + snap;
        });
    }

    private static byte[] BuildChipCue() => BuildWave(0.15d, t =>
        (0.22d * Math.Sin(2d * Math.PI * 2300d * t)
        + 0.12d * Math.Sin(2d * Math.PI * 3150d * t)
        + 0.07d * Math.Sin(2d * Math.PI * 4700d * t))
        * Envelope(t, 0.001d, 0.035d));

    private static byte[] BuildWinCue() => BuildToneSequence(
        0.42d,
        [(659.25d, 0.00d, 0.17d), (783.99d, 0.08d, 0.17d), (987.77d, 0.16d, 0.17d), (1318.51d, 0.24d, 0.17d)],
        amplitude: 0.17d,
        decay: 0.09d);

    private static byte[] BuildLossCue() => BuildToneSequence(
        0.36d,
        [(392d, 0.00d, 0.18d), (329.63d, 0.10d, 0.18d), (261.63d, 0.20d, 0.16d)],
        amplitude: 0.13d,
        decay: 0.10d);

    private static byte[] BuildPushCue() => BuildToneSequence(
        0.24d,
        [(523.25d, 0.00d, 0.11d), (523.25d, 0.11d, 0.11d)],
        amplitude: 0.12d,
        decay: 0.055d);

    private static byte[] BuildToneSequence(
        double duration,
        IReadOnlyList<(double Frequency, double Start, double Length)> tones,
        double amplitude,
        double decay)
    {
        return BuildWave(duration, t =>
        {
            double sample = 0;
            foreach ((double frequency, double start, double length) in tones)
            {
                double local = t - start;
                if (local < 0 || local >= length)
                {
                    continue;
                }

                sample += amplitude * Math.Sin(2d * Math.PI * frequency * local) * Envelope(local, 0.004d, decay);
            }

            return sample;
        });
    }

    private static byte[] BuildWave(double durationSeconds, Func<double, double> sampleFactory)
    {
        const int sampleRate = 44_100;
        const short channels = 1;
        const short bitsPerSample = 16;
        int sampleCount = (int)Math.Ceiling(durationSeconds * sampleRate);
        int dataSize = sampleCount * channels * (bitsPerSample / 8);

        using var stream = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(stream);

        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * (bitsPerSample / 8));
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)sampleRate;
            double sample = Math.Clamp(sampleFactory(t), -1d, 1d);
            writer.Write((short)Math.Round(sample * short.MaxValue));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static double Envelope(double t, double attack, double decay)
    {
        double attackGain = Math.Min(1d, t / Math.Max(attack, 0.0001d));
        return attackGain * Math.Exp(-t / decay);
    }
}
