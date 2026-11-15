using BlackjackCasino.Core.Empire;
using Xunit;

namespace BlackjackCasino.Tests;

public class EmpireClickerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ActiveClicksStartTheEconomyAndUnlockTheFirstPassiveBuilding()
    {
        var service = new EmpireService();
        service.Start(Now);
        Assert.Equal(1m, service.CreditsPerClick);
        Assert.Equal(0m, service.CreditsPerSecond);
        Assert.True(service.Tap(12, Now).Success);
        Assert.Equal(12m, service.Credits);
        Assert.True(service.Upgrade("cashier", Now).Success);
        Assert.Equal(1, service.Level("cashier"));
        Assert.True(service.Unlocked(EmpireCatalog.Find("slots")!));
        Assert.True(service.CreditsPerClick > 1m);
        Assert.Equal(0m, service.Credits);
    }

    [Fact]
    public void EveryTwentyFifthClickHasOneDeterministicCriticalReward()
    {
        var service = new EmpireService();
        service.Start(Now);
        Assert.True(service.Tap(20, Now).Success);
        Assert.True(service.Tap(5, Now).Success);
        Assert.Equal(27m, service.Credits);
        Assert.Equal(25, service.Clicks);
        Assert.False(service.Tap(21, Now).Success);
        Assert.Equal(25, service.Clicks);
    }

    [Fact]
    public void OfflineProductionIsCappedButTheClockAdvancesFully()
    {
        var starting = new EmpireState
        {
            Started = true,
            LastAccruedAt = Now,
            Buildings = new() { ["cashier"] = 1, ["slots"] = 1 },
        };
        var service = new EmpireService(starting);
        decimal perSecond = service.CreditsPerSecond;
        Assert.True(perSecond > 0m);
        EmpireAccrual accrual = service.Accrue(Now.AddHours(24));
        Assert.True(accrual.Capped);
        Assert.Equal(TimeSpan.FromHours(4), accrual.Elapsed);
        Assert.Equal(perSecond * 4m * 3600m, accrual.Credits);
        Assert.Equal(0m, service.Accrue(Now.AddHours(23)).Credits);
        Assert.Equal(0m, service.Accrue(Now.AddHours(24)).Credits);
    }

    [Fact]
    public void EveryBuildingHasExactlyEightLevelsAndAVisualDevelopmentTier()
    {
        Assert.Equal(12, EmpireCatalog.Buildings.Count);
        foreach (EmpireBuilding b in EmpireCatalog.Buildings)
        {
            Assert.True(EmpireCatalog.NextCost(b, 0) > 0m);
            Assert.Equal(0m, EmpireCatalog.NextCost(b, 8));
        }
        Assert.Equal(0, EmpireCatalog.VisualTier(0));
        Assert.Equal(1, EmpireCatalog.VisualTier(1));
        Assert.Equal(2, EmpireCatalog.VisualTier(3));
        Assert.Equal(3, EmpireCatalog.VisualTier(5));
        Assert.Equal(4, EmpireCatalog.VisualTier(8));
    }

    [Fact]
    public void PrestigeResetsBuildingsButNotAchievementsOrClickHistory()
    {
        var state = new EmpireState
        {
            Started = true, LastAccruedAt = Now,
            Credits = 50_000m, RunCredits = 300_000m, LifetimeCredits = 300_000m,
            Clicks = 432,
            Buildings = new() { ["cashier"] = 8, ["slots"] = 8, ["tables"] = 4 },
        };
        var service = new EmpireService(state);
        Assert.True(service.CanPrestige);
        Assert.True(service.ResetForPrestige(Now).Success);
        Assert.Equal(1, service.Prestige);
        Assert.Equal(0m, service.Credits);
        Assert.Equal(0, service.TotalLevels);
        Assert.Equal(432, service.Clicks);
        Assert.Equal(300_000m, service.LifetimeCredits);
    }

    [Fact]
    public void LegacySchemaNeverImportsUnboundedIdleLevelsOrCredits()
    {
        var service = new EmpireService(new EmpireState
        {
            SchemaVersion = 3, Started = true,
            Credits = 100_000_000m, Prestige = 42,
            Buildings = new() { ["cashier"] = 9000 },
        });
        Assert.False(service.Started);
        Assert.Equal(0m, service.Credits);
        Assert.Equal(0, service.Prestige);
        Assert.Equal(0, service.Level("cashier"));
    }

    [Fact]
    public void FailedDurableCommitCannotChangeTheRunningEconomy()
    {
        var service = new EmpireService();
        service.Start(Now);
        Assert.Throws<IOException>(() => service.Transact(
            candidate => candidate.Tap(10, Now), _ => throw new IOException("disk unavailable")));
        Assert.Equal(0m, service.Credits);
        Assert.Equal(0, service.Clicks);
    }
}
