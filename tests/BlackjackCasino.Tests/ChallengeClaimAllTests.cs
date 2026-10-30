using System.Collections.Concurrent;
using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class ChallengeClaimAllTests
{
    [Fact]
    public void EndgameCatalogSpansEveryGameLifetimeAndPrestigeWithValidDistinctRewards()
    {
        ChallengeDefinition[] endgame = ChallengeCatalog.All.Where(challenge => challenge.IsEndgame).ToArray();
        Assert.True(endgame.Length >= 15);
        Assert.Equal(ChallengeCatalog.All.Count, ChallengeCatalog.All.Select(challenge => challenge.Id).Distinct().Count());
        foreach (ChallengeCategory category in Enum.GetValues<ChallengeCategory>())
        {
            Assert.Contains(endgame, challenge => challenge.Category == category);
        }
        Assert.All(ChallengeCatalog.All, challenge =>
        {
            Assert.True(challenge.Goal > 0);
            Assert.True(challenge.CoinReward > 0);
        });
    }

    [Fact]
    public void EveryEndgameGoalRequiresItsExactBoundaryAndCanBeClaimedOnlyOnce()
    {
        foreach (ChallengeDefinition challenge in ChallengeCatalog.All.Where(challenge => challenge.IsEndgame))
        {
            var profile = new PlayerProfileState();
            var service = new ProgressionService(profile);
            SetEndgameMetric(profile, challenge.Metric, challenge.Goal - 1);
            Assert.False(ChallengeCatalog.Complete(profile, challenge));
            Assert.False(service.ClaimChallenge(challenge.Id).Success);
            SetEndgameMetric(profile, challenge.Metric, challenge.Goal);
            Assert.True(ChallengeCatalog.Complete(profile, challenge));
            Assert.True(service.ClaimChallenge(challenge.Id).Success);
            Assert.False(service.ClaimChallenge(challenge.Id).Success);
            Assert.Equal(challenge.CoinReward, profile.Coins);
        }
    }

    [Fact]
    public void ClaimAllPaysEarnedUnclaimedAcrossCategoriesOnlyAndIsIdempotentAfterReload()
    {
        PlayerProfileState profile = EarnedProfile();
        var service = new ProgressionService(profile);
        ChallengeClaimResult single = service.ClaimChallenge("bj-win-1");
        Assert.True(single.Success);
        ChallengeDefinition[] expected = Claimable(profile);
        long previousCoins = profile.Coins;

        ChallengeClaimAllResult result = service.ClaimAllChallenges();

        Assert.True(result.Success);
        Assert.Equal(expected.Select(challenge => challenge.Id), result.Challenges.Select(challenge => challenge.Id));
        Assert.Equal(expected.Length, result.ClaimsCount);
        Assert.Equal(expected.Sum(challenge => challenge.CoinReward), result.CoinsEarned);
        Assert.Equal(previousCoins + result.CoinsEarned, profile.Coins);
        Assert.DoesNotContain("bj-rounds-250", profile.ClaimedChallengeIds);
        Assert.DoesNotContain(result.Challenges, challenge => challenge.Id == "bj-win-1");
        Assert.False(service.ClaimAllChallenges().Success);
        Assert.False(service.ClaimChallenge(expected[0].Id).Success);
        PlayerProfileState restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        var reloaded = new ProgressionService(restored);
        Assert.False(reloaded.ClaimAllChallenges().Success);
        Assert.Equal(profile.Coins, restored.Coins);
        Assert.Equal(profile.ClaimedChallengeIds.Count, restored.ClaimedChallengeIds.Distinct().Count());
    }

    [Fact]
    public void ClaimAllDoesNothingWhenNoRewardsAreEarned()
    {
        var profile = new PlayerProfileState { Coins = 123 };
        var service = new ProgressionService(profile);
        ChallengeClaimAllResult result = service.ClaimAllChallenges();
        Assert.False(result.Success);
        Assert.Empty(result.Challenges);
        Assert.Equal(0, result.ClaimsCount);
        Assert.Equal(0, result.CoinsEarned);
        Assert.Equal(123, profile.Coins);
        Assert.Empty(profile.ClaimedChallengeIds);
    }

    [Fact]
    public void ConcurrentSingleAndBulkClaimsAcrossServicesNeverDoublePay()
    {
        PlayerProfileState profile = EarnedProfile();
        var first = new ProgressionService(profile);
        var second = new ProgressionService(profile);
        ChallengeDefinition[] expected = Claimable(profile);
        long before = profile.Coins;
        var rewards = new ConcurrentBag<long>();

        Parallel.For(0, 64, index =>
        {
            ProgressionService service = index % 2 == 0 ? first : second;
            rewards.Add(index % 3 == 0
                ? service.ClaimChallenge("bj-rounds-10").CoinsEarned
                : service.ClaimAllChallenges().CoinsEarned);
        });

        Assert.Equal(expected.Sum(challenge => challenge.CoinReward), rewards.Sum());
        Assert.Equal(before + rewards.Sum(), profile.Coins);
        Assert.Equal(expected.Length, profile.ClaimedChallengeIds.Count);
        Assert.Equal(expected.Length, profile.ClaimedChallengeIds.Distinct().Count());
    }

    [Fact]
    public void OverflowLeavesBothWalletAndClaimLedgerUntouchedForSingleAndBulkClaims()
    {
        PlayerProfileState profile = EarnedProfile();
        profile.Coins = long.MaxValue - 1;
        var service = new ProgressionService(profile);
        List<string> originalClaims = profile.ClaimedChallengeIds;

        Assert.Throws<OverflowException>(() => service.ClaimAllChallenges());
        Assert.Equal(long.MaxValue - 1, profile.Coins);
        Assert.Same(originalClaims, profile.ClaimedChallengeIds);
        Assert.Empty(profile.ClaimedChallengeIds);
        Assert.Throws<OverflowException>(() => service.ClaimChallenge("bj-rounds-10"));
        Assert.Equal(long.MaxValue - 1, profile.Coins);
        Assert.Empty(profile.ClaimedChallengeIds);
    }

    [Fact]
    public void ControllerWritesOneCompleteProfileForAllRewardsAndZeroForRepeatedClaims()
    {
        PlayerProfileState profile = EarnedProfile();
        int saves = 0;
        string? persisted = null;
        var controller = new PlayerProgressionController(profile, state =>
        {
            saves++;
            persisted = JsonSerializer.Serialize(state);
        });
        ChallengeDefinition[] expected = Claimable(profile);
        long before = profile.Coins;

        ChallengeClaimAllResult result = controller.ClaimAllChallenges();
        ChallengeClaimAllResult repeated = controller.ClaimAllChallenges();

        Assert.True(result.Success);
        Assert.False(repeated.Success);
        Assert.Equal(1, saves);
        Assert.NotNull(persisted);
        PlayerProfileState saved = JsonSerializer.Deserialize<PlayerProfileState>(persisted)!;
        Assert.Equal(before + expected.Sum(challenge => challenge.CoinReward), saved.Coins);
        Assert.Equal(expected.Select(challenge => challenge.Id), saved.ClaimedChallengeIds);
        Assert.False(new ProgressionService(saved).ClaimAllChallenges().Success);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ControllerSaveFailureRollsBackWholeBatchAndAllowsOneSuccessfulRetry(bool unauthorized)
    {
        PlayerProfileState profile = EarnedProfile();
        long before = profile.Coins;
        int saves = 0;
        var controller = new PlayerProgressionController(profile, _ =>
        {
            saves++;
            if (saves == 1)
            {
                if (unauthorized) throw new UnauthorizedAccessException("test failure");
                throw new IOException("test failure");
            }
        });
        ChallengeDefinition[] expected = Claimable(profile);

        ChallengeClaimAllResult failed = controller.ClaimAllChallenges();

        Assert.False(failed.Success);
        Assert.Empty(failed.Challenges);
        Assert.Equal(0, failed.CoinsEarned);
        Assert.Equal(before, profile.Coins);
        Assert.Empty(profile.ClaimedChallengeIds);
        ChallengeClaimAllResult retried = controller.ClaimAllChallenges();
        Assert.True(retried.Success);
        Assert.Equal(expected.Length, retried.ClaimsCount);
        Assert.Equal(before + expected.Sum(challenge => challenge.CoinReward), profile.Coins);
        Assert.False(controller.ClaimAllChallenges().Success);
        Assert.Equal(2, saves);
    }

    [Fact]
    public void PrestigeRetainsPerGameHistoryAndClaimedEndgameRewards()
    {
        PlayerProfileState profile = EarnedProfile();
        profile.Level = ProgressionCurve.MaximumLevel;
        profile.NeonVault.GrandJackpots = 10;
        profile.LegacyPooledSlots.Spins = 999;
        var service = new ProgressionService(profile);
        Assert.True(service.ClaimAllChallenges().Success);
        string[] originalClaims = profile.ClaimedChallengeIds.ToArray();

        Assert.True(service.Prestige().Success);

        Assert.Equal(4, profile.Prestige);
        Assert.Equal(25, profile.Slots.Spins);
        Assert.Equal(25, profile.NeonVault.Spins);
        Assert.Equal(10, profile.NeonVault.GrandJackpots);
        Assert.Equal(25, profile.Manor.PaidSpins);
        Assert.Equal(999, profile.LegacyPooledSlots.Spins);
        Assert.Equal(originalClaims, profile.ClaimedChallengeIds);
        Assert.False(service.ClaimAllChallenges().Success);
    }

    [Fact]
    public void ManorSnapshotAdoptionKeepsTrackingAndExistingServiceSessionState()
    {
        PlayerProfileState profile = EarnedProfile();
        var controller = new PlayerProgressionController(profile);
        controller.Service.SessionStatistics.RoundsPlayed = 6;
        PlayerProfileState committed = ManorPersistence.Clone(profile);
        committed.Manor.PaidSpins = 26;
        committed.NeonVault.Spins = 27;
        committed.NeonVault.GrandJackpots = 2;
        committed.Slots.Spins = 28;
        committed.LegacyPooledSlots.Spins = 29;

        controller.AdoptManorProfile(committed);

        Assert.Same(profile, controller.Service.State);
        Assert.Equal(6, controller.Service.SessionStatistics.RoundsPlayed);
        Assert.Equal(26, profile.Manor.PaidSpins);
        Assert.Equal(27, profile.NeonVault.Spins);
        Assert.Equal(2, profile.NeonVault.GrandJackpots);
        Assert.Equal(28, profile.Slots.Spins);
        Assert.Equal(29, profile.LegacyPooledSlots.Spins);
        Assert.Equal(1, profile.SlotTrackingVersion);
    }

    private static PlayerProfileState EarnedProfile()
    {
        var profile = new PlayerProfileState { Coins = 37, Prestige = 3, LifetimeExperience = 1000000 };
        _ = new ProgressionService(profile);
        profile.LifetimeStatistics.RoundsPlayed = 50;
        profile.LifetimeStatistics.HandsWon = 25;
        profile.Slots.Spins = 25;
        profile.NeonVault.Spins = 25;
        profile.Manor.PaidSpins = 25;
        return profile;
    }

    private static ChallengeDefinition[] Claimable(PlayerProfileState profile) => ChallengeCatalog.All
        .Where(challenge => ChallengeCatalog.Complete(profile, challenge)
            && !profile.ClaimedChallengeIds.Contains(challenge.Id, StringComparer.Ordinal))
        .ToArray();

    private static void SetEndgameMetric(PlayerProfileState profile, ChallengeMetric metric, decimal value)
    {
        switch (metric)
        {
            case ChallengeMetric.BlackjackRounds: profile.LifetimeStatistics.RoundsPlayed = (long)value; break;
            case ChallengeMetric.BlackjackWins: profile.LifetimeStatistics.HandsWon = (long)value; break;
            case ChallengeMetric.BlackjackBlackjacks: profile.LifetimeStatistics.Blackjacks = (long)value; break;
            case ChallengeMetric.BlackjackDoubleWins: profile.LifetimeStatistics.DoubleWins = (long)value; break;
            case ChallengeMetric.BlackjackWagered: profile.LifetimeStatistics.TotalWagered = value; break;
            case ChallengeMetric.ReelsSpins: profile.Slots.Spins = (long)value; break;
            case ChallengeMetric.ReelsWins: profile.Slots.Wins = (long)value; break;
            case ChallengeMetric.ReelsReturned: profile.Slots.TotalReturned = value; break;
            case ChallengeMetric.ReelsWagered: profile.Slots.TotalWagered = value; break;
            case ChallengeMetric.ManorPaidSpins: profile.Manor.PaidSpins = (long)value; break;
            case ChallengeMetric.ManorFreeSpins: profile.Manor.FreeSpins = (long)value; break;
            case ChallengeMetric.ManorExits: profile.Manor.Exits = (long)value; break;
            case ChallengeMetric.ManorMaximumExits: profile.Manor.MaximumExits = (long)value; break;
            case ChallengeMetric.NeonPaidSpins: profile.NeonVault.PaidSpins = (long)value; break;
            case ChallengeMetric.NeonFreeSpins: profile.NeonVault.FreeSpins = (long)value; break;
            case ChallengeMetric.NeonGrandJackpots: profile.NeonVault.GrandJackpots = (long)value; break;
            case ChallengeMetric.NeonTotalBlackouts: profile.NeonVault.TotalBlackouts = (long)value; break;
            case ChallengeMetric.NeonSecurityOverrides: profile.NeonVault.SecurityOverrides = (long)value; break;
            case ChallengeMetric.NineLivesPaidSpins: profile.NineLives.PaidSpins = (long)value; break;
            case ChallengeMetric.NineLivesBonusSpins: profile.NineLives.BonusSpins = (long)value; break;
            case ChallengeMetric.NineLivesPounceWilds: profile.NineLives.PounceWilds = (long)value; break;
            case ChallengeMetric.NineLivesFullGridFrenzies: profile.NineLives.FullGridFrenzies = (long)value; break;
            case ChallengeMetric.LifetimeExperience: profile.LifetimeExperience = (long)value; break;
            case ChallengeMetric.LifetimePlaytimeSeconds: profile.TotalPlaytimeSeconds = (long)value; break;
            case ChallengeMetric.LifetimeWagered: profile.LegacyPooledSlots.TotalWagered = value; break;
            case ChallengeMetric.LifetimePlays: profile.LegacyPooledSlots.Spins = (long)value; break;
            case ChallengeMetric.PrestigeCount: profile.Prestige = (int)value; break;
            case ChallengeMetric.DiceGames: profile.Kniffel.CompletedGames = (long)value; break;
            case ChallengeMetric.DiceBestScore: profile.Kniffel.BestScore = (int)value; break;
            case ChallengeMetric.DiceFiveOfAKinds: profile.Kniffel.FiveOfAKinds = (long)value; break;
            case ChallengeMetric.DiceCasinoWins: profile.Kniffel.CasinoWins = (long)value; break;
            case ChallengeMetric.DiceBiggestPayout: profile.Kniffel.BiggestPayout = value; break;
            case ChallengeMetric.SudokuCompleted: profile.Sudoku.CompletedPuzzles = (long)value; break;
            case ChallengeMetric.SudokuMasterCompleted: profile.Sudoku.MasterCompleted = (long)value; break;
            default: throw new InvalidOperationException($"Missing endgame boundary test for {metric}.");
        }
    }
}
