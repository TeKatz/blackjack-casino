namespace BlackjackCasino.Desktop;

internal enum TableCue { Deal, Chip, Win, Loss, Push, Menu, ReelSpin, ReelLoss, DiceRoll, DiceFiveOfAKind }
internal static class CasinoSoundSynthesis
{
    internal static AudioClip Build(TableCue cue)
    {
        double duration = cue switch { TableCue.DiceFiveOfAKind => 1.25, TableCue.DiceRoll => .62, TableCue.Deal => 0.17, TableCue.Menu => 0.055, TableCue.ReelSpin => 0.11, TableCue.Chip => 0.16, TableCue.Win => 0.56, TableCue.Loss => 0.48, _ => 0.38 };
        var samples = new float[(int)(duration * AudioClip.Rate) * 2];
        double low = 0, mid = 0;
        double[] notes = cue switch { TableCue.DiceFiveOfAKind => [659.25, 783.99, 987.77, 1174.66, 1318.51], TableCue.Win => [523.25,659.25,783.99], TableCue.Loss => [349.23,293.66,246.94], TableCue.Push => [440,440], TableCue.ReelLoss => [196,146.83], _ => [] };
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
                TableCue.DiceRoll => DiceTap(t, 0.015, 580, mid - low) + DiceTap(t, 0.085, 780, mid - low)
                    + DiceTap(t, 0.17, 640, mid - low) + DiceTap(t, 0.29, 870, mid - low)
                    + DiceTap(t, 0.43, 530, mid - low),
                _ => 0,
            };
            for (int n = 0; n < notes.Length; n++)
            {
                double local = t - n * (cue == TableCue.DiceFiveOfAKind ? 0.105 : cue == TableCue.Win ? 0.09 : 0.115);
                if (local < 0) continue;
                double f = notes[n];
                bool celebration = cue == TableCue.DiceFiveOfAKind;
                value += (Math.Sin(2*Math.PI*f*local)+Math.Sin(2*Math.PI*f*2*local)*(celebration ? .18 : .085))
                    * Env(local,0.008,celebration ? .22 : cue == TableCue.Win ? 0.13 : 0.11) * (celebration ? .075 : cue == TableCue.Win ? 0.085 : 0.065);
            }
            value *= 0.24 * Math.Min(1,(duration-t)/0.025);
            samples[i*2] = samples[i*2+1] = (float)value;
        }
        return new(samples);
    }
    private static double DiceTap(double t, double start, double pitch, double noise)
    {
        double local = t - start;
        return local < 0 ? 0 : (Math.Sin(2 * Math.PI * pitch * local) * .055
            + Math.Sin(2 * Math.PI * pitch * 1.43 * local) * .025 + noise * .12) * Env(local, .0018, .018);
    }
    private static double Env(double t,double attack,double decay) => t < 0 ? 0 : Math.Min(1,t/attack)*Math.Exp(-t/decay);
}
