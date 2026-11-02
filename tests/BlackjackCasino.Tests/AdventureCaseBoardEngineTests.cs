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
    public void InitializeRepairsPersistedV2LayoutExactlyOnceWithoutLosingCaseProgress()
    {
        var board = new CaseBoardDefinition(
            [
                new CaseNodeDefinition("victim", CaseNodeCategory.Person, "Arthur Vane", "The missing man.", X: 120, Y: 180),
                new CaseNodeDefinition("ledger", CaseNodeCategory.Evidence, "Casino Ledger", "A coded ledger.", X: 460, Y: 340),
            ],
            [
                new CaseConnectionDefinition("victim-ledger", "victim", "ledger", CaseConnectionType.Financial),
            ]);
        var state = new CaseBoardState
        {
            Layout = new BoardLayoutState
            {
                LayoutVersion = 2,
                Zoom = .24,
                PanX = -940,
                PanY = 680,
                NodePositions = new Dictionary<string, CaseBoardNodePosition>(StringComparer.OrdinalIgnoreCase)
                {
                    ["victim"] = new() { X = 5_900, Y = 4_100 },
                    ["ledger"] = new() { X = 7_800, Y = 5_200 },
                },
            },
            Nodes = new Dictionary<string, CaseNodeState>(StringComparer.OrdinalIgnoreCase)
            {
                ["victim"] = new() { IsKnown = true, Reliability = CaseReliability.Confirmed },
                ["ledger"] = new() { IsKnown = true, Reliability = CaseReliability.Credible },
            },
            Connections = new Dictionary<string, CaseConnectionState>(StringComparer.OrdinalIgnoreCase)
            {
                ["victim-ledger"] = new() { IsKnown = true, Status = CaseConnectionStatus.Confirmed },
            },
        };

        CaseBoardState repaired = AdventureCaseBoard.Initialize(board, state);

        Assert.Equal(1, repaired.Layout.RepairVersion);
        Assert.Equal(0, repaired.Layout.LayoutVersion);
        Assert.Equal(1d, repaired.Layout.Zoom);
        Assert.Equal(0d, repaired.Layout.PanX);
        Assert.Equal(0d, repaired.Layout.PanY);
        Assert.Equal(120d, repaired.Layout.NodePositions["victim"].X);
        Assert.Equal(180d, repaired.Layout.NodePositions["victim"].Y);
        Assert.Equal(460d, repaired.Layout.NodePositions["ledger"].X);
        Assert.Equal(340d, repaired.Layout.NodePositions["ledger"].Y);
        Assert.True(repaired.Nodes["victim"].IsKnown);
        Assert.Equal(CaseReliability.Confirmed, repaired.Nodes["victim"].Reliability);
        Assert.True(repaired.Connections["victim-ledger"].IsKnown);
        Assert.Equal(CaseConnectionStatus.Confirmed, repaired.Connections["victim-ledger"].Status);

        repaired.Layout.LayoutVersion = 2;
        repaired.Layout.Zoom = 1.35;
        repaired.Layout.PanX = 42;
        repaired.Layout.PanY = -18;
        repaired.Layout.NodePositions["victim"] = new CaseBoardNodePosition { X = 777, Y = 555 };

        CaseBoardState reopened = AdventureCaseBoard.Initialize(board, repaired);

        Assert.Equal(1, reopened.Layout.RepairVersion);
        Assert.Equal(2, reopened.Layout.LayoutVersion);
        Assert.Equal(1.35, reopened.Layout.Zoom);
        Assert.Equal(42d, reopened.Layout.PanX);
        Assert.Equal(-18d, reopened.Layout.PanY);
        Assert.Equal(777d, reopened.Layout.NodePositions["victim"].X);
        Assert.Equal(555d, reopened.Layout.NodePositions["victim"].Y);
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
