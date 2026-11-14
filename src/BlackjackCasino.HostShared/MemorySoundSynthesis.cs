namespace BlackjackCasino.HostShared;

internal enum MemoryCue { Flip, Match, Mismatch, Complete, Perfect }

internal static class MemorySoundSynthesis
{
    internal static AudioClip Build(MemoryCue cue)
    {
        double[] notes = cue switch
        {
            MemoryCue.Flip => [520],
            MemoryCue.Match => [659.25, 880],
            MemoryCue.Mismatch => [230, 190],
            MemoryCue.Complete => [523.25, 659.25, 783.99, 1046.5],
            MemoryCue.Perfect => [659.25, 783.99, 987.77, 1174.66, 1567.98],
            _ => [440],
        };
        double step = cue is MemoryCue.Complete or MemoryCue.Perfect ? .09 : .055;
        double decay = cue == MemoryCue.Flip ? .035 : cue == MemoryCue.Mismatch ? .08 : .13;
        int frames = (int)(AudioClip.Rate * (notes.Length * step + decay * 6));
        var samples = new float[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            double t = i / (double)AudioClip.Rate;
            double value = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                double local = t - n * step;
                if (local < 0) continue;
                double envelope = Math.Min(1, local / .004) * Math.Exp(-local / decay);
                value += (Math.Sin(2 * Math.PI * notes[n] * local)
                    + .12 * Math.Sin(2 * Math.PI * notes[n] * 2.01 * local)) * envelope * .09;
            }
            samples[i * 2] = samples[i * 2 + 1] = (float)Math.Clamp(value, -.75, .75);
        }
        return new AudioClip(samples);
    }
}
