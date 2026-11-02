using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class AdventureEncounterFrameworkTests
{
    [Fact]
    public void FiveOfAKindEncounterIsRegisteredAndValid()
    {
        AdventureDefinition adventure = AdventureCatalog.StrangersWager;

        AdventureDefinitionValidator.Validate(adventure);
        AdventureEncounterCatalog.Validate(adventure);

        Assert.Equal(2, adventure.Version);
        Assert.True(AdventureEncounterCatalog.TryGet(adventure.Id, "five-kind-table", out AdventureEncounterDefinition? encounter));
        Assert.NotNull(encounter);
        Assert.Equal(AdventureEncounterType.FiveOfAKind, encounter!.Type);
        Assert.NotNull(encounter.FiveOfAKind);
        Assert.Equal("dice-encounter-win", encounter.WinChoiceId);
        Assert.Equal("dice-encounter-loss", encounter.LossChoiceId);
        Assert.Equal("dice-encounter-draw", encounter.DrawChoiceId);
    }

    [Fact]
    public void FiveOfAKindWinRoutesThroughOrdinaryAdventureEffects()
    {
        AdventureEngine engine = AdventureEngine.StartNew(
            AdventureCatalog.StrangersWager,
            AdventureCharacterCatalog.Create("rogue", "Test"));

        engine.SelectChoice("take-seat");
        engine.SelectChoice("choose-dice");
        Assert.Equal("five-kind-table", engine.State.CurrentSceneId);
        Assert.True(AdventureEncounterCatalog.TryGet(engine.Definition.Id, engine.CurrentScene.Id, out AdventureEncounterDefinition? encounter));

        engine.SelectChoice(encounter!.WinChoiceId);

        Assert.True(engine.State.Completed);
        Assert.Equal("raven-token-dice", engine.State.EndingId);
        Assert.Equal(30, engine.State.Coins);
        Assert.Equal(1, engine.State.ItemStacks["black-iron-token"].Quantity);
        Assert.Equal(50, engine.State.Character.Experience);
    }

    [Fact]
    public void FiveOfAKindDrawReturnsToAChoiceDrivenRematchScene()
    {
        AdventureEngine engine = AdventureEngine.StartNew(
            AdventureCatalog.StrangersWager,
            AdventureCharacterCatalog.Create("mage", "Test"));

        engine.SelectChoice("take-seat");
        engine.SelectChoice("choose-dice");
        AdventureEncounterCatalog.TryGet(engine.Definition.Id, engine.CurrentScene.Id, out AdventureEncounterDefinition? encounter);

        engine.SelectChoice(encounter!.DrawChoiceId);

        Assert.False(engine.State.Completed);
        Assert.Equal("dice-push", engine.State.CurrentSceneId);
        string[] choices = engine.AvailableChoices().Select(choice => choice.Id).ToArray();
        Assert.Contains("dice-rematch", choices);
        Assert.Contains("switch-to-cards", choices);
        Assert.Contains("leave-dice-push", choices);
    }

    [Fact]
    public void BothEncounterTypesRemainDiscoverableFromTheSameAdventure()
    {
        AdventureDefinition adventure = AdventureCatalog.StrangersWager;

        Assert.True(AdventureEncounterCatalog.TryGet(adventure.Id, "blackjack-table", out AdventureEncounterDefinition? blackjack));
        Assert.True(AdventureEncounterCatalog.TryGet(adventure.Id, "five-kind-table", out AdventureEncounterDefinition? dice));
        Assert.Equal(AdventureEncounterType.Blackjack, blackjack!.Type);
        Assert.Equal(AdventureEncounterType.FiveOfAKind, dice!.Type);
    }
}
