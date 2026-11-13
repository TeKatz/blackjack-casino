namespace BlackjackCasino.Core.Sudoku;

/// <summary>Independent 100-level Sudoku career, never reset by casino prestige.</summary>
public sealed class SudokuCareerState
{
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public long LifetimeExperience { get; set; }
    public long CosmeticPointsEarned { get; set; }

    public void Normalize()
    {
        Level = Math.Clamp(Level, 1, SudokuCareer.MaximumLevel);
        Experience = Level == SudokuCareer.MaximumLevel
            ? 0 : Math.Clamp(Experience, 0, SudokuCareer.ExperienceForNextLevel(Level) - 1);
        LifetimeExperience = Math.Max(0, LifetimeExperience);
        CosmeticPointsEarned = Math.Max(0, CosmeticPointsEarned);
    }
}

public static class SudokuCareer
{
    public const int MaximumLevel = 100;
    public const int CosmeticPointsPerLevel = 25;

    public static long ExperienceForNextLevel(int level)
    {
        if (level < 1 || level >= MaximumLevel)
            throw new ArgumentOutOfRangeException(nameof(level));
        long n = level - 1L;
        return 100L + 9L * n + n * n / 8L;
    }

    public static long TotalExperienceToLevel(int level)
    {
        if (level < 1 || level > MaximumLevel)
            throw new ArgumentOutOfRangeException(nameof(level));
        long total = 0;
        for (int i = 1; i < level; i++)
            total = checked(total + ExperienceForNextLevel(i));
        return total;
    }

    public static int ExperienceForPuzzle(SudokuDifficulty difficulty) => difficulty switch
    {
        SudokuDifficulty.Relaxed => 25,
        SudokuDifficulty.Easy => 40,
        SudokuDifficulty.Medium => 65,
        SudokuDifficulty.Hard => 100,
        SudokuDifficulty.Expert => 145,
        SudokuDifficulty.Master => 200,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };

    public static IReadOnlyList<string> TitlesAtLevel(int level)
    {
        if (level < 10 || level > MaximumLevel || level % 10 != 0) return [];
        return level switch
        {
            90 => ["title-sudoku-90", "title-sudoku-90-premium"],
            100 => ["title-sudoku-100", "title-sudoku-100-ultra"],
            _ => ["title-sudoku-" + level.ToString(System.Globalization.CultureInfo.InvariantCulture)],
        };
    }

    /// <summary>Call only after accepting an idempotent Sudoku completion receipt.</summary>
    public static void Grant(PlayerProfileState profile, SudokuDifficulty difficulty) =>
        GrantPoints(profile, ExperienceForPuzzle(difficulty));

    public static int ExperienceForVariant(string mode, SudokuDifficulty difficulty, int miniSize)
    {
        int xp = ExperienceForPuzzle(difficulty);
        if (mode == "samurai") xp *= 3;
        else if (mode == "killer") xp = xp * 3 / 2;
        else if (mode == "mini") xp /= miniSize == 4 ? 3 : 2;
        return xp;
    }

    public static void GrantVariant(PlayerProfileState profile, string mode,
        SudokuDifficulty difficulty, int miniSize) =>
        GrantPoints(profile, ExperienceForVariant(mode, difficulty, miniSize));

    private static void GrantPoints(PlayerProfileState profile, int xp)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.SudokuCareer ??= new();
        SudokuCareerState career = profile.SudokuCareer;
        career.Normalize();
        career.LifetimeExperience = checked(career.LifetimeExperience + xp);
        if (career.Level == MaximumLevel) return;
        career.Experience += xp;
        while (career.Level < MaximumLevel && career.Experience >= ExperienceForNextLevel(career.Level))
        {
            career.Experience -= ExperienceForNextLevel(career.Level);
            career.Level++;
            profile.Coins = checked(profile.Coins + CosmeticPointsPerLevel);
            career.CosmeticPointsEarned = checked(career.CosmeticPointsEarned + CosmeticPointsPerLevel);
            foreach (string title in TitlesAtLevel(career.Level))
                if (!profile.OwnedCosmeticIds.Contains(title, StringComparer.Ordinal))
                    profile.OwnedCosmeticIds.Add(title);
        }
        if (career.Level == MaximumLevel) career.Experience = 0;
    }
}
