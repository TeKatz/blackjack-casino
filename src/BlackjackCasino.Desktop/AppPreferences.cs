using BlackjackCasino.Update;

namespace BlackjackCasino.Desktop;

internal enum PlayModePreference
{
    Solo,
    FullTable,
}

internal enum TableThemePreference
{
    ClassicGreen,
    MidnightBlue,
    Burgundy,
}

internal sealed record AppPreferences
{
    public decimal Bankroll { get; init; } = 1_000m;

    public decimal SelectedBet { get; init; } = 25m;

    public PlayModePreference PlayMode { get; init; } = PlayModePreference.Solo;

    public int AiSeatCount { get; init; } = 3;

    public TableThemePreference Theme { get; init; } = TableThemePreference.ClassicGreen;

    public bool SoundEnabled { get; init; } = true;

    public bool AnimationsEnabled { get; init; } = true;

    public bool MusicEnabled { get; init; } = true;

    public double MusicVolume { get; init; } = 0.18d;

    public UpdateChannel? UpdateChannel { get; init; }

    public double WindowWidth { get; init; } = 1360d;

    public double WindowHeight { get; init; } = 860d;

    public bool WindowMaximized { get; init; }
}
