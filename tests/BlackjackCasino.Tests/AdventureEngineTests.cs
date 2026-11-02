using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class AdventureEngineTests
{
    [Fact]
    public void CatalogAdventureIsValidAndUsesStableSceneIds()
    {
        AdventureDefinition adventure = AdventureCatalog.BlackStag;
        AdventureDefinitionValidator.Validate(adventure);
        Assert.Equal("black-stag", adventure.Id);
        Assert.Equal(2, adventure.Version);
        Assert.Equal(adventure.Scenes.Count, adventure.Scenes.Select(scene => scene.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(adventure.Scenes.Count >= 60);
        Assert.Equal(5, adventure.Acts?.Count);
        Assert.Contains(adventure.Scenes, scene => string.Equals(scene.Speaker, "Isi", StringComparison.Ordinal));
        Assert.Contains(adventure.Items ?? [], item => item.Id == "brass-key");
        Assert.Contains(adventure.Items ?? [], item => item.Id == "rope");
        Assert.Contains(adventure.Items ?? [], item => item.Id == "healing-potion");
        Assert.Contains(adventure.Conditions ?? [], condition => condition.Id == "poisoned");
        Assert.Contains(adventure.Conditions ?? [], condition => condition.Id == "bleeding");
    }

    [Fact]
    public void AdventureDefinitionsRoundTripThroughJsonContentBoundary()
    {
        string json = AdventureContentJson.Serialize(AdventureCatalog.BlackStag);
        AdventureDefinition restored = AdventureContentJson.Deserialize(json);
        Assert.Equal(AdventureCatalog.BlackStag.Id, restored.Id);
        Assert.Equal(AdventureCatalog.BlackStag.Version, restored.Version);
        Assert.Equal(AdventureCatalog.BlackStag.Scenes.Count, restored.Scenes.Count);
        Assert.Equal(AdventureCatalog.BlackStag.StartSceneId, restored.StartSceneId);
        Assert.Equal(AdventureCatalog.BlackStag.Items?.Count, restored.Items?.Count);
        Assert.Equal(AdventureCatalog.BlackStag.Conditions?.Count, restored.Conditions?.Count);
        Assert.Equal(AdventureCatalog.BlackStag.Acts?.Count, restored.Acts?.Count);
    }

    [Fact]
    public void InvestigationChoiceWaitsForExplicitD20Roll()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.BlackStag, AdventureCharacterCatalog.Create("rogue", "Test"));
        engine.SelectChoice("inspect-seal");
        Assert.NotNull(engine.PendingCheck);
        Assert.Equal("crooked-crown", engine.State.CurrentSceneId);
        Assert.Equal("inspect-seal", engine.State.PendingChoiceId);

        AdventureCheckResult result = engine.ResolvePendingCheck(9);
        Assert.True(result.Succeeded);
        Assert.Equal(13, result.Total);
        Assert.Equal("forged-seal", engine.State.CurrentSceneId);
        Assert.Null(engine.State.PendingChoiceId);
        Assert.Equal(1, engine.State.RollsMade);
    }

    [Fact]
    public void NaturalOneFailsMovesStoryForwardAndAppliesBleeding()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.BlackStag, AdventureCharacterCatalog.Create("warrior", "Test"));
        engine.SelectChoice("open-letter");
        engine.SelectChoice("take-north-road");
        int hp = engine.State.Character.CurrentHitPoints;
        engine.SelectChoice("draw-steel");
        AdventureCheckResult result = engine.ResolvePendingCheck(1);

        Assert.Equal(AdventureCheckOutcome.CriticalFailure, result.Outcome);
        Assert.Equal("wounded-escape", engine.State.CurrentSceneId);
        Assert.Equal(hp - 4, engine.State.Character.CurrentHitPoints);
        Assert.True(engine.State.ActiveConditions.ContainsKey("bleeding"));
        Assert.Equal(4, engine.State.ActiveConditions["bleeding"].Remaining);
    }

    [Fact]
    public void NaturalTwentyUsesCriticalSuccessRoute()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.BlackStag, AdventureCharacterCatalog.Create("rogue", "Test"));
        engine.SelectChoice("open-letter");
        engine.SelectChoice("take-north-road");
        engine.SelectChoice("draw-steel");
        AdventureCheckResult result = engine.ResolvePendingCheck(20);
        Assert.Equal(AdventureCheckOutcome.CriticalSuccess, result.Outcome);
        Assert.Equal("hunter-disarmed", engine.State.CurrentSceneId);
        Assert.Equal(1, engine.State.NaturalTwenties);
    }

    [Fact]
    public void ConditionalEndingAppearsOnlyAfterItsWorldFlagExists()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.BlackStag, AdventureCharacterCatalog.Create("cleric", "Test"));
        engine.SelectChoice("open-letter");
        engine.SelectChoice("take-north-road");
        engine.SelectChoice("talk-hunter");
        _ = engine.ResolvePendingCheck(20);
        engine.SelectChoice("hear-hunter");
        engine.SelectChoice("take-ledger-warning");
        string[] choices = engine.AvailableChoices().Select(choice => choice.Id).ToArray();
        Assert.Contains("verify-first", choices);
        Assert.Contains("keep-ledger", choices);
        Assert.DoesNotContain("follow-stag", choices);
    }

    [Fact]
    public void AdvantageAndDisadvantageChooseTheCorrectDie()
    {
        AdventureCharacterState character = AdventureCharacterCatalog.Create("rogue", "Test");
        var advantage = new AdventureCheckDefinition("adv", AdventureAbility.Dexterity, 10, RollMode: AdventureRollMode.Advantage);
        var disadvantage = advantage with { Id = "dis", RollMode = AdventureRollMode.Disadvantage };
        Assert.Equal(18, AdventureCheckResolver.Resolve(advantage, character, 4, 18).ChosenRoll);
        Assert.Equal(4, AdventureCheckResolver.Resolve(disadvantage, character, 4, 18).ChosenRoll);
    }

    [Fact]
    public void BlueprintItemsCoinsConditionsAndFeedbackAreAdventureAgnostic()
    {
        AdventureEngine engine = AdventureEngine.StartNew(BuildBlueprintAdventure(), AdventureCharacterCatalog.Create("rogue", "Test"));
        Assert.Equal(20, engine.State.Coins);

        engine.SelectChoice("take-cache");

        Assert.Equal("danger", engine.State.CurrentSceneId);
        Assert.Equal(25, engine.State.Coins);
        Assert.Equal(1, engine.State.ItemStacks["rope"].Quantity);
        Assert.True(engine.State.ActiveConditions.ContainsKey("poisoned"));
        Assert.Equal(AdventureRollMode.Disadvantage, engine.PendingCheck is null
            ? SelectAndGetRollMode(engine)
            : engine.PendingCheck.RollMode);

        AdventureFeedbackEvent[] feedback = engine.DrainFeedback().ToArray();
        Assert.Contains(feedback, item => item.Kind == AdventureFeedbackKind.ItemAcquired && item.Key == "rope");
        Assert.Contains(feedback, item => item.Kind == AdventureFeedbackKind.CoinsChanged && item.Delta == 5);
        Assert.Contains(feedback, item => item.Kind == AdventureFeedbackKind.ConditionApplied && item.Key == "poisoned");
    }

    [Fact]
    public void BleedingTicksAcrossScenesAndExpiresWithFeedback()
    {
        AdventureEngine engine = AdventureEngine.StartNew(BuildBleedingAdventure(), AdventureCharacterCatalog.Create("warrior", "Test"));
        int hp = engine.State.Character.CurrentHitPoints;

        engine.SelectChoice("get-cut");
        Assert.Equal("middle", engine.State.CurrentSceneId);
        Assert.Equal(2, engine.State.ActiveConditions["bleeding"].Remaining);

        engine.SelectChoice("continue");
        Assert.Equal(hp - 1, engine.State.Character.CurrentHitPoints);
        Assert.Equal(1, engine.State.ActiveConditions["bleeding"].Remaining);

        engine.SelectChoice("finish");
        Assert.Equal(hp - 2, engine.State.Character.CurrentHitPoints);
        Assert.False(engine.State.ActiveConditions.ContainsKey("bleeding"));
        Assert.Contains(engine.DrainFeedback(), item => item.Kind == AdventureFeedbackKind.ConditionRemoved && item.Key == "bleeding");
    }

    [Fact]
    public void EndingCompletesRunAndAppliesCharacterExperience()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.BlackStag, AdventureCharacterCatalog.Create("rogue", "Test"));
        engine.SelectChoice("open-letter");
        engine.SelectChoice("take-north-road");
        engine.SelectChoice("draw-steel");
        _ = engine.ResolvePendingCheck(20);
        engine.SelectChoice("take-ledger-clean");
        engine.SelectChoice("keep-ledger");
        Assert.True(engine.State.Completed);
        Assert.Equal("name-in-ink", engine.State.EndingId);
        Assert.Equal(100, engine.State.Character.Experience);
    }

    private static AdventureRollMode SelectAndGetRollMode(AdventureEngine engine)
    {
        engine.SelectChoice("risk-check");
        return engine.PendingCheck!.RollMode;
    }

    private static AdventureDefinition BuildBlueprintAdventure() => new(
        "blueprint-test",
        1,
        "Blueprint",
        "Engine systems",
        "start",
        [
            new AdventureSceneDefinition("start", "Start", "Find a cache.", Choices:
            [
                new AdventureChoiceDefinition("take-cache", "Take it.", "danger", Effects:
                [
                    new AdventureEffect(AdventureEffectKind.AddItem, "rope"),
                    new AdventureEffect(AdventureEffectKind.ChangeCoins, Value: 5),
                    new AdventureEffect(AdventureEffectKind.ApplyCondition, "poisoned", 3),
                ]),
            ]),
            new AdventureSceneDefinition("danger", "Danger", "The poison makes the next task harder.", Choices:
            [
                new AdventureChoiceDefinition("risk-check", "Try anyway.", Check:
                    new AdventureCheckDefinition("risk", AdventureAbility.Dexterity, 12, "stealth", SuccessSceneId: "end", FailureSceneId: "end")),
            ]),
            new AdventureSceneDefinition("end", "End", "Done.", IsEnding: true),
        ],
        [new AdventureItemDefinition("rope", "Rope", "Useful rope.", "rope", AdventureItemKind.Utility)],
        [new AdventureStatusDefinition("poisoned", "Poisoned", "Checks are harder.", "poison", DisadvantageOnAbilityChecks: true)]);

    private static AdventureDefinition BuildBleedingAdventure() => new(
        "bleeding-test",
        1,
        "Bleeding",
        "Duration test",
        "start",
        [
            new AdventureSceneDefinition("start", "Start", "A blade catches you.", Choices:
            [
                new AdventureChoiceDefinition("get-cut", "Continue.", "middle", Effects:
                    [new AdventureEffect(AdventureEffectKind.ApplyCondition, "bleeding", 2)]),
            ]),
            new AdventureSceneDefinition("middle", "Middle", "You are still bleeding.", Choices:
            [new AdventureChoiceDefinition("continue", "Continue.", "last")]),
            new AdventureSceneDefinition("last", "Last", "The wound is slowing.", Choices:
            [new AdventureChoiceDefinition("finish", "Finish.", "end")]),
            new AdventureSceneDefinition("end", "End", "Done.", IsEnding: true),
        ],
        Conditions:
        [new AdventureStatusDefinition("bleeding", "Bleeding", "Lose health as scenes advance.", "blood", PeriodicHitPointDelta: -1)]);
}
