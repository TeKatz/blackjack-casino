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

    public static AdventureCharacterState Create(string classId, string? name = null)
    {
        AdventureClassDefinition definition = Classes.FirstOrDefault(
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
            (string displayName, AdventureAbility ability) = Skill(id);
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

    public static (string Name, AdventureAbility Ability) Skill(string id) => id.ToLowerInvariant() switch
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
    private static readonly IReadOnlyList<AdventureDefinition> Adventures = [BlackStagAdventure.Build()];

    public static IReadOnlyList<AdventureDefinition> All => Adventures;
    public static AdventureDefinition BlackStag => Adventures[0];

    public static bool TryGet(string id, out AdventureDefinition? adventure)
    {
        adventure = Adventures.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        return adventure is not null;
    }
}
