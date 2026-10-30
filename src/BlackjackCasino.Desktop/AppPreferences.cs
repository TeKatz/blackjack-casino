using BlackjackCasino.Core;
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
    public BlackjackCasino.Core.Kniffel.KniffelTableState? KniffelTable { get; init; }
    public bool KniffelCasinoSelected { get; init; }
    public BlackjackCasino.Core.Kniffel.KniffelState Kniffel { get; init; } = new();

    public BlackjackCasino.Core.Sudoku.SudokuProgressState Sudoku { get; init; } = new();
    public bool SudokuShowTimer { get; init; } = true;
    public bool SudokuShowGuides { get; init; } = true;

    public BlackjackCasino.Core.Empire.EmpireState Empire { get; init; } = new();

    public decimal Bankroll { get; init; } = 1_000m;
    public Guid LastAppliedVaultPayoutId { get; init; }

    public decimal ManorBet { get; init; } = 25m;
    public SlotSpinSpeed ManorSpeed { get; init; }
    public AutoSpinSettings ManorAuto { get; init; } = new();
    public bool ManorInteractive { get; init; } = true;

    public decimal SlotBet { get; init; } = 25m;

    public decimal NeonVaultBet { get; init; } = 100m;

    public decimal NineLivesBet { get; init; } = 10m;

    public decimal SelectedBet { get; init; } = 25m;

    public PlayModePreference PlayMode { get; init; } = PlayModePreference.Solo;

    public int AiSeatCount { get; init; } = 3;

    public TablePresetId TablePreset { get; init; } = TablePresetId.Casual;

    public GameRules CustomRules { get; init; } = TablePresets.Get(TablePresetId.Casual).Rules with { };

    public TableThemePreference Theme { get; init; } = TableThemePreference.ClassicGreen;

    public bool SoundEnabled { get; init; } = true;

    public bool AnimationsEnabled { get; init; } = true;

    public bool ShowEmpireBalance { get; init; } = true;

    public bool MusicEnabled { get; init; } = true;

    public double MusicVolume { get; init; } = 0.10d;

    public UpdateChannel? UpdateChannel { get; init; }

    public double WindowWidth { get; init; } = 1360d;

    public double WindowHeight { get; init; } = 860d;

    public bool WindowMaximized { get; init; }
}
