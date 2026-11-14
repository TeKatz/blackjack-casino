using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Desktop;

internal static class CollectionSoundSynthesis
{
    internal static AudioClip Build(CollectionRarity rarity, bool opening)
    {
        int rank = (int)rarity;
        double duration = opening ? .65 + rank * .15 : .4 + rank * .18;
        var samples = new float[(int)(AudioClip.Rate * duration) * 2];
        double[] notes = [523.25, 659.25, 783.99, 987.77, 1174.66];
        for (int i = 0; i < samples.Length / 2; i++)
        {
            double t = i / (double)AudioClip.Rate;
            double envelope = Math.Min(1, t / .008) * Math.Min(1, (duration - t) / .05);
            double value = 0;
            if (opening)
            {
                // Foil tear over a rising resonant sweep; longer and more layered for rare packs.
                double noise = CasinoMusicSynthesis.Noise(i + 8101);
                value = noise * .035 * Math.Exp(-t * 7)
                    + Math.Sin(2 * Math.PI * (150 * t + (110 + rank * 50) * t * t)) * .025 * Math.Sin(Math.PI * t / duration);
            }
            else
            {
                for (int n = 0; n < 1 + rank; n++)
                {
                    double local = t - n * .075;
                    if (local < 0) continue;
                    value += (Math.Sin(2 * Math.PI * notes[n] * local) + .14 * Math.Sin(4 * Math.PI * notes[n] * local))
                        * Math.Min(1, local / .006) * Math.Exp(-local / (.1 + rank * .025)) * .042;
                }
            }
            samples[i * 2] = (float)(value * envelope);
            samples[i * 2 + 1] = (float)(value * envelope * .96);
        }
        return new(samples);
    }
}
