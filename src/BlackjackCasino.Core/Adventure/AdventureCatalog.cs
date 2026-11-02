namespace BlackjackCasino.Core.Adventure;

public static class AdventureCharacterCatalog
{
    private static readonly IReadOnlyList<AdventureClassDefinition> Classes =
    [
        new("warrior", "Warrior", "Durable, direct and dangerous when a situation turns physical.", 22, 16,
            Stats(16, 12, 15, 10, 11, 10), Skills(("athletics", 2), ("intimidation", 2), ("survival", 1))),
        new("rogue", "Rogue", "Quick hands, sharp eyes and several ways around a locked door.", 16, 14,
            Stats(10, 16, 12, 14, 12, 13), Skills(("stealth", 2), ("investigation", 2), ("persuasion", 2), ("sleight-of-hand", 2))),
        new("mage", "Mage", "Fragile, observant and exceptionally good at understanding the impossible.", 14, 12,
            Stats(8, 14, 12, 16, 13, 10), Skills(("arcana", 2), ("investigation", 2), ("perception", 2), ("history", 2))),
        new("cleric", "Cleric", "Steady under pressure, perceptive and difficult to deceive.", 18, 15,
            Stats(12, 10, 14, 10, 16, 13), Skills(("insight", 2), ("perception", 2), ("persuasion", 2), ("religion", 2))),
    ];

    public static IReadOnlyList<AdventureClassDefinition> All => Classes;

    public static IReadOnlyList<AdventureClassDefinition> AllFor(AdventureDefinition? adventure) =>
        adventure?.CharacterCatalog?.Classes is { Count: > 0 } custom ? custom : Classes;

    public static AdventureCharacterState Create(string classId, string? name = null) =>
        CreateFrom(Classes, null, classId, name);

    public static AdventureCharacterState Create(AdventureDefinition adventure, string classId, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(adventure);
        return CreateFrom(AllFor(adventure), adventure.CharacterCatalog, classId, name);
    }

    public static (string Name, AdventureAbility Ability) Skill(string id) => Skill(null, id);

    public static (string Name, AdventureAbility Ability) Skill(AdventureDefinition? adventure, string id)
    {
        AdventureSkillDefinition? custom = adventure?.CharacterCatalog?.Skills?.FirstOrDefault(
            skill => string.Equals(skill.Id, id, StringComparison.OrdinalIgnoreCase));
        if (custom is not null) return (custom.Name, custom.Ability);

        return id.ToLowerInvariant() switch
        {
            "athletics" => ("Athletics", AdventureAbility.Strength),
            "acrobatics" => ("Acrobatics", AdventureAbility.Dexterity),
            "stealth" => ("Stealth", AdventureAbility.Dexterity),
            "sleight-of-hand" => ("Sleight of Hand", AdventureAbility.Dexterity),
            "investigation" => ("Investigation", AdventureAbility.Intelligence),
            "arcana" => ("Arcana", AdventureAbility.Intelligence),
            "history" => ("History", AdventureAbility.Intelligence),
            "religion" => ("Religion", AdventureAbility.Intelligence),
            "perception" => ("Perception", AdventureAbility.Wisdom),
            "insight" => ("Insight", AdventureAbility.Wisdom),
            "survival" => ("Survival", AdventureAbility.Wisdom),
            "persuasion" => ("Persuasion", AdventureAbility.Charisma),
            "intimidation" => ("Intimidation", AdventureAbility.Charisma),
            _ => (Humanize(id), AdventureAbility.Intelligence),
        };
    }

    public static string AbilityLabel(AdventureDefinition? adventure, AdventureAbility ability)
    {
        if (adventure?.CharacterCatalog?.AbilityLabels?.TryGetValue(ability, out string? custom) == true && !string.IsNullOrWhiteSpace(custom))
            return custom;
        return ability.ToString();
    }

    public static string AbilityShortLabel(AdventureDefinition? adventure, AdventureAbility ability)
    {
        if (adventure?.CharacterCatalog?.AbilityShortLabels?.TryGetValue(ability, out string? custom) == true && !string.IsNullOrWhiteSpace(custom))
            return custom;

        return ability switch
        {
            AdventureAbility.Strength => "STR",
            AdventureAbility.Dexterity => "DEX",
            AdventureAbility.Constitution => "CON",
            AdventureAbility.Intelligence => "INT",
            AdventureAbility.Wisdom => "WIS",
            AdventureAbility.Charisma => "CHA",
            _ => ability.ToString().ToUpperInvariant(),
        };
    }

    public static void Validate(AdventureCharacterCatalogDefinition catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (catalog.Classes.Count == 0)
            throw new ArgumentException("Adventure-specific character catalogs require at least one class.", nameof(catalog));

        var classIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdventureClassDefinition definition in catalog.Classes)
        {
            if (string.IsNullOrWhiteSpace(definition.Id) || !classIds.Add(definition.Id))
                throw new ArgumentException($"Adventure character catalog contains a blank or duplicate class id '{definition.Id}'.", nameof(catalog));
            if (string.IsNullOrWhiteSpace(definition.Name))
                throw new ArgumentException($"Adventure class '{definition.Id}' requires a display name.", nameof(catalog));
            if (definition.MaxHitPoints < 1)
                throw new ArgumentException($"Adventure class '{definition.Id}' requires positive hit points.", nameof(catalog));
        }

        var skillIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (AdventureSkillDefinition skill in catalog.Skills ?? [])
        {
            if (string.IsNullOrWhiteSpace(skill.Id) || !skillIds.Add(skill.Id))
                throw new ArgumentException($"Adventure character catalog contains a blank or duplicate skill id '{skill.Id}'.", nameof(catalog));
            if (string.IsNullOrWhiteSpace(skill.Name))
                throw new ArgumentException($"Adventure skill '{skill.Id}' requires a display name.", nameof(catalog));
        }
    }

    private static AdventureCharacterState CreateFrom(
        IReadOnlyList<AdventureClassDefinition> classes,
        AdventureCharacterCatalogDefinition? catalog,
        string classId,
        string? name)
    {
        AdventureClassDefinition definition = classes.FirstOrDefault(
            item => string.Equals(item.Id, classId, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Unknown adventure class '{classId}'.", nameof(classId));

        var character = new AdventureCharacterState
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(),
            ClassId = definition.Id,
            ClassName = definition.Name,
            MaxHitPoints = definition.MaxHitPoints,
            CurrentHitPoints = definition.MaxHitPoints,
            Armor = definition.Armor,
            Abilities = definition.Abilities.ToDictionary(pair => pair.Key, pair => pair.Value),
        };

        foreach ((string id, int bonus) in definition.SkillProficiencies)
        {
            AdventureSkillDefinition? customSkill = catalog?.Skills?.FirstOrDefault(
                skill => string.Equals(skill.Id, id, StringComparison.OrdinalIgnoreCase));
            (string displayName, AdventureAbility ability) = customSkill is null
                ? Skill(id)
                : (customSkill.Name, customSkill.Ability);
            character.Skills[id] = new AdventureSkillState
            {
                Id = id,
                Name = displayName,
                Ability = ability,
                ProficiencyBonus = bonus,
            };
        }

        return character;
    }

    private static IReadOnlyDictionary<AdventureAbility, int> Stats(int strength, int dexterity, int constitution, int intelligence, int wisdom, int charisma) =>
        new Dictionary<AdventureAbility, int>
        {
            [AdventureAbility.Strength] = strength,
            [AdventureAbility.Dexterity] = dexterity,
            [AdventureAbility.Constitution] = constitution,
            [AdventureAbility.Intelligence] = intelligence,
            [AdventureAbility.Wisdom] = wisdom,
            [AdventureAbility.Charisma] = charisma,
        };

    private static IReadOnlyDictionary<string, int> Skills(params (string Id, int Bonus)[] values) =>
        values.ToDictionary(value => value.Id, value => value.Bonus, StringComparer.OrdinalIgnoreCase);

    private static string Humanize(string value) =>
        string.IsNullOrWhiteSpace(value) ? "Skill" : char.ToUpperInvariant(value[0]) + value[1..].Replace('-', ' ');
}

public static class AdventureCatalog
{
    private static readonly AdventureDefinition BlackStagDefinition = BlackStagAdventure.Build();
    private static readonly AdventureDefinition DrownedBellDefinition = DrownedBellAdventure.Build();
    private static readonly AdventureDefinition StrangersWagerDefinition = StrangersWagerAdventure.Build();

    // Stranger's Wager remains temporarily registered while the reusable encounter framework is being exercised.
    private static readonly IReadOnlyList<AdventureDefinition> Adventures =
    [
        BlackStagDefinition,
        DrownedBellDefinition,
        StrangersWagerDefinition,
    ];

    public static IReadOnlyList<AdventureDefinition> All => Adventures;
    public static AdventureDefinition BlackStag => BlackStagDefinition;
    public static AdventureDefinition DrownedBell => DrownedBellDefinition;
    public static AdventureDefinition StrangersWager => StrangersWagerDefinition;

    public static bool TryGet(string id, out AdventureDefinition? adventure)
    {
        adventure = Adventures.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        return adventure is not null;
    }
}
