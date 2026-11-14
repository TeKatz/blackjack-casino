namespace BlackjackCasino.Desktop;

/// <summary>Original 68 BPM lounge score, rendered once off the UI thread and cached by BackgroundMusic.</summary>
internal static class DiceLoungeSynthesis
{
    internal static AudioClip Build()
    {
        const double beat = 60d / 68;
        int frames = (int)Math.Round(64 * beat * AudioClip.Rate);
        var samples = new float[frames * 2];
        int[][] chords = [[57, 60, 64, 67, 71], [53, 57, 60, 64, 67], [55, 60, 64, 67, 74], [55, 59, 62, 64, 69]];
        int[] roots = [33, 29, 36, 31];
        int[][] melody = [[76, 71, 72], [72, 69, 67], [74, 76, 71], [71, 69, 67]];
        for (int bar = 0; bar < 16; bar++)
        {
            double start = bar * 4 * beat;
            int harmony = bar / 2 % 4;
            foreach (int note in chords[harmony])
                Tone(note, start + .09 * beat + (note % 3) * .012, 3.6 * beat, .075, note % 2 == 0 ? -.3 : .3, 0);
            if (bar % 2 == 1)
                foreach (int note in chords[harmony].Skip(1)) Tone(note, start + 2.7 * beat, 1.9 * beat, .04, -.2, 0);
            Tone(roots[harmony], start, 2.6 * beat, .13, 0, 1);
            Tone(roots[harmony] + 7, start + 2.65 * beat, 1.1 * beat, .07, 0, 1);
            if (bar % 2 == 1)
                for (int n = 0; n < 3; n++) Tone(melody[harmony][n], start + (.6 + n * 1.05) * beat, 1.7 * beat, .033, .4, 2);
            for (int half = 0; half < 8; half++)
                Brush(start + (half * .5 + (half % 2) * .055) * beat, .028, half % 2 == 0 ? .006 : .004);
            Brush(start + 1.04 * beat, .11, .018);
            Brush(start + 3.04 * beat, .11, .014);
            Kick(start, .035);
            Kick(start + 2.5 * beat, .022);
        }
        // Circular room reflections preserve the musical bar boundary and warm tails.
        float[] dry = (float[])samples.Clone();
        int echo1 = (int)(beat * .75 * AudioClip.Rate), echo2 = (int)(beat * 1.5 * AudioClip.Rate);
        double energy = 0, peak = 0;
        for (int i = 0; i < frames; i++)
        {
            for (int channel = 0; channel < 2; channel++)
            {
                int offset = i * 2 + channel;
                double value = dry[offset] + dry[((i - echo1 + frames) % frames) * 2 + 1 - channel] * .18
                    + dry[((i - echo2 + frames) % frames) * 2 + channel] * .09;
                samples[offset] = (float)Math.Tanh(value * .85);
                energy += samples[offset] * samples[offset];
                peak = Math.Max(peak, Math.Abs(samples[offset]));
            }
        }
        double gain = Math.Min(.45 / Math.Max(.001, peak), .065 / Math.Max(.001, Math.Sqrt(energy / samples.Length)));
        for (int i = 0; i < samples.Length; i++) samples[i] *= (float)gain;
        for (int channel = 0; channel < 2; channel++)
        {
            float difference = samples[(frames - 1) * 2 + channel] - samples[channel];
            for (int n = 0; n < 128; n++) samples[(frames - 128 + n) * 2 + channel] -= difference * n / 127f;
        }
        return new(samples);

        void Tone(int midi, double start, double duration, double gain, double pan, int instrument)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12d);
            int count = (int)(duration * AudioClip.Rate), first = (int)(start * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double phase = 2 * Math.PI * frequency * t;
                double attack = Math.Min(1, t / (instrument == 1 ? .025 : .012));
                double release = Math.Min(1, (duration - t) / .15);
                double value = instrument switch
                {
                    1 => (Math.Sin(phase) + .16 * Math.Sin(2 * phase)) * Math.Exp(-t / 1.3),
                    2 => (Math.Sin(phase) + .18 * Math.Sin(phase * 2) * Math.Exp(-t * 3)
                        + .07 * Math.Sin(phase * 3)) * Math.Exp(-t / .9),
                    _ => (Math.Sin(phase) + .22 * Math.Sin(phase * 2) * Math.Exp(-t * 2)
                        + .09 * Math.Sin(phase * 3) * Math.Exp(-t * 3)
                        + .06 * Math.Sin(phase * 1.002)) * Math.Exp(-t / 1.8),
                };
                Add(first + n, value * attack * release * gain, pan);
            }
        }
        void Brush(double start, double duration, double gain)
        {
            int first = (int)(start * AudioClip.Rate), count = (int)(duration * AudioClip.Rate);
            double low = 0;
            for (int n = 0; n < count; n++)
            {
                double noise = CasinoMusicSynthesis.Noise(first + n + 18337);
                low = .6 * low + .4 * noise;
                double t = n / (double)AudioClip.Rate;
                Add(first + n, low * Math.Min(1, t / .004) * Math.Exp(-t / (duration / 4)) * gain, -.25);
            }
        }
        void Kick(double start, double gain)
        {
            int first = (int)(start * AudioClip.Rate);
            for (int n = 0; n < (int)(.3 * AudioClip.Rate); n++)
            {
                double t = n / (double)AudioClip.Rate;
                Add(first + n, Math.Sin(2 * Math.PI * (48 * t + 1.3 * (1 - Math.Exp(-t * 28))))
                    * Math.Min(1, t / .006) * Math.Exp(-t * 22) * gain, 0);
            }
        }
        void Add(int frame, double value, double pan)
        {
            frame %= frames;
            samples[frame * 2] += (float)(value * (1 - pan * .45));
            samples[frame * 2 + 1] += (float)(value * (1 + pan * .45));
        }
    }
}
