namespace BlackjackCasino.Desktop;

internal enum Soundtrack { Blackjack, Reels, Manor }

/// <summary>Original sixteen-bar compositions; notes and room tails wrap on the sample grid.</summary>
internal static class CasinoMusicSynthesis
{
    internal static int Tempo(Soundtrack track) => track switch { Soundtrack.Blackjack => 75, Soundtrack.Reels => 96, _ => 70 };
    internal static AudioClip Build(Soundtrack track)
    {
        double beat = 60d / Tempo(track);
        int frames = (int)Math.Round(64 * beat * AudioClip.Rate);
        var song = new float[frames * 2];
        double[][] chords = track switch
        {
            Soundtrack.Blackjack => [[50,57,60,64,69], [43,53,57,59,64], [48,55,59,62,67], [45,55,61,64,70]],
            Soundtrack.Reels => [[53,57,60,64], [50,57,60,65], [43,53,57,62], [48,58,62,64,69]],
            _ => [[50,57,62,64,65], [46,53,57,62,65], [43,50,57,58,62], [45,52,58,61,64]],
        };
        int[] roots = track == Soundtrack.Reels ? [41,38,43,36] : track == Soundtrack.Manor ? [38,34,31,33] : [38,31,36,33];
        void Add(double start, double length, Func<double,double> voice, double gain, double pan = 0)
        {
            int at = (int)Math.Round(start * AudioClip.Rate), count = (int)(length * AudioClip.Rate);
            for (int i = 0; i < count; i++)
            {
                double t = i / (double)AudioClip.Rate;
                double release = Math.Min(1, (count - 1 - i) / (AudioClip.Rate * 0.035));
                double sample = voice(t) * gain * Math.Max(0, release);
                int index = ((at + i) % frames + frames) % frames * 2;
                song[index] += (float)(sample * (1 - pan * 0.55));
                song[index + 1] += (float)(sample * (1 + pan * 0.55));
            }
        }
        void Note(double start, double midi, double length, int instrument, double gain, double pan = 0)
        {
            double f = 440 * Math.Pow(2, (midi - 69) / 12);
            Add(start, length, t => Instrument(f, t, instrument), gain, pan);
        }
        for (int bar = 0; bar < 16; bar++)
        {
            int harmony = bar / 2 % 4;
            double time = bar * 4 * beat;
            double[] chord = chords[harmony];
            for (int n = 0; n < chord.Length; n++)
            {
                if (track == Soundtrack.Manor)
                    Note(time + n * 0.035, chord[n], beat * 6, 3, 0.033, (n - 2) * 0.25);
                else
                {
                    int instrument = track == Soundtrack.Blackjack ? 0 : 1;
                    Note(time + 0.015 + n * 0.009, chord[n] + 12, beat * 2.7, instrument, 0.037, (n - 2) * 0.28);
                    Note(time + beat * (track == Soundtrack.Blackjack ? 2.65 : 2.5) + n * 0.01, chord[n] + 12, beat * 1.8, instrument, 0.023, (2 - n) * 0.24);
                }
            }
            if (track == Soundtrack.Blackjack)
            {
                foreach (double step in bar % 4 == 3 ? new[] {0d, 1.75, 2.5, 3.35} : new[] {0d, 1.65, 2.5})
                    Add(time + step * beat, 0.36, Kick, step == 0 ? 0.19 : 0.135);
                foreach (double step in new[] {1.045, 3.045}) Add(time + step * beat, 0.24, t => Snare(t, bar), 0.11, 0.06);
                for (int eighth = 0; eighth < 8; eighth++)
                    Add(time + (eighth * 0.5 + (eighth % 2 == 1 ? 0.065 : 0)) * beat, 0.095,
                        t => Hat(t, bar * 8 + eighth), eighth % 2 == 0 ? 0.029 : 0.021, -0.33);
                Note(time + beat * 0.025, roots[harmony], beat * 1.55, 2, 0.15);
                Note(time + beat * 2.56, roots[harmony] + (bar % 4 == 3 ? 7 : 0), beat * 1.15, 2, 0.12);
                if (bar % 2 == 1)
                    foreach (var (step, note) in new[] {(0.6, chord[3] + 24), (1.25, chord[2] + 24), (3.1, chord[1] + 24)})
                        Note(time + step * beat, note, beat * 1.2, 0, 0.023, 0.28);
            }
            else if (track == Soundtrack.Reels)
            {
                // Lounge quartet: walking upright, brushed backbeat, ride and vibraphone.
                for (int step = 0; step < 4; step++)
                {
                    int interval = new[] {0, harmony == 0 ? 4 : 3, 7, bar % 2 == 0 ? 9 : 10}[step];
                    Note(time + step * beat, roots[harmony] + interval, beat * 0.88, 4, 0.11);
                    Add(time + step * beat, 0.12, t => Hat(t, bar * 4 + step), 0.023, 0.4);
                }
                foreach (double step in new[] {1d, 3d}) Add(time + step * beat, 0.22, t => Snare(t, bar + 71), 0.047, -0.18);
                Add(time, 0.3, Kick, 0.055);
                int[] motif = bar % 2 == 0 ? [0, 2, 3, 1] : [3, 2, 1, 2];
                for (int note = 0; note < motif.Length; note++)
                    Note(time + new[] {0.5, 1.5, 2.75, 3.5}[note] * beat, chord[motif[note]] + 24, beat * 1.4, 5, 0.038, -0.28);
            }
            else
            {
                // Calm minor chamber score: celesta, felt plucks, slow string/organ beds.
                Note(time, roots[harmony], beat * 4.6, 3, 0.055);
                for (int note = 0; note < 4; note++)
                {
                    int index = (note + bar / 4) % chord.Length;
                    Note(time + (note + 0.18) * beat, chord[index] + 24, beat * 2.6, 5, 0.029, Math.Sin(note * 2.1) * 0.5);
                    if (note % 2 == 0) Note(time + (note + 0.66) * beat, chord[(index + 1) % chord.Length] + 12, beat * 1.8, 0, 0.017, -0.3);
                }
                foreach (double step in new[] {0d, 2d}) Add(time + step * beat, 0.25,
                    t => Math.Sin(2 * Math.PI * 83 * t) * Math.Min(1, t / 0.012) * Math.Exp(-t / 0.065), 0.021);
            }
        }
        // Circular feed-forward reflections carry the last bar into bar one, without a fade gap.
        float[] dry = (float[])song.Clone();
        if (track == Soundtrack.Blackjack)
        {
            // Darkened tape-like top end. Warm the filter through a whole circular
            // pass first, so the EQ state itself cannot click at the loop boundary.
            for (int channel = 0; channel < 2; channel++)
            {
                double low = 0;
                for (int i = 0; i < frames * 2; i++)
                {
                    int at = i % frames * 2 + channel;
                    low += 0.37 * (dry[at] - low);
                    if (i >= frames) song[at] = (float)low;
                }
            }
            song.CopyTo(dry,0);
        }
        foreach (var (seconds, gain) in new[] {(0.137, 0.13f), (0.281, 0.085f), (0.431, 0.045f)})
        {
            int delay = (int)(seconds * AudioClip.Rate);
            for (int i = 0; i < frames; i++)
            {
                int source = (i - delay + frames) % frames * 2;
                song[i * 2] += dry[source + 1] * gain; song[i * 2 + 1] += dry[source] * gain;
            }
        }
        double master = Math.Min(1.8, 0.58 / Math.Max(0.001, song.Max(s => Math.Abs(s))));
        for (int i = 0; i < song.Length; i++) song[i] = (float)(Math.Tanh(song[i] * master * 1.1) * 0.85);
        return new(song);
    }
    private static double Instrument(double f, double t, int kind)
    {
        double p = 2 * Math.PI * f * t;
        return kind switch
        {
            0 => (Math.Sin(p + 0.32 * Math.Sin(2 * p) * Math.Exp(-t * 3)) + 0.1 * Math.Sin(2 * p)) * Math.Min(1, t / 0.012) * Math.Exp(-t / 1.05),
            1 => (Math.Sin(p) + 0.21 * Math.Sin(2 * p) + 0.1 * Math.Sin(3 * p)) * Math.Min(1, t / 0.007) * Math.Exp(-t / 0.65),
            2 => (Math.Sin(p) + 0.12 * Math.Sin(2 * p)) * Math.Min(1, t / 0.025) * Math.Exp(-t / 0.55),
            3 => (Math.Sin(p) + 0.16 * Math.Sin(p * 1.002) + 0.12 * Math.Sin(3 * p)) * Math.Min(1, t / 0.65) * Math.Exp(-t / 2.4),
            4 => (Math.Sin(p) + 0.23 * Math.Sin(2 * p) + 0.065 * Math.Sin(3 * p)) * Math.Min(1, t / 0.009) * Math.Exp(-t / 0.24),
            _ => (Math.Sin(p) + 0.25 * Math.Sin(2 * p) * Math.Exp(-t / 0.2) + 0.06 * Math.Sin(3 * p)) * Math.Min(1, t / 0.008) * Math.Exp(-t / 0.85),
        };
    }
    private static double Kick(double t) => Math.Sin(2 * Math.PI * (43 * t + 2.7 * (1 - Math.Exp(-t / 0.045)))) * Math.Min(1, t / 0.003) * Math.Exp(-t / 0.095);
    private static double Snare(double t, int seed) => (Noise((int)(t * AudioClip.Rate) + seed * 907) * 0.65 + Math.Sin(2 * Math.PI * 185 * t) * 0.3) * Math.Min(1, t / 0.003) * Math.Exp(-t / 0.045);
    private static double Hat(double t, int seed)
    {
        int i = (int)(t * AudioClip.Rate) + seed * 513;
        return (Noise(i) - Noise(i - 1)) * 0.5 * Math.Min(1, t / 0.001) * Math.Exp(-t / 0.017);
    }
    internal static double Noise(int i)
    {
        uint x = unchecked((uint)i * 747796405u + 2891336453u);
        x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u; x = (x >> 22) ^ x;
        return x / (double)uint.MaxValue * 2 - 1;
    }
}
