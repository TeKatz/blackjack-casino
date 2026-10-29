namespace BlackjackCasino.Desktop;

internal enum TableCue { Deal, Chip, Win, Loss, Push, Menu, ReelSpin, ReelLoss }
internal static class CasinoSoundSynthesis
{
    internal static AudioClip Build(TableCue cue)
    {
        double duration = cue switch { TableCue.Deal => 0.17, TableCue.Menu => 0.055, TableCue.ReelSpin => 0.11, TableCue.Chip => 0.16, TableCue.Win => 0.56, TableCue.Loss => 0.48, _ => 0.38 };
        var samples = new float[(int)(duration * AudioClip.Rate) * 2];
        double low = 0, mid = 0;
        double[] notes = cue switch { TableCue.Win => [523.25,659.25,783.99], TableCue.Loss => [349.23,293.66,246.94], TableCue.Push => [440,440], TableCue.ReelLoss => [196,146.83], _ => [] };
        for (int i = 0; i < samples.Length / 2; i++)
        {
            double t = i / (double)AudioClip.Rate;
            double noise = CasinoMusicSynthesis.Noise(i + (int)cue * 971);
            low = low * 0.94 + noise * 0.06; mid = mid * 0.65 + noise * 0.35;
            double value = cue switch
            {
                TableCue.Deal => ((mid-low)*0.16 + low*0.16)*Env(t,0.004,0.045)
                    + ((mid-low)*0.09 + Math.Sin(2*Math.PI*170*t)*0.016)*Env(t-0.045,0.003,0.024),
                TableCue.Chip => (Math.Sin(2*Math.PI*1900*t)*0.095+Math.Sin(2*Math.PI*2750*t)*0.045+Math.Sin(2*Math.PI*3900*t)*0.018)*Env(t,0.0015,0.043),
                TableCue.Menu => (Math.Sin(2*Math.PI*1140*t)*0.035+(mid-low)*0.028)*Env(t,0.002,0.009),
                TableCue.ReelSpin => (Math.Sin(2*Math.PI*225*t)*0.14+(mid-low)*0.30)*Env(t,0.002,0.019),
                _ => 0,
            };
            for (int n = 0; n < notes.Length; n++)
            {
                double local = t - n * (cue == TableCue.Win ? 0.09 : 0.115);
                if (local < 0) continue;
                double f = notes[n];
                value += (Math.Sin(2*Math.PI*f*local)+Math.Sin(2*Math.PI*f*2*local)*0.085)
                    * Env(local,0.008,cue == TableCue.Win ? 0.13 : 0.11) * (cue == TableCue.Win ? 0.085 : 0.065);
            }
            value *= 0.24 * Math.Min(1,(duration-t)/0.025);
            samples[i*2] = samples[i*2+1] = (float)value;
        }
        return new(samples);
    }
    private static double Env(double t,double attack,double decay) => t < 0 ? 0 : Math.Min(1,t/attack)*Math.Exp(-t/decay);
}
