using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class PlayerStateStore
{
    private const decimal DefaultBankroll = 1_000m;
    private static readonly TimeSpan RoutineSaveDelay = TimeSpan.FromMilliseconds(350);

    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino");

    private static readonly string StatePath = Path.Combine(DirectoryPath, "player-state.json");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly object FileWriteGate = new();
    private static readonly DebouncedPersistenceWriter RoutineWriter = new(
        RoutineSaveDelay,
        payload => WriteSerializedAtomically(payload, DirectoryPath));

    public static AppPreferences Load(string? directory = null)
    {
        // Preserve read-after-save semantics for the normal store even though routine writes are
        // now debounced. Custom directories are used by transactional/test paths and are not
        // serviced by the routine writer.
        if (directory is null)
        {
            FlushPending();
        }

        string path = directory is null ? StatePath : Path.Combine(directory, "player-state.json");
        try
        {
            if (!File.Exists(path))
            {
                return new AppPreferences();
            }

            string json = File.ReadAllText(path);
            AppPreferences? state = JsonSerializer.Deserialize<AppPreferences>(json, JsonOptions);
            return Sanitize(state ?? new AppPreferences());
        }
        catch
        {
            return new AppPreferences();
        }
    }

    public static void Save(AppPreferences state)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            // Serialize the snapshot on the caller thread so mutable nested preference state is
            // observed consistently. Only the latest payload in a short burst reaches disk, and
            // the actual file I/O runs on the timer thread instead of the WPF dispatcher.
            string payload = JsonSerializer.Serialize(Sanitize(state), JsonOptions);
            RoutineWriter.Schedule(payload);
        }
        catch
        {
            // A failed local save must never interrupt a blackjack hand.
        }
    }

    // Cash dividends and their consumed reserve must survive as a single durable snapshot.
    internal static void SaveOrThrow(AppPreferences state, string? directory = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (directory is null)
        {
            // An older deferred routine save must never land after a newer durable transaction.
            FlushPending();
            directory = DirectoryPath;
        }

        WriteAtomically(state, directory, durable: true);
    }

    internal static void FlushPending()
    {
        RoutineWriter.Flush();
    }

    private static void WriteAtomically(AppPreferences state, string directory, bool durable)
    {
        lock (FileWriteGate)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "player-state.json");
            string temporary = path + ".tmp";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, Sanitize(state), JsonOptions);
                if (durable)
                {
                    // SaveOrThrow is used for transactional money/progression boundaries where the
                    // caller only commits the in-memory state after durable persistence succeeds.
                    file.Flush(flushToDisk: true);
                }
            }
            File.Move(temporary, path, overwrite: true);
        }
    }

    private static void WriteSerializedAtomically(string payload, string directory)
    {
        lock (FileWriteGate)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "player-state.json");
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, payload);
            File.Move(temporary, path, overwrite: true);
        }
    }

    public static decimal LoadBankroll() => Load().Bankroll;

    public static void SaveBankroll(decimal bankroll)
    {
        AppPreferences state = Load() with { Bankroll = Math.Max(0m, bankroll) };
        Save(state);
    }

    private static AppPreferences Sanitize(AppPreferences state)
    {
        decimal bankroll = state.Bankroll >= 0m ? state.Bankroll : DefaultBankroll;
        decimal selectedBet = Math.Clamp(state.SelectedBet, 0m, bankroll);
        int aiSeats = Math.Clamp(state.AiSeatCount, 1, 5);
        double width = double.IsFinite(state.WindowWidth) ? Math.Clamp(state.WindowWidth, 1080d, 3840d) : 1360d;
        double height = double.IsFinite(state.WindowHeight) ? Math.Clamp(state.WindowHeight, 720d, 2160d) : 860d;
        double musicVolume = double.IsFinite(state.MusicVolume) ? Math.Clamp(state.MusicVolume, 0d, 0.32d) : 0.10d;

        PlayModePreference playMode = Enum.IsDefined(state.PlayMode)
            ? state.PlayMode
            : PlayModePreference.Solo;
        TableThemePreference theme = Enum.IsDefined(state.Theme)
            ? state.Theme
            : TableThemePreference.ClassicGreen;
        TablePresetId preset = Enum.IsDefined(state.TablePreset)
            ? state.TablePreset
            : TablePresetId.Casual;

        GameRules customRules = SanitizeRules(state.CustomRules);

        return state.InitializePresentationAreas() with
        {
            MidnightMerge = BlackjackCasino.Core.MidnightMerge.MergeGameState.Normalize(state.MidnightMerge),
            Empire = BlackjackCasino.Core.Empire.EmpireState.Normalize(state.Empire),
            Kniffel = BlackjackCasino.Core.Kniffel.KniffelGame.Normalize(state.Kniffel),
            Sudoku = BlackjackCasino.Core.Sudoku.SudokuProgressState.Normalize(state.Sudoku),
            Jigsaw = NormalizeJigsaw(state.Jigsaw),
            Bankroll = bankroll,
            ManorBet = Math.Clamp(decimal.Truncate(state.ManorBet), 1, 1_000),
            ManorSpeed = Enum.IsDefined(state.ManorSpeed) ? state.ManorSpeed : SlotSpinSpeed.Normal,
            ManorAuto = (state.ManorAuto ?? new AutoSpinSettings()).Sanitize(),
            SelectedBet = selectedBet,
            SlotBet = Math.Clamp(decimal.Round(state.SlotBet, 2), SlotMachineRules.MinimumBet, SlotMachineRules.MaximumBet),
            PlayMode = playMode,
            AiSeatCount = aiSeats,
            TablePreset = preset,
            CustomRules = customRules,
            Theme = theme,
            MusicVolume = musicVolume,
            WindowWidth = width,
            WindowHeight = height,
        };
    }

    private static BlackjackCasino.Core.Jigsaw.JigsawProgressState NormalizeJigsaw(BlackjackCasino.Core.Jigsaw.JigsawProgressState? progress)
    {
        progress ??= new();
        progress.Normalize();
        return progress;
    }

    private static GameRules SanitizeRules(GameRules? rules)
    {
        GameRules fallback = TablePresets.Get(TablePresetId.Casual).Rules with { };
        if (rules is null)
        {
            return fallback;
        }

        try
        {
            rules.Validate();
            return rules;
        }
        catch
        {
            return fallback;
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
