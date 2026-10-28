using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class BackgroundMusic : IDisposable
{
    private const int SampleRate = 32_000;
    private const double BeatsPerMinute = 72d;
    private const double BeatSeconds = 60d / BeatsPerMinute;
    private const double BarSeconds = BeatSeconds * 4d;
    private const int Bars = 8;
    private const string TrackFileName = "blackjack-lofi-v1.wav";

    private static readonly double[][] Chords =
    [
        [293.66d, 349.23d, 440.00d, 523.25d], // Dm7
        [233.08d, 293.66d, 349.23d, 440.00d], // Bbmaj7
        [261.63d, 329.63d, 392.00d, 493.88d], // Fmaj7
        [261.63d, 329.63d, 392.00d, 587.33d], // Cadd9/E voicing
    ];

    private static readonly double[] BassRoots = [73.42d, 58.27d, 87.31d, 65.41d];

    private readonly MediaPlayer player = new();
    private bool opened;
    private bool enabled;
    private bool disposed;

    public BackgroundMusic()
    {
        player.MediaEnded += (_, _) =>
        {
            if (!enabled || disposed)
            {
                return;
            }

            player.Position = TimeSpan.Zero;
            player.Play();
        };
    }

    public void Start(bool shouldPlay, double volume)
    {
        SetVolume(volume);
        enabled = shouldPlay;
        EnsureOpen();

        if (enabled)
        {
            player.Play();
        }
    }

    public void SetEnabled(bool value)
    {
        enabled = value;
        EnsureOpen();

        if (enabled)
        {
            player.Play();
        }
        else
        {
            player.Pause();
        }
    }

    public void SetVolume(double value)
    {
        player.Volume = Math.Clamp(value, 0d, 0.45d);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        player.Stop();
        player.Close();
    }

    private void EnsureOpen()
    {
        if (opened || disposed)
        {
            return;
        }

        try
        {
            string path = EnsureTrackFile();
            player.Open(new Uri(path, UriKind.Absolute));
            opened = true;
        }
        catch
        {
            // Background music is cosmetic and must never prevent the game from launching.
        }
    }

    private static string EnsureTrackFile()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlackjackCasino",
            "Audio");
        string path = Path.Combine(directory, TrackFileName);

        if (File.Exists(path) && new FileInfo(path).Length > 100_000)
        {
            return path;
        }

        Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        File.WriteAllBytes(temporary, BuildTrack());
        File.Move(temporary, path, overwrite: true);
        return path;
    }

    private static byte[] BuildTrack()
    {
        const short channels = 2;
        const short bitsPerSample = 16;
        double duration = BarSeconds * Bars;
        int sampleCount = (int)Math.Ceiling(duration * SampleRate);
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
        writer.Write(SampleRate);
        writer.Write(SampleRate * channels * (bitsPerSample / 8));
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        double vinylState = 0d;
        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)SampleRate;
            int bar = Math.Min(Bars - 1, (int)(t / BarSeconds));
            int progression = (bar / 2) % Chords.Length;
            double localBar = t - (bar * BarSeconds);
            double beatPosition = localBar / BeatSeconds;
            double beatFraction = beatPosition - Math.Floor(beatPosition);
            int beat = Math.Min(3, (int)Math.Floor(beatPosition));

            (double left, double right) = Rhodes(Chords[progression], t, localBar);
            double bass = Bass(BassRoots[progression], t, beatPosition);
            double kick = Kick(t, beat, beatFraction);
            double snare = Snare(i, beat, beatFraction);
            double hat = HiHat(i, beatPosition);

            double rawNoise = PseudoNoise(i + 41_791);
            vinylState = (0.025d * rawNoise) + (0.975d * vinylState);
            double vinyl = (0.010d * rawNoise) + (0.018d * vinylState);
            double wow = 0.985d + (0.015d * Math.Sin(2d * Math.PI * 0.23d * t));

            left = ((left * wow) + bass + kick + snare + hat + vinyl) * 0.72d;
            right = ((right * wow) + bass + kick + (snare * 0.92d) + (hat * 1.05d) + vinyl) * 0.72d;

            writer.Write(ToPcm(left));
            writer.Write(ToPcm(right));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static (double Left, double Right) Rhodes(IReadOnlyList<double> chord, double t, double localBar)
    {
        double envelope = AttackDecay(localBar, 0.035d, 2.6d);
        double tremolo = 0.95d + (0.05d * Math.Sin(2d * Math.PI * 0.42d * t));
        double left = 0d;
        double right = 0d;

        for (int note = 0; note < chord.Count; note++)
        {
            double frequency = chord[note];
            double phase = (2d * Math.PI * frequency * t) + (note * 0.31d);
            double voice = Math.Sin(phase)
                + (0.23d * Math.Sin((2d * phase) + 0.4d))
                + (0.07d * Math.Sin((3d * phase) + 0.9d));
            voice *= 0.055d * envelope * tremolo;

            double pan = ((note / (double)Math.Max(1, chord.Count - 1)) * 0.5d) - 0.25d;
            left += voice * (1d - pan);
            right += voice * (1d + pan);
        }

        return (left, right);
    }

    private static double Bass(double root, double t, double beatPosition)
    {
        double twoBeatPosition = beatPosition % 2d;
        if (twoBeatPosition > 0.92d)
        {
            return 0d;
        }

        double envelope = AttackDecay(twoBeatPosition * BeatSeconds, 0.018d, 0.44d);
        double phase = 2d * Math.PI * root * t;
        return (0.105d * Math.Sin(phase) + 0.025d * Math.Sin(2d * phase)) * envelope;
    }

    private static double Kick(double t, int beat, double beatFraction)
    {
        if (beat is not 0 and not 2 || beatFraction > 0.28d)
        {
            return 0d;
        }

        double local = beatFraction * BeatSeconds;
        double frequency = 74d - (30d * Math.Min(1d, local / 0.18d));
        return 0.15d * Math.Sin(2d * Math.PI * frequency * local) * Math.Exp(-local / 0.12d);
    }

    private static double Snare(int sampleIndex, int beat, double beatFraction)
    {
        if (beat is not 1 and not 3 || beatFraction > 0.24d)
        {
            return 0d;
        }

        double local = beatFraction * BeatSeconds;
        double noise = PseudoNoise(sampleIndex * 3 + 91);
        double body = Math.Sin(2d * Math.PI * 175d * local);
        return ((0.055d * noise) + (0.025d * body)) * Math.Exp(-local / 0.075d);
    }

    private static double HiHat(int sampleIndex, double beatPosition)
    {
        double eighthPosition = (beatPosition * 2d) % 1d;
        if (eighthPosition > 0.13d)
        {
            return 0d;
        }

        double local = eighthPosition * (BeatSeconds / 2d);
        double noise = PseudoNoise(sampleIndex * 7 + 173);
        return 0.018d * noise * Math.Exp(-local / 0.025d);
    }

    private static double AttackDecay(double time, double attack, double decay)
    {
        double attackGain = Math.Min(1d, time / Math.Max(attack, 0.0001d));
        return attackGain * Math.Exp(-time / decay);
    }

    private static double PseudoNoise(int index)
    {
        uint x = unchecked((uint)index * 747796405u + 2891336453u);
        x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u;
        x = (x >> 22) ^ x;
        return (x / (double)uint.MaxValue * 2d) - 1d;
    }

    private static short ToPcm(double sample) =>
        (short)Math.Round(Math.Clamp(sample, -1d, 1d) * short.MaxValue);
}
