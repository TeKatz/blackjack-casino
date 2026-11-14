namespace BlackjackCasino.Desktop;

internal sealed record AudioClip(float[] Samples)
{
    internal const int Rate = 44_100;
    internal int Frames => Samples.Length / 2;
    internal static AudioClip FromWave(byte[] wav)
    {
        using var reader = new BinaryReader(new MemoryStream(wav));
        if (new string(reader.ReadChars(4)) != "RIFF") throw new InvalidDataException("Expected PCM WAV.");
        reader.ReadInt32();
        if (new string(reader.ReadChars(4)) != "WAVE") throw new InvalidDataException("Expected WAVE.");
        int rate = 0, channels = 0; byte[]? data = null;
        while (reader.BaseStream.Position + 8 <= wav.Length)
        {
            string id = new(reader.ReadChars(4)); int size = reader.ReadInt32();
            long end = reader.BaseStream.Position + size;
            if (size < 0 || end > wav.Length) throw new InvalidDataException("Truncated WAV.");
            if (id == "fmt ")
            {
                if (reader.ReadInt16() != 1) throw new InvalidDataException("Expected PCM.");
                channels = reader.ReadInt16(); rate = reader.ReadInt32(); reader.ReadInt32(); reader.ReadInt16();
                if (reader.ReadInt16() != 16) throw new InvalidDataException("Expected 16-bit audio.");
            }
            if (id == "data") data = reader.ReadBytes(size);
            reader.BaseStream.Position = end + (size & 1);
        }
        if (data is null || data.Length == 0 || rate <= 0 || channels is < 1 or > 2 || data.Length % (channels * 2) != 0)
            throw new InvalidDataException("Invalid PCM format.");
        int inputFrames = data.Length / (channels * 2);
        int frames = (int)Math.Round(inputFrames * (double)Rate / rate);
        var samples = new float[frames * 2];
        for (int i = 0; i < frames; i++)
        {
            double position = i * (double)rate / Rate;
            int a = Math.Min(inputFrames - 1, (int)position), b = Math.Min(inputFrames - 1, a + 1);
            double fraction = position - a;
            for (int c = 0; c < 2; c++)
            {
                int channel = Math.Min(c, channels - 1);
                double x = BitConverter.ToInt16(data, (a * channels + channel) * 2) / 32768d;
                double y = BitConverter.ToInt16(data, (b * channels + channel) * 2) / 32768d;
                samples[i * 2 + c] = (float)(x + (y - x) * fraction);
            }
        }
        return new(samples);
    }

    internal byte[] Wave()
    {
        using var stream = new MemoryStream(44 + Samples.Length * 2);
        using var w = new BinaryWriter(stream);
        w.Write("RIFF"u8); w.Write(36 + Samples.Length * 2); w.Write("WAVEfmt "u8);
        w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(Rate); w.Write(Rate * 4);
        w.Write((short)4); w.Write((short)16); w.Write("data"u8); w.Write(Samples.Length * 2);
        foreach (float sample in Samples) w.Write((short)Math.Round(Math.Clamp(sample, -1, 1) * 32767));
        return stream.ToArray();
    }
}
