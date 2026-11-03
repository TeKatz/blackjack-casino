using System.Text.Json;
using BlackjackCasino.Core.Adventure;

namespace BlackjackCasino.Tests;

public sealed class DeadMansHandCampaignTests
{
    private static AdventureEngine Start() => AdventureEngine.StartNew(AdventureCatalog.DeadMansHand,
        AdventureCharacterCatalog.Create(AdventureCatalog.DeadMansHand, "private-eye", "Morgan"));

    private static void Walk(AdventureEngine engine, params string[] choices)
    {
        foreach (string choice in choices)
        {
            Assert.Contains(engine.AvailableChoices(), c => c.Id == choice);
            engine.SelectChoice(choice);
            if (engine.PendingCheck is not null) engine.ResolvePendingCheck(20, 20);
        }
    }

    private static AdventureEngine FullInvestigation()
    {
        AdventureEngine engine = Start();
        Walk(engine, "take-case", "bribe-clerk", "study-phone", "return-to-room", "leave-room", "foundation-end", "continue-investigation",
            "visit-evelyn", "evelyn-trust", "visit-exchange", "patch-call", "exchange-return",
            "visit-supplies", "buy-dressing", "leave-chemist", "visit-ivory", "show-pass",
            "ask-lena", "show-photo", "lena-trust-back", "watch-cashier", "audit-carbon", "carbon-back", "leave-ivory",
            "follow-freight", "dock-rescue", "pump-quiet", "dress-vane", "safehouse-statement", "dock-badge",
            "archive-route", "archive-with-recording", "signed-book-back", "precinct-to-editor");
        return engine;
    }

    [Theory]
    [InlineData("publish-full", "press-end", "open-record")]
    [InlineData("publish-recording", "press-end", "open-record")]
    [InlineData("file-court", "court-end", "exhibit-one")]
    [InlineData("print-ledger", "ledger-end", "paper-trail")]
    [InlineData("take-ferry", "ferry-end", "two-berths")]
    [InlineData("keep-witness", "witness-end", "living-witness")]
    public void FullRouteCanFinishWithDifferentConsequencesAndSurviveReload(string choice, string finish, string ending)
    {
        AdventureEngine engine = FullInvestigation();
        Walk(engine, "copy-dossier", "copy-back", "guard-witness", "quiet-transfer", "secured-back");
        AdventureRunState saved = JsonSerializer.Deserialize<AdventureRunState>(JsonSerializer.Serialize(engine.State))!;
        engine = new AdventureEngine(AdventureCatalog.DeadMansHand, saved);
        Walk(engine, "finish-investigation", choice, finish);
        Assert.True(engine.State.Completed);
        Assert.Equal(ending, engine.State.EndingId);
        Assert.True(engine.State.Flags["dmh-witness-protected"]);
        Assert.DoesNotContain("dmh-aid", engine.State.Inventory);
        if (choice == "take-ferry") Assert.DoesNotContain("dmh-ticket", engine.State.Inventory);
    }

    [Theory]
    [InlineData("ashes")]
    [InlineData("new-creditor")]
    [InlineData("house-credit")]
    public void OriginalLedgerCanBeDestroyedUsedForBlackmailOrTraded(string ending)
    {
        AdventureEngine engine = FullInvestigation();
        if (ending == "new-creditor")
            Walk(engine, "one-last-lead", "rook-route", "rook-play", "rook-consider", "precinct-to-editor");
        if (ending == "house-credit") Walk(engine, "meet-broker", "trade-ledger");
        Walk(engine, "finish-investigation");
        Walk(engine, ending switch { "ashes" => "burn-book", "new-creditor" => "blackmail-rook", _ => "take-bargain" });
        Walk(engine, ending switch { "ashes" => "ashes-end", "new-creditor" => "blackmail-end", _ => "bargain-end" });
        Assert.Equal(ending, engine.State.EndingId);
        Assert.Equal(ending == "new-creditor", engine.State.Inventory.Contains("dmh-ledger"));
    }

    [Fact]
    public void AnUnsubstantiatedCaseStillHasAnHonestEndingAndCannotClaimProofItLacks()
    {
        AdventureEngine engine = Start();
        Walk(engine, "take-case", "bribe-clerk", "leave-room", "foundation-end", "continue-investigation",
            "follow-freight", "dock-badge", "precinct-to-editor", "finish-investigation");
        Assert.DoesNotContain(engine.AvailableChoices(), c => c.Id is "file-court" or "publish-full" or "burn-book" or "take-ferry");
        Walk(engine, "leave-open", "cold-end");
        Assert.Equal("open-file", engine.State.EndingId);
    }

    [Fact]
    public void HaleCanBecomeTheFallGuyWithoutBeingMistakenForTheProtectedWitness()
    {
        AdventureEngine engine = Start();
        Walk(engine, "take-case", "bribe-clerk", "leave-room", "foundation-end", "continue-investigation",
            "follow-freight", "dock-badge", "hale-private", "press-hale", "hale-confession-back", "precinct-to-editor");
        Assert.DoesNotContain("dmh-affidavit", engine.State.Inventory);
        Assert.DoesNotContain(engine.AvailableChoices(), c => c.Id == "guard-witness");
        Walk(engine, "finish-investigation", "name-hale", "fallguy-end");
        Assert.Equal("fallguy", engine.State.EndingId);
    }

    [Fact]
    public void FailedChecksMoveForwardAndHeatChangesTheWitnessTransfer()
    {
        AdventureEngine engine = Start();
        engine.SelectChoice("take-case"); engine.SelectChoice("talk-clerk"); engine.ResolvePendingCheck(1);
        Assert.Equal("service-hall", engine.State.CurrentSceneId);
        engine.SelectChoice("pick-service-lock"); engine.ResolvePendingCheck(1);
        Assert.Equal("hale-arrives", engine.State.CurrentSceneId);
        Walk(engine, "answer-hale", "hale-pass-room", "leave-room", "foundation-end", "continue-investigation");
        Assert.NotEmpty(engine.AvailableChoices());
        engine = FullInvestigation();
        Walk(engine, "guard-witness");
        engine.State.Counters["dmh-heat"] = 3;
        Assert.DoesNotContain(engine.AvailableChoices(), c => c.Id == "quiet-transfer");
        Assert.Contains(engine.AvailableChoices(), c => c.Id == "watched-transfer" && c.Check!.Difficulty == 16);
        engine.SelectChoice("watched-transfer"); engine.ResolvePendingCheck(1);
        Walk(engine, "decoy-back", "secured-back");
        Assert.True(engine.State.Flags["dmh-witness-protected"]);
    }

    [Fact]
    public void ACompletedFoundationSaveReopensWithoutLosingIdentityCharacterOrBoard()
    {
        AdventureDefinition adventure = AdventureCatalog.DeadMansHand;
        AdventureRunState state = Start().State;
        state.AdventureVersion = 1; state.CurrentSceneId = "foundation-checkpoint";
        state.Completed = true; state.EndingId = "foundation-preview";
        state.CaseBoard.Nodes["cufflink"].IsKnown = true;
        string id = state.RunId;
        var engine = new AdventureEngine(adventure, state);
        Assert.False(state.Completed); Assert.Null(state.EndingId);
        Assert.Equal(id, state.RunId); Assert.Equal("Morgan", state.Character.Name);
        Assert.Contains("dmh-cufflink", state.Inventory);
        Assert.True(state.CaseBoard.Nodes["cufflink"].IsKnown);
        Assert.False(AdventureRunMigrations.Apply(adventure, state));
        Walk(engine, "continue-investigation");
        Assert.Equal("city-threads", state.CurrentSceneId);
    }

    [Fact]
    public void EveryActIsPlayableAllScenesAreReachableAndContentReferencesAreValid()
    {
        AdventureDefinition adventure = AdventureCatalog.DeadMansHand;
        AdventureDefinitionValidator.Validate(adventure);
        Assert.True(adventure.Scenes.Count >= 75);
        Assert.Equal(10, adventure.Scenes.Count(s => s.IsEnding));
        foreach (string act in adventure.Acts!.Keys) Assert.Contains(adventure.Scenes, s => s.ActId == act && !s.IsEnding);
        var reached = new HashSet<string> { adventure.StartSceneId };
        var queue = new Queue<string>(reached);
        while (queue.TryDequeue(out string? id))
            foreach (AdventureChoiceDefinition choice in adventure.Scenes.Single(s => s.Id == id).Choices ?? [])
                foreach (string? next in new[] { choice.NextSceneId, choice.Check?.SuccessSceneId, choice.Check?.FailureSceneId })
                    if (next is not null && reached.Add(next)) queue.Enqueue(next);
        Assert.Equal(adventure.Scenes.Count, reached.Count);
        foreach (AdventureEffect effect in adventure.Scenes.SelectMany(s => (s.OnEnterEffects ?? []).Concat(
            (s.Choices ?? []).SelectMany(c => (c.Effects ?? []).Concat(c.SuccessEffects ?? []).Concat(c.FailureEffects ?? [])))))
        {
            if (effect.Kind is AdventureEffectKind.AddItem or AdventureEffectKind.RemoveItem)
                Assert.Contains(adventure.Items!, item => item.Id == effect.Key);
            if (effect.Kind is AdventureEffectKind.DiscoverCaseNode or AdventureEffectKind.ResolveLead or AdventureEffectKind.UnlockTheory or
                AdventureEffectKind.SupportTheory or AdventureEffectKind.ConfirmTheory)
                Assert.Contains(adventure.CaseBoard!.Nodes, node => node.Id == effect.Key);
            if (effect.Kind is AdventureEffectKind.RevealCaseConnection or AdventureEffectKind.ConfirmCaseConnection or AdventureEffectKind.ContradictCaseConnection)
                Assert.Contains(adventure.CaseBoard!.Connections, link => link.Id == effect.Key);
        }
    }
}
