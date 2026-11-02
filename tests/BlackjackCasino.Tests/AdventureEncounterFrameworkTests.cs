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
    public void StrangersWagerBlackjackLossRoutesAndRecordsWithoutThrowing()
    {
        AdventureEngine engine = AdventureEngine.StartNew(
            AdventureCatalog.StrangersWager,
            AdventureCharacterCatalog.Create("rogue", "Test"));

        engine.SelectChoice("take-seat");
        engine.SelectChoice("accept-now");
        Assert.True(AdventureEncounterCatalog.TryGet(engine.Definition.Id, engine.CurrentScene.Id, out AdventureEncounterDefinition? encounter));
        Assert.NotNull(encounter);
        Assert.Equal("encounter-loss", encounter!.ChoiceIdFor(AdventureEncounterOutcome.Loss));

        engine.SelectChoice(encounter.ChoiceIdFor(AdventureEncounterOutcome.Loss));
        engine.RecordEncounter(encounter.Type, AdventureEncounterOutcome.Loss);

        Assert.True(engine.State.Completed);
        Assert.Equal("house-lesson", engine.State.EndingId);
        Assert.Equal(15, engine.State.Coins);
        Assert.Equal(20, engine.State.Character.Experience);
        Assert.Equal(1, engine.State.Statistics.EncountersPlayed);
        Assert.Equal(1, engine.State.Statistics.EncountersLost);
        Assert.Equal(0, engine.State.Statistics.BlackjackWins);
    }

    [Fact]
    public void DrownedBellBlackjackLossRoutesToCaptainCollectsWithoutThrowing()
    {
        AdventureEngine engine = AdventureEngine.StartNew(
            AdventureCatalog.DrownedBell,
            AdventureCharacterCatalog.Create("warrior", "Test"));
        engine.State.CurrentSceneId = "varro-blackjack";
        int relationBefore = engine.State.Npcs.TryGetValue("varro", out AdventureNpcState? varro) ? varro.Relation : 0;

        Assert.True(AdventureEncounterCatalog.TryGet(engine.Definition.Id, engine.CurrentScene.Id, out AdventureEncounterDefinition? encounter));
        Assert.NotNull(encounter);
        Assert.Equal("varro-game-loss", encounter!.ChoiceIdFor(AdventureEncounterOutcome.Loss));

        engine.SelectChoice(encounter.ChoiceIdFor(AdventureEncounterOutcome.Loss));
        engine.RecordEncounter(encounter.Type, AdventureEncounterOutcome.Loss);

        Assert.False(engine.State.Completed);
        Assert.Equal("varro-loss", engine.State.CurrentSceneId);
        Assert.Equal(relationBefore - 1, engine.State.Npcs["varro"].Relation);
        Assert.Equal(1, engine.State.Statistics.EncountersPlayed);
        Assert.Equal(1, engine.State.Statistics.EncountersLost);
        Assert.Equal(0, engine.State.Statistics.BlackjackWins);
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
