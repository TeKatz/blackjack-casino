using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class DeadMansHandAdventureTests
{
    [Fact]
    public void NoirCampaignDeclaresSixActsAndSixCampaignSpecificArchetypes()
    {
        AdventureDefinition adventure = DeadMansHandAdventure.Build();

        AdventureDefinitionValidator.Validate(adventure);
        Assert.Equal(6, adventure.Acts!.Count);
        Assert.Equal(6, adventure.CharacterCatalog!.Classes.Count);
        Assert.Equal("Brawn", adventure.CharacterCatalog.AbilityLabels![AdventureAbility.Strength]);
        Assert.Equal("Reflex", adventure.CharacterCatalog.AbilityLabels[AdventureAbility.Dexterity]);
        Assert.Contains(adventure.CharacterCatalog.Classes, item => item.Id == "private-eye" && item.SpecialMechanicName == "Hunch");
        Assert.Contains(adventure.CharacterCatalog.Classes, item => item.Id == "grifter" && item.RouteTags!.Contains("casino"));
    }

    [Fact]
    public void SharedOpeningBuildsTheVaneCaseBoardAsStoryAdvances()
    {
        AdventureDefinition adventure = DeadMansHandAdventure.Build();
        AdventureCharacterState character = AdventureCharacterCatalog.Create(adventure, "private-eye", "Morgan");
        AdventureEngine engine = AdventureEngine.StartNew(adventure, character);

        Assert.True(engine.State.CaseBoard.Nodes["arthur-vane"].IsKnown);
        Assert.True(engine.State.CaseBoard.Nodes["evelyn-vane"].IsKnown);
        Assert.True(engine.State.CaseBoard.Nodes["lead-where-vane"].IsKnown);
        Assert.True(engine.State.CaseBoard.Connections["vane-marriage"].IsKnown);
        Assert.Equal(CaseConnectionStatus.Confirmed, engine.State.CaseBoard.Connections["vane-marriage"].Status);

        engine.SelectChoice("take-case");
        Assert.Equal("hotel-lobby", engine.State.CurrentSceneId);
        Assert.True(engine.State.CaseBoard.Nodes["hotel-mirabelle"].IsKnown);

        engine.SelectChoice("bribe-clerk");
        Assert.Equal("room-306", engine.State.CurrentSceneId);
        Assert.Equal(15, engine.State.Coins);
        Assert.True(engine.State.CaseBoard.Nodes["cufflink"].IsKnown);
        Assert.True(engine.State.CaseBoard.Nodes["torn-photo"].IsKnown);
        Assert.True(engine.State.CaseBoard.Nodes["theory-vane-knew-visitor"].IsKnown);
    }

    [Fact]
    public void ExistingFantasyCatalogStillUsesLegacyClasses()
    {
        Assert.DoesNotContain(AdventureCharacterCatalog.AllFor(AdventureCatalog.BlackStag), item => item.Id == "private-eye");
        Assert.Contains(AdventureCharacterCatalog.AllFor(AdventureCatalog.BlackStag), item => item.Id == "rogue");
        Assert.Contains(AdventureCharacterCatalog.AllFor(AdventureCatalog.DeadMansHand), item => item.Id == "private-eye");
    }
}
