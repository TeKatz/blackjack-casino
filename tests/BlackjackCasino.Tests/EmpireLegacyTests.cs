using BlackjackCasino.Core.Empire;
using Xunit;

namespace BlackjackCasino.Tests;

public class EmpireLegacyTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExistingClickerSaveKeepsItsEconomyAndReceivesPriorPrestigePointsOnce()
    {
        var saved = new EmpireState
        {
            SchemaVersion = EmpireState.CurrentSchemaVersion,
            Started = true, LastAccruedAt = Now,
            Credits = 42_000m, LifetimeCredits = 1_000_000m,
            Clicks = 520, Prestige = 2,
            Buildings = new() { ["cashier"] = 4, ["slots"] = 3 },
        };
        var upgraded = new EmpireService(saved);
        Assert.Equal(42_000m, upgraded.Credits);
        Assert.Equal(2, upgraded.Prestige);
        Assert.Equal(4, upgraded.Level("cashier"));
        Assert.Equal(3, upgraded.Level("slots"));
        Assert.True(upgraded.PrestigeTokens >= 4);
        int tokens = upgraded.PrestigeTokens;
        var reloaded = new EmpireService(upgraded.Snapshot);
        Assert.Equal(tokens, reloaded.PrestigeTokens);
        Assert.Equal(upgraded.CompletedMilestones.Count, reloaded.CompletedMilestones.Count);
    }

    [Fact]
    public void MilestonesPayExactlyOnceAcrossBatchesAndReloads()
    {
        var service = new EmpireService();
        service.Start(Now);
        Assert.True(service.Tap(20, Now).Success);
        Assert.True(service.Tap(20, Now).Success);
        Assert.True(service.Tap(10, Now).Success);
        Assert.Contains("click-50", service.CompletedMilestones);
        Assert.Equal(1, service.PrestigeTokens);
        var reloaded = new EmpireService(service.Snapshot);
        Assert.Equal(1, reloaded.PrestigeTokens);
        Assert.True(reloaded.Tap(1, Now).Success);
        Assert.Equal(1, reloaded.PrestigeTokens);
    }

    [Fact]
    public void PermanentUpgradesStayLockedBeforePrestigeEvenWithTokens()
    {
        var service = new EmpireService(new EmpireState
        {
            Started = true, LastAccruedAt = Now, PrestigeTokens = 20,
        });
        Assert.False(service.CanBuyLegacyUpgrade(EmpireLegacyCatalog.FindUpgrade("active-1")!));
        Assert.False(service.BuyLegacyUpgrade("active-1", Now).Success);
        Assert.Empty(service.PermanentUpgrades);
    }

    [Fact]
    public void PermanentUpgradesArePaidAndPersistWithPrerequisiteAndPrestigeFences()
    {
        var service = new EmpireService(new EmpireState
        {
            Started = true, LastAccruedAt = Now, Prestige = 1,
            GrantedPrestigeLevels = 1, PrestigeTokens = 4,
            Buildings = new() { ["cashier"] = 5 },
        });
        decimal initialClick = service.CreditsPerClick;
        int initialTokens = service.PrestigeTokens; // includes auto-earned building and prestige milestones
        Assert.False(service.BuyLegacyUpgrade("active-3", Now).Success);
        Assert.True(service.BuyLegacyUpgrade("active-1", Now).Success);
        Assert.True(service.CreditsPerClick > initialClick);
        Assert.True(service.BuyLegacyUpgrade("active-2", Now).Success);
        Assert.Equal(4m, service.CriticalClickMultiplier);
        Assert.False(service.BuyLegacyUpgrade("active-2", Now).Success);
        Assert.False(service.BuyLegacyUpgrade("active-3", Now).Success);
        Assert.Equal(initialTokens - 3, service.PrestigeTokens);
        var reloaded = new EmpireService(service.Snapshot);
        Assert.Equal(initialTokens - 3, reloaded.PrestigeTokens);
        Assert.True(reloaded.OwnsLegacyUpgrade("active-1"));
        Assert.True(reloaded.OwnsLegacyUpgrade("active-2"));
    }

    [Fact]
    public void ProductionAndHybridPathsMultiplyTheRealIncomeWithoutDuplicatingBonuses()
    {
        var saved = new EmpireState
        {
            Started = true, LastAccruedAt = Now, Prestige = 1,
            GrantedPrestigeLevels = 1, PrestigeTokens = 4,
            Buildings = new() { ["cashier"] = 3, ["slots"] = 4 },
        };
        var service = new EmpireService(saved);
        decimal beforeProduction = service.CreditsPerSecond;
        decimal beforeClick = service.CreditsPerClick;
        Assert.True(service.BuyLegacyUpgrade("passive-1", Now).Success);
        Assert.True(service.CreditsPerSecond > beforeProduction);
        Assert.True(service.BuyLegacyUpgrade("hybrid-1", Now).Success);
        Assert.True(service.CreditsPerClick > beforeClick);
        Assert.Equal(1.23m, service.LegacyProductionMultiplier);
        Assert.Equal(1.08m, service.LegacyClickMultiplier);
    }

    [Fact]
    public void BuildingPeakAndMilestonesSurvivePrestigeWhileBuildingsReset()
    {
        var service = new EmpireService(new EmpireState
        {
            Started = true, LastAccruedAt = Now, Prestige = 0,
            RunCredits = 300_000m, LifetimeCredits = 300_000m,
            Buildings = new() { ["cashier"] = 8, ["slots"] = 8, ["tables"] = 4 },
        });
        int priorTokens = service.PrestigeTokens;
        Assert.True(service.CanPrestige);
        Assert.True(service.ResetForPrestige(Now).Success);
        Assert.Equal(1, service.Prestige);
        Assert.Equal(0, service.TotalLevels);
        Assert.Equal(20, service.PeakBuildingLevels);
        Assert.Contains("build-20", service.CompletedMilestones);
        Assert.Contains("prestige-1", service.CompletedMilestones);
        Assert.True(service.PrestigeTokens >= priorTokens + 3);
        Assert.True(service.BuyLegacyUpgrade("active-1", Now).Success);
        Assert.Contains("active-1", new EmpireService(service.Snapshot).PermanentUpgrades);
    }

    [Fact]
    public void ProfileNormalizationRejectsUnknownAndDuplicateRewardIds()
    {
        var saved = EmpireState.Normalize(new EmpireState
        {
            Started = true, Prestige = 3,
            PrestigeTokens = 99_999, GrantedPrestigeLevels = 900,
            PeakBuildingLevels = 900,
            PermanentUpgrades = ["active-1", "active-1", "not-real"],
            CompletedMilestones = ["click-50", "click-50", "not-real"],
        });
        Assert.Single(saved.PermanentUpgrades);
        Assert.Single(saved.CompletedMilestones);
        Assert.Equal(EmpireLegacyCatalog.MaximumTokens, saved.PrestigeTokens);
        Assert.Equal(3, saved.GrantedPrestigeLevels);
        Assert.Equal(96, saved.PeakBuildingLevels);
    }

    [Fact]
    public void FailedPersistOfPermanentUpgradeDoesNotConsumeTokensOrMutateState()
    {
        var service = new EmpireService(new EmpireState
        {
            Started = true, LastAccruedAt = Now,
            Prestige = 1, GrantedPrestigeLevels = 1, PrestigeTokens = 3,
        });
        int before = service.PrestigeTokens;
        Assert.Throws<IOException>(() => service.Transact(
            candidate => candidate.BuyLegacyUpgrade("active-1", Now),
            _ => throw new IOException("disk full")));
        Assert.Equal(before, service.PrestigeTokens);
        Assert.Empty(service.PermanentUpgrades);
    }
}
