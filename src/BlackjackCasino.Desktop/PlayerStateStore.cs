using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class PlayerStateStore
{
    private const decimal DefaultBankroll = 1_000m;

    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino");

    private static readonly string StatePath = Path.Combine(DirectoryPath, "player-state.json");

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static AppPreferences Load()
    {
        try
        {
            if (!File.Exists(StatePath))
            {
                return new AppPreferences();
            }

            string json = File.ReadAllText(StatePath);
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
            Directory.CreateDirectory(DirectoryPath);
            string json = JsonSerializer.Serialize(Sanitize(state), JsonOptions);
            File.WriteAllText(StatePath, json);
        }
        catch
        {
            // A failed local save must never interrupt a blackjack hand.
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

        return state with
        {
            Bankroll = bankroll,
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
