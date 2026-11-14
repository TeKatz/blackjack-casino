namespace BlackjackCasino.Desktop;

internal enum ManorCue
{
    Start,Reel,Stop1,Stop2,Stop3,Stop4,Stop5,SmallWin,MediumWin,BigWin,Wild,Scatter,Bonus,
    Heartbeat,Fakeout,Trigger,Unlock,DoorOpen,GhostFar,GhostNear,Checkpoint,Item,Escape,Catch,
    FreeStart,FreeEnd,MaximumExit,
}

/// <summary>Original PCM synthesis with restrained gain and soft attack/release. No system sounds.</summary>
internal static class ManorSoundSynthesis
{
    public const int Rate=24_000;
    public static byte[] Cue(ManorCue cue)
    {
        double duration=cue is ManorCue.MaximumExit or ManorCue.BigWin or ManorCue.Escape ? 1.4
            : cue is ManorCue.Trigger or ManorCue.DoorOpen or ManorCue.FreeStart ? 0.9
            : cue is >=ManorCue.Stop1 and <=ManorCue.Stop5 ? 0.2 : cue == ManorCue.Reel ? 0.11 : 0.4;
        var random=new Random(8113+(int)cue);
        double rollingNoise=0;
        double root=cue switch { ManorCue.Catch=>110,ManorCue.GhostNear=>82.4,ManorCue.GhostFar=>98,
            ManorCue.SmallWin=>523.25,ManorCue.MediumWin=>392,ManorCue.BigWin=>196,ManorCue.MaximumExit=>261.63,
            ManorCue.Scatter=>659.25,ManorCue.Wild=>293.66,ManorCue.FreeStart=>440,_=>146.83 };
        return Wave(duration,t=>
        {
            double attack=Math.Min(1,t/0.009),release=Math.Min(1,(duration-t)/0.025);
            double value;
            if(cue == ManorCue.Reel)
            {
                double noise=random.NextDouble()*2-1;
                rollingNoise=rollingNoise*0.72+noise*0.28;
                value=(rollingNoise*0.65+Math.Sin(2*Math.PI*174*t)*0.24)*Math.Exp(-t/0.025);
            }
            else if(cue is >=ManorCue.Stop1 and <=ManorCue.Stop5 || cue is ManorCue.Reel or ManorCue.Unlock)
            {
                double tone=130+17*(int)cue;
                value=(Math.Sin(tone*2*Math.PI*t)*0.5+(random.NextDouble()*2-1)*0.16)*Math.Exp(-t/0.042);
            }
            else if(cue==ManorCue.Heartbeat)
            {
                double local=t<0.17 ? t : t-0.17;
                value=Math.Sin(2*Math.PI*64*local)*Math.Exp(-local/0.05)*0.8;
            }
            else
            {
                value=0;
                int notes=cue is ManorCue.MaximumExit or ManorCue.Escape ? 6 : 3;
                for(int n=0;n<notes;n++)
                {
                    double local=t-n*0.12;
                    if(local<0) continue;
                    double frequency=root*Math.Pow(2,new[]{0,3,7,12,15,19}[n]/12d);
                    value+=(Math.Sin(2*Math.PI*frequency*local)+0.17*Math.Sin(2*Math.PI*2*frequency*local))*Math.Exp(-local/0.20)*0.22;
                }
                if(cue is ManorCue.DoorOpen or ManorCue.GhostFar or ManorCue.GhostNear or ManorCue.Fakeout)
                    value+=(random.NextDouble()*2-1)*0.12*Math.Exp(-t/0.35);
            }
            return value*attack*release*0.055;
        });
    }

    public static byte[] Ambience(int layer)
    {
        var random=new Random(1911+layer);
        double low=0;
        return Wave(24,t=>
        {
            low=0.992*low+0.008*(random.NextDouble()*2-1);
            double room=Math.Sin(2*Math.PI*55*t)*0.10+Math.Sin(2*Math.PI*82.5*t)*0.04;
            double wind=low*(0.7+0.3*Math.Sin(t*0.3));
            double fire=(random.NextDouble()*2-1)*0.025*(0.5+0.5*Math.Sin(t*3.2));
            double clock=Math.Sin(2*Math.PI*1200*t)*Math.Exp(-(t%2)/0.009)*0.07;
            double whisper=t>19 && t<21 ? low*0.65*Math.Sin((t-19)*Math.PI/2) : 0;
            double thunder=t>10 && t<14 ? Math.Sin(2*Math.PI*37*t)*Math.Exp(-(t-10))*0.14 : 0;
            double heart=Math.Sin(2*Math.PI*65*t)*Math.Exp(-(t%(layer==2?0.65:1.1))/0.07);
            double sample=layer switch
            {
                0=>room+wind+fire+clock+whisper+thunder,
                1 or 2=>heart*0.45+room*0.3,
                3=>room*0.6+fire*3+clock*0.3,
                4=>room*0.4+clock*2+whisper*0.6,
                5=>room+Math.Sin(2*Math.PI*146.83*t)*0.06+wind*0.2,
                6=>wind*0.6+clock*1.5+fire,
                7=>wind*2+thunder*1.4+fire*0.2,
                _=>room*1.5+whisper+thunder*0.6,
            };
            return sample*0.06*Math.Min(1,t/0.12)*Math.Min(1,(24-t)/0.15);
        });
    }

    private static byte[] Wave(double duration,Func<double,double> sample)
    {
        int count=(int)(Rate*duration);
        using var stream=new MemoryStream(44+count*2);
        using var writer=new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray()); writer.Write(36+count*2); writer.Write("WAVEfmt "u8.ToArray());
        writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(Rate); writer.Write(Rate*2);
        writer.Write((short)2); writer.Write((short)16); writer.Write("data"u8.ToArray()); writer.Write(count*2);
        for(int i=0;i<count;i++) writer.Write((short)(Math.Clamp(sample(i/(double)Rate),-0.12,0.12)*short.MaxValue));
        return stream.ToArray();
    }
}
