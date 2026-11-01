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
}

public enum AdventureEffectKind
{
    SetFlag,
    AddItem,
    RemoveItem,
    ChangeNpcRelation,
    ChangeHitPoints,
    GainCharacterExperience,
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
    string? BodyLocalizationKey = null);

public sealed record AdventureDefinition(
    string Id,
    int Version,
    string Title,
    string Subtitle,
    string StartSceneId,
    IReadOnlyList<AdventureSceneDefinition> Scenes);

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

public sealed class AdventureRunState
{
    public int SchemaVersion { get; set; } = 1;
    public string AdventureId { get; set; } = "";
    public int AdventureVersion { get; set; } = 1;
    public string CurrentSceneId { get; set; } = "";
    public AdventureCharacterState Character { get; set; } = new();
    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> Inventory { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AdventureNpcState> Npcs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? PendingChoiceId { get; set; }
    public bool Completed { get; set; }
    public string? EndingId { get; set; }
    public int RollsMade { get; set; }
    public int NaturalTwenties { get; set; }
    public int NaturalOnes { get; set; }

    public static AdventureRunState Normalize(AdventureRunState? value)
    {
        AdventureRunState state = value ?? new AdventureRunState();
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
        state.Npcs ??= new Dictionary<string, AdventureNpcState>(StringComparer.OrdinalIgnoreCase);
        state.Character.CurrentHitPoints = Math.Clamp(state.Character.CurrentHitPoints, 0, Math.Max(1, state.Character.MaxHitPoints));
        return state;
    }
}

public sealed class AdventureProgressState
{
    public int SchemaVersion { get; set; } = 1;
    public string? LastAdventureId { get; set; }
    public Dictionary<string, AdventureRunState> Runs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static AdventureProgressState Normalize(AdventureProgressState? value)
    {
        AdventureProgressState state = value ?? new AdventureProgressState();
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
    IReadOnlyDictionary<string, int> SkillProficiencies);
