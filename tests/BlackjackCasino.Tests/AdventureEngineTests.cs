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
        Assert.Equal(adventure.Scenes.Count, adventure.Scenes.Select(scene => scene.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(adventure.Scenes.Count >= 15);
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
    public void NaturalOneFailsAndFailureCanMoveStoryForward()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.BlackStag, AdventureCharacterCatalog.Create("warrior", "Test"));
        engine.SelectChoice("open-letter");
        engine.SelectChoice("take-north-road");
        int hp = engine.State.Character.CurrentHitPoints;
        engine.SelectChoice("draw-steel");
        AdventureCheckResult result = engine.ResolvePendingCheck(1);

        Assert.Equal(AdventureCheckOutcome.CriticalFailure, result.Outcome);
        Assert.Equal("wounded-escape", engine.State.CurrentSceneId);
        Assert.Equal(hp - 5, engine.State.Character.CurrentHitPoints);
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
}
