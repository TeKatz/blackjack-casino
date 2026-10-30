namespace BlackjackCasino.Core.Kniffel;

public sealed class KniffelStatistics
{
    public HashSet<Guid> RewardedGameIds { get; set; } = [];
    public long CompletedGames { get; set; }
    public long TotalScore { get; set; }
    public int BestScore { get; set; }
}

/// <summary>Completion rewards remain separate from Blackjack hands and cash statistics.</summary>
public sealed class KniffelProgression(ProgressionService progression)
{
    public static long PlayerExperience(int score) => 200L + 2L * score;
    public static long SeasonExperience(int score) => 100L + score;

    public ProgressionAward Process(KniffelState checkpoint)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);
        if (checkpoint.GameId == Guid.Empty) throw new ArgumentException("A saved game ID is required.", nameof(checkpoint));
        KniffelGame game = KniffelGame.Restore(checkpoint);
        if (!game.IsComplete) return ProgressionAward.None;
        KniffelStatistics stats = progression.State.Kniffel;
        if (!stats.RewardedGameIds.Add(game.GameId)) return ProgressionAward.None;
        stats.CompletedGames++;
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
