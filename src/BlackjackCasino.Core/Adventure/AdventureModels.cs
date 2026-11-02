namespace BlackjackCasino.Core.Adventure;

public enum AdventureAbility
{
    Strength,
    Dexterity,
    Constitution,
    Intelligence,
    Wisdom,
    Charisma,
}

public enum AdventureRollMode
{
    Normal,
    Advantage,
    Disadvantage,
}

public enum AdventureCheckOutcome
{
    CriticalFailure,
    Failure,
    Success,
    CriticalSuccess,
}

public enum AdventureConditionKind
{
    Flag,
    ItemOwned,
    AbilityMinimum,
    SkillMinimum,
    NpcRelationMinimum,
    ActiveCondition,
    CoinsMinimum,
    CounterMinimum,
}

public enum AdventureEffectKind
{
    SetFlag,
    AddItem,
    RemoveItem,
    ChangeNpcRelation,
    ChangeHitPoints,
    GainCharacterExperience,
    ChangeCoins,
    ChangeCounter,
    ApplyCondition,
    RemoveCondition,
}

public enum AdventureItemKind
{
    Quest,
    Consumable,
    Utility,
    Relic,
}

public enum AdventureDurationUnit
{
    Scene,
    Turn,
    Permanent,
}

public enum AdventureFeedbackKind
{
    ItemAcquired,
    ItemRemoved,
    ConditionApplied,
    ConditionRemoved,
    CoinsChanged,
    HitPointsChanged,
    Information,
}

public sealed record AdventureCondition(
    AdventureConditionKind Kind,
    string Key = "",
    int Value = 0,
    bool Expected = true,
    AdventureAbility? Ability = null);

public sealed record AdventureEffect(
    AdventureEffectKind Kind,
    string Key = "",
    int Value = 0,
    bool FlagValue = true);

public sealed record AdventureCheckDefinition(
    string Id,
    AdventureAbility Ability,
    int Difficulty,
    string? SkillId = null,
    int Modifier = 0,
    AdventureRollMode RollMode = AdventureRollMode.Normal,
    string? SuccessSceneId = null,
    string? FailureSceneId = null,
    string? CriticalSuccessSceneId = null,
    string? CriticalFailureSceneId = null);

public sealed record AdventureChoiceDefinition(
    string Id,
    string Text,
    string? NextSceneId = null,
    IReadOnlyList<AdventureCondition>? Conditions = null,
    AdventureCheckDefinition? Check = null,
    IReadOnlyList<AdventureEffect>? Effects = null,
    IReadOnlyList<AdventureEffect>? SuccessEffects = null,
    IReadOnlyList<AdventureEffect>? FailureEffects = null,
    string? LocalizationKey = null);

public sealed record AdventureSceneDefinition(
    string Id,
    string Title,
    string Body,
    string? Speaker = null,
    IReadOnlyList<AdventureChoiceDefinition>? Choices = null,
    IReadOnlyList<AdventureEffect>? OnEnterEffects = null,
    bool IsEnding = false,
    string? EndingId = null,
    string? TitleLocalizationKey = null,
    string? BodyLocalizationKey = null,
    string? ActId = null,
    string? Location = null);

public sealed record AdventureItemDefinition(
    string Id,
    string Name,
    string Description,
    string IconKey,
    AdventureItemKind Kind,
    int MaxStack = 1);

public sealed record AdventureStatusDefinition(
    string Id,
    string Name,
    string Description,
    string IconKey,
    AdventureDurationUnit DurationUnit = AdventureDurationUnit.Scene,
    bool Beneficial = false,
    bool Stackable = false,
    bool DisadvantageOnAbilityChecks = false,
    int PeriodicHitPointDelta = 0);

public sealed record AdventureDefinition(
    string Id,
    int Version,
    string Title,
    string Subtitle,
    string StartSceneId,
    IReadOnlyList<AdventureSceneDefinition> Scenes,
    IReadOnlyList<AdventureItemDefinition>? Items = null,
    IReadOnlyList<AdventureStatusDefinition>? Conditions = null,
    IReadOnlyDictionary<string, string>? Acts = null,
    AdventureCharacterCatalogDefinition? CharacterCatalog = null,
    CaseBoardDefinition? CaseBoard = null);

public sealed class AdventureSkillState
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public AdventureAbility Ability { get; set; }
    public int ProficiencyBonus { get; set; }
}

public sealed class AdventureCharacterState
{
    public string Name { get; set; } = "Player";
    public string ClassId { get; set; } = "rogue";
    public string ClassName { get; set; } = "Rogue";
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int MaxHitPoints { get; set; } = 16;
    public int CurrentHitPoints { get; set; } = 16;
    public int Armor { get; set; } = 12;
    public Dictionary<AdventureAbility, int> Abilities { get; set; } = new();
    public Dictionary<string, AdventureSkillState> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public int AbilityScore(AdventureAbility ability) =>
        Abilities.TryGetValue(ability, out int score) ? score : 10;

    public int AbilityModifier(AdventureAbility ability)
    {
        int score = AbilityScore(ability);
        return (int)Math.Floor((score - 10) / 2d);
    }

    public int SkillBonus(string? skillId) =>
        !string.IsNullOrWhiteSpace(skillId) &&
        Skills.TryGetValue(skillId, out AdventureSkillState? skill)
            ? skill.ProficiencyBonus
            : 0;
}

public sealed class AdventureNpcState
{
    public int Relation { get; set; }
    public bool Alive { get; set; } = true;
    public HashSet<string> KnownFacts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AdventureItemState
{
    public string ItemId { get; set; } = "";
    public int Quantity { get; set; } = 1;
}

public sealed class AdventureStatusState
{
    public string ConditionId { get; set; } = "";
    public AdventureDurationUnit Unit { get; set; } = AdventureDurationUnit.Scene;
    public int Remaining { get; set; } = 1;
    public int Stacks { get; set; } = 1;
    public string? Source { get; set; }
}

public sealed record AdventureFeedbackEvent(
    AdventureFeedbackKind Kind,
    string Key,
    string Title,
    string Detail,
    int Delta = 0);

public sealed class AdventureRunStatistics
{
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public int ScenesVisited { get; set; }
    public HashSet<string> UniqueSceneIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int ChoicesMade { get; set; }
    public int ChecksSucceeded { get; set; }
    public int ChecksFailed { get; set; }
    public int NaturalTwenties { get; set; }
    public int NaturalOnes { get; set; }
    public int DamageTaken { get; set; }
    public int HealingReceived { get; set; }
    public int ItemsFound { get; set; }
    public int ItemsConsumed { get; set; }
    public int CoinsEarned { get; set; }
    public int CoinsSpent { get; set; }
    public int ConditionsReceived { get; set; }
    public int EncountersPlayed { get; set; }
    public int EncountersWon { get; set; }
    public int EncountersLost { get; set; }
    public int EncountersDrawn { get; set; }
    public int BlackjackWins { get; set; }
    public int FiveOfAKindWins { get; set; }

    public static AdventureRunStatistics Normalize(AdventureRunStatistics? value)
    {
        AdventureRunStatistics stats = value ?? new AdventureRunStatistics();
        stats.UniqueSceneIds ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (stats.StartedAtUtc == default) stats.StartedAtUtc = DateTimeOffset.UtcNow;
        return stats;
    }
}

public sealed class AdventureRunState
{
    public int SchemaVersion { get; set; } = 4;
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");
    public string AdventureId { get; set; } = "";
    public int AdventureVersion { get; set; } = 1;
    public string CurrentSceneId { get; set; } = "";
    public AdventureCharacterState Character { get; set; } = new();
    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Legacy inventory mirror retained so old adventure saves can migrate without data loss.
    public HashSet<string> Inventory { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AdventureItemState> ItemStacks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AdventureStatusState> ActiveConditions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Counters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AdventureFeedbackEvent> PendingFeedback { get; set; } = [];
    public int Coins { get; set; } = 20;

    public Dictionary<string, AdventureNpcState> Npcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public CaseBoardState CaseBoard { get; set; } = new();
    public string? PendingChoiceId { get; set; }
    public bool Completed { get; set; }
    public string? EndingId { get; set; }
    public int RollsMade { get; set; }
    public int NaturalTwenties { get; set; }
    public int NaturalOnes { get; set; }
    public AdventureRunStatistics Statistics { get; set; } = new();

    public static AdventureRunState Normalize(AdventureRunState? value)
    {
        AdventureRunState state = value ?? new AdventureRunState();
        state.SchemaVersion = Math.Max(4, state.SchemaVersion);
        state.RunId = string.IsNullOrWhiteSpace(state.RunId) ? Guid.NewGuid().ToString("N") : state.RunId;
        state.AdventureId ??= "";
        state.CurrentSceneId ??= "";
        state.Character ??= new AdventureCharacterState();
        state.Character.Name ??= "Player";
        state.Character.ClassId ??= "rogue";
        state.Character.ClassName ??= "Rogue";
        state.Character.Abilities ??= new Dictionary<AdventureAbility, int>();
        state.Character.Skills ??= new Dictionary<string, AdventureSkillState>(StringComparer.OrdinalIgnoreCase);
        state.Flags ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        state.Inventory ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        state.ItemStacks ??= new Dictionary<string, AdventureItemState>(StringComparer.OrdinalIgnoreCase);
        state.ActiveConditions ??= new Dictionary<string, AdventureStatusState>(StringComparer.OrdinalIgnoreCase);
        state.Counters ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        state.PendingFeedback ??= [];
        state.Npcs ??= new Dictionary<string, AdventureNpcState>(StringComparer.OrdinalIgnoreCase);
        state.CaseBoard = CaseBoardState.Normalize(state.CaseBoard);
        state.Statistics = AdventureRunStatistics.Normalize(state.Statistics);

        foreach (string legacyItem in state.Inventory)
        {
            if (!state.ItemStacks.ContainsKey(legacyItem))
                state.ItemStacks[legacyItem] = new AdventureItemState { ItemId = legacyItem, Quantity = 1 };
        }
        state.Inventory = new HashSet<string>(
            state.ItemStacks.Where(pair => pair.Value.Quantity > 0).Select(pair => pair.Key),
            StringComparer.OrdinalIgnoreCase);

        state.Coins = Math.Max(0, state.Coins);
        state.Character.CurrentHitPoints = Math.Clamp(state.Character.CurrentHitPoints, 0, Math.Max(1, state.Character.MaxHitPoints));
        return state;
    }
}

public sealed class AdventureProgressState
{
    public int SchemaVersion { get; set; } = 4;
    public string? LastAdventureId { get; set; }
    public Dictionary<string, AdventureRunState> Runs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AdventureProgressState Normalize(AdventureProgressState? value)
    {
        AdventureProgressState state = value ?? new AdventureProgressState();
        state.SchemaVersion = Math.Max(4, state.SchemaVersion);
        state.Runs ??= new Dictionary<string, AdventureRunState>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in state.Runs.Keys.ToArray())
            state.Runs[key] = AdventureRunState.Normalize(state.Runs[key]);
        return state;
    }
}

public sealed record AdventureCheckResult(
    int FirstRoll,
    int? SecondRoll,
    int ChosenRoll,
    int AbilityModifier,
    int SkillBonus,
    int AdditionalModifier,
    int Total,
    int Difficulty,
    AdventureCheckOutcome Outcome)
{
    public bool Succeeded => Outcome is AdventureCheckOutcome.Success or AdventureCheckOutcome.CriticalSuccess;
}

public sealed record AdventureClassDefinition(
    string Id,
    string Name,
    string Description,
    int MaxHitPoints,
    int Armor,
    IReadOnlyDictionary<AdventureAbility, int> Abilities,
    IReadOnlyDictionary<string, int> SkillProficiencies,
    string? SpecialMechanicId = null,
    string? SpecialMechanicName = null,
    IReadOnlyList<string>? RouteTags = null);

public sealed record AdventureSkillDefinition(
    string Id,
    string Name,
    AdventureAbility Ability);

public sealed record AdventureCharacterCatalogDefinition(
    IReadOnlyList<AdventureClassDefinition> Classes,
    IReadOnlyDictionary<AdventureAbility, string>? AbilityLabels = null,
    IReadOnlyDictionary<AdventureAbility, string>? AbilityShortLabels = null,
    IReadOnlyList<AdventureSkillDefinition>? Skills = null,
    string? SpecialResourceId = null,
    string? SpecialResourceName = null);
