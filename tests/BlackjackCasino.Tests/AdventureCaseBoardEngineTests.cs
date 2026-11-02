using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class AdventureCaseBoardEngineTests
{
    [Fact]
    public void StoryEffectsDiscoverNodesAndConnectionsAndPersistSemanticState()
    {
        AdventureDefinition adventure = BuildInvestigationAdventure();
        AdventureCharacterState character = AdventureCharacterCatalog.Create("rogue", "Investigator");

        AdventureEngine engine = AdventureEngine.StartNew(adventure, character);

        Assert.True(engine.State.CaseBoard.Nodes["victim"].IsKnown);
        Assert.True(engine.State.CaseBoard.Nodes["missing-call"].IsKnown);
        Assert.False(engine.State.CaseBoard.Nodes["ledger"].IsKnown);
        Assert.True(engine.Evaluate(new AdventureCondition(AdventureConditionKind.CaseNodeKnown, "victim")));
        Assert.False(engine.Evaluate(new AdventureCondition(AdventureConditionKind.LeadResolved, "missing-call")));

        engine.SelectChoice("search-desk");

        Assert.True(engine.State.CaseBoard.Nodes["ledger"].IsKnown);
        Assert.True(engine.State.CaseBoard.Connections["victim-ledger"].IsKnown);
        Assert.Equal(CaseConnectionStatus.Confirmed, engine.State.CaseBoard.Connections["victim-ledger"].Status);
        Assert.Equal(CaseLeadState.Resolved, engine.State.CaseBoard.Nodes["missing-call"].LeadState);
        Assert.True(engine.Evaluate(new AdventureCondition(AdventureConditionKind.CaseConnectionKnown, "victim-ledger")));
        Assert.True(engine.Evaluate(new AdventureCondition(AdventureConditionKind.LeadResolved, "missing-call")));
    }

    [Fact]
    public void TheoryEffectsMoveAWorkingThoughtThroughItsNarrativeStates()
    {
        AdventureDefinition adventure = BuildInvestigationAdventure();
        AdventureEngine engine = AdventureEngine.StartNew(adventure, AdventureCharacterCatalog.Create("rogue"));

        engine.SelectChoice("follow-theory");

        Assert.True(engine.State.CaseBoard.Nodes["inside-job"].IsKnown);
        Assert.Equal(CaseTheoryState.Supported, engine.State.CaseBoard.Nodes["inside-job"].TheoryState);
        Assert.True(engine.Evaluate(new AdventureCondition(AdventureConditionKind.TheoryKnown, "inside-job")));
    }

    [Fact]
    public void CaseBoardEffectsAreRejectedWhenAdventureHasNoBoard()
    {
        var adventure = new AdventureDefinition(
            "invalid-board-effect",
            1,
            "Invalid",
            "Test",
            "start",
            [
                new AdventureSceneDefinition(
                    "start",
                    "Start",
                    "Test",
                    OnEnterEffects: [new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "missing")]),
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            AdventureEngine.StartNew(adventure, AdventureCharacterCatalog.Create("rogue")));
    }

    private static AdventureDefinition BuildInvestigationAdventure()
    {
        var board = new CaseBoardDefinition(
            [
                new CaseNodeDefinition("victim", CaseNodeCategory.Person, "Arthur Vane", "The missing man."),
                new CaseNodeDefinition("ledger", CaseNodeCategory.Evidence, "Casino Ledger", "A coded ledger."),
                new CaseNodeDefinition("missing-call", CaseNodeCategory.Lead, "Who called room 306?", "A missing switchboard entry."),
                new CaseNodeDefinition("inside-job", CaseNodeCategory.Theory, "Inside job", "Someone erased the report."),
            ],
            [
                new CaseConnectionDefinition("victim-ledger", "victim", "ledger", CaseConnectionType.Financial, Label: "payments"),
            ]);

        return new AdventureDefinition(
            "case-board-engine-test",
            1,
            "Case Board Test",
            "Investigation integration",
            "opening",
            [
                new AdventureSceneDefinition(
                    "opening",
                    "Opening",
                    "Rain on the glass.",
                    Choices:
                    [
                        new AdventureChoiceDefinition(
                            "search-desk",
                            "Search the desk.",
                            "searched",
                            Effects:
                            [
                                new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "ledger"),
                                new AdventureEffect(AdventureEffectKind.RevealCaseConnection, "victim-ledger"),
                                new AdventureEffect(AdventureEffectKind.ConfirmCaseConnection, "victim-ledger"),
                                new AdventureEffect(AdventureEffectKind.ResolveLead, "missing-call"),
                            ]),
                        new AdventureChoiceDefinition(
                            "follow-theory",
                            "Follow the internal angle.",
                            "theory",
                            Effects:
                            [
                                new AdventureEffect(AdventureEffectKind.UnlockTheory, "inside-job"),
                                new AdventureEffect(AdventureEffectKind.SupportTheory, "inside-job"),
                            ]),
                    ],
                    OnEnterEffects:
                    [
                        new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "victim"),
                        new AdventureEffect(AdventureEffectKind.DiscoverCaseNode, "missing-call"),
                    ]),
                new AdventureSceneDefinition("searched", "Searched", "The ledger changes the case.", IsEnding: true, EndingId: "ledger-found"),
                new AdventureSceneDefinition("theory", "Theory", "The erased report matters.", IsEnding: true, EndingId: "theory-followed"),
            ],
            CaseBoard: board);
    }
}
