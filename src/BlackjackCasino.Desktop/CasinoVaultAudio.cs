namespace BlackjackCasino.Desktop;

internal enum CasinoVaultCue
{
    Unlock,
    Door,
    Reward,
}

internal static class CasinoVaultAudio
{
    private static readonly Lazy<AudioClip> Unlock = new(() => Build(CasinoVaultCue.Unlock));
    private static readonly Lazy<AudioClip> Door = new(() => Build(CasinoVaultCue.Door));
    private static readonly Lazy<AudioClip> Reward = new(() => Build(CasinoVaultCue.Reward));

    internal static void Play(CasinoVaultCue cue, object owner, bool enabled)
    {
        if (!enabled) return;
        AudioClip clip = cue switch
        {
            CasinoVaultCue.Unlock => Unlock.Value,
            CasinoVaultCue.Door => Door.Value,
            _ => Reward.Value,
        };
        CasinoAudio.Play(clip, owner, cue == CasinoVaultCue.Door ? 0.78 : 0.88);
    }

    private static AudioClip Build(CasinoVaultCue cue)
    {
        double duration = cue switch
        {
            CasinoVaultCue.Unlock => 0.86,
            CasinoVaultCue.Door => 1.35,
            _ => 1.55,
        };
        int frames = (int)(AudioClip.Rate * duration);
        var samples = new float[frames * 2];
        double lowNoise = 0d;

        for (int i = 0; i < frames; i++)
        {
            double t = i / (double)AudioClip.Rate;
            double noise = CasinoMusicSynthesis.Noise(i + 4_219);
            lowNoise = lowNoise * 0.94 + noise * 0.06;
            double value = cue switch
            {
                CasinoVaultCue.Unlock =>
                    MetallicClick(t, 0.05, 1_180) + MetallicClick(t, 0.23, 1_460)
                    + MetallicClick(t, 0.41, 1_720) + MetallicClick(t, 0.58, 1_940)
                    + Math.Sin(2 * Math.PI * 92 * t) * Envelope(t, 0.01, 0.42) * 0.025,
                CasinoVaultCue.Door =>
                    (Math.Sin(2 * Math.PI * (54 - 8 * t) * t) * 0.16 + lowNoise * 0.18)
                    * Envelope(t, 0.02, 0.72)
                    + MetallicClick(t, 0.88, 310) * 0.75,
                _ => RewardTone(t),
            };

            value *= Math.Min(1d, Math.Max(0d, (duration - t) / 0.035));
            samples[i * 2] = samples[i * 2 + 1] = (float)Math.Clamp(value, -0.9, 0.9);
        }
        return new AudioClip(samples);
    }

    private static double MetallicClick(double t, double start, double pitch)
    {
        double local = t - start;
        if (local < 0) return 0;
        return (Math.Sin(2 * Math.PI * pitch * local) * 0.10
            + Math.Sin(2 * Math.PI * pitch * 1.63 * local) * 0.055
            + Math.Sin(2 * Math.PI * pitch * 2.21 * local) * 0.028)
            * Envelope(local, 0.0015, 0.045);
    }

    private static double RewardTone(double t)
    {
        double[] notes = [523.25, 659.25, 783.99, 987.77, 1_174.66];
        double value = 0d;
        for (int n = 0; n < notes.Length; n++)
        {
            double local = t - n * 0.17;
            if (local < 0) continue;
            double f = notes[n];
            value += (Math.Sin(2 * Math.PI * f * local) + Math.Sin(2 * Math.PI * f * 2 * local) * 0.14)
                * Envelope(local, 0.008, 0.24) * 0.075;
        }
        value += Math.Sin(2 * Math.PI * 130 * t) * Envelope(t, 0.025, 0.7) * 0.025;
        return value;
    }

    private static double Envelope(double t, double attack, double decay) =>
        t < 0 ? 0 : Math.Min(1, t / attack) * Math.Exp(-t / decay);
}
