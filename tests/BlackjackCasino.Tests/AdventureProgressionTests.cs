using BlackjackCasino.Core;
using BlackjackCasino.Core.Adventure;
using BlackjackCasino.Core.Collections;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class AdventureProgressionTests
{
    [Fact]
    public void LibraryContainsRegisteredCampaignsPlusTemporaryEncounterTestTale()
    {
        Assert.Contains(AdventureCatalog.All, adventure => adventure.Id == "black-stag");
        Assert.Contains(AdventureCatalog.All, adventure => adventure.Id == "drowned-bell");
        Assert.Contains(AdventureCatalog.All, adventure => adventure.Id == "strangers-wager");
        Assert.Contains(AdventureCatalog.All, adventure => adventure.Id == "dead-mans-hand");
        Assert.Equal(4, AdventureCatalog.All.Count);
    }

    [Fact]
    public void DrownedBellIsAValidatedFiveActCampaignWithBothEncounterTypes()
    {
        AdventureDefinition adventure = AdventureCatalog.DrownedBell;
        AdventureDefinitionValidator.Validate(adventure);
        AdventureEncounterCatalog.Validate(adventure);

        Assert.Equal("drowned-bell", adventure.Id);
        Assert.Equal(5, adventure.Acts?.Count);
        Assert.True(adventure.Scenes.Count >= 60);
        Assert.Contains(adventure.Scenes, scene => scene.EndingId == "debt-paid");
        Assert.Contains(adventure.Scenes, scene => scene.EndingId == "sea-remembers");
        Assert.Contains(adventure.Scenes, scene => scene.EndingId == "silent-bell");
        Assert.Contains(adventure.Scenes, scene => scene.EndingId == "the-drowned");
        Assert.Contains(adventure.Scenes, scene => scene.EndingId == "greyhaven-gold");

        Assert.True(AdventureEncounterCatalog.TryGet(adventure.Id, "varro-blackjack", out AdventureEncounterDefinition? blackjack));
        Assert.Equal(AdventureEncounterType.Blackjack, blackjack!.Type);
        Assert.True(AdventureEncounterCatalog.TryGet(adventure.Id, "bram-dice", out AdventureEncounterDefinition? dice));
        Assert.Equal(AdventureEncounterType.FiveOfAKind, dice!.Type);
    }

    [Fact]
    public void DrownedBellPrestigeEndingRecordsAchievementsAndUnlocksBellwardenOnce()
    {
        var profile = new PlayerProfileState();
        ProfileNormalization.Normalize(profile);
        AdventureRunState state = CompletedRun(AdventureCatalog.DrownedBell, "debt-paid", "rogue");
        state.Statistics.ChecksSucceeded = 7;
        state.Statistics.ItemsFound = 6;
        state.Statistics.EncountersPlayed = 2;
        state.Statistics.EncountersWon = 2;
        state.Statistics.BlackjackWins = 1;
        state.Statistics.FiveOfAKindWins = 1;
        state.Statistics.NaturalTwenties = 1;

        AdventureCompletionMetaResult first = AdventureProgressionService.RecordCompletion(profile, AdventureCatalog.DrownedBell, state);

        Assert.True(first.Recorded);
        Assert.True(first.NewEnding);
        Assert.Equal("title-bellwarden", first.UnlockedTitle?.Id);
        Assert.Contains("title-bellwarden", profile.OwnedCosmeticIds);
        Assert.Equal(1, profile.Adventure.RunsCompleted);
        Assert.Contains(AdventureProgressionService.EndingKey("drowned-bell", "debt-paid"), profile.Adventure.EndingKeys);
        Assert.Equal(1, profile.Adventure.BlackjackWins);
        Assert.Equal(1, profile.Adventure.FiveOfAKindWins);
        Assert.Contains(first.NewlyCompletedChallenges, challenge => challenge.Id == "adventure-first-tale");
        Assert.Contains(first.NewlyCompletedChallenges, challenge => challenge.Id == "adventure-drowned-bell-debt-paid");
        Assert.Contains(first.NewlyCompletedChallenges, challenge => challenge.Id == "adventure-against-house");
        Assert.Contains(first.NewlyCompletedChallenges, challenge => challenge.Id == "adventure-loaded-dice");
        Assert.Contains(first.NewlyCompletedChallenges, challenge => challenge.Id == "adventure-natural-talent");

        ChallengeDefinition achievement = ChallengeRegistry.TryGet("adventure-drowned-bell-debt-paid")!;
        Assert.True(ChallengeRegistry.Complete(profile, achievement));

        AdventureCompletionMetaResult second = AdventureProgressionService.RecordCompletion(profile, AdventureCatalog.DrownedBell, state);
        Assert.False(second.Recorded);
        Assert.Equal(1, profile.Adventure.RunsCompleted);
        Assert.Equal(1, profile.OwnedCosmeticIds.Count(id => id == "title-bellwarden"));
    }

    [Fact]
    public void BlackStagPrestigeEndingUnlocksKeeperOfNames()
    {
        var profile = new PlayerProfileState();
        ProfileNormalization.Normalize(profile);
        AdventureRunState state = CompletedRun(AdventureCatalog.BlackStag, "missing-return", "cleric");

        AdventureCompletionMetaResult result = AdventureProgressionService.RecordCompletion(profile, AdventureCatalog.BlackStag, state);

        Assert.True(result.Recorded);
        Assert.Equal("title-keeper-of-names", result.UnlockedTitle?.Id);
        Assert.Contains("title-keeper-of-names", profile.OwnedCosmeticIds);
        Assert.True(ChallengeRegistry.Complete(profile, ChallengeRegistry.TryGet("adventure-black-stag-missing-return")!));
    }

    [Fact]
    public void AdventureRunStatisticsTrackChoicesChecksItemsDamageCoinsAndEncounters()
    {
        AdventureEngine engine = AdventureEngine.StartNew(AdventureCatalog.StrangersWager, AdventureCharacterCatalog.Create("rogue", "Stats"));
        engine.SelectChoice("take-seat");
        engine.SelectChoice("accept-now");
        Assert.Equal("blackjack-table", engine.State.CurrentSceneId);

        engine.RecordEncounter(AdventureEncounterType.Blackjack, AdventureEncounterOutcome.Win);
        AdventureEncounterCatalog.TryGet(engine.Definition.Id, engine.CurrentScene.Id, out AdventureEncounterDefinition? encounter);
        engine.SelectChoice(encounter!.WinChoiceId);

        Assert.True(engine.State.Completed);
        Assert.True(engine.State.Statistics.ScenesVisited >= 3);
        Assert.True(engine.State.Statistics.UniqueSceneIds.Count >= 3);
        Assert.True(engine.State.Statistics.ChoicesMade >= 3);
        Assert.Equal(1, engine.State.Statistics.EncountersPlayed);
        Assert.Equal(1, engine.State.Statistics.EncountersWon);
        Assert.Equal(1, engine.State.Statistics.BlackjackWins);
        Assert.Equal(1, engine.State.Statistics.ItemsFound);
        Assert.Equal(10, engine.State.Statistics.CoinsEarned);
        Assert.NotNull(engine.State.Statistics.CompletedAtUtc);
    }

    [Fact]
    public void AdventureChallengesUseTheExistingClaimPipeline()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        AdventureRunState state = CompletedRun(AdventureCatalog.DrownedBell, "silent-bell", "warrior");
        _ = AdventureProgressionService.RecordCompletion(profile, AdventureCatalog.DrownedBell, state);

        ChallengeClaimResult result = service.ClaimChallenge("adventure-drowned-bell-silent");

        Assert.True(result.Success);
        Assert.Contains("adventure-drowned-bell-silent", profile.ClaimedChallengeIds);
        Assert.True(profile.Coins >= result.CoinsEarned);
    }

    [Fact]
    public void AdventureChallengeClaimSurvivesProfileSanitizationAndCannotPayTwice()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        AdventureRunState state = CompletedRun(AdventureCatalog.DrownedBell, "silent-bell", "warrior");
        _ = AdventureProgressionService.RecordCompletion(profile, AdventureCatalog.DrownedBell, state);

        ChallengeClaimResult first = service.ClaimChallenge("adventure-drowned-bell-silent");
        Assert.True(first.Success);
        CollectionRarity packRarity = ProgressionService.ChallengePack(first.Challenge!);
        long coinsAfterFirstClaim = profile.Coins;
        int packsAfterFirstClaim = profile.Collection.Packs.GetValueOrDefault(packRarity);

        PlayerProfileStore.SanitizeProfile(profile);
        Assert.Contains("adventure-drowned-bell-silent", profile.ClaimedChallengeIds);

        var reloadedService = new ProgressionService(profile);
        ChallengeClaimResult second = reloadedService.ClaimChallenge("adventure-drowned-bell-silent");

        Assert.False(second.Success);
        Assert.Equal("Challenge reward already claimed.", second.Message);
        Assert.Equal(coinsAfterFirstClaim, profile.Coins);
        Assert.Equal(packsAfterFirstClaim, profile.Collection.Packs.GetValueOrDefault(packRarity));
    }

    [Fact]
    public void AdventureClaimAllReceiptsSurviveProfileSanitization()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        AdventureRunState state = CompletedRun(AdventureCatalog.DrownedBell, "debt-paid", "rogue");
        state.Statistics.BlackjackWins = 1;
        state.Statistics.FiveOfAKindWins = 1;
        _ = AdventureProgressionService.RecordCompletion(profile, AdventureCatalog.DrownedBell, state);

        ChallengeClaimAllResult first = service.ClaimAllChallenges();
        Assert.True(first.Success);
        Assert.Contains(first.Challenges, challenge => challenge.Id == "adventure-first-tale");
        Assert.Contains(first.Challenges, challenge => challenge.Id == "adventure-against-house");
        Assert.Contains(first.Challenges, challenge => challenge.Id == "adventure-loaded-dice");
        long coinsAfterFirstClaim = profile.Coins;
        Dictionary<CollectionRarity, int> packsAfterFirstClaim = profile.Collection.Packs.ToDictionary(pair => pair.Key, pair => pair.Value);

        PlayerProfileStore.SanitizeProfile(profile);
        Assert.All(first.Challenges, challenge => Assert.Contains(challenge.Id, profile.ClaimedChallengeIds));

        var reloadedService = new ProgressionService(profile);
        ChallengeClaimAllResult second = reloadedService.ClaimAllChallenges();

        Assert.False(second.Success);
        Assert.Equal(coinsAfterFirstClaim, profile.Coins);
        Assert.Equal(packsAfterFirstClaim.OrderBy(pair => pair.Key), profile.Collection.Packs.OrderBy(pair => pair.Key));
    }

    private static AdventureRunState CompletedRun(AdventureDefinition definition, string endingId, string classId)
    {
        AdventureSceneDefinition ending = definition.Scenes.Single(scene =>
            scene.IsEnding && string.Equals(scene.EndingId, endingId, StringComparison.OrdinalIgnoreCase));
        return AdventureRunState.Normalize(new AdventureRunState
        {
            AdventureId = definition.Id,
            AdventureVersion = definition.Version,
            CurrentSceneId = ending.Id,
            Character = AdventureCharacterCatalog.Create(classId, "Test"),
            Completed = true,
            EndingId = endingId,
            Statistics = new AdventureRunStatistics
            {
                StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
                CompletedAtUtc = DateTimeOffset.UtcNow,
                ScenesVisited = 20,
                UniqueSceneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ending.Id },
            },
        });
    }
}
