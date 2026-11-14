namespace BlackjackCasino.Desktop;

/// <summary>
/// Original, locally synthesized Daily Login chime. Only play after the native
/// reward transaction has been persisted; never on an unverified UI click.
/// </summary>
internal static class DailyLoginClaimAudio
{
    private static readonly object Owner = new();
    private static readonly Lazy<AudioClip> Clip = new(Build);

    internal static void Play(bool soundEnabled)
    {
        if (!soundEnabled) return;
        CasinoAudio.Play(Clip.Value, Owner, 0.75);
    }

    internal static AudioClip Build()
    {
        const double duration = 1.30;
        int frames = (int)Math.Ceiling(AudioClip.Rate * duration);
        var result = new float[frames * 2];
        // An initial velvet coin click followed by a warm five-note glockenspiel
        // fanfare. Partial harmonics and short echoes add sparkle without clipping.
        double[] pitches = [392.0, 523.25, 659.25, 783.99, 1046.50];
        for (int i = 0; i < frames; i++)
        {
            double t = i / (double)AudioClip.Rate;
            double left = 0, right = 0;

            // Muted metallic clasp, not a generic chip-drop UI sound.
            if (t < .17)
            {
                double tick = Math.Min(1, t / .002) * Math.Exp(-t * 36);
                double click = (Math.Sin(2 * Math.PI * 1260 * t) * .045
                    + Math.Sin(2 * Math.PI * 1775 * t) * .022
                    + Math.Sin(2 * Math.PI * 2325 * t) * .012) * tick;
                left += click; right += click * .83;
            }

            for (int j = 0; j < pitches.Length; j++)
            {
                double local = t - (.12 + j * .125);
                if (local < 0) continue;
                double attack = Math.Min(1d, local / .009d);
                double decay = Math.Exp(-local * (j == pitches.Length - 1 ? 3.7 : 6.6));
                double frequency = pitches[j];
                double tone = (Math.Sin(2 * Math.PI * frequency * local)
                    + Math.Sin(2 * Math.PI * (frequency * 2.001) * local) * .21
                    + Math.Sin(2 * Math.PI * (frequency * 3.005) * local) * .075)
                    * attack * decay * .053;
                left += tone * (j % 2 == 0 ? 1 : .75);
                right += tone * (j % 2 == 1 ? 1 : .75);

                double echo = local - .16;
                if (echo > 0)
                {
                    double reflected = Math.Sin(2 * Math.PI * frequency * echo)
                        * Math.Exp(-echo * 7) * .008;
                    left += reflected * .55;
                    right += reflected * .85;
                }
            }

            double release = Math.Clamp((duration - t) / .09, 0, 1);
            result[i * 2] = (float)Math.Clamp(left * release, -.30, .30);
            result[i * 2 + 1] = (float)Math.Clamp(right * release, -.30, .30);
        }
        return new AudioClip(result);
    }
}
