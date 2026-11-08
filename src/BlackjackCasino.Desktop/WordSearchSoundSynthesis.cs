namespace BlackjackCasino.Desktop;

internal enum WordSearchCue { Found, NewDiscovery }

/// <summary>Short, original, soft stereo plucks for saved native word finds.</summary>
internal static class WordSearchSoundSynthesis
{
    internal static AudioClip Build(WordSearchCue cue)
    {
        bool first = cue == WordSearchCue.NewDiscovery;
        double[] notes = first ? [659.25, 987.77, 1318.51] : [784.0, 1046.5];
        double step = first ? .057 : .045;
        double duration = first ? .37 : .24;
        int frames = (int)Math.Ceiling(AudioClip.Rate * duration);
        var output = new float[frames * 2];
        for (int sample = 0; sample < frames; sample++)
        {
            double time = sample / (double)AudioClip.Rate;
            double left = 0, right = 0;
            for (int note = 0; note < notes.Length; note++)
            {
                double local = time - note * step;
                if (local < 0) continue;
                double attack = Math.Min(1, local / .007);
                double decay = Math.Exp(-local / (first ? .072 : .052));
                double pluck = (Math.Sin(2 * Math.PI * notes[note] * local)
                    + .13 * Math.Sin(2 * Math.PI * 2.005 * notes[note] * local))
                    * attack * decay * .075;
                left += pluck * (note % 2 == 0 ? 1 : .84);
                right += pluck * (note % 2 == 1 ? 1 : .84);
            }
            double release = Math.Clamp((duration - time) / .06, 0, 1);
            output[sample * 2] = (float)Math.Clamp(left * release, -.2, .2);
            output[sample * 2 + 1] = (float)Math.Clamp(right * release, -.2, .2);
        }
        return new AudioClip(output);
    }
}
