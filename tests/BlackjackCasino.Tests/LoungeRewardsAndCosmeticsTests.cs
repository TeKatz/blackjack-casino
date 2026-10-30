using System.Text.Json;
using BlackjackCasino.Core;
using BlackjackCasino.Core.Sudoku;
using BlackjackCasino.Desktop;

namespace BlackjackCasino.Tests;

public sealed class LoungeRewardsAndCosmeticsTests
{
    [Theory]
    [InlineData(SudokuDifficulty.Relaxed, 150)]
    [InlineData(SudokuDifficulty.Easy, 250)]
    [InlineData(SudokuDifficulty.Medium, 400)]
    [InlineData(SudokuDifficulty.Hard, 600)]
    [InlineData(SudokuDifficulty.Expert, 850)]
    [InlineData(SudokuDifficulty.Master, 1200)]
    public void SudokuPaysScaledPlayerAndSeasonXpOnceAcrossReloads(SudokuDifficulty difficulty, long xp)
    {
        var state = new PlayerProfileState();
        var service = new ProgressionService(state);
        var completion = new SudokuCompletionRecord("reward-test", difficulty, SudokuPuzzlePool.Daily, 900, 3);
        ProgressionAward award = new SudokuProgression(service).Process(completion);
        Assert.Equal(xp, award.PlayerExperience);
        Assert.Equal(xp / 2, award.SeasonExperience);
        Assert.Equal(xp, state.LifetimeExperience);
        Assert.Equal(xp / 2, service.GetOrCreateSeasonProgress(state.ActiveSeasonId).Experience);
        Assert.Equal(1, state.Sudoku.CompletedPuzzles);
        Assert.Equal(1, state.Sudoku.DailyCompleted);
        var restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(state))!;
        Assert.Equal(ProgressionAward.None, new SudokuProgression(new ProgressionService(restored)).Process(completion));
        Assert.Equal(xp, restored.LifetimeExperience);
        Assert.Equal(1, restored.Sudoku.CompletedPuzzles);
    }

    [Fact]
    public void SudokuSaveFailureCanRetryWithoutLosingOrDuplicatingRewards()
    {
        bool fail = true;
        PlayerProfileState? saved = null;
        using var controller = new PlayerProgressionController(new PlayerProfileState(), kniffelProfileSaver: candidate =>
        {
            if (fail) throw new IOException("Simulated disk failure");
            saved = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(candidate));
        });
        var completion = new SudokuCompletionRecord("transaction-test", SudokuDifficulty.Hard, SudokuPuzzlePool.Normal, 1000, 0);
        Assert.Throws<IOException>(() => controller.ProcessSudoku(completion));
        Assert.Equal(0, controller.Profile.LifetimeExperience);
        Assert.Empty(controller.Profile.Sudoku.RecordedPuzzleIds);
        fail = false;
        Assert.Equal(600, controller.ProcessSudoku(completion).PlayerExperience);
        Assert.NotNull(saved);
        Assert.Equal(1, controller.Profile.Sudoku.CompletedPuzzles);
        Assert.Contains(completion.PuzzleId, saved.Sudoku.RecordedPuzzleIds);
        Assert.Equal(ProgressionAward.None, controller.ProcessSudoku(completion));
        using var reopened = new PlayerProgressionController(saved);
        Assert.Equal(ProgressionAward.None, reopened.ProcessSudoku(completion));
    }

    [Fact]
    public void DiceCosmeticsMigratePurchaseEquipAndSurviveOtherGameRewards()
    {
        var profile = new PlayerProfileState { Coins = 10000, EquippedDiceSetId = "missing-set", OwnedCosmeticIds = [] };
        var service = new ProgressionService(profile);
        Assert.Equal(CosmeticCatalog.DefaultDiceSetId, profile.EquippedDiceSetId);
        Assert.Contains(CosmeticCatalog.DefaultDiceSetId, profile.OwnedCosmeticIds);
        Assert.False(service.EquipCosmetic("diceset-jade"));
        Assert.True(service.PurchaseCosmetic("diceset-jade").Success);
        Assert.Equal(6800, profile.Coins);
        Assert.True(service.EquipCosmetic("diceset-jade"));
        Assert.Equal(CosmeticCatalog.DefaultCardSetId, profile.EquippedCardSetId);
        using var controller = new PlayerProgressionController(profile);
        controller.ProcessSudoku(new("keep-dice", SudokuDifficulty.Easy, SudokuPuzzlePool.Normal, 700, 1));
        Assert.Equal("diceset-jade", controller.Profile.EquippedDiceSetId);
        var restored = JsonSerializer.Deserialize<PlayerProfileState>(JsonSerializer.Serialize(profile))!;
        _ = new ProgressionService(restored);
        Assert.Equal("diceset-jade", restored.EquippedDiceSetId);
        Assert.Equal(6, CosmeticCatalog.ShopItems.Count(c => c.Kind == CosmeticKind.DiceSet));
    }

    [Fact]
    public void AddedChallengesUseTheirOwnGameStatisticsAndClaimOnlyOnce()
    {
        var profile = new PlayerProfileState();
        var service = new ProgressionService(profile);
        Assert.Equal(21, ChallengeCatalog.For(ChallengeCategory.Sudoku).Count);
        Assert.Equal(30, ChallengeCatalog.For(ChallengeCategory.MidnightDice).Count);
        Assert.False(service.ClaimChallenge("sudoku-25").Success);
        Assert.False(service.ClaimChallenge("dice-table-10").Success);
        profile.Sudoku.CompletedPuzzles = 25;
        profile.Kniffel.CasinoGamesCompleted = 10;
        Assert.True(service.ClaimChallenge("sudoku-25").Success);
        Assert.True(service.ClaimChallenge("dice-table-10").Success);
        long coins = profile.Coins;
        Assert.False(service.ClaimChallenge("sudoku-25").Success);
        Assert.False(service.ClaimChallenge("dice-table-10").Success);
        Assert.Equal(coins, profile.Coins);
    }
}
