namespace BlackjackCasino.Core.Kniffel;

public sealed class KniffelStatistics
{
    public HashSet<Guid> RewardedGameIds { get; set; } = [];
    public HashSet<Guid> EnteredTableIds { get; set; } = [];
    public HashSet<Guid> SettledTableIds { get; set; } = [];
    public long CasinoGamesStarted { get; set; }
    public long CasinoGamesCompleted { get; set; }
    public long CasinoWins { get; set; }
    public long PodiumFinishes { get; set; }
    public long Forfeits { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal TotalReturned { get; set; }
    public decimal BiggestPayout { get; set; }
    public decimal NetProfit => TotalReturned - TotalWagered;
    public long UpperBonuses { get; set; }
    public long FiveOfAKinds { get; set; }
    public long FullHouses { get; set; }
    public long LargeStraights { get; set; }
    public long CompletedGames { get; set; }
    public long TotalScore { get; set; }
    public int BestScore { get; set; }
}

/// <summary>Completion rewards remain separate from Blackjack hands and cash statistics.</summary>
public sealed class KniffelProgression(ProgressionService progression)
{
    public static long PlayerExperience(int score) => 200L + 2L * score;
    public static long SeasonExperience(int score) => 100L + score;

    public ProgressionAward ProcessTable(KniffelTableState table)
    {
        KniffelTable.Validate(table);
        KniffelStatistics stats = progression.State.Kniffel;
        if (stats.EnteredTableIds.Add(table.Player.GameId))
        {
            stats.CasinoGamesStarted++;
            stats.TotalWagered += table.Stake;
        }
        if (!table.Settled || !stats.SettledTableIds.Add(table.Player.GameId)) return ProgressionAward.None;
        stats.TotalReturned += table.Payout;
        stats.BiggestPayout = Math.Max(stats.BiggestPayout, table.Payout);
        if (table.Forfeited) { stats.Forfeits++; return ProgressionAward.None; }
        stats.CasinoGamesCompleted++;
        stats.CasinoWins += table.Placement == 1 ? 1 : 0;
        stats.PodiumFinishes += table.Placement <= 2 ? 1 : 0;
        return Process(table.Player);
    }

    public ProgressionAward Process(KniffelState checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.GameId == Guid.Empty) throw new ArgumentException("A saved game ID is required.", nameof(checkpoint));
        KniffelGame game = KniffelGame.Restore(checkpoint);
        if (!game.IsComplete) return ProgressionAward.None;
        KniffelStatistics stats = progression.State.Kniffel;
        if (!stats.RewardedGameIds.Add(game.GameId)) return ProgressionAward.None;
        stats.CompletedGames++;
        stats.UpperBonuses += game.UpperBonus > 0 ? 1 : 0;
        stats.FiveOfAKinds += game.KniffelsScored;
        stats.FullHouses += game.ScoreFor(KniffelCategory.FullHouse) > 0 ? 1 : 0;
        stats.LargeStraights += game.ScoreFor(KniffelCategory.LargeStraight) > 0 ? 1 : 0;
        stats.TotalScore += game.Total;
        stats.BestScore = Math.Max(stats.BestScore, game.Total);
        long playerXp = PlayerExperience(game.Total), seasonXp = SeasonExperience(game.Total);
        ProgressionAward player = progression.GrantPlayerExperience(playerXp);
        ProgressionAward season = progression.GrantSeasonExperience(progression.State.ActiveSeasonId, seasonXp);
        progression.State.LastPlayedAt = DateTimeOffset.UtcNow;
        return new(playerXp, seasonXp, player.LevelsGained, season.SeasonTiersGained,
            player.CoinsEarned + season.CoinsEarned, player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());
    }
}
