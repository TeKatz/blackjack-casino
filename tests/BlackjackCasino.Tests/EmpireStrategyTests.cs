using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class EmpireStrategyTests
{
    private static readonly DateTimeOffset Now = new(2026,10,30,12,0,0,TimeSpan.Zero);
    private static EmpireService Make(int prestige = 0, decimal credits = 1_000_000_000_000m, int level = 1, string[]? projects = null, Dictionary<string,int>? research = null)
    {
        var state = new EmpireState { Started = true, LastAccruedAt = Now, Prestige = prestige, Credits = credits,
            Levels = EmpireCatalog.Departments.ToDictionary(room => room.Id, _ => level), Research = research ?? [] };
        EmpireState migrated = EmpireState.Normalize(state);
        var estate = migrated.Properties[EmpirePropertyCatalog.DefaultId] with { Projects = new(projects ?? []), Levels = state.Levels };
        return new(migrated with { Properties = new(migrated.Properties) { [EmpirePropertyCatalog.DefaultId] = estate } });
    }

    [Fact]
    public void LegacySavesRetainBudgetLevelsPrestigeAndReceiveUnspentLegacyPoints()
    {
        var legacy = new EmpireState { SchemaVersion = 1, Prestige = 5, Credits = 12345, Dividend = 23.45m, Levels = new() { ["slots"] = 27, ["tables"] = 15 } };
        var service = new EmpireService(legacy);
        Assert.Equal(3, service.Snapshot.SchemaVersion);
        Assert.Equal(12345, service.Credits);
        Assert.Equal(23.45m, service.Dividend);
        Assert.Equal(27, service.Level("slots"));
        Assert.Equal(5, service.LegacyPointsAvailable);
        Assert.Single(service.OwnedProperties);
        Assert.False(service.FloorUnlocked(3));
        Assert.Equal(150m, new EmpireService().Credits);
    }

    [Fact]
    public void ResearchSpendsRealPointsEnforcesPrerequisitesAndSurvivesPrestige()
    {
        var service = Make(prestige: 5, level: 100);
        Assert.False(service.Research("automation",Now).Success);
        Assert.True(service.Research("construction",Now).Success);
        Assert.Equal(4, service.LegacyPointsAvailable);
        Assert.Equal(.02m, service.ConstructionDiscount("slots"));
        Assert.True(service.Research("construction",Now).Success);
        Assert.True(service.Research("compound",Now).Success);
        Assert.False(service.Research("automation",Now).Success); // one point left, needs three
        EmpireState snapshot = service.Snapshot;
        var estate = snapshot.Properties[snapshot.ActivePropertyId] with { RunCredits = EmpireRules.PrestigeRunCreditRequirement(5) };
        service = new(snapshot with { Properties = new(snapshot.Properties) { [snapshot.ActivePropertyId] = estate } });
        Assert.True(service.PrestigeEmpire(Now).Success);
        Assert.Equal(2, service.ResearchRank("construction"));
        Assert.Equal(2, service.LegacyPointsAvailable);
    }

    [Fact]
    public void BulkDiscountEqualsRepeatedUnitQuotesAndDoesNotExceedMaximumLevel()
    {
        var service = Make(10, level: 20, projects: ["executive-offices"], research: new() { ["construction"] = 3 });
        Assert.True(service.AssignManager("daniel","slots",Now).Success);
        EmpireUpgradeQuote bulk = service.QuoteUpgrade("slots",10);
        decimal expected = Enumerable.Range(20,10).Sum(level => decimal.Ceiling(EmpireRules.UpgradeCost(EmpireCatalog.Find("slots")!,level) * .86m));
        Assert.Equal(expected, bulk.Cost);
        Assert.True(service.UpgradeMany("slots",10,Now));
        Assert.Equal(30, service.Level("slots"));
    }

    [Fact]
    public void TagsDriveSynergyThresholdsAndResortDoesNotMoveExistingCircuitGoalposts()
    {
        var service = Make(5, level: 50, projects: ["resort-wing"]);
        EmpireSynergyStatus circuit = service.Synergies.Single(s=>s.Definition.Id=="high-roller");
        Assert.Equal(2, circuit.Tier);
        Assert.Equal(.20m,circuit.Bonus);
        Assert.Equal(100,circuit.NextLevel);
        EmpireState snapshot = service.Snapshot;
        var estate = snapshot.Properties[snapshot.ActivePropertyId];
        estate.Levels["penthouse-club"] = 1;
        var changed = new EmpireService(snapshot);
        Assert.Equal(2,changed.Synergies.Single(s=>s.Definition.Id=="high-roller").Tier);
        Assert.Equal(0,changed.Synergies.Single(s=>s.Definition.Id=="destination").Tier);
        Assert.Equal(50,service.Level("penthouse-club")); // detached snapshot
    }

    [Fact]
    public void CapitalProjectsHaveRealGatesPayOnceAndRemainAfterReset()
    {
        var service = Make(3,level:1);
        Assert.False(service.BuildProject("grand-entrance",Now).Success);
        service = Make(3,level:60);
        decimal before = service.Credits;
        Assert.True(service.BuildProject("grand-entrance",Now).Success);
        Assert.Equal(before-25_000_000m,service.Credits);
        Assert.False(service.BuildProject("grand-entrance",Now).Success);
        Assert.True(service.BuildProject("executive-offices",Now).Success);
        Assert.True(service.BuildProject("luxury-atrium",Now).Success);
        EmpireState snapshot = service.Snapshot;
        var estate = snapshot.Properties[snapshot.ActivePropertyId] with { RunCredits = EmpireRules.PrestigeRunCreditRequirement(3) };
        service = new(snapshot with { Properties = new(snapshot.Properties) { [snapshot.ActivePropertyId] = estate } });
        Assert.True(service.PrestigeEmpire(Now).Success);
        Assert.Contains("grand-entrance",service.Projects);
        Assert.Contains("executive-offices",service.Projects);
        Assert.Equal(1,service.ManagerSlots);
    }

    [Fact]
    public void ManagerCapacityAndAssignmentsControlBonusesWithoutRandomUnlocks()
    {
        var service = Make(3,level:25);
        Assert.False(service.AssignManager("elena","slots",Now).Success);
        Assert.True(service.BuildProject("executive-offices",Now).Success);
        decimal baseline = service.RoomCreditsPerSecond("slots");
        Assert.True(service.AssignManager("elena","slots",Now).Success);
        Assert.True(service.RoomCreditsPerSecond("slots") > baseline);
        Assert.False(service.AssignManager("victor","vip",Now).Success);
        Assert.True(service.AssignManager("elena",null,Now).Success);
        Assert.Equal(baseline,service.RoomCreditsPerSecond("slots"));
        Assert.True(service.AssignManager("victor","vip",Now).Success);
        Assert.Single(service.ManagerAssignments);
        Assert.False(service.AssignManager("unknown","vip",Now).Success);
    }

    [Fact]
    public void AutomationRespectsReservePercentageTargetAndMatchesOfflineDecisions()
    {
        var research = new Dictionary<string,int> { ["construction"] = 2, ["compound"] = 1, ["automation"] = 1 };
        var offline = Make(10, credits:1_000_000m, level:1, projects:["executive-offices"],research:research);
        var config = new EmpireAutomationState { Enabled=true,Reserve=900_000m,SpendPercent=.10m,TargetLevel=10,CheapestMilestone=true,Priorities=["slots","tables"] };
        Assert.True(offline.ConfigureAutomation(config,Now).Success);
        var online = new EmpireService(offline.Snapshot);
        offline.Accrue(Now.AddMinutes(10));
        for(int i=1;i<=1200;i++) online.Accrue(Now.AddMilliseconds(i*500));
        Assert.Equal(offline.Snapshot.Properties[offline.ActivePropertyId].Levels,online.Snapshot.Properties[online.ActivePropertyId].Levels);
        Assert.Equal(offline.Credits,online.Credits);
        Assert.True(offline.Credits>=config.Reserve);
        Assert.All(offline.Snapshot.Levels, pair=>Assert.InRange(pair.Value,0,10));
        Assert.True(offline.PropertySnapshot(offline.ActivePropertyId).AutomatedLevels>0);
        Assert.False(offline.ConfigureAutomation(config with { SpendPercent=1 },Now.AddMinutes(10)).Success);
    }

    [Fact]
    public void EventExpirationIsSegmentedAndEncoreUsesOnlyNewCrossGameActivityOnce()
    {
        var plain = Make(level:10);
        var eventGame = new EmpireService(plain.Snapshot);
        eventGame.SetActivitySnapshot(new(Blackjack:100));
        Assert.True(eventGame.StartEvent("blackjack",Now).Success);
        Assert.False(eventGame.StartEvent("dice",Now).Success);
        eventGame.ObserveActivity(new(Blackjack:109),Now.AddMinutes(5));
        Assert.False(eventGame.ActiveEvent!.Encore);
        eventGame.ObserveActivity(new(Blackjack:110),Now.AddMinutes(10));
        Assert.True(eventGame.ActiveEvent!.Encore);
        Assert.Equal(Now.AddMinutes(40),eventGame.ActiveEvent.EndsAt);
        eventGame.ObserveActivity(new(Blackjack:200),Now.AddMinutes(11));
        Assert.Equal(Now.AddMinutes(40),eventGame.ActiveEvent.EndsAt);
        decimal before = eventGame.Credits;
        eventGame.Accrue(Now.AddHours(2));
        Assert.Null(eventGame.ActiveEvent);
        Assert.Equal(1,eventGame.PropertySnapshot(eventGame.ActivePropertyId).EventsCompleted);
        Assert.Equal(1,eventGame.PropertySnapshot(eventGame.ActivePropertyId).EventEncores);
        decimal ending = eventGame.Credits;
        eventGame.Accrue(Now.AddHours(3));
        Assert.Equal(plain.CreditsPerSecond*3600m,eventGame.Credits-ending);
        Assert.True(eventGame.Credits>before);
    }

    [Fact]
    public void EventsProduceTheSameReportWhetherAccruedOnceOrEverySecond()
    {
        var offline = Make(level:10);
        offline.StartEvent("dice",Now);
        var online = new EmpireService(offline.Snapshot);
        offline.Accrue(Now.AddHours(2));
        for(int i=1;i<=7200;i++) online.Accrue(Now.AddSeconds(i));
        Assert.Equal(offline.Credits,online.Credits);
        Assert.Equal(offline.ClaimableDividend,online.ClaimableDividend);
        Assert.Null(online.ActiveEvent);
        Assert.Equal(1,online.PropertySnapshot(online.ActivePropertyId).EventsCompleted);
    }

    [Fact]
    public void ContractsTrackPostAcceptanceProductionWithoutDeadlinesAndClaimOnlyOnce()
    {
        var service = Make(1,level:50);
        Assert.True(service.AcceptContract("tourist-weekend",Now).Success);
        Assert.Equal(0,service.ContractProgress);
        Assert.False(service.AcceptContract("expansion-drive",Now).Success);
        service.Accrue(Now.AddDays(90));
        Assert.True(service.CanClaimContract);
        Assert.True(service.ClaimContract(Now.AddDays(90)).Success);
        Assert.False(service.ClaimContract(Now.AddDays(90)).Success);
        Assert.Equal(7200,service.ContractBuff!.RemainingSeconds);
        Assert.True(service.AcceptContract("expansion-drive",Now.AddDays(90)).Success);
        service.Accrue(Now.AddDays(90).AddHours(3));
        Assert.Null(service.ContractBuff);
        Assert.NotNull(service.ActiveContract);
        Assert.Equal(0,service.ContractProgress);
        Assert.True(service.UpgradeMany("slots",50,Now.AddDays(90).AddHours(3)));
        Assert.True(service.CanClaimContract);
    }

    [Fact]
    public void PropertiesHaveIndependentBudgetsSpecialtiesAndSimultaneousProduction()
    {
        var service = Make(15,level:100,projects:["grand-entrance","resort-wing","skyline-property","executive-offices"]);
        decimal before=service.Credits;
        Assert.True(service.BuyProperty("neon-bay",Now).Success);
        Assert.Equal(before-250_000_000_000m,service.Credits);
        Assert.False(service.BuyProperty("neon-bay",Now).Success);
        Assert.True(service.SelectProperty("neon-bay",Now).Success);
        Assert.Equal(150,service.Credits);
        Assert.Equal(.625m,service.CreditsPerSecond);
        Assert.True(service.Upgrade("slots",Now));
        decimal neon=service.Credits;
        service.SelectProperty("midnight-club",Now);
        Assert.Equal(before-250_000_000_000m,service.Credits);
        service.Accrue(Now.AddSeconds(10));
        Assert.Equal(neon+12.5m,service.PropertySnapshot("neon-bay").Credits);
        Assert.Equal(2,service.PropertySnapshot("neon-bay").Levels["slots"]);
        Assert.Equal(100,service.Level("slots"));
    }

    [Fact]
    public void ResortFloorUsesItsProjectGateAndSurvivesPrestige()
    {
        var locked=Make(3,level:100);
        Assert.False(locked.FloorUnlocked(3));
        Assert.False(locked.Upgrade("hotel",Now));
        Assert.True(locked.BuildProject("grand-entrance",Now).Success);
        Assert.True(locked.BuildProject("resort-wing",Now).Success);
        Assert.True(locked.FloorUnlocked(3));
        Assert.True(locked.Upgrade("hotel",Now));
        Assert.Equal(1,locked.Level("hotel"));
        Assert.True(locked.PrestigeEmpire(Now.AddSeconds(1)).Success);
        Assert.True(locked.FloorUnlocked(3));
        Assert.True(locked.QuoteUpgrade("hotel",1).Unlocked);
        Assert.True(locked.Reputation<5000);
        Assert.Equal(3,EmpireFloorCatalog.All.Count);
        Assert.Equal(5,EmpireCatalog.Floor(3).Count);
    }

    [Fact]
    public void FailedTransactionLeavesEveryManagementLayerUnchangedAndCanRetry()
    {
        var service=Make(5,level:100);
        decimal credits=service.Credits;
        Assert.Throws<IOException>(()=>service.Transact(s=>s.BuildProject("grand-entrance",Now),_=>throw new IOException("disk full")));
        Assert.Equal(credits,service.Credits);
        Assert.DoesNotContain("grand-entrance",service.Projects);
        EmpireState? saved=null;
        Assert.True(service.Transact(s=>s.BuildProject("grand-entrance",Now),snapshot=>saved=snapshot).Success);
        var restored=new EmpireService(JsonSerializer.Deserialize<EmpireState>(JsonSerializer.Serialize(saved)));
        Assert.Contains("grand-entrance",restored.Projects);
        Assert.Equal(service.Credits,restored.Credits);
        Assert.False(restored.BuildProject("grand-entrance",Now).Success);
    }

    [Fact]
    public void ResearchStorageAndVaultBoundOfflineIncomeWithoutDuplication()
    {
        var service=Make(10,level:50,projects:["casino-vault"],research:new(){["construction"]=1,["night-shift"]=2,["hospitality"]=1,["guests"]=1});
        Assert.Equal(12,service.OfflineCapacity.TotalHours);
        Assert.Equal(520m,service.DividendCapacity);
        Assert.True(service.Accrue(Now.AddDays(10)).Capped);
        Assert.InRange(service.Dividend,0,520);
        Assert.Equal(0,service.Accrue(Now.AddDays(10)).Credits);
        decimal credits=service.Credits;
        service.Accrue(Now);
        Assert.Equal(credits,service.Credits);
    }

    [Fact]
    public void DiceAndSudokuAchievementsAreReadFromTheExistingChallengeClaims()
    {
        var service=Make(1,level:50);
        service.SetClaimedAchievements(["dice-games-50","dice-score-300","dice-five-50","dice-wins-50","sudoku-50"]);
        Assert.Equal(.05m,service.AchievementBonusPercent("bar"));
        Assert.Equal(.075m,service.AchievementBonusPercent("tables"));
        Assert.Equal(.10m,service.AchievementBonusPercent("stage"));
        Assert.Equal(.15m,service.AchievementBonusPercent("private-salon"));
        Assert.All(EmpireAchievementBoostCatalog.All,boost=>Assert.NotNull(ChallengeCatalog.TryGet(boost.ChallengeId)));
    }

    [Fact]
    public void ResortFinalLevelsRemainPurchasableWithinTheTechnicalCreditLimit()
    {
        var service = Make(50, EmpireRules.MaximumCredits, 199, ["resort-wing"]);
        foreach (EmpireDepartment room in EmpireCatalog.Departments)
        {
            EmpireUpgradeQuote quote = service.QuoteUpgrade(room.Id,1);
            Assert.InRange(quote.Cost,1,EmpireRules.MaximumCredits);
            Assert.Equal(200,quote.TargetLevel);
        }
    }

    [Fact]
    public void AutomationBudgetIsAppliedOncePerDecisionAndRollbackCannotReplayIt()
    {
        var state = new EmpireState { Started=true, LastAccruedAt=Now, Prestige=10, Credits=1_000_000m,
            Research=new(){["construction"]=2,["compound"]=1,["automation"]=1} };
        state = EmpireState.Normalize(state);
        var estate = state.Properties[state.ActivePropertyId] with { Projects=new(["executive-offices"]) };
        var service = new EmpireService(state with { Properties=new(state.Properties){[state.ActivePropertyId]=estate} });
        service.ConfigureAutomation(new(){Enabled=true,Reserve=0,SpendPercent=.1m,TargetLevel=200},Now);
        decimal earned = service.CreditsPerSecond*30;
        service.Accrue(Now.AddSeconds(30));
        Assert.True(service.Credits >= (1_000_000m+earned)*.9m);
        EmpireState checkpoint=service.Snapshot;
        service.Accrue(Now.AddSeconds(29));
        service.Accrue(Now.AddSeconds(30));
        Assert.Equal(checkpoint.Credits,service.Credits);
        Assert.Equal(checkpoint.Levels,service.Snapshot.Levels);
    }

    [Fact]
    public void FullPortfolioCapsProductionAndDividendWithoutOverflow()
    {
        var projects=new HashSet<string>(EmpireProjectCatalog.All.Select(project=>project.Id));
        var property=new EmpirePropertyState { Prestige=100,Levels=EmpireCatalog.Departments.ToDictionary(room=>room.Id,_=>200),Projects=projects };
        var service=new EmpireService(new EmpireState { Started=true,LastAccruedAt=Now,
            Properties=EmpirePropertyCatalog.All.ToDictionary(p=>p.Id,_=>property),
            Research=EmpireResearchCatalog.All.ToDictionary(n=>n.Id,n=>n.MaxRank) });
        service.SetClaimedAchievements(EmpireAchievementBoostCatalog.All.Select(b=>b.ChallengeId));
        EmpireAccrual report=service.Accrue(Now.AddDays(30));
        Assert.Equal(4,report.Properties.Count);
        Assert.InRange(service.CashPerHour,0,60);
        Assert.InRange(service.Dividend,0,640);
        Assert.All(service.Snapshot.Properties, pair=>Assert.InRange(pair.Value.Credits,0,EmpireRules.MaximumCredits));
        Assert.True(report.Credits>0);
        Assert.Equal(0,service.Accrue(Now.AddDays(30)).Credits);
    }

    [Fact]
    public void AllPropertySpecialtiesRemainLocalToTheirTaggedDepartments()
    {
        var service=Make(25,100_000_000_000_000m,100,["grand-entrance","resort-wing","skyline-property"]);
        foreach(string id in new[]{"neon-bay","crown-palace","aurora"}) Assert.True(service.BuyProperty(id,Now).Success);
        EmpireState snapshot=service.Snapshot;
        foreach(string id in new[]{"neon-bay","crown-palace","aurora"})
            snapshot.Properties[id]=snapshot.Properties[id] with { Credits=1_000_000m, Levels=EmpireCatalog.Floor(1).ToDictionary(room=>room.Id,_=>1) };
        service=new(snapshot);
        service.SelectProperty("neon-bay",Now);
        Assert.Equal(.625m,service.RoomCreditsPerSecond("slots"));
        Assert.Equal(3m,service.RoomCreditsPerSecond("tables"));
        service.SelectProperty("crown-palace",Now);
        Assert.Equal(.5m,service.RoomCreditsPerSecond("slots"));
        Assert.Equal(3.75m,service.RoomCreditsPerSecond("tables"));
        Assert.Equal(375m,service.RoomCreditsPerSecond("vip"));
        service.SelectProperty("aurora",Now);
        Assert.Equal(18.75m,service.RoomCreditsPerSecond("bar"));
        Assert.Equal(3m,service.RoomCreditsPerSecond("tables"));
    }

    [Fact]
    public void AtomicPlayerStateSaveRetainsEveryNewManagementLayerAndActiveProperty()
    {
        string folder=Path.Combine(Path.GetTempPath(),"empire-portfolio-"+Guid.NewGuid());
        try
        {
            var service=Make(15,100_000_000_000_000m,100,["grand-entrance","executive-offices","resort-wing","skyline-property"],
                new(){["construction"]=2,["compound"]=1,["automation"]=1});
            Assert.True(service.AssignManager("elena","slots",Now).Success);
            Assert.True(service.ConfigureAutomation(new(){Enabled=true,Reserve=1000},Now).Success);
            Assert.True(service.StartEvent("dice",Now).Success);
            Assert.True(service.AcceptContract("tourist-weekend",Now).Success);
            Assert.True(service.BuyProperty("neon-bay",Now).Success);
            service.SelectProperty("neon-bay",Now);
            PlayerStateStore.SaveOrThrow(new AppPreferences { Empire=service.Snapshot,Bankroll=777m },folder);
            var loaded=JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(Path.Combine(folder,"player-state.json")),ManorPersistence.Json)!;
            var restored=new EmpireService(loaded.Empire);
            Assert.Equal(777m,loaded.Bankroll);
            Assert.Equal("neon-bay",restored.ActivePropertyId);
            Assert.Equal(150m,restored.Credits);
            Assert.Equal(1,restored.ResearchRank("automation"));
            var club=restored.PropertySnapshot("midnight-club");
            Assert.Contains("resort-wing",club.Projects);
            Assert.Equal("slots",club.Managers["elena"]);
            Assert.True(club.Automation.Enabled);
            Assert.Equal("dice",club.Event!.Id);
            Assert.Equal("tourist-weekend",club.Contract!.Id);
            Assert.False(File.Exists(Path.Combine(folder,"player-state.json.tmp")));
        }
        finally {if(Directory.Exists(folder))Directory.Delete(folder,true);}
    }

    [Fact]
    public void CompletedContractProgressPersistsAcrossPrestigeAndCannotPayTwice()
    {
        var service=Make(3,EmpireRules.MaximumCredits,100);
        service.AcceptContract("expansion-drive",Now);
        Assert.True(service.UpgradeMany("slots",50,Now));
        Assert.True(service.CanClaimContract);
        Assert.True(service.PrestigeEmpire(Now.AddSeconds(1)).Success);
        Assert.True(service.CanClaimContract);
        Assert.True(service.ClaimContract(Now.AddSeconds(1)).Success);
        Assert.False(service.ClaimContract(Now.AddSeconds(1)).Success);
        Assert.Equal(1,service.PropertySnapshot(service.ActivePropertyId).ContractsCompleted);
    }

    [Fact]
    public void MalformedNewSaveFieldsNormalizeWithoutOverspendingLegacyPoints()
    {
        EmpireState state=EmpireState.Normalize(new EmpireState {Prestige=1,Research=new(){["automation"]=1,["construction"]=99,["unknown"]=2}});
        var service=new EmpireService(state);
        Assert.Equal(1,service.ResearchRank("construction"));
        Assert.Equal(0,service.ResearchRank("automation"));
        Assert.Equal(0,service.LegacyPointsAvailable);
        EmpireState snapshot=service.Snapshot;
        snapshot.Properties[service.ActivePropertyId].Levels["slots"]=200;
        snapshot.Research.Clear();
        Assert.Equal(1,service.Level("slots"));
        Assert.Equal(1,service.ResearchRank("construction"));
    }
}
