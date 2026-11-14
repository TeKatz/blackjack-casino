namespace BlackjackCasino.Desktop;

/// <summary>Sample-clock mixer: independent cue tails, circular loops, smoothed gain.</summary>
internal sealed class AudioMixer
{
    private sealed class Voice(AudioClip clip, object owner, bool loop, double gain)
    {
        internal readonly AudioClip Clip = clip;
        internal readonly object Owner = owner;
        internal readonly bool Loop = loop;
        internal int Position;
        internal double Gain = loop ? 0 : gain;
        internal double Target = gain;
        internal bool Removing;
    }
    private readonly object gate = new();
    private readonly List<Voice> voices = [];
    internal int VoiceCount { get { lock (gate) return voices.Count; } }
    internal void Play(AudioClip clip, object owner, double gain = 1)
    {
        if (clip.Frames == 0) return;
        lock (gate)
        {
            // Bounded polyphony even under Turbo/rapid input; do not steal an existing tail.
            if (voices.Count(v => !v.Loop) < 48) voices.Add(new(clip, owner, false, Math.Clamp(gain, 0, 1)));
        }
    }
    internal void Loop(AudioClip clip, object owner, double gain)
    {
        lock (gate)
        {
            Voice? voice = voices.FirstOrDefault(v => v.Loop && !v.Removing && ReferenceEquals(v.Owner, owner) && ReferenceEquals(v.Clip, clip));
            if (voice is null)
            {
                foreach (Voice old in voices.Where(v => v.Loop && ReferenceEquals(v.Owner, owner))) { old.Target = 0; old.Removing = true; }
                voices.Add(new(clip, owner, true, gain));
            }
            else { voice.Target = gain; voice.Removing = false; }
        }
    }
    internal void Volume(object owner, double gain)
    {
        lock (gate) foreach (Voice v in voices.Where(v => !v.Removing && ReferenceEquals(v.Owner, owner))) v.Target = Math.Clamp(gain, 0, 1);
    }
    internal void Stop(object owner)
    {
        lock (gate) foreach (Voice v in voices.Where(v => ReferenceEquals(v.Owner, owner))) { v.Target = 0; v.Removing = true; }
    }
    internal void Render(Span<short> output)
    {
        output.Clear();
        lock (gate)
        {
            for (int frame = 0; frame < output.Length / 2; frame++)
            {
                double left = 0, right = 0;
                foreach (Voice v in voices)
                {
                    if (v.Position >= v.Clip.Frames) continue;
                    v.Gain += (v.Target - v.Gain) * 0.0015; // ~15ms gain ramp, no mute/scene clicks
                    left += v.Clip.Samples[v.Position * 2] * v.Gain;
                    right += v.Clip.Samples[v.Position * 2 + 1] * v.Gain;
                    v.Position++;
                    if (v.Loop && v.Position == v.Clip.Frames) v.Position = 0;
                }
                output[frame * 2] = Limit(left);
                output[frame * 2 + 1] = Limit(right);
            }
            voices.RemoveAll(v => (!v.Loop && v.Position >= v.Clip.Frames) || (v.Removing && v.Gain < 0.00001));
        }
    }
    private static short Limit(double value) => (short)Math.Round(Math.Tanh(value) * 32767);
}
