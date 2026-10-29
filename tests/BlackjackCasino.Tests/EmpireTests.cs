using System.Text.Json;
using BlackjackCasino.Core.Empire;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class EmpireTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private static EmpireService Started(EmpireState? state = null)
    {
        var service = new EmpireService(state);
        service.Start(Now);
        return service;
    }

    [Fact]
    public void FirstVisitStartsProductionWithoutAnUnseenOfflineWindfall()
    {
        var service = new EmpireService();
        Assert.Equal(0m, service.Accrue(Now).Credits);
        service.Start(Now);
        Assert.Equal(150m, service.Credits);
        Assert.Equal(.5m, service.CreditsPerSecond);
        service.Accrue(Now.AddMinutes(2));
        Assert.Equal(210m, service.Credits);
        Assert.Equal(0m, service.Accrue(Now.AddMinutes(2)).Credits);
        service.Start(Now.AddHours(1));
        Assert.Equal(Now.AddMinutes(2), service.Snapshot.LastAccruedAt);
    }

    [Fact]
    public void OfflineAndFrequentOnlineTicksProduceTheSameEconomy()
    {
        var offline = Started();
        var online = Started();
        offline.Accrue(Now.AddHours(1));
        for (int i = 1; i <= 3600; i++) online.Accrue(Now.AddSeconds(i));
        Assert.Equal(offline.Credits, online.Credits);
        Assert.InRange(Math.Abs(offline.Dividend - online.Dividend), 0, .00000001m);
        Assert.Equal(offline.ClaimableDividend, online.ClaimableDividend);
    }

    [Fact]
    public void OfflineCapConsumesExcessTimeAndCannotBeClaimedRepeatedly()
    {
        var service = Started();
        EmpireAccrual result = service.Accrue(Now.AddDays(3));
        Assert.True(result.Capped);
        Assert.Equal(TimeSpan.FromHours(8), result.Elapsed);
        Assert.Equal(14_400m, result.Credits);
        Assert.Equal(0m, service.Accrue(Now.AddDays(3)).Credits);
        Assert.Equal(.5m, service.Accrue(Now.AddDays(3).AddSeconds(1)).Credits);
    }

    [Fact]
    public void ClockRollbackDoesNotDuplicateProduction()
    {
        var service = Started();
        service.Accrue(Now.AddHours(1));
        decimal earned = service.Credits;
        service.Accrue(Now.AddMinutes(30));
        service.Accrue(Now.AddHours(1));
        Assert.Equal(earned, service.Credits);
        Assert.Equal(.5m, service.Accrue(Now.AddHours(1).AddSeconds(1)).Credits);
    }

    [Fact]
    public void PurchaseAccruesAtOldRateThenOnlySpendsInternalCredits()
    {
        var service = Started();
        Assert.True(service.Upgrade("slots", Now.AddSeconds(10)));
        Assert.Equal(96m, service.Credits); // 150 + 5 - ceil(50 * 1.18).
        Assert.Equal(2, service.Level("slots"));
        Assert.Equal(1m, service.Accrue(Now.AddSeconds(11)).Credits);
        Assert.False(service.Upgrade("vip", Now.AddSeconds(11)));
        Assert.False(service.Upgrade("unknown", Now.AddSeconds(11)));
    }

    [Fact]
    public void EachTenthLevelDoublesProductionAndMaxLevelRejectsPurchases()
    {
        var service = Started(new EmpireState { Credits = 10_000, Levels = new() { ["slots"] = 9 } });
        Assert.Equal(4.5m, service.CreditsPerSecond);
        Assert.True(service.Upgrade("slots", Now));
        Assert.Equal(10m, service.CreditsPerSecond);
        var maxed = Started(new EmpireState { Credits = EmpireRules.MaximumCredits, Levels = new() { ["slots"] = EmpireRules.MaximumLevel } });
        Assert.False(maxed.Upgrade("slots", Now));
        Assert.True(maxed.CreditsPerSecond > 0);
    }

    [Fact]
    public void MalformedValuesAreBoundedAndStarterCannotBeRemoved()
    {
        EmpireState state = EmpireState.Normalize(new EmpireState
        {
            Credits = -10, LifetimeCredits = decimal.MaxValue, Dividend = decimal.MaxValue,
            Levels = new() { ["slots"] = -10, ["vip"] = int.MaxValue, ["unknown"] = 10 },
        });
        Assert.Equal(0m, state.Credits);
        Assert.Equal(EmpireRules.MaximumCredits, state.LifetimeCredits);
        Assert.Equal(EmpireRules.MaximumDividend, state.Dividend);
        Assert.Equal(1, state.Levels["slots"]);
        Assert.Equal(200, state.Levels["vip"]);
        Assert.False(state.Levels.ContainsKey("unknown"));
        Assert.Equal(1, EmpireState.Normalize(new EmpireState { Levels = null! }).Levels["slots"]);
    }

    [Fact]
    public void SnapshotsCannotMutateTheLiveEconomy()
    {
        var service = Started();
        service.Snapshot.Levels["slots"] = 200;
        Assert.Equal(1, service.Level("slots"));
    }

    [Fact]
    public void DividendIsBoundedAndClaimCannotRepeatOrSpendCredits()
    {
        var service = Started(new EmpireState { Dividend = 1.239m });
        decimal credits = service.Credits;
        decimal wallet = 1_000;
        decimal claimed = service.ClaimDividend(Now, (_, cash) => wallet += cash);
        Assert.Equal(1.23m, claimed);
        Assert.Equal(1_001.23m, wallet);
        Assert.Equal(.009m, service.Dividend);
        Assert.Equal(credits, service.Credits);
        Assert.Equal(0m, service.ClaimDividend(Now, (_, _) => Assert.Fail("Duplicate payout")));
        service.Accrue(Now.AddDays(10));
        Assert.InRange(service.Dividend, 0, EmpireRules.MaximumDividend);
    }

    [Fact]
    public void FailedDividendCommitLeavesTheReserveAvailableForRetry()
    {
        var service = Started(new EmpireState { Dividend = 25m });
        Assert.Throws<IOException>(() => service.ClaimDividend(Now, (_, _) => throw new IOException("disk unavailable")));
        Assert.Equal(25m, service.ClaimableDividend);
        Assert.Equal(0m, service.Snapshot.LifetimeCashClaimed);
        Assert.Equal(25m, service.ClaimDividend(Now, (_, _) => { }));
    }

    [Fact]
    public void WalletAndConsumedDividendReloadTogetherAndLegacySavesStillWork()
    {
        string folder = Path.Combine(Path.GetTempPath(), "empire-test-" + Guid.NewGuid());
        try
        {
            var service = Started(new EmpireState { Dividend = 10m });
            service.ClaimDividend(Now, (state, cash) =>
                PlayerStateStore.SaveOrThrow(new AppPreferences { Bankroll = 1_000 + cash, Empire = state }, folder));
            string path = Path.Combine(folder, "player-state.json");
            AppPreferences loaded = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(path), ManorPersistence.Json)!;
            Assert.Equal(1_010m, loaded.Bankroll);
            var restored = new EmpireService(loaded.Empire);
            Assert.Equal(0m, restored.ClaimDividend(Now, (_, _) => Assert.Fail("Payout survived as unclaimed")));
            Assert.Equal(150m, restored.Credits);
            Assert.False(File.Exists(path + ".tmp"));
            AppPreferences legacy = JsonSerializer.Deserialize<AppPreferences>("{\"Bankroll\":777}")!;
            Assert.Equal(777m, legacy.Bankroll);
            Assert.False(new EmpireService(legacy.Empire).Started);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void FailedAtomicReplacementPreservesThePreviousSaveAndReserve()
    {
        string folder = Path.Combine(Path.GetTempPath(), "empire-blocked-" + Guid.NewGuid());
        try
        {
            PlayerStateStore.SaveOrThrow(new AppPreferences { Bankroll = 777m }, folder);
            Directory.CreateDirectory(Path.Combine(folder, "player-state.json.tmp"));
            var service = Started(new EmpireState { Dividend = 10m });
            Assert.ThrowsAny<Exception>(() => service.ClaimDividend(Now, (state, cash) =>
                PlayerStateStore.SaveOrThrow(new AppPreferences { Bankroll = 777 + cash, Empire = state }, folder)));
            Assert.Equal(10m, service.ClaimableDividend);
            AppPreferences previous = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(Path.Combine(folder, "player-state.json")), ManorPersistence.Json)!;
            Assert.Equal(777m, previous.Bankroll);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true); }
    }
}
