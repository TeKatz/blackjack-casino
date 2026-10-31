using BlackjackCasino.Core.Empire;

namespace BlackjackCasino.Tests;

public sealed class Empire2Tests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BulkUpgradeUsesExactEscalatingCostAndCapsAtMaximumLevel()
    {
        EmpireDepartment slots = EmpireCatalog.Find("slots")!;
        decimal expected = Enumerable.Range(1, 5).Sum(level => EmpireRules.UpgradeCost(slots, level));
        var service = Started(new EmpireState { Credits = 1_000_000m, Levels = new() { ["slots"] = 1 } });

        EmpireUpgradeQuote quote = service.QuoteUpgrade("slots", 5);
        Assert.Equal(5, quote.Levels);
        Assert.Equal(1, quote.StartLevel);
        Assert.Equal(6, quote.TargetLevel);
        Assert.Equal(expected, quote.Cost);

        decimal before = service.Credits;
        Assert.True(service.UpgradeMany("slots", 5, Now));
        Assert.Equal(6, service.Level("slots"));
        Assert.Equal(before - expected, service.Credits);

        var nearCap = Started(new EmpireState { Credits = EmpireRules.MaximumCredits, Levels = new() { ["slots"] = 198 } });
        EmpireUpgradeQuote capped = nearCap.QuoteUpgrade("slots", 100);
        Assert.Equal(2, capped.Levels);
        Assert.Equal(200, capped.TargetLevel);
        Assert.Equal(EmpireRules.UpgradeCost(slots, 198) + EmpireRules.UpgradeCost(slots, 199), capped.Cost);
    }

    [Fact]
    public void ClaimedAchievementsProvideRoomAndGlobalProductionBoosts()
    {
        var service = Started();
        Assert.Equal(.5m, service.RoomCreditsPerSecond("slots"));

        service.SetClaimedAchievements(["reels-spins-1500"]);
        Assert.Equal(.525m, service.RoomCreditsPerSecond("slots"));
        Assert.Equal(0m, service.AchievementBonusPercent("tables"));

        service.SetClaimedAchievements(["reels-spins-1500", "lifetime-xp-1000000"]);
        Assert.Equal(.55m, service.RoomCreditsPerSecond("slots"));
        Assert.Equal(.05m, service.AchievementBonusPercent("tables"));
        Assert.Contains(service.ActiveAchievementBoosts, boost => boost.ChallengeId == "reels-spins-1500");
    }

    [Fact]
    public void OfflineAccrualReturnsPerRoomBreakdownThatMatchesPaidCredits()
    {
        var service = Started(new EmpireState
        {
            Credits = 10_000m,
            Levels = new() { ["slots"] = 2, ["tables"] = 1 },
        });
        service.SetClaimedAchievements(["reels-spins-1500"]);

        EmpireAccrual report = service.Accrue(Now.AddHours(1));
        Assert.Equal(report.Credits, report.Rooms.Values.Sum());
        Assert.True(report.Rooms["slots"] > 0);
        Assert.True(report.Rooms["tables"] > 0);
        Assert.Equal(TimeSpan.FromHours(1), report.Elapsed);
        Assert.False(report.Capped);
    }

    [Fact]
    public void UpperFloorIsServiceLockedUntilFirstEmpirePrestige()
    {
        var locked = Started(new EmpireState { Credits = EmpireRules.MaximumCredits });
        Assert.False(locked.Floor2Unlocked);
        Assert.False(locked.UpgradeMany("high-limit-slots", 1, Now));
        Assert.Equal(0, locked.Level("high-limit-slots"));

        Dictionary<string, int> levels = EmpireCatalog.Floor(1).ToDictionary(room => room.Id, _ => 50, StringComparer.Ordinal);
        var qualified = new EmpireService(new EmpireState
        {
            Started = true,
            Credits = 25_000_000m,
            LifetimeCredits = 123_000_000m,
            RunCredits = EmpireRules.FirstPrestigeRunCredits,
            Dividend = 17.25m,
            LastAccruedAt = Now,
            Levels = levels,
        });
        Assert.True(qualified.CanPrestige);

        EmpirePrestigeResult result = qualified.PrestigeEmpire(Now);
        Assert.True(result.Success);
        Assert.Equal(1, qualified.Prestige);
        Assert.True(qualified.Floor2Unlocked);
        Assert.Equal(150m, qualified.Credits);
        Assert.Equal(1, qualified.Level("slots"));
        Assert.Equal(0, qualified.Level("tables"));
        Assert.Equal(123_000_000m, qualified.Snapshot.LifetimeCredits);
        Assert.Equal(17.25m, qualified.Dividend);

        var unlocked = Started(new EmpireState
        {
            Prestige = 1,
            Credits = EmpireRules.MaximumCredits,
            Levels = new() { ["slots"] = 1 },
        });
        Assert.True(unlocked.UpgradeMany("high-limit-slots", 1, Now));
        Assert.Equal(1, unlocked.Level("high-limit-slots"));
    }

    [Fact]
    public void EmpirePrestigeAndAchievementBoostsStackWithoutTouchingPlayerPrestige()
    {
        var service = Started(new EmpireState { Prestige = 1, Levels = new() { ["slots"] = 1 } });
        service.SetClaimedAchievements(["reels-spins-1500"]);

        Assert.Equal(1.25m, service.PrestigeMultiplier);
        Assert.Equal(.65625m, service.RoomCreditsPerSecond("slots")); // .5 * 1.25 * 1.05
        Assert.Equal(.25m, service.PrestigeBonusPercent);
    }

    [Fact]
    public void LegacyEmpireStateNormalizesIntoNewSchemaWithoutUnlockingUpperFloor()
    {
        EmpireState state = EmpireState.Normalize(new EmpireState
        {
            SchemaVersion = 1,
            Credits = 777m,
            Levels = new() { ["slots"] = 4, ["tables"] = 2 },
        });

        Assert.Equal(EmpireState.CurrentSchemaVersion, state.SchemaVersion);
        Assert.Equal(777m, state.Credits);
        Assert.Equal(4, state.Levels["slots"]);
        Assert.Equal(2, state.Levels["tables"]);
        Assert.Equal(0, state.Levels["high-limit-slots"]);
        Assert.Equal(0, state.Prestige);
    }

    private static EmpireService Started(EmpireState? state = null)
    {
        var service = new EmpireService(state);
        service.Start(Now);
        return service;
    }
}
