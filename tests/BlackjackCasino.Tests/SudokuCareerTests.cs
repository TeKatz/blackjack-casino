using BlackjackCasino.Core;
using BlackjackCasino.Core.Sudoku;

namespace BlackjackCasino.Tests;

public sealed class SudokuCareerTests
{
    [Fact]
    public void HundredLevelCurveIsGrowingAndBounded()
    {
        Assert.Equal(0, SudokuCareer.TotalExperienceToLevel(1));
        Assert.True(SudokuCareer.TotalExperienceToLevel(100) > SudokuCareer.TotalExperienceToLevel(90));
        for (int i = 1; i < 99; i++)
            Assert.True(SudokuCareer.ExperienceForNextLevel(i + 1) >= SudokuCareer.ExperienceForNextLevel(i));
        Assert.Throws<ArgumentOutOfRangeException>(() => SudokuCareer.ExperienceForNextLevel(100));
    }

    [Fact]
    public void DuplicatePuzzleDoesNotAwardMoreCareerXp()
    {
        var profile = new PlayerProfileState();
        var progression = new SudokuProgression(new ProgressionService(profile));
        var receipt = new SudokuCompletionRecord("sudoku-career-test", SudokuDifficulty.Easy, SudokuPuzzlePool.Normal, 50, 0);
        Assert.True(progression.Process(receipt).PlayerExperience > 0);
        Assert.Equal(SudokuCareer.ExperienceForPuzzle(SudokuDifficulty.Easy), profile.SudokuCareer.LifetimeExperience);
        Assert.Equal(ProgressionAward.None, progression.Process(receipt));
        Assert.Equal(SudokuCareer.ExperienceForPuzzle(SudokuDifficulty.Easy), profile.SudokuCareer.LifetimeExperience);
    }

    [Fact]
    public void AllTwelveExclusiveTitlesUnlockAtMilestonesAndAreNotPurchasable()
    {
        var profile = new PlayerProfileState();
        for (int i = 0; i < 4000 && profile.SudokuCareer.Level < 100; i++)
            SudokuCareer.Grant(profile, SudokuDifficulty.Master);
        Assert.Equal(100, profile.SudokuCareer.Level);
        Assert.Equal(0, profile.SudokuCareer.Experience);
        Assert.Equal(99 * SudokuCareer.CosmeticPointsPerLevel, profile.SudokuCareer.CosmeticPointsEarned);
        for (int level = 10; level <= 100; level += 10)
            foreach (string id in SudokuCareer.TitlesAtLevel(level))
            {
                Assert.Contains(id, profile.OwnedCosmeticIds);
                Assert.False(CosmeticCatalog.Get(id).ShopAvailable);
                Assert.Equal(level, CosmeticCatalog.Get(id).RequiredSudokuLevel);
            }
        Assert.Equal(12, profile.OwnedCosmeticIds.Count(id => id.StartsWith("title-sudoku-", StringComparison.Ordinal)));
        var service = new ProgressionService(profile);
        Assert.False(service.PurchaseCosmetic("title-sudoku-100-ultra").Success);
        Assert.True(service.EquipCosmetic("title-sudoku-100-ultra"));
        long points = profile.SudokuCareer.CosmeticPointsEarned;
        SudokuCareer.Grant(profile, SudokuDifficulty.Master);
        Assert.Equal(points, profile.SudokuCareer.CosmeticPointsEarned);
    }

    [Fact]
    public void CareerNormalizationNeverResetsCasinoPrestige()
    {
        var profile = new PlayerProfileState { Prestige = 7,
            SudokuCareer = new SudokuCareerState { Level = 120, Experience = 500000 } };
        ProfileNormalization.Normalize(profile);
        Assert.Equal(100, profile.SudokuCareer.Level);
        Assert.Equal(0, profile.SudokuCareer.Experience);
        Assert.Equal(7, profile.Prestige);
    }
}
