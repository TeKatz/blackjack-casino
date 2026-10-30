namespace BlackjackCasino.Core;

public enum CosmeticKind
{
    Title,
    Frame,
    Portrait,
    CardSet,
    DiceSet,
}

public sealed record CosmeticDefinition(
    string Id,
    string Name,
    CosmeticKind Kind,
    long Price,
    bool ShopAvailable,
    string Description,
    string VisualVariant = "standard",
    int RequiredPrestige = 0);

public sealed class StatisticsSnapshot
{
    public long RoundsPlayed { get; set; }
    public long HandsPlayed { get; set; }
    public long HandsWon { get; set; }
    public long HandsLost { get; set; }
    public long Pushes { get; set; }
    public long Blackjacks { get; set; }
    public long Doubles { get; set; }
    public long DoubleWins { get; set; }
    public long Splits { get; set; }
    public long Surrenders { get; set; }
    public long InsuranceTaken { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal NetProfit { get; set; }
    public decimal InsuranceNet { get; set; }
    public decimal BiggestWin { get; set; }
    public decimal BiggestLoss { get; set; }
    public decimal HighestBankroll { get; set; }
    public decimal LowestBankroll { get; set; }
    public int CurrentWinStreak { get; set; }
    public int BestWinStreak { get; set; }
    public int CurrentLossStreak { get; set; }
    public int WorstLossStreak { get; set; }
}

public sealed class SeasonProgressState
{
    public string SeasonId { get; set; } = SeasonCatalog.Current.Id;
    public long Experience { get; set; }
    public int Tier { get; set; }
}

public sealed class PlayerProfileState
{
    public int SchemaVersion { get; set; } = 5;
    public string DisplayName { get; set; } = "Player";
    public int Level { get; set; }
    public long LevelExperience { get; set; }
    public int Prestige { get; set; }
    public long Coins { get; set; }
    public long LifetimeExperience { get; set; }
    public long TotalPlaytimeSeconds { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastPlayedAt { get; set; } = DateTimeOffset.UtcNow;
    public string EquippedTitleId { get; set; } = CosmeticCatalog.DefaultTitleId;
    public string EquippedFrameId { get; set; } = CosmeticCatalog.DefaultFrameId;
    public string EquippedPortraitId { get; set; } = CosmeticCatalog.DefaultPortraitId;
    public string EquippedCardSetId { get; set; } = CosmeticCatalog.DefaultCardSetId;
    public string EquippedDiceSetId { get; set; } = CosmeticCatalog.DefaultDiceSetId;
    public List<string> OwnedCosmeticIds { get; set; } =
    [
        CosmeticCatalog.DefaultTitleId,
        CosmeticCatalog.DefaultFrameId,
        CosmeticCatalog.DefaultPortraitId,
        CosmeticCatalog.DefaultCardSetId,
        CosmeticCatalog.DefaultDiceSetId,
    ];
    public List<string> ClaimedChallengeIds { get; set; } = [];
    public Kniffel.KniffelStatistics Kniffel { get; set; } = new();
    public Sudoku.SudokuStatistics Sudoku { get; set; } = new();
    public Manor.ManorStatistics Manor { get; set; } = new();
    public CasinoVaultState Vault { get; set; } = new();
    // Zero also identifies saves with no version field. Normalize before recording new spins.
    public int SlotTrackingVersion { get; set; }
    /// <summary>Verified Midnight Reels play since separate tracking began.</summary>
    public SlotStatistics Slots { get; set; } = new();
    public NeonVaultStatistics NeonVault { get; set; } = new();
    public NineLivesStatistics NineLives { get; set; } = new();
    /// <summary>Unattributable historical Reels + Neon Vault totals; never used for game-specific goals.</summary>
    public SlotStatistics LegacyPooledSlots { get; set; } = new();
    public StatisticsSnapshot LifetimeStatistics { get; set; } = new();
    public Dictionary<string, StatisticsSnapshot> TableStatistics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, SeasonProgressState> Seasons { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string ActiveSeasonId { get; set; } = SeasonCatalog.Current.Id;
}

public sealed record ProgressionAward(
    long PlayerExperience,
    long SeasonExperience,
    IReadOnlyList<int> LevelsGained,
    int SeasonTiersGained,
    long CoinsEarned,
    IReadOnlyList<CosmeticDefinition> CosmeticsUnlocked)
{
    public static ProgressionAward None { get; } = new(0, 0, [], 0, 0, []);

    public bool HasLevelUp => LevelsGained.Count > 0;
}

public sealed record PurchaseResult(bool Success, string Message, CosmeticDefinition? Cosmetic = null);

public sealed record ChallengeClaimResult(
    bool Success,
    string Message,
    ChallengeDefinition? Challenge = null,
    long CoinsEarned = 0);

public sealed record ChallengeClaimAllResult(
    bool Success,
    string Message,
    IReadOnlyList<ChallengeDefinition> Challenges,
    long CoinsEarned = 0)
{
    public int ClaimsCount => Challenges.Count;
}

public sealed record PrestigeResult(
    bool Success,
    int Prestige,
    long CoinsEarned,
    IReadOnlyList<CosmeticDefinition> CosmeticsUnlocked,
    string Message);
