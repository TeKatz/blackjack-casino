namespace BlackjackCasino.Desktop;

internal enum Soundtrack { Blackjack, Reels, Manor, DiceLounge, ChillLounge }

/// <summary>Original cached scores: sampled themes plus generated lounge compositions; no network dependency.</summary>
internal static class CasinoMusicSynthesis
{
    internal static int Tempo(Soundtrack track) => track switch
    {
        Soundtrack.Blackjack => 72,
        Soundtrack.Reels => 104,
        Soundtrack.DiceLounge => 68,
        Soundtrack.ChillLounge => 54,
        _ => 62,
    };

    internal static int Beats(Soundtrack track) => track == Soundtrack.Manor ? 48 : 64;

    internal static string Theme(Soundtrack track) => track switch
    {
        Soundtrack.Blackjack => "Felt after hours",
        Soundtrack.Reels => "Brass and lucky sevens",
        Soundtrack.DiceLounge => "A quiet roll",
        Soundtrack.ChillLounge => "Quiet Pages",
        _ => "The clockwork waltz",
    };

    internal static AudioClip Build(Soundtrack track)
    {
        if (track == Soundtrack.DiceLounge)
        {
            return DiceLoungeSynthesis.Build();
        }
        if (track == Soundtrack.ChillLounge)
        {
            return ChillLoungeSynthesis.Build();
        }

        string name = "BlackjackCasino.Music." + track.ToString().ToLowerInvariant() + ".wav";
        using Stream stream = typeof(CasinoMusicSynthesis).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException("Missing music resource: " + name);
        using var data = new MemoryStream();
        stream.CopyTo(data);
        return AudioClip.FromWave(data.ToArray());
    }

    internal static double Noise(int i)
    {
        uint x = unchecked((uint)i * 747796405u + 2891336453u);
        x = ((x >> ((int)(x >> 28) + 4)) ^ x) * 277803737u;
        x = (x >> 22) ^ x;
        return x / (double)uint.MaxValue * 2 - 1;
    }
}
