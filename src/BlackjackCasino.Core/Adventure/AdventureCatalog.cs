namespace BlackjackCasino.Core.Adventure;

public static class AdventureCharacterCatalog
{
    private static readonly IReadOnlyList<AdventureClassDefinition> Classes =
    [
        new("warrior", "Warrior", "Durable, direct and dangerous when a situation turns physical.", 22, 16,
            Stats(16, 12, 15, 10, 11, 10), Skills(("athletics", 2), ("intimidation", 2))),
        new("rogue", "Rogue", "Quick hands, sharp eyes and several ways around a locked door.", 16, 14,
            Stats(10, 16, 12, 14, 12, 13), Skills(("stealth", 2), ("investigation", 2), ("persuasion", 2))),
        new("mage", "Mage", "Fragile, observant and exceptionally good at understanding the impossible.", 14, 12,
            Stats(8, 14, 12, 16, 13, 10), Skills(("arcana", 2), ("investigation", 2), ("perception", 2))),
        new("cleric", "Cleric", "Steady under pressure, perceptive and difficult to deceive.", 18, 15,
            Stats(12, 10, 14, 10, 16, 13), Skills(("insight", 2), ("perception", 2), ("persuasion", 2))),
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
        "investigation" => ("Investigation", AdventureAbility.Intelligence),
        "arcana" => ("Arcana", AdventureAbility.Intelligence),
        "perception" => ("Perception", AdventureAbility.Wisdom),
        "insight" => ("Insight", AdventureAbility.Wisdom),
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
    private static readonly IReadOnlyList<AdventureDefinition> Adventures = [BuildBlackStag()];

    public static IReadOnlyList<AdventureDefinition> All => Adventures;
    public static AdventureDefinition BlackStag => Adventures[0];

    public static bool TryGet(string id, out AdventureDefinition? adventure)
    {
        adventure = Adventures.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        return adventure is not null;
    }

    private static AdventureDefinition BuildBlackStag()
    {
        var scenes = new List<AdventureSceneDefinition>
        {
            new("crooked-crown", "The Crooked Crown",
                "Rain needles the windows of the nearly empty inn. The keeper sets a sealed letter beside your cup. \"A stranger paid well to make sure this reached you.\" The black wax carries the impression of a stag with broken antlers.",
                "Innkeeper",
                [
                    new("open-letter", "Break the seal and read the letter.", "letter-opened", Effects: [new(AdventureEffectKind.AddItem, "black-stag-letter")]),
                    new("inspect-seal", "Inspect the seal before opening it.", Check: new AdventureCheckDefinition("seal-investigation", AdventureAbility.Intelligence, 13, "investigation", SuccessSceneId: "forged-seal", FailureSceneId: "seal-uncertain"), Effects: [new(AdventureEffectKind.SetFlag, "inspected-seal")]),
                    new("question-keeper", "Watch the keeper carefully and ask who delivered it.", Check: new AdventureCheckDefinition("keeper-insight", AdventureAbility.Wisdom, 12, "insight", SuccessSceneId: "keeper-truth", FailureSceneId: "keeper-guarded")),
                ]),
            new("forged-seal", "A False Signet",
                "The wax is genuine black sealing wax, but the impression is not. Tiny doubled lines around the antlers reveal a cast copy of a signet. Whoever sent this wanted the symbol recognized without owning the original.",
                Choices:
                [
                    new("open-after-inspect", "Open the letter.", "letter-opened", Effects: [new(AdventureEffectKind.SetFlag, "seal-forged"), new(AdventureEffectKind.AddItem, "black-stag-letter")]),
                    new("press-keeper", "Show the flaw to the innkeeper and press him for details.", "keeper-truth", Effects: [new(AdventureEffectKind.ChangeNpcRelation, "innkeeper", 1)]),
                ]),
            new("seal-uncertain", "Cold Wax",
                "You turn the seal beneath the lamplight, but find nothing you can trust. The keeper's eyes remain fixed on the rag in his hands.",
                Choices:
                [
                    new("open-uncertain", "Open the letter anyway.", "letter-opened", Effects: [new(AdventureEffectKind.AddItem, "black-stag-letter")]),
                    new("ask-uncertain", "Ask the keeper what he remembers.", "keeper-guarded"),
                ]),
            new("keeper-truth", "What the Keeper Saw",
                "\"Didn't see his face,\" the keeper admits. \"Grey travelling coat. Mud to the knees. But the horse had silver tack, expensive stuff. He kept looking toward the old north road.\" He lowers his voice. \"And he was afraid of being followed.\"",
                "Innkeeper",
                [new("thank-keeper", "Thank him and read the letter.", "letter-opened", Effects: [new(AdventureEffectKind.SetFlag, "knows-silver-tack"), new(AdventureEffectKind.ChangeNpcRelation, "innkeeper", 1), new(AdventureEffectKind.AddItem, "black-stag-letter")])]),
            new("keeper-guarded", "A Short Memory",
                "\"Traveller. Hood up. Paid in silver.\" The answer arrives too quickly. Whether from fear or irritation, the keeper has decided that is all you are getting.",
                "Innkeeper",
                [new("leave-keeper", "Stop pushing and read the letter.", "letter-opened", Effects: [new(AdventureEffectKind.AddItem, "black-stag-letter")])]),
            new("letter-opened", "The Invitation",
                "The letter contains only three lines: \"North road. Saint Orra's bridge. Before midnight. Come alone if you want the missing names.\" Beneath them is a charcoal sketch of the same broken-antlered stag.",
                Choices:
                [
                    new("take-north-road", "Leave for Saint Orra's bridge.", "north-road"),
                    new("search-for-mark", "Search your memory for the broken-antler mark.", Check: new AdventureCheckDefinition("stag-arcana", AdventureAbility.Intelligence, 14, "arcana", SuccessSceneId: "remembered-mark", FailureSceneId: "north-road")),
                ]),
            new("remembered-mark", "An Older Story",
                "You remember the mark from a banned ledger: the Black Stag was once used by royal couriers carrying testimony that could not safely enter the courts. The network supposedly died twenty years ago.",
                Choices: [new("go-informed", "Keep the memory to yourself and take the north road.", "north-road", Effects: [new(AdventureEffectKind.SetFlag, "knows-black-stag")])]),
            new("north-road", "Saint Orra's Bridge",
                "The bridge rises from the fog like a broken rib. A hooded courier waits beside the parapet. Before either of you can speak, boots scrape stone behind you. A hunter in a dark coat blocks the road back, one hand already on his sword.",
                Choices:
                [
                    new("draw-steel", "Meet the hunter head-on.", Check: new AdventureCheckDefinition("bridge-fight", AdventureAbility.Strength, 14, "athletics", SuccessSceneId: "hunter-defeated", FailureSceneId: "wounded-escape", CriticalSuccessSceneId: "hunter-disarmed"), FailureEffects: [new(AdventureEffectKind.ChangeHitPoints, Value: -5)]),
                    new("dive-cover", "Vault the parapet and use the lower maintenance ledge.", Check: new AdventureCheckDefinition("bridge-acrobatics", AdventureAbility.Dexterity, 13, "acrobatics", SuccessSceneId: "lower-ledge", FailureSceneId: "wounded-escape"), FailureEffects: [new(AdventureEffectKind.ChangeHitPoints, Value: -3)]),
                    new("talk-hunter", "\"If you wanted the courier dead, you would have struck already.\"", Check: new AdventureCheckDefinition("bridge-persuasion", AdventureAbility.Charisma, 15, "persuasion", SuccessSceneId: "hunter-hesitates", FailureSceneId: "wounded-escape")),
                ]),
            new("hunter-disarmed", "Steel in the Fog",
                "The hunter commits too early. You turn the strike aside and send his sword spinning across the stones. The courier stares at you, then laughs once in disbelief.",
                Choices: [new("take-ledger-clean", "Demand the truth from the courier.", "ledger-revealed", Effects: [new(AdventureEffectKind.SetFlag, "hunter-disarmed")])]),
            new("hunter-defeated", "A Narrow Opening",
                "The exchange is fast and ugly, but you force the hunter back far enough for the courier to reach you. \"No time,\" she says. \"Take this.\"", "Courier",
                [new("take-ledger", "Take what she is carrying.", "ledger-revealed")]),
            new("lower-ledge", "Below the Bridge",
                "You drop beneath the parapet as steel flashes overhead. The courier follows, landing badly but keeping hold of a leather packet. A maintenance door waits ten paces away.",
                Choices:
                [
                    new("force-door", "Force the swollen maintenance door.", Check: new AdventureCheckDefinition("maintenance-strength", AdventureAbility.Strength, 12, SuccessSceneId: "ledger-revealed", FailureSceneId: "wounded-escape"), SuccessEffects: [new(AdventureEffectKind.SetFlag, "escaped-below")]),
                    new("take-packet", "Take the packet and run the ledge.", "ledger-revealed"),
                ]),
            new("hunter-hesitates", "The Question Behind the Blade",
                "The hunter's grip loosens by a fraction. \"You don't know what she's carrying.\" The courier answers before you can: \"Names. Judges, officers, merchants. Everyone who bought a place in the disappearances.\"",
                Choices:
                [
                    new("choose-courier", "Side with the courier.", "ledger-revealed", Effects: [new(AdventureEffectKind.SetFlag, "spared-hunter")]),
                    new("hear-hunter", "Ask the hunter why the list must disappear.", "hunter-warning"),
                ]),
            new("hunter-warning", "A Dangerous Ledger",
                "\"Because half the names are bait,\" the hunter says. \"The real list is shorter. Someone wants a purge, not justice.\" The courier says nothing, which is answer enough.",
                Choices: [new("take-ledger-warning", "Take the ledger. Decide after you verify it.", "ledger-revealed", Effects: [new(AdventureEffectKind.SetFlag, "knows-ledger-may-be-false")])]),
            new("wounded-escape", "Blood on the Stones",
                "The plan collapses into noise. You escape the hunter, but only just. When the fog finally swallows the bridge, the courier is beside you and there is blood on your sleeve.",
                Choices: [new("continue-wounded", "Keep moving. Ask what this was all for.", "ledger-revealed", Effects: [new(AdventureEffectKind.SetFlag, "wounded-at-bridge")])]),
            new("ledger-revealed", "The Missing Names",
                "Inside the courier's packet is a ledger filled with dates, payments and names — including people officially declared dead years ago. One entry is fresh. Tonight's date. Your own name is written beside it.",
                Choices:
                [
                    new("verify-first", "Do not trust either side. Keep the ledger and verify the names yourself.", "ending-watchful", Conditions: [new AdventureCondition(AdventureConditionKind.Flag, "knows-ledger-may-be-false")]),
                    new("follow-stag", "Accept the courier's offer to meet the remaining Black Stag network.", "ending-stag", Conditions: [new AdventureCondition(AdventureConditionKind.Flag, "knows-black-stag")]),
                    new("keep-ledger", "Take the ledger and disappear before either side can reclaim it.", "ending-ledger"),
                ]),
            new("ending-watchful", "Ending — The Watchful Hand",
                "You trust the evidence only as far as you can test it. By dawn, the ledger is hidden, the first name is under observation and both factions believe you fled the city. For now, that is exactly what you need.",
                IsEnding: true, EndingId: "watchful-hand", OnEnterEffects: [new(AdventureEffectKind.GainCharacterExperience, Value: 120)]),
            new("ending-stag", "Ending — The Black Stag",
                "At sunrise you follow the courier beyond the old toll house. Three riders wait beneath the trees, each wearing the broken-antler sign. The network is not dead. It has simply been waiting for someone willing to carry the truth again.",
                IsEnding: true, EndingId: "black-stag", OnEnterEffects: [new(AdventureEffectKind.GainCharacterExperience, Value: 150)]),
            new("ending-ledger", "Ending — A Name in Ink",
                "You leave with the ledger and no promises. Somewhere behind you, the Black Stag and its hunter begin searching for the same missing book. This time, however, you are the one choosing where the trail leads.",
                IsEnding: true, EndingId: "name-in-ink", OnEnterEffects: [new(AdventureEffectKind.GainCharacterExperience, Value: 100)]),
        };

        var adventure = new AdventureDefinition("black-stag", 1, "The Black Stag",
            "A rain-soaked mystery about a dead courier network and a list of missing names.", "crooked-crown", scenes);
        AdventureDefinitionValidator.Validate(adventure);
        return adventure;
    }
}
