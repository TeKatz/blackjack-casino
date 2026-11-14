namespace BlackjackCasino.HostShared;

/// <summary>
/// Original 58 BPM fantasy ambience for Tales from the Backroom. The arrangement stays deliberately
/// restrained: warm drones, felt-like harp plucks, a sparse wooden-flute lead and a soft room bed.
/// Generated locally so the Adventure mode has its own identity without shipping licensed audio.
/// </summary>
internal static class AdventureSynthesis
{
    internal static AudioClip Build()
    {
        const double bpm = 58d;
        const int bars = 16;
        double beat = 60d / bpm;
        int frames = (int)Math.Round(bars * 4 * beat * AudioClip.Rate);
        var samples = new float[frames * 2];

        // Dm(add9) · Bbmaj7 · Gm9 · Asus4/A. The repeated D/A pedal keeps the piece calm while
        // the upper notes provide the slightly mysterious tabletop-fantasy colour.
        int[][] harmony =
        [
            [38, 45, 50, 53, 57, 64],
            [34, 41, 46, 50, 53, 57],
            [31, 38, 43, 46, 50, 57],
            [33, 40, 45, 50, 52, 57],
        ];
        int[][] harp =
        [
            [62, 69, 65, 74, 69, 76, 65, 69],
            [58, 65, 62, 69, 65, 74, 62, 65],
            [55, 62, 58, 69, 62, 70, 58, 62],
            [57, 64, 62, 69, 64, 74, 62, 69],
        ];
        int[] melody = [74, 76, 77, 81, 79, 77, 76, 74, 72, 74, 76, 69, 72, 74, 69, 67];

        for (int bar = 0; bar < bars; bar++)
        {
            double start = bar * 4 * beat;
            int chord = bar % harmony.Length;

            // Slow stereo bed. Upper voices are spread slightly while the low pedal remains centred.
            for (int voice = 0; voice < harmony[chord].Length; voice++)
            {
                double pan = voice < 2 ? 0 : (voice - 3.5) * 0.16;
                Pad(harmony[chord][voice], start, 4.35 * beat, voice < 2 ? 0.024 : 0.017, pan);
            }

            // Harp-like figure. Small timing gaps leave air between the notes instead of becoming a looped arpeggiator.
            for (int note = 0; note < harp[chord].Length; note++)
            {
                double offset = note * 0.5 * beat + (note % 2 == 1 ? 0.025 * beat : 0);
                double pan = note % 2 == 0 ? -0.23 : 0.23;
                Pluck(harp[chord][note], start + offset, 0.82 * beat, note is 0 or 4 ? 0.036 : 0.026, pan);
            }

            // One restrained lead phrase every other bar. The long release keeps it closer to breath than a synth lead.
            if (bar % 2 == 1)
            {
                int index = (bar / 2) * 2;
                Flute(melody[index % melody.Length], start + 0.62 * beat, 1.16 * beat, 0.018, -0.14);
                Flute(melody[(index + 1) % melody.Length], start + 2.34 * beat, 1.28 * beat, 0.016, 0.18);
            }

            // Sparse bell punctuation marks chapter-like moments without turning into percussion.
            if (bar % 4 == 0) Bell(86 - (bar % 8 == 0 ? 0 : 2), start + 0.08 * beat, 1.9 * beat, 0.013, 0.34);
        }

        // A very low, filtered-feeling room/tape bed keeps complete silence from appearing between plucks.
        double air = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            air = air * 0.992 + CasinoMusicSynthesis.Noise(frame * 5 + 27183) * 0.008;
            double t = frame / (double)AudioClip.Rate;
            double breathe = 0.72 + 0.28 * Math.Sin(2 * Math.PI * 0.031 * t);
            double bed = air * 0.0017 * breathe;
            samples[frame * 2] += (float)(bed * 0.88);
            samples[frame * 2 + 1] += (float)(bed * 1.12);
        }

        // Short circular room reflections. Using wrapped taps preserves the loop instead of adding a hard tail at the seam.
        float[] dry = (float[])samples.Clone();
        int reflectionA = (int)Math.Round(0.72 * beat * AudioClip.Rate);
        int reflectionB = (int)Math.Round(1.51 * beat * AudioClip.Rate);
        double peak = 0;
        double energy = 0;
        for (int frame = 0; frame < frames; frame++)
        {
            for (int channel = 0; channel < 2; channel++)
            {
                int index = frame * 2 + channel;
                double value = dry[index]
                    + dry[((frame - reflectionA + frames) % frames) * 2 + 1 - channel] * 0.085
                    + dry[((frame - reflectionB + frames) % frames) * 2 + channel] * 0.045;
                samples[index] = (float)Math.Tanh(value * 0.96);
                peak = Math.Max(peak, Math.Abs(samples[index]));
                energy += samples[index] * samples[index];
            }
        }

        double rms = Math.Sqrt(energy / samples.Length);
        double gain = Math.Min(0.34 / Math.Max(0.001, peak), 0.041 / Math.Max(0.001, rms));
        for (int index = 0; index < samples.Length; index++) samples[index] *= (float)gain;

        // Make the final few milliseconds meet the first sample exactly. The adjustment is intentionally tiny and inaudible.
        for (int channel = 0; channel < 2; channel++)
        {
            float difference = samples[(frames - 1) * 2 + channel] - samples[channel];
            for (int n = 0; n < 256; n++)
            {
                samples[(frames - 256 + n) * 2 + channel] -= difference * n / 255f;
            }
        }

        return new AudioClip(samples);

        void Pad(int midi, double start, double duration, double level, double pan)
        {
            RenderVoice(midi, start, duration, level, pan, (phase, age, length) =>
            {
                double attack = Math.Min(1, age / 0.42);
                double release = Math.Min(1, (length - age) / 0.72);
                double envelope = Smooth(Math.Min(attack, release));
                return (Math.Sin(phase) + 0.31 * Math.Sin(phase * 0.5 + 0.6) + 0.12 * Math.Sin(phase * 2 + 1.2)) * envelope;
            });
        }

        void Pluck(int midi, double start, double duration, double level, double pan)
        {
            RenderVoice(midi, start, duration, level, pan, (phase, age, _) =>
            {
                double envelope = Math.Exp(-age * 4.6) * Math.Min(1, age / 0.008);
                return (Math.Sin(phase) + 0.43 * Math.Sin(phase * 2.01) + 0.16 * Math.Sin(phase * 3.98)) * envelope;
            });
        }

        void Flute(int midi, double start, double duration, double level, double pan)
        {
            RenderVoice(midi, start, duration, level, pan, (phase, age, length) =>
            {
                double attack = Math.Min(1, age / 0.12);
                double release = Math.Min(1, (length - age) / 0.32);
                double envelope = Smooth(Math.Min(attack, release));
                double breath = CasinoMusicSynthesis.Noise((int)(age * AudioClip.Rate) + midi * 997) * 0.045;
                return (Math.Sin(phase + 0.012 * Math.Sin(age * 13)) + 0.14 * Math.Sin(phase * 2)) * envelope + breath * envelope;
            });
        }

        void Bell(int midi, double start, double duration, double level, double pan)
        {
            RenderVoice(midi, start, duration, level, pan, (phase, age, _) =>
            {
                double envelope = Math.Exp(-age * 2.35) * Math.Min(1, age / 0.004);
                return (Math.Sin(phase) + 0.37 * Math.Sin(phase * 2.72) + 0.18 * Math.Sin(phase * 4.18)) * envelope;
            });
        }

        void RenderVoice(int midi, double start, double duration, double level, double pan, Func<double, double, double, double> oscillator)
        {
            int first = Math.Max(0, (int)Math.Round(start * AudioClip.Rate));
            int last = Math.Min(frames, first + (int)Math.Round(duration * AudioClip.Rate));
            double frequency = 440d * Math.Pow(2, (midi - 69) / 12d);
            double step = 2 * Math.PI * frequency / AudioClip.Rate;
            double left = level * Math.Sqrt((1 - Math.Clamp(pan, -1, 1)) * 0.5);
            double right = level * Math.Sqrt((1 + Math.Clamp(pan, -1, 1)) * 0.5);
            double length = Math.Max(1d / AudioClip.Rate, duration);
            double phase = 0;
            for (int frame = first; frame < last; frame++)
            {
                double age = (frame - first) / (double)AudioClip.Rate;
                double signal = oscillator(phase, age, length);
                samples[frame * 2] += (float)(signal * left);
                samples[frame * 2 + 1] += (float)(signal * right);
                phase += step;
            }
        }

        static double Smooth(double value)
        {
            value = Math.Clamp(value, 0, 1);
            return value * value * (3 - 2 * value);
        }
    }
}
