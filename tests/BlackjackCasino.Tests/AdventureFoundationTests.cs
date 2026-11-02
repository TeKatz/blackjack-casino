using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class AdventureFoundationTests
{
    [Fact]
    public void LegacyAdventuresKeepTheSharedCharacterCatalog()
    {
        IReadOnlyList<AdventureClassDefinition> classes = AdventureCharacterCatalog.AllFor(AdventureCatalog.BlackStag);

        Assert.Equal(AdventureCharacterCatalog.All.Count, classes.Count);
        Assert.Contains(classes, item => item.Id == "rogue");
        AdventureCharacterState character = AdventureCharacterCatalog.Create(AdventureCatalog.BlackStag, "rogue", "Test");
        Assert.Equal("Rogue", character.ClassName);
    }

    [Fact]
    public void AdventureSpecificCatalogCreatesCustomArchetypeAndSkillBinding()
    {
        AdventureDefinition adventure = BuildNoirFoundation();
        AdventureCharacterCatalog.Validate(adventure.CharacterCatalog!);

        AdventureCharacterState character = AdventureCharacterCatalog.Create(adventure, "private-eye", "Marlowe");

        Assert.Equal("Private Eye", character.ClassName);
        Assert.Equal(18, character.MaxHitPoints);
        Assert.Equal(3, character.SkillBonus("investigation"));
        Assert.Equal(AdventureAbility.Wisdom, character.Skills["investigation"].Ability);
        Assert.Equal("Insight", AdventureCharacterCatalog.AbilityLabel(adventure, AdventureAbility.Wisdom));
        Assert.Equal("NERVE", AdventureCharacterCatalog.AbilityShortLabel(adventure, AdventureAbility.Constitution));
    }

    [Fact]
    public void CaseBoardInitializesAuthoredLayoutAndSupportsNarrativeStateChanges()
    {
        AdventureDefinition adventure = BuildNoirFoundation();
        CaseBoardDefinition board = adventure.CaseBoard!;
        CaseBoardState state = AdventureCaseBoard.Initialize(board);

        Assert.False(state.Nodes["victim"].IsKnown);
        Assert.Equal(160, state.Layout.NodePositions["victim"].X);
        Assert.Equal(CaseConnectionStatus.Suspected, state.Connections["victim-ledger"].Status);

        AdventureCaseBoard.DiscoverNode(board, state, "victim", "opening");
        AdventureCaseBoard.DiscoverNode(board, state, "ledger", "casino-office");
        AdventureCaseBoard.RevealConnection(board, state, "victim-ledger", "casino-office");
        AdventureCaseBoard.SetLeadState(board, state, "who-called", CaseLeadState.Resolved, "phone-records");
        AdventureCaseBoard.SetTheoryState(board, state, "inside-job", CaseTheoryState.Supported, "archive");

        Assert.True(state.Nodes["victim"].IsKnown);
        Assert.Equal("opening", state.Nodes["victim"].DiscoveredAtSceneId);
        Assert.True(state.Connections["victim-ledger"].IsKnown);
        Assert.Equal(CaseLeadState.Resolved, state.Nodes["who-called"].LeadState);
        Assert.Equal(CaseTheoryState.Supported, state.Nodes["inside-job"].TheoryState);
    }

    [Fact]
    public void AdventureRunNormalizationUpgradesOldSavesWithEmptyCaseBoardSafely()
    {
        var legacy = new AdventureRunState
        {
            SchemaVersion = 3,
            AdventureId = "black-stag",
            CurrentSceneId = AdventureCatalog.BlackStag.StartSceneId,
            Character = AdventureCharacterCatalog.Create("rogue", "Legacy"),
            CaseBoard = null!,
        };

        AdventureRunState normalized = AdventureRunState.Normalize(legacy);

        Assert.Equal(4, normalized.SchemaVersion);
        Assert.NotNull(normalized.CaseBoard);
        Assert.Empty(normalized.CaseBoard.Nodes);
        Assert.Equal(1d, normalized.CaseBoard.Layout.Zoom);
    }

    [Fact]
    public void AdventureSpecificCatalogAndCaseBoardRoundTripThroughJson()
    {
        AdventureDefinition adventure = BuildNoirFoundation();

        string json = AdventureContentJson.Serialize(adventure);
        AdventureDefinition restored = AdventureContentJson.Deserialize(json);

        Assert.NotNull(restored.CharacterCatalog);
        Assert.Equal("Private Eye", restored.CharacterCatalog!.Classes.Single().Name);
        Assert.Equal("Brawn", restored.CharacterCatalog.AbilityLabels![AdventureAbility.Strength]);
        Assert.NotNull(restored.CaseBoard);
        Assert.Equal(5, restored.CaseBoard!.Nodes.Count);
        Assert.Single(restored.CaseBoard.Connections!);
        CaseBoardDefinitionValidator.Validate(restored.CaseBoard);
    }

    private static AdventureDefinition BuildNoirFoundation()
    {
        var characterCatalog = new AdventureCharacterCatalogDefinition(
            [
                new AdventureClassDefinition(
                    "private-eye",
                    "Private Eye",
                    "A patient investigator with a nose for contradictions.",
                    18,
                    12,
                    new Dictionary<AdventureAbility, int>
                    {
                        [AdventureAbility.Strength] = 11,
                        [AdventureAbility.Dexterity] = 13,
                        [AdventureAbility.Constitution] = 14,
                        [AdventureAbility.Intelligence] = 15,
                        [AdventureAbility.Wisdom] = 16,
                        [AdventureAbility.Charisma] = 12,
                    },
                    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["investigation"] = 3,
                        ["streetwise"] = 2,
                    },
                    "hunch",
                    "Hunch",
                    ["detective"]),
            ],
            new Dictionary<AdventureAbility, string>
            {
                [AdventureAbility.Strength] = "Brawn",
                [AdventureAbility.Dexterity] = "Reflex",
                [AdventureAbility.Constitution] = "Nerve",
                [AdventureAbility.Intelligence] = "Intellect",
                [AdventureAbility.Wisdom] = "Insight",
                [AdventureAbility.Charisma] = "Presence",
            },
            new Dictionary<AdventureAbility, string>
            {
                [AdventureAbility.Strength] = "BRAWN",
                [AdventureAbility.Dexterity] = "REFLEX",
                [AdventureAbility.Constitution] = "NERVE",
                [AdventureAbility.Intelligence] = "INT",
                [AdventureAbility.Wisdom] = "INSIGHT",
                [AdventureAbility.Charisma] = "PRES",
            },
            [
                new AdventureSkillDefinition("investigation", "Investigation", AdventureAbility.Wisdom),
                new AdventureSkillDefinition("streetwise", "Streetwise", AdventureAbility.Charisma),
            ],
            "hunch",
            "Hunch");

        var board = new CaseBoardDefinition(
            [
                new CaseNodeDefinition("victim", CaseNodeCategory.Person, "Arthur Vane", "The missing industrialist.", X: 160, Y: 120),
                new CaseNodeDefinition("ledger", CaseNodeCategory.Evidence, "Casino Ledger", "A coded ledger from the Ivory Room.", X: 520, Y: 180),
                new CaseNodeDefinition("pier", CaseNodeCategory.Location, "Pier 17", "A rain-soaked freight pier.", X: 340, Y: 390),
                new CaseNodeDefinition("who-called", CaseNodeCategory.Lead, "Who called room 306?", "The hotel switchboard log has a missing line.", X: 760, Y: 120),
                new CaseNodeDefinition("inside-job", CaseNodeCategory.Theory, "Someone inside the department erased the report.", "A working theory, not yet a fact.", X: 700, Y: 390),
            ],
            [
                new CaseConnectionDefinition("victim-ledger", "victim", "ledger", CaseConnectionType.Financial, Label: "payments"),
            ],
            "Vane Case");

        return new AdventureDefinition(
            "noir-foundation-test",
            1,
            "Dead Man's Hand",
            "Foundation test",
            "opening",
            [new AdventureSceneDefinition("opening", "Opening", "Rain on the office glass.", IsEnding: true)],
            CharacterCatalog: characterCatalog,
            CaseBoard: board);
    }
}
