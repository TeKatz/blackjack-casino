using System.Runtime.InteropServices;

namespace BlackjackCasino.Desktop;

/// <summary>One buffered PCM output, independent of WPF dispatcher/MediaPlayer seeks.</summary>
internal static class CasinoAudio
{
    internal static readonly AudioMixer Mixer = new();
    private static readonly Lazy<WaveOutput> Device = new(() => new WaveOutput(Mixer));
    private static long renderedFrames;
    internal static long RenderedFrames => Interlocked.Read(ref renderedFrames);
    internal static string DeviceStatus { get; private set; } = "not-started";
    internal static void Start() { _ = Device.Value; }
    internal static void Play(AudioClip clip, object owner, double gain = 1) { Start(); Mixer.Play(clip, owner, gain); }
    internal static void Shutdown() { if (Device.IsValueCreated) Device.Value.Dispose(); }

    private sealed class WaveOutput : IDisposable
    {
        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        private struct Format
        {
            internal ushort Tag, Channels;
            internal uint Rate, Bytes;
            internal ushort Align, Bits, Extra;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct Header
        {
            internal IntPtr Data;
            internal uint Length, Recorded;
            internal UIntPtr User;
            internal uint Flags, Loops;
            internal IntPtr Next;
            internal UIntPtr Reserved;
        }
        [DllImport("winmm.dll")] private static extern uint waveOutOpen(out IntPtr device, uint id, ref Format format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")] private static extern uint waveOutPrepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutWrite(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutReset(IntPtr device);
        [DllImport("winmm.dll")] private static extern uint waveOutUnprepareHeader(IntPtr device, IntPtr header, uint size);
        [DllImport("winmm.dll")] private static extern uint waveOutClose(IntPtr device);
        private readonly AutoResetEvent changed = new(false);
        private readonly Thread thread;
        private volatile bool stopping;
        internal WaveOutput(AudioMixer mixer)
        {
            thread = new Thread(() => Run(mixer)) { IsBackground = true, Name = "Casino PCM output", Priority = ThreadPriority.AboveNormal };
            thread.Start();
        }
        private void Run(AudioMixer mixer)
        {
            IntPtr device = IntPtr.Zero;
            var buffers = new List<(IntPtr Header, IntPtr Data)>();
            uint headerSize = (uint)Marshal.SizeOf<Header>();
            try
            {
                var format = new Format { Tag = 1, Channels = 2, Rate = AudioClip.Rate, Bytes = AudioClip.Rate * 4, Align = 4, Bits = 16 };
                var samples = new short[512 * 2];
                if (waveOutOpen(out device, uint.MaxValue, ref format, changed.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 0x50000) != 0)
                {
                    DeviceStatus = "no-device";
                    device = IntPtr.Zero;
                    // Headless/no-device sessions still advance the mixer and release stopped voices.
                    while (!stopping) { mixer.Render(samples); Interlocked.Add(ref renderedFrames,512); changed.WaitOne(12); }
                    return;
                }
                DeviceStatus = "ready";
                for (int i = 0; i < 4; i++)
                {
                    IntPtr data = Marshal.AllocHGlobal(samples.Length * 2), header = Marshal.AllocHGlobal((int)headerSize);
                    buffers.Add((header, data));
                    Marshal.StructureToPtr(new Header { Data = data, Length = (uint)samples.Length * 2 }, header, false);
                    Check(waveOutPrepareHeader(device, header, headerSize));
                    mixer.Render(samples); Interlocked.Add(ref renderedFrames,512); Marshal.Copy(samples, 0, data, samples.Length);
                    Check(waveOutWrite(device, header, headerSize));
                }
                int next = 0;
                while (!stopping)
                {
                    changed.WaitOne(20);
                    // Requeue in original playback order, including the 3 -> 0 wrap.
                    // Scanning indices from zero could reorder returned buffers after a stall.
                    for (int ready = 0; ready < buffers.Count; ready++)
                    {
                        if (stopping) break;
                        var buffer = buffers[next];
                        if ((Marshal.PtrToStructure<Header>(buffer.Header).Flags & 1) == 0) break;
                        mixer.Render(samples); Interlocked.Add(ref renderedFrames,512); Marshal.Copy(samples, 0, buffer.Data, samples.Length);
                        Check(waveOutWrite(device, buffer.Header, headerSize));
                        next = (next + 1) % buffers.Count;
                    }
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException) { DeviceStatus = "unavailable"; }
            finally
            {
                if (device != IntPtr.Zero) waveOutReset(device);
                foreach (var b in buffers)
                {
                    if (device != IntPtr.Zero) waveOutUnprepareHeader(device, b.Header, headerSize);
                    Marshal.FreeHGlobal(b.Header); Marshal.FreeHGlobal(b.Data);
                }
                if (device != IntPtr.Zero) waveOutClose(device);
                changed.Dispose();
            }
        }
        private static void Check(uint result) { if (result != 0) throw new InvalidOperationException("PCM output unavailable: " + result); }
        public void Dispose()
        {
            stopping = true;
            try { changed.Set(); } catch (ObjectDisposedException) { }
            thread.Join();
        }
    }
}
