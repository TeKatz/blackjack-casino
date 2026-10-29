using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;

namespace BlackjackCasino.Desktop;

internal static class PlayerProfileStore
{
    private const int HistoryLimit = 1_000;

    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino");

    private static readonly string ProfilePath = Path.Combine(DirectoryPath, "profile.json");
    private static readonly string HistoryPath = Path.Combine(DirectoryPath, "hand-history.json");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static PlayerProfileState LoadProfile()
    {
        try
        {
            if (!File.Exists(ProfilePath))
            {
                return new PlayerProfileState();
            }

            string json = File.ReadAllText(ProfilePath);
            PlayerProfileState? state = JsonSerializer.Deserialize<PlayerProfileState>(json, JsonOptions);
            return SanitizeProfile(state ?? new PlayerProfileState());
        }
        catch
        {
            return new PlayerProfileState();
        }
    }

    public static IReadOnlyList<RoundHistoryEntry> LoadHistory()
    {
        try
        {
            if (!File.Exists(HistoryPath))
            {
                return [];
            }

            string json = File.ReadAllText(HistoryPath);
            List<RoundHistoryEntry>? history = JsonSerializer.Deserialize<List<RoundHistoryEntry>>(json, JsonOptions);
            return history?
                .Where(x => x.RoundId != Guid.Empty)
                .OrderBy(x => x.CompletedAt)
                .TakeLast(HistoryLimit)
                .ToArray() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void SaveProfile(PlayerProfileState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        WriteJsonAtomically(ProfilePath, SanitizeProfile(state));
    }

    internal static void SaveProfileOrThrow(PlayerProfileState state)
    {
        Directory.CreateDirectory(DirectoryPath);
        string temporary = ProfilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(SanitizeProfile(state), JsonOptions));
        File.Move(temporary, ProfilePath, overwrite: true);
    }

    public static void SaveHistory(IEnumerable<RoundHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        RoundHistoryEntry[] snapshot = history
            .OrderBy(x => x.CompletedAt)
            .TakeLast(HistoryLimit)
            .ToArray();
        WriteJsonAtomically(HistoryPath, snapshot);
    }

    private static PlayerProfileState SanitizeProfile(PlayerProfileState state)
    {
        state.Level = Math.Clamp(state.Level, 0, ProgressionCurve.MaximumLevel);
        state.Prestige = Math.Max(0, state.Prestige);
        state.Coins = Math.Max(0, state.Coins);
        state.LifetimeExperience = Math.Max(0, state.LifetimeExperience);
        state.TotalPlaytimeSeconds = Math.Max(0, state.TotalPlaytimeSeconds);
        state.Manor ??= new BlackjackCasino.Core.Manor.ManorStatistics();
        state.Slots ??= new SlotStatistics();
        state.LifetimeStatistics ??= new StatisticsSnapshot();
        state.TableStatistics ??= new Dictionary<string, StatisticsSnapshot>(StringComparer.OrdinalIgnoreCase);
        state.Seasons ??= new Dictionary<string, SeasonProgressState>(StringComparer.OrdinalIgnoreCase);
        state.OwnedCosmeticIds ??= [];
        state.ClaimedChallengeIds ??= [];

        state.OwnedCosmeticIds = state.OwnedCosmeticIds
            .Where(id => CosmeticCatalog.TryGet(id) is not null)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        state.ClaimedChallengeIds = state.ClaimedChallengeIds
            .Where(id => ChallengeCatalog.TryGet(id) is not null)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (state.Level >= ProgressionCurve.MaximumLevel)
        {
            state.Level = ProgressionCurve.MaximumLevel;
            state.LevelExperience = 0;
        }
        else
        {
            state.LevelExperience = Math.Clamp(
                state.LevelExperience,
                0,
                Math.Max(0, ProgressionCurve.ExperienceForNextLevel(state.Level) - 1));
        }

        foreach ((string key, SeasonProgressState value) in state.Seasons.ToArray())
        {
            SeasonDefinition? season = SeasonCatalog.All.FirstOrDefault(x => string.Equals(x.Id, key, StringComparison.Ordinal));
            if (season is null)
            {
                state.Seasons.Remove(key);
                continue;
            }

            value.Experience = Math.Max(0, value.Experience);
            value.Tier = Math.Clamp(value.Tier, 0, season.Tiers.Count);
            value.SeasonId = key;
        }

        _ = new ProgressionService(state);
        return state;
    }

    private static void WriteJsonAtomically<T>(string path, T value)
    {
        try
        {
            Directory.CreateDirectory(DirectoryPath);
            string temporary = path + ".tmp";
            string json = JsonSerializer.Serialize(value, JsonOptions);
            File.WriteAllText(temporary, json);
            File.Move(temporary, path, overwrite: true);
        }
        catch
        {
            // Progression persistence must never interrupt a blackjack hand.
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
