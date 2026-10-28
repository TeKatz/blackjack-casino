using System.Windows.Media;

namespace BlackjackCasino.Desktop;

internal sealed class BackgroundMusic : IDisposable
{
    private const int SampleRate = 32_000;
    private const double BeatsPerMinute = 64d;
    private const double BeatSeconds = 60d / BeatsPerMinute;
    private const double BarSeconds = BeatSeconds * 4d;
    private const int Bars = 12;
    private const string TrackFileName = "blackjack-lofi-v2.wav";

    private static readonly double[][] Chords =
    [
        [146.83d, 220.00d, 261.63d, 329.63d, 349.23d], // Dm9
        [130.81d, 196.00d, 233.08d, 293.66d, 329.63d], // Cmaj9-ish modal voicing
        [146.83d, 196.00d, 233.08d, 293.66d, 349.23d], // Gm9/D
        [110.00d, 164.81d, 220.00d, 246.94d, 311.13d], // A7sus/b9 colour
    ];

    private static readonly double[] BassRoots = [73.42d, 65.41d, 73.42d, 55.00d];

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
        player.Volume = Math.Clamp(value, 0d, 0.32d);
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
        double roomStateLeft = 0d;
        double roomStateRight = 0d;

        for (int i = 0; i < sampleCount; i++)
        {
            double t = i / (double)SampleRate;
            int bar = Math.Min(Bars - 1, (int)(t / BarSeconds));
            int phrase = (bar / 3) % Chords.Length;
            double localBar = t - (bar * BarSeconds);
            double localPhrase = t - ((bar / 3) * 3d * BarSeconds);
            double beatPosition = localBar / BeatSeconds;
            double beatFraction = beatPosition - Math.Floor(beatPosition);
            int beat = Math.Min(3, (int)Math.Floor(beatPosition));

            (double left, double right) = DustyKeys(Chords[phrase], t, localPhrase);
            double bass = Bass(BassRoots[phrase], t, beatPosition, bar);
            double kick = Kick(beat, beatFraction, bar);
            double brush = BrushSnare(i, beat, beatFraction, bar);
            double hat = HiHat(i, beatPosition, bar);

            double rawNoise = PseudoNoise(i + 41_791);
            vinylState = (0.018d * rawNoise) + (0.982d * vinylState);
            double vinyl = (0.005d * rawNoise) + (0.010d * vinylState);

            double inputLeft = left + bass + kick + brush + hat + vinyl;
            double inputRight = right + bass + (kick * 0.96d) + (brush * 1.04d) + (hat * 0.88d) + vinyl;

            roomStateLeft = (0.015d * inputLeft) + (0.985d * roomStateLeft);
            roomStateRight = (0.015d * inputRight) + (0.985d * roomStateRight);

            left = (inputLeft + (roomStateRight * 0.22d)) * 0.52d;
            right = (inputRight + (roomStateLeft * 0.22d)) * 0.52d;

            writer.Write(ToPcm(left));
            writer.Write(ToPcm(right));
        }

        writer.Flush();
        return stream.ToArray();
    }

    private static (double Left, double Right) DustyKeys(IReadOnlyList<double> chord, double t, double localPhrase)
    {
        double envelope = AttackDecay(localPhrase, 0.09d, 5.2d);
        double left = 0d;
        double right = 0d;

        for (int note = 0; note < chord.Count; note++)
        {
            double frequency = chord[note];
            double drift = 1d + (0.0014d * Math.Sin((2d * Math.PI * (0.07d + (note * 0.011d)) * t) + note));
            double phase = 2d * Math.PI * frequency * drift * t;
            double voice = Math.Sin(phase)
                + (0.11d * Math.Sin((2d * phase) + 0.6d));
            voice *= 0.031d * envelope;

            double pan = ((note / (double)Math.Max(1, chord.Count - 1)) * 0.34d) - 0.17d;
            left += voice * (1d - pan);
            right += voice * (1d + pan);
        }

        return (left, right);
    }

    private static double Bass(double root, double t, double beatPosition, int bar)
    {
        if ((bar % 2) == 1)
        {
            return 0d;
        }

        double twoBeatPosition = beatPosition % 2d;
        if (twoBeatPosition > 0.72d)
        {
            return 0d;
        }

        double envelope = AttackDecay(twoBeatPosition * BeatSeconds, 0.03d, 0.52d);
        double phase = 2d * Math.PI * root * t;
        return (0.058d * Math.Sin(phase) + 0.011d * Math.Sin(2d * phase)) * envelope;
    }

    private static double Kick(int beat, double beatFraction, int bar)
    {
        bool active = beat == 0 || (beat == 2 && (bar % 2) == 0);
        if (!active || beatFraction > 0.22d)
        {
            return 0d;
        }

        double local = beatFraction * BeatSeconds;
        double frequency = 61d - (20d * Math.Min(1d, local / 0.16d));
        return 0.068d * Math.Sin(2d * Math.PI * frequency * local) * Math.Exp(-local / 0.14d);
    }

    private static double BrushSnare(int sampleIndex, int beat, double beatFraction, int bar)
    {
        if (beat != 3 || beatFraction > 0.30d || (bar % 3) == 2)
        {
            return 0d;
        }

        double local = beatFraction * BeatSeconds;
        double noise = PseudoNoise((sampleIndex * 5) + 91);
        return 0.024d * noise * Math.Exp(-local / 0.12d);
    }

    private static double HiHat(int sampleIndex, double beatPosition, int bar)
    {
        double eighthPosition = (beatPosition * 2d) % 1d;
        if (eighthPosition > 0.10d || ((int)Math.Floor(beatPosition * 2d) + bar) % 3 == 0)
        {
            return 0d;
        }

        double local = eighthPosition * (BeatSeconds / 2d);
        double noise = PseudoNoise((sampleIndex * 7) + 173);
        return 0.0075d * noise * Math.Exp(-local / 0.032d);
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
