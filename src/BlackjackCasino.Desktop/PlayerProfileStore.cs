using System.Text.Json;
using System.Text.Json.Serialization;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Desktop;

internal static class PlayerProfileStore
{
    private const int HistoryLimit = 1_000;
    private static readonly TimeSpan RoutineSaveDelay = TimeSpan.FromMilliseconds(350);

    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlackjackCasino");

    private static readonly string ProfilePath = Path.Combine(DirectoryPath, "profile.json");
    private static readonly string HistoryPath = Path.Combine(DirectoryPath, "hand-history.json");
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly object ProfileFileWriteGate = new();
    private static readonly object HistoryFileWriteGate = new();
    private static readonly DebouncedPersistenceWriter ProfileWriter = new(
        RoutineSaveDelay,
        payload => WriteSerializedAtomically(ProfilePath, payload, ProfileFileWriteGate));
    private static readonly DebouncedPersistenceWriter<RoundHistoryEntry[]> HistoryWriter = new(
        RoutineSaveDelay,
        WriteHistorySnapshot);

    public static PlayerProfileState LoadProfile()
    {
        FlushProfilePending();

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
        FlushHistoryPending();

        try
        {
            if (!File.Exists(HistoryPath))
            {
                return [];
            }

            using var file = new FileStream(HistoryPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            List<RoundHistoryEntry>? history = JsonSerializer.Deserialize<List<RoundHistoryEntry>>(file, JsonOptions);
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

        try
        {
            // Preserve the legacy normalization timing: callers still receive the sanitized
            // in-memory profile immediately. Only the latest serialized routine snapshot is
            // written to disk, and that I/O no longer blocks the gameplay/UI call path.
            string payload = JsonSerializer.Serialize(SanitizeProfile(state), JsonOptions);
            ProfileWriter.Schedule(payload);
        }
        catch
        {
            // Progression persistence must never interrupt a blackjack hand.
        }
    }

    internal static void SaveProfileOrThrow(PlayerProfileState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        WriteProfileTransaction(SanitizeProfile(state));
    }

    /// <summary>
    /// Persists a transaction candidate that was created from an already-normalized live profile.
    /// Unlike SaveProfileOrThrow this deliberately avoids a second full graph normalization pass,
    /// which would both cost CPU and mutate any read-only subgraphs shared by a scoped snapshot.
    /// </summary>
    internal static void SaveNormalizedProfileOrThrow(PlayerProfileState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        WriteProfileTransaction(state);
    }

    public static void SaveHistory(IEnumerable<RoundHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        try
        {
            // Capture a stable bounded object snapshot on the caller thread. The comparatively
            // expensive JSON serialization and file replacement then happen on the debounced
            // worker, without ever exposing the live mutable history graph to that worker.
            HistoryWriter.Schedule(CreateHistorySnapshot(history));
        }
        catch
        {
            // History persistence is routine and must not interrupt gameplay.
        }
    }

    internal static RoundHistoryEntry[] CreateHistorySnapshot(IEnumerable<RoundHistoryEntry> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        IReadOnlyList<RoundHistoryEntry> source = history as IReadOnlyList<RoundHistoryEntry> ?? history.ToArray();
        if (source.Count == 0)
        {
            return [];
        }

        bool chronological = true;
        for (int i = 1; i < source.Count; i++)
        {
            if (source[i - 1].CompletedAt > source[i].CompletedAt)
            {
                chronological = false;
                break;
            }
        }

        if (!chronological)
        {
            // Preserve the legacy stable ordering semantics for callers that provide arbitrary
            // input. The normal controller path is already chronological and avoids this sort.
            return source
                .OrderBy(x => x.CompletedAt)
                .TakeLast(HistoryLimit)
                .Select(CloneHistoryEntry)
                .ToArray();
        }

        int count = Math.Min(source.Count, HistoryLimit);
        int start = source.Count - count;
        var snapshot = new RoundHistoryEntry[count];
        for (int i = 0; i < count; i++)
        {
            snapshot[i] = CloneHistoryEntry(source[start + i]);
        }
        return snapshot;
    }

    private static RoundHistoryEntry CloneHistoryEntry(RoundHistoryEntry entry) => entry with
    {
        Rules = entry.Rules with { },
        DealerCards = entry.DealerCards.ToArray(),
        PlayerHands = entry.PlayerHands.Select(CloneHistoricalHand).ToArray(),
        Actions = entry.Actions.ToArray(),
        AiSeats = entry.AiSeats.Select(seat => seat with
        {
            Hands = seat.Hands.Select(CloneHistoricalHand).ToArray(),
        }).ToArray(),
    };

    private static HistoricalHand CloneHistoricalHand(HistoricalHand hand) => hand with
    {
        Cards = hand.Cards.ToArray(),
    };

    internal static void FlushPending()
    {
        FlushProfilePending();
        FlushHistoryPending();
    }

    private static void FlushProfilePending()
    {
        ProfileWriter.Flush();
    }

    private static void FlushHistoryPending()
    {
        HistoryWriter.Flush();
    }

    internal static PlayerProfileState SanitizeProfile(PlayerProfileState state)
    {
        ProfileNormalization.Normalize(state);
        state.Level = Math.Clamp(state.Level, 0, ProgressionCurve.MaximumLevel);
        state.Prestige = Math.Max(0, state.Prestige);
        state.Coins = Math.Max(0, state.Coins);
        state.LifetimeExperience = Math.Max(0, state.LifetimeExperience);
        state.TotalPlaytimeSeconds = Math.Max(0, state.TotalPlaytimeSeconds);
        state.TowerDefense ??= new BlackjackCasino.Core.TowerDefense.TdProgress();

        state.OwnedCosmeticIds = state.OwnedCosmeticIds
            .Where(id => CosmeticCatalog.TryGet(id) is not null)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        // ChallengeRegistry is the authoritative catalog. Adventure achievements are intentionally
        // registered outside the legacy ChallengeCatalog, so filtering against ChallengeCatalog here
        // silently deleted their claim receipts during every save and made their rewards repeatable.
        state.ClaimedChallengeIds = state.ClaimedChallengeIds
            .Where(id => ChallengeRegistry.TryGet(id) is not null)
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

    private static void WriteProfileTransaction(PlayerProfileState state)
    {
        // Prevent an older deferred profile snapshot from landing after this transactional save.
        FlushProfilePending();
        lock (ProfileFileWriteGate)
        {
            Directory.CreateDirectory(DirectoryPath);
            string temporary = ProfilePath + ".tmp";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, state, JsonOptions);
            }
            File.Move(temporary, ProfilePath, overwrite: true);
        }
    }

    private static void WriteHistorySnapshot(RoundHistoryEntry[] snapshot)
    {
        lock (HistoryFileWriteGate)
        {
            Directory.CreateDirectory(DirectoryPath);
            string temporary = HistoryPath + ".tmp";
            using (var file = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(file, snapshot, JsonOptions);
            }
            File.Move(temporary, HistoryPath, overwrite: true);
        }
    }

    private static void WriteSerializedAtomically(string path, string payload, object writeGate)
    {
        lock (writeGate)
        {
            Directory.CreateDirectory(DirectoryPath);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, payload);
            File.Move(temporary, path, overwrite: true);
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
