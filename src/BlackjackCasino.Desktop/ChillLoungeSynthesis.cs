namespace BlackjackCasino.Desktop;

/// <summary>Original 74 BPM chill hip-hop loop: swung hats, dusty kick and snare,
/// mellow electric keys, warm sub bass and a quiet tape bed. Generated offline.</summary>
internal static class ChillLoungeSynthesis
{
    internal static AudioClip Build()
    {
        const double bpm = 74d;
        const int bars = 16;
        double beat = 60d / bpm;
        int frames = (int)Math.Round(bars * 4 * beat * AudioClip.Rate);
        var samples = new float[frames * 2];

        // Dm9 · Bbmaj9 · Fmaj9 · Cadd9/E. Lower, more suspended voicings deliberately
        // avoid the bright C-major lounge progression used by Midnight Dice.
        int[][] chords =
        [
            [50, 53, 57, 60, 64],
            [46, 50, 53, 57, 60],
            [41, 45, 48, 52, 55],
            [40, 48, 50, 55, 60],
        ];
        int[] roots = [38, 34, 29, 28];
        int[] airNotes = [69, 65, 72, 67];

        for (int bar = 0; bar < bars; bar++)
        {
            double start = bar * 4 * beat;
            int harmony = bar % 4;

            for (int voice = 0; voice < chords[harmony].Length; voice++)
            {
                double pan = (voice - 2) * 0.17;
                Pad(chords[harmony][voice] - 12, start, 4.35 * beat, 0.025, pan);
            }

            // Soft syncopated electric-key chords, alternating voicings every four bars.
            for (int voice = 1; voice < chords[harmony].Length; voice++)
            {
                Felt(chords[harmony][voice], start + 0.08 * beat, 1.65 * beat, 0.032, (voice - 2) * 0.14);
                Felt(chords[harmony][voice], start + 2.65 * beat, 1.15 * beat, 0.023, (voice - 2) * 0.14);
            }
            Felt(chords[harmony][1] + 12, start + 0.34 * beat, 1.55 * beat, 0.044, -0.22);
            Felt(chords[harmony][3] + 12, start + 2.42 * beat, 1.28 * beat, 0.041, 0.20);
            if (bar % 2 == 1)
            {
                Felt(chords[harmony][4] + 12, start + 3.18 * beat, 0.74 * beat, 0.026, 0.34);
            }

            Sub(roots[harmony], start + 0.08 * beat, 2.20 * beat, 0.075);

            if (bar % 4 == 3)
            {
                Felt(airNotes[harmony], start + 1.62 * beat, 2.08 * beat, 0.020, 0.38);
            }

            Kick(start + 0.03 * beat, 0.17);
            Kick(start + (bar % 2 == 0 ? 2.48 : 2.02) * beat, 0.125);
            if (bar % 4 == 3) Kick(start + 3.60 * beat, 0.075);
            Snare(start + 1.04 * beat, 0.055);
            Snare(start + 3.05 * beat, 0.048);
            for (int eighth = 0; eighth < 8; eighth++)
                Hat(start + (eighth * 0.5 + (eighth % 2 == 1 ? 0.095 : 0.01)) * beat,
                    eighth % 2 == 0 ? 0.018 : 0.011, eighth % 2 == 0 ? -0.20 : 0.26);
            Sub(roots[harmony], start + 2.52 * beat, 1.15 * beat, 0.044);
        }

        double tape = 0;
        double room = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            double raw = CasinoMusicSynthesis.Noise(frame + 48127);
            tape = tape * 0.9965 + raw * 0.0035;
            room = room * 0.935 + CasinoMusicSynthesis.Noise(frame * 7 + 913) * 0.065;
            double t = frame / (double)AudioClip.Rate;
            double breath = 0.72 + 0.28 * Math.Sin(2 * Math.PI * 0.047 * t);
            double bed = tape * 0.0028 + room * 0.00115 * breath;
            samples[frame * 2] += (float)(bed * 0.92);
            samples[frame * 2 + 1] += (float)(bed * 1.08);
        }

        float[] dry = (float[])samples.Clone();
        int echoA = (int)(1.73 * beat * AudioClip.Rate);
        int echoB = (int)(3.41 * beat * AudioClip.Rate);
        double peak = 0;
        double energy = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            for (int channel = 0; channel < 2; channel++)
            {
                int index = frame * 2 + channel;
                double value = dry[index]
                    + dry[((frame - echoA + frames) % frames) * 2 + 1 - channel] * 0.075
                    + dry[((frame - echoB + frames) % frames) * 2 + channel] * 0.038;
                samples[index] = (float)Math.Tanh(value * 0.93);
                peak = Math.Max(peak, Math.Abs(samples[index]));
                energy += samples[index] * samples[index];
            }
        }

        double rms = Math.Sqrt(energy / samples.Length);
        double gain = Math.Min(0.36 / Math.Max(0.001, peak), 0.046 / Math.Max(0.001, rms));
        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] *= (float)gain;
        }

        for (int channel = 0; channel < 2; channel++)
        {
            float difference = samples[(frames - 1) * 2 + channel] - samples[channel];
            for (int n = 0; n < 220; n++)
            {
                samples[(frames - 220 + n) * 2 + channel] -= difference * n / 219f;
            }
        }

        return new AudioClip(samples);

        void Felt(int midi, double start, double duration, double level, double pan)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12d);
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(duration * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double absolute = start + t;
                double drift = 1 + 0.0008 * Math.Sin(2 * Math.PI * 0.19 * absolute)
                    + 0.00035 * Math.Sin(2 * Math.PI * 0.43 * absolute);
                double phase = 2 * Math.PI * frequency * drift * t;
                double attack = Math.Min(1, t / 0.038);
                double release = Math.Min(1, (duration - t) / 0.34);
                double body = Math.Sin(phase)
                    + 0.095 * Math.Sin(phase * 2.003) * Math.Exp(-t * 2.8)
                    + 0.028 * Math.Sin(phase * 3.01) * Math.Exp(-t * 4.5);
                Add(first + n, body * Math.Exp(-t / 1.65) * attack * release * level, pan);
            }
        }

        void Pad(int midi, double start, double duration, double level, double pan)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12d);
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(duration * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double absolute = start + t;
                double phase = 2 * Math.PI * frequency * t;
                double detune = 2 * Math.PI * frequency * 1.0032 * t;
                double attack = Math.Min(1, t / 0.62);
                double release = Math.Min(1, (duration - t) / 0.78);
                double sway = 0.88 + 0.12 * Math.Sin(2 * Math.PI * 0.083 * absolute + midi);
                double body = Math.Sin(phase) * 0.58 + Math.Sin(detune) * 0.31 + Math.Sin(phase * 0.5) * 0.11;
                Add(first + n, body * attack * release * sway * level, pan);
            }
        }

        void Sub(int midi, double start, double duration, double level)
        {
            double frequency = 440 * Math.Pow(2, (midi - 69) / 12d);
            int first = (int)(start * AudioClip.Rate);
            int count = (int)(duration * AudioClip.Rate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double phase = 2 * Math.PI * frequency * t;
                double attack = Math.Min(1, t / 0.12);
                double release = Math.Min(1, (duration - t) / 0.42);
                double body = Math.Sin(phase) + 0.055 * Math.Sin(phase * 2);
                Add(first + n, body * Math.Exp(-t / 2.1) * attack * release * level, 0);
            }
        }

        void Kick(double start, double level)
        {
            int first = (int)(start * AudioClip.Rate);
            for (int n = 0; n < AudioClip.Rate * 0.32; n++)
            {
                double t = n / (double)AudioClip.Rate;
                double phase = 2 * Math.PI * (48 * t + 42 * 0.028 * (1 - Math.Exp(-t / 0.028)));
                double envelope = Math.Min(1, t / 0.003) * Math.Exp(-t / 0.070);
                Add(first + n, Math.Sin(phase) * envelope * level, 0);
            }
        }

        void Snare(double start, double level)
        {
            int first = (int)(start * AudioClip.Rate);
            double filtered = 0;
            for (int n = 0; n < AudioClip.Rate * 0.22; n++)
            {
                double t = n / (double)AudioClip.Rate;
                filtered = filtered * 0.62 + CasinoMusicSynthesis.Noise(first + n + 541) * 0.38;
                double body = filtered * 0.82 + Math.Sin(2 * Math.PI * 174 * t) * 0.18;
                Add(first + n, body * Math.Min(1, t / 0.003) * Math.Exp(-t / 0.048) * level, -0.08);
            }
        }

        void Hat(double start, double level, double pan)
        {
            int first = (int)(start * AudioClip.Rate);
            double low = 0;
            for (int n = 0; n < AudioClip.Rate * 0.085; n++)
            {
                double raw = CasinoMusicSynthesis.Noise(first * 3 + n + 7811);
                low = low * 0.7 + raw * 0.3;
                double t = n / (double)AudioClip.Rate;
                Add(first + n, (raw - low) * Math.Min(1, t / 0.002) * Math.Exp(-t / 0.019) * level, pan);
            }
        }

        void Add(int frame, double value, double pan)
        {
            frame %= frames;
            if (frame < 0)
            {
                frame += frames;
            }
            samples[frame * 2] += (float)(value * (1 - pan * 0.40));
            samples[frame * 2 + 1] += (float)(value * (1 + pan * 0.40));
        }
    }
}
