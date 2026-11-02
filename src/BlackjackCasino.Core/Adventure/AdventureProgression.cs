using BlackjackCasino.Core.Collections;

namespace BlackjackCasino.Core.Adventure;

public sealed class AdventureMetaProgressState
{
    public long RunsCompleted { get; set; }
    public long ChecksSucceeded { get; set; }
    public long ChecksFailed { get; set; }
    public long NaturalTwenties { get; set; }
    public long NaturalOnes { get; set; }
    public long ItemsFound { get; set; }
    public long EncountersPlayed { get; set; }
    public long EncountersWon { get; set; }
    public long EncountersLost { get; set; }
    public long BlackjackWins { get; set; }
    public long FiveOfAKindWins { get; set; }
    public HashSet<string> EndingKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> CompletedClassIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> RecordedRunIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AdventureMetaProgressState Normalize(AdventureMetaProgressState? value)
    {
        AdventureMetaProgressState state = value ?? new AdventureMetaProgressState();
        state.EndingKeys ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        state.CompletedClassIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        state.RecordedRunIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return state;
    }
}

public sealed record AdventurePrestigeReward(
    string AdventureId,
    string EndingId,
    string TitleId);

public static class AdventurePrestigeCatalog
{
    private static readonly IReadOnlyList<AdventurePrestigeReward> Rewards =
    [
        new("black-stag", "missing-return", "title-keeper-of-names"),
        new("drowned-bell", "debt-paid", "title-bellwarden"),
    ];

    public static IReadOnlyList<AdventurePrestigeReward> All => Rewards;

    public static AdventurePrestigeReward? For(string adventureId, string endingId) =>
        Rewards.FirstOrDefault(reward =>
            string.Equals(reward.AdventureId, adventureId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(reward.EndingId, endingId, StringComparison.OrdinalIgnoreCase));
}

public sealed record AdventureCompletionMetaResult(
    bool Recorded,
    bool NewEnding,
    string EndingKey,
    IReadOnlyList<ChallengeDefinition> NewlyCompletedChallenges,
    CosmeticDefinition? UnlockedTitle);

/// <summary>
/// Bridges story completion into the account-wide progression layer. Recording is idempotent per run id,
/// so reopening a completed save can never pay or count the same run twice.
/// </summary>
public static class AdventureProgressionService
{
    public static string EndingKey(string adventureId, string endingId) => $"{adventureId}:{endingId}";

    public static AdventureCompletionMetaResult RecordCompletion(PlayerProfileState profile, AdventureDefinition definition, AdventureRunState state)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(state);
        profile.Adventure = AdventureMetaProgressState.Normalize(profile.Adventure);

        if (!state.Completed || string.IsNullOrWhiteSpace(state.EndingId))
            return new AdventureCompletionMetaResult(false, false, string.Empty, [], null);

        string endingKey = EndingKey(definition.Id, state.EndingId);
        if (!profile.Adventure.RecordedRunIds.Add(state.RunId))
            return new AdventureCompletionMetaResult(false, false, endingKey, [], null);

        var completedBefore = ChallengeRegistry.All
            .Where(AdventureChallengeCatalog.IsAdventure)
            .Where(challenge => ChallengeRegistry.Complete(profile, challenge))
            .Select(challenge => challenge.Id)
            .ToHashSet(StringComparer.Ordinal);

        AdventureRunStatistics stats = state.Statistics;
        profile.Adventure.RunsCompleted++;
        profile.Adventure.ChecksSucceeded += stats.ChecksSucceeded;
        profile.Adventure.ChecksFailed += stats.ChecksFailed;
        profile.Adventure.NaturalTwenties += stats.NaturalTwenties;
        profile.Adventure.NaturalOnes += stats.NaturalOnes;
        profile.Adventure.ItemsFound += stats.ItemsFound;
        profile.Adventure.EncountersPlayed += stats.EncountersPlayed;
        profile.Adventure.EncountersWon += stats.EncountersWon;
        profile.Adventure.EncountersLost += stats.EncountersLost;
        profile.Adventure.BlackjackWins += stats.BlackjackWins;
        profile.Adventure.FiveOfAKindWins += stats.FiveOfAKindWins;
        profile.Adventure.CompletedClassIds.Add(state.Character.ClassId);
        bool newEnding = profile.Adventure.EndingKeys.Add(endingKey);

        CosmeticDefinition? unlockedTitle = null;
        AdventurePrestigeReward? prestige = AdventurePrestigeCatalog.For(definition.Id, state.EndingId);
        if (prestige is not null && !profile.OwnedCosmeticIds.Contains(prestige.TitleId, StringComparer.Ordinal))
        {
            CosmeticDefinition? title = CosmeticCatalog.TryGet(prestige.TitleId);
            if (title is not null)
            {
                profile.OwnedCosmeticIds.Add(title.Id);
                unlockedTitle = title;
            }
        }

        ChallengeDefinition[] newlyCompleted = ChallengeRegistry.All
            .Where(AdventureChallengeCatalog.IsAdventure)
            .Where(challenge => !completedBefore.Contains(challenge.Id) && ChallengeRegistry.Complete(profile, challenge))
            .ToArray();

        profile.LastPlayedAt = DateTimeOffset.UtcNow;
        return new AdventureCompletionMetaResult(true, newEnding, endingKey, Array.AsReadOnly(newlyCompleted), unlockedTitle);
    }
}

/// <summary>
/// Adventure challenges deliberately use an existing ChallengeDefinition shape. Their metric is routed by id
/// through ChallengeRegistry, so the established challenge reward, claim and pack systems stay authoritative.
/// </summary>
public static class AdventureChallengeCatalog
{
    private static readonly IReadOnlyList<ChallengeDefinition> Challenges = Array.AsReadOnly(new[]
    {
        // Account-wide Adventure milestones.
        A("adventure-first-tale", "The First Tale", "Complete your first Tales from the Backroom adventure.", 1, 125),
        A("adventure-storyteller", "Storyteller", "Complete 5 Adventure runs across any tales and endings.", 5, 350),
        A("adventure-many-roads", "Many Roads", "Discover 5 different Adventure endings.", 5, 450),
        A("adventure-no-stone-unturned", "No Stone Unturned", "Discover 10 different Adventure endings.", 10, 900, true),
        A("adventure-against-house", "Against the House", "Win a Blackjack encounter inside an Adventure.", 1, 150),
        A("adventure-loaded-dice", "Loaded Dice", "Win a Five of a Kind encounter inside an Adventure.", 1, 150),
        A("adventure-natural-talent", "Natural Talent", "Roll a natural 20 during an Adventure check.", 1, 100),
        A("adventure-fates-cruel-joke", "Fate's Cruel Joke", "Roll a natural 1 during an Adventure check.", 1, 75),
        A("adventure-proven-adventurer", "Proven Adventurer", "Succeed on 25 Adventure checks.", 25, 300),
        A("adventure-pack-rat", "Pack Rat", "Acquire 15 items across completed Adventure runs.", 15, 250),
        A("adventure-seasoned-traveler", "Seasoned Traveler", "Complete Adventures with 3 different character classes.", 3, 400),

        // The Black Stag endings.
        E("adventure-black-stag-name-in-ink", "A Name in Ink", "Reach the A Name in Ink ending in The Black Stag.", "black-stag", "name-in-ink", 100),
        E("adventure-black-stag-crown-exposed", "The Crown Exposed", "Reach The Crown Exposed ending in The Black Stag.", "black-stag", "crown-exposed", 175),
        E("adventure-black-stag-survives", "The Black Stag Survives", "Reach The Black Stag Survives ending in The Black Stag.", "black-stag", "black-stag", 175),
        E("adventure-black-stag-missing-return", "The Missing Return", "Reach The Missing Return ending in The Black Stag.", "black-stag", "missing-return", 250),
        E("adventure-black-stag-ashes", "Ashes of the Crown", "Reach Ashes of the Crown ending in The Black Stag.", "black-stag", "ashes-of-the-crown", 150),
        E("adventure-black-stag-quiet-lie", "The Quiet Lie", "Reach The Quiet Lie ending in The Black Stag.", "black-stag", "quiet-lie", 150),
        E("adventure-black-stag-watchful-hand", "The Watchful Hand", "Reach The Watchful Hand ending in The Black Stag.", "black-stag", "watchful-hand", 200),

        // The Drowned Bell endings.
        E("adventure-drowned-bell-silent", "The Silent Bell", "Reach The Silent Bell ending in The Drowned Bell.", "drowned-bell", "silent-bell", 150),
        E("adventure-drowned-bell-remembers", "What the Sea Remembers", "Reach What the Sea Remembers ending in The Drowned Bell.", "drowned-bell", "sea-remembers", 200),
        E("adventure-drowned-bell-debt-paid", "A Debt Paid", "Reach the hidden prestige ending A Debt Paid in The Drowned Bell.", "drowned-bell", "debt-paid", 300),
        E("adventure-drowned-bell-drowned", "The Drowned", "Reach The Drowned ending in The Drowned Bell.", "drowned-bell", "the-drowned", 100),
        E("adventure-drowned-bell-gold", "Gold Beneath Greyhaven", "Reach Gold Beneath Greyhaven ending in The Drowned Bell.", "drowned-bell", "greyhaven-gold", 175),
    });

    private static readonly HashSet<string> Ids = Challenges.Select(challenge => challenge.Id).ToHashSet(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> EndingKeys = new(StringComparer.Ordinal)
    {
        ["adventure-black-stag-name-in-ink"] = AdventureProgressionService.EndingKey("black-stag", "name-in-ink"),
        ["adventure-black-stag-crown-exposed"] = AdventureProgressionService.EndingKey("black-stag", "crown-exposed"),
        ["adventure-black-stag-survives"] = AdventureProgressionService.EndingKey("black-stag", "black-stag"),
        ["adventure-black-stag-missing-return"] = AdventureProgressionService.EndingKey("black-stag", "missing-return"),
        ["adventure-black-stag-ashes"] = AdventureProgressionService.EndingKey("black-stag", "ashes-of-the-crown"),
        ["adventure-black-stag-quiet-lie"] = AdventureProgressionService.EndingKey("black-stag", "quiet-lie"),
        ["adventure-black-stag-watchful-hand"] = AdventureProgressionService.EndingKey("black-stag", "watchful-hand"),
        ["adventure-drowned-bell-silent"] = AdventureProgressionService.EndingKey("drowned-bell", "silent-bell"),
        ["adventure-drowned-bell-remembers"] = AdventureProgressionService.EndingKey("drowned-bell", "sea-remembers"),
        ["adventure-drowned-bell-debt-paid"] = AdventureProgressionService.EndingKey("drowned-bell", "debt-paid"),
        ["adventure-drowned-bell-drowned"] = AdventureProgressionService.EndingKey("drowned-bell", "the-drowned"),
        ["adventure-drowned-bell-gold"] = AdventureProgressionService.EndingKey("drowned-bell", "greyhaven-gold"),
    };

    public static IReadOnlyList<ChallengeDefinition> All => Challenges;
    public static bool IsAdventure(ChallengeDefinition challenge) => challenge is not null && Ids.Contains(challenge.Id);
    public static ChallengeDefinition? TryGet(string id) => Challenges.FirstOrDefault(challenge => string.Equals(challenge.Id, id, StringComparison.Ordinal));

    public static decimal Progress(PlayerProfileState profile, ChallengeDefinition challenge)
    {
        AdventureMetaProgressState state = AdventureMetaProgressState.Normalize(profile.Adventure);
        if (EndingKeys.TryGetValue(challenge.Id, out string? endingKey))
            return state.EndingKeys.Contains(endingKey) ? 1 : 0;

        return challenge.Id switch
        {
            "adventure-first-tale" or "adventure-storyteller" => state.RunsCompleted,
            "adventure-many-roads" or "adventure-no-stone-unturned" => state.EndingKeys.Count,
            "adventure-against-house" => state.BlackjackWins,
            "adventure-loaded-dice" => state.FiveOfAKindWins,
            "adventure-natural-talent" => state.NaturalTwenties,
            "adventure-fates-cruel-joke" => state.NaturalOnes,
            "adventure-proven-adventurer" => state.ChecksSucceeded,
            "adventure-pack-rat" => state.ItemsFound,
            "adventure-seasoned-traveler" => state.CompletedClassIds.Count,
            _ => 0,
        };
    }

    private static ChallengeDefinition A(string id, string name, string description, decimal goal, long coins, bool endgame = false) =>
        new(id, ChallengeCategory.Career, name, description, ChallengeMetric.HandsPlayed, goal, coins, endgame);

    private static ChallengeDefinition E(string id, string name, string description, string adventureId, string endingId, long coins) =>
        new(id, ChallengeCategory.Career, name, description, ChallengeMetric.HandsPlayed, 1, coins);
}

public static class ChallengeRegistry
{
    private static readonly IReadOnlyList<ChallengeDefinition> Challenges =
        ChallengeCatalog.All.Concat(AdventureChallengeCatalog.All).ToArray();

    public static IReadOnlyList<ChallengeDefinition> All => Challenges;

    public static ChallengeDefinition? TryGet(string id) =>
        AdventureChallengeCatalog.TryGet(id) ?? ChallengeCatalog.TryGet(id);

    public static decimal Progress(PlayerProfileState profile, ChallengeDefinition challenge) =>
        AdventureChallengeCatalog.IsAdventure(challenge)
            ? AdventureChallengeCatalog.Progress(profile, challenge)
            : ChallengeCatalog.Progress(profile, challenge);

    public static bool Complete(PlayerProfileState profile, ChallengeDefinition challenge) =>
        Progress(profile, challenge) >= challenge.Goal;

    public static string CategoryName(ChallengeDefinition challenge) =>
        AdventureChallengeCatalog.IsAdventure(challenge) ? "Adventure" : challenge.Category.ToString();
}
