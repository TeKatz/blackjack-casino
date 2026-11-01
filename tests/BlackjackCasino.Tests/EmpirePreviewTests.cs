using System.Text.Json;
using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Tests;

public sealed class EmpirePreviewTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static EmpireService Estate(int level = 24)
    {
        EmpireState state = EmpireState.Normalize(new EmpireState { Started = true, LastAccruedAt = Now,
            Prestige = 25, Credits = EmpireRules.MaximumCredits,
            Levels = EmpireCatalog.Departments.ToDictionary(r => r.Id, _ => level),
            Research = new() { ["construction"] = 2, ["compound"] = 2, ["automation"] = 1 } });
        EmpirePropertyState property = state.Properties[state.ActivePropertyId] with { Projects = new(["executive-offices", "resort-wing"]) };
        return new(state with { Properties = new(state.Properties) { [state.ActivePropertyId] = property } });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public void UpgradePreviewIsIsolatedAndMatchesCommittedEconomy(int count)
    {
        var service = Estate();
        service.SetClaimedAchievements(["reels-spins-1500", "lifetime-xp-1000000"]);
        string before = JsonSerializer.Serialize(service.Snapshot);
        EmpireActionPreview preview = service.PreviewUpgrade("slots", count);
        Assert.True(preview.Success);
        Assert.Equal(before, JsonSerializer.Serialize(service.Snapshot));
        Assert.True(service.UpgradeMany("slots", count, Now));
        Assert.Equal(service.Credits, preview.BudgetAfter);
        Assert.Equal(service.Level("slots"), preview.LevelAfter);
        Assert.Equal(service.TotalCreditsPerSecond, preview.PortfolioAfter);
        Assert.Equal(service.ProductionBreakdown("slots"), preview.RoomAfter);
        Assert.Equal(service.Synergies.Select(s => s.Tier), preview.SynergiesAfter.Select(s => s.Tier));
    }

    [Fact]
    public void CompletingCircuitPreviewsItsBenefitToOtherRooms()
    {
        var service = Estate(25);
        var state = service.Snapshot;
        var property = state.Properties[state.ActivePropertyId];
        property = property with { Levels = new(property.Levels) { ["slots"] = 24 } };
        service = new(state with { Properties = new(state.Properties) { [state.ActivePropertyId] = property } });
        EmpireActionPreview preview = service.PreviewUpgrade("slots", 1);
        Assert.Equal(1, preview.SynergiesAfter.Single(s => s.Definition.Id == "classic-casino").Tier);
        Assert.True(preview.PortfolioAfter - preview.PortfolioBefore > preview.RoomAfter.Final - preview.RoomBefore.Final);
        Assert.Equal(0, service.Synergies.Single(s => s.Definition.Id == "classic-casino").Tier);
    }

    [Fact]
    public void MovingSpecialistAccountsForLostProductionAtPreviousRoom()
    {
        var service = Estate(50);
        Assert.True(service.AssignManager("elena", "private-salon", Now).Success);
        EmpireActionPreview preview = service.Preview("slots", (s, at) => s.AssignManager("elena", "slots", at));
        Assert.True(preview.Success);
        Assert.True(preview.RoomAfter.Final > preview.RoomBefore.Final);
        Assert.True(preview.PortfolioAfter < preview.PortfolioBefore);
        Assert.Equal("private-salon", service.ManagerAssignments["elena"]);
        Assert.True(service.AssignManager("elena", "slots", Now).Success);
        Assert.Equal(service.TotalCreditsPerSecond, preview.PortfolioAfter);
    }

    [Fact]
    public void AutomationPreviewDoesNotInventAnImmediateProductionBonusOrRunPurchases()
    {
        var service = Estate(50);
        var preview = service.Preview("slots", (s, at) => s.ConfigureAutomation(s.Automation with { Enabled = true }, at));
        Assert.True(preview.Success);
        Assert.True(preview.AutomationAfter.Enabled);
        Assert.False(service.Automation.Enabled);
        Assert.Equal(preview.PortfolioBefore, preview.PortfolioAfter);
        Assert.Equal(preview.BudgetBefore, preview.BudgetAfter);
        Assert.Equal(preview.LevelBefore, preview.LevelAfter);
        Assert.True(service.ConfigureAutomation(service.Automation with { Enabled = true }, Now).Success);
        Assert.Equal(service.Automation.Enabled, preview.AutomationAfter.Enabled);
    }

    [Theory]
    [InlineData("missing", 1)]
    [InlineData("slots", 0)]
    [InlineData("slots", -1)]
    public void InvalidPreviewsReturnUnchangedValues(string room, int count)
    {
        var service = Estate();
        string before = JsonSerializer.Serialize(service.Snapshot);
        var preview = service.PreviewUpgrade(room, count);
        Assert.False(preview.Success);
        Assert.Equal(preview.BudgetBefore, preview.BudgetAfter);
        Assert.Equal(preview.PortfolioBefore, preview.PortfolioAfter);
        Assert.Equal(before, JsonSerializer.Serialize(service.Snapshot));
    }

    [Fact]
    public void LockedUnaffordableAndFullyUpgradedRoomsCannotBePreviewedAsPurchases()
    {
        var service = new EmpireService(new EmpireState { Started = true, LastAccruedAt = Now, Credits = 0 });
        Assert.False(service.PreviewUpgrade("slots", 1).Success);
        Assert.False(service.PreviewUpgrade("hotel", 1).Success);
        var full = Estate(200);
        Assert.False(full.PreviewUpgrade("slots", 1).Success);
        var almostFull = Estate(199);
        var preview = almostFull.PreviewUpgrade("slots", 100);
        Assert.Equal(200, preview.LevelAfter);
        Assert.True(almostFull.UpgradeMany("slots", 100, Now));
        Assert.Equal(almostFull.TotalCreditsPerSecond, preview.PortfolioAfter);
    }

    [Fact]
    public void BreakdownUsesTheSameFactorsAsTheLiveRateIncludingAchievementsAndEffects()
    {
        var service = Estate(100);
        service.SetClaimedAchievements(EmpireAchievementBoostCatalog.All.Select(b => b.ChallengeId));
        Assert.True(service.AssignManager("elena", "slots", Now).Success);
        Assert.True(service.StartEvent("neon", Now).Success);
        foreach (EmpireDepartment room in EmpireCatalog.Departments)
        {
            var b = service.ProductionBreakdown(room.Id);
            decimal expected = Math.Min(100_000_000_000_000_000_000m, b.Base * b.Prestige * b.Achievements
                * (1 + Math.Min(5m, b.Strategy + b.Managers + b.Synergies + b.Projects + b.Events + b.Contracts)));
            Assert.Equal(expected, b.Final);
            Assert.Equal(service.RoomCreditsPerSecond(room.Id), b.Final);
        }
        var preview = service.PreviewUpgrade("slots", 5);
        Assert.True(preview.RoomAfter.Achievements > 1);
        Assert.Equal(service.ProductionBreakdown("slots").Achievements, preview.RoomAfter.Achievements);
    }

    [Fact]
    public void PortfolioPreviewIncludesOtherPropertiesWithoutChangingTheirState()
    {
        var service = Estate(100);
        var state = service.Snapshot;
        service = new(state with { Properties = new(state.Properties) { ["neon-bay"] = new() { Prestige = 2, Levels = new() { ["slots"] = 50 } } } });
        decimal other = service.TotalCreditsPerSecond - service.CreditsPerSecond;
        var preview = service.PreviewUpgrade("slots", 5);
        Assert.True(service.UpgradeMany("slots", 5, Now));
        Assert.Equal(other, service.TotalCreditsPerSecond - service.CreditsPerSecond);
        Assert.Equal(service.TotalCreditsPerSecond, preview.PortfolioAfter);
    }
}
