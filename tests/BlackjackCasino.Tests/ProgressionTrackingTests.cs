using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Manor;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ProgressionTrackingTests
{
    [Theory]
    [InlineData("\"SchemaVersion\":3,")]
    [InlineData("")]
    public void OldPooledHistoryIsArchivedExactlyWithoutAttributingEitherGame(string version)
    {
        PlayerProfileState profile = JsonSerializer.Deserialize<PlayerProfileState>("{" + version + """
            "Coins":321,"LifetimeExperience":456,"Prestige":2,
            "ClaimedChallengeIds":["reels-spins-25"],
            "Slots":{"Spins":80,"Wins":9,"Pushes":3,"TotalWagered":2000,
                     "TotalReturned":1900,"BiggestPayout":500},
            "Manor":{"PaidSpins":7,"TotalWagered":175},
            "LifetimeStatistics":{"RoundsPlayed":15}
            }
            """)!;

        var service = new ProgressionService(profile);

        Assert.Equal(4, profile.SchemaVersion);
        Assert.Equal(1, profile.SlotTrackingVersion);
        Assert.Equal(80, profile.LegacyPooledSlots.Spins);
        Assert.Equal(9, profile.LegacyPooledSlots.Wins);
        Assert.Equal(3, profile.LegacyPooledSlots.Pushes);
        Assert.Equal(2000, profile.LegacyPooledSlots.TotalWagered);
        Assert.Equal(1900, profile.LegacyPooledSlots.TotalReturned);
        Assert.Equal(500, profile.LegacyPooledSlots.BiggestPayout);
        Assert.Equal(-100, profile.LegacyPooledSlots.NetProfit);
        Assert.Equal(0, profile.Slots.Spins);
        Assert.Equal(0, profile.NeonVault.Spins);
        Assert.Equal(7, profile.Manor.PaidSpins);
        Assert.Equal(15, profile.LifetimeStatistics.RoundsPlayed);
        Assert.Equal(321, profile.Coins);
        Assert.Equal(456, profile.LifetimeExperience);
        Assert.Equal(2, profile.Prestige);
        Assert.Contains("reels-spins-25", profile.ClaimedChallengeIds);
        Assert.False(service.ClaimChallenge("reels-spins-25").Success);
        Assert.False(service.ClaimChallenge("neon-spins-25").Success);
        Assert.False(service.ClaimChallenge("reels-spins-100").Success);
    }

    [Fact]
    public void MigrationRoundTripAndRepeatedNormalizationNeverResetNewTracking()
    {
        var profile = new PlayerProfileState { Slots = new() { Spins = 90, TotalWagered = 2250 } };
        _ = new ProgressionService(profile);
        profile.Slots.Spins = 12;
        profile.NeonVault.Spins = 23;
        profile.NeonVault.GrandJackpots = 2;
        profile.Manor.PaidSpins = 34;
        PlayerProfileState restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;

        _ = new ProgressionService(restored);
        PlayerProfileStore.SanitizeProfile(restored);
        ProfileNormalization.Normalize(restored);

        Assert.Equal(90, restored.LegacyPooledSlots.Spins);
        Assert.Equal(2250, restored.LegacyPooledSlots.TotalWagered);
        Assert.Equal(12, restored.Slots.Spins);
        Assert.Equal(23, restored.NeonVault.Spins);
        Assert.Equal(2, restored.NeonVault.GrandJackpots);
        Assert.Equal(34, restored.Manor.PaidSpins);
        Assert.NotSame(restored.Slots, restored.LegacyPooledSlots);
        Assert.NotSame(restored.Slots, restored.NeonVault);
    }

    [Fact]
    public void NullLegacyCollectionsAndDictionaryValuesAreNormalizedBeforeStoreOrServiceUse()
    {
        const string json = """
            {"Slots":null,"NeonVault":null,"LegacyPooledSlots":null,"Manor":null,
             "LifetimeStatistics":null,"OwnedCosmeticIds":null,"ClaimedChallengeIds":null,
             "TableStatistics":{"Casual":null},"Seasons":{"season-1-midnight-shift":null}}
            """;
        PlayerProfileState profile = JsonSerializer.Deserialize<PlayerProfileState>(json)!;

        PlayerProfileStore.SanitizeProfile(profile);
        var service = new ProgressionService(profile);

        Assert.NotNull(profile.Slots);
        Assert.NotNull(profile.NeonVault);
        Assert.NotNull(profile.LegacyPooledSlots);
        Assert.NotNull(profile.Manor);
        Assert.NotNull(profile.LifetimeStatistics);
        Assert.NotNull(profile.TableStatistics["Casual"]);
        Assert.NotNull(profile.Seasons["season-1-midnight-shift"]);
        Assert.NotEmpty(profile.OwnedCosmeticIds);
        Assert.False(service.ClaimAllChallenges().Success);
    }

    [Fact]
    public void ReelsNeonAndManorStatisticsAndChallengesStayIsolated()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        var slots = new SlotProgression(service);
        var reels = new SlotMachineSession(1000, _ => 0);
        var manorState = new ManorSessionState { Bankroll = 1000, Statistics = profile.Manor };
        var manor = new ManorSession(manorState, _ => 0);
        _ = manor.Start(25);
        ManorProgression.Award(service, manorState);
        for (int i = 0; i < 25; i++)
        {
            slots.Process(NeonSession().Spin(25));
        }
        Assert.True(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("neon-spins-25")!));
        Assert.False(ChallengeCatalog.Complete(profile, ChallengeCatalog.TryGet("reels-spins-25")!));
        Assert.Equal(0, profile.Slots.Spins);

        slots.Process(reels.Spin(25));

        Assert.Equal(1, profile.Slots.Spins);
        Assert.Equal(25, profile.Slots.TotalWagered);
        Assert.Equal(25, profile.NeonVault.Spins);
        Assert.Equal(625, profile.NeonVault.TotalWagered);
        Assert.Equal(1, profile.Manor.PaidSpins);
        Assert.Equal(25, profile.Manor.TotalWagered);
        Assert.Equal(0, profile.LegacyPooledSlots.Spins);
        Assert.Equal(0, profile.LifetimeStatistics.RoundsPlayed);
        Assert.Empty(profile.TableStatistics);
    }

    [Fact]
    public void NeonFreeSpinsRetriggersAndGrandJackpotsUseActualStakeAndDeduplicateFeatures()
    {
        int[] trigger = NoMatchGrid();
        trigger[0] = trigger[1] = trigger[2] = 96;
        int[] grand = NoMatchGrid();
        for (int reel = 0; reel < 5; reel++) grand[reel] = 99;
        var random = new SequenceRandom(trigger.Concat([5000]).Concat(trigger).Concat([5000]).Concat(grand).Concat([5000]));
        var neon = new NeonVaultSession(1000, random.Next);
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        var slots = new SlotProgression(service);
        NeonVaultSpinResult paid = neon.Spin(25);
        NeonVaultSpinResult retrigger = neon.Spin(25);
        NeonVaultSpinResult jackpot = neon.Spin(25);

        ProgressionAward paidAward = slots.Process(paid);
        slots.Process(retrigger);
        slots.Process(jackpot);
        long xp = profile.LifetimeExperience;
        Assert.Equal(ProgressionAward.None, slots.Process(jackpot));
        Assert.Equal(ProgressionAward.None, slots.Process(retrigger));

        Assert.Equal(25, paidAward.PlayerExperience);
        Assert.Equal(3, profile.NeonVault.Spins);
        Assert.Equal(1, profile.NeonVault.PaidSpins);
        Assert.Equal(2, profile.NeonVault.FreeSpins);
        Assert.Equal(1, profile.NeonVault.FreeSpinSessions);
        Assert.Equal(16, profile.NeonVault.FreeSpinsAwarded);
        Assert.Equal(1, profile.NeonVault.Jackpots);
        Assert.Equal(1, profile.NeonVault.GrandJackpots);
        Assert.Equal(25, profile.NeonVault.TotalWagered);
        Assert.Equal(paid.Payout + retrigger.Payout + jackpot.Payout, profile.NeonVault.TotalReturned);
        Assert.Equal(jackpot.Payout / jackpot.Bet, profile.NeonVault.BestPayoutMultiplier);
        Assert.Equal(xp, profile.LifetimeExperience);
        Assert.Equal(0, profile.Slots.Spins);
    }

    [Fact]
    public void NeonTotalBlackoutAndSecurityOverrideCountersComeFromSettledResults()
    {
        int[] trigger = NoMatchGrid();
        trigger[0] = trigger[1] = trigger[2] = 96;
        int[] wilds = Enumerable.Repeat(92, 20).ToArray();
        var random = new SequenceRandom(trigger.Concat([5000]).Concat(wilds).Concat([5000])
            .Concat(NoMatchGrid()).Concat([5000, 0]));
        var neon = new NeonVaultSession(1000, random.Next);
        var profile = new PlayerProfileState();
        var slots = new SlotProgression(new ProgressionService(profile));
        slots.Process(neon.Spin(25));
        slots.Process(neon.Spin(25));
        NeonVaultSpinResult overridden = neon.Spin(25);
        Assert.NotNull(overridden.SecurityOverrideReel);
        slots.Process(overridden);

        var blackoutRandom = new SequenceRandom(NoMatchGrid().Concat([0, 0, 0, 0, 0]));
        NeonVaultSpinResult blackout = new NeonVaultSession(1000, blackoutRandom.Next).Spin(25);
        slots.Process(blackout);
        slots.Process(blackout);

        Assert.Equal(NeonVaultBlackout.Total, blackout.Blackout);
        Assert.Equal(1, profile.NeonVault.SecurityOverrides);
        Assert.Equal(1, profile.NeonVault.Blackouts);
        Assert.Equal(1, profile.NeonVault.TotalBlackouts);
    }

    [Fact]
    public void ZeroReturnFreeSpinIsAPushAndAwardsBaseXpWithoutAnotherWager()
    {
        int[] trigger = NoMatchGrid();
        trigger[0] = trigger[1] = trigger[2] = 96;
        var random = new SequenceRandom(trigger.Concat([5000]).Concat(NoMatchGrid()).Concat([5000]));
        var neon = new NeonVaultSession(1000, random.Next);
        var profile = new PlayerProfileState();
        var slots = new SlotProgression(new ProgressionService(profile));
        slots.Process(neon.Spin(25));

        ProgressionAward award = slots.Process(neon.Spin(25));

        Assert.Equal(20, award.PlayerExperience);
        Assert.Equal(15, award.SeasonExperience);
        Assert.Equal(2, profile.NeonVault.Spins);
        Assert.Equal(1, profile.NeonVault.PaidSpins);
        Assert.Equal(1, profile.NeonVault.FreeSpins);
        Assert.Equal(1, profile.NeonVault.Wins);
        Assert.Equal(1, profile.NeonVault.Pushes);
        Assert.Equal(25, profile.NeonVault.TotalWagered);
        Assert.Equal(50, profile.NeonVault.TotalReturned);
        Assert.Equal(25, profile.NeonVault.NetProfit);
    }

    [Fact]
    public void CrossGameLifetimeGoalsCountHistoricalPoolOnceWithoutSpecificGameAttribution()
    {
        var profile = new PlayerProfileState { Slots = new() { Spins = 50, TotalWagered = 1250 } };
        _ = new ProgressionService(profile);
        profile.LifetimeStatistics.RoundsPlayed = 10;
        profile.LifetimeStatistics.TotalWagered = 250;
        profile.Slots.Spins = 2;
        profile.Slots.TotalWagered = 50;
        profile.NeonVault.Spins = 4;
        profile.NeonVault.TotalWagered = 75;
        profile.Manor.PaidSpins = 3;
        profile.Manor.FreeSpins = 8;
        profile.Manor.TotalWagered = 75;

        Assert.Equal(77, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("lifetime-plays-100000")!));
        Assert.Equal(1700, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("lifetime-wager-10000000")!));
        Assert.Equal(2, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("reels-spins-25")!));
        Assert.Equal(4, ChallengeCatalog.Progress(profile, ChallengeCatalog.TryGet("neon-spins-25")!));
    }

    private static NeonVaultSession NeonSession()
    {
        var random = new SequenceRandom(NoMatchGrid().Concat([5000]));
        return new(1000, random.Next);
    }

    private static int[] NoMatchGrid() =>
    [
        0, 28, 50, 68, 82,
        0, 28, 50, 68, 82,
        0, 28, 50, 68, 82,
        0, 28, 50, 68, 82,
    ];

    private sealed class SequenceRandom(IEnumerable<int> values)
    {
        private readonly Queue<int> remaining = new(values);
        public int Next(int maximum)
        {
            int value = remaining.Count > 0 ? remaining.Dequeue() : maximum - 1;
            Assert.InRange(value, 0, maximum - 1);
            return value;
        }
    }
}
