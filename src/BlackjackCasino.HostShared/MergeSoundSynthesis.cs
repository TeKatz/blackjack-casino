namespace BlackjackCasino.Desktop;

internal enum MergeCue { Spawn, PickUp, Drop, Merge, TierUp, Invalid, Order, Milestone }
internal static class MergeSoundSynthesis
{
    internal static AudioClip Build(MergeCue cue)
    {
        double[] notes = cue switch
        {
            MergeCue.Spawn => [660, 880], MergeCue.PickUp => [440], MergeCue.Drop => [330],
            MergeCue.Merge => [523.25, 659.25, 783.99], MergeCue.TierUp => [659.25, 783.99, 987.77, 1318.5],
            MergeCue.Invalid => [170, 150], MergeCue.Order => [587.33, 739.99, 880, 1174.66],
            _ => [523.25, 659.25, 783.99, 1046.5, 1318.5],
        };
        double step = cue is MergeCue.Order or MergeCue.Milestone ? .11 : .045;
        double decay = cue is MergeCue.PickUp or MergeCue.Drop ? .035 : .12;
        int frames = (int)(AudioClip.Rate * (notes.Length * step + decay * 6));
        var samples = new float[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            double t = i / (double)AudioClip.Rate, value = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                double local = t - n * step;
                if (local < 0) continue;
                double envelope = Math.Min(1, local / .004) * Math.Exp(-local / decay);
                value += (Math.Sin(2 * Math.PI * notes[n] * local) + .15 * Math.Sin(2 * Math.PI * notes[n] * 2.01 * local)) * envelope * .095;
            }
            samples[i * 2] = samples[i * 2 + 1] = (float)Math.Clamp(value, -.8, .8);
        }
        return new AudioClip(samples);
    }
}
