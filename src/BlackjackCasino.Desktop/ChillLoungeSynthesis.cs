namespace BlackjackCasino.Desktop;

/// <summary>
/// Original slow LoFi loop for the Chill Lounge: soft electric-piano harmony, warm bass,
/// brushed drums and a very light vinyl/tape bed. Rendered once off the UI thread and cached.
/// </summary>
internal static class ChillLoungeSynthesis
{
    internal static AudioClip Build()
    {
        const double bpm = 60d;
        const int bars = 16;
        double beat = 60d / bpm;
        int frames = (int)Math.Round(bars * 4 * beat * AudioClip.Rate);
        var samples = new float[frames * 2];

        int[][] chords =
        [
            [60, 64, 67, 71, 74], // Cmaj9
            [57, 60, 64, 67, 71], // Am9
            [53, 57, 60, 64, 67], // Fmaj9
            [55, 59, 62, 64, 69], // G6/9
        ];
        int[] roots = [36, 33, 29, 31];
        int[][] phrases =
        [
            [76, 74, 71],
            [72, 71, 67],
            [69, 67, 64],
            [71, 69, 67],
        ];

        for (int bar = 0; bar < bars; bar++)
        {
            double start = bar * 4 * beat;
            int harmony = bar % 4;

            // Wide, slightly late electric-piano voicings give the loop its relaxed pocket.
            foreach (int note in chords[harmony])
            {
                double spread = (note % 4 - 1.5) * 0.012;
                Rhodes(note, start + 0.075 * beat + Math.Max(0, spread), 3.55 * beat, 0.064, spread * 16);
            }
            if (bar % 2 == 1)
            {
                foreach (int note in chords[harmony].Skip(1))
                {
                    Rhodes(note, start + 2.72 * beat, 1.18 * beat, 0.026, note % 2 == 0 ? -0.24 : 0.24);
                }
            }

            Bass(roots[harmony], start + 0.03 * beat, 1.75 * beat, 0.115);
            Bass(roots[harmony] + 7, start + 2.52 * beat, 1.18 * beat, 0.066);

            if (bar is 3 or 7 or 11 or 15)
            {
                for (int note = 0; note < phrases[harmony].Length; note++)
                {
                    Rhodes(phrases[harmony][note], start + (0.72 + note * 0.94) * beat, 1.28 * beat, 0.024, 0.34);
                }
            }

            Kick(start + 0.02 * beat, 0.031);
            Kick(start + 2.52 * beat, 0.021);
            Rim(start + 1.03 * beat, 0.013);
            Rim(start + 3.03 * beat, 0.011);

            for (int half = 0; half < 8; half++)
            {
                double swing = half % 2 == 1 ? 0.055 : 0;
                Brush(start + (half * 0.5 + swing) * beat, 0.052, half % 2 == 0 ? 0.0037 : 0.0052);
            }
        }

        // Quiet vinyl/tape texture. It is intentionally low enough to disappear under the music.
        double tape = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            double noise = CasinoMusicSynthesis.Noise(frame + 9137);
            tape = tape * 0.992 + noise * 0.008;
            double crackle = Math.Abs(CasinoMusicSynthesis.Noise(frame * 17 + 271)) > 0.9975
                ? CasinoMusicSynthesis.Noise(frame * 31 + 97) * 0.0024
                : 0;
            double bed = tape * 0.0022 + noise * 0.00055 + crackle;
            samples[frame * 2] += (float)(bed * 0.94);
            samples[frame * 2 + 1] += (float)(bed * 1.06);
        }

        // Circular echoes keep the tail continuous at the loop seam.
        float[] dry = (float[])samples.Clone();
        int echoA = (int)(0.75 * beat * AudioClip.Rate);
        int echoB = (int)(1.5 * beat * AudioClip.Rate);
        double energy = 0;
        double peak = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            for (int channel = 0; channel < 2; channel++)
            {
                int index = frame * 2 + channel;
                double value = dry[index]
                    + dry[((frame - echoA + frames) % frames) * 2 + 1 - channel] * 0.105
                    + dry[((frame - echoB + frames) % frames) * 2 + channel] * 0.055;
                samples[index] = (float)Math.Tanh(value * 0.88);
                energy += samples[index] * samples[index];
                peak = Math.Max(peak, Math.Abs(samples[index]));
            }
        }

        double rms = Math.Sqrt(energy / samples.Length);
        double gain = Math.Min(0.42 / Math.Max(0.001, peak), 0.055 / Math.Max(0.001, rms));
        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] *= (float)gain;
        }

        // Tiny seam correction prevents a click without changing the musical boundary.
        for (int channel = 0; channel < 2; channel++)
        {
            float difference = samples[(frames - 1) * 2 + channel] - samples[channel];
            for (int n = 0; n < 160; n++)
            {
                samples[(frames - 160 + n) * 2 + channel] -= difference * n / 159f;
            }
        }

        return new AudioClip(samples);

        void Rhodes(int midi, double start, double duration, double level, double pan)
        {
            double baseFrequency = 440 * Math.Pow(2, (midi - 69) / 12d);
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(duration * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double absolute = start + t;
                double wow = 1 + 0.00125 * Math.Sin(2 * Math.PI * 0.31 * absolute)
                    + 0.00055 * Math.Sin(2 * Math.PI * 0.73 * absolute);
                double phase = 2 * Math.PI * baseFrequency * wow * t;
                double attack = Math.Min(1, t / 0.022);
                double release = Math.Min(1, (duration - t) / 0.28);
                double body = Math.Sin(phase)
                    + 0.19 * Math.Sin(phase * 2.001) * Math.Exp(-t * 1.45)
                    + 0.075 * Math.Sin(phase * 3) * Math.Exp(-t * 2.5)
                    + 0.045 * Math.Sin(phase * 0.501);
                Add(first + n, body * Math.Exp(-t / 2.55) * attack * release * level, pan);
            }
        }

        void Bass(int midi, double start, double duration, double level)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12d);
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(duration * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double phase = 2 * Math.PI * frequency * t;
                double attack = Math.Min(1, t / 0.035);
                double release = Math.Min(1, (duration - t) / 0.19);
                double body = Math.Sin(phase) + 0.115 * Math.Sin(2 * phase) * Math.Exp(-t * 3.4);
                Add(first + n, body * Math.Exp(-t / 1.18) * attack * release * level, -0.04);
            }
        }

        void Kick(double start, double level)
        {
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(0.34 * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double phase = 2 * Math.PI * (45 * t + 1.55 * (1 - Math.Exp(-t * 24)));
                Add(first + n, Math.Sin(phase) * Math.Min(1, t / 0.007) * Math.Exp(-t * 18) * level, 0);
            }
        }

        void Rim(double start, double level)
        {
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(0.12 * AudioClip.Rate);
            double low = 0;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double noise = CasinoMusicSynthesis.Noise(first * 3 + n + 4171);
                low = low * 0.46 + noise * 0.54;
                double click = Math.Sin(2 * Math.PI * 980 * t) * 0.32 + low * 0.68;
                Add(first + n, click * Math.Exp(-t * 39) * level, 0.18);
            }
        }

        void Brush(double start, double duration, double level)
        {
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(duration * AudioClip.Rate);
            double low = 0;
            for (int n = 0; n < count; n++)
            {
                double noise = CasinoMusicSynthesis.Noise(first + n + 23471);
                low = low * 0.7 + noise * 0.3;
                double t = n / (double)AudioClip.Rate;
                Add(first + n, low * Math.Min(1, t / 0.005) * Math.Exp(-t / Math.Max(0.008, duration / 2.8)) * level, -0.28);
            }
        }

        void Add(int frame, double value, double pan)
        {
            frame %= frames;
            if (frame < 0)
            {
                frame += frames;
            }
            samples[frame * 2] += (float)(value * (1 - pan * 0.42));
            samples[frame * 2 + 1] += (float)(value * (1 + pan * 0.42));
        }
    }
}
