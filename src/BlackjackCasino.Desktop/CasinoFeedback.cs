using System.Media;

namespace BlackjackCasino.Desktop;

internal static class CasinoFeedback
{
    private const double CueMasterGain = 0.24d;

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
        const double duration = 0.13d;
        var random = new Random(21421);
        double lowPassedNoise = 0d;

        return BuildWave(duration, t =>
        {
            double noise = (random.NextDouble() * 2d) - 1d;
            lowPassedNoise = (0.075d * noise) + (0.925d * lowPassedNoise);
            double felt = 0.18d * lowPassedNoise * Envelope(t, 0.003d, 0.055d);
            double paper = 0.018d * Math.Sin(2d * Math.PI * 620d * t) * Envelope(t, 0.002d, 0.025d);
            return felt + paper;
        });
    }

    private static byte[] BuildChipCue() => BuildWave(0.16d, t =>
        (0.095d * Math.Sin(2d * Math.PI * 1900d * t)
        + 0.045d * Math.Sin(2d * Math.PI * 2750d * t)
        + 0.018d * Math.Sin(2d * Math.PI * 3900d * t))
        * Envelope(t, 0.0015d, 0.043d));

    private static byte[] BuildWinCue() => BuildToneSequence(
        0.44d,
        [(523.25d, 0.00d, 0.19d), (659.25d, 0.10d, 0.19d), (783.99d, 0.20d, 0.18d)],
        amplitude: 0.095d,
        decay: 0.11d);

    private static byte[] BuildLossCue() => BuildToneSequence(
        0.38d,
        [(349.23d, 0.00d, 0.19d), (293.66d, 0.12d, 0.18d), (246.94d, 0.24d, 0.13d)],
        amplitude: 0.075d,
        decay: 0.12d);

    private static byte[] BuildPushCue() => BuildToneSequence(
        0.25d,
        [(440d, 0.00d, 0.10d), (440d, 0.12d, 0.10d)],
        amplitude: 0.065d,
        decay: 0.065d);

    private static byte[] BuildToneSequence(
        double duration,
        IReadOnlyList<(double Frequency, double Start, double Length)> tones,
        double amplitude,
        double decay)
    {
        return BuildWave(duration, t =>
        {
            double sample = 0d;
            foreach ((double frequency, double start, double length) in tones)
            {
                double local = t - start;
                if (local < 0d || local >= length)
                {
                    continue;
                }

                sample += amplitude * Math.Sin(2d * Math.PI * frequency * local) * Envelope(local, 0.005d, decay);
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
            double sample = Math.Clamp(sampleFactory(t) * CueMasterGain, -1d, 1d);
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
