namespace BlackjackCasino.Core;

/// <summary>
/// Shared reward plumbing for non-wagering Chill Lounge games.
/// Each game remains responsible for its own durable completion receipt/statistics,
/// while this helper keeps player XP, season XP, level rewards and timestamps consistent.
/// </summary>
public static class LoungeProgression
{
    public static ProgressionAward GrantOnce(
        ProgressionService progression,
        Func<bool> tryRecordCompletion,
        long playerExperience,
        long seasonExperience)
    {
        ArgumentNullException.ThrowIfNull(progression);
        ArgumentNullException.ThrowIfNull(tryRecordCompletion);

        if (playerExperience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerExperience));
        }
        if (seasonExperience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seasonExperience));
        }
        if (!tryRecordCompletion())
        {
            return ProgressionAward.None;
        }

        ProgressionAward player = progression.GrantPlayerExperience(playerExperience);
        ProgressionAward season = progression.GrantSeasonExperience(progression.State.ActiveSeasonId, seasonExperience);
        progression.State.LastPlayedAt = DateTimeOffset.UtcNow;

        return new ProgressionAward(
            playerExperience,
            seasonExperience,
            player.LevelsGained,
            season.SeasonTiersGained,
            player.CoinsEarned + season.CoinsEarned,
            player.CosmeticsUnlocked.Concat(season.CosmeticsUnlocked).ToArray());
    }
}
